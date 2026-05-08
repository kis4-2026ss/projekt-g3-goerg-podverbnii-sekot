#!/usr/bin/env python3
import argparse
import pathlib
import subprocess
import sys
from typing import Any


API_VERSION = "2022-11-28"


def run_cmd(cmd: list[str], capture_output: bool = True) -> str:
    try:
        result = subprocess.run(
            cmd,
            check=True,
            text=True,
            capture_output=capture_output,
        )
        return result.stdout if capture_output else ""
    except subprocess.CalledProcessError as exc:
        print(f"Command failed: {' '.join(cmd)}", file=sys.stderr)
        if exc.stdout:
            print(exc.stdout, file=sys.stderr)
        if exc.stderr:
            print(exc.stderr, file=sys.stderr)
        sys.exit(1)


def load_students(file_path: pathlib.Path) -> set[str]:
    students = set()

    with file_path.open("r", encoding="utf-8") as f:
        for line in f:
            value = line.strip()
            if value and not value.startswith("#"):
                students.add(value)

    return students


def gh_api_paginated(endpoint: str) -> list[dict[str, Any]]:
    cmd = [
        "gh",
        "api",
        "--paginate",
        "-H",
        "Accept: application/vnd.github+json",
        "-H",
        f"X-GitHub-Api-Version: {API_VERSION}",
        endpoint,
    ]
    output = run_cmd(cmd)
    import json
    return json.loads(output)


def get_match_key(entry: dict[str, Any], mode: str) -> set[str]:
    values: set[str] = set()

    if mode == "login":
        for student in entry.get("students") or []:
            login = student.get("login")
            if login:
                values.add(login.strip())

    elif mode == "roster":
        roster_identifier = entry.get("roster_identifier")
        if roster_identifier:
            values.add(roster_identifier.strip())

    return values


def clone_repo(repo_full_name: str, target_dir: pathlib.Path, dry_run: bool) -> None:
    repo_name = repo_full_name.split("/")[-1]
    destination = target_dir / repo_name

    if destination.exists():
        print(f"Skip existing: {repo_full_name}")
        return

    cmd = ["gh", "repo", "clone", repo_full_name, str(destination)]

    if dry_run:
        print(f"[DRY RUN] {' '.join(cmd)}")
        return

    print(f"Cloning: {repo_full_name}")
    run_cmd(cmd, capture_output=False)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Clone only selected GitHub Classroom student repositories."
    )
    parser.add_argument(
        "--assignment-id",
        required=True,
        type=int,
        help="GitHub Classroom assignment ID",
    )
    parser.add_argument(
        "--students-file",
        required=True,
        type=pathlib.Path,
        help="Path to text file containing one student login or roster identifier per line",
    )
    parser.add_argument(
        "--match-by",
        choices=["login", "roster"],
        default="login",
        help="Match student list against GitHub login or roster_identifier",
    )
    parser.add_argument(
        "--output-dir",
        type=pathlib.Path,
        default=pathlib.Path("cloned_repos"),
        help="Directory where repositories will be cloned",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be cloned without actually cloning",
    )

    args = parser.parse_args()

    if not args.students_file.exists():
        print(f"Student list not found: {args.students_file}", file=sys.stderr)
        sys.exit(1)

    students = load_students(args.students_file)
    if not students:
        print("Student list is empty.", file=sys.stderr)
        sys.exit(1)

    args.output_dir.mkdir(parents=True, exist_ok=True)

    endpoint = f"/assignments/{args.assignment_id}/accepted_assignments"
    accepted_assignments = gh_api_paginated(endpoint)

    matched_repos: list[str] = []
    seen_repos: set[str] = set()

    for entry in accepted_assignments:
        entry_keys = get_match_key(entry, args.match_by)
        if not entry_keys.intersection(students):
            continue

        repository = entry.get("repository") or {}
        repo_full_name = repository.get("full_name")
        if not repo_full_name or repo_full_name in seen_repos:
            continue

        matched_repos.append(repo_full_name)
        seen_repos.add(repo_full_name)

    if not matched_repos:
        print("No matching student repositories found.")
        return

    print(f"Found {len(matched_repos)} matching repositories.\n")

    for repo_full_name in matched_repos:
        clone_repo(repo_full_name, args.output_dir, args.dry_run)


if __name__ == "__main__":
    main()