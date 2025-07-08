// --- File: SessionManager.cs (MODIFIED) ---

using Scripts.Core.Progression;
using UnityEngine;

namespace Scripts.Core
{
    /// <summary>
    /// A static, VOLATILE manager for the player's current game session.
    /// It tracks the active bounty, progress within it, and boss checkpoints.
    /// This data is NOT saved to disk and is reset when the game is closed or
    /// when the player returns to the main menu.
    /// </summary>
    public static class SessionManager
    {
        // --- Existing Properties ---
        public static Bounty ActiveBounty { get; private set; }
        public static int CurrentLevelIndex { get; private set; }
        public static bool IsOnBounty => ActiveBounty != null;

        // --- NEW: Boss Checkpoint ---
        /// <summary>
        /// Stores the current phase the player has reached in a boss fight.
        /// Defaults to 1. This is reset when a new bounty starts or the session ends.
        /// </summary>
        public static int CurrentBossPhaseCheckpoint { get; private set; }


        /// <summary>
        /// Begins a new bounty run, setting the active bounty and resetting progress.
        /// This is called when the player launches a mission from the main menu.
        /// </summary>
        public static void StartBounty(Bounty bounty)
        {
            ActiveBounty = bounty;
            CurrentLevelIndex = 0;
            // IMPORTANT: Reset the boss checkpoint to Phase 1 at the start of any new run.
            CurrentBossPhaseCheckpoint = 1;

            Debug.Log($"Session started for bounty: {bounty.title}");
        }

        // --- NEW: Method for the BossController to call ---
        /// <summary>
        /// Updates the boss phase checkpoint for the current session.
        /// This should be called by the BossController after a phase transition is complete.
        /// </summary>
        /// <param name="phase">The new phase number to save (e.g., 2, 3).</param>
        public static void SetBossPhaseCheckpoint(int phase)
        {
            // We only update if the new phase is higher than the current one.
            // This prevents accidentally reverting progress.
            if (phase > CurrentBossPhaseCheckpoint)
            {
                CurrentBossPhaseCheckpoint = phase;
                Debug.Log($"SESSION CHECKPOINT SAVED: Boss Phase {phase}");
            }
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
            // Also reset the checkpoint when the entire session is over.
            CurrentBossPhaseCheckpoint = 1;
        }
    }
}