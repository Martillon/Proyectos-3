using UnityEngine;

namespace Scripts.Core.Checkpoint
{
    /// <summary>
    /// A simple component that marks the initial player spawn point for a level.
    /// On Awake, it registers its position with the CheckpointManager.
    /// </summary>
    public class LevelSpawnPoint : MonoBehaviour
    {
        private void Awake()
        {
            // Tell the CheckpointManager that THIS is the starting point for the level.
            CheckpointManager.SetInitialSpawnPoint(transform.position);
            Debug.Log($"Initial spawn point set at: {transform.position}");
        }
    }
}