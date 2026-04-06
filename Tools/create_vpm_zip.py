#!/usr/bin/env python3

"""VPM ZIP作成スクリプト。"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

SEMVER_PATTERN = re.compile(
    r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)"
    r"(?:-((?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)(?:\."
    r"(?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*))*))?"
    r"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$"
)
ZIP_NAME_PREFIX = "jp.aramaa.dakochite-gimmick"
PACKAGE_JSON_RELATIVE_PATH = Path("Assets/Aramaa/DakochiteGimmick/package.json")


def find_repo_root(start: Path) -> Path:
    for candidate in [start, *start.parents]:
        if (candidate / PACKAGE_JSON_RELATIVE_PATH).is_file():
            return candidate
    raise FileNotFoundError("リポジトリルートを特定できませんでした。")


ROOT_DIR = find_repo_root(Path(__file__).resolve().parent)
SOURCE_DIR = ROOT_DIR / "Assets/Aramaa/DakochiteGimmick"
PACKAGE_JSON = ROOT_DIR / "Assets/Aramaa/DakochiteGimmick/package.json"
BUILD_DIR = ROOT_DIR / "Build"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="VPM ZIPを作成します。")
    parser.add_argument("version", nargs="?", help="バージョン (例: 1.1.2)")
    return parser.parse_args()


def remove_if_exists(path: Path) -> None:
    if path.is_file():
        print(f"削除: {path.as_posix()}")
        path.unlink()
    elif path.is_dir():
        print(f"削除: {path.as_posix()}")
        shutil.rmtree(path)


def read_package_json(package_json_path: Path) -> dict[str, object]:
    if not package_json_path.is_file():
        raise FileNotFoundError(f"package.jsonが見つかりません: {package_json_path.as_posix()}")

    try:
        package = json.loads(package_json_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"package.jsonの解析に失敗しました: {package_json_path.as_posix()}") from exc

    if not isinstance(package, dict):
        raise ValueError(f"package.jsonの形式が不正です: {package_json_path.as_posix()}")

    return package


def read_package_metadata(package_json_path: Path) -> tuple[str, str]:
    package = read_package_json(package_json_path)
    package_name = package.get("name")
    if not isinstance(package_name, str) or not package_name:
        raise ValueError(f"package.jsonからnameを取得できません: {package_json_path.as_posix()}")

    version = package.get("version")
    if not isinstance(version, str) or not version:
        raise ValueError(f"package.jsonからversionを取得できません: {package_json_path.as_posix()}")

    return package_name, version


def validate_version(version: str) -> None:
    if not SEMVER_PATTERN.fullmatch(version):
        raise ValueError(
            f"バージョン形式が不正です: {version} (例: 1.1.2 / 1.1.2-beta.1)"
        )


def copy_source_tree(source_dir: Path, temp_dir: Path) -> None:
    if not source_dir.is_dir():
        raise FileNotFoundError(f"ソースディレクトリが見つかりません: {source_dir.as_posix()}")

    for child in source_dir.iterdir():
        dst = temp_dir / child.name
        if child.is_dir():
            shutil.copytree(child, dst, symlinks=True)
        else:
            shutil.copy2(child, dst, follow_symlinks=True)


def create_zip_from_temp(temp_dir: Path, zip_file_path: Path) -> None:
    with zipfile.ZipFile(zip_file_path, "w", compression=zipfile.ZIP_DEFLATED) as zip_file:
        for root, dirs, files in os.walk(temp_dir):
            dirs.sort()
            files.sort()
            root_path = Path(root)
            relative_root = root_path.relative_to(temp_dir)
            if not files and not dirs and relative_root != Path("."):
                zip_file.writestr(relative_root.as_posix().rstrip("/") + "/", "")
                continue
            for file_name in files:
                file_path = root_path / file_name
                arcname = (relative_root / file_name).as_posix()
                zip_file.write(file_path, arcname)


def main() -> int:
    args = parse_args()

    try:
        package_name, package_version = read_package_metadata(PACKAGE_JSON)
        if package_name != ZIP_NAME_PREFIX:
            print(
                f"[WARN] package.jsonのnameが想定と異なります: {package_name} "
                f"(期待値: {ZIP_NAME_PREFIX})",
                file=sys.stderr,
            )
        version = args.version or package_version
        validate_version(version)
        zip_file_name = f"{ZIP_NAME_PREFIX}-{version}.zip"
        zip_file_path = BUILD_DIR / zip_file_name

        remove_if_exists(zip_file_path)
        BUILD_DIR.mkdir(parents=True, exist_ok=True)
        staging_root = Path(tempfile.mkdtemp(prefix="vpm-zip-staging-", dir=ROOT_DIR))
        try:
            copy_source_tree(SOURCE_DIR, staging_root)
            create_zip_from_temp(staging_root, zip_file_path)
        finally:
            if staging_root.exists():
                shutil.rmtree(staging_root)
    except (FileNotFoundError, ValueError, OSError) as exc:
        print(str(exc), file=sys.stderr)
        return 1

    print(f"ZIP作成完了: {zip_file_path.as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
