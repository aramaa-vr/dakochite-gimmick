#if UNITY_EDITOR

using System;
using UnityEngine.Networking;

namespace Aramaa.DakochiteGimmick.Editor
{
    internal enum DGVersionStatus
    {
        Unknown,
        UpToDate,
        UpdateAvailable,
        Ahead,
    }

    internal static class DGVersionUtility
    {
        public const string LOCAL_INSTALLED_VERSION = GimmickConstants.CURRENT_VERSION;

        public static void CheckForUpdateAsync(Action<DGVersionStatus, string> onComplete)
        {
            FetchLatestVersionAsync(GimmickConstants.LATEST_VERSION_URL, result =>
            {
                if (!result.Succeeded)
                {
                    onComplete?.Invoke(DGVersionStatus.Unknown, result.Error);
                    return;
                }

                var status = GetVersionStatus(LOCAL_INSTALLED_VERSION, result.LatestVersion);
                onComplete?.Invoke(status, BuildStatusMessage(status, LOCAL_INSTALLED_VERSION, result.LatestVersion));
            });
        }

        public static void FetchLatestVersionAsync(string url, Action<DGVersionFetchResult> onComplete)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                onComplete?.Invoke(DGVersionFetchResult.Failure(GimmickConstants.VERSION_ERROR_MISSING_URL));
                return;
            }

            if (!IsSupportedHttpUrl(url))
            {
                onComplete?.Invoke(DGVersionFetchResult.Failure(GimmickConstants.VERSION_ERROR_INVALID_URL));
                return;
            }

            var requestUrl = AppendCacheBuster(url);
            var request = UnityWebRequest.Get(requestUrl);
            request.timeout = 10;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onComplete?.Invoke(DGVersionFetchResult.Failure(request.error));
                        return;
                    }

                    var latestVersion = DGLatestJsonParser.ExtractLatestVersion(request.downloadHandler.text);
                    if (string.IsNullOrWhiteSpace(latestVersion))
                    {
                        onComplete?.Invoke(DGVersionFetchResult.Failure(GimmickConstants.VERSION_EXTRACT_FAILED));
                        return;
                    }

                    onComplete?.Invoke(DGVersionFetchResult.Success(latestVersion));
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static DGVersionStatus GetVersionStatus(string currentVersion, string latestVersion)
        {
            return DGVersionComparer.GetVersionStatus(currentVersion, latestVersion);
        }

        private static string BuildStatusMessage(DGVersionStatus status, string currentVersion, string latestVersion)
        {
            switch (status)
            {
                case DGVersionStatus.UpdateAvailable:
                    return $"「Manage Project」から新しいバージョンが利用可能です！ ({currentVersion} -> {latestVersion}) 「Search Packages...」に「だこちてギミック」を入れる";
                case DGVersionStatus.Ahead:
                    return $"パッケージは最新です ({currentVersion}) (ローカルの方が新しい可能性があります)";
                case DGVersionStatus.UpToDate:
                    return $"パッケージは最新です ({currentVersion})";
                case DGVersionStatus.Unknown:
                default:
                    return "バージョン比較に失敗しました。";
            }
        }

        private static string AppendCacheBuster(string url)
        {
            var separator = url.Contains("?") ? "&" : "?";
            var cacheBuster = DateTime.UtcNow.Ticks;
            return $"{url}{separator}t={cacheBuster}";
        }

        private static bool IsSupportedHttpUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

    }

    internal readonly struct DGVersionFetchResult
    {
        private DGVersionFetchResult(string latestVersion, string error)
        {
            LatestVersion = latestVersion;
            Error = error;
        }

        public string LatestVersion { get; }
        public string Error { get; }
        public bool Succeeded => string.IsNullOrWhiteSpace(Error);

        public static DGVersionFetchResult Success(string latestVersion)
        {
            return new DGVersionFetchResult(latestVersion, null);
        }

        public static DGVersionFetchResult Failure(string error)
        {
            return new DGVersionFetchResult(null, error);
        }
    }
}

#endif
