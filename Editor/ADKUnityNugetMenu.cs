//--------------------------------------------------------------------------------//
// ファイル名: ADKUnityNugetMenu.cs
//
// ファイル記述: Unity の NuGet トップレベルメニューと主要コマンドを提供する。
//
// 作成者: 2026/08/19 - アシェシュ・デベロップメント
//
// Copyleft(c) 2026-2026 アシェシュ・デベロップメント. All rights, Reversed.
//--------------------------------------------------------------------------------//

using System;
using UnityEditor;
using UnityEngine;

namespace ADKUnityNuGet
{
    internal static class ADKUnityNugetMenu
    {
        [MenuItem("NuGet/Open Package Manager...", false, 0)]
        private static void OpenPackageManager()
        {
            ADKUnityNugetWindow.OpenBrowse();
        }

        [MenuItem("NuGet/Restore Project Packages", false, 10)]
        private static async void RestoreProjectPackages()
        {
            try
            {
                await NuGetPackageInstaller.RestoreAsync(message => Debug.Log($"[ADK Unity Nuget] {message}"));
                EditorUtility.DisplayDialog("ADK Unity Nuget", "Tracked NuGet packages were restored successfully.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("ADK Unity Nuget", exception.Message, "OK");
            }
        }

        [MenuItem("NuGet/Explore Dependency Graph...", false, 20)]
        private static void ExploreDependencyGraph()
        {
            ADKUnityNugetDependencyWindow.Open();
        }

        [MenuItem("NuGet/Check Installed Package Updates...", false, 30)]
        private static void CheckInstalledPackageUpdates()
        {
            ADKUnityNugetWindow.OpenUpdates(true);
        }

        [MenuItem("NuGet/Project Settings...", false, 50)]
        private static void OpenProjectSettings()
        {
            ADKUnityNugetSettings.EnsureInitialized();
            SettingsService.OpenProjectSettings(ADKUnityNugetSettings.SettingsMenuPath);
        }

        [MenuItem("NuGet/About ADK Unity Nuget", false, 100)]
        private static void About()
        {
            EditorUtility.DisplayDialog(
                "Unity NuGet",
                "Unity NuGet 0.3.0\n\nReusable NuGet package management for Unity projects.\n\nAshesh Development",
                "Close");
        }
    }
}
