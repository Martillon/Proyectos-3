// --- START OF FILE Checkpoint.cs ---
using UnityEngine;
using Scripts.Core.Audio; // For Sounds class
using Scripts.Core.Interfaces;// For IHealLife, IHealArmor
using Scripts.Items.Checkpoint; 

namespace Scripts.Checkpoints
{
    /// <summary>
    /// Represents an individual checkpoint object within the game scene.
    /// When triggered by the player, it registers itself with the static CheckpointManager,
    /// can optionally heal the player, and provides visual and audio feedback upon its first activation.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [Tooltip("If true, attempts to heal the player's health/armor when this checkpoint is activated.")]
        [SerializeField] private bool healOnActivate = true;
        [Tooltip("Number of 'lives' to restore if healOnActivate is true (e.g., 1 to potentially add a life up to max). Use 0 if only restoring armor.")]
        [SerializeField] private int livesToRestoreOnHeal = 0;
        [Tooltip("Amount of armor to restore if healOnActivate is true. Use a high value (e.g., 999) to signify full armor restoration for the current life.")]
        [SerializeField] private int armorToRestoreOnHeal = 999;

        [Header("Feedback On First Activation")]
        [Tooltip("Visual effect (Prefab) to instantiate when this checkpoint is activated for the first time.")]
        [SerializeField] private GameObject activationVFX;
        [Tooltip("Sound effect to play when this checkpoint is activated for the first time.")]
        [SerializeField] private Sounds activationSFX;
        [Tooltip("AudioSource for playing the activationSFX. If null, will attempt to get one from this GameObject.")]
        [SerializeField] private AudioSource audioSourceForSFX;

        [Header("State (Editor Only For Debug)")]
        [Tooltip("Tracks if this specific checkpoint instance has been activated (primarily for feedback).")]
        [SerializeField] private bool hasBeenActivatedThisSession = false;
        // [SerializeField] private Sprite originalSprite; // If you want to revert sprite on ResetCheckpointInstance
        // [SerializeField] private Sprite activatedSprite;
        // private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            // spriteRenderer = GetComponent<SpriteRenderer>();
            // if (spriteRenderer != null) originalSprite = spriteRenderer.sprite; // Store original sprite

            if (audioSourceForSFX == null)
            {
                audioSourceForSFX = GetComponent<AudioSource>();
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                // Checkpoints should typically be triggers to not impede player movement.
                // Debug.LogWarning($"Checkpoint '{gameObject.name}': Collider is not set to 'Is Trigger'. Forcing it to true.", this); // Uncomment for debugging
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return; // Only interact with the Player

            // Always update the CheckpointManager, as the player might be backtracking
            // or activating checkpoints in a non-linear order.
            CheckpointManager.SetActiveCheckpoint(transform);
            // Debug.Log($"Checkpoint '{gameObject.name}': Player entered. Registered with CheckpointManager.", this); // Uncomment for debugging

            // Heal and play feedback only if this specific instance hasn't been fully processed before,
            // OR if healOnActivate is true and we want it to heal every time (though current logic is first-time feedback).
            if (!hasBeenActivatedThisSession || healOnActivate)
            {
                if (healOnActivate)
                {
                    // Debug.Log($"Checkpoint '{gameObject.name}': Attempting to heal player.", this); // Uncomment for debugging
                    if (other.TryGetComponent<IHealLife>(out var lifeHealer) && livesToRestoreOnHeal > 0)
                    {
                        lifeHealer.HealLife(livesToRestoreOnHeal);
                    }
                    if (other.TryGetComponent<IHealArmor>(out var armorHealer) && armorToRestoreOnHeal > 0)
                    {
                        // Using armorToRestoreOnHeal (e.g., 999 for max)
                        armorHealer.HealArmor(armorToRestoreOnHeal);
                    }
                }

                if (!hasBeenActivatedThisSession) // Play VFX/SFX only on the very first activation of this instance
                {
                    if (activationVFX != null)
                    {
                        Instantiate(activationVFX, transform.position, Quaternion.identity);
                    }
                    activationSFX?.Play(audioSourceForSFX);
                    // if (spriteRenderer != null && activatedSprite != null) spriteRenderer.sprite = activatedSprite;
                    
                    hasBeenActivatedThisSession = true; // Mark this instance as having played its one-time feedback
                }
            }
        }

        /// <summary>
        /// Resets the activation state of this specific checkpoint instance.
        /// Useful if levels are reloaded in a way that Checkpoint GameObjects are reused (e.g. object pooling).
        /// For simple scene reloads, this might not be necessary as Awake will handle initial state.
        /// CheckpointManager.ResetCheckpointData() handles the global static data.
        /// </summary>
        public void ResetCheckpointInstanceActivation()
        {
            hasBeenActivatedThisSession = false;
            // if (spriteRenderer != null && originalSprite != null) spriteRenderer.sprite = originalSprite; // Revert sprite
            // Debug.Log($"Checkpoint '{gameObject.name}': Instance activation state reset.", this); // Uncomment for debugging
        }
    }
}
// --- END OF FILE Checkpoint.cs ---