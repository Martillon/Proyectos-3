using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.Core
{
    /// <summary>
    /// Manages loading and unloading of scenes. Designed to work with a persistent "Program"
    /// scene and load/unload gameplay or menu scenes additively.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Loading Screen")]
        [Tooltip("Optional. A GameObject to activate during scene transitions.")]
        [SerializeField] private GameObject loadingScreenObject;
        [Tooltip("The minimum time in seconds the loading screen will be displayed, to prevent jarringly fast transitions.")]
        [SerializeField] private float minLoadingScreenDisplayTime = 5f;

        [Header("Scene Configuration")]
        [Tooltip("The name of the main menu scene.")]
        [SerializeField] private string mainMenuSceneName = GameConstants.MainMenuSceneName;
        [Tooltip("The name of the persistent manager scene that should never be unloaded.")]
        [SerializeField] private string persistentSceneName = GameConstants.ProgramSceneName;
        
        public string _currentSceneName;
        
        private string CurrentGameplaySceneName { get; set; }

        private Coroutine _sceneOperationCoroutine;
        
        /// <summary>
        /// Invoked when a new scene has been fully loaded, set as active,
        /// and all transitions (fades, loading screens) are complete.
        /// Game systems should subscribe to this to know when it's safe to "start".
        /// </summary>
        public static event Action OnSceneReady;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            loadingScreenObject?.SetActive(false);
        }

        private void Start()
        {
            // This logic handles starting the game from the editor in any scene.
            // It ensures the Main Menu is loaded if no other gameplay/menu scene is present.
            bool isOnlyPersistentSceneLoaded = SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == persistentSceneName;

            if (isOnlyPersistentSceneLoaded)
            {
                // If we start the game from the persistent scene, load the main menu.
                LoadMenu();
            }
            else
            {
                // If we started in a different scene, figure out what it is.
                _currentSceneName = SceneManager.GetActiveScene().name;
                // If the active scene is not the menu or a known level, it might be an unmanaged scene.
                // The CurrentGameplaySceneName will remain as its last valid value or null.
            }
        }

        public void LoadMenu()
        {
            if (string.IsNullOrEmpty(mainMenuSceneName))
            {
                Debug.LogError("SceneLoader: MainMenuSceneName is not set!", this);
                return;
            }
            InitiateLoadProcess(mainMenuSceneName);
        }

        public void LoadLevelByName(string levelSceneName)
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogError("SceneLoader: LoadLevelByName called with an empty scene name.", this);
                return;
            }
            InitiateLoadProcess(levelSceneName);
        }

        public void ReloadCurrentLevel()
        {
            if (!string.IsNullOrEmpty(_currentSceneName) && _currentSceneName != mainMenuSceneName)
            {
                InitiateLoadProcess(_currentSceneName);
            }
            else
            {
                Debug.LogWarning("SceneLoader: No valid current level to reload. Loading Main Menu.", this);
                LoadMenu();
            }
        }

        private void InitiateLoadProcess(string sceneNameToLoad)
        {
            if (_sceneOperationCoroutine != null)
            {
                Debug.LogWarning($"SceneLoader: Scene operation already in progress. Request for '{sceneNameToLoad}' ignored.", this);
                return;
            }
            _sceneOperationCoroutine = StartCoroutine(LoadAndUnloadProcess(sceneNameToLoad));
        }

        private IEnumerator LoadAndUnloadProcess(string sceneNameToLoad)
        {
            // --- Start of Transition ---
            float loadStartTime = Time.realtimeSinceStartup; // Use realtime for unscaled timing

            loadingScreenObject?.SetActive(true);
            // We fade to black, and BEHIND the black screen, we do the loading.
            yield return ScreenFader.Instance?.FadeToBlack();

            // --- Step 1: Unload all scenes except the persistent one ---
            List<AsyncOperation> unloadOperations = new List<AsyncOperation>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name != persistentSceneName)
                {
                    unloadOperations.Add(SceneManager.UnloadSceneAsync(scene));
                }
            }

            foreach (var op in unloadOperations)
            {
                if (op != null) while (!op.isDone) yield return null;
            }
            yield return null; // Wait a frame for SceneManager to update.

            // --- Step 2: Load the new scene additively ---
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneNameToLoad, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"SceneLoader: LoadSceneAsync for '{sceneNameToLoad}' returned null. Is the scene in Build Settings?", this);
                // Handle error gracefully
                loadingScreenObject?.SetActive(false);
                yield return ScreenFader.Instance?.FadeToClear();
                _sceneOperationCoroutine = null;
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }
            
            // --- Step 3: Set the newly loaded scene as active ---
            Scene newScene = SceneManager.GetSceneByName(sceneNameToLoad);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
                _currentSceneName = sceneNameToLoad; // <-- ADD THIS LINE
                CurrentGameplaySceneName = sceneNameToLoad;
            }
            else
            {
                Debug.LogError($"SceneLoader: Scene '{sceneNameToLoad}' was not valid after loading.", this);
            }

            // --- Timing Control ---
            float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
            if (elapsedTime < minLoadingScreenDisplayTime)
            {
                // Wait for the remaining time.
                yield return new WaitForSecondsRealtime(minLoadingScreenDisplayTime - elapsedTime);
            }
            
            // --- End of Transition ---
            // Now that we've waited, we can fade back in.
            yield return ScreenFader.Instance?.FadeToClear();
            loadingScreenObject?.SetActive(false);
            
            _sceneOperationCoroutine = null;
            
            // Notify that the scene is ready.
            Debug.Log($"SceneLoader: Scene '{sceneNameToLoad}' is now ready. Firing OnSceneReady event.");
            OnSceneReady?.Invoke();
        }
        
        /// <summary>
        /// A debug-only method to manually fire the OnSceneReady event.
        /// This allows test scenes to be started without a full scene transition.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")] // This attribute ensures this method is ONLY included in Editor builds
        public static void Debug_FireSceneReady()
        {
            Debug.LogWarning("DEBUG: Manually firing OnSceneReady event.");
            OnSceneReady?.Invoke();
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }
    }
}