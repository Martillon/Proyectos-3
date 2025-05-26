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
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable, IInstakillable
    {
        [Header("Health Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int maxArmorPerLife = 3;
        [SerializeField] private float invulnerabilityDuration = 1.5f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer playerBodySpriteRenderer;
        [SerializeField] private float invulnerabilityFlashInterval = 0.1f;

        [Header("Lose Life & Respawn Sequence Config")] // SECCIÓN RENOMBRADA Y CON NUEVOS CAMPOS
        [Tooltip("Animator principal del jugador.")]
        [SerializeField] private Animator playerAnimator;
        [Tooltip("Nombre del trigger para la animación de 'herido' al perder una vida.")]
        [SerializeField] private string loseLifeHurtTrigger = "HurtTrigger"; 
        [Tooltip("Duración (en segundos) que se espera después de la animación de 'herido' y ANTES del fade a negro.")]
        [SerializeField] private float hurtStateDuration = 0.75f; 
        [Tooltip("Nombre del trigger para la animación de 'reaparición' en el checkpoint.")]
        [SerializeField] private string respawnAppearTrigger = "RespawnAppearTrigger";
        [Tooltip("Duración (en segundos) que se espera después de la animación de 'reaparición' y DESPUÉS del fade de vuelta.")]
        [SerializeField] private float respawnStateDuration = 0.8f; 
        [Tooltip("Duración del fade in/out de pantalla para la secuencia de respawn.")]
        [SerializeField] private float screenFadeDurationForRespawn = 0.5f; 

        [Header("Final Death (Game Over) Sequence Config")]
        [Tooltip("Nombre del trigger para la animación de muerte final.")]
        [SerializeField] private string finalDeathAnimationTrigger = GameConstants.AnimDieTrigger;
        [SerializeField] private CinemachineCamera playerFocusedCamera; // Para el zoom de Game Over
        [SerializeField] private float deathCameraZoomSize = 3f;
        [SerializeField] private float deathCameraZoomDuration = 1.5f;

        // --- Referencias a otros componentes del jugador ---
        private PlayerMotor playerMovement;
        private WeaponBase playerWeaponBase;   
        private PlayerVisualController playerVisualController; // NUEVO: Para controlar el brazo/arma
        
        private int currentLives;
        private int currentArmor;
        private bool isInvulnerable = false;
        private bool isDead = false;
        private float initialCameraOrthographicSize;
        private Coroutine _activeRespawnSequence;
        private Coroutine invulnerabilityFlashCoroutine;
        private Rigidbody2D playerRb;
        
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
            
            if(playerRb == null)
            {
                playerRb = playerRoot.GetComponent<Rigidbody2D>();
                if (playerRb == null)
                {
                    Debug.LogError("PlayerHealthSystem: Rigidbody2D not found on player hierarchy! Cannot reset velocity on damage.", this);
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
            if (isInvulnerable || isDead || _activeRespawnSequence != null) return; // No tomar daño si ya está en una secuencia

            int damage = Mathf.FloorToInt(damageAmount);
            currentArmor -= damage;

            if (currentArmor < 0) // Se rompió la armadura, se considera perder una vida
            {
                currentLives--;
                Debug.Log($"PHS: Armor broken or gone. Lives now: {currentLives}");

                if (currentLives < 0) // Vidas llegaron a -1 (es decir, perdió la última vida que era 0)
                {
                    currentLives = 0; 
                    currentArmor = 0;
                    PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
                    isDead = true; // Marcar para Game Over
                    if (_activeRespawnSequence != null) StopCoroutine(_activeRespawnSequence); // Detener cualquier respawn en curso
                    StartCoroutine(FinalDeathSequence());
                }
                else // Perdió una vida, pero aún quedan (currentLives >= 0)
                {
                    // Aunque currentLives pueda ser 0 aquí, no es Game Over inmediato,
                    // sino que es la "última vida" que se acaba de perder. El Game Over es si baja de 0.
                    // Si currentLives es 0 DESPUÉS de decrementar, significa que acaba de perder su ÚLTIMA vida.
                    // En muchos juegos, esto también es Game Over. Si quieres "0 vidas = game over",
                    // la condición de arriba sería currentLives <= 0.
                    // Asumamos que "perder una vida" significa que aún no es game over inmediato si currentLives >= 0.

                    currentArmor = maxArmorPerLife; // Restaurar armadura para la "nueva vida" que va a usar
                    PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); 
                    if (_activeRespawnSequence != null) StopCoroutine(_activeRespawnSequence);
                    _activeRespawnSequence = StartCoroutine(LoseLifeAndRespawnSequence()); // Nueva corrutina con fades
                }
            }
            else // Solo se dañó la armadura
            {
                PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
                StartCoroutine(BeginInvulnerability());
            }
        }
        
        // Implementación de IInstakillable
        public void ApplyInstakill()
        {
            Debug.Log($"[{Time.frameCount}] PHS: ApplyInstakill called on {gameObject.name}.");
            if (isDead || _activeRespawnSequence != null) return; // Si ya está muerto o en secuencia, no hacer nada

            // Forzar directamente la secuencia de muerte final
            currentLives = 0;
            currentArmor = 0;
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
            isDead = true; // Marcar para Game Over
            if (_activeRespawnSequence != null) StopCoroutine(_activeRespawnSequence);
            StartCoroutine(FinalDeathSequence());
        }
        
         private IEnumerator LoseLifeAndRespawnSequence() 
        {
            //Debug.Log($"[{Time.frameCount}] PHS: LoseLifeAndRespawnSequence STARTED");
            isInvulnerable = true; 
            
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;

            InputManager.Instance?.DisableAllControls(); 
            if (playerMovement != null) playerMovement.enabled = false;
            playerVisualController?.HideArmObject();

            // 1. Animación de Herido
            if (playerAnimator != null && !string.IsNullOrEmpty(loseLifeHurtTrigger))
            {
                playerAnimator.SetTrigger(loseLifeHurtTrigger);
            }
            yield return new WaitForSeconds(hurtStateDuration); 
            
            // 2. Fade a Negro
            //Debug.Log($"[{Time.frameCount}] PHS: Fading to black for respawn.");
            if (ScreenFader.Instance != null)
            {
                // Esperar a que la corrutina de fade termine
                yield return ScreenFader.Instance.FadeToBlack(screenFadeDurationForRespawn); 
            }
            else
            {
                Debug.LogWarning("PHS: ScreenFader.Instance not found! Skipping fade to black.");
                yield return new WaitForSeconds(screenFadeDurationForRespawn); // Simular espera si no hay fader
            }

            // 3. Durante el Negro: Mover al Checkpoint, Ocultar Sprite Temporalmente, Resetear Arma
            if (playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = false; 

            transform.root.position = CheckpointManager.GetCurrentRespawnPosition(); 
            
            if (playerWeaponBase != null) playerWeaponBase.EquipInitialUpgrade();
            // currentArmor ya se reseteó o se mantuvo en TakeDamage.

            // 4. Fade de Vuelta (Aparecer)
            // Hacer visible el sprite justo ANTES de que el fade comience a aclararse
            if (playerBodySpriteRenderer != null) playerBodySpriteRenderer.enabled = true; 
            playerVisualController?.ShowArmObject();

            Debug.Log($"[{Time.frameCount}] PHS: Fading to clear for respawn.");
            if (ScreenFader.Instance != null)
            {
                // Esperar a que la corrutina de fade termine
                yield return ScreenFader.Instance.FadeToClear(screenFadeDurationForRespawn);
            }
            else
            {
                Debug.LogWarning("PHS: ScreenFader.Instance not found! Skipping fade to clear.");
                yield return new WaitForSeconds(screenFadeDurationForRespawn); // Simular espera
            }

            // 5. Animación de Reaparición (Teleport In)
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor); 

            if (playerAnimator != null && !string.IsNullOrEmpty(respawnAppearTrigger))
            {
                playerAnimator.SetTrigger(respawnAppearTrigger);
            }
            yield return new WaitForSeconds(respawnStateDuration); 
            
            // 6. Reactivar todo
            if (playerMovement != null) playerMovement.enabled = true;
            InputManager.Instance?.EnablePlayerControls();
            
            _activeRespawnSequence = null; 
            //Debug.Log($"[{Time.frameCount}] PHS: LoseLifeAndRespawnSequence FINISHED.");
            StartCoroutine(BeginInvulnerability()); 
        }

        private IEnumerator FinalDeathSequence()
        {
            isDead = true;
            isInvulnerable = true;

            InputManager.Instance?.DisableAllControls();
            if (playerMovement != null) playerMovement.enabled = false;

            // También podrías querer resetear el arma aquí si el juego permite "continuar" desde un game over
            // volviendo al checkpoint pero con el arma inicial. Por ahora, lo dejamos así.

            if (playerAnimator != null && !string.IsNullOrEmpty(finalDeathAnimationTrigger))
            {
                playerAnimator.SetTrigger(finalDeathAnimationTrigger);
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
            ApplyInstakill();
        }
    }
}