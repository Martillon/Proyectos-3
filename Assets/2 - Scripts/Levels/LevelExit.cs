// --- START OF FILE LevelExit.cs (Modificado) ---
using UnityEngine;
using Scripts.Core; // For GameConstants, InputManager
using Scripts.Player.Core; // For PlayerEvents (o GameEvents si lo moviste)
using UnityEngine.SceneManagement; // For SceneManager (Unity's)

namespace Scripts.Levels
{
    /// <summary>
    /// Detects when the player reaches the end of a level.
    /// It marks the level as completed via LevelProgressionManager and
    /// raises an event for other systems (like LevelCompleteUIController) to handle the UI and transition.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        // [Header("Feedback (Optional - if direct feedback still desired here)")]
        // [SerializeField] private GameObject exitReachedVFX; // e.g., a sparkle at the exit point
        // [SerializeField] private Sounds exitReachedSFX;
        // [SerializeField] private AudioSource audioSourceForSFX;

        private bool hasBeenTriggered = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasBeenTriggered || !other.CompareTag(GameConstants.PlayerTag))
            {
                return;
            }

            hasBeenTriggered = true;
            // Debug.Log($"LevelExit: Player entered exit trigger for level '{SceneManager.GetActiveScene().name}'.", this); // Uncomment for debugging

            // 1. Disable player input immediately to prevent movement during sequence
            InputManager.Instance?.DisableAllControls();

            // 2. Mark level as completed and unlock next (this also saves progression)
            string currentLevelIdentifier = SceneManager.GetActiveScene().name;
            LevelProgressionManager.Instance?.CompleteLevel(currentLevelIdentifier);

            // 3. Play any immediate feedback at the exit point itself (optional)
            // if (exitReachedVFX != null) Instantiate(exitReachedVFX, transform.position, Quaternion.identity);
            // exitReachedSFX?.Play(audioSourceForSFX);

            // 4. Raise the event for UI and other systems to handle the "Level Complete" sequence
            PlayerEvents.RaiseLevelCompleted(currentLevelIdentifier); // Or GameEvents.RaiseLevelCompleted

            // The LevelExit's job is done here. LevelCompleteUIController will take over.
            // We might want to disable this LevelExit trigger object after it's used once per scene load.
            // gameObject.SetActive(false); // Or just rely on hasBeenTriggered
        }
    }
}
// --- END OF FILE LevelExit.cs ---
