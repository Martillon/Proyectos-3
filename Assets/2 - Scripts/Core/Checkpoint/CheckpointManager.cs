// --- File: CheckpointManager.cs ---
using UnityEngine;

namespace Scripts.Core.Checkpoint
{
    /// <summary>
    /// A static class that manages the global checkpoint state for a level,
    /// including the player's current respawn position.
    /// </summary>
    public static class CheckpointManager
    {
        private static Vector3 _levelInitialSpawnPoint;
        private static Vector3? _activeCheckpointPosition; // Use nullable Vector3

        /// <summary>
        /// Sets the initial spawn point for the current level.
        /// </summary>
        public static void SetInitialSpawnPoint(Vector3 spawnPosition)
        {
            _levelInitialSpawnPoint = spawnPosition;
            // Initially, there is no active checkpoint.
            _activeCheckpointPosition = null; 
        }

        /// <summary>
        /// Registers a newly activated checkpoint's position.
        /// </summary>
        public static void SetActiveCheckpoint(Vector3 checkpointPosition)
        {
            _activeCheckpointPosition = checkpointPosition;
        }

        /// <summary>
        /// Gets the position where the player should respawn.
        /// </summary>
        /// <returns>The last active checkpoint's position, or the level's initial spawn point if no checkpoint was activated.</returns>
        public static Vector3 GetCurrentRespawnPosition()
        {
            return _activeCheckpointPosition ?? _levelInitialSpawnPoint;
        }

        /// <summary>
        /// Resets all checkpoint data. Should be called when leaving a level or returning to the main menu.
        /// </summary>
        public static void ResetCheckpointData()
        {
            _activeCheckpointPosition = null;
            _levelInitialSpawnPoint = Vector3.zero;
        }
    }
}