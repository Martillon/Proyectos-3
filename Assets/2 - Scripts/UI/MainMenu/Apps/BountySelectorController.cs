using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Scripts.Core;
using Scripts.Core.Progression;
using Scripts.Core.Audio;
using Scripts.Player.Core;
using TMPro;

namespace Scripts.UI.MainMenu.Apps
{
    /// <summary>
    /// The master controller for the bounty selection screen. It manages instantiating bounties,
    /// handling user input for selection, and controlling the paging system.
    /// </summary>
    public class BountySelectorController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The ScriptableObject that contains the list of all bounties.")]
        [SerializeField] private BountyBoard bountyBoard;
        [Tooltip("A reference to the PlayerStats ScriptableObject for resetting on mission launch.")]
        [SerializeField] private PlayerStats playerStats;
        [Tooltip("How many bounty posters to show on a single page.")]
        [SerializeField] private int itemsPerPage = 3;

        [Header("Core UI References")]
        [Tooltip("The prefab for a single Wanted Poster UI element.")]
        [SerializeField] private GameObject wantedPosterPrefab;
        [Tooltip("The RectTransform of the parent object where posters will be instantiated. Must have a Layout Group.")]
        [SerializeField] private RectTransform contentParent;
        [Tooltip("The ScrollRect component that manages the scrolling content.")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Paging Controls")]
        [Tooltip("The button used to scroll to the previous page.")]
        [SerializeField] private Button pageUpButton;
        [Tooltip("The button used to scroll to the next page.")]
        [SerializeField] private Button pageDownButton;

        // The "Details Panel" section has been completely REMOVED.

        [Header("Confirmation Popup")]
        [Tooltip("The animator for showing/hiding the confirmation popup.")]
        [SerializeField] private UIPanelAnimator confirmationPopupAnimator;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Character Quote Popup")]
        [SerializeField] private GameObject quotePopup;
        [SerializeField] private TMP_Text quoteText;
        [SerializeField] private float quoteDisplayDuration = 2.5f;
        
        [Header("Audio Feedback")]
        [Tooltip("The central component for playing UI sounds.")]
        [SerializeField] private UIAudioFeedback uiAudioFeedback;

        // --- Internal State Variables ---
        private List<WantedPosterUI> _instantiatedPosters = new List<WantedPosterUI>();
        private WantedPosterUI _currentlySelectedPoster;

        private int _currentPage = 0;
        private int _pageCount = 0;
        private Coroutine _scrollingCoroutine;

        private void Start()
        {
            pageUpButton?.onClick.AddListener(GoToPreviousPage);
            pageDownButton?.onClick.AddListener(GoToNextPage);
        }
        
        private void OnEnable()
        {
            PopulateBountyList();
        }

        private void PopulateBountyList()
        {
            // --- 1. Cleanup ---
            foreach (var poster in _instantiatedPosters)
            {
                Destroy(poster.gameObject);
            }
            _instantiatedPosters.Clear();
            
            confirmationPopupAnimator.gameObject.SetActive(false);
            quotePopup.SetActive(false);

            // --- 2. THE DEFINITIVE DEBUG CHECK ---
            if (bountyBoard == null)
            {
                Debug.LogError("BOUNTY BOARD IS NOT ASSIGNED IN THE INSPECTOR!");
                return; // Stop here
            }
            if (ProgressionManager.Instance == null)
            {
                Debug.LogError("PROGRESSION MANAGER INSTANCE IS NULL! Is it in the scene and active?");
                return; // Stop here
            }
            
            // This log will tell us EXACTLY how many bounties the script sees.
            Debug.Log($"--- Starting Bounty Population. Bounties found on board: {bountyBoard.allBounties.Count} ---");


            // --- 3. The Loop ---
            foreach (var bountyAsset in bountyBoard.allBounties)
            {
                // Check if the bounty asset itself is null (e.g., an empty slot in the list)
                if (bountyAsset == null)
                {
                    Debug.LogWarning("Found a NULL bounty asset in the BountyBoard list. Skipping.");
                    continue;
                }

                Debug.Log($"Processing bounty with ID: '{bountyAsset.bountyID}'");
                BountySaveData savedStatus = ProgressionManager.Instance.GetBountyStatus(bountyAsset.bountyID);
                
                if (savedStatus == null)
                {
                    // This is the most likely culprit. This log will tell us for sure.
                    Debug.LogWarning($"SKIPPED: ProgressionManager returned NULL save data for bounty ID: '{bountyAsset.bountyID}'. Check if this ID is correct in your Bounty asset and that your save file logic is working.");
                    continue;
                }

                // If we get this far, instantiation should happen.
                Debug.Log($"SUCCESS: Instantiating poster for '{bountyAsset.bountyID}'.");
                GameObject posterGO = Instantiate(wantedPosterPrefab, contentParent);
                var posterUI = posterGO.GetComponent<WantedPosterUI>();
                if (posterUI != null)
                {
                    posterUI.Setup(bountyAsset, savedStatus, this);
                    _instantiatedPosters.Add(posterUI);
                }
            }
        }

        // --- Paging Logic (Unchanged) ---
        // This section handles the math and animation for scrolling between pages.
        #region Paging
        private IEnumerator CalculateAndApplyPaging()
        {
            yield return null; 

            _pageCount = (_instantiatedPosters.Count > 0) ? Mathf.CeilToInt((float)_instantiatedPosters.Count / itemsPerPage) : 0;
            _currentPage = 0;
            UpdatePaging(true);
        }

        public void GoToPreviousPage()
        {
            uiAudioFeedback?.PlayClick();
            if (_currentPage > 0)
            {
                _currentPage--;
                UpdatePaging();
            }
        }

        public void GoToNextPage()
        {
            uiAudioFeedback?.PlayClick();
            if (_currentPage < _pageCount - 1)
            {
                _currentPage++;
                UpdatePaging();
            }
        }

        private void UpdatePaging(bool instant = false)
        {
            pageUpButton.interactable = (_currentPage > 0);
            pageDownButton.interactable = (_currentPage < _pageCount - 1);

            float targetScrollPos = _pageCount > 1 ? (float)_currentPage / (_pageCount - 1) : 0;
            
            if (_scrollingCoroutine != null) StopCoroutine(_scrollingCoroutine);

            if (instant)
            {
                // The scroll position is normalized 0-1, but 1 is the top and 0 is the bottom.
                scrollRect.horizontalNormalizedPosition = targetScrollPos;
            }
            else
            {
                _scrollingCoroutine = StartCoroutine(SmoothScrollTo(targetScrollPos));
            }
        }

        private IEnumerator SmoothScrollTo(float target)
        {
            float elapsedTime = 0f;
            float duration = 0.3f;
            float start = scrollRect.horizontalNormalizedPosition;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsedTime / duration);
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, t);
                yield return null;
            }
            scrollRect.horizontalNormalizedPosition = target;
        }
        #endregion

        // --- Selection and Launch Logic ---

        /// <summary>
        /// This method is called by a WantedPosterUI instance when it's clicked.
        /// It handles the highlight state and immediately proceeds to the confirmation step.
        /// </summary>
        public void OnBountySelected(WantedPosterUI selectedPoster)
        {
            uiAudioFeedback?.PlaySelect();

            if (_currentlySelectedPoster != null) _currentlySelectedPoster.SetHighlight(false);

            _currentlySelectedPoster = selectedPoster;
            _currentlySelectedPoster.SetHighlight(true);

            // Get the data from the selected poster.
            Bounty data = selectedPoster.GetBountyData();
            
            // REFACTORED: Instead of showing a details panel, we now
            // immediately show the confirmation pop-up.
            ShowConfirmationPopup(data);
        }

        private void ShowConfirmationPopup(Bounty bountyToConfirm)
        {
            uiAudioFeedback?.PlayOpen();
            confirmationText.text = $"Launch mission against target:\n<color=yellow>{bountyToConfirm.title}</color>?";
            
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(() => OnLaunchConfirmed(bountyToConfirm));
            
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(OnLaunchCancelled);

            confirmationPopupAnimator.Show();
        }

        private void OnLaunchCancelled()
        {
            uiAudioFeedback?.PlayClick();
            confirmationPopupAnimator.Hide();
            uiAudioFeedback?.PlayClose();

            // Optional: Deselect the poster when cancelling.
            if (_currentlySelectedPoster != null)
            {
                _currentlySelectedPoster.SetHighlight(false);
                _currentlySelectedPoster = null;
            }
        }

        private void OnLaunchConfirmed(Bounty bountyToLaunch)
        {
            uiAudioFeedback?.PlayClick();
            StartCoroutine(LaunchSequence(bountyToLaunch));
        }
        
        /// <summary>
        /// The final sequence that connects the UI to the core game systems.
        /// It prepares the game state and then tells the SceneLoader to begin the transition.
        /// </summary>
        private IEnumerator LaunchSequence(Bounty bountyToLaunch)
        {
            // --- 1. UI Cleanup ---
            // Hide the popups first to give the player feedback.
            confirmationPopupAnimator.Hide();
            uiAudioFeedback?.PlayClose();
            
            // Show the character quote for thematic effect.
            quoteText.text = $"\"{bountyToLaunch.characterQuoteOnLaunch}\"";
            quotePopup.SetActive(true);
            
            // Wait for the quote to be visible for a moment.
            yield return new WaitForSeconds(quoteDisplayDuration);
            
            // --- 2. Core Game Logic Setup (The "Coupling") ---
            // This is where we prepare the game for the new run.
            Debug.Log("Resetting player stats for a new run.");
            playerStats.ResetForNewRun();
            
            Debug.Log($"Starting session for bounty: {bountyToLaunch.title}");
            SessionManager.StartBounty(bountyToLaunch);
                                        
            // Get the name of the first scene for this bounty.
            string firstScene = bountyToLaunch.levelSceneNames[0];
            
            // --- 3. Hide the Main Menu UI and Load the Level ---
            // Find the main UIPanelAnimator for the "BHA.exe" window and tell it to hide.
            //var parentPanelAnimator = GetComponentInParent<UIPanelAnimator>();
            //parentPanelAnimator?.Hide();
            
            // Wait a very short moment for the hide animation to begin before starting
            // the scene load, which might freeze the screen for a moment.
            yield return new WaitForSeconds(0.2f);
            
            Debug.Log($"Handing off to SceneLoader to load level: {firstScene}");
            // Tell the SceneLoader service to begin loading the level.
            SceneLoader.Instance.LoadLevelByName(firstScene);
        }
    }
}