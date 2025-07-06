using UnityEngine;
using Scripts.Core;
using Scripts.Core.Progression;
using Scripts.Player.Core;

public class BountySelectorController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BountyBoard bountyBoard;
    [SerializeField] private PlayerStats playerStats; // Assign the PlayerStats SO asset

    [Header("UI References")]
    [SerializeField] private GameObject wantedPosterPrefab;
    [SerializeField] private Transform contentParent;
    // ... references to side panel, confirm button, character quote UI, etc.

    public void OnEnable() // Or in a ShowPanel() method
    {
        PopulateBountyList();
    }

    private void PopulateBountyList()
    {
        // ... (Clear existing posters) ...

        foreach (var bountyAsset in bountyBoard.allBounties)
        {
            BountySaveData savedStatus = ProgressionManager.Instance.GetBountyStatus(bountyAsset.bountyID);
            
            // Instantiate prefab, get WantedPosterButton script
            // ...
            
            // Setup button visuals based on saved status (isUnlocked, isCompleted)
            // posterButton.Setup(bountyAsset, savedStatus);
        }
    }

    // Called when the "Confirm Mission" button is clicked
    public void OnConfirmMission(Bounty bountyToLaunch)
    {
        // 1. Reset player's health/weapon stats for a fresh run.
        playerStats.ResetForNewRun();
        
        // 2. Tell the SessionManager which mission is starting.
        SessionManager.StartBounty(bountyToLaunch);
                                        
        // 3. Load the first level of the bounty.
        string firstScene = bountyToLaunch.levelSceneNames[0];
        
        // Example of closing the UI before loading
        // MainMenuController.Instance.CloseCurrentPanel(() => {
        //     SceneLoader.Instance.LoadLevelByName(firstScene);
        // });
        
        SceneLoader.Instance.LoadLevelByName(firstScene);
    }
}
