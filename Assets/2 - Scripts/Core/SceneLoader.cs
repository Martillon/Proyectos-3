// --- START OF FILE SceneLoader.cs ---
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Namespace de Unity para manejo de escenas
using System.Linq; // For LINQ operations like Array.IndexOf

namespace Scripts.Core
{
    /// <summary>
    /// Manages scene transitions using additive loading. Ensures essential scenes like 'Program'
    /// are preserved while unloading others. Supports an optional loading screen.
    /// This is a persistent singleton.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Loading Screen")]
        [Tooltip("Optional GameObject to activate during scene unloading/loading transitions.")]
        [SerializeField] private GameObject loaderScreenObject; // Renamed for clarity

        [Header("Core Scene Configuration")]
        [Tooltip("Name of the main menu scene.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu"; // Used GameConstants if available
        [Tooltip("Name of the persistent program/manager scene that should never be unloaded.")]
        [SerializeField] private string programSceneName = "Program"; // Used GameConstants if available
        
        [Header("Game Levels")]
        [Tooltip("Array of scene names for all playable game levels, in order of progression.")]
        public string[] levels; // These are scene names, e.g., "Level1", "ForestPath"

        // Internal tracking of which scenes should remain loaded after a transition
        private List<string> wantedSceneNames = new List<string>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Ensure this loader persists across scene loads
                // Debug.Log("SceneLoader: Instance created and marked as DontDestroyOnLoad.", this); // Uncomment for debugging
            }
            else
            {
                // Debug.LogWarning("SceneLoader: Duplicate instance found. Destroying self.", this); // Uncomment for debugging
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Automatically load the main menu when the SceneLoader first starts
            // (typically when the game launches into the Program/Initializer scene).
            // Check if main menu is already loaded (e.g. if starting directly in MainMenu scene for testing)
            if (!IsSceneLoaded(mainMenuSceneName) && SceneManager.GetActiveScene().name != programSceneName)
            {
                // Only load if not already in main menu or if main menu isn't the active scene (and not program scene)
                 LoadMenu();
            }
            // else Debug.Log($"SceneLoader: Main menu ('{mainMenuSceneName}') might already be loaded or starting in it.", this); // Uncomment for debugging
        }

        /// <summary>
        /// Loads the main menu scene additively and initiates cleanup of other non-essential scenes.
        /// </summary>
        public void LoadMenu()
        {
            // Debug.Log($"SceneLoader: Attempting to load Main Menu ('{mainMenuSceneName}').", this); // Uncomment for debugging
            LoadSceneAndPrepareCleanup(mainMenuSceneName);
        }

        /// <summary>
        /// Loads a game level by its scene name from the 'levels' array.
        /// Ensures the level is loaded additively and initiates cleanup of other scenes.
        /// </summary>
        /// <param name="levelSceneName">The name of the level scene to load.</param>
        public void LoadLevelByName(string levelSceneName)
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogError("SceneLoader: LoadLevelByName called with null or empty scene name.", this);
                return;
            }
            // Debug.Log($"SceneLoader: Attempting to load level by name: '{levelSceneName}'.", this); // Uncomment for debugging
            
            // Validate if the levelSceneName is actually in our list of known levels (optional but good)
            if (levels == null || !levels.Contains(levelSceneName))
            {
                Debug.LogWarning($"SceneLoader: Scene name '{levelSceneName}' is not in the configured 'levels' array. Attempting to load anyway.", this);
            }
            LoadSceneAndPrepareCleanup(levelSceneName);
        }

        /// <summary>
        /// Loads a game level by its index in the 'levels' array.
        /// Ensures the level is loaded additively and initiates cleanup of other scenes.
        /// </summary>
        /// <param name="levelArrayIndex">The 0-based index of the level in the 'levels' array.</param>
        public void LoadLevelByIndex(int levelArrayIndex)
        {
            if (levels == null || levelArrayIndex < 0 || levelArrayIndex >= levels.Length)
            {
                Debug.LogError($"SceneLoader: LoadLevelByIndex called with invalid index {levelArrayIndex}. Max index is {levels?.Length - 1}.", this);
                return;
            }
            string sceneNameToLoad = levels[levelArrayIndex];
            // Debug.Log($"SceneLoader: Attempting to load level by index {levelArrayIndex}: '{sceneNameToLoad}'.", this); // Uncomment for debugging
            LoadSceneAndPrepareCleanup(sceneNameToLoad);
        }

        /// <summary>
        /// Core logic to load a target scene additively and then start the cleanup coroutine.
        /// </summary>
        private void LoadSceneAndPrepareCleanup(string sceneNameToLoad)
        {
            if (!IsSceneLoaded(sceneNameToLoad))
            {
                // Debug.Log($"SceneLoader: Loading scene '{sceneNameToLoad}' additively.", this); // Uncomment for debugging
                SceneManager.LoadScene(sceneNameToLoad, LoadSceneMode.Additive);
            }
            // else Debug.Log($"SceneLoader: Scene '{sceneNameToLoad}' is already loaded. Skipping load.", this); // Uncomment for debugging

            wantedSceneNames.Clear();
            wantedSceneNames.Add(sceneNameToLoad);
            // Always keep the program scene loaded if it exists and is configured
            if (!string.IsNullOrEmpty(programSceneName) && IsSceneLoaded(programSceneName)) 
            {
                wantedSceneNames.Add(programSceneName);
            }

            StartCoroutine(UnloadUnwantedScenesCoroutine());
        }

        /// <summary>
        /// Coroutine to unload all scenes that are not in the 'wantedSceneNames' list.
        /// The 'programSceneName' is always preserved.
        /// </summary>
        private IEnumerator UnloadUnwantedScenesCoroutine()
        {
            if (loaderScreenObject != null) loaderScreenObject.SetActive(true);

            // Wait a frame to ensure the newly loaded scene is fully registered and its Awake/Start methods
            // have a chance to run before we potentially unload scenes it might depend on (though additive usually ok).
            yield return null; 
            //yield return new WaitForEndOfFrame(); // Alternative, sometimes more robust for UI updates

            List<Scene> scenesToUnload = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene currentScene = SceneManager.GetSceneAt(i);
                if (currentScene.isLoaded && // Only consider loaded scenes
                    !wantedSceneNames.Contains(currentScene.name) &&
                    currentScene.name != programSceneName) // Always keep the program scene
                {
                    scenesToUnload.Add(currentScene);
                }
            }

            if (scenesToUnload.Count > 0)
            {
                // Debug.Log($"SceneLoader: Unloading {scenesToUnload.Count} unwanted scene(s).", this); // Uncomment for debugging
                foreach (Scene sceneToUnload in scenesToUnload)
                {
                    // Debug.Log($"SceneLoader: Unloading '{sceneToUnload.name}'.", this); // Uncomment for debugging
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneToUnload);
                    // Optionally, wait for each unload operation to complete if sequence matters
                    // while (unloadOperation != null && !unloadOperation.isDone) yield return null; 
                }
                // Wait for all unload operations to likely complete (can also track all AsyncOperations)
                yield return new WaitForSeconds(0.1f); // Small delay to let async operations progress
            }
            // else Debug.Log("SceneLoader: No unwanted scenes to unload.", this); // Uncomment for debugging

            if (loaderScreenObject != null) loaderScreenObject.SetActive(false);
            // Debug.Log("SceneLoader: Scene cleanup finished.", this); // Uncomment for debugging
        }

        /// <summary>
        /// Checks if a scene with the given name is currently loaded.
        /// </summary>
        /// <param name="sceneName">The name of the scene to check.</param>
        /// <returns>True if the scene is loaded, false otherwise.</returns>
        public static bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName && SceneManager.GetSceneAt(i).isLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        // Helper to get Build Index if needed, though names are preferred with this setup
        // public int GetBuildIndexFromName(string sceneName) { ... }
        
        public void ReloadCurrentLevelScene() // O ReloadSceneByBuildIndex(int buildIndex)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name != programSceneName && currentScene.name != mainMenuSceneName)
            {
                // Debug.Log($"SceneLoader: Reloading current scene '{currentScene.name}'.", this); // Uncomment for debugging
                // We want to ensure only this scene remains, plus the program scene.
                LoadSceneAndPrepareCleanup(currentScene.name);
            }
            else
            {
                Debug.LogWarning($"SceneLoader: Attempted to reload a non-level scene ('{currentScene.name}'). Aborting reload.", this);
            }
        }
        
        /// <summary>
        /// Loads a scene by its build index. Primarily for special cases like reloading.
        /// </summary>
        /// <param name="sceneBuildIndex">The build index of the scene to load.</param>
        public void LoadSceneByBuildIndex(int sceneBuildIndex)
        {
            if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"SceneLoader: Invalid scene build index: {sceneBuildIndex}", this);
                return;
            }
            string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"SceneLoader: Could not get scene name for build index: {sceneBuildIndex}", this);
                return;
            }
            // Debug.Log($"SceneLoader: Attempting to load scene by build index {sceneBuildIndex} ('{sceneName}').", this); // Uncomment for debugging
            LoadSceneAndPrepareCleanup(sceneName);
        }

    }
}
// --- END OF FILE SceneLoader.cs ---