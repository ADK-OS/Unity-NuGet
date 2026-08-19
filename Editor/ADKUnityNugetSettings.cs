//--------------------------------------------------------------------------------//
// ファイル名: ADKUnityNugetSettings.cs
//
// ファイル記述: ADK Unity Nuget のプロジェクト設定、保存先、永続化を管理する。
//
// 作成者: 2026/08/19 - アシェシュ・デベロップメント
//
// Copyleft(c) 2026-2026 アシェシュ・デベロップメント. All rights, Reversed.
//--------------------------------------------------------------------------------//

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ADKUnityNuGet
{
    [Serializable]
    internal sealed class ADKUnityNugetSettingsData
    {
        public string installRoot = ADKUnityNugetSettings.DefaultInstallRoot;
        public bool includePrereleaseByDefault;
        public bool initialized;
    }

    internal static class ADKUnityNugetSettings
    {
        public const string DefaultInstallRoot = "Assets/Plugins/ADKUnityNuget";
        public const string SettingsPath = "ProjectSettings/ADKUnityNugetSettings.json";
        public const string SettingsMenuPath = "Project/ADK Unity Nuget";

        private static ADKUnityNugetSettingsData current;

        public static ADKUnityNugetSettingsData Current
        {
            get
            {
                EnsureInitialized();
                return current;
            }
        }

        public static bool EnsureInitialized()
        {
            if (current != null)
            {
                return false;
            }

            bool firstLaunch = !File.Exists(SettingsPath);
            current = Load();

            if (string.IsNullOrWhiteSpace(current.installRoot) || !IsValidInstallRoot(current.installRoot))
            {
                current.installRoot = DefaultInstallRoot;
            }

            if (!current.initialized || firstLaunch)
            {
                current.initialized = true;
                Save();
                return true;
            }

            return false;
        }

        public static void SetInstallRoot(string path)
        {
            EnsureInitialized();
            string normalized = NormalizeInstallRoot(path);
            if (!IsValidInstallRoot(normalized))
            {
                throw new ArgumentException("The NuGet install location must be a folder inside this project's Assets folder.");
            }

            if (string.Equals(current.installRoot, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string previous = current.installRoot;
            NuGetPackageInstaller.MigrateInstallRoot(previous, normalized);
            current.installRoot = normalized;
            Save();
        }

        public static void SetIncludePrereleaseByDefault(bool value)
        {
            EnsureInitialized();
            if (current.includePrereleaseByDefault == value)
            {
                return;
            }

            current.includePrereleaseByDefault = value;
            Save();
        }

        public static void ResetInstallRoot()
        {
            SetInstallRoot(DefaultInstallRoot);
        }

        public static string ChooseInstallRoot(string currentPath)
        {
            string initialAbsolute = ToAbsoluteProjectPath(IsValidInstallRoot(currentPath) ? currentPath : DefaultInstallRoot);
            string selected = EditorUtility.OpenFolderPanel("Choose ADK Unity Nuget Install Folder", initialAbsolute, string.Empty);
            if (string.IsNullOrEmpty(selected))
            {
                return null;
            }

            string assetsAbsolute = NormalizeSlashes(Path.GetFullPath(Application.dataPath)).TrimEnd('/');
            string selectedAbsolute = NormalizeSlashes(Path.GetFullPath(selected)).TrimEnd('/');
            if (!selectedAbsolute.StartsWith(assetsAbsolute + "/", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(selectedAbsolute, assetsAbsolute, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "ADK Unity Nuget",
                    "Choose a folder inside this project's Assets folder.",
                    "OK");
                return null;
            }

            string suffix = selectedAbsolute.Length == assetsAbsolute.Length
                ? string.Empty
                : selectedAbsolute.Substring(assetsAbsolute.Length).TrimStart('/');

            string projectPath = string.IsNullOrEmpty(suffix) ? "Assets" : "Assets/" + suffix;
            if (string.Equals(projectPath, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "ADK Unity Nuget",
                    "Choose a subfolder inside Assets instead of the Assets root itself.",
                    "OK");
                return null;
            }

            return NormalizeInstallRoot(projectPath);
        }

        public static bool IsValidInstallRoot(string path)
        {
            string normalized = NormalizeInstallRoot(path);
            return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                   normalized.Length > "Assets/".Length &&
                   normalized.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        private static ADKUnityNugetSettingsData Load()
        {
            if (!File.Exists(SettingsPath))
            {
                return new ADKUnityNugetSettingsData();
            }

            try
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonUtility.FromJson<ADKUnityNugetSettingsData>(json) ?? new ADKUnityNugetSettingsData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ADK Unity Nuget could not load its settings: {exception.Message}");
                return new ADKUnityNugetSettingsData();
            }
        }

        private static void Save()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, JsonUtility.ToJson(current, true));
        }

        private static string NormalizeInstallRoot(string path)
        {
            string normalized = NormalizeSlashes(path ?? string.Empty).Trim();
            while (normalized.EndsWith("/", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }

            return normalized;
        }

        private static string NormalizeSlashes(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }

        private static string ToAbsoluteProjectPath(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }
    }

    internal sealed class ADKUnityNugetSettingsProvider : SettingsProvider
    {
        private ADKUnityNugetSettingsProvider(string path, SettingsScope scope)
            : base(path, scope)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ADKUnityNugetSettingsProvider(ADKUnityNugetSettings.SettingsMenuPath, SettingsScope.Project)
            {
                keywords = new System.Collections.Generic.HashSet<string>(new[]
                {
                    "NuGet",
                    "ADK",
                    "Packages",
                    "Install",
                    "Assets"
                })
            };
        }

        public override void OnGUI(string searchContext)
        {
            ADKUnityNugetSettings.EnsureInitialized();
            ADKUnityNugetSettingsData settings = ADKUnityNugetSettings.Current;

            EditorGUILayout.LabelField("Package Installation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "NuGet package assets are installed inside this project's Assets folder. The location is saved automatically per project.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string editedInstallRoot = EditorGUILayout.DelayedTextField("Install Location", settings.installRoot);
                if (EditorGUI.EndChangeCheck())
                {
                    TrySetInstallRoot(editedInstallRoot);
                }

                if (GUILayout.Button("Browse...", GUILayout.Width(80f)))
                {
                    string selected = ADKUnityNugetSettings.ChooseInstallRoot(settings.installRoot);
                    if (!string.IsNullOrEmpty(selected))
                    {
                        TrySetInstallRoot(selected);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                using (new EditorGUI.DisabledScope(string.Equals(settings.installRoot, ADKUnityNugetSettings.DefaultInstallRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    if (GUILayout.Button("Reset to Default", GUILayout.Width(120f)))
                    {
                        TrySetInstallRoot(ADKUnityNugetSettings.DefaultInstallRoot);
                    }
                }
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Package Discovery", EditorStyles.boldLabel);
            bool includePrerelease = EditorGUILayout.Toggle("Include Prerelease by Default", settings.includePrereleaseByDefault);
            if (includePrerelease != settings.includePrereleaseByDefault)
            {
                ADKUnityNugetSettings.SetIncludePrereleaseByDefault(includePrerelease);
            }
        }

        private static void TrySetInstallRoot(string path)
        {
            try
            {
                ADKUnityNugetSettings.SetInstallRoot(path);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("ADK Unity Nuget", exception.Message, "OK");
            }
        }
    }
}
