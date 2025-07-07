using UnityEngine;
using Scripts.Core;
using Scripts.Core.Progression;
using Scripts.Player.Core;

namespace Scripts.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.transform.root.CompareTag(GameConstants.PlayerTag)) return;

            // Prevent being triggered multiple times
            GetComponent<Collider2D>().enabled = false;
            
            // Disable player input
            InputManager.Instance?.DisableAllControls();

            HandleLevelCompletion();
        }

        private void HandleLevelCompletion()
        {
            if (!SessionManager.IsOnBounty)
            {
                Debug.LogError("LevelExit triggered but no active bounty in SessionManager! Returning to menu.", this);
                SceneLoader.Instance.LoadMenu();
                return;
            }
            
            Bounty currentBounty = SessionManager.ActiveBounty;
            int nextLevelIndex = SessionManager.CurrentLevelIndex + 1;

            if (nextLevelIndex < currentBounty.levelSceneNames.Length)
            {
                // THERE IS A NEXT STAGE
                Debug.Log($"Stage {SessionManager.CurrentLevelIndex + 1}/{currentBounty.levelSceneNames.Length} of bounty '{currentBounty.title}' complete. Loading next stage.");
                SessionManager.AdvanceToNextLevel();
                string nextScene = currentBounty.levelSceneNames[nextLevelIndex];
                SceneLoader.Instance.LoadLevelByName(nextScene);
            }
            else
            {
                // THIS WAS THE LAST STAGE! Bounty is complete.
                Debug.Log($"Final stage of bounty '{currentBounty.title}' complete! Player returns to HQ.");
                
                // Update permanent progress file.
                ProgressionManager.Instance.CompleteBounty(currentBounty.bountyID);
                
                // End the volatile session.
                SessionManager.EndSession();
                
                // Fire the event to show the "Bounty Complete" UI.
                PlayerEvents.RaiseLevelCompleted(currentBounty.title);
            }
        }
    }
}