// --- File: Checkpoint.cs ---
using UnityEngine;
using Scripts.Core.Audio;
using Scripts.Core.Interfaces;
using Scripts.Core.Checkpoint; // Updated namespace

namespace Scripts.Gameplay.Items
{
    /// <summary>
    /// Represents an individual checkpoint. When triggered by the player, it updates the CheckpointManager,
    /// provides feedback, and can optionally heal the player.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, this checkpoint will heal the player upon activation.")]
        [SerializeField] private bool healOnActivate = true;
        [Tooltip("Amount of armor to restore. A high value like 999 effectively means full armor.")]
        [SerializeField] private int armorToRestore = 999;
        
        [Header("Feedback")]
        [Tooltip("VFX prefab to instantiate on first activation.")]
        [SerializeField] private GameObject activationVFX;
        [Tooltip("Sound to play on first activation.")]
        [SerializeField] private Sounds activationSound;
        [SerializeField] private AudioSource audioSource;
        
        [Header("Visual State")]
        [Tooltip("Sprite to display when the checkpoint is active.")]
        [SerializeField] private Sprite activatedSprite;
        
        private SpriteRenderer _spriteRenderer;
        private Sprite _originalSprite;
        private bool _hasBeenActivated = false;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalSprite = _spriteRenderer.sprite;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // Always register with the manager.
            CheckpointManager.SetActiveCheckpoint(transform.position);

            if (_hasBeenActivated) return;
            
            // First-time activation logic
            _hasBeenActivated = true;
            
            if (healOnActivate && other.TryGetComponent<IHealArmor>(out var armorHealer))
            {
                armorHealer.HealArmor(armorToRestore);
            }
            
            // Play feedback
            if (activationVFX != null) Instantiate(activationVFX, transform.position, Quaternion.identity);
            activationSound?.Play(audioSource);
            if (_spriteRenderer != null && activatedSprite != null)
            {
                _spriteRenderer.sprite = activatedSprite;
            }
        }

        // Call this if you need to reset the state of a single checkpoint instance,
        // for example, if you reload a level without a full scene change.
        public void ResetActivationState()
        {
            _hasBeenActivated = false;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _originalSprite;
            }
        }
    }
}