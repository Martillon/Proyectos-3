using Scripts.Core;
using Scripts.Core.Progression;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Scripts.UI.MainMenu.Apps
{
    /// <summary>
    /// Manages the visuals and data for a single Wanted Poster.
    /// It can now also handle "placeholder" bounties which are defined by having no levels.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WantedPosterUI : MonoBehaviour
    {
        [Header("UI References (Content)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image posterBackgroundImage;
        [SerializeField] private Image posterArtImage;

        [Header("Poster Art States")]
        [SerializeField] private Sprite availablePosterArt;
        [SerializeField] private Sprite lockedPosterArt;
        [SerializeField] private Sprite highlightPosterArt;
        
        [Header("State Overlays")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject completedOverlay;
        
        [Header("Connections")]
        [SerializeField] private UIStateColorizer colorizer;

        // --- Internal State ---
        private Button _button;
        private Bounty _bountyData;
        private BountySelectorController _controller;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnPosterClicked);
            if (colorizer == null) Debug.LogError("Colorizer not connected on " + gameObject.name, this);
        }

        /// <summary>
        /// Populates this poster with data from a Bounty asset.
        /// It now intelligently handles real bounties, placeholders, and their states.
        /// </summary>
        public void Setup(Bounty bounty, BountySaveData saveData, BountySelectorController controller)
        {
            // Store the data and references.
            _bountyData = bounty;
            _controller = controller;

            // --- The New Data-Driven Logic ---
            // A bounty is considered a placeholder if it has no levels assigned.
            bool isPlaceholder = bounty.levelSceneNames == null || bounty.levelSceneNames.Length == 0;

            // Populate the text fields regardless of state.
            // This allows placeholders to have their own text like "CLASSIFIED".
            if (titleText != null) titleText.text = bounty.title;
            if (rewardText != null) rewardText.text = bounty.reward;
            if (descriptionText != null) descriptionText.text = bounty.description;
            if (posterArtImage != null) posterArtImage.sprite = bounty.wantedPosterArt;

            // If it's a placeholder, it's always locked and non-interactable.
            if (isPlaceholder)
            {
                _button.interactable = false;
                UpdateVisuals(false, false, false); // isUnlocked=false, isCompleted=false, isHighlighted=false
            }
            // If it's a real bounty, use the player's save data to determine its state.
            else
            {
                bool isUnlocked = saveData.isUnlocked;
                bool isCompleted = saveData.isCompleted;
                _button.interactable = isUnlocked && !isCompleted;
                UpdateVisuals(isUnlocked, isCompleted, false); // Not highlighted by default.
            }
        }

        /// <summary>
        /// When clicked, only notify the controller if this is a real, interactable bounty.
        /// </summary>
        private void OnPosterClicked()
        {
            // The interactable state of the button already prevents clicks on placeholders,
            // but this is an extra layer of safety.
            if (_controller != null && _button.interactable)
            {
                _controller.OnBountySelected(this);
            }
        }

        /// <summary>
        /// Called by the controller to change the highlight state.
        /// A placeholder or completed bounty cannot be highlighted.
        /// </summary>
        public void SetHighlight(bool isHighlighted)
        {
            // We can only highlight if the button is interactable.
            if (_button.interactable)
            {
                var saveData = ProgressionManager.Instance.GetBountyStatus(_bountyData.bountyID);
                UpdateVisuals(saveData.isUnlocked, saveData.isCompleted, isHighlighted);
            }
        }
        
        // This visual update method is now private and driven by state parameters.
        private void UpdateVisuals(bool isUnlocked, bool isCompleted, bool isHighlighted)
        {
            if (colorizer == null) return;

            if(lockedOverlay) lockedOverlay.SetActive(false);
            if(completedOverlay) completedOverlay.SetActive(false);

            if (!isUnlocked)
            {
                posterBackgroundImage.sprite = lockedPosterArt;
                colorizer.SetLockedState();
                if(lockedOverlay) lockedOverlay.SetActive(true);
            }
            else
            {
                if (isCompleted)
                {
                    if(completedOverlay) completedOverlay.SetActive(true);
                }

                if (isHighlighted && !isCompleted)
                {
                    posterBackgroundImage.sprite = highlightPosterArt;
                    colorizer.SetHighlightedState();
                }
                else
                {
                    posterBackgroundImage.sprite = availablePosterArt;
                    colorizer.SetNormalState();
                }
            }
        }
        
        public Bounty GetBountyData() => _bountyData;

        private void OnDestroy()
        {
            if(_button != null) _button.onClick.RemoveListener(OnPosterClicked);
        }
    }
}