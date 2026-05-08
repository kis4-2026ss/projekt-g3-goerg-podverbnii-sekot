#!/usr/bin/env python3
import argparse
import json
import pathlib
import subprocess
import sys
from dataclasses import dataclass
from typing import Any


API_VERSION = "2022-11-28"


@dataclass
class Finding:
    file: str
    line: int
    comment: str


def run_cmd(cmd: list[str], cwd: pathlib.Path | None = None) -> str:
    result = subprocess.run(
        cmd,
        cwd=str(cwd) if cwd else None,
        text=True,
        capture_output=True,
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"Command failed: {' '.join(cmd)}\nSTDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}"
        )
    return result.stdout.strip()


def gh_api(endpoint: str) -> Any:
    cmd = [
        "gh",
        "api",
        "-H",
        "Accept: application/vnd.github+json",
        "-H",
        f"X-GitHub-Api-Version: {API_VERSION}",
        endpoint,
    ]
    return json.loads(run_cmd(cmd))


def gh_api_post(endpoint: str, payload: dict[str, Any]) -> Any:
    cmd = [
        "gh",
        "api",
        "--method",
        "POST",
        "-H",
        "Accept: application/vnd.github+json",
        "-H",
        f"X-GitHub-Api-Version: {API_VERSION}",
        endpoint,
        "--input",
        "-",
    ]
    result = subprocess.run(
        cmd,
        input=json.dumps(payload),
        text=True,
        capture_output=True,
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"POST failed: {' '.join(cmd)}\nSTDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}\nPAYLOAD:\n{json.dumps(payload, indent=2, ensure_ascii=False)}"
        )
    return json.loads(result.stdout) if result.stdout.strip() else None


def parse_repo_full_name(repo_dir: pathlib.Path) -> tuple[str, str]:
    remote_url = run_cmd(["git", "remote", "get-url", "origin"], cwd=repo_dir).strip()

    if remote_url.startswith("git@github.com:"):
        full_name = remote_url[len("git@github.com:"):]
    elif remote_url.startswith("https://github.com/"):
        full_name = remote_url[len("https://github.com/"):]
    else:
        raise RuntimeError(f"Unsupported remote URL: {remote_url}")

    if full_name.endswith(".git"):
        full_name = full_name[:-4]

    parts = full_name.split("/", 1)
    if len(parts) != 2:
        raise RuntimeError(f"Could not parse owner/repo from remote URL: {remote_url}")

    return parts[0], parts[1]


def find_feedback_pr(owner: str, repo: str, head_branch_hint: str = "feedback") -> dict[str, Any] | None:
    prs = gh_api(f"/repos/{owner}/{repo}/pulls?state=open&per_page=100")
    for pr in prs:
        title = (pr.get("title") or "").lower()
        head_ref = ((pr.get("head") or {}).get("ref") or "").lower()
        if "feedback" in title or head_branch_hint in head_ref:
            return pr
    return None


def load_review_file(review_file: pathlib.Path) -> tuple[str, list[Finding]]:
    data = json.loads(review_file.read_text(encoding="utf-8"))
    summary = str(data.get("summary", "")).strip()

    findings: list[Finding] = []

    for file_entry in data.get("files", []):
        if not isinstance(file_entry, dict):
            continue

        for item in file_entry.get("findings", []):
            try:
                file_name = str(item["file"]).strip()
                line = int(item["line"])
                comment = str(item["comment"]).strip()
            except (KeyError, ValueError, TypeError):
                continue

            if file_name and line > 0 and comment:
                findings.append(Finding(file=file_name, line=line, comment=comment))

    return summary, findings


def normalize_path(path: str) -> str:
    return path.replace("\\", "/").lstrip("./")


def build_new_line_to_position_map(patch: str) -> dict[int, int]:
    pos = 0
    new_line = None
    old_line = None
    mapping: dict[int, int] = {}

    for raw_line in patch.splitlines():
        pos += 1

        if raw_line.startswith("@@"):
            try:
                header = raw_line.split("@@")[1].strip()
                old_part, new_part = header.split(" ")[:2]
                old_start = int(old_part.split(",")[0][1:])
                new_start = int(new_part.split(",")[0][1:])
                old_line = old_start
                new_line = new_start
            except Exception as exc:
                raise RuntimeError(f"Could not parse patch hunk header: {raw_line}") from exc
            continue

        if new_line is None or old_line is None:
            continue

        if raw_line.startswith("+") and not raw_line.startswith("+++"):
            mapping[new_line] = pos
            new_line += 1
        elif raw_line.startswith("-") and not raw_line.startswith("---"):
            old_line += 1
        else:
            mapping[new_line] = pos
            new_line += 1
            old_line += 1

    return mapping


def list_pr_files(owner: str, repo: str, pull_number: int) -> list[dict[str, Any]]:
    return gh_api(f"/repos/{owner}/{repo}/pulls/{pull_number}/files?per_page=100")


def build_comment_targets(
    findings: list[Finding],
    pr_files: list[dict[str, Any]],
) -> tuple[list[dict[str, Any]], list[Finding]]:
    file_index: dict[str, dict[str, Any]] = {}
    line_maps: dict[str, dict[int, int]] = {}

    for f in pr_files:
        filename = normalize_path(f.get("filename", ""))
        if not filename:
            continue

        file_index[filename] = f

        patch = f.get("patch")
        if patch:
            line_maps[filename] = build_new_line_to_position_map(patch)

    comments: list[dict[str, Any]] = []
    leftovers: list[Finding] = []

    for finding in findings:
        normalized_finding_path = normalize_path(finding.file)
        target = file_index.get(normalized_finding_path)

        if not target:
            leftovers.append(finding)
            continue

        line_map = line_maps.get(normalized_finding_path)
        if not line_map:
            leftovers.append(finding)
            continue

        position = line_map.get(finding.line)
        if position is None:
            leftovers.append(finding)
            continue

        comments.append(
            {
                "path": target["filename"],
                "position": position,
                "body": finding.comment,
            }
        )

    return comments, leftovers


def build_review_body(summary: str, leftovers: list[Finding]) -> str:
    parts: list[str] = []

    if summary:
        parts.append(summary)

    if leftovers:
        leftover_lines = ["Nicht inline zuordenbare Punkte:"]
        for item in leftovers:
            leftover_lines.append(f"- `{item.file}:{item.line}` — {item.comment}")
        parts.append("\n".join(leftover_lines))

    if not parts:
        parts.append("Automatisches Erstfeedback.")

    return "\n\n".join(parts)


def create_pending_review(
    owner: str,
    repo: str,
    pull_number: int,
    body: str,
    comments: list[dict[str, Any]],
    dry_run: bool,
) -> int | None:
    payload: dict[str, Any] = {
        "body": body,
    }

    if comments:
        payload["comments"] = comments

    if dry_run:
        print(json.dumps(payload, indent=2, ensure_ascii=False))
        return None

    response = gh_api_post(f"/repos/{owner}/{repo}/pulls/{pull_number}/reviews", payload)
    return int(response["id"])


def submit_review(
    owner: str,
    repo: str,
    pull_number: int,
    review_id: int,
    body: str,
    dry_run: bool,
) -> None:
    payload = {
        "body": body,
        "event": "COMMENT",
    }

    if dry_run:
        print(json.dumps(payload, indent=2, ensure_ascii=False))
        return

    gh_api_post(
        f"/repos/{owner}/{repo}/pulls/{pull_number}/reviews/{review_id}/events",
        payload,
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Create or submit pending GitHub PR reviews from AI review JSON.")
    parser.add_argument("--repos-dir", required=True, type=pathlib.Path, help="Directory containing cloned repos")
    parser.add_argument("--reviews-dir", required=True, type=pathlib.Path, help="Directory containing review JSON files")
    parser.add_argument("--repo-filter", default="", help="Optional substring filter for repo names")
    parser.add_argument("--dry-run", action="store_true", help="Print payload instead of posting")
    parser.add_argument("--submit", action="store_true", help="Submit the created review immediately")
    args = parser.parse_args()

    if not args.repos_dir.exists():
        print(f"Repos dir not found: {args.repos_dir}", file=sys.stderr)
        sys.exit(1)

    if not args.reviews_dir.exists():
        print(f"Reviews dir not found: {args.reviews_dir}", file=sys.stderr)
        sys.exit(1)

    repo_dirs = sorted(
        p for p in args.repos_dir.iterdir()
        if p.is_dir() and (p / ".git").exists()
    )

    if args.repo_filter:
        repo_dirs = [p for p in repo_dirs if args.repo_filter.lower() in p.name.lower()]

    if not repo_dirs:
        print("No repositories found.")
        return

    for repo_dir in repo_dirs:
        repo_name = repo_dir.name
        review_file = args.reviews_dir / f"{repo_name}.json"

        print(f"\nProcessing {repo_name} ...")

        if not review_file.exists():
            print("  Skip: no review JSON found")
            continue

        try:
            owner, repo = parse_repo_full_name(repo_dir)
            pr = find_feedback_pr(owner, repo)
            if not pr:
                print("  Skip: no open feedback PR found")
                continue

            pull_number = pr["number"]
            summary, findings = load_review_file(review_file)
            pr_files = list_pr_files(owner, repo, pull_number)
            inline_comments, leftovers = build_comment_targets(findings, pr_files)
            body = build_review_body(summary, leftovers)

            print(f"  PR #{pull_number}")
            print(f"  Findings loaded: {len(findings)}")
            print(f"  Inline comments: {len(inline_comments)}")
            print(f"  Leftovers in body: {len(leftovers)}")

            review_id = create_pending_review(
                owner=owner,
                repo=repo,
                pull_number=pull_number,
                body=body,
                comments=inline_comments,
                dry_run=args.dry_run,
            )

            if args.dry_run:
                print("  Dry run complete")
                continue

            print(f"  Pending review created (ID: {review_id})")

            if args.submit and review_id is not None:
                submit_review(
                    owner=owner,
                    repo=repo,
                    pull_number=pull_number,
                    review_id=review_id,
                    body="Automatisches Erstfeedback.",
                    dry_run=args.dry_run,
                )
                print("  Review submitted")

        except Exception as exc:
            print(f"  ERROR: {exc}", file=sys.stderr)


if __name__ == "__main__":
    main()