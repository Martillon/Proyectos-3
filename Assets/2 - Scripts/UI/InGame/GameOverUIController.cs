using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Core.Checkpoint;
using Scripts.Player.Core;

namespace Scripts.UI
{
    /// <summary>
    /// Manages the Game Over screen sequence, which is triggered by the OnPlayerDeath event.
    /// Fades in the screen, shows a message, then presents options to the player.
    /// </summary>
    public class GameOverUIController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The CanvasGroup for the entire Game Over panel, used for fading.")]
        [SerializeField] private CanvasGroup gameOverCanvasGroup;
        [Tooltip("The GameObject containing the 'Game Over' message text or image.")]
        [SerializeField] private GameObject messageGroup;
        [Tooltip("The GameObject containing the UI buttons.")]
        [SerializeField] private GameObject buttonsGroup;
        [Tooltip("The in-game HUD object, which will be hidden.")]
        [SerializeField] private GameObject inGameHUD;

        [Header("Animation & Timing")]
        [Tooltip("Duration for the Game Over screen to fade in.")]
        [SerializeField] private float fadeInDuration = 1.5f;
        [Tooltip("Delay after the message appears before the buttons are shown.")]
        [SerializeField] private float buttonDelay = 1.0f;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [Tooltip("The button to be selected by default.")]
        [SerializeField] private Button firstSelectedButton;
        
        [Header("Audio")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;

        private void Awake()
        {
            // Initialize the panel to be fully hidden.
            if (gameOverCanvasGroup != null)
            {
                gameOverCanvasGroup.alpha = 0f;
                gameOverCanvasGroup.interactable = false;
                gameOverCanvasGroup.blocksRaycasts = false;
                gameOverCanvasGroup.gameObject.SetActive(false);
            }
            // Ensure child groups are also hidden initially.
            messageGroup?.SetActive(false);
            buttonsGroup?.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerEvents.OnPlayerDeath += OnPlayerFinalDeath;
            restartButton?.onClick.AddListener(OnRestartClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerDeath -= OnPlayerFinalDeath;
            restartButton?.onClick.RemoveListener(OnRestartClicked);
            mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void OnPlayerFinalDeath()
        {
            // Hide the main game HUD and start the game over sequence.
            inGameHUD?.SetActive(false);
            if (gameOverCanvasGroup != null)
            {
                StartCoroutine(ShowGameOverSequence());
            }
        }

        private IEnumerator ShowGameOverSequence()
        {
            // The PlayerHealthSystem handles the camera zoom and initial death animation.
            // This coroutine starts after that.
            
            gameOverCanvasGroup.gameObject.SetActive(true);
            messageGroup?.SetActive(true);

            // Fade in the entire panel.
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                // Use unscaledDeltaTime so the fade works even if Time.timeScale is 0.
                elapsedTime += Time.unscaledDeltaTime;
                gameOverCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                yield return null;
            }

            // Wait before showing buttons.
            yield return new WaitForSecondsRealtime(buttonDelay);

            // Show buttons and enable UI controls.
            buttonsGroup?.SetActive(true);
            InputManager.Instance?.EnableUIControls();
            firstSelectedButton?.Select();
            
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
            
            // Pausing time after the sequence makes it feel more definitive.
            Time.timeScale = 0f;
        }

        private void OnRestartClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f;
            CheckpointManager.ResetCheckpointData();
            
            // Reload the current level by its build index or name.
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneLoader.Instance?.LoadLevelByName(currentSceneName);
        }

        private void OnMainMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f;
            CheckpointManager.ResetCheckpointData();
            SceneLoader.Instance?.LoadMenu();
        }
    }
}
