#if UNITY_EDITOR

using System;

namespace Aramaa.DakochiteGimmick.Editor
{
    internal static class DGVersionComparer
    {
        public static DGVersionStatus GetVersionStatus(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(latestVersion))
            {
                return DGVersionStatus.Unknown;
            }

            if (!TryParseVersion(latestVersion, out var latest))
            {
                return DGVersionStatus.Unknown;
            }

            if (!TryParseVersion(currentVersion, out var current))
            {
                return DGVersionStatus.UpdateAvailable;
            }

            return CompareVersions(current, latest);
        }

        public static bool TryParseStableVersion(string versionText, out Version stableVersion)
        {
            stableVersion = null;
            if (!TryParseVersion(versionText, out var parts))
            {
                return false;
            }

            if (parts.IsPreRelease)
            {
                return false;
            }

            stableVersion = parts.CoreVersion;
            return true;
        }

        private static bool TryParseVersion(string version, out VersionParts parsed)
        {
            parsed = default;

            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            var trimmed = version.Trim();

            var plusIndex = trimmed.IndexOf('+');
            if (plusIndex >= 0)
            {
                return false;
            }

            var withoutBuild = trimmed;

            string corePart;
            string preReleasePart = null;
            var hyphenIndex = withoutBuild.IndexOf('-');
            if (hyphenIndex >= 0)
            {
                corePart = withoutBuild.Substring(0, hyphenIndex);
                if (hyphenIndex + 1 >= withoutBuild.Length)
                {
                    return false;
                }

                preReleasePart = withoutBuild.Substring(hyphenIndex + 1);
            }
            else
            {
                corePart = withoutBuild;
            }

            if (!TryParseCoreVersion(corePart, out var coreVersion))
            {
                return false;
            }

            if (!TryParseIdentifiers(preReleasePart, out var isPreRelease, out var preReleaseNumber))
            {
                return false;
            }

            parsed = new VersionParts(coreVersion, isPreRelease, preReleaseNumber);
            return true;
        }

        private static bool TryParseCoreVersion(string corePart, out Version normalizedVersion)
        {
            normalizedVersion = null;
            if (string.IsNullOrWhiteSpace(corePart))
            {
                return false;
            }

            var segments = corePart.Split('.');
            if (segments.Length != 3)
            {
                return false;
            }

            if (!Version.TryParse(corePart, out var parsedCore))
            {
                return false;
            }

            var build = parsedCore.Build >= 0 ? parsedCore.Build : 0;
            var revision = parsedCore.Revision >= 0 ? parsedCore.Revision : 0;
            normalizedVersion = new Version(parsedCore.Major, parsedCore.Minor, build, revision);
            return true;
        }

        private static bool TryParseIdentifiers(string identifiers, out bool hasIdentifiers, out string preReleaseNumber)
        {
            hasIdentifiers = false;
            preReleaseNumber = null;
            if (string.IsNullOrEmpty(identifiers))
            {
                return true;
            }

            var raw = identifiers.Split('.');
            if (raw.Length != 2 || raw[0] != "beta")
            {
                return false;
            }

            if (!IsValidNumericIdentifier(raw[1]))
            {
                return false;
            }

            hasIdentifiers = true;
            preReleaseNumber = raw[1];

            return true;
        }

        private static DGVersionStatus CompareVersions(VersionParts current, VersionParts latest)
        {
            var coreComparison = current.CoreVersion.CompareTo(latest.CoreVersion);
            if (coreComparison != 0)
            {
                return coreComparison < 0 ? DGVersionStatus.UpdateAvailable : DGVersionStatus.Ahead;
            }

            if (current.IsPreRelease != latest.IsPreRelease)
            {
                return current.IsPreRelease ? DGVersionStatus.UpdateAvailable : DGVersionStatus.Ahead;
            }

            if (!current.IsPreRelease)
            {
                return DGVersionStatus.UpToDate;
            }

            var preReleaseComparison = CompareNumericIdentifiers(current.PreReleaseNumber, latest.PreReleaseNumber);
            if (preReleaseComparison == 0)
            {
                return DGVersionStatus.UpToDate;
            }

            return preReleaseComparison < 0
                ? DGVersionStatus.UpdateAvailable
                : DGVersionStatus.Ahead;
        }

        private static int CompareNumericIdentifiers(string left, string right)
        {
            if (!IsValidNumericIdentifier(left) || !IsValidNumericIdentifier(right))
            {
                return string.CompareOrdinal(left ?? string.Empty, right ?? string.Empty);
            }

            if (left.Length != right.Length)
            {
                return left.Length.CompareTo(right.Length);
            }

            return string.CompareOrdinal(left, right);
        }

        private static bool IsValidNumericIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                {
                    return false;
                }
            }

            return value.Length == 1 || value[0] != '0';
        }

        private readonly struct VersionParts
        {
            public VersionParts(Version coreVersion, bool isPreRelease, string preReleaseNumber)
            {
                CoreVersion = coreVersion;
                IsPreRelease = isPreRelease;
                PreReleaseNumber = preReleaseNumber;
            }

            public Version CoreVersion { get; }
            public bool IsPreRelease { get; }
            public string PreReleaseNumber { get; }
        }
    }
}

#endif
