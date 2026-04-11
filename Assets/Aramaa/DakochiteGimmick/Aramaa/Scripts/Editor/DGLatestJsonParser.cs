#if UNITY_EDITOR

using System;
using System.Globalization;
using UnityEngine;

namespace Aramaa.DakochiteGimmick.Editor
{
    internal static class DGLatestJsonParser
    {
        public static string ExtractLatestVersion(string latestJsonText)
        {
            if (string.IsNullOrWhiteSpace(latestJsonText))
            {
                return null;
            }

            LatestJsonRoot root;
            try
            {
                root = JsonUtility.FromJson<LatestJsonRoot>(latestJsonText);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (root == null || root.schemaVersion != GimmickConstants.LATEST_JSON_SCHEMA_VERSION
                || root.packages == null || root.packages.Length == 0)
            {
                return null;
            }

            Version latestStable = null;
            string latestStableText = null;
            Version latestPrereleaseCore = null;
            int latestPrereleaseNumber = -1;
            string latestPrereleaseText = null;

            for (var i = 0; i < root.packages.Length; i++)
            {
                var entry = root.packages[i];
                if (entry == null || !string.Equals(entry.id, GimmickConstants.TARGET_PACKAGE_ID, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!DGVersionComparer.TryParseStableVersion(entry.version, out var candidate))
                {
                    if (TryParseSupportedBetaVersion(entry.version, out var prereleaseCore, out var prereleaseNumber))
                    {
                        var isNewerPrerelease = false;
                        if (latestPrereleaseCore == null)
                        {
                            isNewerPrerelease = true;
                        }
                        else
                        {
                            var coreComparison = prereleaseCore.CompareTo(latestPrereleaseCore);
                            isNewerPrerelease = coreComparison > 0
                                || (coreComparison == 0 && prereleaseNumber > latestPrereleaseNumber);
                        }

                        if (isNewerPrerelease)
                        {
                            latestPrereleaseCore = prereleaseCore;
                            latestPrereleaseNumber = prereleaseNumber;
                            latestPrereleaseText = entry.version;
                        }
                    }

                    continue;
                }

                if (latestStable == null || candidate.CompareTo(latestStable) > 0)
                {
                    latestStable = candidate;
                    latestStableText = entry.version;
                }
            }

            return latestStableText ?? latestPrereleaseText;
        }

        private static bool TryParseSupportedBetaVersion(string versionText, out Version coreVersion, out int betaNumber)
        {
            coreVersion = null;
            betaNumber = -1;

            if (string.IsNullOrWhiteSpace(versionText))
            {
                return false;
            }

            var separator = "-beta.";
            var separatorIndex = versionText.IndexOf(separator, StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                return false;
            }

            var coreText = versionText.Substring(0, separatorIndex);
            var suffix = versionText.Substring(separatorIndex + separator.Length);
            if (!DGVersionComparer.TryParseStableVersion(coreText, out coreVersion))
            {
                return false;
            }

            if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out betaNumber) || betaNumber < 0)
            {
                return false;
            }

            return true;
        }

        [Serializable]
        private sealed class LatestJsonRoot
        {
            public int schemaVersion;
            public LatestPackageEntry[] packages;
        }

        [Serializable]
        private sealed class LatestPackageEntry
        {
            public string id;
            public string version;
        }
    }
}

#endif
