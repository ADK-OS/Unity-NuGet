//--------------------------------------------------------------------------------//
// ファイル名: NuGetModels.cs
//
// ファイル記述: ADK Unity Nuget の検索結果、バージョン、インストール状態を表すモデル。
//
// 作成者: 2026/08/19 - アシェシュ・デベロップメント
//
// Copyleft(c) 2026-2026 アシェシュ・デベロップメント. All rights, Reversed.
//--------------------------------------------------------------------------------//

using System;
using System.Collections.Generic;

namespace ADKUnityNuGet
{
    [Serializable]
    internal sealed class NuGetSearchResponse
    {
        public int totalHits;
        public List<NuGetSearchPackage> data = new List<NuGetSearchPackage>();
    }

    [Serializable]
    internal sealed class NuGetSearchPackage
    {
        public string id;
        public string version;
        public string description;
        public List<string> authors = new List<string>();
        public long totalDownloads;
        public List<NuGetSearchVersion> versions = new List<NuGetSearchVersion>();
    }

    [Serializable]
    internal sealed class NuGetSearchVersion
    {
        public string version;
        public long downloads;
    }

    [Serializable]
    internal sealed class NuGetVersionIndex
    {
        public List<string> versions = new List<string>();
    }

    [Serializable]
    internal sealed class ADKNuGetManifest
    {
        public List<ADKNuGetInstalledPackage> packages = new List<ADKNuGetInstalledPackage>();
    }

    [Serializable]
    internal sealed class ADKNuGetInstalledPackage
    {
        public string id;
        public string version;
        public bool isDependency;
        public List<string> dependencies = new List<string>();
    }

    internal sealed class NuGetPackageUpdate
    {
        public NuGetPackageUpdate(ADKNuGetInstalledPackage installedPackage, string availableVersion)
        {
            InstalledPackage = installedPackage;
            AvailableVersion = availableVersion;
        }

        public ADKNuGetInstalledPackage InstalledPackage { get; }
        public string AvailableVersion { get; }
    }

    internal readonly struct NuGetDependency
    {
        public NuGetDependency(string id, string versionRange)
        {
            Id = id;
            VersionRange = versionRange;
        }

        public string Id { get; }
        public string VersionRange { get; }
    }
}
