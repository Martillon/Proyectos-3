using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // Para LINQ (Contains) si lo usas, aunque no es estrictamente necesario aquí

namespace Scripts.Core
{
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        public static SceneLoader Instance => _instance;

        [Header("Loading Screen")]
        [Tooltip("Optional GameObject to activate during scene transitions.")]
        [SerializeField] private GameObject loaderScreenObject;

        [Header("Scene Configuration")]
        [Tooltip("Name of the main menu scene.")]
        [SerializeField] private string mainMenuSceneName = GameConstants.MainMenuSceneName; // Asegúrate que GameConstants.MainMenuSceneName exista
        [Tooltip("Name of the persistent program/manager scene that should never be unloaded.")]
        [SerializeField] private string programSceneName = GameConstants.ProgramSceneName; // Asegúrate que GameConstants.ProgramSceneName exista
        
        [Header("Game Levels")]
        [Tooltip("Array of scene names for all playable game levels, in order of progression.")]
        public string[] levels; // Estos son NOMBRES de escena

        // Rastrea la escena de gameplay o menú que está actualmente "enfocada" o activa
        public string CurrentLoadedGameplaySceneName { get; private set; }

        private Coroutine _sceneOperationCoroutine; // Para evitar operaciones múltiples simultáneas

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                if (loaderScreenObject != null) loaderScreenObject.SetActive(false);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Determinar la escena inicial
            bool onlyProgramSceneLoaded = SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == programSceneName;
            bool inNonCoreSceneWithoutMenu = SceneManager.GetActiveScene().name != programSceneName &&
                                             SceneManager.GetActiveScene().name != mainMenuSceneName &&
                                             !IsSceneLoaded(mainMenuSceneName);

            if (onlyProgramSceneLoaded || inNonCoreSceneWithoutMenu)
            {
                 LoadMenu(); 
            }
            else if (IsSceneLoaded(mainMenuSceneName) && (levels == null || !levels.Contains(SceneManager.GetActiveScene().name)))
            {
                CurrentLoadedGameplaySceneName = mainMenuSceneName;
                // Asegurar que el menú sea la escena activa si no estamos en un nivel
                Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
                if (menuScene.IsValid() && menuScene.isLoaded && SceneManager.GetActiveScene() != menuScene)
                {
                    SceneManager.SetActiveScene(menuScene);
                }
            }
            else if (levels != null && levels.Contains(SceneManager.GetActiveScene().name))
            {
                CurrentLoadedGameplaySceneName = SceneManager.GetActiveScene().name;
            }
             // Si no se cumple nada, CurrentLoadedGameplaySceneName podría ser null,
             // lo cual se manejará en los métodos de carga.
        }

        public void LoadMenu()
        {
            if (string.IsNullOrEmpty(mainMenuSceneName)) {
                Debug.LogError("SceneLoader: MainMenuSceneName is not set in the inspector!");
                return;
            }
            Debug.Log($"SceneLoader: Attempting to load Main Menu ('{mainMenuSceneName}').");
            InitiateLoadProcess(mainMenuSceneName);
        }

        public void LoadLevelByName(string levelSceneName)
        {
            if (string.IsNullOrEmpty(levelSceneName)) {
                Debug.LogError("SceneLoader: LoadLevelByName called with null or empty scene name.");
                return;
            }
            Debug.Log($"SceneLoader: Attempting to load level by name: '{levelSceneName}'.");
            InitiateLoadProcess(levelSceneName);
        }

        public void LoadLevelByIndex(int levelArrayIndex)
        {
            if (levels == null || levelArrayIndex < 0 || levelArrayIndex >= levels.Length) {
                Debug.LogError($"SceneLoader: LoadLevelByIndex - Invalid index {levelArrayIndex}. Levels array size: {levels?.Length}.");
                return;
            }
            InitiateLoadProcess(levels[levelArrayIndex]);
        }
        
        public void ReloadCurrentLevelScene()
        {
            if (!string.IsNullOrEmpty(CurrentLoadedGameplaySceneName) &&
                CurrentLoadedGameplaySceneName != mainMenuSceneName && // No "recargar" el menú como si fuera un nivel desde aquí
                levels != null && levels.Contains(CurrentLoadedGameplaySceneName))
            {
                Debug.Log($"SceneLoader: Reloading current level '{CurrentLoadedGameplaySceneName}'.");
                InitiateLoadProcess(CurrentLoadedGameplaySceneName);
            }
            else
            {
                Debug.LogWarning($"SceneLoader: Cannot reload. No valid current gameplay level name stored, or it's the main menu: '{CurrentLoadedGameplaySceneName}'. Loading Main Menu instead.");
                LoadMenu();
            }
        }
        
        public void LoadSceneByBuildIndex(int sceneBuildIndex) // Usar con precaución
        {
            if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings) {
                Debug.LogError($"SceneLoader: Invalid scene build index: {sceneBuildIndex}", this); return;
            }
            string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (string.IsNullOrEmpty(sceneName)) {
                Debug.LogError($"SceneLoader: Could not get scene name for build index: {sceneBuildIndex}", this); return;
            }
            InitiateLoadProcess(sceneName);
        }

        private void InitiateLoadProcess(string sceneNameToLoad)
        {
            if (_sceneOperationCoroutine != null)
            {
                Debug.LogWarning($"SceneLoader: Scene operation already in progress. Request for '{sceneNameToLoad}' ignored to prevent conflicts.");
                return;
            }
            _sceneOperationCoroutine = StartCoroutine(LoadAndUnloadProcess(sceneNameToLoad));
        }

        private IEnumerator LoadAndUnloadProcess(string sceneNameToLoad)
        {
            if (loaderScreenObject != null) loaderScreenObject.SetActive(true);
            Debug.Log($"[{Time.frameCount}] SceneLoader: BEGIN LoadAndUnloadProcess for '{sceneNameToLoad}'.");

            // --- PASO 1: Descargar escenas no deseadas ---
            // (Esto incluye la instancia actual de sceneNameToLoad si estamos recargando)
            List<Scene> scenesToUnload = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene currentSceneInManager = SceneManager.GetSceneAt(i);
                if (currentSceneInManager.isLoaded && currentSceneInManager.name != programSceneName)
                {
                    // Si estamos recargando la misma escena, la instancia actual debe descargarse.
                    // Si estamos cargando una escena diferente, cualquier otra escena de nivel/menú que no sea la nueva también debe descargarse.
                    if (currentSceneInManager.name == sceneNameToLoad || // Descargar si es la misma que vamos a (re)cargar
                        currentSceneInManager.name != sceneNameToLoad) // O si es cualquier otra que no sea la ProgramScene y no la nueva
                    {
                        // Corrección: solo descargar si NO es la ProgramScene
                        // Y si es la escena que queremos recargar O si no es la escena que queremos cargar (y tampoco es program)
                        if (currentSceneInManager.name == sceneNameToLoad || 
                           (currentSceneInManager.name != sceneNameToLoad && currentSceneInManager.name != programSceneName))
                        {
                             // El segundo check es redundante por el if exterior.
                             // El objetivo es: descargar todo excepto ProgramScene. Si sceneNameToLoad ya está, se descargará.
                             scenesToUnload.Add(currentSceneInManager);
                        }
                    }
                }
            }
            // Refinamiento de la lógica de descarga:
            // Queremos descargar CUALQUIER escena que no sea programSceneName y no sea la que vamos a cargar A MENOS QUE
            // la que vamos a cargar YA ESTÉ CARGADA (caso de reinicio), en cuyo caso TAMBIÉN la descargamos.
            // Forma más simple: Descargar todo excepto ProgramScene. Luego cargar la nueva.
            scenesToUnload.Clear(); // Empezar de nuevo la lista
             for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene currentSceneInManager = SceneManager.GetSceneAt(i);
                if (currentSceneInManager.isLoaded && currentSceneInManager.name != programSceneName)
                {
                    scenesToUnload.Add(currentSceneInManager);
                }
            }


            if (scenesToUnload.Count > 0)
            {
                List<AsyncOperation> unloadOps = new List<AsyncOperation>();
                Debug.Log($"[{Time.frameCount}] SceneLoader: Unloading {scenesToUnload.Count} scene(s): {string.Join(", ", scenesToUnload.Select(s => s.name))}");
                foreach (Scene sceneToUnload in scenesToUnload)
                {
                    unloadOps.Add(SceneManager.UnloadSceneAsync(sceneToUnload));
                }
                foreach (AsyncOperation op in unloadOps)
                {
                    if (op != null) while (!op.isDone) yield return null;
                }
                Debug.Log($"[{Time.frameCount}] SceneLoader: Finished unloading scenes.");
                yield return null; // Esperar un frame para que SceneManager se actualice bien
            } else {
                Debug.Log($"[{Time.frameCount}] SceneLoader: No scenes to unload (except possibly ProgramScene).");
            }

            // --- PASO 2: Cargar la nueva escena (o recargarla) ---
            Debug.Log($"[{Time.frameCount}] SceneLoader: Loading scene '{sceneNameToLoad}' additively.");
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneNameToLoad, LoadSceneMode.Additive);
            if (loadOperation == null) { // Esto puede pasar si la escena no está en build settings
                Debug.LogError($"SceneLoader: LoadSceneAsync for '{sceneNameToLoad}' returned null. Is the scene in Build Settings and spelled correctly?");
                if (loaderScreenObject != null) loaderScreenObject.SetActive(false);
                _sceneOperationCoroutine = null; 
                yield break; // Salir de la corrutina
            }
            while (!loadOperation.isDone)
            {
                // Opcional: Actualizar progreso de barra de carga aquí
                yield return null;
            }
            Debug.Log($"[{Time.frameCount}] SceneLoader: Scene '{sceneNameToLoad}' finished loading.");

            // --- PASO 3: Establecer la escena cargada como activa ---
            Scene targetScene = SceneManager.GetSceneByName(sceneNameToLoad);
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                SceneManager.SetActiveScene(targetScene);
                CurrentLoadedGameplaySceneName = sceneNameToLoad; 
                Debug.Log($"[{Time.frameCount}] SceneLoader: Scene '{sceneNameToLoad}' set as active. CurrentGameplayScene: {CurrentLoadedGameplaySceneName}");
            }
            else
            {
                Debug.LogError($"SceneLoader: Scene '{sceneNameToLoad}' NOT valid or loaded after attempt. Cannot set active.");
            }

            if (loaderScreenObject != null) loaderScreenObject.SetActive(false);
            _sceneOperationCoroutine = null; 
            Debug.Log($"[{Time.frameCount}] SceneLoader: LoadAndUnloadProcess for '{sceneNameToLoad}' FULLY FINISHED.");
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName) return true; 
            }
            return false;
        }
    }
}