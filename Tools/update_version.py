#!/usr/bin/env python3
"""だこちてギミックのバージョン更新ヘルパー。"""

from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

SEMVER_PATTERN = re.compile(
    r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)"
    r"(?:-((?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)(?:\."
    r"(?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*))*))?"
    r"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$"
)
PACKAGE_JSON_RELATIVE_PATH = Path("Assets/Aramaa/DakochiteGimmick/package.json")


def find_repo_root(start: Path) -> Path:
    for candidate in [start, *start.parents]:
        if (candidate / PACKAGE_JSON_RELATIVE_PATH).is_file():
            return candidate
    raise FileNotFoundError("スクリプト配置場所からリポジトリルートを特定できませんでした。")


ROOT = find_repo_root(Path(__file__).resolve().parent)
PACKAGE_JSON = ROOT / "Assets/Aramaa/DakochiteGimmick/package.json"
PACKAGE_UPDATER_CS = ROOT / "Assets/Aramaa/DakochiteGimmick/Aramaa/Scripts/Editor/PackageUpdater.cs"
HOLD_MENU_ASSET = ROOT / "Assets/Aramaa/DakochiteGimmick/Aramaa/Menus/HoldGimickMenuMain.asset"


def ensure_file_exists(path: Path) -> None:
    if not path.is_file():
        raise FileNotFoundError(f"Required file not found: {path.as_posix()}")


def read_text(path: Path) -> str:
    content = path.read_bytes().decode("utf-8")
    return content.replace("\r\n", "\n").replace("\r", "\n")


def write_text(path: Path, content: str) -> None:
    normalized = content.replace("\r\n", "\n").replace("\r", "\n")
    path.write_bytes(normalized.encode("utf-8"))


def parse_version(version: str) -> str:
    if not SEMVER_PATTERN.fullmatch(version):
        raise argparse.ArgumentTypeError(
            "Invalid version format. Use SemVer style values like '1.1.2' or '1.1.3-beta.1'."
        )
    return version


def update_package_updater(version: str, dry_run: bool) -> None:
    ensure_file_exists(PACKAGE_UPDATER_CS)
    content = read_text(PACKAGE_UPDATER_CS)
    new_content, count = re.subn(
        r'(public const string LOCAL_INSTALLED_VERSION = ")([^"]+)(";)',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if count != 1:
        raise ValueError(
            f"LOCAL_INSTALLED_VERSION not updated (matches: {count}) in {PACKAGE_UPDATER_CS.as_posix()}"
        )
    if not dry_run:
        write_text(PACKAGE_UPDATER_CS, new_content)


def update_package_json(version: str, dry_run: bool) -> None:
    ensure_file_exists(PACKAGE_JSON)
    content = read_text(PACKAGE_JSON)
    version_content, version_count = re.subn(
        r'("version"\s*:\s*")([^"]+)(")',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if version_count != 1:
        raise ValueError(
            f"package.json version not updated (matches: {version_count}) in {PACKAGE_JSON.as_posix()}"
        )

    url = (
        "https://github.com/aramaa-vr/dakochite-gimmick/releases/download/"
        f"{version}/jp.aramaa.dakochite-gimmick-{version}.zip?"
    )
    new_content, url_count = re.subn(
        r'("url"\s*:\s*")([^"]+)(")',
        rf"\g<1>{url}\g<3>",
        version_content,
    )
    if url_count != 1:
        raise ValueError(
            f"package.json url not updated (matches: {url_count}) in {PACKAGE_JSON.as_posix()}"
        )

    if not dry_run:
        write_text(PACKAGE_JSON, new_content)


def update_hold_menu_asset(version: str, dry_run: bool) -> None:
    ensure_file_exists(HOLD_MENU_ASSET)
    content = read_text(HOLD_MENU_ASSET)
    new_content, count = replace_hold_menu_version(content, version)
    if count != 1:
        raise ValueError(
            f"Hold menu version not updated (matches: {count}) in {HOLD_MENU_ASSET.as_posix()}"
        )
    if not dry_run:
        write_text(HOLD_MENU_ASSET, new_content)


def replace_hold_menu_version(content: str, version: str) -> tuple[str, int]:
    return re.subn(
        r'(^\s*-\sname:\s*")([^"]*?ver\s+)([^"\n]+)(")',
        rf"\g<1>\g<2>{version}\g<4>",
        content,
        flags=re.MULTILINE,
        count=1,
    )


def configure_console_encoding() -> None:
    if os.name != "nt":
        return

    for stream_name in ("stdout", "stderr"):
        stream = getattr(sys, stream_name, None)
        if stream is None:
            continue
        reconfigure = getattr(stream, "reconfigure", None)
        if callable(reconfigure):
            reconfigure(encoding="utf-8", errors="replace")


def main() -> int:
    configure_console_encoding()
    parser = argparse.ArgumentParser(description="Update DakochiteGimmick version references.")
    parser.add_argument(
        "version",
        type=parse_version,
        help="New version string (e.g. 1.1.3, 1.1.3-beta.1)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate and report changes without writing files.",
    )
    args = parser.parse_args()

    try:
        # 先に全ファイルを検証して、途中失敗による部分更新を防ぐ。
        update_hold_menu_asset(args.version, dry_run=True)
        update_package_updater(args.version, dry_run=True)
        update_package_json(args.version, dry_run=True)

        if not args.dry_run:
            update_hold_menu_asset(args.version, dry_run=False)
            update_package_updater(args.version, dry_run=False)
            update_package_json(args.version, dry_run=False)
    except (FileNotFoundError, ValueError) as exc:
        print(str(exc), file=sys.stderr)
        return 1

    if args.dry_run:
        print(f"[dry-run] Version update validated for {args.version}")
    else:
        print(f"Version updated to {args.version}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
