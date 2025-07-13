#!/bin/bash

set -euo pipefail

readonly SEVEN_ZIP_PATH="/c/Program Files/7-Zip/7z.exe"
readonly DEFAULT_VERSION="1.0.0"
readonly SOURCE_DIR="./Assets/Aramaa/DakochiteGimmick"
readonly TEMP_DIR_PREFIX="VPM_TEMP_"
readonly PACKAGE_BASE_NAME="jp.aramaa.dakochite-gimmick"

VPM_TEMP_DIR=""

cleanup() {
    if [[ -n "${VPM_TEMP_DIR}" && -d "${VPM_TEMP_DIR}" ]]; then
        rm -rf "${VPM_TEMP_DIR}"
    fi
    if [[ "$?" -ne 0 && "$_CLEANUP_CALLED" != "true" ]]; then
        echo "エラー発生。終了。" >&2
    fi
    _CLEANUP_CALLED="true"
}

trap cleanup EXIT ERR SIGINT SIGTERM

show_help() {
    echo "使用法: $(basename "$0") [バージョン]"
    echo "  例: ./Tools/create_vpm_zip.sh ${DEFAULT_VERSION}"
    echo "  例: ./Tools/create_vpm_zip.sh"
    exit 0
}

validate_7zip_path() {
    if [[ ! -f "${SEVEN_ZIP_PATH}" ]]; then
        echo "エラー: 7-Zip見つからず: ${SEVEN_ZIP_PATH}" >&2
        echo "7-Zipパス確認・修正。" >&2
        exit 1
    fi
}

main() {
    validate_7zip_path

    local version="${1:-${DEFAULT_VERSION}}"
    local zip_file_name="${PACKAGE_BASE_NAME}-${version}.zip"

    VPM_TEMP_DIR=$(mktemp -d -t "${TEMP_DIR_PREFIX}XXXXXXXX")
    
    if [[ -f "${zip_file_name}" ]]; then
        rm -f "${zip_file_name}" || { echo "エラー: 既存ZIP削除失敗。" >&2; exit 1; }
    fi

    cp -r "${SOURCE_DIR}/." "${VPM_TEMP_DIR}/" || { echo "エラー: ファイルコピー失敗。" >&2; exit 1; }

    (cd "${VPM_TEMP_DIR}" && "${SEVEN_ZIP_PATH}" a -tzip "../${zip_file_name}" ./*) || { echo "エラー: ZIP作成失敗。" >&2; exit 1; }
    echo "${zip_file_name} 作成完了。"
}

main "$@"
