using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Player.Core;
using TMPro;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Manages the "Bounty Complete" screen, which appears after the final stage of a bounty.
    /// It displays a victory message and a button to return to the main menu.
    /// </summary>
    public class LevelCompleteUIController : MonoBehaviour
    {
        [Header("UI Panel References")]
        [Tooltip("The CanvasGroup for the entire panel, used for fading.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [Tooltip("The TextMeshPro field to display the 'Bounty Complete: [Bounty Name]' message.")]
        [SerializeField] private TMP_Text bountyNameText;
        [Tooltip("The button that returns the player to the main menu.")]
        [SerializeField] private Button returnToMenuButton;
        [Tooltip("Reference to the in-game HUD GameObject to be hidden when this UI appears.")]
        [SerializeField] private GameObject inGameHUD;

        [Header("Timing & Animation")]
        [Tooltip("Delay after the victory event is fired before this UI sequence begins.")]
        [SerializeField] private float initialDelay = 1.0f;
        [Tooltip("Duration of the main panel fade-in.")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;
        [SerializeField] private Sounds victoryStingerSound;
        [SerializeField] private AudioSource uiAudioSource;

        private Coroutine _showSequenceCoroutine;

        private void Awake()
        {
            // Ensure the panel is hidden on start.
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.gameObject.SetActive(false);
            }
            if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            // Subscribe to the event fired by LevelExit when a bounty is fully completed.
            PlayerEvents.OnLevelCompleted += HandleBountyCompleted;
            returnToMenuButton?.onClick.AddListener(OnReturnToMenuClicked);
        }

        private void OnDisable()
        {
            PlayerEvents.OnLevelCompleted -= HandleBountyCompleted;
            returnToMenuButton?.onClick.RemoveListener(OnReturnToMenuClicked);
        }

        /// <summary>
        /// This method is triggered by the OnLevelCompleted event.
        /// It starts the UI sequence to show the "Bounty Complete" screen.
        /// </summary>
        /// <param name="completedBountyTitle">The title of the bounty that was just finished.</param>
        private void HandleBountyCompleted(string completedBountyTitle)
        {
            // Stop any previous sequence to prevent conflicts.
            if (_showSequenceCoroutine != null) StopCoroutine(_showSequenceCoroutine);

            // Hide the in-game HUD so it doesn't overlap with this screen.
            inGameHUD?.SetActive(false);

            _showSequenceCoroutine = StartCoroutine(ShowBountyCompleteSequence(completedBountyTitle));
        }

        /// <summary>
        /// The coroutine that handles the entire animation and display sequence for the panel.
        /// </summary>
        private IEnumerator ShowBountyCompleteSequence(string bountyTitle)
        {
            // Play a victory sound and ensure player controls are disabled.
            victoryStingerSound?.Play(uiAudioSource);
            InputManager.Instance?.DisableAllControls();

            // Wait for a moment to let the player's victory animation or effect play out.
            yield return new WaitForSecondsRealtime(initialDelay);

            // --- Animate Panel ---
            // Update the text to show which bounty was completed.
            if (bountyNameText != null)
            {
                bountyNameText.text = $"BOUNTY COMPLETE:\n<color=yellow>{bountyTitle}</color>";
            }
            
            // Activate the panel and fade it in.
            panelCanvasGroup.gameObject.SetActive(true);
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                yield return null;
            }

            // --- Enable Interaction ---
            // Once faded in, make the panel interactive, enable UI controls,
            // and automatically select the "Return to Menu" button.
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
            InputManager.Instance?.EnableUIControls();
            returnToMenuButton?.Select();
            
            // Pause game time. This is good practice in case any background animations are still running.
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// Called when the "Return to Menu" button is clicked.
        /// </summary>
        private void OnReturnToMenuClicked()
        {
            uiSoundFeedback?.PlayClick();
            
            // IMPORTANT: Reset the time scale before changing scenes.
            Time.timeScale = 1f;

            // Tell the SceneLoader to load the main menu.
            SceneLoader.Instance.LoadMenu();
        }
    }
}
