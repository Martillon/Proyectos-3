using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine;


namespace TMG_EditorTools
{

    public class BuildPostProcessor : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildLogger.SetLog("");
            Debug.Log("Build log cleared before build start.");
        }


        public void OnPostprocessBuild(BuildReport report)
        {
            BuildLogger.AppendLog("========== Build Completed ==========");
            BuildLogger.AppendLog($"Build Result: {report.summary.result}");
            BuildLogger.AppendLog($"Total Size: {report.summary.totalSize} bytes");
            BuildLogger.AppendLog($"Output Path: {report.summary.outputPath}");
            BuildLogger.AppendLog("-----------------------------------------");

            LogUsedAssets();
            LogUnusedAssets();
            SaveBuildLogToProjectFolder();
            if (BuildLogWindow.openLastLogOnBuild)
            {
                if (EditorWindow.HasOpenInstances<BuildLogWindow>())
                {
                    BuildLogger.SetLog("");
                    BuildLogWindow.OpenLastLogFile();
                }
            }
            else if (EditorWindow.HasOpenInstances<BuildLogWindow>())
            {
                BuildLogWindow.ShowWindow();
            }

        }

        private void LogUsedAssets()
        {
            if (!BuildLogWindow.enableUsedAssetReport) return;

            HashSet<string> assetsUsed = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;

                string[] dependencies = AssetDatabase.GetDependencies(scene.path, true);
                assetsUsed.UnionWith(dependencies);
            }

            // Collect assets with their sizes in a list
            List<(string asset, double sizeInMB)> assetList = new List<(string, double)>();
            foreach (string asset in assetsUsed)
            {
                double sizeInMB = new FileInfo(asset).Length / 1048576.0;
                assetList.Add((asset, sizeInMB));
            }

            // Sort by size (descending)
            assetList.Sort((x, y) => y.sizeInMB.CompareTo(x.sizeInMB));

            BuildLogger.AppendLog("========== Assets Used in Build ==========");
            foreach (var item in assetList)
            {
                BuildLogger.AppendLog($"Asset: {item.asset}, Size: {item.sizeInMB:F2} MB");
            }
            BuildLogger.AppendLog("============================================");
        }

        private void LogUnusedAssets()
        {
            if (!BuildLogWindow.enableUnusedAssetReport) return;

            HashSet<string> assetsUsed = new HashSet<string>(AssetDatabase.GetDependencies("Assets", true));
            HashSet<string> assetsAll = new HashSet<string>(Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories));
            HashSet<string> assetsUnused = new HashSet<string>(assetsAll.Except(assetsUsed));

            // Collect assets with their sizes in a list
            List<(string asset, double sizeInMB)> unusedAssetList = new List<(string, double)>();
            foreach (string asset in assetsUnused)
            {
                double sizeInMB = new FileInfo(asset).Length / 1048576.0;
                unusedAssetList.Add((asset, sizeInMB));
            }

            // Sort by size (descending)
            unusedAssetList.Sort((x, y) => y.sizeInMB.CompareTo(x.sizeInMB));

            BuildLogger.AppendLog("========== Unused Assets ==========");
            foreach (var item in unusedAssetList)
            {
                BuildLogger.AppendLog($"Unused Asset: {item.asset}, Size: {item.sizeInMB:F2} MB");
            }
            BuildLogger.AppendLog("====================================");
        }


        private void SaveBuildLogToProjectFolder()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string logFilePath = Path.Combine(projectRoot, "BuildLog.txt");

            File.WriteAllText(logFilePath, BuildLogger.GetBuildLog());
            string input = "Build log automatically saved to: " + logFilePath;
            string output = DebugC.FormatMessage(input, Color.green);
            Debug.Log(output);

            AssetDatabase.Refresh();
        }
        public static string LoadBuildLogFromProjectFolder()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string logFilePath = Path.Combine(projectRoot, "BuildLog.txt");

            if (File.Exists(logFilePath))
            {
                string loadedLog = File.ReadAllText(logFilePath);
                string input = "Build log automatically loaded from: " + logFilePath;
                string output = DebugC.FormatMessage(input, Color.green);
                Debug.Log(output);
                return loadedLog;
            }
            else
            {
                Debug.LogWarning("Build log file not found at: " + logFilePath);
                return "";
            }
        }
    }
}