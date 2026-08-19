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
    [InitializeOnLoad]
    public static class ADKUnityNugetMenu
    {
        [MenuItem("NuGet/Open Package Manager...", false, 0)]
        [MenuItem("Window/NuGet/Package Manager", false, 1500)]
        public static void OpenPackageManager()
        {
            ADKUnityNugetWindow.OpenBrowse();
        }

        [MenuItem("NuGet/Restore Project Packages", false, 10)]
        [MenuItem("Window/NuGet/Restore Packages", false, 1510)]
        public static async void RestoreProjectPackages()
        {
            try
            {
                await NuGetPackageInstaller.RestoreAsync(message => Debug.Log($"[Unity NuGet] {message}"));
                EditorUtility.DisplayDialog("Unity NuGet", "Tracked NuGet packages were restored successfully.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Unity NuGet", exception.Message, "OK");
            }
        }

        [MenuItem("NuGet/Explore Dependency Graph...", false, 20)]
        [MenuItem("Window/NuGet/Dependency Graph", false, 1520)]
        public static void ExploreDependencyGraph()
        {
            ADKUnityNugetDependencyWindow.Open();
        }

        [MenuItem("NuGet/Check Installed Package Updates...", false, 30)]
        [MenuItem("Window/NuGet/Check for Updates", false, 1530)]
        public static void CheckInstalledPackageUpdates()
        {
            ADKUnityNugetWindow.OpenUpdates(true);
        }

        [MenuItem("NuGet/Project Settings...", false, 50)]
        [MenuItem("Window/NuGet/Settings", false, 1540)]
        public static void OpenProjectSettings()
        {
            ADKUnityNugetSettings.EnsureInitialized();
            SettingsService.OpenProjectSettings(ADKUnityNugetSettings.SettingsMenuPath);
        }

        [MenuItem("NuGet/About Unity NuGet", false, 100)]
        [MenuItem("Window/NuGet/About", false, 1550)]
        public static void About()
        {
            EditorUtility.DisplayDialog(
                "Unity NuGet",
                "Unity NuGet 0.3.0\n\nReusable NuGet package management for Unity projects.\n\nAshesh Development",
                "Close");
        }
    }
}
