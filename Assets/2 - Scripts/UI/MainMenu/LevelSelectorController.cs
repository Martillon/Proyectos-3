using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.UI.MainMenu;

namespace Scripts.UI.LevelSelection
{
    /// <summary>
    /// Manages the UI for the level selection screen. Dynamically populates level buttons
    /// based on player progression and handles navigation.
    /// </summary>
    public class LevelSelectorController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The parent GameObject of the level selection panel.")]
        [SerializeField] private GameObject levelSelectPanel;
        [Tooltip("The transform under which level buttons will be instantiated.")]
        [SerializeField] private Transform buttonsParent;
        [Tooltip("The prefab for a single level button.")]
        [SerializeField] private GameObject levelButtonPrefab;
        [Tooltip("The button to return to the main menu.")]
        [SerializeField] private Button backButton;

        [Header("Button Sprites (Optional)")]
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private Sprite completedSprite;

        [Header("Audio")]
        [SerializeField] private UIAudioFeedback uiSoundFeedback;
        
        private MainMenuController _mainMenuController;
        private readonly List<GameObject> _instantiatedButtons = new List<GameObject>();

        public void Initialize(MainMenuController mainMenu)
        {
            _mainMenuController = mainMenu;
        }

        private void Awake()
        {
            // Validate references
            if (levelSelectPanel == null) Debug.LogError("LSC: LevelSelectPanel is not assigned!", this);
            if (buttonsParent == null) Debug.LogError("LSC: ButtonsParent is not assigned!", this);
            if (levelButtonPrefab == null) Debug.LogError("LSC: LevelButtonPrefab is not assigned!", this);
            
            // The panel is shown/hidden by the MainMenuController.
            levelSelectPanel.SetActive(false);
            backButton?.onClick.AddListener(OnBackPressed);
        }

        public void ShowPanel()
        {
            levelSelectPanel.SetActive(true);
            PopulateLevels();
            // Select the first interactable button or the back button.
            var firstInteractable = _instantiatedButtons.Find(go => go.GetComponent<Button>().interactable);
            if(firstInteractable != null)
            {
                firstInteractable.GetComponent<Button>().Select();
            }
            else
            {
                backButton?.Select();
            }
        }

        private void OnBackPressed()
        {
            uiSoundFeedback?.PlayClick();
            levelSelectPanel.SetActive(false);
            _mainMenuController?.ShowMainMenu();
        }

        private void PopulateLevels()
        {
            // Clear any old buttons
            foreach (var button in _instantiatedButtons)
            {
                Destroy(button);
            }
            _instantiatedButtons.Clear();

            if (LevelProgressionManager.Instance == null || SceneLoader.Instance == null)
            {
                Debug.LogError("LSC: Cannot populate levels. Progression or Scene Loader is missing.", this);
                return;
            }

            int totalLevels = LevelProgressionManager.Instance.GetTotalLevelCount();
            for (int i = 0; i < totalLevels; i++)
            {
                GameObject buttonGO = Instantiate(levelButtonPrefab, buttonsParent);
                _instantiatedButtons.Add(buttonGO);

                string levelIdentifier = LevelProgressionManager.Instance.GetLevelIdentifierByIndex(i);
                if (string.IsNullOrEmpty(levelIdentifier)) continue;

                // --- Configure Button ---
                var button = buttonGO.GetComponent<Button>();
                var buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
                var buttonImage = buttonGO.GetComponent<Image>();

                buttonText.text = $"Level {i + 1}";

                bool isUnlocked = LevelProgressionManager.Instance.IsLevelUnlocked(levelIdentifier);
                bool isCompleted = LevelProgressionManager.Instance.IsLevelCompleted(levelIdentifier);
                
                button.interactable = isUnlocked;

                if (isUnlocked)
                {
                    // Use a captured variable in the listener to avoid issues with loop variables.
                    string capturedId = levelIdentifier;
                    button.onClick.AddListener(() => OnLevelSelected(capturedId));
                    buttonImage.sprite = isCompleted ? completedSprite : unlockedSprite;
                }
                else
                {
                    buttonImage.sprite = lockedSprite;
                }
            }
        }
        
        private void OnLevelSelected(string levelIdentifier)
        {
            uiSoundFeedback?.PlayClick();
            // Fade out before loading
            ScreenFader.Instance?.FadeToBlack();
            // SceneLoader will handle the rest
            SceneLoader.Instance?.LoadLevelByName(levelIdentifier);
        }
    }
}
