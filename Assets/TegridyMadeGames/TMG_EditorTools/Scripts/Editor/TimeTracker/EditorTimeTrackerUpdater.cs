using UnityEditor;
using UnityEngine;
using System;

namespace TMG_EditorTools
{
    [InitializeOnLoad]
    public static class EditorTimeTrackerUpdater
    {
        private const string PREFS_KEY = "EditorTimeTracker_TotalElapsedTicks";
        private static DateTime _lastUpdate;     // When did we last record time?
        private static TimeSpan _totalElapsed;   // How much total time has passed?

        public static TimeSpan CurrentElapsedTime => _totalElapsed;

        static EditorTimeTrackerUpdater()
        {
            // Load previously saved total elapsed from EditorPrefs
            long savedTicks = long.Parse(EditorPrefs.GetString(PREFS_KEY, "0"));
            _totalElapsed = new TimeSpan(savedTicks);

            // Start new “session” from now
            _lastUpdate = DateTime.Now;

            // Hook up events
            EditorApplication.update += Update;
            EditorApplication.quitting += SaveData;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void Update()
        {
            // Calculate how long since last update
            var now = DateTime.Now;
            var delta = now - _lastUpdate;
            _lastUpdate = now;

            // Accumulate it into our total
            _totalElapsed += delta;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Save whenever we exit EditMode or exit PlayMode
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.ExitingPlayMode)
            {
                SaveData();
            }
            // When re-entering EditMode, reset _lastUpdate so we don’t double-count
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                _lastUpdate = DateTime.Now;
            }
        }

        private static void SaveData()
        {
            // Store total elapsed in Ticks as string
            EditorPrefs.SetString(PREFS_KEY, _totalElapsed.Ticks.ToString());
        }
    }
}
