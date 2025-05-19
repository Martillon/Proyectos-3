// --- START OF FILE AimDirectionResolver.cs (Modificado para usar PlayerStateManager) ---
using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core; // For InputManager, PlayerStateManager
// Ya no necesita Scripts.Player.Movement directamente si lee estados del PlayerStateManager

namespace Scripts.Player.Weapons
{
    public class AimDirectionResolver : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the PlayerStateManager to get player state.")]
        [SerializeField] private PlayerStateManager playerStateManager; // <--- CAMBIO: Referencia principal

        [Header("Aiming Configuration")]
        [SerializeField] private float aimingDownThreshold = -0.7f;

        [Header("Debug")]
        [SerializeField] private float gizmoLength = 1f;
        [SerializeField] private Color gizmoColor = Color.green;

        private Vector2 _currentCalculatedAimDirection = Vector2.right;
        private float _lastNonZeroHorizontalInput = 1f;

        public Vector2 CurrentDirection => _currentCalculatedAimDirection;
        public bool IsAimingDownwards { get; private set; }

        private void Awake()
        {
            if (playerStateManager == null)
            {
                // AimDirectionResolver está en LogicContainer_Weapon.
                // PlayerStateManager está en Player_Root.
                playerStateManager = GetComponentInParent<PlayerStateManager>(); // Correcto si ambos están bajo Player_Root
                if (playerStateManager == null)
                {
                    Debug.LogError("AimDirectionResolver: PlayerStateManager reference is missing and not found in parent! This component will not function correctly.", this);
                    enabled = false; // Deshabilitar si falta la dependencia crítica
                }
            }
        }

        private void Update()
        {
            // Necesitamos input para _lastNonZeroHorizontalInput y para el cálculo directo de aim.
            // PlayerStateManager ya tiene HorizontalInput y VerticalInput actualizados por PlayerInputReader.
            if (playerStateManager == null || InputManager.Instance?.Controls == null) return;

            // Leer el input de movimiento directamente o desde PlayerStateManager si PlayerInputReader lo actualiza allí
            // Para _lastNonZeroHorizontalInput, es mejor leerlo directamente del input manager
            // o que PlayerInputReader actualice una propiedad específica en PlayerStateManager.
            // Asumamos que PlayerStateManager.HorizontalInput ya está procesado a -1, 0, 1.
            float inputX = playerStateManager.HorizontalInput;
            float inputY = playerStateManager.VerticalInput;

            if (inputX != 0) // Para mantener la última dirección horizontal si el input es 0
            {
                _lastNonZeroHorizontalInput = inputX;
            }

            // Obtener estados del PlayerStateManager
            bool isGrounded = playerStateManager.IsGrounded;
            bool isCrouching = playerStateManager.IsCrouchingLogic; // Usar el estado lógico
            bool isLocked = playerStateManager.PositionLockInputActive; // Leer del StateManager
            bool isDropping = playerStateManager.IsDroppingFromPlatform;

            _currentCalculatedAimDirection = CalculateAimDirection(inputX, inputY, isGrounded, isCrouching, isLocked, isDropping);
            IsAimingDownwards = _currentCalculatedAimDirection.y < aimingDownThreshold;
        }

        // CalculateAimDirection ya toma los booleanos de estado como parámetros, por lo que su lógica interna no cambia.
        private Vector2 CalculateAimDirection(float inputX, float inputY, bool isGrounded, bool isCrouching, bool isLocked, bool isDropping)
        {
            if (isDropping)
            {
                return new Vector2(_lastNonZeroHorizontalInput, 0f).normalized;
            }
            Vector2 resolvedDirection = _currentCalculatedAimDirection;
            if (inputY > 0)
            {
                resolvedDirection = (inputX != 0) ? new Vector2(inputX, 1f).normalized : Vector2.up;
            }
            else if (inputY < 0) // Presionando Abajo
            {
                if (isLocked) // <<<< SI ESTAMOS AQUÍ
                {
                    // Si hay inputX, apunta diagonal abajo. Sino, apunta recto abajo.
                    resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                }
                else // No Bloqueado + Presionando Abajo
                {
                    if (isCrouching) // <<<< Y SI ESTAMOS AQUÍ TAMBIÉN (ESTADO ACTUAL PROBLEMÁTICO)
                    {
                        // Si hay inputX, apunta diagonal abajo. Sino (solo abajo), apunta horizontalmente adelante.
                        resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : new Vector2(_lastNonZeroHorizontalInput, 0f);
                    }
                    else if (!isGrounded)
                    {
                        resolvedDirection = (inputX != 0) ? new Vector2(inputX, -1f).normalized : Vector2.down;
                    } 
                    else if (isGrounded && inputX == 0)
                    {
                        resolvedDirection = new Vector2(_lastNonZeroHorizontalInput, 0f);
                    }
                }
            }
            else if (inputX != 0)
            {
                resolvedDirection = new Vector2(inputX, 0f);
            }
            else
            {
                if (isGrounded && !isLocked && !isCrouching)
                {
                    resolvedDirection = new Vector2(_lastNonZeroHorizontalInput, 0f);
                }
            }
            
            return resolvedDirection.sqrMagnitude > 0.001f ? resolvedDirection.normalized : new Vector2(_lastNonZeroHorizontalInput, 0f).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            // Para el Gizmo en editor, si playerStateManager no está disponible, intenta usar el transform actual
            // o el de Player_Root si es posible encontrarlo.
            Gizmos.color = gizmoColor;
            Vector3 basePos = transform.position; // Posición de este GO (AimDirectionResolver)

            if (Application.isPlaying && playerStateManager != null)
            {
                basePos = playerStateManager.transform.position;
            } else if (!Application.isPlaying && transform.parent != null && transform.parent.parent != null) {
                //basePos = transform.parent.parent.parent.position; 
            }
            
            Vector2 dir = Application.isPlaying ? _currentCalculatedAimDirection : new Vector2(_lastNonZeroHorizontalInput, 0);
            Gizmos.DrawLine(basePos, basePos + (Vector3)(dir * gizmoLength));
        }

        private void OnValidate()
        {
            // Validar la referencia a PlayerStateManager
            if (playerStateManager == null)
            {
                playerStateManager = GetComponentInParent<PlayerStateManager>();
            }
        }
    }
}
// --- END OF FILE AimDirectionResolver.cs ---

