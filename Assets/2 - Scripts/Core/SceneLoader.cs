using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.Core
{
    public class SceneLoader : MonoBehaviour
    {
        // Public static instance for global access
        public static SceneLoader Instance { get; private set; }

        [Header("Optional loading screen")]
        public GameObject loaderScreen;

        [Header("Scene configuration")]
        public string mainMenu = "MainMenu";
        public string programScene = "Program";
        public string[] levels;

        // Tracks which scenes should remain loaded
        public string[] wantedLevels;

        private void Awake()
        {
            // Singleton pattern: ensures only one SceneLoader exists
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        private void Start()
        {
            LoadMenu();
        }

        /// <summary>
        /// Loads the main menu scene additively and removes any unwanted scenes.
        /// </summary>
        public void LoadMenu()
        {
            if (!IsSceneLoaded(mainMenu))
            {
                SceneManager.LoadScene(mainMenu, LoadSceneMode.Additive);
            }

            wantedLevels = new[] { mainMenu };
            StartCoroutine(RemoveUnwantedScenes(wantedLevels));
        }

        /// <summary>
        /// Loads a level by index from the levels array and removes any unwanted scenes.
        /// </summary>
        /// <param name="levelLoad">Index of the level to load</param>
        public void LoadLevel(int levelLoad)
        {
            if (!IsSceneLoaded(levels[levelLoad]))
            {
                SceneManager.LoadScene(levels[levelLoad], LoadSceneMode.Additive);
            }

            wantedLevels = new[] { levels[levelLoad] };
            StartCoroutine(RemoveUnwantedScenes(wantedLevels));
        }

        /// <summary>
        /// Unloads all currently loaded scenes except those specified in the wanted list.
        /// The program scene is always kept loaded.
        /// </summary>
        /// <param name="wantedScenes">Array of scene names to keep</param>
        private IEnumerator RemoveUnwantedScenes(string[] wantedScenes)
        {
            Debug.Log("Removing Scenes");

            // Wait one frame to ensure that newly loaded scenes are fully registered
            yield return null;

            List<string> unwantedScenes = new List<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sceneName = SceneManager.GetSceneAt(i).name;
                bool removeScene = true;

                foreach (var wanted in wantedScenes)
                {
                    if (sceneName == wanted)
                    {
                        removeScene = false;
                        break;
                    }
                }

                if (removeScene)
                {
                    unwantedScenes.Add(sceneName);
                }
            }

            if (loaderScreen != null)
                loaderScreen.SetActive(true);

            foreach (var scene in unwantedScenes)
            {
                Debug.Log("Unloading: " + scene);
            }

            foreach (var scene in unwantedScenes)
            {
                if (scene != programScene)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }

            if (loaderScreen != null)
                loaderScreen.SetActive(false);
        }

        /// <summary>
        /// Checks whether a scene is currently loaded in the scene manager.
        /// </summary>
        /// <param name="sceneName">Name of the scene to check</param>
        /// <returns>True if the scene is loaded</returns>
        private static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (sceneName == SceneManager.GetSceneAt(i).name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
