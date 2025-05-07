// --- START OF FILE CheckpointManager.cs ---
using UnityEngine;

namespace Scripts.Items.Checkpoint
{
    /// <summary>
    /// Manages the global state of checkpoints, such as the current respawn position
    /// and the initial spawn point for the level. This is a static utility class.
    /// </summary>
    public static class CheckpointManager
    {
        private static Transform currentActiveCheckpointTransform;
        private static Vector3 levelInitialSpawnPoint = Vector3.zero;

        /// <summary>
        /// Sets the initial spawn point for the current level.
        /// This should typically be called when a level starts.
        /// </summary>
        /// <param name="spawnPosition">The player's starting position in the level.</param>
        public static void SetInitialLevelSpawnPoint(Vector3 spawnPosition)
        {
            levelInitialSpawnPoint = spawnPosition;
            // If no checkpoint has been activated yet, the initial spawn is effectively the current one.
            // Debug.Log($"CheckpointManager: Initial level spawn point set to: {spawnPosition}"); // Uncomment for debugging
        }

        /// <summary>
        /// Sets the currently active checkpoint. Called by individual Checkpoint instances when activated.
        /// </summary>
        /// <param name="checkpointTransform">The Transform of the checkpoint being activated.</param>
        public static void SetActiveCheckpoint(Transform checkpointTransform)
        {
            currentActiveCheckpointTransform = checkpointTransform;
            // Debug.Log($"CheckpointManager: Active checkpoint updated to: {checkpointTransform.name} at {checkpointTransform.position}"); // Uncomment for debugging
        }

        /// <summary>
        /// Gets the position where the player should respawn.
        /// Returns the position of the last activated checkpoint, or the
        /// level's initial spawn point if no checkpoint has been activated.
        /// </summary>
        /// <returns>The respawn position as a Vector3.</returns>
        public static Vector3 GetCurrentRespawnPosition()
        {
            if (currentActiveCheckpointTransform != null)
            {
                return currentActiveCheckpointTransform.position;
            }
            // Debug.Log("CheckpointManager: No active checkpoint, returning initial level spawn point."); // Uncomment for debugging
            return levelInitialSpawnPoint;
        }

        /// <summary>
        /// Resets all static checkpoint data.
        /// Should be called when returning to the main menu or starting a new game session
        /// to ensure a fresh state for the next level load.
        /// </summary>
        public static void ResetCheckpointData()
        {
            currentActiveCheckpointTransform = null;
            levelInitialSpawnPoint = Vector3.zero; // Or to a sensible default if Vector3.zero is a valid game position
            // Debug.Log("CheckpointManager: All checkpoint data has been reset."); // Uncomment for debugging
        }
    }
}
// --- END OF FILE CheckpointManager.cs ---