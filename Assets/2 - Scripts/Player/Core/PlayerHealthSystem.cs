// --- START OF FILE PlayerHealthSystem.cs ---
using System.Collections;
using Scripts.Core;
using Scripts.Core.Interfaces;
using Scripts.Checkpoints;
using Scripts.Items.Checkpoint;
using Unity.Cinemachine;
using UnityEngine;
using Scripts.Player.Core;
using Scripts.Player.Movement;

namespace Scripts.Player.Core
{
    // Ya no necesita [RequireComponent(typeof(SpriteRenderer))] si el SpriteRenderer está en un hijo
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable
    {
        [Header("Health Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int maxArmorPerLife = 3;
        [SerializeField] private float invulnerabilityDuration = 1.5f;

        [Header("Visual Feedback")]
        [Tooltip("SpriteRenderer for the player's main body. Used for flashing effect during invulnerability. Should be assigned if not on this GameObject.")]
        [SerializeField] private SpriteRenderer playerBodySpriteRenderer; // <--- NUEVO CAMPO / MODIFICADO
        [Tooltip("Interval (in seconds) for sprite flashing during invulnerability.")]
        [SerializeField] private float invulnerabilityFlashInterval = 0.1f;

        // ... (resto de tus campos existentes: playerAnimator, deathAnimationTrigger, etc.) ...
        [Header("Death & Respawn")]
        [SerializeField] private Animator playerAnimator; // Este debería ser el Animator en PlayerVisuals/BodySprite
        [SerializeField] private string deathAnimationTrigger = GameConstants.AnimDieTrigger; // Usando GameConstants
        [SerializeField] private string respawnAnimationTrigger = GameConstants.AnimRespawnTrigger;
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
            
            // Si playerBodySpriteRenderer no se asigna en el Inspector, intenta encontrarlo en hijos.
            // Esto asume una estructura como PlayerRoot -> PlayerVisuals -> BodySprite (con SpriteRenderer)
            if (playerBodySpriteRenderer == null)
            {
                playerBodySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true); // true para incluir inactivos
                if (playerBodySpriteRenderer == null)
                {
                    Debug.LogError("PlayerHealthSystem: Player Body SpriteRenderer not found or assigned! Flashing will not work.", this);
                }
            }
            // Similar para playerAnimator si no está en el mismo objeto que PlayerHealthSystem
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>(true);
                if (playerAnimator == null)
                {
                     Debug.LogWarning("PlayerHealthSystem: Player Animator not found or assigned. Death/respawn animations might not play.", this);
                }
            }


            if (playerFocusedCamera != null)
            {
                initialCameraOrthographicSize = playerFocusedCamera.Lens.OrthographicSize;
            }
        }

        private void Start()
        {
            // ... (tu lógica de Start existente, incluyendo el reseteo de checkpoints y la inicialización de salud) ...
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
            isDead = false; 
            isInvulnerable = false; 
            if (invulnerabilityFlashCoroutine != null) 
            {
                StopCoroutine(invulnerabilityFlashCoroutine);
                if(playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true; 
            }
            if (playerFocusedCamera != null && Mathf.Abs(playerFocusedCamera.Lens.OrthographicSize - initialCameraOrthographicSize) > 0.01f)
            {
                playerFocusedCamera.Lens.OrthographicSize = initialCameraOrthographicSize;
            }
            CheckpointManager.ResetCheckpointData(); 
            CheckpointManager.SetInitialLevelSpawnPoint(transform.position); 
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); 
            InputManager.Instance?.EnablePlayerControls();
            Time.timeScale = 1f;
        }

        public void TakeDamage(float damageAmount)
        {
            if (isInvulnerable || isDead) return;

            currentArmor--;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor < 0 ? 0 : currentArmor);

            if (currentArmor < 0)
            {
                currentLives--;
                if (currentLives < 0)
                {
                    currentLives = 0; 
                    PlayerEvents.RaiseHealthChanged(currentLives, 0); 
                    StartCoroutine(FinalDeathSequence());
                }
                else
                {
                    // Actualiza la UI para mostrar la nueva cantidad de vidas, y que la armadura se restaurará.
                    PlayerEvents.RaiseHealthChanged(currentLives, maxArmorPerLife); 
                    StartCoroutine(LoseLifeAndRespawnSequence());
                }
            }
            else
            {
                StartCoroutine(BeginInvulnerability());
            }
        }
        
        // --- SECUENCIAS DE MUERTE Y RESPAWN ---
        private IEnumerator LoseLifeAndRespawnSequence()
        {
            isDead = true; 
            isInvulnerable = true; 

            InputManager.Instance?.DisableAllControls();
            if (playerMovement != null) playerMovement.enabled = false;

            if (playerAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                playerAnimator.SetTrigger(deathAnimationTrigger);
                // Estima un tiempo para la animación de "perder vida" o usa un evento de animación si es complejo
                yield return new WaitForSeconds(0.75f); 
            }
            
            if (playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = false; 
            yield return new WaitForSeconds(0.25f); 

            transform.position = CheckpointManager.GetCurrentRespawnPosition();
            
            if (playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true;

            currentArmor = maxArmorPerLife; // Restaura la armadura para la vida recuperada
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);

            if (playerAnimator != null && !string.IsNullOrEmpty(respawnAnimationTrigger))
            {
                 playerAnimator.SetTrigger(respawnAnimationTrigger);
                 yield return new WaitForSeconds(0.5f); 
            }

            if (playerMovement != null) playerMovement.enabled = true;
            InputManager.Instance?.EnablePlayerControls();
            
            isDead = false; 
            StartCoroutine(BeginInvulnerability()); // Invulnerabilidad después de respawnear
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
            
            PlayerEvents.RaisePlayerDeath(); // Notifica a GameOverUIController, etc.

            if (playerFocusedCamera != null)
            {
                float startTime = Time.unscaledTime; 
                float currentCamSize = playerFocusedCamera.Lens.OrthographicSize; // Cachear al inicio del lerp
                while (Time.unscaledTime < startTime + deathCameraZoomDuration)
                {
                    float t = (Time.unscaledTime - startTime) / deathCameraZoomDuration;
                    playerFocusedCamera.Lens.OrthographicSize = Mathf.Lerp(currentCamSize, deathCameraZoomSize, t);
                    yield return null; 
                }
                playerFocusedCamera.Lens.OrthographicSize = deathCameraZoomSize; 
            }
            else
            {
                yield return new WaitForSecondsRealtime(1.0f); 
            }
            // El juego debería estar pausado por GameOverUIController en este punto
        }

        // --- INVULNERABILIDAD Y FEEDBACK VISUAL ---
        private IEnumerator BeginInvulnerability()
        {
            isInvulnerable = true;
            if (invulnerabilityFlashCoroutine != null) StopCoroutine(invulnerabilityFlashCoroutine);
            
            // Solo inicia el flash si el SpriteRenderer del cuerpo está asignado y el GO está activo
            if (playerBodySpriteRenderer != null && playerBodySpriteRenderer.gameObject.activeInHierarchy) 
            {
                 invulnerabilityFlashCoroutine = StartCoroutine(FlashSpriteCoroutine(playerBodySpriteRenderer, invulnerabilityDuration, invulnerabilityFlashInterval));
            }
           
            yield return new WaitForSeconds(invulnerabilityDuration); 
            
            isInvulnerable = false;
            if (invulnerabilityFlashCoroutine != null) 
            {
                StopCoroutine(invulnerabilityFlashCoroutine);
                if(playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true; 
            }
        }

        private IEnumerator FlashSpriteCoroutine(SpriteRenderer spriteToFlash, float duration, float interval)
        {
            if (spriteToFlash == null) yield break;
            
            // Debug.Log($"FlashSpriteCoroutine START for {duration}s, interval {interval}s", this); // Uncomment for debugging
            float endTime = Time.time + duration;
            bool originalVisibility = spriteToFlash.enabled; // Captura el estado inicial

            try
            {
                while (Time.time < endTime && isInvulnerable) // Continúa solo si sigue invulnerable
                {
                    spriteToFlash.enabled = !spriteToFlash.enabled;
                    yield return new WaitForSeconds(interval);
                }
            }
            finally // Bloque finally para asegurar que el sprite se restaure
            {
                spriteToFlash.enabled = originalVisibility; // Restaura al estado original (o siempre true)
                // Debug.Log($"FlashSpriteCoroutine END. Sprite enabled: {spriteToFlash.enabled}", this); // Uncomment for debugging
            }
        }

        // --- MÉTODOS DE CURACIÓN ---
        public void HealLife(int amount)
        {
            // ... (lógica existente sin cambios significativos) ...
            if (isDead || amount <= 0) return;
            int previousLives = currentLives;
            currentLives = Mathf.Min(currentLives + amount, maxLives);
            if(currentLives > previousLives) currentArmor = maxArmorPerLife;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        public void HealArmor(int amount)
        {
            // ... (lógica existente sin cambios significativos) ...
            if (isDead || currentLives < 0 || amount <= 0) return;
            currentArmor = (amount >= maxArmorPerLife) ? maxArmorPerLife : Mathf.Min(currentArmor + amount, maxArmorPerLife);
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        // --- GETTERS Y RESET ---
        public int GetCurrentLives() => currentLives;
        public int GetCurrentArmor() => currentArmor;
        public bool IsCurrentlyInvulnerable() => isInvulnerable;

        public void FullResetPlayerHealth()
        {
            // ... (lógica existente) ...
            currentLives = maxLives; currentArmor = maxArmorPerLife; isDead = false; isInvulnerable = false;
            if (invulnerabilityFlashCoroutine != null) StopCoroutine(invulnerabilityFlashCoroutine);
            if(playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true;
            if (playerFocusedCamera != null) playerFocusedCamera.Lens.OrthographicSize = initialCameraOrthographicSize;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        /// <summary>
        /// Public method to directly trigger the final death sequence.
        /// Used by DeathZones or other instakill mechanics.
        /// </summary>
        public void TriggerInstakill()
        {
            if (isDead) return;
            currentLives = 0; 
            currentArmor = 0; 
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); 
            StartCoroutine(FinalDeathSequence());
        }
    }
}
// --- END OF FILE PlayerHealthSystem.cs ---