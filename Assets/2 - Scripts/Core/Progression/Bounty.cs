using UnityEngine;

namespace Scripts.Core.Progression
{
    // We use a regular class for the main bounty data so we can have methods,
    // but the status will be stored in a simple struct for easy saving.
    [CreateAssetMenu(fileName = "Bounty_", menuName = "My Game/Progression/Bounty")]
    public class Bounty : ScriptableObject
    {
        [Tooltip("A unique ID like 'bounty_01_cyberslums'. Used for saving progress.")]
        public string bountyID;
        
        [Header("Display Information")]
        public string title;
        [Tooltip("The reward for completing this bounty (e.g., '15,000c').")]
        public string reward;
        [TextArea(3, 5)] public string description;
        public Sprite wantedPosterArt;
        public string characterQuoteOnLaunch;
        
        
        [Header("Mission Structure")]
        [Tooltip("The list of scene names that make up this bounty, in order.")]
        public string[] levelSceneNames;
    }
}