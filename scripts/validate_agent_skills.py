#!/usr/bin/env python3
"""Validate repo-local Codex skill manifests with no external Python packages.

This intentionally validates wAInery's small, supported YAML subset only:
single-line 'name:' and 'description:'. It does not claim full YAML validation.
"""
from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path

NAME = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
LINE = re.compile(r"^(name|description):\s*(.+?)\s*$")


def validate_skill(path: Path) -> list[str]:
    errors: list[str] = []
    source = path.read_text(encoding="utf-8")
    if not source.startswith("---\n"):
        return [f"{path}: missing opening YAML frontmatter marker"]
    parts = source.split("---\n", 2)
    if len(parts) < 3:
        return [f"{path}: missing closing YAML frontmatter marker"]
    metadata: dict[str, str] = {}
    for line in parts[1].splitlines():
        if not line.strip():
            continue
        match = LINE.fullmatch(line)
        if not match:
            errors.append(f"{path}: unexpected frontmatter line: {line!r}")
            continue
        key, value = match.groups()
        if key in metadata:
            errors.append(f"{path}: duplicated {key}")
        metadata[key] = value.strip("'\"")
    name = metadata.get("name", "")
    description = metadata.get("description", "")
    if not name or not NAME.fullmatch(name):
        errors.append(f"{path}: name must be nonempty kebab-case")
    if name != path.parent.name:
        errors.append(f"{path}: name must match containing directory")
    if not description or len(description) > 240:
        errors.append(f"{path}: description must be 1–240 characters")
    body = parts[2]
    for marker in ("Use when:", "Do not use", "Stop:"):
        if marker not in body:
            errors.append(f"{path}: workflow missing {marker!r}")
    return errors


def validate_repository(root: Path) -> list[str]:
    base = root / ".agents" / "skills"
    registry = root / "agents" / "knowledge" / "skill-registry.md"
    if not base.is_dir() or not registry.is_file():
        return ["Skill directory or registry is missing"]
    paths = sorted(base.glob("*/SKILL.md"))
    if not paths:
        return ["No registered skill files"]
    errors: list[str] = []
    entries = registry.read_text(encoding="utf-8")
    for path in paths:
        errors.extend(validate_skill(path))
        if path.parent.name not in entries:
            errors.append(f"{path}: no matching inventory entry")
    return errors


def self_test() -> list[str]:
    with tempfile.TemporaryDirectory() as td:
        root = Path(td)
        path = root / "sample-skill" / "SKILL.md"
        path.parent.mkdir()
        path.write_text(
            "---\nname: sample-skill\ndescription: Validate a sample after change.\n"
            "---\nUse when: applicable.\nDo not use for unrelated work.\nStop: on error.\n",
            encoding="utf-8",
        )
        errors: list[str] = []
        if validate_skill(path):
            errors.append("valid synthetic skill was rejected")
        path.write_text(
            "---\nname: wrong-name\ndescription: Validate a sample.\n"
            "---\nUse when: applicable.\nDo not use otherwise.\nStop: on error.\n",
            encoding="utf-8",
        )
        if not validate_skill(path):
            errors.append("invalid synthetic skill was accepted")
        return errors


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    errors = self_test() + validate_repository(root)
    if errors:
        for error in errors:
            print(f"FAIL: {error}", file=sys.stderr)
        return 1
    print("PASS: sample positive/negative cases and all registered repo-local skills")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
