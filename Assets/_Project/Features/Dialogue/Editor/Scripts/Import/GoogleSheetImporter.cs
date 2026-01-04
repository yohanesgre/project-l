using System;
using System.Collections.Generic;
using System.IO;
using MyGame.Features.Dialogue.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MyGame.Features.Dialogue.Editor.Import
{
    /// <summary>
    /// Editor window for importing dialogue data from Google Sheets.
    /// Accessible via: Tools > Import > Import Dialogues
    /// </summary>
    public class GoogleSheetImporter : EditorWindow
    {
        private const string OutputPath = "Data/Generated/Dialogues";
        private const string PrefsKeyURL = "Import_LastSheetURL";
        private const string PrefsKeyName = "Import_LastSheetName";

        private string _sheetURL = "";
        private string _sheetName = "DialogueDatabase";
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;
        private bool _isLoading = false;
        private List<DialogueEntry> _previewEntries;
        private Vector2 _scrollPosition;
        private DialogueDatabase _existingDatabase;

        [MenuItem("Tools/Import/Import Dialogues")]
        public static void ShowWindow()
        {
            var window = GetWindow<GoogleSheetImporter>("Import Dialogues");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            _sheetURL = EditorPrefs.GetString(PrefsKeyURL, "");
            _sheetName = EditorPrefs.GetString(PrefsKeyName, "DialogueDatabase");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Google Sheet Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawInstructions();
            EditorGUILayout.Space(10);

            DrawURLInput();
            EditorGUILayout.Space(5);

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

        private void DrawInstructions()
        {
            EditorGUILayout.HelpBox(
                "1. Open your Google Sheet\n" +
                "2. Go to File > Share > Publish to web\n" +
                "3. Select the sheet tab, choose 'CSV' format\n" +
                "4. Click Publish and copy the URL\n" +
                "5. Paste the URL below and click 'Fetch & Preview'",
                MessageType.Info);
        }

        private void DrawURLInput()
        {
            EditorGUILayout.LabelField("Published CSV URL:", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _sheetURL = EditorGUILayout.TextField(_sheetURL);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PrefsKeyURL, _sheetURL);
            }

            // Validate URL format
            if (!string.IsNullOrEmpty(_sheetURL) && !_sheetURL.Contains("docs.google.com"))
            {
                EditorGUILayout.HelpBox("URL should be from docs.google.com", MessageType.Warning);
            }
        }

        private void DrawDatabaseSettings()
        {
            EditorGUILayout.LabelField("Database Settings:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _sheetName = EditorGUILayout.TextField("Database Name", _sheetName);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PrefsKeyName, _sheetName);
            }

            // Existing database reference
            _existingDatabase = (DialogueDatabase)EditorGUILayout.ObjectField(
                "Update Existing", _existingDatabase, typeof(DialogueDatabase), false);
        }

        private void DrawActionButtons()
        {
            EditorGUI.BeginDisabledGroup(_isLoading || string.IsNullOrEmpty(_sheetURL));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Fetch & Preview", GUILayout.Height(30)))
            {
                FetchAndPreview();
            }

            EditorGUI.BeginDisabledGroup(_previewEntries == null || _previewEntries.Count == 0);
            if (GUILayout.Button("Import to ScriptableObject", GUILayout.Height(30)))
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
                GUILayout.Label(entry.TextID ?? "", GUILayout.Width(80));
                GUILayout.Label(entry.SceneID ?? "", GUILayout.Width(80));
                GUILayout.Label(entry.Order.ToString(), GUILayout.Width(40));
                GUILayout.Label(entry.Speaker ?? "", GUILayout.Width(80));
                
                var dialoguePreview = entry.DialogueText ?? "";
                if (dialoguePreview.Length > 50)
                {
                    dialoguePreview = dialoguePreview.Substring(0, 47) + "...";
                }
                GUILayout.Label(dialoguePreview, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void FetchAndPreview()
        {
            _isLoading = true;
            _statusMessage = "Fetching data from Google Sheets...";
            _statusType = MessageType.None;
            _previewEntries = null;

            EditorApplication.update += FetchUpdate;
            _fetchRequest = UnityWebRequest.Get(_sheetURL);
            _fetchRequest.SendWebRequest();
        }

        private UnityWebRequest _fetchRequest;

        private void FetchUpdate()
        {
            if (_fetchRequest == null || !_fetchRequest.isDone) return;

            EditorApplication.update -= FetchUpdate;
            _isLoading = false;

            if (_fetchRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var csvContent = _fetchRequest.downloadHandler.text;
                    _previewEntries = CSVParser.Parse(csvContent);

                    if (_previewEntries.Count > 0)
                    {
                        _statusMessage = $"Successfully parsed {_previewEntries.Count} entries. Ready to import.";
                        _statusType = MessageType.Info;
                    }
                    else
                    {
                        _statusMessage = "No entries found. Check your spreadsheet format.";
                        _statusType = MessageType.Warning;
                    }
                }
                catch (Exception e)
                {
                    _statusMessage = $"Parse error: {e.Message}";
                    _statusType = MessageType.Error;
                }
            }
            else
            {
                _statusMessage = $"Fetch failed: {_fetchRequest.error}";
                _statusType = MessageType.Error;
            }

            _fetchRequest.Dispose();
            _fetchRequest = null;
            Repaint();
        }

        private void ImportToScriptableObject()
        {
            if (_previewEntries == null || _previewEntries.Count == 0)
            {
                _statusMessage = "No data to import. Fetch data first.";
                _statusType = MessageType.Warning;
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
                // Update existing database
                database = _existingDatabase;
            }
            else
            {
                // Create new database
                database = ScriptableObject.CreateInstance<DialogueDatabase>();
                var assetPath = $"{OutputPath}/{_sheetName}.asset";
                
                // Check for existing file
                if (File.Exists(assetPath))
                {
                    if (!EditorUtility.DisplayDialog("File Exists",
                        $"A database named '{_sheetName}' already exists. Overwrite?",
                        "Overwrite", "Cancel"))
                    {
                        return;
                    }
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(database, assetPath);
            }

            // Update database fields
            database.SourceSheetName = _sheetName;
            database.SourceURL = _sheetURL;
            database.ImportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            database.Entries = new List<DialogueEntry>(_previewEntries);

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

            _statusMessage = $"Successfully imported {_previewEntries.Count} entries to {(_existingDatabase != null ? "existing" : "new")} database.";
            _statusType = MessageType.Info;

            // Ping the created asset
            EditorGUIUtility.PingObject(database);
            Selection.activeObject = database;
        }
    }
}
