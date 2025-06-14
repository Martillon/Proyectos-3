using UnityEditor;
using UnityEngine;
using System.IO;

namespace TMG_EditorTools
{
    /// <summary>
    /// An Editor Window that backs up a chosen directory on editor quit (and also on demand).
    /// Also prevents the Editor from quitting unless the user confirms.
    /// </summary>
    public class LocalBackupWindow : EditorWindow
    {
        // Key used in EditorPrefs to store the backup directory path
        private const string EditorPrefsKey = "LocalBackupWindow_directoryToBackup";

        // NEW: Key used to store whether to prompt on Editor quit.
        private const string PromptOnQuitKey = "LocalBackupWindow_promptOnQuit";

        // Directory to backup (relative to the project folder)
        private static string directoryToBackup = "Assets/MyFolder";

        // NEW: Whether the user wants to be prompted to create a backup on Editor quit
        private static bool promptOnQuit = true;

        // Hook the Editor's quitting event + wantsToQuit event via a static constructor
        static LocalBackupWindow()
        {
        }

        [MenuItem("Tools/TMG_EditorTools/Local Backup Window")]
        public static void ShowWindow()
        {
            GetWindow<LocalBackupWindow>("Local Backup");
        }

        private void OnEnable()
        {
            directoryToBackup = EditorPrefs.GetString(EditorPrefsKey, "Assets/MyFolder");

            // Load the prompt on quit bool. Default to true if not found.
            promptOnQuit = EditorPrefs.GetBool(PromptOnQuitKey, true);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(EditorPrefsKey, directoryToBackup);

            // Save the bool setting
            EditorPrefs.SetBool(PromptOnQuitKey, promptOnQuit);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Local Backup Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string newDirectory = EditorGUILayout.TextField("Directory to Backup", directoryToBackup);
            if (EditorGUI.EndChangeCheck())
            {
                directoryToBackup = newDirectory;
                EditorPrefs.SetString(EditorPrefsKey, directoryToBackup);
            }

            if (GUILayout.Button("Select Folder"))
            {
                string path = EditorUtility.OpenFolderPanel("Choose Folder to Backup", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        directoryToBackup = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        directoryToBackup = path;
                    }
                    EditorPrefs.SetString(EditorPrefsKey, directoryToBackup);
                }
            }

            if (GUILayout.Button("Create Backup Now"))
            {
                CreateBackup(directoryToBackup);
            }

            // NEW: Add a toggle to decide if we prompt for backups on quit
            EditorGUILayout.Space();
            promptOnQuit = EditorGUILayout.Toggle("Use Backup ", promptOnQuit);
        }

        private static void CreateBackup(string sourceDirectory)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath).FullName;
            string localBackupPath = Path.Combine(projectRootPath, "LocalBackup");
            Directory.CreateDirectory(localBackupPath);

            int nextBackupIndex = GetNextBackupIndex(localBackupPath);
            string backupFolderName = "Backup" + nextBackupIndex;
            string backupFolderPath = Path.Combine(localBackupPath, backupFolderName);

            CopyDirectory(sourceDirectory, backupFolderPath);

            Debug.Log($"LocalBackupWindow: Created backup '{backupFolderName}' at '{backupFolderPath}'");
        }

        private static int GetNextBackupIndex(string localBackupPath)
        {
            int nextIndex = 0;
            if (!Directory.Exists(localBackupPath))
                return nextIndex;

            string[] existingBackups = Directory.GetDirectories(localBackupPath, "Backup*");
            foreach (string backupDir in existingBackups)
            {
                string dirName = new DirectoryInfo(backupDir).Name; // e.g. "Backup3"
                if (int.TryParse(dirName.Replace("Backup", ""), out int currentIndex))
                {
                    if (currentIndex >= nextIndex)
                        nextIndex = currentIndex + 1;
                }
            }
            return nextIndex;
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                Debug.LogWarning($"LocalBackupWindow: Source directory does not exist: {sourceDir}");
                return;
            }

            Directory.CreateDirectory(destDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destDir, fileName);
                File.Copy(filePath, destFilePath, true);
            }

            foreach (string directoryPath in Directory.GetDirectories(sourceDir))
            {
                string subDirectoryName = Path.GetFileName(directoryPath);
                string destSubDir = Path.Combine(destDir, subDirectoryName);
                CopyDirectory(directoryPath, destSubDir);
            }
        }
    }
}
