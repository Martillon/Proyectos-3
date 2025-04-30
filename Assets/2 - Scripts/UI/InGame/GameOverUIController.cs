using System.Collections;
using Scripts.Core;
using UnityEngine;
using Scripts.Player.Core;
using UnityEngine.UI;

namespace Scripts.UI
{
    /// <summary>
    /// Manages the UI shown when the player dies.
    /// Handles fade-in of the panel and delayed display of buttons.
    /// </summary>
    public class GameOverUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject hud;
        [SerializeField] private CanvasGroup deathScreen;
        [SerializeField] private GameObject buttonsGroup;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float delayBeforeButtons = 1f;
        
        [Header("Menu Buttons")]
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;

        private void OnEnable()
        {
            PlayerEvents.OnPlayerDeath += ShowGameOverUI;
            
            btn_RestartLevel?.onClick.AddListener(RestartLevel);
            btn_MainMenu?.onClick.AddListener(GoToMainMenu);
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerDeath -= ShowGameOverUI;
            
            btn_RestartLevel?.onClick.RemoveListener(RestartLevel);
            btn_MainMenu?.onClick.RemoveListener(GoToMainMenu);
        }

        private void ShowGameOverUI()
        {
            if (hud != null)
                hud.SetActive(false);

            if (deathScreen != null)
                StartCoroutine(FadeInPanel());
        }

        private IEnumerator FadeInPanel()
        {
            float t = 0f;
            deathScreen.alpha = 0f;
            deathScreen.gameObject.SetActive(true);

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                deathScreen.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }

            deathScreen.interactable = true;
            deathScreen.blocksRaycasts = true;

            yield return new WaitForSecondsRealtime(delayBeforeButtons);

            if (buttonsGroup != null)
                buttonsGroup.SetActive(true);
            
            InputManager.Instance.EnableUIControls();
        }
        
        private void RestartLevel()
        {
            Time.timeScale = 1f;
            InputManager.Instance.EnablePlayerControls();

            // Probably the sceneLoader will maintain the scene intact.
            int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            SceneLoader.Instance.LoadLevel(currentSceneIndex);
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            InputManager.Instance.EnablePlayerControls();

            SceneLoader.Instance.LoadMenu();
        }
    }
}
