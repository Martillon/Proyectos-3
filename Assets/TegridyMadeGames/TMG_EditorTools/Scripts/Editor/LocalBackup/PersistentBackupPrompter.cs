using UnityEditor;
using UnityEngine;
using System;
using System.IO;

namespace TMG_EditorTools
{
    [InitializeOnLoad]
    public static class PersistentBackupPrompter
    {
        private const string EditorPrefsKey = "LocalBackupWindow_directoryToBackup";

        // NEW: Must match the key in LocalBackupWindow
        private const string PromptOnQuitKey = "LocalBackupWindow_promptOnQuit";

        static PersistentBackupPrompter()
        {
            EditorApplication.wantsToQuit += OnEditorWantsToQuit;
        }

        private static bool OnEditorWantsToQuit()
        {
            // 1. Check if the user wants to be prompted at all
            bool shouldPrompt = EditorPrefs.GetBool(PromptOnQuitKey, true);
            if (!shouldPrompt)
            {
                // If user has disabled prompts, allow Editor to quit immediately
                return true;
            }

            // 2. Otherwise, proceed with the confirmation prompt
            string directoryToBackup = EditorPrefs.GetString(EditorPrefsKey, "Assets/MyFolder");
            bool userSaidYes = EditorUtility.DisplayDialog(
                "Backup Before Quitting?",
                $"Would you like to create a backup of '{directoryToBackup}' before closing the Editor?",
                "Yes",
                "No"
            );

            if (userSaidYes)
            {
                CreateBackup(directoryToBackup);
            }

            // Returning true always allows Editor to quit
            return true;
        }

        /// <summary>
        /// Creates a new backup folder (Backup0_YYYYMMDD_HHMMss, etc.) in LocalBackup
        /// at the project root level, and copies all files from the source directory
        /// into a subfolder named after the last part of the source directory path.
        /// </summary>
        private static void CreateBackup(string sourceDirectory)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath).FullName;
            string localBackupPath = Path.Combine(projectRootPath, "LocalBackup");
            Directory.CreateDirectory(localBackupPath);

            int nextBackupIndex = GetNextBackupIndex(localBackupPath);

            string dateTimeSuffix = DateTime.Now.ToString("'Y'yyyy'_M'MM'_D'dd'_H'HH'_M'mm'_S'ss");
            string backupFolderName = $"Backup{nextBackupIndex}_{dateTimeSuffix}";
            string backupFolderPath = Path.Combine(localBackupPath, backupFolderName);

            string subFolderName = Path.GetFileName(
                sourceDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            );
            string finalDestinationPath = Path.Combine(backupFolderPath, subFolderName);

            CopyDirectory(sourceDirectory, finalDestinationPath);

            Debug.Log($"PersistentBackupPrompter: Created backup '{backupFolderName}' at '{finalDestinationPath}'.");
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
                string[] nameParts = dirName.Split('_');
                string backupRoot = nameParts[0]; // e.g. "Backup3"

                if (int.TryParse(backupRoot.Replace("Backup", ""), out int currentIndex))
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
                Debug.LogWarning($"PersistentBackupPrompter: Source directory does not exist: {sourceDir}");
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
