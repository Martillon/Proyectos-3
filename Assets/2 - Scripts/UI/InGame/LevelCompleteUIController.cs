using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Core.Checkpoint;
using Scripts.Player.Core;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Manages the "Level Complete" screen, which appears after the player reaches a level exit.
    /// It handles the display sequence, audio feedback, and navigation options.
    /// </summary>
    public class LevelCompleteUIController : MonoBehaviour
    {
        [Header("UI Panel References")]
        [Tooltip("The CanvasGroup for the entire Level Complete panel. Used for fading.")]
        [SerializeField] private CanvasGroup levelCompleteCanvasGroup;
        [Tooltip("GameObject containing the 'Victory' message text or image.")]
        [SerializeField] private GameObject messageGroup;
        [Tooltip("GameObject containing the navigation buttons.")]
        [SerializeField] private GameObject buttonsGroup;
        [Tooltip("Reference to the in-game HUD GameObject to be hidden.")]
        [SerializeField] private GameObject inGameHUD;

        [Header("Timing & Animation")]
        [Tooltip("Delay after the player's victory animation starts before this UI sequence begins.")]
        [SerializeField] private float initialDelay = 1.0f;
        [Tooltip("Duration of the main panel fade-in.")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [Tooltip("Delay after the panel fades in before the 'Continue' button appears.")]
        [SerializeField] private float buttonsDelay = 0.75f;

        [Header("Button References")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [Tooltip("The button to be selected by default when the panel appears.")]
        [SerializeField] private Button firstSelectedButton;

        [Header("Audio")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;
        [SerializeField] private Sounds victoryStingerSound;
        [SerializeField] private AudioSource uiAudioSource;

        private string _completedLevelIdentifier;
        private Coroutine _showSequenceCoroutine;

        private void Awake()
        {
            // Initial setup to ensure the panel is hidden correctly.
            if (levelCompleteCanvasGroup != null)
            {
                levelCompleteCanvasGroup.alpha = 0f;
                levelCompleteCanvasGroup.interactable = false;
                levelCompleteCanvasGroup.blocksRaycasts = false;
                levelCompleteCanvasGroup.gameObject.SetActive(false);
            }
            if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            PlayerEvents.OnLevelCompleted += HandleLevelCompleted;
            continueButton?.onClick.AddListener(OnContinueClicked);
            retryButton?.onClick.AddListener(OnRetryClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            PlayerEvents.OnLevelCompleted -= HandleLevelCompleted;
            continueButton?.onClick.RemoveListener(OnContinueClicked);
            retryButton?.onClick.RemoveListener(OnRetryClicked);
            mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void HandleLevelCompleted(string levelId)
        {
            if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);
            _completedLevelIdentifier = levelId;
            inGameHUD?.SetActive(false);
            _showSequenceCoroutine = StartCoroutine(ShowLevelCompleteSequence());
        }

        private IEnumerator ShowLevelCompleteSequence()
        {
            // The LevelExit triggers the player's victory animation. This coroutine starts in parallel.
            victoryStingerSound?.Play(uiAudioSource);
            InputManager.Instance?.DisableAllControls();

            yield return new WaitForSecondsRealtime(initialDelay);

            // Activate and fade in the panel
            levelCompleteCanvasGroup.gameObject.SetActive(true);
            messageGroup?.SetActive(true);
            
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                levelCompleteCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(buttonsDelay);
            
            // Configure and show buttons
            SetupButtons();
            buttonsGroup?.SetActive(true);
            
            // Enable UI interaction
            levelCompleteCanvasGroup.interactable = true;
            levelCompleteCanvasGroup.blocksRaycasts = true;
            InputManager.Instance?.EnableUIControls();
            firstSelectedButton?.Select();
            
            Time.timeScale = 0f; // Pause game time once UI is fully interactive
        }

        private void SetupButtons()
        {
            if (continueButton == null || SceneLoader.Instance?.levels == null)
            {
                continueButton?.gameObject.SetActive(false);
                return;
            }

            int currentIndex = System.Array.IndexOf(SceneLoader.Instance.levels, _completedLevelIdentifier);
            bool isThereNextLevel = currentIndex > -1 && currentIndex + 1 < SceneLoader.Instance.levels.Length;

            continueButton.gameObject.SetActive(isThereNextLevel);

            // Adjust default selection if "Continue" is hidden
            if (!isThereNextLevel && firstSelectedButton == continueButton)
            {
                firstSelectedButton = retryButton;
            }
        }
        
        private void CleanupAndResume()
        {
            Time.timeScale = 1f;
            if (levelCompleteCanvasGroup != null)
            {
                levelCompleteCanvasGroup.gameObject.SetActive(false);
            }
        }

        private void OnContinueClicked()
        {
            uiSoundFeedback?.PlayClick();
            CleanupAndResume();
            
            int currentIndex = System.Array.IndexOf(SceneLoader.Instance.levels, _completedLevelIdentifier);
            string nextLevel = SceneLoader.Instance.levels[currentIndex + 1];
            
            SceneLoader.Instance.LoadLevelByName(nextLevel);
        }

        private void OnRetryClicked()
        {
            uiSoundFeedback?.PlayClick();
            CleanupAndResume();
            CheckpointManager.ResetCheckpointData();
            SceneLoader.Instance.LoadLevelByName(_completedLevelIdentifier);
        }

        private void OnMainMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            CleanupAndResume();
            SceneLoader.Instance.LoadMenu();
        }
    }
}
