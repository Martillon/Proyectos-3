// --- START OF FILE LevelSelectorController.cs ---
using UnityEngine;
using UnityEngine.UI; // For Button, LayoutGroup, etc.
using TMPro; // For TextMeshProUGUI
using System.Collections.Generic; // For List
using Scripts.Core; // For SceneLoader, LevelProgressionManager, GameConstants
using Scripts.Core.Progression; // For LevelStatus (if needed directly, though manager abstracts it)
using Scripts.Core.Audio;
using Scripts.UI.MainMenu; // For UIAudioFeedback

namespace Scripts.UI.LevelSelection // New namespace
{
    /// <summary>
    /// Manages the UI for the level selection screen.
    /// Populates level buttons based on player progression and handles navigation.
    /// </summary>
    public class LevelSelectorController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The parent GameObject of this level selection panel. Used for activation/deactivation.")]
        [SerializeField] private GameObject levelSelectPanelObject;
        [Tooltip("The Transform parent under which level buttons will be instantiated or found.")]
        [SerializeField] private Transform levelButtonsParent;
        [Tooltip("Prefab for a single level button. Must have a Button and TextMeshProUGUI child.")]
        [SerializeField] private GameObject levelButtonPrefab;
        [Tooltip("Button to go back to the main menu.")]
        [SerializeField] private Button backButton;
        [Tooltip("Optional: TextMeshProUGUI to display a title like 'Select Level'.")]
        [SerializeField] private TMP_Text titleText;

        [Header("Button Appearance (Optional - for visual states)")]
        [SerializeField] private Sprite lockedLevelSprite; // Sprite for locked button background or icon
        [SerializeField] private Sprite unlockedLevelSprite; // Sprite for unlocked but not completed
        [SerializeField] private Sprite completedLevelSprite; // Sprite for completed level

        [Header("Sound Feedback")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback; // For button clicks, navigation

        // Reference to the MainMenuController to reactivate its panel when going back
        private MainMenuController mainMenuController; // Will be set by MainMenuController

        private List<GameObject> instantiatedButtons = new List<GameObject>();

        private void Awake()
        {
            if (levelSelectPanelObject == null) Debug.LogError("LevelSelectorController: LevelSelectPanelObject is not assigned!", this);
            if (levelButtonsParent == null) Debug.LogError("LevelSelectorController: LevelButtonsParent is not assigned!", this);
            if (levelButtonPrefab == null) Debug.LogError("LevelSelectorController: LevelButtonPrefab is not assigned!", this);
            if (backButton == null) Debug.LogError("LevelSelectorController: BackButton is not assigned!", this);

            // Ensure panel is initially inactive (MainMenuController will activate it)
            if (levelSelectPanelObject != null)
            {
                levelSelectPanelObject.SetActive(false);
            }
        }

        private void Start()
        {
            backButton?.onClick.AddListener(HandleBackButton);
        }
        
        /// <summary>
        /// Called by MainMenuController to pass its reference for callback.
        /// </summary>
        public void Initialize(MainMenuController MMenuController)
        {
            mainMenuController = MMenuController;
        }

        /// <summary>
        /// Call this method when the level selection panel should be shown.
        /// It populates/updates the level buttons.
        /// </summary>
        public void ShowPanel()
        {
            // Debug.Log("LevelSelectorController: Showing panel and populating levels."); // Uncomment for debugging
            if (levelSelectPanelObject != null) levelSelectPanelObject.SetActive(true);
            PopulateLevelButtons();
            // Potentially select the first available button or the back button
            backButton?.Select(); 
        }

        private void HandleBackButton()
        {
            uiSoundFeedback?.PlayClick();
            if (levelSelectPanelObject != null) levelSelectPanelObject.SetActive(false);
            mainMenuController?.ShowThisMenuAgain(); // Call method on MainMenuController to show itself
        }

        private void PopulateLevelButtons()
        {
            if (LevelProgressionManager.Instance == null || SceneLoader.Instance == null || levelButtonPrefab == null || levelButtonsParent == null)
            {
                Debug.LogError("LevelSelectorController: Missing critical references (ProgressionManager, SceneLoader, Prefab, or Parent) for populating buttons.");
                return;
            }

            // Clear existing buttons before repopulating (important if panel is reused)
            foreach (GameObject btnGO in instantiatedButtons)
            {
                Destroy(btnGO);
            }
            instantiatedButtons.Clear();

            int totalLevels = LevelProgressionManager.Instance.GetTotalLevels();
            if (totalLevels == 0)
            {
                // Debug.LogWarning("LevelSelectorController: No levels found to display (TotalLevels is 0)."); // Uncomment for debugging
                // Optionally display a "No levels available" message
                if(titleText != null) titleText.text = "No Levels Available";
                return;
            }
            if(titleText != null && totalLevels > 0) titleText.text = "Select Level";


            for (int i = 0; i < totalLevels; i++)
            {
                GameObject buttonGO = Instantiate(levelButtonPrefab, levelButtonsParent);
                instantiatedButtons.Add(buttonGO);

                Button buttonComponent = buttonGO.GetComponent<Button>();
                TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>(); // Assumes text is a child
                Image buttonImage = buttonGO.GetComponent<Image>(); // For changing background sprite based on state

                if (buttonComponent == null || buttonText == null)
                {
                    Debug.LogError($"LevelSelectorController: LevelButtonPrefab '{levelButtonPrefab.name}' is missing Button or TextMeshProUGUI child.", buttonGO);
                    continue;
                }

                string levelIdentifier = LevelProgressionManager.Instance.GetLevelIdentifier(i);
                if (string.IsNullOrEmpty(levelIdentifier))
                {
                     Debug.LogError($"LevelSelectorController: Could not get identifier for level index {i}.", buttonGO);
                     buttonText.text = "Error";
                     buttonComponent.interactable = false;
                     continue;
                }

                // Set button text (e.g., "Level 1", "Level 2")
                buttonText.text = $"Mission {i}"; // Or use a display name from LevelStatus if you add one

                bool isUnlocked = LevelProgressionManager.Instance.IsLevelUnlocked(levelIdentifier);
                bool isCompleted = LevelProgressionManager.Instance.IsLevelCompleted(levelIdentifier);

                if (isUnlocked)
                {
                    buttonComponent.interactable = true;
                    // Capture variables for the closure
                    string capturedLevelIdentifier = levelIdentifier; 
                    buttonComponent.onClick.AddListener(() => OnLevelSelected(capturedLevelIdentifier));
                    buttonComponent.onClick.AddListener(() => uiSoundFeedback?.PlayClick());


                    if (buttonImage != null)
                    {
                        buttonImage.sprite = isCompleted ? completedLevelSprite : unlockedLevelSprite;
                        if(isCompleted && completedLevelSprite == null) Debug.LogWarning($"Level button for '{levelIdentifier}' is completed but 'Completed Level Sprite' is not set.", this);
                        if(!isCompleted && unlockedLevelSprite == null) Debug.LogWarning($"Level button for '{levelIdentifier}' is unlocked but 'Unlocked Level Sprite' is not set.", this);
                    }
                }
                else
                {
                    buttonComponent.interactable = false;
                    if (buttonImage != null && lockedLevelSprite != null)
                    {
                        buttonImage.sprite = lockedLevelSprite;
                    }
                    // else if (buttonImage != null) buttonImage.color = Color.gray; // Fallback if no locked sprite
                }

                // Add EventTrigger for hover sounds if needed, similar to PauseMenuController
                // EventTrigger trigger = buttonGO.GetComponent<EventTrigger>() ?? buttonGO.AddComponent<EventTrigger>();
                // AddEntry(trigger, EventTriggerType.PointerEnter, () => uiSoundFeedback?.PlayHighlight());
                // AddEntry(trigger, EventTriggerType.Select, () => uiSoundFeedback?.PlaySelect());
            }
        }

        private void OnLevelSelected(string levelIdentifierToLoad)
        {
            // Debug.Log($"LevelSelectorController: Level '{levelIdentifierToLoad}' selected for loading.", this); // Uncomment for debugging
            if (SceneLoader.Instance != null)
            {
                // Assuming SceneLoader.levels array stores scene names/identifiers that LoadLevelByNameOrIndex can use
                // If SceneLoader.levels is an array of scene names:
                SceneLoader.Instance.LoadLevelByName(levelIdentifierToLoad); 
                // If SceneLoader.levels is just for count and you need to load by build index:
                // int buildIndex = FindBuildIndexForLevel(levelIdentifierToLoad); // You'd need this mapping
                // if (buildIndex != -1) SceneLoader.Instance.LoadLevel(buildIndex);
            }
            else
            {
                Debug.LogError("LevelSelectorController: SceneLoader.Instance is null. Cannot load level.", this);
            }
        }
        
        // Helper for EventTriggers (if you use them on level buttons)
        // private void AddEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction callback)
        // {
        //     EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        //     entry.callback.AddListener(callback);
        //     trigger.triggers.Add(entry);
        // }
    }
}
// --- END OF FILE LevelSelectorController.cs ---
