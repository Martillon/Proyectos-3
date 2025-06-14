using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace TMG_EditorTools
{
    public class EditorTimeTrackerLegacy : EditorWindow
    {
        private DateTime startTime;
        private TimeSpan elapsedTime;
        private bool tracking;
        private string saveFilePath = "Assets/EditorTimeTracker.json";

        [MenuItem("Tools/TMG_EditorTools/Editor Timer Legacy")]
        public static void ShowWindow()
        {
            // Show the Legacy tracker window
            GetWindow<EditorTimeTrackerLegacy>("Editor Timer Legacy");
        }

        private void OnEnable()
        {
            LoadTime();
            tracking = true;
            EditorApplication.update += UpdateTime;
            // Ensure we save whenever the editor quits
            EditorApplication.quitting += SaveTime;
        }

        private void OnDisable()
        {
            tracking = false;
            EditorApplication.update -= UpdateTime;
            EditorApplication.quitting -= SaveTime;
            SaveTime();
        }

        private void UpdateTime()
        {
            if (tracking)
            {
                elapsedTime = DateTime.Now - startTime;
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Elapsed Time: " + elapsedTime.ToString());
            GUILayout.Label("Elapsed Hours: " + elapsedTime.TotalHours.ToString("F2"));
        }

        private void SaveTime()
        {
            // Save current elapsed time to a JSON file
            var data = new EditorTimeData { elapsedTimeTicks = elapsedTime.Ticks };
            var jsonData = JsonUtility.ToJson(data);
            File.WriteAllText(saveFilePath, jsonData);
        }

        private void LoadTime()
        {
            if (File.Exists(saveFilePath))
            {
                // Load from our JSON file if it exists
                var jsonData = File.ReadAllText(saveFilePath);
                var data = JsonUtility.FromJson<EditorTimeData>(jsonData);
                elapsedTime = new TimeSpan(data.elapsedTimeTicks);

                // We set the startTime so that the "elapsedTime" continues from 
                // whatever was previously saved.
                startTime = DateTime.Now - elapsedTime;
            }
            else
            {
                // No saved data; start fresh
                startTime = DateTime.Now;
            }
        }

        [Serializable]
        private class EditorTimeData
        {
            public long elapsedTimeTicks;
        }
    }
}
