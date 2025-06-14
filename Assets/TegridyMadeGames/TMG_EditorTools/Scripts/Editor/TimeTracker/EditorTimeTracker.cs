using UnityEngine;
using UnityEditor;
using System;

namespace TMG_EditorTools
{
    public class EditorTimeTracker : EditorWindow
    {
        [MenuItem("Tools/TMG_EditorTools/Editor Timer")]
        public static void ShowWindow()
        {
            GetWindow<EditorTimeTracker>("Editor Timer");
        }

        private void OnGUI()
        {
            // Fetch the current elapsed time from the static class
            TimeSpan elapsedTime = EditorTimeTrackerUpdater.CurrentElapsedTime;

            // Break it down to days, hours, minutes, seconds
            int totalDays = (int)elapsedTime.TotalDays;
            int hours = elapsedTime.Hours;
            int minutes = elapsedTime.Minutes;
            int seconds = elapsedTime.Seconds;

            GUILayout.Label("Elapsed Time: " + elapsedTime.ToString("c"));
            GUILayout.Label($"Total Days: {totalDays}");
            GUILayout.Label($"Hours: {hours}" + $", Mins: {minutes}" + $", Secs: {seconds}");

            // Alternatively, you can show it all in one line, e.g.:
            // GUILayout.Label($"Elapsed: {totalDays}d {hours}h {minutes}m {seconds}s");
        }
    }
}
