#!/usr/bin/env python3
import argparse
import json
import os
import pathlib
import sys
import time
from typing import Any

from google import genai
from google.genai import types


SYSTEM_PROMPT = """Du bewertest eine Java-Hausübung.

Aufgabe:
- Bewerte nur den tatsächlich gegebenen Java-Code im Hinblick auf die Aufgabenstellung.
- Antworte ausschließlich auf Deutsch.
- Antworte ausschließlich mit validem JSON im vorgegebenen Format.

Allgemeine Regeln:
- Gib nur Verbesserungspunkte und mögliche Abzüge.
- Gib kein positives Feedback.
- Jeder Feedbackpunkt muss aus 1 bis 2 kurzen, direkt weiterleitbaren Sätzen bestehen.
- Sei konstruktiv, sachlich und nicht übertrieben kritisch.
- Bewerte im Rahmen einer Hausübung von Studierenden im 2. Semester, nicht wie Produktivcode.

Worauf du achten sollst:
- Korrektheit laut Aufgabenstellung
- sinnvolle Programmlogik
- Lesbarkeit
- Randfälle
- unnötige Komplexität
- hilfreiche Java-Sprachfeatures
- schlechter Stil im Code, wenn er vermeidbar ist und für zukünftige Aufgaben relevant wäre

Was du kritisieren sollst:
- fehlerhafte oder unvollständige Logik
- unnötig komplizierte Lösungen
- unnötig schwer lesbaren Code
- ineffiziente oder umständliche Konstruktionen, wenn eine deutlich einfachere Lösung naheliegt
- vermeidbaren schlechten Stil, z. B. unnötige break-Anweisungen in Schleifen
- unnötige vorzeitige Rückgaben mitten in Methoden, wenn sie nicht einfache Fehlerbehandlung am Anfang sind

Was du nicht kritisieren sollst:
- keine fehlenden Exceptions, da Exceptions noch nicht gelernt wurden
- keine Architekturfragen
- keine Skalierbarkeit
- keine Produktivcode-Kritik
- keine Unterklassenflexibilität
- keine Klassennamen
- keine informellen Kommentare, solange sie verständlich sind
- keine spekulativen oder unsicheren Punkte
- keine erfundenen Methoden, Anforderungen oder Probleme, die im Code nicht vorkommen

Zusätzliche Regeln:
- Wenn eine Datei erkennbar nur eine Testdatei ist und keine eigentliche Hausübungslogik enthält, gib für diese Datei keine Findings zurück.
- Nenne denselben Kritikpunkt pro Hausübung nur einmal.
- Gib lieber wenige, relevante und sichere Kritikpunkte als viele schwache.
- Maximal 6 Findings pro Datei.
- Wenn es keine relevanten Probleme gibt, gib findings als leeres Array zurück.
- Verwende als file-Wert genau den Dateipfad aus dem Input.
- Zeilennummern müssen sich auf die Zeilennummern im Input beziehen.
"""

RESPONSE_SCHEMA = {
    "type": "object",
    "properties": {
        "repo_name": {"type": "string"},
        "summary": {"type": "string"},
        "files": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "file": {"type": "string"},
                    "summary": {"type": "string"},
                    "findings": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "file": {"type": "string"},
                                "line": {"type": "integer"},
                                "comment": {"type": "string"},
                            },
                            "required": ["file", "line", "comment"],
                        },
                    },
                },
                "required": ["file", "summary", "findings"],
            },
        },
    },
    "required": ["repo_name", "summary", "files"],
}


def log(message: str) -> None:
    print(message, flush=True)


def find_repos(base_dir: pathlib.Path) -> list[pathlib.Path]:
    return sorted(
        p for p in base_dir.iterdir()
        if p.is_dir() and (p / ".git").exists()
    )


def find_relevant_java_files(repo_dir: pathlib.Path) -> list[pathlib.Path]:
    files = []
    ignored_dir_names = {"test", "tests"}

    for p in sorted(repo_dir.rglob("*.java")):
        parts_lower = {part.lower() for part in p.parts}
        name = p.name.lower()

        if ".git" in p.parts:
            continue

        if ignored_dir_names.intersection(parts_lower):
            continue

        if "test" in name:
            continue

        files.append(p)

    return files


def read_text_safe(file_path: pathlib.Path) -> str:
    try:
        return file_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return file_path.read_text(encoding="latin-1")


def build_repo_code_blob(java_files: list[pathlib.Path], repo_dir: pathlib.Path, max_chars: int) -> str:
    parts: list[str] = []
    used = 0

    for file_path in java_files:
        rel_path = str(file_path.relative_to(repo_dir))
        raw = read_text_safe(file_path)

        numbered_lines = []
        for idx, line in enumerate(raw.splitlines(), start=1):
            numbered_lines.append(f"{idx:4d}: {line}")

        block = f"\n===== FILE: {rel_path} =====\n" + "\n".join(numbered_lines) + "\n"

        if used + len(block) > max_chars:
            remaining = max_chars - used
            if remaining > 0:
                parts.append(block[:remaining])
            break

        parts.append(block)
        used += len(block)

    return "".join(parts)


def build_prompt(repo_name: str, code_blob: str) -> str:
    return f"""Repository: {repo_name}

Zu prüfender Code:
{code_blob}

Antworte nur mit JSON im geforderten Format.
Alle Texte in summary und comment müssen auf Deutsch sein.
"""


def normalize_review(repo_name: str, java_files: list[pathlib.Path], repo_dir: pathlib.Path, data: dict[str, Any]) -> dict[str, Any]:
    valid_files = {str(p.relative_to(repo_dir)) for p in java_files}

    normalized_files = []
    raw_files = data.get("files", [])
    if not isinstance(raw_files, list):
        raw_files = []

    for item in raw_files:
        if not isinstance(item, dict):
            continue

        file_name = str(item.get("file", "")).strip()
        if file_name not in valid_files:
            continue

        file_summary = str(item.get("summary", "")).strip()
        raw_findings = item.get("findings", [])
        if not isinstance(raw_findings, list):
            raw_findings = []

        findings = []
        for finding in raw_findings:
            if not isinstance(finding, dict):
                continue

            finding_file = str(finding.get("file", file_name)).strip() or file_name
            comment = str(finding.get("comment", "")).strip()

            try:
                line = int(finding.get("line", 0))
            except (TypeError, ValueError):
                line = 0

            if finding_file != file_name:
                continue

            if comment and line > 0:
                findings.append(
                    {
                        "file": file_name,
                        "line": line,
                        "comment": comment,
                    }
                )

        normalized_files.append(
            {
                "file": file_name,
                "summary": file_summary,
                "findings": findings,
            }
        )

    files_present = {item["file"] for item in normalized_files}
    for file_path in java_files:
        rel = str(file_path.relative_to(repo_dir))
        if rel not in files_present:
            normalized_files.append(
                {
                    "file": rel,
                    "summary": "",
                    "findings": [],
                }
            )

    normalized_files.sort(key=lambda x: x["file"].lower())

    return {
        "repo_name": repo_name,
        "summary": str(data.get("summary", "")).strip(),
        "files": normalized_files,
    }


def maybe_create_readme_cache(
    client: genai.Client,
    model_name: str,
    readme_text: str | None,
    ttl_seconds: int,
    enable_cache: bool,
) -> str | None:
    if not enable_cache or not readme_text:
        return None

    cache = client.caches.create(
        model=model_name,
        config=types.CreateCachedContentConfig(
            display_name="homework-readme-cache",
            contents=[readme_text],
            ttl=f"{ttl_seconds}s",
        ),
    )
    return cache.name


def generate_review_for_repo(
    client: genai.Client,
    model_name: str,
    repo_name: str,
    code_blob: str,
    temperature: float,
    cached_content_name: str | None = None,
    readme_text: str | None = None,
) -> dict[str, Any]:
    prompt_parts = []

    if not cached_content_name and readme_text:
        prompt_parts.append("Aufgabenstellung / README:\n")
        prompt_parts.append(readme_text.strip())
        prompt_parts.append("\n\n")

    prompt_parts.append(build_prompt(repo_name, code_blob))
    prompt = "".join(prompt_parts)

    config_kwargs = {
        "system_instruction": SYSTEM_PROMPT,
        "response_mime_type": "application/json",
        "response_json_schema": RESPONSE_SCHEMA,
        "temperature": temperature,
    }

    if cached_content_name:
        config_kwargs["cached_content"] = cached_content_name

    response = client.models.generate_content(
        model=model_name,
        contents=prompt,
        config=types.GenerateContentConfig(**config_kwargs),
    )

    text = response.text
    if not text:
        raise RuntimeError("Leere Antwort vom Modell erhalten.")

    return json.loads(text)


def save_repo_review(output_dir: pathlib.Path, repo_name: str, review: dict[str, Any]) -> pathlib.Path:
    output_dir.mkdir(parents=True, exist_ok=True)
    out_file = output_dir / f"{repo_name}.json"
    out_file.write_text(json.dumps(review, ensure_ascii=False, indent=2), encoding="utf-8")
    return out_file


def main() -> None:
    parser = argparse.ArgumentParser(description="Ein Gemini-Request pro Repo für Java-Hausübungen")
    parser.add_argument("--repos-dir", required=True, type=pathlib.Path, help="Ordner mit geklonten Repositories")
    parser.add_argument("--output-dir", required=True, type=pathlib.Path, help="Ordner für JSON-Reviews")
    parser.add_argument("--model", default="gemini-2.5-flash", help="Zu verwendendes Gemini-Modell")
    parser.add_argument("--repo-filter", default="", help="Optionaler Filter auf Repo-Namen")
    parser.add_argument("--max-chars", type=int, default=50000, help="Maximale Zeichenzahl pro Repo-Prompt")
    parser.add_argument("--temperature", type=float, default=0.2, help="Sampling-Temperatur")
    parser.add_argument("--sleep-seconds", type=float, default=0.0, help="Pause zwischen Requests")
    parser.add_argument("--readme-file", type=pathlib.Path, default=None, help="Pfad zur Aufgabenstellung / README.md")
    parser.add_argument("--use-readme-cache", action="store_true", help="README per Gemini explicit cache wiederverwenden")
    parser.add_argument("--cache-ttl-seconds", type=int, default=3600, help="TTL für den README-Cache")
    args = parser.parse_args()

    api_key = os.environ.get("GEMINI_API_KEY") or os.environ.get("GOOGLE_API_KEY")
    if not api_key:
        print("Fehler: GEMINI_API_KEY oder GOOGLE_API_KEY ist nicht gesetzt.", file=sys.stderr)
        sys.exit(1)

    if not args.repos_dir.exists():
        print(f"Fehler: Repos-Ordner existiert nicht: {args.repos_dir}", file=sys.stderr)
        sys.exit(1)

    if args.readme_file and not args.readme_file.exists():
        print(f"Fehler: README-Datei existiert nicht: {args.readme_file}", file=sys.stderr)
        sys.exit(1)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    client = genai.Client(api_key=api_key)

    readme_text = None
    cached_content_name = None

    if args.readme_file:
        readme_text = read_text_safe(args.readme_file)
        log(f"README geladen: {args.readme_file}")

        if args.use_readme_cache:
            try:
                log("README-Cache wird erstellt ...")
                cached_content_name = maybe_create_readme_cache(
                    client=client,
                    model_name=args.model,
                    readme_text=readme_text,
                    ttl_seconds=args.cache_ttl_seconds,
                    enable_cache=True,
                )
                log(f"README-Cache erstellt: {cached_content_name}")
            except Exception as exc:
                log(f"README-Cache konnte nicht erstellt werden: {exc}")
                log("Falle auf normales Mitsenden des README pro Request zurück.")
                cached_content_name = None

    repos = find_repos(args.repos_dir)
    if args.repo_filter:
        repos = [r for r in repos if args.repo_filter.lower() in r.name.lower()]

    if not repos:
        log("Keine Repositories gefunden.")
        return

    total_repos = len(repos)

    for repo_index, repo_dir in enumerate(repos, start=1):
        repo_name = repo_dir.name
        log(f"[{repo_index}/{total_repos}] Repo: {repo_name}")

        java_files = find_relevant_java_files(repo_dir)
        if not java_files:
            log("  -> Übersprungen: keine relevanten .java-Dateien gefunden")
            continue

        log("  -> Verwendete Dateien:")
        for file_path in java_files:
            log(f"     - {file_path.relative_to(repo_dir)}")

        code_blob = build_repo_code_blob(java_files, repo_dir, args.max_chars)
        if len(code_blob) >= args.max_chars:
            log(f"  -> Prompt wurde auf {args.max_chars} Zeichen begrenzt")

        try:
            log(f"  -> Request an Modell '{args.model}' gestartet")
            started = time.perf_counter()

            raw_review = generate_review_for_repo(
                client=client,
                model_name=args.model,
                repo_name=repo_name,
                code_blob=code_blob,
                temperature=args.temperature,
                cached_content_name=cached_content_name,
                readme_text=readme_text,
            )

            duration = time.perf_counter() - started
            review = normalize_review(repo_name, java_files, repo_dir, raw_review)
            out_file = save_repo_review(args.output_dir, repo_name, review)

            finding_count = sum(len(file_item["findings"]) for file_item in review["files"])

            log(f"  -> Antwort erhalten in {duration:.2f}s")
            log(f"  -> Gespeichert: {out_file}")
            log(f"  -> Dateien im Review: {len(review['files'])}")
            log(f"  -> Gesamt-Findings: {finding_count}")

        except Exception as exc:
            log(f"  -> FEHLER: {exc}")

        if args.sleep_seconds > 0:
            time.sleep(args.sleep_seconds)

        log("")

    log("Fertig.")


if __name__ == "__main__":
    main()