//--------------------------------------------------------------------------------//
// ファイル名: ADKUnityNugetWindow.cs
//
// ファイル記述: ADK Unity Nuget の検索、インストール、更新、削除を操作する Unity Editor ウィンドウ。
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
    public sealed class ADKUnityNugetWindow : EditorWindow
    {
        private enum Page
        {
            Online,
            Installed,
            Updates
        }

        private Page page;
        private string onlineSearch = string.Empty;
        private string installedSearch = string.Empty;
        private bool includePrerelease;
        private bool busy;
        private string status = "Ready.";
        private Vector2 resultScroll;
        private Vector2 installedScroll;
        private Vector2 updateScroll;
        private Vector2 detailScroll;
        private IReadOnlyList<NuGetSearchPackage> results = Array.Empty<NuGetSearchPackage>();
        private IReadOnlyList<NuGetPackageUpdate> updates = Array.Empty<NuGetPackageUpdate>();
        private NuGetSearchPackage selectedPackage;
        private IReadOnlyList<string> selectedVersions = Array.Empty<string>();
        private int selectedVersionIndex;

        public static void OpenBrowse()
        {
            Open(Page.Online, false);
        }

        public static void OpenUpdates(bool scanImmediately)
        {
            Open(Page.Updates, scanImmediately);
        }

        private static void Open(Page targetPage, bool scanUpdates)
        {
            ADKUnityNugetWindow window = GetWindow<ADKUnityNugetWindow>();
            window.titleContent = new GUIContent("ADK Unity Nuget");
            window.minSize = new Vector2(780f, 500f);
            window.page = targetPage;
            window.Show();

            if (scanUpdates)
            {
                window.RefreshUpdates();
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("ADK Unity Nuget");
            minSize = new Vector2(780f, 500f);

            bool firstLaunch = ADKUnityNugetSettings.EnsureInitialized();
            includePrerelease = ADKUnityNugetSettings.Current.includePrereleaseByDefault;
            status = $"Install location: {ADKUnityNugetSettings.Current.installRoot}";

            if (firstLaunch)
            {
                bool openSettings = EditorUtility.DisplayDialog(
                    "ADK Unity Nuget",
                    $"ADK Unity Nuget is ready.\n\nDefault package install location:\n{ADKUnityNugetSettings.Current.installRoot}\n\nThis setting is saved automatically per project and can be changed at any time.",
                    "Open Settings",
                    "Use Default");

                if (openSettings)
                {
                    SettingsService.OpenProjectSettings(ADKUnityNugetSettings.SettingsMenuPath);
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            switch (page)
            {
                case Page.Online:
                    DrawOnline();
                    break;
                case Page.Installed:
                    DrawInstalled();
                    break;
                case Page.Updates:
                    DrawUpdates();
                    break;
            }

            DrawFooter();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawTab(Page.Online, "Online");
                DrawTab(Page.Installed, "Installed");
                DrawTab(Page.Updates, "Updates");

                GUILayout.FlexibleSpace();

                bool prerelease = GUILayout.Toggle(includePrerelease, "Prerelease", EditorStyles.toolbarButton);
                if (prerelease != includePrerelease)
                {
                    includePrerelease = prerelease;
                }

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    SettingsService.OpenProjectSettings(ADKUnityNugetSettings.SettingsMenuPath);
                }
            }
        }

        private void DrawTab(Page targetPage, string label)
        {
            bool selected = page == targetPage;
            if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton) && !selected)
            {
                page = targetPage;
                GUI.FocusControl(null);
            }
        }

        private void DrawOnline()
        {
            DrawSearchBar();
            EditorGUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSearchResults();
                GUILayout.Space(6f);
                DrawPackageDetails();
            }
        }

        private void DrawSearchBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("ADKNugetSearch");
                onlineSearch = EditorGUILayout.TextField(onlineSearch, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField);

                using (new EditorGUI.DisabledScope(busy || string.IsNullOrWhiteSpace(onlineSearch)))
                {
                    if (GUILayout.Button("Search", GUILayout.Width(90f)))
                    {
                        Search();
                    }
                }

                if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                {
                    onlineSearch = string.Empty;
                    results = Array.Empty<NuGetSearchPackage>();
                    selectedPackage = null;
                    selectedVersions = Array.Empty<string>();
                    GUI.FocusControl("ADKNugetSearch");
                }
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter) &&
                !busy && !string.IsNullOrWhiteSpace(onlineSearch))
            {
                Search();
                currentEvent.Use();
            }
        }

        private void DrawSearchResults()
        {
            float width = Mathf.Clamp(position.width * 0.5f, 330f, 520f);
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(results.Count == 0 ? string.Empty : results.Count + " results", EditorStyles.miniLabel, GUILayout.Width(70f));
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    resultScroll = EditorGUILayout.BeginScrollView(resultScroll);

                    if (results.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Search NuGet.org by package ID, title, or keyword.", MessageType.Info);
                    }

                    foreach (NuGetSearchPackage package in results)
                    {
                        bool isSelected = selectedPackage == package;
                        using (new EditorGUILayout.VerticalScope(isSelected ? EditorStyles.helpBox : GUIStyle.none))
                        {
                            if (GUILayout.Button(package.id ?? "(unknown)", EditorStyles.linkLabel))
                            {
                                SelectPackage(package);
                            }

                            if (!string.IsNullOrEmpty(package.description))
                            {
                                EditorGUILayout.LabelField(package.description, EditorStyles.wordWrappedMiniLabel);
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Latest: " + (package.version ?? "-"), EditorStyles.miniLabel);
                                GUILayout.FlexibleSpace();
                                if (package.totalDownloads > 0)
                                {
                                    EditorGUILayout.LabelField(FormatDownloads(package.totalDownloads) + " downloads", EditorStyles.miniLabel, GUILayout.Width(100f));
                                }
                            }
                        }

                        EditorGUILayout.Space(4f);
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawPackageDetails()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                EditorGUILayout.LabelField("Package Details", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true)))
                {
                    detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

                    if (selectedPackage == null)
                    {
                        EditorGUILayout.HelpBox("Select a package to inspect versions and install it.", MessageType.Info);
                        EditorGUILayout.EndScrollView();
                        return;
                    }

                    EditorGUILayout.LabelField(selectedPackage.id ?? string.Empty, EditorStyles.largeLabel);

                    if (selectedPackage.authors != null && selectedPackage.authors.Count > 0)
                    {
                        EditorGUILayout.LabelField("Authors", string.Join(", ", selectedPackage.authors));
                    }

                    if (selectedPackage.totalDownloads > 0)
                    {
                        EditorGUILayout.LabelField("Downloads", selectedPackage.totalDownloads.ToString("N0"));
                    }

                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField(selectedPackage.description ?? "No description provided.", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(10f);

                    if (selectedVersions.Count == 0)
                    {
                        EditorGUILayout.HelpBox(busy ? "Loading versions..." : "No versions loaded.", MessageType.None);
                        EditorGUILayout.EndScrollView();
                        return;
                    }

                    string[] versions = selectedVersions.ToArray();
                    selectedVersionIndex = Mathf.Clamp(selectedVersionIndex, 0, versions.Length - 1);
                    selectedVersionIndex = EditorGUILayout.Popup("Version", selectedVersionIndex, versions);

                    EditorGUILayout.LabelField("Install To", ADKUnityNugetSettings.Current.installRoot);
                    EditorGUILayout.Space(8f);

                    using (new EditorGUI.DisabledScope(busy))
                    {
                        if (GUILayout.Button("Install Selected Version", GUILayout.Height(32f)))
                        {
                            Install(selectedPackage.id, versions[selectedVersionIndex]);
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawInstalled()
        {
            ADKNuGetManifest manifest = NuGetPackageInstaller.LoadManifest();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Installed Packages", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                installedSearch = EditorGUILayout.TextField(installedSearch, GUILayout.Width(220f));
                if (GUILayout.Button("Restore", GUILayout.Width(80f)))
                {
                    Restore();
                }
            }

            EditorGUILayout.LabelField("Location: " + ADKUnityNugetSettings.Current.installRoot, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                installedScroll = EditorGUILayout.BeginScrollView(installedScroll);

                List<ADKNuGetInstalledPackage> visiblePackages = manifest.packages
                    .Where(package => string.IsNullOrWhiteSpace(installedSearch) ||
                                      package.id.IndexOf(installedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(item => item.isDependency)
                    .ThenBy(item => item.id, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (visiblePackages.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        manifest.packages.Count == 0
                            ? "No packages are tracked by ADK Unity Nuget in this project."
                            : "No installed packages match the filter.",
                        MessageType.Info);
                }

                foreach (ADKNuGetInstalledPackage package in visiblePackages)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField(package.id, EditorStyles.boldLabel);
                            string kind = package.isDependency ? "Dependency" : "Direct install";
                            EditorGUILayout.LabelField(package.version + "  •  " + kind, EditorStyles.miniLabel);
                        }

                        GUILayout.FlexibleSpace();
                        using (new EditorGUI.DisabledScope(busy))
                        {
                            if (GUILayout.Button("Remove", GUILayout.Width(80f), GUILayout.Height(26f)))
                            {
                                Remove(package);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawUpdates()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Package Updates", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(busy))
                {
                    if (GUILayout.Button("Scan for Updates", GUILayout.Width(130f)))
                    {
                        RefreshUpdates();
                    }
                }
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                updateScroll = EditorGUILayout.BeginScrollView(updateScroll);

                if (updates.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        busy ? "Checking installed packages..." : "No pending updates are currently listed. Select Scan for Updates to refresh.",
                        MessageType.Info);
                }

                foreach (NuGetPackageUpdate update in updates)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField(update.InstalledPackage.id, EditorStyles.boldLabel);
                            EditorGUILayout.LabelField(
                                update.InstalledPackage.version + "  →  " + update.AvailableVersion,
                                EditorStyles.miniLabel);
                        }

                        GUILayout.FlexibleSpace();
                        using (new EditorGUI.DisabledScope(busy))
                        {
                            if (GUILayout.Button("Update", GUILayout.Width(80f), GUILayout.Height(26f)))
                            {
                                Install(update.InstalledPackage.id, update.AvailableVersion, true);
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(5f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Unity NuGet 0.3.0", EditorStyles.miniLabel, GUILayout.Width(130f));
            }
        }

        private async void Search()
        {
            busy = true;
            status = "Searching NuGet.org...";
            Repaint();

            try
            {
                results = await NuGetV3Client.SearchAsync(onlineSearch.Trim(), includePrerelease);
                selectedPackage = null;
                selectedVersions = Array.Empty<string>();
                status = $"Found {results.Count} package(s).";
            }
            catch (Exception exception)
            {
                HandleException("Search failed.", exception);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private async void SelectPackage(NuGetSearchPackage package)
        {
            selectedPackage = package;
            selectedVersions = Array.Empty<string>();
            selectedVersionIndex = 0;
            detailScroll = Vector2.zero;
            busy = true;
            status = $"Loading versions for {package.id}...";
            Repaint();

            try
            {
                selectedVersions = await NuGetV3Client.GetVersionsAsync(package.id, includePrerelease);
                status = $"Loaded {selectedVersions.Count} version(s) for {package.id}.";
            }
            catch (Exception exception)
            {
                HandleException("Version lookup failed.", exception);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private async void Install(string packageId, string version, bool refreshUpdatesAfterInstall = false)
        {
            busy = true;
            status = $"Installing {packageId} {version}...";
            Repaint();

            try
            {
                await NuGetPackageInstaller.InstallAsync(packageId, version, includePrerelease, message =>
                {
                    status = message;
                    Repaint();
                });

                status = $"Installed {packageId} {version}.";
                if (refreshUpdatesAfterInstall)
                {
                    updates = await NuGetPackageInstaller.CheckForUpdatesAsync(includePrerelease);
                }
            }
            catch (Exception exception)
            {
                HandleException("Install failed.", exception);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private async void Restore()
        {
            busy = true;
            status = "Restoring tracked NuGet packages...";
            Repaint();

            try
            {
                await NuGetPackageInstaller.RestoreAsync(message =>
                {
                    status = message;
                    Repaint();
                });
                status = "Package restore completed.";
            }
            catch (Exception exception)
            {
                HandleException("Restore failed.", exception);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private async void RefreshUpdates()
        {
            if (busy)
            {
                return;
            }

            busy = true;
            status = "Checking installed package updates...";
            Repaint();

            try
            {
                updates = await NuGetPackageInstaller.CheckForUpdatesAsync(includePrerelease);
                status = updates.Count == 0 ? "All direct packages are up to date." : $"Found {updates.Count} available update(s).";
            }
            catch (Exception exception)
            {
                HandleException("Update check failed.", exception);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private void Remove(ADKNuGetInstalledPackage package)
        {
            if (!EditorUtility.DisplayDialog(
                    "Remove NuGet Package",
                    $"Remove {package.id} {package.version} from this project?",
                    "Remove",
                    "Cancel"))
            {
                return;
            }

            try
            {
                NuGetPackageInstaller.Uninstall(package.id, package.version);
                status = $"Removed {package.id} {package.version}.";
            }
            catch (Exception exception)
            {
                HandleException("Remove failed.", exception);
            }
        }

        private void HandleException(string failureStatus, Exception exception)
        {
            status = failureStatus;
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("ADK Unity Nuget", exception.Message, "OK");
        }

        private static string FormatDownloads(long downloads)
        {
            if (downloads >= 1000000000)
            {
                return (downloads / 1000000000d).ToString("0.#") + "B";
            }

            if (downloads >= 1000000)
            {
                return (downloads / 1000000d).ToString("0.#") + "M";
            }

            if (downloads >= 1000)
            {
                return (downloads / 1000d).ToString("0.#") + "K";
            }

            return downloads.ToString();
        }
    }
}
