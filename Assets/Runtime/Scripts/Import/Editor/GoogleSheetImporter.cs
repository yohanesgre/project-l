using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime
{
    /// <summary>
    /// Editor window for importing dialogue data from Google Sheets.
    /// Supports multi-tab import via Google Sheets API with tab auto-detection.
    /// Accessible via: Tools > Import > Import Dialogues
    /// </summary>
    public class GoogleSheetImporter : EditorWindow
    {
        private const string OutputPath = "Data/Generated/Dialogues";
        private const string SheetsApiBaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";

        // Cached settings
        private GoogleSheetsSettings _settings;
        private string _spreadsheetId = "";
        private string _databaseName = "DialogueDatabase";

        // Tab management
        private List<SheetTabInfo> _tabs = new List<SheetTabInfo>();
        private bool _tabsFetched = false;

        // State
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;
        private bool _isLoading = false;
        private bool _showApiKeyField = false;

        // Preview data
        private List<DialogueEntry> _previewEntries;
        private Vector2 _scrollPosition;
        private Vector2 _tabScrollPosition;

        // Web request state
        private UnityWebRequest _activeRequest;
        private Action<string> _requestCallback;
        private Action<string> _requestErrorCallback;

        // Existing database reference
        private DialogueDatabase _existingDatabase;

        [MenuItem("Tools/Import/Import Dialogues")]
        public static void ShowWindow()
        {
            var window = GetWindow<GoogleSheetImporter>("Import Dialogues");
            window.minSize = new Vector2(550, 500);
        }

        private void OnEnable()
        {
            _settings = GoogleSheetsSettings.GetOrCreate();

            // Restore last used values
            if (_settings != null)
            {
                _spreadsheetId = ExtractSpreadsheetId(_settings.LastSpreadsheetURL);
                _databaseName = _settings.LastDatabaseName;
            }
        }

        private void OnDisable()
        {
            // Cleanup any pending requests
            if (_activeRequest != null)
            {
                _activeRequest.Dispose();
                _activeRequest = null;
            }

            EditorApplication.update -= ProcessWebRequest;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Google Sheet Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawApiKeySection();
            EditorGUILayout.Space(10);

            DrawSpreadsheetInput();
            EditorGUILayout.Space(5);

            if (_tabsFetched && _tabs.Count > 0)
            {
                DrawTabSelection();
                EditorGUILayout.Space(5);
            }

            DrawDatabaseSettings();
            EditorGUILayout.Space(10);

            DrawActionButtons();
            EditorGUILayout.Space(10);

            DrawStatusMessage();

            if (_previewEntries != null && _previewEntries.Count > 0)
            {
                DrawPreviewTable();
            }
        }

        #region UI Drawing Methods

        private void DrawApiKeySection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("API Key Status:", EditorStyles.boldLabel, GUILayout.Width(100));

            if (_settings != null && _settings.HasValidApiKey)
            {
                EditorGUILayout.LabelField("✓ Configured", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("⚠ Not configured", EditorStyles.miniLabel);
            }

            if (GUILayout.Button(_showApiKeyField ? "Hide" : "Edit", GUILayout.Width(50)))
            {
                _showApiKeyField = !_showApiKeyField;
            }

            EditorGUILayout.EndHorizontal();

            if (_showApiKeyField && _settings != null)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                _settings.ApiKey = EditorGUILayout.TextField("API Key", _settings.ApiKey);
                if (EditorGUI.EndChangeCheck())
                {
                    _settings.Save();
                }

                EditorGUILayout.HelpBox(
                    "To get an API key:\n" +
                    "1. Go to Google Cloud Console\n" +
                    "2. Create a project and enable Google Sheets API\n" +
                    "3. Create an API key (no OAuth needed for public sheets)",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSpreadsheetInput()
        {
            EditorGUILayout.LabelField("Spreadsheet URL or ID:", EditorStyles.boldLabel);

            var url = _settings?.LastSpreadsheetURL ?? "";
            EditorGUI.BeginChangeCheck();
            url = EditorGUILayout.TextField(url);
            if (EditorGUI.EndChangeCheck())
            {
                if (_settings != null)
                {
                    _settings.LastSpreadsheetURL = url;
                    _settings.Save();
                }
                _spreadsheetId = ExtractSpreadsheetId(url);
                _tabsFetched = false;
                _tabs.Clear();
                _previewEntries = null;
            }

            if (!string.IsNullOrEmpty(url) && string.IsNullOrEmpty(_spreadsheetId))
            {
                EditorGUILayout.HelpBox("Could not extract spreadsheet ID from URL.", MessageType.Warning);
            }
            else if (!string.IsNullOrEmpty(_spreadsheetId))
            {
                EditorGUILayout.LabelField($"Spreadsheet ID: {_spreadsheetId}", EditorStyles.miniLabel);
            }
        }

        private void DrawTabSelection()
        {
            EditorGUILayout.LabelField("Select Tabs to Import:", EditorStyles.boldLabel);

            _tabScrollPosition = EditorGUILayout.BeginScrollView(_tabScrollPosition, GUILayout.Height(100));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Import", GUILayout.Width(50));
            GUILayout.Label("Tab Name", GUILayout.Width(150));
            GUILayout.Label("Sheet ID", GUILayout.Width(80));
            GUILayout.Label("Entries", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            foreach (var tab in _tabs)
            {
                EditorGUILayout.BeginHorizontal();
                tab.Selected = EditorGUILayout.Toggle(tab.Selected, GUILayout.Width(50));
                GUILayout.Label(tab.Title, GUILayout.Width(150));
                GUILayout.Label(tab.SheetId.ToString(), GUILayout.Width(80));
                GUILayout.Label(tab.EntryCount > 0 ? tab.EntryCount.ToString() : "-", GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Select all / none buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                foreach (var tab in _tabs) tab.Selected = true;
            }
            if (GUILayout.Button("Select None", GUILayout.Width(80)))
            {
                foreach (var tab in _tabs) tab.Selected = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDatabaseSettings()
        {
            EditorGUILayout.LabelField("Database Settings:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _databaseName = EditorGUILayout.TextField("Database Name", _databaseName);
            if (EditorGUI.EndChangeCheck() && _settings != null)
            {
                _settings.LastDatabaseName = _databaseName;
                _settings.Save();
            }

            _existingDatabase = (DialogueDatabase)EditorGUILayout.ObjectField(
                "Update Existing", _existingDatabase, typeof(DialogueDatabase), false);
        }

        private void DrawActionButtons()
        {
            EditorGUI.BeginDisabledGroup(_isLoading);

            EditorGUILayout.BeginHorizontal();

            // Fetch Tabs button
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_spreadsheetId) || !HasValidApiKey());
            if (GUILayout.Button("Fetch Tabs", GUILayout.Height(30)))
            {
                FetchSheetTabs();
            }
            EditorGUI.EndDisabledGroup();

            // Fetch & Preview button
            EditorGUI.BeginDisabledGroup(!CanFetchData());
            if (GUILayout.Button("Fetch & Preview", GUILayout.Height(30)))
            {
                FetchSelectedTabsData();
            }
            EditorGUI.EndDisabledGroup();

            // Import button
            EditorGUI.BeginDisabledGroup(_previewEntries == null || _previewEntries.Count == 0);
            if (GUILayout.Button("Import", GUILayout.Height(30)))
            {
                ImportToScriptableObject();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            if (_isLoading)
            {
                EditorGUILayout.HelpBox("Loading...", MessageType.None);
            }

            if (!HasValidApiKey())
            {
                EditorGUILayout.HelpBox("Configure your API key to fetch tabs automatically.", MessageType.Warning);
            }
        }

        private void DrawStatusMessage()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void DrawPreviewTable()
        {
            EditorGUILayout.LabelField($"Preview ({_previewEntries.Count} entries):", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Tab", GUILayout.Width(60));
            GUILayout.Label("TextID", GUILayout.Width(80));
            GUILayout.Label("Scene", GUILayout.Width(80));
            GUILayout.Label("Order", GUILayout.Width(40));
            GUILayout.Label("Speaker", GUILayout.Width(80));
            GUILayout.Label("Dialogue", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            // Rows
            foreach (var entry in _previewEntries)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(entry.SourceTab ?? "", GUILayout.Width(60));
                GUILayout.Label(entry.TextID ?? "", GUILayout.Width(80));
                GUILayout.Label(entry.SceneID ?? "", GUILayout.Width(80));
                GUILayout.Label(entry.Order.ToString(), GUILayout.Width(40));
                GUILayout.Label(entry.Speaker ?? "", GUILayout.Width(80));

                var dialoguePreview = entry.DialogueText ?? "";
                if (dialoguePreview.Length > 40)
                {
                    dialoguePreview = dialoguePreview.Substring(0, 37) + "...";
                }
                GUILayout.Label(dialoguePreview, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region API Methods

        private void FetchSheetTabs()
        {
            if (!HasValidApiKey())
            {
                SetStatus("API key not configured.", MessageType.Error);
                return;
            }

            _isLoading = true;
            _tabsFetched = false;
            _tabs.Clear();
            SetStatus("Fetching sheet tabs...", MessageType.None);

            var url = $"{SheetsApiBaseUrl}/{_spreadsheetId}?key={_settings.ApiKey}&fields=sheets.properties";

            StartWebRequest(url,
                OnTabsFetched,
                error => SetStatus($"Failed to fetch tabs: {error}", MessageType.Error));
        }

        private void OnTabsFetched(string json)
        {
            _isLoading = false;

            try
            {
                // Parse the JSON response to extract sheet info
                // Format: {"sheets":[{"properties":{"sheetId":0,"title":"Sheet1","index":0}},...]}
                var sheets = ParseSheetsFromJson(json);

                _tabs.Clear();
                foreach (var sheet in sheets)
                {
                    _tabs.Add(new SheetTabInfo
                    {
                        Title = sheet.title,
                        SheetId = sheet.sheetId,
                        Selected = true,
                        EntryCount = 0
                    });
                }

                _tabsFetched = true;
                SetStatus($"Found {_tabs.Count} tabs. Select which to import.", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"Failed to parse tabs: {e.Message}", MessageType.Error);
            }

            Repaint();
        }

        private void FetchSelectedTabsData()
        {
            var selectedTabs = _tabs.FindAll(t => t.Selected);
            if (selectedTabs.Count == 0)
            {
                SetStatus("No tabs selected.", MessageType.Warning);
                return;
            }

            _isLoading = true;
            _previewEntries = new List<DialogueEntry>();
            SetStatus($"Fetching data from {selectedTabs.Count} tabs...", MessageType.None);

            FetchNextTab(selectedTabs, 0);
        }

        private void FetchNextTab(List<SheetTabInfo> tabs, int index)
        {
            if (index >= tabs.Count)
            {
                // All tabs fetched
                _isLoading = false;
                SetStatus($"Fetched {_previewEntries.Count} entries from {tabs.Count} tabs. Ready to import.", MessageType.Info);
                Repaint();
                return;
            }

            var tab = tabs[index];
            var csvUrl = tab.GetExportURL(_spreadsheetId);

            StartWebRequest(csvUrl,
                csv =>
                {
                    try
                    {
                        var entries = CSVParser.Parse(csv);

                        // Set SourceTab for each entry
                        foreach (var entry in entries)
                        {
                            entry.SourceTab = tab.Title;
                        }

                        tab.EntryCount = entries.Count;
                        _previewEntries.AddRange(entries);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[GoogleSheetImporter] Failed to parse tab '{tab.Title}': {e.Message}");
                    }

                    // Fetch next tab
                    FetchNextTab(tabs, index + 1);
                },
                error =>
                {
                    Debug.LogWarning($"[GoogleSheetImporter] Failed to fetch tab '{tab.Title}': {error}");
                    // Continue with next tab
                    FetchNextTab(tabs, index + 1);
                });
        }

        private void ImportToScriptableObject()
        {
            if (_previewEntries == null || _previewEntries.Count == 0)
            {
                SetStatus("No data to import. Fetch data first.", MessageType.Warning);
                return;
            }

            // Ensure output directory exists
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            DialogueDatabase database;

            if (_existingDatabase != null)
            {
                database = _existingDatabase;
            }
            else
            {
                database = ScriptableObject.CreateInstance<DialogueDatabase>();
                var assetPath = $"{OutputPath}/{_databaseName}.asset";

                if (File.Exists(assetPath))
                {
                    if (!EditorUtility.DisplayDialog("File Exists",
                        $"A database named '{_databaseName}' already exists. Overwrite?",
                        "Overwrite", "Cancel"))
                    {
                        return;
                    }
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(database, assetPath);
            }

            // Update database fields
            database.SourceSheetName = _databaseName;
            database.SourceURL = _settings?.LastSpreadsheetURL ?? "";
            database.ImportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            database.Entries = new List<DialogueEntry>(_previewEntries);

            // Store source tabs info
            database.SourceTabs = new List<string>();
            foreach (var tab in _tabs)
            {
                if (tab.Selected)
                {
                    database.SourceTabs.Add(tab.Title);
                }
            }

            // Validate and log any issues
            var errors = database.Validate();
            if (errors.Count > 0)
            {
                Debug.LogWarning($"[GoogleSheetImporter] Validation found {errors.Count} issues:");
                foreach (var error in errors)
                {
                    Debug.LogWarning($"  - {error}");
                }
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetStatus($"Imported {_previewEntries.Count} entries from {database.SourceTabs.Count} tabs.", MessageType.Info);

            EditorGUIUtility.PingObject(database);
            Selection.activeObject = database;
        }

        #endregion

        #region Helper Methods

        private bool HasValidApiKey()
        {
            return _settings != null && _settings.HasValidApiKey;
        }

        private bool CanFetchData()
        {
            if (string.IsNullOrEmpty(_spreadsheetId)) return false;

            // If tabs are fetched, need at least one selected
            if (_tabsFetched && _tabs.Count > 0)
            {
                return _tabs.Exists(t => t.Selected);
            }

            // If no API key, can still fetch via published CSV URL
            return true;
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            _isLoading = type == MessageType.None && message.Contains("...");
        }

        /// <summary>
        /// Extracts the spreadsheet ID from a Google Sheets URL.
        /// Supports formats like:
        /// - https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit
        /// - https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/export?format=csv
        /// - Or just the raw ID
        /// </summary>
        private static string ExtractSpreadsheetId(string urlOrId)
        {
            if (string.IsNullOrEmpty(urlOrId)) return "";

            // If it's already just an ID (no slashes), return as-is
            if (!urlOrId.Contains("/"))
            {
                return urlOrId.Trim();
            }

            // Extract from URL pattern: /d/SPREADSHEET_ID/
            var match = Regex.Match(urlOrId, @"/d/([a-zA-Z0-9-_]+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return "";
        }

        /// <summary>
        /// Parses the Google Sheets API response to extract sheet metadata.
        /// </summary>
        private static List<(string title, int sheetId)> ParseSheetsFromJson(string json)
        {
            var result = new List<(string title, int sheetId)>();

            // Simple JSON parsing for the specific format we expect
            // {"sheets":[{"properties":{"sheetId":0,"title":"Sheet1"}},...]}

            // Find all sheet properties
            var sheetsPattern = @"""properties""\s*:\s*\{[^}]*""sheetId""\s*:\s*(\d+)[^}]*""title""\s*:\s*""([^""]+)""";
            var altPattern = @"""properties""\s*:\s*\{[^}]*""title""\s*:\s*""([^""]+)""[^}]*""sheetId""\s*:\s*(\d+)";

            foreach (Match match in Regex.Matches(json, sheetsPattern))
            {
                if (int.TryParse(match.Groups[1].Value, out int sheetId))
                {
                    result.Add((match.Groups[2].Value, sheetId));
                }
            }

            // Try alternate order if no matches
            if (result.Count == 0)
            {
                foreach (Match match in Regex.Matches(json, altPattern))
                {
                    if (int.TryParse(match.Groups[2].Value, out int sheetId))
                    {
                        result.Add((match.Groups[1].Value, sheetId));
                    }
                }
            }

            return result;
        }

        private void StartWebRequest(string url, Action<string> onSuccess, Action<string> onError)
        {
            if (_activeRequest != null)
            {
                _activeRequest.Dispose();
                EditorApplication.update -= ProcessWebRequest;
            }

            _activeRequest = UnityWebRequest.Get(url);
            _requestCallback = onSuccess;
            _requestErrorCallback = onError;

            _activeRequest.SendWebRequest();
            EditorApplication.update += ProcessWebRequest;
        }

        private void ProcessWebRequest()
        {
            if (_activeRequest == null || !_activeRequest.isDone) return;

            EditorApplication.update -= ProcessWebRequest;

            if (_activeRequest.result == UnityWebRequest.Result.Success)
            {
                var text = _activeRequest.downloadHandler.text;
                _activeRequest.Dispose();
                _activeRequest = null;
                _requestCallback?.Invoke(text);
            }
            else
            {
                var error = _activeRequest.error;
                _activeRequest.Dispose();
                _activeRequest = null;
                _isLoading = false;
                _requestErrorCallback?.Invoke(error);
            }

            Repaint();
        }

        #endregion
    }
}
