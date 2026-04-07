#!/usr/bin/env python3
"""公開・無料配布前のリリース妥当性チェック。"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import zipfile
from pathlib import Path

PACKAGE_JSON_RELATIVE_PATH = Path("Assets/Aramaa/DakochiteGimmick/package.json")
ZIP_NAME_PREFIX = "jp.aramaa.dakochite-gimmick"
EXPECTED_LICENSES_URL = "https://github.com/aramaa-vr/dakochite-gimmick/blob/master/LICENSE"

SECRET_PATTERN = re.compile(
    r"(AKIA[0-9A-Z]{16}|ghp_[0-9A-Za-z]{36}|xoxb-[0-9A-Za-z-]{20,}|AIza[0-9A-Za-z\-_]{35}|BEGIN (?:RSA|OPENSSH|EC) PRIVATE KEY)",
    re.IGNORECASE,
)
SEMVER_IN_ZIP_URL = re.compile(
    r"/releases/download/(?P<version>[^/]+)/"
    r"jp\.aramaa\.dakochite-gimmick-(?P=version)\.zip\??$"
)

REQUIRED_FILES = [
    "LICENSE",
    "third-party-notices.md",
    "CHANGELOG.md",
    "Assets/Aramaa/DakochiteGimmick/LICENSE.txt",
    "Assets/Aramaa/DakochiteGimmick/package.json",
]
TEXT_SCAN_EXCLUDE_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".webp", ".ico", ".pdf", ".meta", ".zip", ".svg", ".mp3"
}
SCAN_ALLOWLIST = {"Tools/validate_public_release.py"}
PURCHASE_REQUIRED_ASSET_EXTENSIONS = {
    ".fbx",
    ".blend",
    ".obj",
    ".dae",
    ".3ds",
    ".max",
    ".c4d",
}


def log_info(message: str) -> None:
    print(f"[INFO] {message}")


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


class CheckResult:
    def __init__(self) -> None:
        self.errors: list[str] = []
        self.warnings: list[str] = []

    def error(self, message: str) -> None:
        self.errors.append(message)

    def warn(self, message: str) -> None:
        self.warnings.append(message)

    def ok(self) -> bool:
        return not self.errors


def find_repo_root(start: Path) -> Path:
    for candidate in [start, *start.parents]:
        if (candidate / PACKAGE_JSON_RELATIVE_PATH).exists():
            return candidate
    raise FileNotFoundError("Repository root not found from script location")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")


def run_git(root: Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", *args],
        cwd=root,
        check=True,
        text=True,
        capture_output=True,
        encoding="utf-8",
        errors="replace",
    )
    return completed.stdout or ""


def check_required_files(root: Path, result: CheckResult) -> None:
    log_info("必須ファイルの存在を確認します")
    for rel in REQUIRED_FILES:
        path = root / rel
        if not path.exists():
            result.error(f"必須ファイルが見つかりません: {rel}")
            continue
        log_info(f"必須ファイル OK: {rel}")


def check_license_docs(root: Path, result: CheckResult) -> None:
    log_info("LICENSE と third-party-notices.md の内容を確認します")
    license_text = read_text(root / "LICENSE")
    if "VN3ライセンス" not in license_text:
        result.error("LICENSE に VN3ライセンスの記載がありません")

    third_party_text = read_text(root / "third-party-notices.md")
    if "同梱" not in third_party_text:
        result.warn("third-party-notices.md に同梱ポリシー記載が見当たりません")


def load_package_json(root: Path, result: CheckResult) -> dict[str, object]:
    package_path = root / PACKAGE_JSON_RELATIVE_PATH
    log_info(f"package.json を読み込みます: {PACKAGE_JSON_RELATIVE_PATH.as_posix()}")
    try:
        package = json.loads(read_text(package_path))
    except json.JSONDecodeError as exc:
        result.error(f"package.json の解析に失敗しました: {exc}")
        return {}

    if not isinstance(package, dict):
        result.error("package.json のトップレベルが object ではありません")
        return {}
    return package


def check_package_consistency(root: Path, package: dict[str, object], result: CheckResult) -> None:
    log_info("package.json の version / url / license / licensesUrl / name 整合性を確認します")

    version = package.get("version")
    url = package.get("url")
    licenses_url = package.get("licensesUrl")
    license_name = package.get("license")
    package_name = package.get("name")

    if not isinstance(version, str) or not version:
        result.error("package.json の version が不正です")
    if not isinstance(url, str) or not url:
        result.error("package.json の url が不正です")
    elif isinstance(version, str):
        match = SEMVER_IN_ZIP_URL.search(url)
        if not match:
            result.error("package.json の url 形式が想定と一致しません")
        elif match.group("version") != version:
            result.error("package.json の version と url 内バージョンが一致しません")

    if package_name != ZIP_NAME_PREFIX:
        result.warn(f"package.json の name が想定値と異なります: {package_name}")
    if license_name != "Custom":
        result.warn(f"package.json の license が Custom ではありません: {license_name}")
    if licenses_url != EXPECTED_LICENSES_URL:
        result.warn("package.json の licensesUrl が想定値と異なります")

    constants_path = root / "Assets/Aramaa/DakochiteGimmick/Aramaa/Scripts/Editor/GimickConstants.cs"
    if constants_path.is_file() and isinstance(version, str) and version:
        constants_text = read_text(constants_path)
        match = re.search(r'CURRENT_VERSION\s*=\s*"([^"]+)"', constants_text)
        if not match:
            result.warn("GimickConstants.cs の CURRENT_VERSION を取得できません")
        elif match.group(1) != version:
            result.error("package.json の version と GimickConstants.CURRENT_VERSION が一致しません")


def check_changelog(root: Path, package: dict[str, object], result: CheckResult) -> None:
    version = package.get("version")
    if not isinstance(version, str) or not version:
        result.warn("CHANGELOG チェックをスキップしました: package version が不正です")
        return

    changelog = read_text(root / "CHANGELOG.md")
    version_heading_pattern = re.compile(
        rf"^## Version {re.escape(version)}(?:\s*$|\s+\(.*\)\s*$)",
        flags=re.MULTILINE,
    )
    if not version_heading_pattern.search(changelog):
        result.error(f"CHANGELOG.md に version {version} の見出しがありません")


def is_purchase_required_asset(path_text: str) -> bool:
    lowered = path_text.lower()
    return any(lowered.endswith(ext) for ext in PURCHASE_REQUIRED_ASSET_EXTENSIONS)


def check_purchase_required_assets_in_git(root: Path, result: CheckResult) -> None:
    log_info("git 管理データに購入必須アセット本体が含まれていないか確認します")
    tracked = run_git(root, "ls-files").splitlines()
    findings = [rel for rel in tracked if is_purchase_required_asset(rel)]
    if findings:
        result.error(
            "git 管理データに購入必須アセット本体の疑いがあるファイルを検出しました: "
            + ", ".join(findings[:20])
        )


def should_scan_file(path: Path) -> bool:
    if path.suffix.lower() in TEXT_SCAN_EXCLUDE_SUFFIXES:
        return False
    return True


def check_secrets(root: Path, result: CheckResult) -> None:
    log_info("git 管理下ファイルに機密情報パターンがないか確認します")
    tracked = run_git(root, "ls-files").splitlines()

    findings: list[str] = []
    for rel in tracked:
        if rel in SCAN_ALLOWLIST:
            continue
        path = root / rel
        if not path.exists() or not path.is_file() or not should_scan_file(path):
            continue
        try:
            content = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue

        for idx, line in enumerate(content.splitlines(), start=1):
            if SECRET_PATTERN.search(line):
                findings.append(f"{rel}:{idx}")

    if findings:
        result.error("機密情報の疑いがある文字列を検出しました: " + ", ".join(findings[:20]))


def check_build_zip_contents(root: Path, package: dict[str, object], result: CheckResult) -> None:
    version = package.get("version")
    if not isinstance(version, str) or not version:
        result.warn("Build ZIP の内容確認をスキップしました: package version が不正です")
        return

    zip_rel = Path(f"Build/{ZIP_NAME_PREFIX}-{version}.zip")
    zip_path = root / zip_rel
    if not zip_path.exists():
        result.warn(f"Build ZIP が見つかりません: {zip_rel.as_posix()}")
        return

    try:
        with zipfile.ZipFile(zip_path) as zip_file:
            names = zip_file.namelist()
    except zipfile.BadZipFile:
        result.error(f"Build ZIP の読み込みに失敗しました: {zip_rel.as_posix()}")
        return

    findings = [
        name for name in names if name and not name.endswith("/") and is_purchase_required_asset(name)
    ]
    if findings:
        result.error(
            "Build ZIP に購入必須アセット本体の疑いがあるファイルを検出しました: "
            + ", ".join(findings[:20])
        )


def check_git_clean(root: Path, result: CheckResult) -> None:
    status = run_git(root, "status", "--short").strip()
    if status:
        result.warn("作業ツリーに未コミット差分があります")


def main() -> int:
    configure_console_encoding()
    root = find_repo_root(Path(__file__).resolve())
    result = CheckResult()

    check_required_files(root, result)
    if result.errors:
        for error in result.errors:
            print(f"[ERROR] {error}")
        return 1

    check_license_docs(root, result)
    package = load_package_json(root, result)
    check_package_consistency(root, package, result)
    check_changelog(root, package, result)
    check_purchase_required_assets_in_git(root, result)
    check_secrets(root, result)
    check_build_zip_contents(root, package, result)
    check_git_clean(root, result)

    for warning in result.warnings:
        print(f"[WARN] {warning}")
    for error in result.errors:
        print(f"[ERROR] {error}")

    if result.ok():
        print("[OK] 公開前チェックに合格しました")
        return 0
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
