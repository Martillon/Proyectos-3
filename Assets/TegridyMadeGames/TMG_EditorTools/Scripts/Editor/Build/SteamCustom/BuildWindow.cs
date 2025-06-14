using System;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace TMG_EditorTools
{

    public class BuildWindow : EditorWindow
    {
        private string buildPath = "C:/Finished Builds";
        private bool isDemoBuild = false;
        private bool showRetailName = true;  // New field for toggling retail name visibility
        private bool showScenePaths = true;  // New field for toggling scene paths visibility

        // File paths for storing scene paths, game names, and app ids
        private string demoLevelsFilePath = "Assets/demoLevels.txt";
        private string retailLevelsFilePath = "Assets/retailLevels.txt";
        private string gameNamesFilePath = "Assets/gameNames.txt";
        private string appIdsFilePath = "Assets/appIds.txt";  // New file for storing app ids
        private string settingsFilePath = "Assets/buildWindowSettings.txt"; // New file for storing settings

        // Level paths for Demo and Retail
        private string[] demoLevels = new string[]
        {
        "Assets/!Game/Game.unity",
        };

        private string[] retailLevels = new string[]
        {
        "Assets/!Game/Game.unity",
        };

        // Game name, demo name, and appId fields
        private string gameName = "GameName"; // Default game name
        private string retailAppId = "480"; // Default app id for retail
        private string demoName = "GameNameDEMO"; // Default demo name
        private string demoAppId = "480";  // Default app id for demo

        // Scroll position for the scroll view
        private Vector2 scrollPosition;

        [MenuItem("Tools/TMG_EditorTools/Build Window Custom")]
        public static void ShowWindow()
        {
            GetWindow<BuildWindow>("Final Build");
        }

        private void OnEnable()
        {
            LoadScenePaths();
            LoadGameNames();
            LoadAppIds();  // Load app ids on enable
            LoadSettings();  // Load settings on enable
        }

        private void OnDisable()
        {
            SaveScenePaths();
            SaveGameNames();
            SaveAppIds();  // Save app ids on close
            SaveSettings();  // Save settings on close
        }

        private void SaveSettings()
        {
            using (StreamWriter writer = new StreamWriter(settingsFilePath))
            {
                writer.WriteLine(showRetailName);  // Save showRetailName state
                writer.WriteLine(isDemoBuild);     // Save isDemoBuild state
                writer.WriteLine(showScenePaths);  // Save showScenePaths state
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                string[] lines = File.ReadAllLines(settingsFilePath);
                if (lines.Length >= 3)
                {
                    showRetailName = bool.Parse(lines[0]);  // Load showRetailName state
                    isDemoBuild = bool.Parse(lines[1]);     // Load isDemoBuild state
                    showScenePaths = bool.Parse(lines[2]);  // Load showScenePaths state
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Final Build Options", EditorStyles.boldLabel);

            isDemoBuild = GUILayout.Toggle(isDemoBuild, "Demo Build");
            GUILayout.Space(5);
            GUILayout.Label("Build Location:", EditorStyles.label);

            GUILayout.BeginHorizontal();
            GUILayout.TextField(buildPath);
            if (GUILayout.Button("Select Folder"))
            {
                buildPath = EditorUtility.SaveFolderPanel("Choose Location of Built Game", buildPath, "");
            }
            GUILayout.EndHorizontal();


            GUILayout.Space(5);
            if (GUILayout.Button("Build Game"))
            {
                SaveScenePaths();
                SaveGameNames();
                SaveAppIds();

                if (isDemoBuild)
                {
                    BuildGameDemo();
                }
                else
                {
                    BuildGameRetail();
                }
            }

            GUILayout.Space(5);

            if (showRetailName)
            {
                // Input fields for game name and demo name
                GUILayout.Label("Retail Name:", EditorStyles.label);
                gameName = GUILayout.TextField(gameName);

                GUILayout.Label("Retail App ID:", EditorStyles.label);
                retailAppId = GUILayout.TextField(retailAppId);

                GUILayout.Label("Demo Name:", EditorStyles.label);
                demoName = GUILayout.TextField(demoName);

                // Input fields for app ids
                GUILayout.Label("Demo App ID:", EditorStyles.label);
                demoAppId = GUILayout.TextField(demoAppId);
            }

            GUILayout.Space(5);
            if (GUILayout.Button(showRetailName ? "Hide Game Names & IDs" : "Show Game Names & IDs"))
            {
                showRetailName = !showRetailName;
            }

            GUILayout.Space(5);


            // Button to toggle scene paths visibility
            if (GUILayout.Button(showScenePaths ? "Hide Scene Paths" : "Show Scene Paths"))
            {
                showScenePaths = !showScenePaths;
                SaveSettings(); // Save this toggle state
            }

            if (showScenePaths)
            {
                GUILayout.Label("Scene Paths:", EditorStyles.boldLabel);
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                if (isDemoBuild)
                {
                    DisplayLevelPaths(ref demoLevels, "Demo");
                }
                else
                {
                    DisplayLevelPaths(ref retailLevels, "Retail");
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(2);
        }

        private void DisplayLevelPaths(ref string[] levelPaths, string buildType)
        {
            for (int i = 0; i < levelPaths.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i.ToString(), GUILayout.Width(20));
                levelPaths[i] = GUILayout.TextField(levelPaths[i]);
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button($"Add {buildType} Level"))
            {
                Array.Resize(ref levelPaths, levelPaths.Length + 1);
                levelPaths[levelPaths.Length - 1] = "New Level Path";
            }

            if (levelPaths.Length > 0)
            {
                if (GUILayout.Button($"Remove Last {buildType} Level"))
                {
                    Array.Resize(ref levelPaths, levelPaths.Length - 1);
                }
            }
        }

        private void BuildGameDemo()
        {
            WriteStringDemo();
            BuildPipeline.BuildPlayer(demoLevels, Path.Combine(buildPath, $"{demoName}.exe"), BuildTarget.StandaloneWindows, BuildOptions.None);
        }

        private void BuildGameRetail()
        {
            WriteStringRetail();
            BuildPipeline.BuildPlayer(retailLevels, Path.Combine(buildPath, $"{gameName}.exe"), BuildTarget.StandaloneWindows, BuildOptions.None);
        }

        private void WriteStringDemo()
        {
            string filePath = "steam_appid.txt";
            File.Delete(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(demoAppId);  // Write demo app id
            }
        }

        private void WriteStringRetail()
        {
            string filePath = "steam_appid.txt";
            File.Delete(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(retailAppId);  // Write retail app id
            }
        }

        private void SaveScenePaths()
        {
            File.WriteAllLines(demoLevelsFilePath, demoLevels);
            File.WriteAllLines(retailLevelsFilePath, retailLevels);
        }

        private void LoadScenePaths()
        {
            if (File.Exists(demoLevelsFilePath))
            {
                demoLevels = File.ReadAllLines(demoLevelsFilePath);
            }

            if (File.Exists(retailLevelsFilePath))
            {
                retailLevels = File.ReadAllLines(retailLevelsFilePath);
            }
        }

        private void SaveGameNames()
        {
            using (StreamWriter writer = new StreamWriter(gameNamesFilePath))
            {
                writer.WriteLine(gameName);
                writer.WriteLine(demoName);
            }
        }

        private void LoadGameNames()
        {
            if (File.Exists(gameNamesFilePath))
            {
                string[] lines = File.ReadAllLines(gameNamesFilePath);
                if (lines.Length >= 2)
                {
                    gameName = lines[0];
                    demoName = lines[1];
                }
            }
        }

        private void SaveAppIds()
        {
            using (StreamWriter writer = new StreamWriter(appIdsFilePath))
            {
                writer.WriteLine(retailAppId);
                writer.WriteLine(demoAppId);
            }
        }

        private void LoadAppIds()
        {
            if (File.Exists(appIdsFilePath))
            {
                string[] lines = File.ReadAllLines(appIdsFilePath);
                if (lines.Length >= 2)
                {
                    retailAppId = lines[0];
                    demoAppId = lines[1];
                }
            }
        }
    }

}