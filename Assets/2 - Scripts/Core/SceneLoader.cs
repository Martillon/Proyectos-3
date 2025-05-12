using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Namespace de Unity para manejo de escenas
using System.Linq; // Para LINQ (Contains)

namespace Scripts.Core // Manteniendo tu namespace
{
    /// <summary>
    /// Manages scene transitions using additive loading.
    /// Ensures essential scenes like the 'Program' scene are preserved while unloading others.
    /// Supports an optional loading screen. This is a persistent singleton.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader singleton;
        public static SceneLoader Instance => singleton; // Usando tu patrón original de Singleton

        [Header("Loading Screen")]
        [Tooltip("Optional GameObject to activate during scene transitions.")]
        [SerializeField] private GameObject loaderScreenObject;

        [Header("Scene Configuration")]
        [Tooltip("Name of the main menu scene.")]
        [SerializeField] private string mainMenuSceneName = GameConstants.MainMenuSceneName; // Usar constante
        [Tooltip("Name of the persistent program/manager scene that should never be unloaded.")]
        [SerializeField] private string programSceneName = GameConstants.ProgramSceneName; // Usar constante
        
        [Header("Game Levels")]
        [Tooltip("Array of scene names for all playable game levels, in order of progression.")]
        public string[] levels; // Estos son NOMBRES de escena

        // Internal: tracks which scenes (plus 'programSceneName') should remain loaded after a transition
        private readonly List<string> _wantedSceneNamesInternal = new List<string>();

        private void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
                DontDestroyOnLoad(gameObject); // Crucial si este script está en la escena "Program" y debe persistir
                // Debug.Log("SceneLoader: Singleton instance created and marked DontDestroyOnLoad.", this); // Uncomment for debugging
            }
            else if (singleton != this) // Asegurarse de que no se destruya a sí mismo si es el singleton
            {
                // Debug.LogWarning("SceneLoader: Duplicate instance found. Destroying this one.", this); // Uncomment for debugging
                Destroy(gameObject); // Destruir este duplicado
            }
        }

        private void Start()
        {
            // Si la escena "Program" es la única cargada al inicio, cargar el menú principal.
            if (SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == programSceneName)
            {
                 LoadMenu();
            }
            // Si por alguna razón el juego empieza en una escena que no es Program ni MainMenu,
            // y MainMenu no está cargado, también cargar MainMenu.
            else if (SceneManager.GetActiveScene().name != programSceneName && 
                     SceneManager.GetActiveScene().name != mainMenuSceneName && 
                     !IsSceneLoaded(mainMenuSceneName))
            {
                LoadMenu();
            }
            // else Debug.Log("SceneLoader: MainMenu already loaded or not needed at this Start().", this); // Uncomment for debugging
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
        /// Loads a game level by its scene name (identifier).
        /// </summary>
        /// <param name="levelSceneName">The name of the level scene to load from the 'levels' array or directly.</param>
        public void LoadLevelByName(string levelSceneName)
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogError("SceneLoader: LoadLevelByName called with null or empty scene name.", this);
                return;
            }
            // Debug.Log($"SceneLoader: Attempting to load level by name: '{levelSceneName}'.", this); // Uncomment for debugging
            LoadSceneAndPrepareCleanup(levelSceneName);
        }

        /// <summary>
        /// Loads a game level by its index in the 'levels' array.
        /// </summary>
        /// <param name="levelArrayIndex">The 0-based index of the level in the 'levels' array.</param>
        public void LoadLevelByIndex(int levelArrayIndex)
        {
            if (levels == null || levelArrayIndex < 0 || levelArrayIndex >= levels.Length)
            {
                Debug.LogError($"SceneLoader: LoadLevelByIndex - Invalid index {levelArrayIndex}. Levels array size: {levels?.Length}.", this);
                return;
            }
            string sceneNameToLoad = levels[levelArrayIndex];
            // Debug.Log($"SceneLoader: Attempting to load level by index {levelArrayIndex} ('{sceneNameToLoad}').", this); // Uncomment for debugging
            LoadSceneAndPrepareCleanup(sceneNameToLoad);
        }
        
        /// <summary>
        /// Reloads the currently active game scene if it's a level scene.
        /// </summary>
        public void ReloadCurrentLevelScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            // Make sure we are not trying to "reload" the program scene or the main menu as a "level"
            // (unless main menu is somehow a level, which is unusual for this setup).
            // A more robust check might be if `levels.Contains(currentScene.name)`.
            if (currentScene.name != programSceneName && currentScene.name != mainMenuSceneName)
            {
                // Debug.Log($"SceneLoader: Reloading current scene '{currentScene.name}'.", this); // Uncomment for debugging
                LoadSceneAndPrepareCleanup(currentScene.name);
            }
            else
            {
                Debug.LogWarning($"SceneLoader: Attempted to reload a non-level or core scene ('{currentScene.name}') via ReloadCurrentLevelScene. Aborting.", this);
            }
        }
        
        /// <summary>
        /// Loads a scene by its build index. Use with caution, prefer name/array index methods.
        /// </summary>
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
            // Debug.Log($"SceneLoader: Loading scene by build index {sceneBuildIndex} ('{sceneName}').", this); // Uncomment for debugging
            LoadSceneAndPrepareCleanup(sceneName);
        }


        /// <summary>
        /// Core logic: loads the target scene additively if not already loaded,
        /// then prepares the list of scenes to keep and starts the unloading coroutine.
        /// </summary>
        private void LoadSceneAndPrepareCleanup(string sceneNameToLoad)
        {
            if (!IsSceneLoaded(sceneNameToLoad))
            {
                // Debug.Log($"SceneLoader: Loading scene '{sceneNameToLoad}' additively.", this); // Uncomment for debugging
                SceneManager.LoadScene(sceneNameToLoad, LoadSceneMode.Additive);
            }
            // else Debug.Log($"SceneLoader: Scene '{sceneNameToLoad}' is already loaded. Skipping load.", this); // Uncomment for debugging

            _wantedSceneNamesInternal.Clear();
            _wantedSceneNamesInternal.Add(sceneNameToLoad); // The newly loaded/focused scene
            // Always keep the program scene
            if (!string.IsNullOrEmpty(programSceneName) /*&& IsSceneLoaded(programSceneName)*/) // IsSceneLoaded check is good but might not be needed if Program always exists
            {
                _wantedSceneNamesInternal.Add(programSceneName);
            }

            StartCoroutine(UnloadUnwantedScenesCoroutine());
        }

        private IEnumerator UnloadUnwantedScenesCoroutine()
        {
            // Debug.Log("SceneLoader: Starting UnloadUnwantedScenesCoroutine."); // Uncomment for debugging
            if (loaderScreenObject != null) loaderScreenObject.SetActive(true);

            // Wait one frame. Crucial for the new scene to be fully registered by SceneManager.
            yield return null;

            List<string> scenesToUnloadNames = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene currentScene = SceneManager.GetSceneAt(i);
                if (currentScene.isLoaded && 
                    currentScene.name != programSceneName && // Never unload the program scene
                    !_wantedSceneNamesInternal.Contains(currentScene.name))
                {
                    scenesToUnloadNames.Add(currentScene.name);
                }
            }

            if (scenesToUnloadNames.Count > 0)
            {
                // Debug.Log($"SceneLoader: Found {scenesToUnloadNames.Count} scene(s) to unload: {string.Join(", ", scenesToUnloadNames)}", this); // Uncomment for debugging
                // It's important to create a list of AsyncOperations if you need to wait for all of them.
                // For simplicity, we'll just start them.
                foreach (string nameOfSceneToUnload in scenesToUnloadNames)
                {
                    // Debug.Log($"SceneLoader: Unloading '{nameOfSceneToUnload}'.", this); // Uncomment for debugging
                    SceneManager.UnloadSceneAsync(nameOfSceneToUnload);
                }
                // Give some time for unload operations to process.
                // A more robust system would track all AsyncOperation.isDone.
                yield return new WaitForSeconds(0.1f); 
            }
            // else Debug.Log("SceneLoader: No unwanted scenes to unload.", this); // Uncomment for debugging

            if (loaderScreenObject != null) loaderScreenObject.SetActive(false);
            // Debug.Log("SceneLoader: UnloadUnwantedScenesCoroutine finished.", this); // Uncomment for debugging
        }

        /// <summary>
        /// Checks if a scene with the given name is currently loaded in the SceneManager.
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
// --- END OF FILE SceneLoader.cs ---