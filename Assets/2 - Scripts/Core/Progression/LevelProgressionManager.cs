using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scripts.Core.Progression;

namespace Scripts.Core
{
    /// <summary>
    /// Manages the player's progression through levels, including unlock and completion status.
    /// Saves and loads progression data to a JSON file.
    /// </summary>
    public class LevelProgressionManager : MonoBehaviour
    {
        public static LevelProgressionManager Instance { get; private set; }

        private AllLevelsProgressionData _progressionData;
        private const string SAVE_FILE_NAME = "level_progression.json";
        private string _saveFilePath;

        // Cached reference to the SceneLoader instance.
        private SceneLoader _sceneLoader;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // This manager should persist.
            // DontDestroyOnLoad(gameObject); // Uncomment if not in a persistent scene.

            _saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }

        private void Start()
        {
            // Get SceneLoader instance here to ensure its Awake has run.
            _sceneLoader = SceneLoader.Instance;
            if (_sceneLoader == null)
            {
                Debug.LogError("LevelProgressionManager: SceneLoader.Instance is null in Start! Progression will not work correctly. Ensure SceneLoader is in your persistent scene and initializes first.", this);
                // Initialize with an empty data set to prevent null reference errors.
                _progressionData = new AllLevelsProgressionData();
                return;
            }

            LoadProgression();
        }

        /// <summary>
        /// Loads progression data from the file, validates it against the current level list,
        /// and initializes a new file if one doesn't exist.
        /// </summary>
        public void LoadProgression()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_saveFilePath);
                    _progressionData = JsonUtility.FromJson<AllLevelsProgressionData>(json);
                    ValidateAndSyncProgressionData();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"LevelProgressionManager: Failed to load or parse progression data from {_saveFilePath}. Error: {e.Message}. Initializing fresh progression.", this);
                    InitializeNewProgression();
                }
            }
            else
            {
                InitializeNewProgression();
            }

            EnsureFirstLevelIsUnlocked();
        }

        /// <summary>
        /// Saves the current progression data to the JSON file.
        /// </summary>
        public void SaveProgression()
        {
            if (_progressionData == null) return;
            try
            {
                string json = JsonUtility.ToJson(_progressionData, true); // 'true' for pretty print
                File.WriteAllText(_saveFilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"LevelProgressionManager: Save failed to path {_saveFilePath}. Error: {e.Message}", this);
            }
        }

        /// <summary>
        /// Marks a level as completed and unlocks the next one.
        /// </summary>
        public void CompleteLevel(string levelIdentifier)
        {
            LevelStatus status = GetLevelStatus(levelIdentifier);
            if (status == null)
            {
                Debug.LogWarning($"LevelProgressionManager: Tried to complete an unknown level: '{levelIdentifier}'", this);
                return;
            }

            if (!status.isCompleted)
            {
                status.isCompleted = true;
                // Debug.Log($"Level '{levelIdentifier}' marked as completed.");

                // Unlock the next level
                int completedIndex = System.Array.IndexOf(_sceneLoader.levels, levelIdentifier);
                if (completedIndex > -1 && completedIndex + 1 < _sceneLoader.levels.Length)
                {
                    string nextLevelIdentifier = _sceneLoader.levels[completedIndex + 1];
                    UnlockLevel(nextLevelIdentifier);
                }

                SaveProgression();
            }
        }
        
        /// <summary>
        /// Wipes all progression data and starts fresh. Unlocks the first level.
        /// </summary>
        public void ResetAllProgression()
        {
            Debug.Log("LevelProgressionManager: Resetting all level progression data.");
            InitializeNewProgression();
            EnsureFirstLevelIsUnlocked();
            SaveProgression();
        }
        
        public void UnlockAllLevels()
        {
            if (_progressionData?.levelStatuses == null) return;

            foreach (var status in _progressionData.levelStatuses)
            {
                status.isUnlocked = true;
            }
            SaveProgression();
            Debug.Log("All levels have been unlocked through debug command.");
        }

        #region Status Queries
        public bool IsLevelUnlocked(string levelIdentifier) => GetLevelStatus(levelIdentifier)?.isUnlocked ?? false;
        public bool IsLevelCompleted(string levelIdentifier) => GetLevelStatus(levelIdentifier)?.isCompleted ?? false;
        public int GetTotalLevelCount() => _sceneLoader?.levels?.Length ?? 0;
        public string GetLevelIdentifierByIndex(int index)
        {
            if (_sceneLoader?.levels != null && index >= 0 && index < _sceneLoader.levels.Length)
            {
                return _sceneLoader.levels[index];
            }
            return null;
        }
        #endregion

        #region Private Helpers
        private void InitializeNewProgression()
        {
            _progressionData = new AllLevelsProgressionData();
            if (_sceneLoader?.levels == null) return;

            foreach (string levelId in _sceneLoader.levels)
            {
                _progressionData.levelStatuses.Add(new LevelStatus(levelId));
            }
            SaveProgression();
        }

        private void ValidateAndSyncProgressionData()
        {
            if (_sceneLoader?.levels == null || _progressionData?.levelStatuses == null) return;

            bool wasModified = false;
            List<string> currentLevelList = _sceneLoader.levels.ToList();
            
            // Remove saved levels that no longer exist in the game's level list
            int removedCount = _progressionData.levelStatuses.RemoveAll(status => !currentLevelList.Contains(status.levelIdentifier));
            if (removedCount > 0)
            {
                wasModified = true;
                // Debug.Log($"Removed {removedCount} obsolete levels from progression data.");
            }

            // Add new levels from the game's list that are not in the save file
            foreach (string levelId in currentLevelList)
            {
                if (!_progressionData.levelStatuses.Any(s => s.levelIdentifier == levelId))
                {
                    _progressionData.levelStatuses.Add(new LevelStatus(levelId));
                    wasModified = true;
                    // Debug.Log($"Added new level '{levelId}' to progression data.");
                }
            }
            
            if (wasModified) SaveProgression();
        }

        private void EnsureFirstLevelIsUnlocked()
        {
            if (_progressionData?.levelStatuses != null && _progressionData.levelStatuses.Count > 0)
            {
                if (!_progressionData.levelStatuses[0].isUnlocked)
                {
                    _progressionData.levelStatuses[0].isUnlocked = true;
                    // Debug.Log($"First level '{_progressionData.levelStatuses[0].levelIdentifier}' was locked. Unlocking it now.");
                    SaveProgression();
                }
            }
        }
        
        private void UnlockLevel(string levelIdentifier)
        {
            LevelStatus status = GetLevelStatus(levelIdentifier);
            if (status != null && !status.isUnlocked)
            {
                status.isUnlocked = true;
                // Debug.Log($"Level '{levelIdentifier}' unlocked.");
                // Note: Saving is handled by CompleteLevel after unlocking.
            }
        }
        
        private LevelStatus GetLevelStatus(string levelIdentifier)
        {
            return _progressionData?.levelStatuses?.FirstOrDefault(s => s.levelIdentifier == levelIdentifier);
        }
        #endregion
    }
}
