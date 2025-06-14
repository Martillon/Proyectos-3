using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TMG_EditorTools
{
    public class ScriptContentReplaceTool : EditorWindow
    {
        // Keys for storing/loading data from EditorPrefs
        private const string PREF_FOLDER_PATH = "ScriptContentReplaceTool.FolderPath";
        private const string PREF_CASE_SENSITIVE = "ScriptContentReplaceTool.CaseSensitive";
        private const string PREF_RECURSIVE_SEARCH = "ScriptContentReplaceTool.RecursiveSearch";
        private const string PREF_ONLY_CS_FILES = "ScriptContentReplaceTool.OnlyCsFiles";

        // **Add a new key for storing the JSON-serialized replaceResults:**
        private const string PREF_REPLACE_RESULTS = "ScriptContentReplaceTool.ReplaceResults";

        private string folderPath = "Assets";
        private string searchString = "";
        private string replaceString = "";

        private bool caseSensitive = false;
        private bool recursiveSearch = true;
        private bool onlyCsFiles = true; // Toggle for only .cs files

        private Vector2 scrollPosition;

        // 1) Mark this class serializable so JsonUtility can serialize it.
        [Serializable]
        private class ReplaceResult
        {
            public string filePath;
            public string message;

            public ReplaceResult(string filePath, string message)
            {
                this.filePath = filePath;
                this.message = message;
            }
        }

        // 2) We need a wrapper class for JSON serialization
        [Serializable]
        private class ReplaceResultsWrapper
        {
            public List<ReplaceResult> replaceResults;
        }

        private List<ReplaceResult> replaceResults = new List<ReplaceResult>();

        [MenuItem("Tools/TMG_EditorTools/Script Content Replace Tool")]
        public static void ShowWindow()
        {
            GetWindow<ScriptContentReplaceTool>("Script Replace Tool");
        }

        /// <summary>
        /// Called when the window is created or opened.
        /// Load our saved data (if any) from EditorPrefs.
        /// </summary>
        private void OnEnable()
        {
            folderPath = EditorPrefs.GetString(PREF_FOLDER_PATH, "Assets");
            caseSensitive = EditorPrefs.GetBool(PREF_CASE_SENSITIVE, false);
            recursiveSearch = EditorPrefs.GetBool(PREF_RECURSIVE_SEARCH, true);
            onlyCsFiles = EditorPrefs.GetBool(PREF_ONLY_CS_FILES, true);

            // 3) Re-load the results list from EditorPrefs (JSON)
            string json = EditorPrefs.GetString(PREF_REPLACE_RESULTS, "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<ReplaceResultsWrapper>(json);
                if (wrapper != null && wrapper.replaceResults != null)
                {
                    replaceResults = wrapper.replaceResults;
                }
            }
        }

        /// <summary>
        /// Called when the window is closed or the editor exits.
        /// We save our current data to EditorPrefs.
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetString(PREF_FOLDER_PATH, folderPath);
            EditorPrefs.SetBool(PREF_CASE_SENSITIVE, caseSensitive);
            EditorPrefs.SetBool(PREF_RECURSIVE_SEARCH, recursiveSearch);
            EditorPrefs.SetBool(PREF_ONLY_CS_FILES, onlyCsFiles);

            // 4) Serialize the replaceResults list as JSON and save it in EditorPrefs
            var wrapper = new ReplaceResultsWrapper { replaceResults = replaceResults };
            string json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(PREF_REPLACE_RESULTS, json);
        }

        private void OnGUI()
        {
            GUILayout.Label("Script Content Replace Tool", EditorStyles.boldLabel);

            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
            searchString = EditorGUILayout.TextField("Search String", searchString);
            replaceString = EditorGUILayout.TextField("Replace With", replaceString);

            caseSensitive = EditorGUILayout.Toggle("Case Sensitive", caseSensitive);
            recursiveSearch = EditorGUILayout.Toggle("Recursive Search", recursiveSearch);
            onlyCsFiles = EditorGUILayout.Toggle("Only Edit .cs Files", onlyCsFiles);

            EditorGUILayout.Space();

            if (GUILayout.Button("Search & Replace"))
            {
                ReplaceInFolder(folderPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Replace Results:");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < replaceResults.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(replaceResults[i].message);

                if (GUILayout.Button("Find", GUILayout.Width(50)))
                {
                    if (!string.IsNullOrEmpty(replaceResults[i].filePath))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(replaceResults[i].filePath);
                        if (obj != null)
                        {
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ReplaceInFolder(string currentFolder)
        {
            replaceResults.Clear(); // Clear previous results

            SearchOption searchOption = recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (!Directory.Exists(currentFolder))
            {
                string result = $"Folder does not exist: {currentFolder}";
                replaceResults.Add(new ReplaceResult("", result));
                Debug.LogWarning(result);
                return;
            }

            string[] patterns = onlyCsFiles ? new[] { "*.cs" } : new[] { "*.*" };

            StringComparison comparisonType = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            RegexOptions regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            string escapedSearch = Regex.Escape(searchString);

            foreach (string patternToSearch in patterns)
            {
                string[] files = Directory.GetFiles(currentFolder, patternToSearch, searchOption);

                foreach (string file in files)
                {
                    try
                    {
                        string fileContent = File.ReadAllText(file);

                        // Check if the content has the search string
                        if (fileContent.IndexOf(searchString, comparisonType) >= 0)
                        {
                            // Perform the replacement using Regex
                            string replacedContent = Regex.Replace(fileContent, escapedSearch, replaceString, regexOptions);

                            // If there's a difference, write the result back to file
                            if (!fileContent.Equals(replacedContent, comparisonType))
                            {
                                File.WriteAllText(file, replacedContent);

                                string message = $"Replaced in file: {file}";
                                replaceResults.Add(new ReplaceResult(file, message));
                                Debug.Log(message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        string error = $"Error processing file {file}: {e.Message}";
                        replaceResults.Add(new ReplaceResult(file, error));
                        Debug.LogError(error);
                    }
                }
            }

            // Refresh the AssetDatabase so Unity sees updated files
            AssetDatabase.Refresh();
        }
    }
}
