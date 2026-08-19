# Changelog

All notable changes to this project are documented in this file in accordance with [Semantic Versioning (SemVer 2.0.0)](https://semver.org/).

## 0.3.0 - 2026-08-19

### Changed

- **Repository URL & Rebranding**: Updated repository URL to `https://github.com/ADK-OS/Unity-NuGet.git` and rebranded display name to **Unity NuGet**.
- **Namespace Refactoring**: Changed codebase namespace to `ADKUnityNuGet` across all C# source files and updated `rootNamespace` in `ADKUnityNuget.Editor.asmdef`.
- **Semantic Versioning Standard**: Formalized Semantic Versioning compliance and version-tracking across `package.json`, editor UI footers, dialogs, and HTTP User-Agent headers (`ADK-Unity-Nuget/0.3.0`).

### Added

- **Project Progress & Roadmap Tracking**: Added detailed development status and feature tracking documentation in `README.md`.
- **Package Manifest Repository Metadata**: Added `repository` field directly to `package.json`.

## 0.2.0 - 2026-08-19

- Reworked the Unity menu into a dedicated top-level `NuGet` workflow.
- Added Online, Installed, and Updates views to the main package manager window.
- Added first-launch project settings with the default install location `Assets/Plugins/ADKUnityNuget`.
- Added editable and browsable install-location settings restricted to the project's `Assets/` folder.
- Added automatic settings save/load under `ProjectSettings/ADKUnityNugetSettings.json`.
- Added migration of the existing package install directory when the configured install location changes.
- Added project package restore support.
- Added dependency relationship tracking and a dependency graph window.
- Added package update scanning and per-package update actions.
- Added thread-safe in-session NuGet version-index caching to reduce repeated network requests.
- Expanded package metadata display and installed-package filtering.
- Fixed package extraction setup in the installer.

## 0.1.0 - 2026-08-19

- Initial ADK Unity Nuget prototype.
- Added NuGet.org package search and version selection.
- Added recursive dependency installation.
- Added Unity-compatible framework selection.
- Added managed and native package extraction.
- Added project manifest tracking and package removal.
