//--------------------------------------------------------------------------------//
// ファイル名: ADKUnityNugetDependencyWindow.cs
//
// ファイル記述: インストール済み NuGet パッケージの依存関係グラフを表示する。
//
// 作成者: 2026/08/19 - アシェシュ・デベロップメント
//
// Copyleft(c) 2026-2026 アシェシュ・デベロップメント. All rights, Reversed.
//--------------------------------------------------------------------------------//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ADKUnityNuGet
{
    public sealed class ADKUnityNugetDependencyWindow : EditorWindow
    {
        private Vector2 scroll;
        private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static void Open()
        {
            ADKUnityNugetDependencyWindow window = GetWindow<ADKUnityNugetDependencyWindow>();
            window.titleContent = new GUIContent("NuGet Dependencies");
            window.minSize = new Vector2(460f, 320f);
            window.Show();
        }

        private void OnGUI()
        {
            ADKNuGetManifest manifest = NuGetPackageInstaller.LoadManifest();
            EditorGUILayout.LabelField("Dependency Graph", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tracked package relationships for this project.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6f);

            if (manifest.packages.Count == 0)
            {
                EditorGUILayout.HelpBox("No NuGet packages are currently tracked.", MessageType.Info);
                return;
            }

            Dictionary<string, ADKNuGetInstalledPackage> byId = manifest.packages
                .GroupBy(package => package.id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (ADKNuGetInstalledPackage package in manifest.packages
                         .Where(item => !item.isDependency)
                         .OrderBy(item => item.id, StringComparer.OrdinalIgnoreCase))
            {
                DrawPackage(package, byId, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPackage(
            ADKNuGetInstalledPackage package,
            IReadOnlyDictionary<string, ADKNuGetInstalledPackage> byId,
            int depth,
            HashSet<string> ancestry)
        {
            if (package == null || string.IsNullOrEmpty(package.id))
            {
                return;
            }

            string key = package.id + "@" + package.version;
            bool hasDependencies = package.dependencies != null && package.dependencies.Count > 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 18f);
                if (hasDependencies)
                {
                    bool expanded = foldouts.TryGetValue(key, out bool stored) && stored;
                    expanded = EditorGUILayout.Foldout(expanded, $"{package.id}  {package.version}", true);
                    foldouts[key] = expanded;
                }
                else
                {
                    GUILayout.Space(13f);
                    EditorGUILayout.LabelField($"{package.id}  {package.version}");
                }
            }

            if (!hasDependencies || !foldouts[key])
            {
                return;
            }

            if (!ancestry.Add(package.id))
            {
                return;
            }

            foreach (string dependencyId in package.dependencies.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                if (byId.TryGetValue(dependencyId, out ADKNuGetInstalledPackage dependency))
                {
                    DrawPackage(dependency, byId, depth + 1, ancestry);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space((depth + 1) * 18f + 13f);
                        EditorGUILayout.LabelField(dependencyId + "  (not tracked)", EditorStyles.miniLabel);
                    }
                }
            }

            ancestry.Remove(package.id);
        }
    }
}
