using UnityEditor;
using UnityEngine;
using System.IO;

namespace TMG_EditorTools
{

    public class BuildLogWindow : EditorWindow
    {
        private Vector2 scrollPos;
        public static bool openLastLogOnBuild = true;
        public static bool enableUsedAssetReport = true; // New toggle variable
        public static bool enableUnusedAssetReport = true; // New toggle variable

        private static string settingsFilePath;

        [MenuItem("Tools/TMG_EditorTools/Build Log")]
        public static void ShowWindow()
        {
            GetWindow<BuildLogWindow>("Build Log");
        }

        private void OnEnable()
        {
            settingsFilePath = Path.Combine(Application.dataPath, "BuildLogSettings.json");
            LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnGUI()
        {
            GUILayout.Label("Build Details & Asset Log", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Last Log In Editor"))
            {
                BuildLogger.SetLog(BuildPostProcessor.LoadBuildLogFromProjectFolder());
            }
            if (GUILayout.Button("Save Log To File"))
            {
                SaveLogToFile();
            }
            if (GUILayout.Button("Load Log From File In Editor"))
            {
                LoadLogFromFile();
            }
            if (GUILayout.Button("Open Last Log File In Txt Editor"))
            {
                OpenLastLogFile();
            }
            if (GUILayout.Button("Clear Log"))
            {
                BuildLogger.SetLog("");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Open Last Log: Notepad/Editor");
            openLastLogOnBuild = EditorGUILayout.Toggle(openLastLogOnBuild);
            GUILayout.Label("Enable Used Asset Report");
            enableUsedAssetReport = EditorGUILayout.Toggle(enableUsedAssetReport);
            GUILayout.Label("Enable Unused Asset Report");
            enableUnusedAssetReport = EditorGUILayout.Toggle(enableUnusedAssetReport);
            EditorGUILayout.EndHorizontal();

            if (openLastLogOnBuild == false)
            {

                GUIStyle warningStyle = new GUIStyle(EditorStyles.label);
                warningStyle.normal.textColor = Color.red;
                GUILayout.Label("WARNING: Opening large build files in editor may cause Unity to LAG or CRASH", warningStyle);
            }

            string currentLog = BuildLogger.GetBuildLog();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            EditorGUILayout.TextArea(currentLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        public static void OpenLastLogFile()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string logFilePath = Path.Combine(projectRoot, "BuildLog.txt");

            if (File.Exists(logFilePath))
            {
                System.Diagnostics.Process.Start(logFilePath);
            }
            else
            {
                Debug.LogWarning("Build log file not found at: " + logFilePath);
            }
        }

        private void SaveSettings()
        {
            BuildLogSettings settings = new BuildLogSettings
            {
                openLastLogOnBuild = openLastLogOnBuild,
                enableUsedAssetReport = enableUsedAssetReport,
                enableUnusedAssetReport = enableUnusedAssetReport
            };

            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(settingsFilePath, json);
            Debug.Log("Build log settings saved.");
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                BuildLogSettings settings = JsonUtility.FromJson<BuildLogSettings>(json);

                openLastLogOnBuild = settings.openLastLogOnBuild;
                enableUsedAssetReport = settings.enableUsedAssetReport;
                enableUnusedAssetReport = settings.enableUnusedAssetReport;

                Debug.Log("Build log settings loaded.");
            }
            else
            {
                Debug.Log("No saved settings found. Using defaults.");
            }
        }

        private void SaveLogToFile()
        {
            string path = EditorUtility.SaveFilePanel("Save Build Log", "", "BuildLog.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, BuildLogger.GetBuildLog());
                Debug.Log("Build log saved to: " + path);
            }
        }

        private void LoadLogFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Load Build Log", "", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                string loadedLog = File.ReadAllText(path);
                BuildLogger.SetLog(loadedLog);
                Debug.Log("Build log loaded from: " + path);
            }
        }
    }

  
  

}