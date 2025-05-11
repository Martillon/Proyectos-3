// --- START OF FILE LevelProgressionManager.cs ---
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scripts.Core.Progression;

namespace Scripts.Core
{
    public class LevelProgressionManager : MonoBehaviour
    {
        public static LevelProgressionManager Instance { get; private set; }

        private AllLevelsProgressionData progressionData;
        private string saveFileName = "level_progression.json";
        private string saveFilePath;

        private SceneLoader sceneLoaderInstance; // Store the reference

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
                // We will get SceneLoader.Instance in Start()
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // Get SceneLoader instance here, as its Awake should have run
            sceneLoaderInstance = SceneLoader.Instance; 
            if (sceneLoaderInstance == null)
            {
                Debug.LogError("LevelProgressionManager: SceneLoader.Instance is null in Start! Level list and progression might be incorrect. Ensure SceneLoader is in your persistent scene and initializes first.", this);
            }
            LoadProgression(); // Now load progression, which might depend on sceneLoaderInstance.levels
        }


        public void LoadProgression()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    progressionData = JsonUtility.FromJson<AllLevelsProgressionData>(json);
                    
                    // Validate and update progression data based on current SceneLoader.levels
                    if (sceneLoaderInstance != null && sceneLoaderInstance.levels != null)
                    {
                        bool dataModified = false;
                        List<string> currentLevelIdentifiers = sceneLoaderInstance.levels.ToList();
                        List<LevelStatus> validatedStatuses = new List<LevelStatus>();

                        // Ensure all levels from SceneLoader exist in progressionData
                        foreach (string levelId in currentLevelIdentifiers)
                        {
                            LevelStatus existingStatus = progressionData.levelStatuses.FirstOrDefault(s => s.levelIdentifier == levelId);
                            if (existingStatus != null)
                            {
                                validatedStatuses.Add(existingStatus);
                            }
                            else
                            {
                                validatedStatuses.Add(new LevelStatus(levelId, false, false)); // Add new level as locked
                                dataModified = true;
                                // Debug.Log($"LevelProgressionManager: Added new level '{levelId}' to progression data."); // Uncomment for debugging
                            }
                        }
                        // Optionally, remove statuses for levels no longer in SceneLoader.levels
                        // progressionData.levelStatuses = progressionData.levelStatuses
                        //    .Where(s => currentLevelIdentifiers.Contains(s.levelIdentifier)).ToList();
                        // if (progressionData.levelStatuses.Count != validatedStatuses.Count) dataModified = true; // if some were removed

                        progressionData.levelStatuses = validatedStatuses;


                        if (dataModified) SaveProgression();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"LevelProgressionManager: Failed to load or parse progression data from {saveFilePath}. Error: {e.Message}. Initializing fresh progression.");
                    InitializeNewProgression();
                }
            }
            else
            {
                InitializeNewProgression();
            }

            // Ensure first level is unlocked
            if (progressionData != null && progressionData.levelStatuses != null && progressionData.levelStatuses.Count > 0)
            {
                if (!progressionData.levelStatuses[0].isUnlocked)
                {
                    progressionData.levelStatuses[0].isUnlocked = true;
                    // Debug.Log($"LevelProgressionManager: First level '{progressionData.levelStatuses[0].levelIdentifier}' auto-unlocked."); // Uncomment for debugging
                    SaveProgression();
                }
            }
            // else Debug.LogWarning("LevelProgressionManager: No progression data or no levels to process after LoadProgression."); // Uncomment for debugging
        }

        private void InitializeNewProgression()
        {
            progressionData = new AllLevelsProgressionData();
            if (sceneLoaderInstance != null && sceneLoaderInstance.levels != null && sceneLoaderInstance.levels.Length > 0)
            {
                for (int i = 0; i < sceneLoaderInstance.levels.Length; i++)
                {
                    string levelIdentifier = sceneLoaderInstance.levels[i];
                    progressionData.levelStatuses.Add(new LevelStatus(levelIdentifier, (i == 0), false));
                }
                // Debug.Log($"LevelProgressionManager: Initialized new progression for {progressionData.levelStatuses.Count} levels."); // Uncomment for debugging
            }
            // else Debug.LogWarning("LevelProgressionManager (Initialize): SceneLoader.Instance or its 'levels' array is not available. Cannot initialize progression."); // Uncomment for debugging
            
            SaveProgression();
        }

        // ... (SaveProgression, GetLevelStatus, IsLevelUnlocked, IsLevelCompleted, UnlockLevel, CompleteLevel sin cambios mayores, pero usando sceneLoaderInstance) ...
        public void SaveProgression()
        {
            if (progressionData == null) return;
            try
            {
                string json = JsonUtility.ToJson(progressionData, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (System.Exception e) { Debug.LogError($"LevelProgressionManager: Save failed: {e.Message}"); }
        }

        private LevelStatus GetLevelStatus(string levelIdentifier)
        {
            return progressionData?.levelStatuses?.FirstOrDefault(s => s.levelIdentifier == levelIdentifier);
        }

        public bool IsLevelUnlocked(string levelIdentifier) => GetLevelStatus(levelIdentifier)?.isUnlocked ?? false;
        public bool IsLevelCompleted(string levelIdentifier) => GetLevelStatus(levelIdentifier)?.isCompleted ?? false;

        public void UnlockLevel(string levelIdentifier)
        {
            LevelStatus status = GetLevelStatus(levelIdentifier);
            if (status != null && !status.isUnlocked)
            {
                status.isUnlocked = true;
                // SaveProgression(); // Decide: save per action or batch
            }
        }

        public void CompleteLevel(string levelIdentifier)
        {
            LevelStatus status = GetLevelStatus(levelIdentifier);
            if (status != null)
            {
                bool wasAlreadyCompleted = status.isCompleted;
                status.isCompleted = true;
                // Debug.Log($"LevelProgressionManager: Level '{levelIdentifier}' marked completed."); // Uncomment for debugging

                if (sceneLoaderInstance != null && sceneLoaderInstance.levels != null)
                {
                    int currentIndex = System.Array.IndexOf(sceneLoaderInstance.levels, levelIdentifier);
                    if (currentIndex != -1 && currentIndex + 1 < sceneLoaderInstance.levels.Length)
                    {
                        string nextLevelIdentifier = sceneLoaderInstance.levels[currentIndex + 1];
                        UnlockLevel(nextLevelIdentifier);
                    }
                }
                // Only save if there was a change (new completion or new unlock)
                if(!wasAlreadyCompleted || (status.isUnlocked && !IsLevelUnlocked(status.levelIdentifier))) // Crude check, can be better
                {
                     SaveProgression();
                }
            }
        }
        
        public int GetTotalLevels() => sceneLoaderInstance?.levels?.Length ?? 0;
        public string GetLevelIdentifier(int displayIndex) => (sceneLoaderInstance?.levels != null && displayIndex >= 0 && displayIndex < sceneLoaderInstance.levels.Length) ? sceneLoaderInstance.levels[displayIndex] : null;
        public void ResetAllProgression() { InitializeNewProgression(); }

    }
}
// --- END OF FILE LevelProgressionManager.cs ---
