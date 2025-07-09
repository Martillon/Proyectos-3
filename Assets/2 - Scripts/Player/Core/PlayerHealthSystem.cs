using UnityEngine;
using System.Collections;
using Scripts.Core;
using Scripts.Core.Checkpoint;
using Scripts.Core.Interfaces;
using Scripts.Player.Movement.Motor;
using Scripts.Player.Visuals;
using Scripts.Player.Weapons;
using Unity.Cinemachine;

namespace Scripts.Player.Core
{
    /// <summary>
    /// Manages the player's health, damage-taking, and death/respawn sequences.
    /// It reads from and writes to a central PlayerStats Scriptable Object to persist state.
    /// Implements the new mechanic of losing weapon upgrades on taking damage.
    /// </summary>
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable, IInstakillable
    {
        [Header("Data Source")]
        [Tooltip("A reference to the PlayerStats Scriptable Object that holds all health and state data.")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Core Dependencies")]
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerVisualController playerVisualController;
        [SerializeField] private Rigidbody2D playerRb;
        [SerializeField] private WeaponBase weaponBase;

        [Header("Feedback & Timings")]
        [SerializeField] private float invulnerabilityDuration = 1.5f;
        [SerializeField] private float armorHitStunDuration = 0.3f;
        [SerializeField] private float loseLifeAnimDuration = 1.0f;
        [SerializeField] private float respawnFadeDuration = 0.4f;
        [SerializeField] private float knockbackForceHorizontal = 5f;
        [SerializeField] private float knockbackForceVertical = 3f;

        [Header("Camera Effects")]
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private float deathCameraZoomSize = 3f;
        [SerializeField] private float deathCameraZoomDuration = 1.5f;
        
        // --- State ---
        private bool _isInvulnerable;
        private bool _isProcessingCriticalSequence;
        private Coroutine _activeCoroutine;
        private float _initialCameraOrthoSize;

        public bool IsDebugInvincible { get; set; } = false;

        private void Awake()
        {
            // Validate all critical references
            if (!playerStats) Debug.LogError("PHS: PlayerStats asset is not assigned!", this);
            if (!weaponBase) Debug.LogError("PHS: WeaponBase reference is missing!", this);
            if (!playerMotor) Debug.LogError("PHS: PlayerMotor reference is missing!", this);
            
            if (playerCamera)
            {
                _initialCameraOrthoSize = playerCamera.Lens.OrthographicSize;
            }
        }

        private void Start()
        {
            if (!playerStats.IsInitialized())
            {
                // If not, this is likely a test run or the first ever run.
                // Run the reset method to ensure stats are at their default values.
                Debug.LogWarning("PlayerStats were not initialized. Resetting for new run as a fallback.");
                playerStats.ResetForNewRun();
            }
        
            // Now that we are GUARANTEED to have correct values, update the UI.
            PlayerEvents.RaiseHealthChanged(playerStats.currentLives, playerStats.currentArmor);
        }

        public void TakeDamage(float amount)
        {
            if (IsDebugInvincible || _isInvulnerable || _isProcessingCriticalSequence) return;

            int damage = Mathf.FloorToInt(amount);
            if (damage <= 0) return;
            
            // Check if we had armor before taking the hit.
            bool hadArmor = playerStats.currentArmor > 0;
            
            // Apply damage to armor first.
            playerStats.currentArmor -= damage;

            // Fire the event immediately to update the UI armor value.
            PlayerEvents.RaiseHealthChanged(playerStats.currentLives, playerStats.currentArmor);

            // If we had armor, this was an "armor hit".
            if (hadArmor)
            {
                // Revert the weapon as per your mechanic.
                weaponBase.RevertToDefaultWeapon();
                
                // Start the simple armor hit sequence (flinch, knockback).
                if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
                _activeCoroutine = StartCoroutine(ArmorHitSequence());
            }
            
            // Now, check if the damage BROKE the armor and spilled over to health.
            if (playerStats.currentArmor < 0)
            {
                // Enter the critical sequence for losing a life.
                _isProcessingCriticalSequence = true;
                playerStats.currentLives--;

                // Check for game over.
                if (playerStats.currentLives < 0)
                {
                    playerStats.currentLives = 0; // Clamp to 0 for UI.
                    PlayerEvents.RaiseHealthChanged(playerStats.currentLives, 0); // Update UI one last time.
                    if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
                    _activeCoroutine = StartCoroutine(FinalDeathSequence());
                }
                else // We lost a life but are not game over.
                {
                    // Reset armor to full for the next life.
                    playerStats.currentArmor = playerStats.maxArmor;
                    // The respawn sequence will update the UI again after the fade.
                    if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
                    _activeCoroutine = StartCoroutine(LoseLifeAndRespawnSequence());
                }
            }
        }

        private IEnumerator ArmorHitSequence()
        {
            StartInvulnerability();
            if (playerMotor) playerMotor.enabled = false;
            
            if (playerRb)
            {
                playerRb.linearVelocity = Vector2.zero;
                Vector2 knockbackDirection = new Vector2(-transform.right.x, 1f).normalized;
                playerRb.AddForce(new Vector2(knockbackDirection.x * knockbackForceHorizontal, knockbackForceVertical), ForceMode2D.Impulse);
            }
            
            playerVisualController.GetBodyAnimator()?.SetTrigger(GameConstants.AnimArmorHitTrigger);
            yield return new WaitForSeconds(armorHitStunDuration);
            if (playerMotor) playerMotor.enabled = true;
        }

        private IEnumerator LoseLifeAndRespawnSequence()
        {
            // --- 1. DEATH PHASE: Player character dies on screen ---
            Debug.Log("Player lost a life. Starting respawn sequence.");
            _isProcessingCriticalSequence = true; // Block any other damage/death calls
            _isInvulnerable = true; // Make sure no stray physics objects can interfere

            // Disable all player control
            InputManager.Instance?.DisableAllControls();
            if (playerMotor) playerMotor.enabled = false;
            
            // Stop all physical movement and interactions
            if (playerRb)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.simulated = false; // Completely turn off physics simulation for the player
            }
            
            // Handle visuals for death
            playerVisualController?.HideArmObject();
            playerVisualController?.GetBodyAnimator()?.SetTrigger(GameConstants.AnimLoseLifeTrigger);

            // Wait for the death animation to play out
            yield return new WaitForSeconds(loseLifeAnimDuration);


            // --- 2. TRANSITION PHASE: Fade to black ---
            // The ScreenFader uses unscaled time, so it works even if we pause the game.
            yield return ScreenFader.Instance?.FadeToBlack(respawnFadeDuration);


            // --- 3. RESPAWN SETUP PHASE: Occurs while the screen is black ---
            
            // Re-enable physics simulation before moving the player
            if (playerRb) playerRb.simulated = true;
            
            // Move the player to the last checkpoint
            transform.root.position = CheckpointManager.GetCurrentRespawnPosition();
            
            // Update the UI with the new life/armor counts (will be visible after fade-in)
            PlayerEvents.RaiseHealthChanged(playerStats.currentLives, playerStats.currentArmor);
            
            // Ensure the player has their correct weapon equipped for the respawn
            // This is important if they lost an upgrade on the hit that killed them.
            weaponBase.EquipWeapon(playerStats.currentWeapon);
            

            // --- 4. APPEARANCE PHASE: Fade back in from black ---
            yield return ScreenFader.Instance?.FadeToClear(respawnFadeDuration);
            
            // Play the respawn animation
            playerVisualController?.GetBodyAnimator()?.SetTrigger(GameConstants.AnimRespawnTrigger);
            playerVisualController?.ShowArmObject();
            
            // Grant post-respawn invulnerability
            StartInvulnerability();


            // --- 5. RESTORE CONTROL PHASE ---
            
            // Wait for a brief moment after respawn animation starts before giving back control
            yield return new WaitForSeconds(0.2f); 

            // Re-enable player controls
            if (playerMotor) playerMotor.enabled = true;
            InputManager.Instance?.EnablePlayerControls();
            
            Debug.Log("Player respawn sequence complete. Control restored.");
            _isProcessingCriticalSequence = false; // Allow damage again
            _activeCoroutine = null;
        }

        private IEnumerator FinalDeathSequence()
        {
            // --- 1. FINAL DEATH PHASE ---
            Debug.Log("Player has no lives left. Starting GAME OVER sequence.");
            _isProcessingCriticalSequence = true;
            _isInvulnerable = true;

            // Disable all player control permanently for this run
            InputManager.Instance?.DisableAllControls();
            if (playerMotor) playerMotor.enabled = false;
            
            // Stop all physical movement and freeze the player in place
            if (playerRb)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.bodyType = RigidbodyType2D.Kinematic; // Make it kinematic so it doesn't drift or fall
            }
            
            // Handle death visuals
            playerVisualController?.HideArmObject();
            playerVisualController?.GetBodyAnimator()?.SetTrigger(GameConstants.AnimLoseLifeTrigger);

            // --- 2. TRIGGER GLOBAL EVENTS ---
            
            // Raise the global OnPlayerDeath event. The GameOverUIController and other
            // systems will react to this.
            PlayerEvents.RaisePlayerDeath();

            // --- 3. CAMERA EFFECT PHASE ---
            
            // Perform the dramatic camera zoom on the player's final demise
            if (playerCamera)
            {
                float startTime = Time.unscaledTime;
                float startSize = playerCamera.Lens.OrthographicSize;
                
                while (Time.unscaledTime < startTime + deathCameraZoomDuration)
                {
                    float t = (Time.unscaledTime - startTime) / deathCameraZoomDuration;
                    playerCamera.Lens.OrthographicSize = Mathf.Lerp(startSize, deathCameraZoomSize, t);
                    yield return null; // Wait for the next frame
                }
                playerCamera.Lens.OrthographicSize = deathCameraZoomSize; // Ensure it ends at the exact size
            }

            // After this, the GameOverUIController takes over. It will handle pausing the game
            // and showing the restart/main menu buttons.
            _activeCoroutine = null;
        }

        private void StartInvulnerability()
        {
            _isInvulnerable = true;
            playerVisualController?.StartCoroutine(playerVisualController.FlashSpriteCoroutine(invulnerabilityDuration, 0.1f));
            StartCoroutine(EndInvulnerability());
        }

        private IEnumerator EndInvulnerability()
        {
            yield return new WaitForSeconds(invulnerabilityDuration);
            _isInvulnerable = false;
        }

        public void HealLife(int amount)
        {
            if (playerStats.currentLives < 0) return;
            playerStats.currentLives = Mathf.Min(playerStats.currentLives + amount, 99); // Use an absolute max if desired
            PlayerEvents.RaiseHealthChanged(playerStats.currentLives, playerStats.currentArmor);
        }

        public void HealArmor(int amount)
        {
            if (playerStats.currentLives < 0) return;
            playerStats.currentArmor = Mathf.Min(playerStats.currentArmor + amount, playerStats.maxArmor);
            PlayerEvents.RaiseHealthChanged(playerStats.currentLives, playerStats.currentArmor);
        }

        public void ApplyInstakill()
        {
            if (_isProcessingCriticalSequence) return;
            playerStats.currentLives = -1;
            TakeDamage(9999);
        }
    }
}