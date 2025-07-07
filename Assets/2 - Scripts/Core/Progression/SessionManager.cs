using Scripts.Core.Progression;
using UnityEngine;

namespace Scripts.Core
{
    /// <summary>
    /// A static, VOLATILE manager for the player's current game session.
    /// It tracks the active bounty and progress within it.
    /// This data is NOT saved to disk and is reset when the game is closed or
    /// when the player returns to the main menu.
    /// </summary>
    public static class SessionManager
    {
        public static Bounty ActiveBounty { get; private set; }
        public static int CurrentLevelIndex { get; private set; }

        public static bool IsOnBounty => ActiveBounty != null;

        /// <summary>
        /// Begins a new bounty run, setting the active bounty and resetting progress.
        /// </summary>
        public static void StartBounty(Bounty bounty)
        {
            ActiveBounty = bounty;
            CurrentLevelIndex = 0;
            Debug.Log($"Session started for bounty: {bounty.title}");
        }

        /// <summary>
        /// Advances progress to the next level within the current bounty.
        /// </summary>
        public static void AdvanceToNextLevel()
        {
            if (IsOnBounty)
            {
                CurrentLevelIndex++;
            }
        }

        /// <summary>
        /// Ends the current session, clearing all active bounty data.
        /// Should be called when returning to the main menu or after a game over.
        /// </summary>
        public static void EndSession()
        {
            if (IsOnBounty)
            {
                Debug.Log($"Session ended for bounty: {ActiveBounty.title}");
            }
            ActiveBounty = null;
            CurrentLevelIndex = 0;
        }
    }
}