//--------------------------------------------------------------------------------//
// ファイル名: NuGetPackageInstaller.cs
//
// ファイル記述: NuGet パッケージの依存関係解決、展開、削除、インストール状態管理を行う。
//
// 作成者: 2026/08/19 - アシェシュ・デベロップメント
//
// Copyleft(c) 2026-2026 アシェシュ・デベロップメント. All rights, Reversed.
//--------------------------------------------------------------------------------//

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace ADKUnityNuGet
{
    internal static class NuGetPackageInstaller
    {
        private const string ManifestPath = "ProjectSettings/ADKUnityNuget.json";

        private static readonly string[] FrameworkPreference =
        {
            "netstandard2.1",
            "netstandard2.0",
            "net48",
            "net472",
            "net471",
            "net47",
            "net462",
            "net461",
            "net46",
            "net452",
            "net45"
        };

        private static string InstallRoot => ADKUnityNugetSettings.Current.installRoot;

        public static ADKNuGetManifest LoadManifest()
        {
            if (!File.Exists(ManifestPath))
            {
                return new ADKNuGetManifest();
            }

            try
            {
                string json = File.ReadAllText(ManifestPath);
                ADKNuGetManifest manifest = JsonUtility.FromJson<ADKNuGetManifest>(json) ?? new ADKNuGetManifest();
                if (manifest.packages == null)
                {
                    manifest.packages = new List<ADKNuGetInstalledPackage>();
                }

                foreach (ADKNuGetInstalledPackage package in manifest.packages)
                {
                    if (package.dependencies == null)
                    {
                        package.dependencies = new List<string>();
                    }
                }
                return manifest;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ADK Unity Nuget could not read its manifest: {exception.Message}");
                return new ADKNuGetManifest();
            }
        }

        public static async Task InstallAsync(string packageId, string version, bool includePrerelease, Action<string> progress)
        {
            ADKNuGetManifest manifest = LoadManifest();
            HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await InstallRecursiveAsync(packageId, version, false, includePrerelease, manifest, visiting, progress, false);
            SaveManifest(manifest);
            AssetDatabase.Refresh();
        }

        public static async Task RestoreAsync(Action<string> progress)
        {
            ADKNuGetManifest existingManifest = LoadManifest();
            List<ADKNuGetInstalledPackage> roots = existingManifest.packages
                .Where(package => !package.isDependency)
                .Select(ClonePackage)
                .ToList();

            if (roots.Count == 0)
            {
                progress?.Invoke("No tracked packages require restoration.");
                return;
            }

            ADKNuGetManifest restoredManifest = new ADKNuGetManifest();
            foreach (ADKNuGetInstalledPackage root in roots)
            {
                HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await InstallRecursiveAsync(root.id, root.version, false, true, restoredManifest, visiting, progress, true);
            }

            SaveManifest(restoredManifest);
            AssetDatabase.Refresh();
        }

        public static async Task<IReadOnlyList<NuGetPackageUpdate>> CheckForUpdatesAsync(bool includePrerelease)
        {
            List<ADKNuGetInstalledPackage> roots = LoadManifest().packages
                .Where(package => !package.isDependency)
                .OrderBy(package => package.id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Task<NuGetPackageUpdate>[] checks = roots.Select(async package =>
            {
                string available = await NuGetV3Client.GetLatestUpdateAsync(package.id, package.version, includePrerelease);
                return string.IsNullOrEmpty(available) ? null : new NuGetPackageUpdate(package, available);
            }).ToArray();

            NuGetPackageUpdate[] results = await Task.WhenAll(checks);
            return results.Where(update => update != null).ToList();
        }

        public static void Uninstall(string packageId, string version)
        {
            ADKNuGetManifest manifest = LoadManifest();
            string packageFolder = Path.Combine(InstallRoot, SanitizePathPart(packageId));

            if (Directory.Exists(packageFolder))
            {
                Directory.Delete(packageFolder, true);
            }

            manifest.packages.RemoveAll(package =>
                string.Equals(package.id, packageId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(package.version, version, StringComparison.OrdinalIgnoreCase));

            SaveManifest(manifest);
            AssetDatabase.Refresh();
        }

        public static void MigrateInstallRoot(string previousRoot, string newRoot)
        {
            if (!ADKUnityNugetSettings.IsValidInstallRoot(newRoot))
            {
                throw new ArgumentException("The target install location must be inside Assets.");
            }

            if (string.IsNullOrWhiteSpace(previousRoot) ||
                string.Equals(previousRoot, newRoot, StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(previousRoot))
            {
                return;
            }

            if (Directory.Exists(newRoot) && Directory.EnumerateFileSystemEntries(newRoot).Any())
            {
                throw new IOException($"The selected NuGet install location is not empty: {newRoot}");
            }

            string parent = Path.GetDirectoryName(newRoot);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            if (Directory.Exists(newRoot))
            {
                Directory.Delete(newRoot, true);
            }

            Directory.Move(previousRoot, newRoot);
            AssetDatabase.Refresh();
        }

        private static async Task InstallRecursiveAsync(
            string packageId,
            string version,
            bool isDependency,
            bool includePrerelease,
            ADKNuGetManifest manifest,
            HashSet<string> visiting,
            Action<string> progress,
            bool forceInstall)
        {
            string key = $"{packageId}@{version}";
            if (!visiting.Add(key))
            {
                return;
            }

            try
            {
                ADKNuGetInstalledPackage existing = manifest.packages.FirstOrDefault(package =>
                    string.Equals(package.id, packageId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(package.version, version, StringComparison.OrdinalIgnoreCase));

                if (existing != null && !forceInstall)
                {
                    if (!isDependency && existing.isDependency)
                    {
                        existing.isDependency = false;
                    }
                    return;
                }

                progress?.Invoke($"Downloading {packageId} {version}...");
                byte[] packageBytes = await NuGetV3Client.DownloadPackageAsync(packageId, version);

                List<NuGetDependency> dependencies;
                using (MemoryStream stream = new MemoryStream(packageBytes, false))
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                {
                    dependencies = ReadDependencies(archive);
                }

                List<string> dependencyIds = new List<string>();
                foreach (NuGetDependency dependency in dependencies)
                {
                    progress?.Invoke($"Resolving {dependency.Id} {dependency.VersionRange}...");
                    string dependencyVersion = await NuGetV3Client.ResolveVersionAsync(dependency.Id, dependency.VersionRange, includePrerelease);
                    if (string.IsNullOrEmpty(dependencyVersion))
                    {
                        throw new InvalidOperationException($"Unable to resolve dependency {dependency.Id} {dependency.VersionRange}.");
                    }

                    dependencyIds.Add(dependency.Id);
                    await InstallRecursiveAsync(
                        dependency.Id,
                        dependencyVersion,
                        true,
                        includePrerelease,
                        manifest,
                        visiting,
                        progress,
                        forceInstall);
                }

                progress?.Invoke($"Installing {packageId} {version}...");
                ExtractPackage(packageId, version, packageBytes);

                manifest.packages.RemoveAll(package => string.Equals(package.id, packageId, StringComparison.OrdinalIgnoreCase));
                manifest.packages.Add(new ADKNuGetInstalledPackage
                {
                    id = packageId,
                    version = version,
                    isDependency = isDependency,
                    dependencies = dependencyIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                });
            }
            finally
            {
                visiting.Remove(key);
            }
        }

        private static List<NuGetDependency> ReadDependencies(ZipArchive archive)
        {
            ZipArchiveEntry nuspecEntry = archive.Entries.FirstOrDefault(entry =>
                entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));

            if (nuspecEntry == null)
            {
                return new List<NuGetDependency>();
            }

            using Stream stream = nuspecEntry.Open();
            XDocument document = XDocument.Load(stream);
            XElement metadata = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "metadata");
            XElement dependencies = metadata?.Elements().FirstOrDefault(element => element.Name.LocalName == "dependencies");
            if (dependencies == null)
            {
                return new List<NuGetDependency>();
            }

            List<XElement> groups = dependencies.Elements().Where(element => element.Name.LocalName == "group").ToList();
            IEnumerable<XElement> dependencyElements;

            if (groups.Count > 0)
            {
                XElement selectedGroup = groups
                    .OrderByDescending(group => FrameworkScore((string)group.Attribute("targetFramework")))
                    .FirstOrDefault();
                dependencyElements = selectedGroup?.Elements().Where(element => element.Name.LocalName == "dependency")
                    ?? Enumerable.Empty<XElement>();
            }
            else
            {
                dependencyElements = dependencies.Elements().Where(element => element.Name.LocalName == "dependency");
            }

            return dependencyElements
                .Select(element => new NuGetDependency(
                    (string)element.Attribute("id"),
                    (string)element.Attribute("version")))
                .Where(dependency => !string.IsNullOrWhiteSpace(dependency.Id))
                .ToList();
        }

        private static void ExtractPackage(string packageId, string version, byte[] packageBytes)
        {
            string packageRoot = Path.Combine(InstallRoot, SanitizePathPart(packageId));
            string versionRoot = Path.Combine(packageRoot, SanitizePathPart(version));

            if (Directory.Exists(packageRoot))
            {
                Directory.Delete(packageRoot, true);
            }

            Directory.CreateDirectory(versionRoot);

            using MemoryStream stream = new MemoryStream(packageBytes, false);
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false);

            string managedRoot = SelectManagedAssetRoot(archive);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                bool managedAsset = managedRoot != null &&
                                    entry.FullName.StartsWith(managedRoot + "/", StringComparison.OrdinalIgnoreCase) &&
                                    IsManagedSidecar(entry.Name);

                bool nativeAsset = entry.FullName.StartsWith("runtimes/", StringComparison.OrdinalIgnoreCase) &&
                                   entry.FullName.IndexOf("/native/", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!managedAsset && !nativeAsset)
                {
                    continue;
                }

                string relativePath = managedAsset
                    ? entry.FullName.Substring(managedRoot.Length + 1)
                    : entry.FullName;

                string targetPath = Path.Combine(versionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                using Stream source = entry.Open();
                using FileStream destination = File.Create(targetPath);
                source.CopyTo(destination);
            }
        }

        private static string SelectManagedAssetRoot(ZipArchive archive)
        {
            HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string normalized = entry.FullName.Replace('\\', '/');
                if (!normalized.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
                    !normalized.StartsWith("ref/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] segments = normalized.Split('/');
                if (segments.Length >= 3)
                {
                    roots.Add(segments[0] + "/" + segments[1]);
                }
            }

            return roots
                .OrderByDescending(root => FrameworkScore(root.Split('/')[1]))
                .ThenBy(root => root.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();
        }

        private static int FrameworkScore(string framework)
        {
            if (string.IsNullOrWhiteSpace(framework))
            {
                return 0;
            }

            string normalized = framework
                .Trim()
                .ToLowerInvariant()
                .Replace(".netstandard", "netstandard")
                .Replace(".netframework", "net")
                .Replace("version=v", string.Empty)
                .Replace("version=", string.Empty)
                .Replace(" ", string.Empty)
                .Replace(".", string.Empty);

            for (int i = 0; i < FrameworkPreference.Length; i++)
            {
                string preferred = FrameworkPreference[i].Replace(".", string.Empty);
                if (normalized.Contains(preferred))
                {
                    return 1000 - i;
                }
            }

            if (normalized.Contains("netstandard"))
            {
                return 500;
            }

            if (normalized.StartsWith("net", StringComparison.Ordinal))
            {
                return 400;
            }

            return 100;
        }

        private static bool IsManagedSidecar(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".xml", StringComparison.OrdinalIgnoreCase);
        }

        private static void SaveManifest(ADKNuGetManifest manifest)
        {
            string directory = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(ManifestPath, json);
        }

        private static ADKNuGetInstalledPackage ClonePackage(ADKNuGetInstalledPackage package)
        {
            return new ADKNuGetInstalledPackage
            {
                id = package.id,
                version = package.version,
                isDependency = package.isDependency,
                dependencies = package.dependencies == null ? new List<string>() : new List<string>(package.dependencies)
            };
        }

        private static string SanitizePathPart(string value)
        {
            string sanitized = value ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalid, '_');
            }

            return sanitized;
        }
    }
}
