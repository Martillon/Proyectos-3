// --- START OF FILE PlayerHealthSystem.cs ---
using System.Collections;
using Scripts.Core;
using Scripts.Core.Interfaces;
using Scripts.Items.Checkpoint; // For CheckpointManager
using Unity.Cinemachine;
using UnityEngine;
using Scripts.Player.Movement; // For PlayerEvents

namespace Scripts.Player.Core
{
    [RequireComponent(typeof(SpriteRenderer))] // Needed for sprite flashing
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable
    {
        [Header("Health Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int maxArmorPerLife = 3;
        [SerializeField] private float invulnerabilityDuration = 1.5f;

        [Header("Visual Feedback")]
        [Tooltip("SpriteRenderer for the player. Used for flashing effect during invulnerability.")]
        [SerializeField] private SpriteRenderer playerSpriteRenderer;
        [Tooltip("Interval (in seconds) for sprite flashing during invulnerability.")]
        [SerializeField] private float invulnerabilityFlashInterval = 0.1f;


        [Header("Death & Respawn")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string deathAnimationTrigger = "Die";
        [SerializeField] private string respawnAnimationTrigger = "Respawn";
        [SerializeField] private CinemachineCamera playerFocusedCamera;
        [SerializeField] private float deathCameraZoomSize = 3f;
        [SerializeField] private float deathCameraZoomDuration = 1.5f;

        private int currentLives;
        private int currentArmor;
        private bool isInvulnerable = false;
        private bool isDead = false;
        private float initialCameraOrthographicSize;

        private PlayerMovement2D playerMovement;
        private Coroutine invulnerabilityFlashCoroutine;


        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement2D>();
            if (playerSpriteRenderer == null) playerSpriteRenderer = GetComponent<SpriteRenderer>();
            if (playerSpriteRenderer == null) Debug.LogError("PlayerHealthSystem: Player SpriteRenderer not found or assigned. Flashing will not work.", this);


            if (playerFocusedCamera != null)
            {
                initialCameraOrthographicSize = playerFocusedCamera.Lens.OrthographicSize;
            }
        }

        private void Start()
        {
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);

            CheckpointManager.SetInitialLevelSpawnPoint(transform.position);
            // CheckpointManager.ResetCheckpointData(); // Called by GameOverUI or SceneLoader now
        }

        public void TakeDamage(float damageAmount)
        {
            if (isInvulnerable || isDead) return;

            currentArmor--;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor < 0 ? 0 : currentArmor);

            if (currentArmor < 0)
            {
                currentLives--;
                // PlayerEvents.RaiseHealthChanged(currentLives, 0); // Health change already sent above for armor, this updates life
                
                if (currentLives < 0)
                {
                    currentLives = 0; // Clamp for UI
                    PlayerEvents.RaiseHealthChanged(currentLives, 0); // Final update before death sequence
                    StartCoroutine(FinalDeathSequence());
                }
                else
                {
                    PlayerEvents.RaiseHealthChanged(currentLives, currentArmor < 0 ? maxArmorPerLife : currentArmor); // Update lives, armor will be reset
                    StartCoroutine(LoseLifeAndRespawnSequence());
                }
            }
            else
            {
                StartCoroutine(BeginInvulnerability());
                // playerAnimator?.SetTrigger("Hit"); 
            }
        }

        private IEnumerator LoseLifeAndRespawnSequence()
        {
            isDead = true;
            isInvulnerable = true; // Implicitly handled by isDead check, but good for clarity

            InputManager.Instance?.DisableAllControls();
            if (playerMovement != null) playerMovement.enabled = false;

            if (playerAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                playerAnimator.SetTrigger(deathAnimationTrigger);
                yield return new WaitForSeconds(0.75f); // Adjust based on actual animation length
            }
            
            if (playerSpriteRenderer != null) playerSpriteRenderer.enabled = false; // Hide player during "teleport"
            yield return new WaitForSeconds(0.25f); // Brief delay before reappearing

            transform.position = CheckpointManager.GetCurrentRespawnPosition();
            
            if (playerSpriteRenderer != null) playerSpriteRenderer.enabled = true; // Show player

            currentArmor = maxArmorPerLife;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); // Update HUD after armor reset

            if (playerAnimator != null && !string.IsNullOrEmpty(respawnAnimationTrigger))
            {
                 playerAnimator.SetTrigger(respawnAnimationTrigger);
                 yield return new WaitForSeconds(0.5f); // Adjust based on respawn animation
            }

            if (playerMovement != null) playerMovement.enabled = true;
            InputManager.Instance?.EnablePlayerControls();
            
            isDead = false;
            StartCoroutine(BeginInvulnerability());
        }

        private IEnumerator FinalDeathSequence()
        {
            isDead = true;
            isInvulnerable = true;

            InputManager.Instance?.DisableAllControls();
            if (playerMovement != null) playerMovement.enabled = false;

            if (playerAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                playerAnimator.SetTrigger(deathAnimationTrigger);
            }
            
            PlayerEvents.RaisePlayerDeath(); 

            if (playerFocusedCamera != null)
            {
                float startTime = Time.unscaledTime; 
                float currentSize = playerFocusedCamera.Lens.OrthographicSize;
                while (Time.unscaledTime < startTime + deathCameraZoomDuration)
                {
                    float t = (Time.unscaledTime - startTime) / deathCameraZoomDuration;
                    playerFocusedCamera.Lens.OrthographicSize = Mathf.Lerp(currentSize, deathCameraZoomSize, t);
                    yield return null; 
                }
                playerFocusedCamera.Lens.OrthographicSize = deathCameraZoomSize; 
            }
            else
            {
                yield return new WaitForSecondsRealtime(1.0f); 
            }
        }

        private IEnumerator BeginInvulnerability()
        {
            isInvulnerable = true;
            if (invulnerabilityFlashCoroutine != null) StopCoroutine(invulnerabilityFlashCoroutine); // Stop existing flash if any
            if (playerSpriteRenderer != null && gameObject.activeInHierarchy) // Check if GO is active for coroutine
            {
                 invulnerabilityFlashCoroutine = StartCoroutine(FlashSpriteCoroutine(playerSpriteRenderer, invulnerabilityDuration, invulnerabilityFlashInterval));
            }
           
            yield return new WaitForSeconds(invulnerabilityDuration); // Actual invulnerability timer
            
            isInvulnerable = false;
            if (invulnerabilityFlashCoroutine != null) // Stop flashing if it's still running (duration might be shorter than flash sequence)
            {
                StopCoroutine(invulnerabilityFlashCoroutine);
                if(playerSpriteRenderer != null) playerSpriteRenderer.enabled = true; // Ensure sprite is visible
            }
        }

        private IEnumerator FlashSpriteCoroutine(SpriteRenderer sprite, float duration, float flashInterval)
        {
            if (sprite == null) yield break;
            float endTime = Time.time + duration;
            bool originalState = sprite.enabled; // Store original state in case it was already hidden for some reason

            try
            {
                while (Time.time < endTime)
                {
                    sprite.enabled = !sprite.enabled;
                    yield return new WaitForSeconds(flashInterval);
                }
            }
            finally // Ensure sprite is visible at the end, regardless of how coroutine exits
            {
                 sprite.enabled = originalState; // Or always true: sprite.enabled = true;
            }
        }

        public void HealLife(int amount)
        {
            if (isDead || amount <= 0) return;
            int previousLives = currentLives;
            currentLives = Mathf.Min(currentLives + amount, maxLives);
            
            // Only restore armor fully if a life was actually gained, or if it's a heal call.
            // Checkpoint logic for armor is separate via HealArmor.
            if(currentLives > previousLives || amount > 0) // Or just always restore armor on HealLife call.
            {
                currentArmor = maxArmorPerLife;
            }
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        public void HealArmor(int amount)
        {
            if (isDead || currentLives < 0 || amount <= 0) return;
            currentArmor = (amount >= maxArmorPerLife) ? maxArmorPerLife : Mathf.Min(currentArmor + amount, maxArmorPerLife); // Simplified logic for 999
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        public int GetCurrentLives() => currentLives;
        public int GetCurrentArmor() => currentArmor;
        public bool IsCurrentlyInvulnerable() => isInvulnerable;

        public void FullResetPlayerHealth()
        {
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
            isDead = false;
            isInvulnerable = false;
            if (invulnerabilityFlashCoroutine != null) StopCoroutine(invulnerabilityFlashCoroutine);
            if(playerSpriteRenderer != null) playerSpriteRenderer.enabled = true;
            if (playerFocusedCamera != null) playerFocusedCamera.Lens.OrthographicSize = initialCameraOrthographicSize;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }
    }
}
// --- END OF FILE PlayerHealthSystem.cs ---