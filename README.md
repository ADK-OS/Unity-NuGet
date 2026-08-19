![Unity NuGet](Documentation~/Images/ADKUnityNuget.jpg)

<div align="center">

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/unity-2021.3%2B-blue.svg)](https://unity.com/)
[![Semantic Versioning](https://img.shields.io/badge/semver-2.0.0-informational)](https://semver.org/#semantic-versioning-200)
[![Release](https://img.shields.io/github/v/release/ADK-OS/Unity-NuGet?include_prereleases&color=blue&label=release)](https://github.com/ADK-OS/Unity-NuGet/releases)

</div>

# What is Unity NuGet?

**Unity NuGet** is a lightweight, reusable NuGet package manager built from scratch to run inside the Unity Editor. NuGet is the standard package management system for .NET, making it easy to discover, distribute, and consume reusable assemblies and libraries.

Unity NuGet provides a modern, visual editor interface within Unity to search [nuget.org](https://www.nuget.org/), inspect package metadata, install packages along with their recursive dependencies, explore dependency graphs, scan for updates, and restore project packages seamlessly after clean checkouts.

> [!NOTE]
> **Inspiration**: This project is inspired by the workflow of [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity), utilizing an independent implementation, dedicated project settings model, safe asset migration system, interactive dependency tracker, and modern Unity Editor UI.

---

## Key Features

- 🔍 **Search & Browse NuGet.org**: Search packages by name, title, or keywords with live download metrics, authors, descriptions, and version selector.
- 📦 **Recursive Dependency Resolution**: Inspects `.nuspec` manifests within `.nupkg` archives and recursively installs required dependencies.
- 🎯 **Unity Framework Targeting**: Automatically prioritizes Unity-compatible target frameworks (`netstandard2.1` > `netstandard2.0` > `net48` down to `net45`).
- 📁 **Configurable Install Folder & Safe Migration**: Easily set or browse custom package install locations under `Assets/`. Migrates existing packages safely and guards against overwriting non-empty folders.
- 🔄 **Parallel Update Checking**: Quickly scans all direct package installations in parallel against NuGet.org and performs one-click updates.
- 🌳 **Visual Dependency Graph**: Dedicated interactive tree view to inspect direct vs. transitive dependency relationships with circular reference protection.
- ♻️ **Project Package Restore**: Automatically re-downloads and reinstalls all tracked packages from `ProjectSettings/ADKUnityNuget.json`.
- ⚡ **Zero Third-Party Dependencies**: Built entirely with standard .NET BCL and Unity Editor APIs—no external DLLs or CLI tools required.

---

## Installation & Setup

### Option 1: Unity Package Manager (via Git URL)

1. In the Unity Editor, open **Window > Package Manager**.
2. Click the **+** button in the top-left corner and select **Add package from git URL...**.
3. Enter the repository URL:
   ```text
   https://github.com/ADK-OS/Unity-NuGet.git
   ```
4. Click **Add**.

### Option 2: Add to `Packages/manifest.json`

Add Unity NuGet directly to your project's `Packages/manifest.json` under `dependencies`:

```json
{
  "dependencies": {
    "com.asheshdevelopment.adk-unity-nuget": "https://github.com/ADK-OS/Unity-NuGet.git"
  }
}
```

*To lock to a specific version (e.g., `v0.3.0`), append `#v0.3.0` to the URL:*
```text
https://github.com/ADK-OS/Unity-NuGet.git#v0.3.0
```

---

## Getting Started

### 1. Opening Package Manager
Access the package manager from the top Unity menu: **NuGet > Open Package Manager...**.

On first launch, Unity NuGet initializes project settings with the default install location:
```text
Assets/Plugins/ADKUnityNuget
```

### 2. Online Search & Installation
1. Navigate to the **Online** tab.
2. Enter a package ID or keyword (e.g., `Newtonsoft.Json`, `LiteDB`, `YamlDotNet`) and click **Search** (or press Enter).
3. Select a package from the results list to view its description, authors, downloads, and available versions.
4. Choose the desired version and click **Install Selected Version**. Unity NuGet will resolve and install all dependencies automatically.

### 3. Reviewing Installed Packages & Restoration
- In the **Installed** tab, review all currently tracked direct installs and dependencies.
- Filter the list using the search box.
- Click **Restore** to re-download all tracked project packages.
- Click **Remove** on any package to safely uninstall its folder and manifest entry.

### 4. Scanning for Updates
- Open the **Updates** tab (or select **NuGet > Check Installed Package Updates...**).
- Unity NuGet scans all direct installations concurrently and displays available newer versions with a one-click **Update** button.

### 5. Exploring Dependency Graph
- Select **NuGet > Explore Dependency Graph...** to open the hierarchical dependency tree visualizer.

### 6. Configuring Project Settings
- Open **NuGet > Project Settings...** (or via **Edit > Project Settings > ADK Unity NuGet**).
- Change the install directory by typing or using **Browse...**. Changing this folder automatically migrates existing installed package folders.

---

## Target Framework Prioritization

Unity NuGet prioritizes framework assets extracted from `lib/` and `ref/` directories in the following order:

$$\text{netstandard2.1} \longrightarrow \text{netstandard2.0} \longrightarrow \text{net48} \longrightarrow \text{net472} \longrightarrow \dots \longrightarrow \text{net45}$$

Managed assemblies (`.dll`), debug symbols (`.pdb`), and documentation (`.xml`) are extracted, along with native runtime libraries found in `runtimes/.../native/`.

---

## State Persistence

Configuration and installation states are kept clean inside `ProjectSettings/`, ensuring they can be tracked in version control alongside Unity settings:

- **Project Configuration:** `ProjectSettings/ADKUnityNugetSettings.json` (stores install directory path, pre-release preferences).
- **Package Manifest:** `ProjectSettings/ADKUnityNuget.json` (stores tracked package IDs, versions, direct vs. dependency flags, and dependency tree links).

---

## Semantic Versioning (SemVer 2.0.0)

This project strictly adheres to the [Semantic Versioning 2.0.0 Specification](https://semver.org/#semantic-versioning-200):

$$\text{MAJOR}.\text{MINOR}.\text{PATCH}$$

1. **MAJOR** (`x.0.0`): Breaking changes, architectural redesigns, or upgraded Unity engine prerequisites.
2. **MINOR** (`0.x.0`): New features, non-breaking workflow enhancements, new editor windows, and backwards-compatible improvements.
3. **PATCH** (`0.0.x`): Backwards-compatible bug fixes, performance optimizations, and documentation updates.

Internal version comparison and resolution also implement full SemVer 2.0.0 Section 11 precedence rules (numeric identifier sorting, dot-separated pre-release segments, and build metadata exclusion).

---

## Progress Tracking & Development Status

| Feature / Milestone | Version | Status | Description |
| :--- | :---: | :---: | :--- |
| **NuGet.org v3 Search & Metadata** | `0.1.0` | ✅ Done | Search packages, query version indices, display descriptions and downloads. |
| **Recursive Dependency Resolution** | `0.1.0` | ✅ Done | Parse `.nuspec` XML, extract framework-specific dependencies recursively. |
| **Framework Compatibility Scoring** | `0.1.0` | ✅ Done | Automatic prioritization of `.NET Standard 2.1/2.0` and `.NET Framework`. |
| **3-Tab Editor Package Manager UI** | `0.2.0` | ✅ Done | Online discovery, Installed management, and Update checking in `ADKUnityNugetWindow`. |
| **Project Settings Provider** | `0.2.0` | ✅ Done | Integrated with Unity Project Settings (`ProjectSettings/ADKUnityNugetSettings.json`). |
| **Safe Path Migration** | `0.2.0` | ✅ Done | Automatic asset directory relocation with non-empty destination guard. |
| **Dependency Graph Viewer** | `0.2.0` | ✅ Done | Interactive foldout tree view visualizing direct and transitive dependencies. |
| **Update Scanning & One-Click Upgrade** | `0.2.0` | ✅ Done | Parallel version query for all direct installs. |
| **Namespace Refactor (`ADKUnityNuGet`)** | `0.3.0` | ✅ Done | Unified clean namespace across all C# code and assembly definition. |
| **Repository URL & Rebranding** | `0.3.0` | ✅ Done | Updated repo link to `ADK-OS/Unity-NuGet` and standardized branding. |
| **Progress & SemVer Documentation** | `0.3.0` | ✅ Done | Detailed changelog and tracking matrices for release management. |
| **GitHub Actions Release Automation** | `0.3.0` | ✅ Done | Automated package zipping and release publishing on version tag pushes. |
| **Transitive Dependency Pruning / GC** | `0.4.0` | 📋 Planned | Automatic cleanup of orphaned dependencies when root packages are removed. |
| **Plugin Platform Import Settings** | `0.4.0` | 📋 Planned | Automated `PluginImporter` RID platform configuration for native runtimes. |
| **Private & Authenticated NuGet Feeds** | `0.5.0` | 📋 Planned | Support custom package sources (Azure DevOps, GitHub Packages, BaGet). |

---

## Limitations

- NuGet.org is currently the primary package source (private feed support planned).
- NuGet `packages.config`, MSBuild `.targets`/`.props`, install scripts, analyzers, and source generators are not executed.
- Dependency range handling covers standard NuGet interval syntax and plain minimum versions.
- Native runtime assets are extracted, but platform import settings are not yet automatically assigned per RID.
- Removing a direct package does not automatically garbage-collect dependencies if other packages share them.
- Assembly compatibility depends on the project's scripting backend (.NET Standard 2.1 / .NET Framework), target platform, and the NuGet package.

---

## Acknowledgements

- Inspired by [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).
- Follows the [Semantic Versioning 2.0.0 Specification](https://semver.org/#semantic-versioning-200) by Tom Preston-Werner ([CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)).

---

## License

MIT License. See [LICENSE](LICENSE).
