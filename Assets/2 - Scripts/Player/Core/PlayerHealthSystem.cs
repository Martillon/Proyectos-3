using UnityEngine;
using System.Collections;
using Scripts.Core;
using Scripts.Core.Checkpoint;
using Scripts.Core.Interfaces;
using Scripts.Player.Movement.Motor;
using Scripts.Player.Visuals;
using Unity.Cinemachine;

namespace Scripts.Player.Core
{
    /// <summary>
    /// Manages the player's lives, armor, damage-taking, and death/respawn sequences.
    /// Interacts with PlayerEvents, CheckpointManager, and ScreenFader.
    /// </summary>
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable, IInstakillable
    {
        [Header("Health & Armor")]
        [Tooltip("Starting lives after a full game over or on first play.")]
        [SerializeField] private int initialLives = 3;
        [Tooltip("The absolute maximum number of lives the player can accumulate.")]
        [SerializeField] private int maxLives = 9;
        [Tooltip("The number of hits the player can take per life before losing a life.")]
        [SerializeField] private int maxArmorPerLife = 2;

        [Header("Damage & Invulnerability")]
        [Tooltip("Duration in seconds the player is invulnerable after taking damage or respawning.")]
        [SerializeField] private float invulnerabilityDuration = 1.5f;
        [Tooltip("How quickly the player sprite flashes during invulnerability.")]
        [SerializeField] private float invulnerabilityFlashInterval = 0.1f;
        [Tooltip("Horizontal force of the knockback when taking damage.")]
        [SerializeField] private float knockbackForceHorizontal = 5f;
        [Tooltip("Vertical force of the knockback when taking damage.")]
        [SerializeField] private float knockbackForceVertical = 3f;

        [Header("Sequence Timings")]
        [Tooltip("Duration of the hit-stun/animation state when taking armor damage.")]
        [SerializeField] private float armorHitStunDuration = 0.3f;
        [Tooltip("Duration of the death animation before the screen fades for respawn.")]
        [SerializeField] private float loseLifeAnimDuration = 1.0f;
        [Tooltip("Duration of the screen fade to/from black for respawns.")]
        [SerializeField] private float respawnFadeDuration = 0.4f;
        [Tooltip("Duration of the respawn animation after the screen has faded back in.")]
        [SerializeField] private float respawnAnimDuration = 0.5f;

        [Header("Death Camera Effect")]
        [Tooltip("The Cinemachine camera focusing on the player, used for the death zoom effect.")]
        [SerializeField] private CinemachineCamera playerCamera;
        [Tooltip("The target orthographic size for the camera zoom on final death.")]
        [SerializeField] private float deathCameraZoomSize = 3f;
        [Tooltip("The duration of the camera zoom effect.")]
        [SerializeField] private float deathCameraZoomDuration = 1.5f;
        
        [Header("Component References")]
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerVisualController playerVisualController;
        [SerializeField] private Rigidbody2D playerRb;
        
        private int _currentLives;
        private int _currentArmor;
        private bool _isInvulnerable;
        private bool _isProcessingCriticalSequence; // Prevents nested death/respawn calls
        private float _initialCameraOrthoSize;
        private Coroutine _activeCoroutine;
        public bool IsDebugInvincible { get; set; } = false;

        private bool IsDead { get; set; }

        private void Awake()
        {
            // Validate references
            if (playerMotor == null) Debug.LogError("PHS: PlayerMotor reference is missing!", this);
            if (playerVisualController == null) Debug.LogError("PHS: PlayerVisualController reference is missing!", this);
            if (playerRb == null) Debug.LogError("PHS: Player's Rigidbody2D reference is missing!", this);

            if (playerCamera != null)
            {
                _initialCameraOrthoSize = playerCamera.Lens.OrthographicSize;
            }
        }

        private void Start()
        {
            InitializeHealth();
            CheckpointManager.SetInitialSpawnPoint(transform.root.position);
        }

        public void InitializeHealth()
        {
            _currentLives = initialLives;
            _currentArmor = maxArmorPerLife;
            IsDead = false;
            _isProcessingCriticalSequence = false;
            _isInvulnerable = false;
            PlayerEvents.RaiseHealthChanged(_currentLives, _currentArmor);
        }

        public void TakeDamage(float amount)
        {
            if (IsDebugInvincible || _isInvulnerable || _isProcessingCriticalSequence || IsDead) return;

            int damage = Mathf.FloorToInt(amount);
            if (damage <= 0) return;
            
            _currentArmor -= damage;

            if (_currentArmor < 0)
            {
                _isProcessingCriticalSequence = true;
                _currentLives--;
                PlayerEvents.RaiseHealthChanged(_currentLives, _currentArmor);

                if (_currentLives < 0)
                {
                    if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
                    _activeCoroutine = StartCoroutine(FinalDeathSequence());
                }
                else
                {
                    if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
                    _activeCoroutine = StartCoroutine(LoseLifeAndRespawnSequence());
                }
            }
            else
            {
                PlayerEvents.RaiseHealthChanged(_currentLives, _currentArmor);
                if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
                _activeCoroutine = StartCoroutine(ArmorHitSequence());
            }
        }

        private IEnumerator ArmorHitSequence()
        {
            // Become invulnerable and start flashing
            StartInvulnerability();
            
            // Short stun period
            if (playerMotor != null) playerMotor.enabled = false;
            
            // Apply knockback
            playerRb.linearVelocity = Vector2.zero;
            Vector2 knockbackDirection = new Vector2(-playerMotor.transform.right.x, 1f).normalized; // Simple knockback away and up
            playerRb.AddForce(new Vector2(knockbackDirection.x * knockbackForceHorizontal, knockbackForceVertical), ForceMode2D.Impulse);
            
            // Trigger hit animation
            playerVisualController.GetBodyAnimator()?.SetTrigger(GameConstants.AnimArmorHitTrigger);

            yield return new WaitForSeconds(armorHitStunDuration);
            
            // Restore control
            if (playerMotor != null) playerMotor.enabled = true;
        }

        private IEnumerator LoseLifeAndRespawnSequence()
        {
            // --- Death Part ---
            IsDead = true; // Temporarily "dead" for this sequence
            if (playerMotor != null) playerMotor.enabled = false;
            if (playerRb != null) { playerRb.linearVelocity = Vector2.zero; playerRb.simulated = false; }
            playerVisualController?.HideArmObject();
            playerVisualController?.GetBodyAnimator()?.SetTrigger(GameConstants.AnimLoseLifeTrigger);

            yield return new WaitForSeconds(loseLifeAnimDuration);

            // --- Transition Part ---
            yield return ScreenFader.Instance?.FadeToBlack(respawnFadeDuration);
            
            // --- Respawn Setup (while screen is black) ---
            transform.root.position = CheckpointManager.GetCurrentRespawnPosition();
            if (playerRb != null) playerRb.simulated = true;
            _currentArmor = maxArmorPerLife;
            IsDead = false;
            PlayerEvents.RaiseHealthChanged(_currentLives, _currentArmor);

            yield return ScreenFader.Instance?.FadeToClear(respawnFadeDuration);

            // --- Appearance Part ---
            playerVisualController?.ShowArmObject();
            playerVisualController?.GetBodyAnimator()?.SetTrigger(GameConstants.AnimRespawnTrigger);
            StartInvulnerability();

            yield return new WaitForSeconds(respawnAnimDuration);
            
            // --- Finalization ---
            if (playerMotor != null) playerMotor.enabled = true;
            _isProcessingCriticalSequence = false;
        }

        private IEnumerator FinalDeathSequence()
        {
            IsDead = true;
            _isProcessingCriticalSequence = true;
            if (playerMotor != null) playerMotor.enabled = false;
            if (playerRb != null) { playerRb.linearVelocity = Vector2.zero; playerRb.simulated = false; }
            playerVisualController?.HideArmObject();

            // Trigger death animation and event
            playerVisualController?.GetBodyAnimator()?.SetTrigger(GameConstants.AnimLoseLifeTrigger);
            PlayerEvents.RaisePlayerDeath();

            // Camera zoom effect
            if (playerCamera != null)
            {
                float startTime = Time.unscaledTime;
                float startSize = playerCamera.Lens.OrthographicSize;
                while (Time.unscaledTime < startTime + deathCameraZoomDuration)
                {
                    float t = (Time.unscaledTime - startTime) / deathCameraZoomDuration;
                    playerCamera.Lens.OrthographicSize = Mathf.Lerp(startSize, deathCameraZoomSize, t);
                    yield return null;
                }
                playerCamera.Lens.OrthographicSize = deathCameraZoomSize;
            }
            
            // The GameOverUIController will take over from the OnPlayerDeath event.
        }

        private void StartInvulnerability()
        {
            _isInvulnerable = true;
            playerVisualController?.StartCoroutine(playerVisualController.FlashSpriteCoroutine(invulnerabilityDuration, invulnerabilityFlashInterval));
            StartCoroutine(EndInvulnerability(invulnerabilityDuration));
        }

        private IEnumerator EndInvulnerability(float duration)
        {
            yield return new WaitForSeconds(duration);
            _isInvulnerable = false;
        }

        public void HealLife(int amount)
        {
            if (IsDead || amount <= 0) return;
            _currentLives = Mathf.Min(_currentLives + amount, maxLives);
            PlayerEvents.RaiseHealthChanged(_currentLives, _currentArmor);
        }

        public void HealArmor(int amount)
        {
            if (IsDead || _currentLives < 0 || amount <= 0) return;
            _currentArmor = Mathf.Min(_currentArmor + amount, maxArmorPerLife);
            PlayerEvents.RaiseHealthChanged(_currentLives, _currentArmor);
        }
        
        public void ApplyInstakill()
        {
            if (IsDead || _isProcessingCriticalSequence) return;
            _isProcessingCriticalSequence = true;
            _currentLives = -1; // Ensure it triggers final death
            TakeDamage(999);
        }
    }
}