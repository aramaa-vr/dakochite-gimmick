#!/bin/bash

# === VPM ZIP作成スクリプト (Git Bash用) ===

set -euo pipefail

# --- 定数 ---
readonly DEFAULT_VERSION="1.0.0"
readonly ZIP_NAME_PREFIX="jp.aramaa.dakochite-gimmick"
readonly SOURCE_DIR="./Assets/Aramaa/DakochiteGimmick"
readonly TEMP_DIR="./VPM_TEMP"
readonly BUILD_DIR="./Build"
readonly SEVEN_ZIP_PATH="/c/Program Files/7-Zip/7z.exe"

# --- バージョン取得 ---
VERSION="${1:-$DEFAULT_VERSION}"
readonly ZIP_FILE_NAME="${ZIP_NAME_PREFIX}-${VERSION}.zip"
readonly ZIP_FILE_PATH="${BUILD_DIR}/${ZIP_FILE_NAME}"

# --- help表示 ---
if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
  echo "Usage: ./Tools/create_vpm_zip.sh [version]"
  exit 0
fi

# --- 不要ファイル削除 ---
[ -f "$ZIP_FILE_PATH" ] && echo "削除: $ZIP_FILE_PATH" && rm -f "$ZIP_FILE_PATH"
[ -d "$TEMP_DIR" ] && echo "削除: $TEMP_DIR" && rm -rf "$TEMP_DIR"

# --- ディレクトリ準備 ---
mkdir -p "$BUILD_DIR"
mkdir -p "$TEMP_DIR"

# --- ギミック中身コピー ---
cp -r "$SOURCE_DIR/." "$TEMP_DIR/"

# --- zip作成 ---
cd "$TEMP_DIR" || { echo "cd失敗: $TEMP_DIR" >&2; exit 1; }

"$SEVEN_ZIP_PATH" a -tzip "../$ZIP_FILE_PATH" * || {
  echo "7z圧縮失敗: $SEVEN_ZIP_PATH" >&2
  exit 1
}

cd - > /dev/null

# --- TEMP削除 ---
rm -rf "$TEMP_DIR"

# --- 完了表示 ---
echo "ZIP作成完了: $ZIP_FILE_PATH"
