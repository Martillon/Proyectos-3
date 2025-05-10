// --- START OF FILE LevelCompleteUIController.cs (NUEVO SCRIPT) ---
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // If using TextMeshPro for messages
using Scripts.Core; // For SceneLoader, InputManager, GameConstants
using Scripts.Player.Core; // For PlayerEvents (or GameEvents)
using Scripts.Core.Audio; // For UIAudioFeedback
using Scripts.Checkpoints;
using Scripts.Items.Checkpoint; // For CheckpointManager
using UnityEngine.SceneManagement; // For SceneManager (Unity's)

namespace Scripts.UI.InGame // Or Scripts.UI.InGame or Scripts.UI.PostLevel
{
    /// <summary>
    /// Manages the UI screen displayed when a player successfully completes a level.
    /// Handles visual effects (like background desaturation), player animations (via events/Animator),
    /// "Mission Accomplished" message, and buttons for progression.
    /// </summary>
    public class LevelCompleteUIController : MonoBehaviour
    {
        [Header("UI Panel References")]
        [Tooltip("The main CanvasGroup for the entire Level Complete screen panel. Used for fading.")]
        [SerializeField] private CanvasGroup levelCompleteScreenCanvasGroup;
        [Tooltip("Optional: An Image used as a background overlay (e.g., for desaturation or color tint).")]
        [SerializeField] private Image backgroundOverlayImage;
        [Tooltip("GameObject containing the 'Mission Accomplished' message text/image.")]
        [SerializeField] private GameObject messageGroup;
        [Tooltip("GameObject containing the actionable buttons (Continue, Retry, Main Menu).")]
        [SerializeField] private GameObject buttonsGroup;
        [Tooltip("Reference to the in-game HUD GameObject, to be hidden.")]
        [SerializeField] private GameObject inGameHUD;

        [Header("Animation & Timing Settings")]
        [Tooltip("Duration for the screen/overlay to fade in.")]
        [SerializeField] private float screenFadeInDuration = 1.0f;
        [Tooltip("Delay after screen fade before the 'Mission Accomplished' message appears.")]
        [SerializeField] private float delayBeforeMessage = 0.5f;
        [Tooltip("Delay after the message appears before the buttons become visible.")]
        [SerializeField] private float delayAfterMessageBeforeButtons = 1.5f;
        [Tooltip("Target alpha for the background overlay when active.")]
        [SerializeField] private float backgroundOverlayTargetAlpha = 0.7f;

        [Header("Player Animation Control (Optional)")]
        [Tooltip("If true, assumes there's a player animation to trigger (e.g., 'VictoryPose').")]
        [SerializeField] private bool triggerPlayerVictoryAnimation = true;
        [Tooltip("Name of the trigger in the Player's Animator for the victory pose/animation.")]
        [SerializeField] private string playerVictoryAnimationTrigger = "Victory"; // Example name

        [Header("Button References")]
        [SerializeField] private Button btn_Continue; // To next level
        [SerializeField] private Button btn_RetryLevel;
        [SerializeField] private Button btn_MainMenu;
        [SerializeField] private Button firstSelectedButtonOnComplete;

        [Header("Audio Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;
        [SerializeField] private Sounds levelCompleteMusicOrStinger; // Sound to play on level complete
        [SerializeField] private AudioSource uiAudioSource; // For playing the stinger/music

        private string completedLevelIdentifier;
        private Animator playerAnimator;

        private void Awake()
        {
            if (levelCompleteScreenCanvasGroup != null)
            {
                levelCompleteScreenCanvasGroup.alpha = 0f;
                levelCompleteScreenCanvasGroup.interactable = false;
                levelCompleteScreenCanvasGroup.blocksRaycasts = false;
                levelCompleteScreenCanvasGroup.gameObject.SetActive(false);
            }
            if (messageGroup != null) messageGroup.SetActive(false);
            if (buttonsGroup != null) buttonsGroup.SetActive(false);
            if (backgroundOverlayImage != null)
            {
                backgroundOverlayImage.gameObject.SetActive(false);
                Color c = backgroundOverlayImage.color;
                c.a = 0;
                backgroundOverlayImage.color = c;
            }
            if(uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            PlayerEvents.OnLevelCompleted += HandleLevelSuccessfullyCompleted; // Or GameEvents.

            btn_Continue?.onClick.AddListener(OnContinueClicked);
            btn_RetryLevel?.onClick.AddListener(OnRetryLevelClicked);
            btn_MainMenu?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            PlayerEvents.OnLevelCompleted -= HandleLevelSuccessfullyCompleted;

            btn_Continue?.onClick.RemoveListener(OnContinueClicked);
            btn_RetryLevel?.onClick.RemoveListener(OnRetryLevelClicked);
            btn_MainMenu?.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void HandleLevelSuccessfullyCompleted(string levelId)
        {
            completedLevelIdentifier = levelId;
            // Debug.Log($"LevelCompleteUIController: Received LevelCompleted event for '{completedLevelIdentifier}'. Starting sequence.", this); // Uncomment for debugging

            if (inGameHUD != null) inGameHUD.SetActive(false);

            // Attempt to find player animator for victory pose
            GameObject playerObject = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
            if (playerObject != null) playerAnimator = playerObject.GetComponent<Animator>();

            StartCoroutine(ShowLevelCompleteSequence());
        }

        private IEnumerator ShowLevelCompleteSequence()
        {
            // 0. Play Level Complete Stinger/Music
            levelCompleteMusicOrStinger?.Play(uiAudioSource);

            // 1. Player Victory Animation (if configured)
            if (triggerPlayerVictoryAnimation && playerAnimator != null && !string.IsNullOrEmpty(playerVictoryAnimationTrigger))
            {
                playerAnimator.SetTrigger(playerVictoryAnimationTrigger);
                // Optional: wait for a part of player animation before UI starts heavily
                // yield return new WaitForSeconds(0.5f); 
            }

            // 2. Activate the main panel and background overlay (initially transparent)
            levelCompleteScreenCanvasGroup.gameObject.SetActive(true);
            levelCompleteScreenCanvasGroup.alpha = 0f; // Will fade in the whole group if desired, or just elements

            if (backgroundOverlayImage != null)
            {
                backgroundOverlayImage.gameObject.SetActive(true);
                StartCoroutine(FadeImageAlpha(backgroundOverlayImage, backgroundOverlayTargetAlpha, screenFadeInDuration));
            }
            // If not using a separate overlay, can fade the main canvas group directly
            // else { StartCoroutine(FadeCanvasGroupAlpha(levelCompleteScreenCanvasGroup, 1f, screenFadeInDuration)); }


            // Wait for screen fade/effect to establish
            yield return new WaitForSecondsRealtime(screenFadeInDuration); // Or a specific delay for the effect

            // 3. Show "Mission Accomplished" message
            if (messageGroup != null)
            {
                yield return new WaitForSecondsRealtime(delayBeforeMessage);
                messageGroup.SetActive(true); // Or animate its appearance
            }

            // 4. Wait before showing buttons
            yield return new WaitForSecondsRealtime(delayAfterMessageBeforeButtons);

            // 5. Show buttons, enable UI controls
            if (buttonsGroup != null)
            {
                buttonsGroup.SetActive(true);
                InputManager.Instance?.EnableUIControls(); // Switch to UI input map
                firstSelectedButtonOnComplete?.Select();
            }
            
            levelCompleteScreenCanvasGroup.interactable = true;
            levelCompleteScreenCanvasGroup.blocksRaycasts = true;

            // Unlike Game Over, we usually don't set Time.timeScale = 0 here,
            // as the game has "ended" for this level. It will transition on button press.
            // However, if background elements should freeze, you might consider it.
            // For now, assume Time.timeScale remains 1 or is handled by SceneLoader on scene change.
        }

        private IEnumerator FadeImageAlpha(Image image, float targetAlpha, float duration)
        {
            float startAlpha = image.color.a;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                Color c = image.color;
                c.a = newAlpha;
                image.color = c;
                yield return null;
            }
            Color finalColor = image.color;
            finalColor.a = targetAlpha;
            image.color = finalColor;
        }
        // Optional: IEnumerator FadeCanvasGroupAlpha(CanvasGroup cg, float targetAlpha, float duration) { ... }

        private void OnContinueClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f; // Ensure time is normal before loading
            // Logic to load the next level
            if (SceneLoader.Instance != null && LevelProgressionManager.Instance != null && !string.IsNullOrEmpty(completedLevelIdentifier))
            {
                int currentIndex = System.Array.IndexOf(SceneLoader.Instance.levels, completedLevelIdentifier);
                if (currentIndex != -1 && currentIndex + 1 < SceneLoader.Instance.levels.Length)
                {
                    string nextLevelIdentifier = SceneLoader.Instance.levels[currentIndex + 1];
                    if (LevelProgressionManager.Instance.IsLevelUnlocked(nextLevelIdentifier))
                    {
                        SceneLoader.Instance.LoadLevelByName(nextLevelIdentifier);
                    }
                    else
                    {
                        // Should not happen if CompleteLevel unlocks correctly
                        Debug.LogWarning("LevelCompleteUIController: Next level not unlocked. Returning to menu.", this);
                        SceneLoader.Instance.LoadMenu();
                    }
                }
                else
                {
                    // No more levels, game completed or end of current sequence
                    // Debug.Log("LevelCompleteUIController: No next level in sequence. Returning to menu or showing game complete screen.", this); // Uncomment for debugging
                    // TODO: Implement a proper "Game Beat" screen/sequence if this is the absolute last level.
                    SceneLoader.Instance.LoadMenu(); 
                }
            }
            else SceneLoader.Instance?.LoadMenu(); // Fallback
        }

        private void OnRetryLevelClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f;
            CheckpointManager.ResetCheckpointData(); // Reset checkpoints for a fresh retry
            if (!string.IsNullOrEmpty(completedLevelIdentifier))
            {
                SceneLoader.Instance?.LoadLevelByName(completedLevelIdentifier); // Reload the same level
            }
            else // Fallback if identifier somehow lost
            {
                 SceneLoader.Instance?.LoadSceneByBuildIndex(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnMainMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            Time.timeScale = 1f;
            // CheckpointManager.ResetCheckpointData(); // Already handled by MainMenu or SceneLoader's start
            SceneLoader.Instance?.LoadMenu();
        }
    }
}
// --- END OF FILE LevelCompleteUIController.cs ---
