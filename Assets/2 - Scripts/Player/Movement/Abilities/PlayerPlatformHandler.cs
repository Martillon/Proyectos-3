using UnityEngine;
using System.Collections;
using Scripts.Core;
using Scripts.Player.Core; // For PlayerStateManager, GameConstants, SceneLoader

namespace Scripts.Player.Movement.Abilities
{
    /// <summary>
    /// Manages the player's ability to drop through one-way platforms.
    /// </summary>
    public class PlayerPlatformHandler : MonoBehaviour
    {
        [Header("Platform Drop Settings")]
        [Tooltip("Duration (in seconds) for which collision with the dropped platform is ignored.")]
        [SerializeField] private float dropThroughTime = 0.25f;

        private PlayerStateManager _playerStateManager;
        private Rigidbody2D _rb; // Necesario para el pequeño empujón o ajuste de velocidad

        // Referencia al collider activo del jugador (obtenido del StateManager o de los GOs directamente)
        // Esto es crucial. PlayerStateManager necesita exponer cuál es el collider activo.
        // private Collider2D _currentPlayerCollider; // Se obtendría de PlayerStateManager

        private Coroutine _dropCoroutine;

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            _rb = GetComponentInParent<Rigidbody2D>(); // El Rigidbody está en el Player_Root

            if (_playerStateManager == null) Debug.LogError("PlayerPlatformHandler: PlayerStateManager not found!", this);
            if (_rb == null) Debug.LogError("PlayerPlatformHandler: Rigidbody2D not found on parent!", this);
        }

        void Update()
        {
            if (_playerStateManager == null) return;

            // Condiciones para iniciar el drop:
            // 1. Se presionó el input de salto este frame.
            // 2. No se está ya en proceso de drop.
            // 3. El jugador está lógicamente en el suelo.
            // 4. El jugador tiene la intención de presionar hacia abajo.
            // 5. El suelo sobre el que está es una plataforma "one-way".
            if (_playerStateManager.JumpInputDown &&
                !_playerStateManager.IsDroppingFromPlatform &&
                _playerStateManager.IsGrounded &&
                _playerStateManager.IntendsToPressDown &&
                _playerStateManager.IsOnOneWayPlatform) // IsOnOneWayPlatform es actualizado por PlayerGroundDetector
            {
                if (_dropCoroutine != null) StopCoroutine(_dropCoroutine);
                _dropCoroutine = StartCoroutine(DropThroughPlatformProcess());
            }
        }

        private IEnumerator DropThroughPlatformProcess()
        {
            _playerStateManager.UpdateDroppingState(true);
            
            // Si el jugador estaba visualmente agachado, el PlayerCrouchHandler ya debería haberlo "levantado"
            // (cambiado al collider de pie) porque IsDroppingFromPlatform se vuelve true, y por ende
            // IsCrouchingLogic se vuelve false. Si no, PlayerCrouchHandler necesita reaccionar a IsDropping.
            // PlayerStateManager.UpdateCrouchVisualState(false); // Podría ser necesario forzarlo aquí o en CrouchHandler.

            Collider2D platformToIgnore = FindPlatformBeneath();
            Collider2D activePlayerCollider = _playerStateManager.ActivePlayerCollider; // NECESITA ESTAR EN STATEMANAGER

            if (platformToIgnore != null && activePlayerCollider != null)
            {
                // Debug.Log($"PlayerPlatformHandler: Ignoring collision with {platformToIgnore.name}", this);
                Physics2D.IgnoreCollision(activePlayerCollider, platformToIgnore, true);
                
                // Opcional: pequeño empujón o ajuste de velocidad para asegurar el paso
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -1.5f); // Pequeña velocidad hacia abajo

                yield return new WaitForSeconds(dropThroughTime);

                // Verificar si el collider del jugador sigue existiendo (podría haber muerto, etc.)
                if (activePlayerCollider != null && platformToIgnore != null) 
                {
                    Physics2D.IgnoreCollision(activePlayerCollider, platformToIgnore, false);
                    // Debug.Log($"PlayerPlatformHandler: Restored collision with {platformToIgnore.name}", this);
                }
            }
            else
            {
                // Debug.LogWarning("PlayerPlatformHandler: No specific platform to ignore found, or active player collider is null. Waiting generic time.", this);
                yield return new WaitForSeconds(dropThroughTime * 0.5f); 
            }
            
            _playerStateManager.UpdateDroppingState(false);
            _dropCoroutine = null;
        }

        private Collider2D FindPlatformBeneath()
        {
            // Necesitamos el groundCheckOrigin. Podríamos pasarlo o asumirlo.
            // Por ahora, asumimos que PlayerGroundDetector es la fuente de verdad para esto.
            // Esta función es más compleja si PlayerGroundDetector no expone los colliders detectados.
            // La forma más simple es que PlayerGroundDetector ya haya identificado IsOnOneWayPlatform.
            // Para obtener el *collider específico*, necesitaríamos que PlayerGroundDetector lo exponga
            // o rehacer parte de su lógica de OverlapCircle aquí.

            // Solución simple: Asumir que si IsOnOneWayPlatform es true, cualquier collider con el tag es el bueno.
            // Esto podría no ser perfecto si hay múltiples plataformas solapadas.
            if (_playerStateManager.IsOnOneWayPlatform)
            {
                // Necesitamos una referencia al groundCheckOrigin de PlayerGroundDetector
                // O PlayerGroundDetector necesita exponer el collider de la plataforma detectada.
                // Esto se está volviendo una dependencia circular o demasiado compleja.

                // --- Alternativa: PlayerGroundDetector actualiza PlayerStateManager con el collider ---
                // En PlayerStateManager:
                // public Collider2D CurrentPlatformCollider { get; private set; }
                // public void UpdateGroundedState(bool isGrounded, bool isOnPlatform, Collider2D platformCol) { ...; CurrentPlatformCollider = isOnPlatform ? platformCol : null; }
                // En PlayerGroundDetector:
                // ... _playerStateManager.UpdateGroundedState(_isCurrentlyGrounded, _isOnPlatformThisFrame, detectedPlatformCollider);
                // Entonces aquí:
                return _playerStateManager.CurrentGroundPlatformCollider; // Este sería el collider de la plataforma one-way
            }
            return null;
        }
    }
}
// --- END OF FILE PlayerPlatformHandler.cs ---
