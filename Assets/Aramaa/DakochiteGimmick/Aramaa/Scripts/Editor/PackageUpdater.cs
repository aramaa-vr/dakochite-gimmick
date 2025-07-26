using System;
using System.Net;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// 指定されたURLから最新バージョンを取得し、コード上でハードコードされた
    /// パッケージのバージョンと比較して、更新状態を判断する最もシンプルなクラス。
    /// </summary>
    public static class PackageUpdater
    {
        // 最新バージョンを取得するJSONファイルのURL
        private const string LATEST_VERSION_JSON_URL = "https://aramaa-vr.github.io/dakochite-gimmick/Assets/Aramaa/DakochiteGimmick/package.json";

        // ローカルのインストール済みパッケージバージョン (コード上でハードコード)
        // ここに現在プロジェクトに導入されているギミックのバージョンを直接入力してください。
        public const string LOCAL_INSTALLED_VERSION = "1.0.3"; // ここに実際のバージョンを記述します

        /// <summary>
        /// JSONからバージョン情報をデシリアライズするためのクラス。
        /// JsonUtilityが機能するために[Serializable]属性が必要です。
        /// </summary>
        [Serializable]
        private class VersionInfo
        {
            public string version;
        }

        /// <summary>
        /// パッケージの更新状態を示す列挙型。
        /// </summary>
        public enum PackageUpdateState
        {
            Unknown,         // 不明またはチェック中
            UpToDate,        // 最新バージョン
            UpdateAvailable, // 更新あり
            Error            // エラー発生
        }

        /// <summary>
        /// パッケージの更新状態を非同期でチェックし、結果を返します。
        /// </summary>
        /// <returns>現在のパッケージ更新状態とメッセージのタプル。</returns>
        public static async Task<(PackageUpdateState state, string message)> CheckForUpdateAsync()
        {
            EditorUtility.DisplayProgressBar("パッケージ更新チェック", "更新情報を取得中...", 0.1f);

            try
            {
                // 1. 外部URLの最新バージョンを取得
                string latestVersionString = await GetLatestPackageVersionFromUrlAsync();
                if (string.IsNullOrEmpty(latestVersionString))
                {
                    EditorUtility.ClearProgressBar();
                    return (PackageUpdateState.Error, "最新のパッケージバージョンを取得できませんでした。");
                }
                Version latestVersion = new Version(latestVersionString); // バージョンオブジェクトに変換
                Debug.Log($"[PackageUpdater] リモート最新バージョン: {latestVersion}");

                // 2. ローカルプロジェクトのインストール済みバージョン (ハードコードされた値を使用)
                Version installedVersion = new Version(LOCAL_INSTALLED_VERSION);
                Debug.Log($"[PackageUpdater] ローカルインストール済みバージョン (ハードコード): {installedVersion}");

                EditorUtility.DisplayProgressBar("パッケージ更新チェック", "バージョンを比較中...", 0.8f);

                // 3. バージョンの比較
                int comparison = installedVersion.CompareTo(latestVersion);

                if (comparison < 0)
                {
                    EditorUtility.ClearProgressBar();
                    return (PackageUpdateState.UpdateAvailable, $"「Manage Project」から新しいバージョンが利用可能です！ ({installedVersion} -> {latestVersion}) 「Search Packages...」に「だこちてギミック」を入れる");
                }
                else if (comparison == 0)
                {
                    EditorUtility.ClearProgressBar();
                    return (PackageUpdateState.UpToDate, $"パッケージは最新です ({installedVersion})");
                }
                else
                {
                    // ローカルバージョンがリモートより新しい場合 (開発版など)
                    EditorUtility.ClearProgressBar();
                    return (PackageUpdateState.UpToDate, $"パッケージは最新です ({installedVersion}) (ローカルの方が新しい可能性があります)");
                }
            }
            catch (FormatException fe)
            {
                Debug.LogError($"[PackageUpdater] バージョン文字列の形式が不正です: {fe.Message}");
                EditorUtility.ClearProgressBar();
                return (PackageUpdateState.Error, $"バージョン形式エラー: {fe.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PackageUpdater] パッケージ更新チェック中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
                EditorUtility.ClearProgressBar();
                return (PackageUpdateState.Error, $"エラーが発生しました: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar(); // 確実にプログレスバーを閉じる
            }
        }

        /// <summary>
        /// 指定されたJSON URLから最新バージョン文字列を非同期で取得します。
        /// </summary>
        /// <returns>最新バージョンの文字列、または取得できなかった場合はnull。</returns>
        private static async Task<string> GetLatestPackageVersionFromUrlAsync()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    EditorUtility.DisplayProgressBar("パッケージ更新チェック", "リモートバージョン情報をダウンロード中...", 0.2f);
                    string jsonString = await client.DownloadStringTaskAsync(LATEST_VERSION_JSON_URL);

                    // JSON文字列をVersionInfoクラスにデシリアライズ
                    VersionInfo versionInfo = JsonUtility.FromJson<VersionInfo>(jsonString);

                    if (versionInfo != null && !string.IsNullOrEmpty(versionInfo.version))
                    {
                        return versionInfo.version.Trim(); // 前後の空白を削除
                    }
                    else
                    {
                        Debug.LogError($"[PackageUpdater] ダウンロードしたJSONからバージョン情報がパースできませんでした: {jsonString}");
                        return null;
                    }
                }
                catch (WebException webEx)
                {
                    Debug.LogError($"[PackageUpdater] バージョン情報ダウンロード中にネットワークエラー: {webEx.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PackageUpdater] バージョン情報取得中にエラー: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
