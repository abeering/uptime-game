#!/usr/bin/env python3
"""Build the five-source Uptime snapshot for ChatGPT Project Sources."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
from urllib.parse import quote, unquote, urlsplit


SOURCES = (
    "Docs/README.md",
    "Docs/Uptime_Current_State.md",
    "Docs/Design/Uptime_Design.md",
    "Docs/Packet/Current_State.md",
    "Docs/Connections/Current_State.md",
)
OUTPUT = Path("Docs/_generated/Uptime_Project_Core.md")
GITHUB_BLOB_ROOT = "https://github.com/abeering/uptime-game/blob/main/"
INLINE_LINK = re.compile(
    r'(?P<before>\]\()(?P<target><[^>]+>|[^\s)]+)(?P<after>(?:\s+"[^"]*")?\))'
)
REFERENCE_LINK = re.compile(
    r'(?m)^(?P<before>[ \t]{0,3}\[[^\]\n]+\]:[ \t]*)(?P<target><[^>]+>|[^\s]+)'
)


def normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def source_url(path: str) -> str:
    return GITHUB_BLOB_ROOT + quote(path, safe="/")


def rewrite_target(target: str, source: str, root: Path) -> str:
    wrapped = target.startswith("<") and target.endswith(">")
    value = target[1:-1] if wrapped else target
    parsed = urlsplit(value)
    if parsed.scheme or parsed.netloc or not parsed.path or parsed.path.startswith("/"):
        return target

    destination = (root / Path(source).parent / unquote(parsed.path)).resolve()
    try:
        relative = destination.relative_to(root)
    except ValueError as exc:
        raise ValueError(f"{source}: link leaves repository: {target}") from exc

    url = source_url(relative.as_posix())
    if parsed.query:
        url += "?" + parsed.query
    if parsed.fragment:
        url += "#" + parsed.fragment
    return url


def portable_links(text: str, source: str, root: Path) -> str:
    def replace(match: re.Match[str]) -> str:
        return (
            match.group("before")
            + rewrite_target(match.group("target"), source, root)
            + match.groupdict().get("after", "")
        )

    return REFERENCE_LINK.sub(replace, INLINE_LINK.sub(replace, text))


def load_sources(root: Path) -> list[tuple[str, str]]:
    missing = [name for name in SOURCES if not (root / name).is_file()]
    if missing:
        raise ValueError(
            "missing Core source file(s): "
            + ", ".join(missing)
            + "; update the manifest only if a source was intentionally renamed"
        )
    return [
        (name, normalize_newlines((root / name).read_text(encoding="utf-8")))
        for name in SOURCES
    ]


def render(root: Path) -> tuple[bytes, str]:
    sources = load_sources(root)
    # JSON framing makes path, order, and content boundaries unambiguous.
    digest_input = json.dumps(
        sources, ensure_ascii=False, separators=(",", ":")
    ).encode("utf-8")
    digest = hashlib.sha256(digest_input).hexdigest()

    source_list = "\n".join(
        f"{index}. [`{name}`]({source_url(name)})"
        for index, name in enumerate(SOURCES, start=1)
    )
    parts = [
        f"""# Uptime Project Core

> **GENERATED FILE — DO NOT EDIT DIRECTLY.** Run `python scripts/build_uptime_project_core.py` after changing an included source.
>
> **Source digest:** `sha256:{digest}`. This hashes the ordered manifest paths and their LF-normalized UTF-8 contents. It does not depend on the clock or Git HEAD.
>
> **Snapshot:** This file is a cold-start Project Source, not a live GitHub feed. Repository links below target current `main`, which may be newer than this snapshot.

## How to use this Core

- Uptime is **one game** explored through two gameplay prototypes. Packet is parked but retained; Connections is the active experiment. Neither is declared final. Shared Uptime design includes systems neither prototype has implemented yet.
- Shared design describes the game being made; the Packet and Connections current-state pages summarize their implementations. Runtime code and serialized Unity scenes decide executable behavior. Working hypotheses are not installed features, and prototype-specific mechanics do not transfer automatically.
- This Core contains five entry points, not the full documentation. `Docs/Design/` retains shared design; `Docs/Packet/` and `Docs/Connections/` retain detailed mechanics; `Docs/research/` and `Docs/historical/` preserve unresolved alternatives and prior models. PR discussions and `Docs/Review/` preserve provenance. Follow the linked sources when a topic needs detail.
- For current implementation or recent PRs, inspect the working repository or GitHub rather than relying on this snapshot. Maintain individual sources directly and regenerate this file; never hand-edit generated content. After merging a change to an included source, refresh the uploaded ChatGPT Project Source separately.

## Included source files

{source_list}

---

"""
    ]
    for index, (name, contents) in enumerate(sources, start=1):
        parts.append(f"<!-- BEGIN SOURCE: {name} -->\n")
        parts.append(f"## Source {index}: `{name}`\n\n")
        parts.append(f"Canonical file: [`{name}`]({source_url(name)}).\n\n")
        portable = portable_links(contents, name, root)
        parts.append(portable)
        if not portable.endswith("\n"):
            parts.append("\n")
        parts.append(f"\n<!-- END SOURCE: {name} -->\n\n")
    return ("".join(parts).rstrip("\n") + "\n").encode("utf-8"), digest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo-root",
        type=Path,
        help="repository root; defaults to the parent of this script's directory",
    )
    action = parser.add_mutually_exclusive_group()
    action.add_argument("--check", action="store_true", help="check freshness without writes")
    action.add_argument("--list", action="store_true", help="print the ordered source manifest")
    args = parser.parse_args()

    if args.list:
        print("\n".join(SOURCES))
        return 0

    root = (args.repo_root or Path(__file__).resolve().parent.parent).resolve()
    if not (root / "Docs").is_dir():
        print(f"error: repository root has no Docs/ directory: {root}", file=sys.stderr)
        return 1
    try:
        expected, digest = render(root)
        output = root / OUTPUT
        if args.check:
            if not output.is_file():
                raise ValueError(f"missing generated Core: {output}")
            if output.read_bytes() != expected:
                raise ValueError(
                    f"stale or modified generated Core: {output}; "
                    f"expected source digest sha256:{digest}; regenerate it"
                )
            print(f"Current: {output} (sha256:{digest})")
            return 0

        output.parent.mkdir(parents=True, exist_ok=True)
        if output.is_file() and output.read_bytes() == expected:
            print(f"Unchanged: {output} (sha256:{digest})")
        else:
            output.write_bytes(expected)
            print(f"Wrote: {output} ({len(expected)} bytes; sha256:{digest})")
        return 0
    except (OSError, UnicodeError, ValueError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
