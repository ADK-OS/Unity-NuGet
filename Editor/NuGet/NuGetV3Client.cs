//--------------------------------------------------------------------------------//
// ファイル名: NuGetV3Client.cs
//
// ファイル記述: NuGet.org v3 API の検索、バージョン取得、パッケージ取得を提供するクライアント。
//
// 作成者: 2026/08/19 - アシェシュ・デベロップメント
//
// Copyleft(c) 2026-2026 アシェシュ・デベロップメント. All rights, Reversed.
//--------------------------------------------------------------------------------//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace ADKUnityNuGet
{
    internal static class NuGetV3Client
    {
        private const string SearchEndpoint = "https://azuresearch-usnc.nuget.org/query";
        private const string FlatContainerEndpoint = "https://api.nuget.org/v3-flatcontainer";
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly ConcurrentDictionary<string, Task<IReadOnlyList<string>>> VersionCache = new ConcurrentDictionary<string, Task<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);

        public static async Task<IReadOnlyList<NuGetSearchPackage>> SearchAsync(string query, bool includePrerelease)
        {
            string encodedQuery = Uri.EscapeDataString(query ?? string.Empty);
            string url = $"{SearchEndpoint}?q={encodedQuery}&prerelease={includePrerelease.ToString().ToLowerInvariant()}&take=40&semVerLevel=2.0.0";
            string json = await HttpClient.GetStringAsync(url);
            NuGetSearchResponse response = JsonUtility.FromJson<NuGetSearchResponse>(json);
            return response?.data ?? new List<NuGetSearchPackage>();
        }

        public static async Task<IReadOnlyList<string>> GetVersionsAsync(string packageId, bool includePrerelease)
        {
            string id = Normalize(packageId);
            Task<IReadOnlyList<string>> loadTask = VersionCache.GetOrAdd(id, LoadVersionsAsync);
            IReadOnlyList<string> allVersions;
            try
            {
                allVersions = await loadTask;
            }
            catch
            {
                VersionCache.TryRemove(id, out _);
                throw;
            }

            if (includePrerelease)
            {
                return allVersions;
            }

            return allVersions.Where(version => version.IndexOf('-', StringComparison.Ordinal) < 0).ToList();
        }

        public static async Task<string> GetLatestUpdateAsync(string packageId, string currentVersion, bool includePrerelease)
        {
            IReadOnlyList<string> versions = await GetVersionsAsync(packageId, includePrerelease);
            return versions.FirstOrDefault(version => NuGetVersionComparer.Instance.Compare(version, currentVersion) > 0);
        }

        public static async Task<byte[]> DownloadPackageAsync(string packageId, string version)
        {
            string id = Normalize(packageId);
            string normalizedVersion = Normalize(version);
            string url = $"{FlatContainerEndpoint}/{id}/{normalizedVersion}/{id}.{normalizedVersion}.nupkg";
            return await HttpClient.GetByteArrayAsync(url);
        }

        public static async Task<string> ResolveVersionAsync(string packageId, string versionRange, bool includePrerelease)
        {
            IReadOnlyList<string> versions = await GetVersionsAsync(packageId, includePrerelease);
            return NuGetVersionRange.SelectBestVersion(versions, versionRange);
        }


        private static async Task<IReadOnlyList<string>> LoadVersionsAsync(string id)
        {
            string json = await HttpClient.GetStringAsync($"{FlatContainerEndpoint}/{id}/index.json");
            NuGetVersionIndex response = JsonUtility.FromJson<NuGetVersionIndex>(json);
            return (response?.versions ?? new List<string>())
                .OrderByDescending(version => version, NuGetVersionComparer.Instance)
                .ToList();
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ADK-Unity-Nuget/0.3.0");
            return client;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }

    internal static class NuGetVersionRange
    {
        public static string SelectBestVersion(IEnumerable<string> versions, string range)
        {
            List<string> ordered = versions
                .Where(version => !string.IsNullOrWhiteSpace(version))
                .OrderByDescending(version => version, NuGetVersionComparer.Instance)
                .ToList();

            if (ordered.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(range))
            {
                return ordered[0];
            }

            range = range.Trim();
            if (!range.StartsWith("[", StringComparison.Ordinal) && !range.StartsWith("(", StringComparison.Ordinal))
            {
                return ordered.FirstOrDefault(version => NuGetVersionComparer.Instance.Compare(version, range) >= 0);
            }

            bool includeMin = range.StartsWith("[", StringComparison.Ordinal);
            bool includeMax = range.EndsWith("]", StringComparison.Ordinal);
            string inner = range.Substring(1, range.Length - 2);
            string[] bounds = inner.Split(',');

            if (bounds.Length == 1)
            {
                string exact = bounds[0].Trim();
                return ordered.FirstOrDefault(version => NuGetVersionComparer.Instance.Compare(version, exact) == 0);
            }

            string min = bounds[0].Trim();
            string max = bounds[1].Trim();

            foreach (string version in ordered)
            {
                bool minOk = string.IsNullOrEmpty(min) || (includeMin
                    ? NuGetVersionComparer.Instance.Compare(version, min) >= 0
                    : NuGetVersionComparer.Instance.Compare(version, min) > 0);

                bool maxOk = string.IsNullOrEmpty(max) || (includeMax
                    ? NuGetVersionComparer.Instance.Compare(version, max) <= 0
                    : NuGetVersionComparer.Instance.Compare(version, max) < 0);

                if (minOk && maxOk)
                {
                    return version;
                }
            }

            return null;
        }
    }

    internal sealed class NuGetVersionComparer : IComparer<string>
    {
        public static readonly NuGetVersionComparer Instance = new NuGetVersionComparer();

        public int Compare(string x, string y)
        {
            ParsedVersion left = ParsedVersion.Parse(x);
            ParsedVersion right = ParsedVersion.Parse(y);

            int numeric = CompareNumbers(left.Numbers, right.Numbers);
            if (numeric != 0)
            {
                return numeric;
            }

            // SemVer 2.0.0 Section 11: Normal versions have higher precedence than pre-release versions.
            if (left.IsStable != right.IsStable)
            {
                return left.IsStable ? 1 : -1;
            }

            if (left.IsStable && right.IsStable)
            {
                return 0;
            }

            // SemVer 2.0.0 Section 11: Pre-release precedence dot-separated comparison.
            return ComparePrerelease(left.Prerelease, right.Prerelease);
        }

        private static int CompareNumbers(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            int count = Math.Max(left.Count, right.Count);
            for (int i = 0; i < count; i++)
            {
                int leftValue = i < left.Count ? left[i] : 0;
                int rightValue = i < right.Count ? right[i] : 0;
                int comparison = leftValue.CompareTo(rightValue);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private static int ComparePrerelease(string left, string right)
        {
            string[] leftParts = (left ?? string.Empty).Split('.');
            string[] rightParts = (right ?? string.Empty).Split('.');
            int minLength = Math.Min(leftParts.Length, rightParts.Length);

            for (int i = 0; i < minLength; i++)
            {
                string leftId = leftParts[i];
                string rightId = rightParts[i];

                bool leftIsNum = long.TryParse(leftId, NumberStyles.None, CultureInfo.InvariantCulture, out long leftNum);
                bool rightIsNum = long.TryParse(rightId, NumberStyles.None, CultureInfo.InvariantCulture, out long rightNum);

                if (leftIsNum && rightIsNum)
                {
                    int comp = leftNum.CompareTo(rightNum);
                    if (comp != 0)
                    {
                        return comp;
                    }
                }
                else if (leftIsNum && !rightIsNum)
                {
                    // Numeric identifiers always have lower precedence than non-numeric identifiers.
                    return -1;
                }
                else if (!leftIsNum && rightIsNum)
                {
                    return 1;
                }
                else
                {
                    // Identifiers with letters or hyphens are compared lexically in ASCII sort order.
                    int comp = string.Compare(leftId, rightId, StringComparison.Ordinal);
                    if (comp != 0)
                    {
                        return comp;
                    }
                }
            }

            // A larger set of pre-release fields has a higher precedence than a smaller set.
            return leftParts.Length.CompareTo(rightParts.Length);
        }

        private readonly struct ParsedVersion
        {
            private ParsedVersion(List<int> numbers, string prerelease)
            {
                Numbers = numbers;
                Prerelease = prerelease ?? string.Empty;
            }

            public IReadOnlyList<int> Numbers { get; }
            public string Prerelease { get; }
            public bool IsStable => string.IsNullOrEmpty(Prerelease);

            public static ParsedVersion Parse(string value)
            {
                string clean = (value ?? string.Empty).Trim();
                int metadata = clean.IndexOf('+');
                if (metadata >= 0)
                {
                    clean = clean.Substring(0, metadata);
                }

                string prerelease = string.Empty;
                int separator = clean.IndexOf('-');
                if (separator >= 0)
                {
                    prerelease = clean.Substring(separator + 1);
                    clean = clean.Substring(0, separator);
                }

                List<int> numbers = clean.Split('.')
                    .Select(part => int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : 0)
                    .ToList();

                return new ParsedVersion(numbers, prerelease);
            }
        }
    }
}
