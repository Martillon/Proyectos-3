// --- START OF FILE GameOverUIController.cs ---
using System.Collections;
using Scripts.Core; 
using Scripts.Core.Audio; 
using UnityEngine;
using Scripts.Player.Core; 
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using Scripts.Items.Checkpoint; // Added for CheckpointManager

namespace Scripts.UI 
{
    public class GameOverUIController : MonoBehaviour
    {
        // ... (campos existentes sin cambios) ...
        [Header("UI Panel References")]
        [Tooltip("The main CanvasGroup for the entire Game Over screen panel. Used for fading.")]
        [SerializeField] private CanvasGroup gameOverScreenCanvasGroup;
        [Tooltip("The GameObject containing the 'Game Over' or 'Wasted' message text/image.")]
        [SerializeField] private GameObject messageGroup; // e.g., "WASTED" text
        [Tooltip("The GameObject containing the actionable buttons (Restart, Main Menu).")]
        [SerializeField] private GameObject buttonsGroup;
        [Tooltip("Reference to the in-game HUD GameObject, to be hidden when Game Over screen appears.")]
        [SerializeField] private GameObject inGameHUD;

        [Header("Animation & Timing Settings")]
        [Tooltip("Duration (in seconds) for the Game Over screen to fade in.")]
        [SerializeField] private float fadeInDuration = 1.5f;
        [Tooltip("Delay (in seconds) after the message appears before the buttons become visible.")]
        [SerializeField] private float delayAfterMessageBeforeButtons = 1.0f;

        [Header("Button References")]
        [SerializeField] private Button btn_RestartLevel;
        [SerializeField] private Button btn_MainMenu;
        [SerializeField] private Button firstSelectedButtonOnGameOver;

        [Header("Audio Feedback (Optional)")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;


        private void Awake()
        {
            if (gameOverScreenCanvasGroup != null)
            {
                gameOverScreenCanvasGroup.alpha = 0f;
                gameOverScreenCanvasGroup.interactable = false;
                gameOverScreenCanvasGroup.blocksRaycasts = false;
                gameOverScreenCanvasGroup.gameObject.SetActive(false); 
            }
            if (messageGroup != null) messageGroup.SetActive(false);
            if (buttonsGroup != null) buttonsGroup.SetActive(false);
        }

        private void OnEnable()
        {
            PlayerEvents.OnPlayerDeath += HandlePlayerFinalDeath;
            btn_RestartLevel?.onClick.AddListener(OnRestartLevelClicked);
            btn_MainMenu?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerDeath -= HandlePlayerFinalDeath;
            btn_RestartLevel?.onClick.RemoveListener(OnRestartLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void HandlePlayerFinalDeath()
        {
            if (inGameHUD != null)
            {
                inGameHUD.SetActive(false); 
            }

            if (gameOverScreenCanvasGroup != null)
            {
                StartCoroutine(ShowGameOverSequence());
            }
            // else: Fallback si no hay canvas group (ya manejado)
        }

        private IEnumerator ShowGameOverSequence()
        {
            gameOverScreenCanvasGroup.gameObject.SetActive(true);
            gameOverScreenCanvasGroup.alpha = 0f;

            if (messageGroup != null)
            {
                messageGroup.SetActive(true);
            }
            
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                gameOverScreenCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                yield return null;
            }
            gameOverScreenCanvasGroup.alpha = 1f; 

            yield return new WaitForSecondsRealtime(delayAfterMessageBeforeButtons);

            if (buttonsGroup != null)
            {
                buttonsGroup.SetActive(true);
                InputManager.Instance?.EnableUIControls();
                firstSelectedButtonOnGameOver?.Select(); 
            }
            
            gameOverScreenCanvasGroup.interactable = true;
            gameOverScreenCanvasGroup.blocksRaycasts = true;
            
            Time.timeScale = 0f; 
        }

        private void OnRestartLevelClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f; 
            CheckpointManager.ResetCheckpointData(); // Crucial

            int currentSceneBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            SceneLoader.Instance?.LoadSceneByBuildIndex(currentSceneBuildIndex);
            // Debug.Log($"GameOverUIController: Restart Level button clicked. Reloading scene with Build Index: {currentSceneBuildIndex}."); // Uncomment for debugging
        }

        private void OnMainMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f; 
            CheckpointManager.ResetCheckpointData(); // Use CheckpointManager
            SceneLoader.Instance?.LoadMenu();
        }
    }
}
// --- END OF FILE GameOverUIController.cs ---
