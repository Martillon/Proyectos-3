using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TMG_EditorTools
{
    public class ScriptContentSearchTool : EditorWindow
    {
        // --- EditorPrefs Keys ---
        private const string PREF_FOLDER_PATH = "ScriptContentSearchTool.FolderPath";
        private const string PREF_SEARCH_STRING = "ScriptContentSearchTool.SearchString";
        private const string PREF_CASE_SENSITIVE = "ScriptContentSearchTool.CaseSensitive";
        private const string PREF_RECURSIVE_SEARCH = "ScriptContentSearchTool.RecursiveSearch";
        private const string PREF_SEARCH_RESULTS = "ScriptContentSearchTool.SearchResults"; // JSON-serialized results

        private string folderPath = "Assets";    // The folder path to search.
        private string searchString = "";        // The string to check for.
        private bool caseSensitive = false;      // Toggle for case sensitivity
        private bool recursiveSearch = true;     // Toggle for recursive searching

        private Vector2 scrollPosition;

        // 1) Mark this class serializable so JsonUtility can serialize it.
        [Serializable]
        private class SearchResult
        {
            public string filePath;
            public string message;

            public SearchResult(string filePath, string message)
            {
                this.filePath = filePath;
                this.message = message;
            }
        }

        // 2) We need a wrapper class for JSON serialization
        [Serializable]
        private class SearchResultsWrapper
        {
            public List<SearchResult> searchResults;
        }

        private List<SearchResult> searchResults = new List<SearchResult>();

        [MenuItem("Tools/TMG_EditorTools/Script Content Search Tool")]
        public static void ShowWindow()
        {
            GetWindow<ScriptContentSearchTool>("Script Search Tool");
        }

        private void OnEnable()
        {
            // Load stored preferences
            folderPath = EditorPrefs.GetString(PREF_FOLDER_PATH, "Assets");
            searchString = EditorPrefs.GetString(PREF_SEARCH_STRING, "");
            caseSensitive = EditorPrefs.GetBool(PREF_CASE_SENSITIVE, false);
            recursiveSearch = EditorPrefs.GetBool(PREF_RECURSIVE_SEARCH, true);

            // 3) Re-load the results list from EditorPrefs (JSON)
            string json = EditorPrefs.GetString(PREF_SEARCH_RESULTS, "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<SearchResultsWrapper>(json);
                if (wrapper != null && wrapper.searchResults != null)
                {
                    searchResults = wrapper.searchResults;
                }
            }
        }

        private void OnDisable()
        {
            // Save current preferences
            EditorPrefs.SetString(PREF_FOLDER_PATH, folderPath);
            EditorPrefs.SetString(PREF_SEARCH_STRING, searchString);
            EditorPrefs.SetBool(PREF_CASE_SENSITIVE, caseSensitive);
            EditorPrefs.SetBool(PREF_RECURSIVE_SEARCH, recursiveSearch);

            // 4) Serialize the searchResults list as JSON and save it in EditorPrefs
            var wrapper = new SearchResultsWrapper { searchResults = searchResults };
            string json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(PREF_SEARCH_RESULTS, json);
        }

        private void OnGUI()
        {
            GUILayout.Label("Script Content Search Tool", EditorStyles.boldLabel);

            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
            searchString = EditorGUILayout.TextField("Search String", searchString);
            caseSensitive = EditorGUILayout.Toggle("Case Sensitive", caseSensitive);
            recursiveSearch = EditorGUILayout.Toggle("Recursive Search", recursiveSearch);

            if (GUILayout.Button("Search"))
            {
                SearchInFolder(folderPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Search Results:");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Display search results
            for (int i = 0; i < searchResults.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                // Show the message in a TextField
                EditorGUILayout.TextField(searchResults[i].message);

                if (GUILayout.Button("Find", GUILayout.Width(50)))
                {
                    // Attempt to Ping the file in the Project view
                    if (!string.IsNullOrEmpty(searchResults[i].filePath))
                    {
                        EditorGUIUtility.PingObject(
                            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(searchResults[i].filePath)
                        );
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void SearchInFolder(string currentFolder)
        {
            // Clear previous search results
            searchResults.Clear();

            StringComparison comparisonType = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            SearchOption searchOption = recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (Directory.Exists(currentFolder))
            {
                string[] files = Directory.GetFiles(currentFolder, "*.cs", searchOption);

                foreach (string file in files)
                {
                    string fileContent = File.ReadAllText(file);
                    if (fileContent.IndexOf(searchString, comparisonType) >= 0)
                    {
                        // Build a user-friendly message
                        string message = "Found matching content in file: " + file;
                        searchResults.Add(new SearchResult(file, message));
                        Debug.Log(message);
                    }
                }
            }
            else
            {
                string message = "Folder does not exist: " + currentFolder;
                searchResults.Add(new SearchResult("", message));
                Debug.LogWarning(message);
            }
        }
    }
}
