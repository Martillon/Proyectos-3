using System.Collections;
using Scripts.Core;
using Scripts.Core.Interfaces;
using Scripts.Items.Checkpoint;
using Unity.Cinemachine;
using UnityEngine;
using Scripts.Player.Movement;
using Scripts.Player.Movement.Motor;
using Scripts.Player.Visuals; // Asumo que PlayerMovement2D es para deshabilitar el movimiento
using Scripts.Player.Weapons; // <--- NUEVO: Para acceder a WeaponBase

namespace Scripts.Player.Core
{
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable
    {
        [Header("Health Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int maxArmorPerLife = 3;
        [SerializeField] private float invulnerabilityDuration = 1.5f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer playerBodySpriteRenderer;
        [SerializeField] private float invulnerabilityFlashInterval = 0.1f;

        [Header("Death & Respawn")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string deathAnimationTrigger = GameConstants.AnimDieTrigger;
        [SerializeField] private string respawnAnimationTrigger = GameConstants.AnimRespawnTrigger;
        [SerializeField] private CinemachineCamera playerFocusedCamera;
        [SerializeField] private float deathCameraZoomSize = 3f;
        [SerializeField] private float deathCameraZoomDuration = 1.5f;
        [Tooltip("Nombre del trigger para la animación de 'stun' al perder una vida (no la muerte final).")]
        [SerializeField] private string loseLifeStunTrigger = "StunTrigger";

        // --- Referencias a otros componentes del jugador ---
        private PlayerMotor playerMovement;
        private WeaponBase playerWeaponBase;   
        private PlayerVisualController playerVisualController; // NUEVO: Para controlar el brazo/arma
        
        private int currentLives;
        private int currentArmor;
        private bool isInvulnerable = false;
        private bool isDead = false;
        private float initialCameraOrthographicSize;
        private Coroutine invulnerabilityFlashCoroutine;
        
        
        private void Awake()
        {
            // Intenta obtener PlayerMovement2D del mismo GameObject o de un padre si es necesario.
            // Si está en Player_Root y PlayerHealthSystem también, GetComponent es suficiente.

            // Obtener WeaponBase
            // Asumimos que PlayerHealthSystem y WeaponBase comparten un ancestro común (Player_root)
            // y WeaponBase está en un hijo de ese ancestro.
            Transform playerRoot = transform.parent?.parent; // Sube dos niveles: LogicContainer_Health -> Logic -> Player_root
            // O si LogicContainer_Health está directamente bajo Player_root:
            // Transform playerRoot = transform.parent; // LogicContainer_Health -> Player_root

            if (playerRoot != null)
            {
                playerWeaponBase = playerRoot.GetComponentInChildren<WeaponBase>(true); // Busca en Player_root y todos sus hijos (incluyendo inactivos)
            }
    
            // Fallback o alternativa si la estructura es más plana o Player_root es el padre directo del GO de este script
            if (playerWeaponBase == null && transform.parent != null) // Si no se encontró arriba, o si playerRoot no era el correcto
            {
                // Quizás Player_root es el padre directo de LogicContainer_Health
                // y LogicContainer_Weapons también es hijo directo de Player_root
                playerWeaponBase = transform.parent.GetComponentInChildren<WeaponBase>(true);
            }
            
            if(playerRoot != null)
            {
                playerMovement = playerRoot.GetComponentInChildren<PlayerMotor>(true); // Busca en Player_root y todos sus hijos (incluyendo inactivos)
                playerVisualController = playerRoot.GetComponentInChildren<PlayerVisualController>(true); // Busca en Player_root y todos sus hijos (incluyendo inactivos)
            }
            else
            {
                Debug.LogError("PlayerHealthSystem: Player root not found in hierarchy! Cannot access PlayerMotor or PlayerVisualController.", this);
            }

            if (playerWeaponBase == null)
            {
                Debug.LogError("PlayerHealthSystem: WeaponBase component not found on player hierarchy! " +
                               "Cannot reset weapon on death. Check player prefab structure and WeaponBase location.", this);
            }

            if (playerBodySpriteRenderer == null)
            {
                playerBodySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
                if (playerBodySpriteRenderer == null)
                {
                    Debug.LogError("PlayerHealthSystem: Player Body SpriteRenderer not found or assigned! Flashing will not work.", this);
                }
            }
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
            // Asegúrate de que esto se llame *después* de que Player_Root (y por tanto el jugador) esté en su posición inicial en la escena.
            // Si Start() de PlayerHealthSystem se ejecuta antes de que el nivel posicione al jugador, este spawn point podría ser incorrecto.
            // Considera llamarlo desde un LevelManager o similar después de la carga/preparación del nivel.
            CheckpointManager.SetInitialLevelSpawnPoint(transform.position); 
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); 
            InputManager.Instance?.EnablePlayerControls();
            Time.timeScale = 1f;

            // Asegurarse de que el arma inicial esté equipada al comenzar el juego/nivel.
            // WeaponBase ya lo hace en su Awake/Start si initialUpgradePrefab está asignado.
        }

        public void TakeDamage(float damageAmount)
        {
            if (isInvulnerable || isDead) return;

            int damage = Mathf.FloorToInt(damageAmount); // Asumiendo que el daño siempre es entero o se redondea hacia abajo.
            currentArmor -= damage;
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
                    PlayerEvents.RaiseHealthChanged(currentLives, maxArmorPerLife); 
                    StartCoroutine(LoseLifeAndRespawnSequence());
                }
            }
            else
            {
                StartCoroutine(BeginInvulnerability());
            }
        }
        
         private IEnumerator LoseLifeAndRespawnSequence()
        {
            Debug.Log($"[{Time.frameCount}] PHS: LoseLifeAndRespawnSequence STARTED");
            isDead = true; // Temporalmente "muerto" para esta secuencia
            isInvulnerable = true;

            InputManager.Instance?.DisableAllControls();
            if (playerMovement != null) playerMovement.enabled = false;

            // Opcional: Ocultar el brazo/arma
            playerVisualController?.HideArmObject(); // Necesitarás añadir HideArmObject() a PlayerVisualController

            // Disparar animación de Stun/HitRecoilHeavy
            if (playerAnimator != null && !string.IsNullOrEmpty(loseLifeStunTrigger))
            {
                Debug.Log($"[{Time.frameCount}] PHS: Triggering '{loseLifeStunTrigger}' animation.");
                playerAnimator.SetTrigger(loseLifeStunTrigger);
                // Esperar a que la animación de stun se complete o una duración fija
                yield return new WaitForSeconds(0.6f); // Ajusta esta duración
            }
            else
            {
                yield return new WaitForSeconds(0.5f); // Delay si no hay animación de stun
            }
            
            // Opcional: Hacer invisible al jugador brevemente durante el "teletransporte"
            if (playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = false;
            // yield return new WaitForSeconds(0.1f); // Pequeño delay opcional

            // Mover al checkpoint
            Vector3 respawnPosition = CheckpointManager.GetCurrentRespawnPosition();
            Debug.Log($"[{Time.frameCount}] PHS: Teleporting to respawn position: {respawnPosition}");
            transform.root.position = respawnPosition; // Mover el Player_root completo

            // Resetear arma
            if (playerWeaponBase != null)
            {
                playerWeaponBase.EquipInitialUpgrade();
                Debug.Log($"[{Time.frameCount}] PHS: Weapon reset to initial.");
            }

            // Hacer visible al jugador de nuevo
            if (playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true;
            // Opcional: Reactivar el brazo/arma (si se ocultó)
            playerVisualController?.ShowArmObject(); // Necesitarás añadir ShowArmObject() a PlayerVisualController

            // Resetear armadura y actualizar UI
            currentArmor = maxArmorPerLife; // Restaura armadura a tope para la nueva "vida"
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); // currentLives ya fue decrementado en TakeDamage

            // Disparar animación de Respawn
            if (playerAnimator != null && !string.IsNullOrEmpty(respawnAnimationTrigger))
            {
                Debug.Log($"[{Time.frameCount}] PHS: Triggering '{respawnAnimationTrigger}' animation.");
                playerAnimator.SetTrigger(respawnAnimationTrigger);
                // Esperar a que la animación de respawn se complete o una duración fija
                yield return new WaitForSeconds(0.5f); // Ajusta esta duración
            }
            else
            {
                 yield return new WaitForSeconds(0.3f); // Delay si no hay animación de respawn
            }

            // Reactivar controles y movimiento
            if (playerMovement != null) playerMovement.enabled = true;
            InputManager.Instance?.EnablePlayerControls();
            
            isDead = false; // Ya no está "muerto"
            Debug.Log($"[{Time.frameCount}] PHS: LoseLifeAndRespawnSequence FINISHED. Player is back.");
            StartCoroutine(BeginInvulnerability()); // Iniciar invulnerabilidad post-respawn
        }

        private IEnumerator FinalDeathSequence()
        {
            isDead = true;
            isInvulnerable = true;

            InputManager.Instance?.DisableAllControls();
            if (playerMovement != null) playerMovement.enabled = false;

            // También podrías querer resetear el arma aquí si el juego permite "continuar" desde un game over
            // volviendo al checkpoint pero con el arma inicial. Por ahora, lo dejamos así.

            if (playerAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                playerAnimator.SetTrigger(deathAnimationTrigger);
            }
            
            PlayerEvents.RaisePlayerDeath(); 

            if (playerFocusedCamera != null)
            {
                float startTime = Time.unscaledTime; 
                float currentCamSize = playerFocusedCamera.Lens.OrthographicSize; 
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
        }

        private IEnumerator BeginInvulnerability()
        {
            isInvulnerable = true;
            if (invulnerabilityFlashCoroutine != null) StopCoroutine(invulnerabilityFlashCoroutine);
            
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
            
            float endTime = Time.time + duration;
            // bool originalVisibility = spriteToFlash.enabled; // No es necesario si siempre lo pones true al final

            try
            {
                while (Time.time < endTime && isInvulnerable) 
                {
                    spriteToFlash.enabled = !spriteToFlash.enabled;
                    yield return new WaitForSeconds(interval);
                }
            }
            finally
            {
                spriteToFlash.enabled = true; // Siempre asegurar que sea visible al final
            }
        }

        public void HealLife(int amount)
        {
            if (isDead || amount <= 0) return;
            int previousLives = currentLives;
            currentLives = Mathf.Min(currentLives + amount, maxLives);
            if(currentLives > previousLives && currentLives <= maxLives) { // Solo restaura armadura si realmente ganó una vida y no estaba ya al máximo de vidas
                 currentArmor = maxArmorPerLife;
            }
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        public void HealArmor(int amount)
        {
            if (isDead || currentLives < 0 || amount <= 0) return; // currentLives < 0 es redundante si isDead ya lo cubre
            currentArmor = Mathf.Min(currentArmor + amount, maxArmorPerLife);
            if (currentLives == 0 && currentArmor > 0) currentArmor = 0; // No deberías tener armadura si no tienes vidas (lógica de game over)
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        public int GetCurrentLives() => currentLives;
        public int GetCurrentArmor() => currentArmor;
        public bool IsCurrentlyInvulnerable() => isInvulnerable;

        public void FullResetPlayerHealth() // Usado para reiniciar nivel o similar
        {
            currentLives = maxLives; 
            currentArmor = maxArmorPerLife; 
            isDead = false; 
            isInvulnerable = false;
            if (invulnerabilityFlashCoroutine != null) StopCoroutine(invulnerabilityFlashCoroutine);
            if(playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true;
            if (playerFocusedCamera != null) playerFocusedCamera.Lens.OrthographicSize = initialCameraOrthographicSize;
            
            if (playerWeaponBase != null)
            {
                playerWeaponBase.EquipInitialUpgrade(); // También resetea el arma aquí
            }

            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

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