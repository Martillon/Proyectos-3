using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core;

namespace Scripts.Player.Movement.Abilities
{
    /// <summary>
    /// Manages the player's crouching ability.
    /// Reads input intention from PlayerStateManager, checks ground status,
    /// handles collider switching, and updates PlayerStateManager with crouch status.
    /// Also responsible for adjusting the fire point position when crouching.
    /// </summary>
    public class PlayerCrouchHandler : MonoBehaviour
    {
        [Header("Collider GameObjects")]
        [SerializeField] private GameObject standingColliderGO;
        [SerializeField] private GameObject crouchingColliderGO;

        [Header("FirePoint Adjustment")]
        [SerializeField] private Transform firePointTransform;
        [SerializeField] private Vector3 firePointStandingLocalPos = new Vector3(0.5f, 0.3f, 0f);
        [SerializeField] private Vector3 firePointCrouchingLocalPos = new Vector3(0.5f, 0.1f, 0f);

        private PlayerStateManager _playerStateManager;
        private bool _isCrouchEffectPhysicallyApplied = false; // Rastrea el estado físico aplicado

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();

            if (_playerStateManager == null) { Debug.LogError("PCH: PlayerStateManager not found!", this); enabled = false; return; }
            if (standingColliderGO == null || standingColliderGO.GetComponent<Collider2D>() == null) { Debug.LogError("PCH: Standing Collider GO o su Collider2D no asignado!", this); enabled = false; return; }
            if (crouchingColliderGO == null || crouchingColliderGO.GetComponent<Collider2D>() == null) { Debug.LogError("PCH: Crouching Collider GO o su Collider2D no asignado!", this); enabled = false; return; }
            if (firePointTransform == null) Debug.LogWarning("PCH: FirePointTransform no asignado.", this);

            // Estado inicial consistente: Empezar de pie
            ApplyPhysicalStateChange(false); // false para estar de pie
            _playerStateManager.UpdateCrouchLogicState(false);
        }

        void Update()
        {
            if (_playerStateManager == null) return;

            // 1. Determinar la intención lógica del jugador
            bool intendsToCrouchLogically = _playerStateManager.IntendsToPressDown &&
                                            _playerStateManager.IsGrounded &&
                                            !_playerStateManager.IsDroppingFromPlatform &&
                                            !_playerStateManager.PositionLockInputActive;
            
            _playerStateManager.UpdateCrouchLogicState(intendsToCrouchLogically); // Actualizar el estado lógico en SM

            // 2. Reaccionar a los cambios en la intención lógica para aplicar el estado físico
            if (intendsToCrouchLogically && !_isCrouchEffectPhysicallyApplied) // Quiere agacharse y está físicamente de pie
            {
                // Aplicar estado de agachado
                ApplyPhysicalStateChange(true);
            }
            else if (!intendsToCrouchLogically && _isCrouchEffectPhysicallyApplied) // No quiere/puede agacharse Y está físicamente agachado
            {
                if (CanStandUp())
                {
                    // Aplicar estado de pie
                    ApplyPhysicalStateChange(false);
                }
                else
                {
                    // No puede levantarse. Forzar que la "intención lógica" siga siendo agachado para evitar
                    // que el PlayerStateManager.IsCrouchingLogic se ponga a false y el visual controller
                    // cambie la animación a de pie mientras físicamente sigue agachado.
                    // El PlayerStateManager.IsCrouchVisualApplied (actualizado por ApplyPhysicalStateChange)
                    // es lo que el VisualController debería usar principalmente.
                    _playerStateManager.UpdateCrouchLogicState(true); // Reflejar que no pudo levantarse
                    // El estado _isCrouchEffectPhysicallyApplied sigue siendo true.
                    // El PlayerStateManager.IsCrouchVisualApplied también sigue siendo true.
                }
            }
        }

        /// <summary>
        /// Cambia el collider activo, actualiza el StateManager y ajusta el firepoint.
        /// </summary>
        private void ApplyPhysicalStateChange(bool activateCrouch)
        {
            if (standingColliderGO == null || crouchingColliderGO == null) return;

            // Solo aplicar cambios si el estado físico deseado es diferente al actual
            if (_isCrouchEffectPhysicallyApplied == activateCrouch)
            {
                // Ya estamos en el estado deseado, no hacer nada más para evitar llamadas redundantes.
                // Esto puede pasar si CanStandUp() devuelve false y luego el input cambia de nuevo.
                return;
            }

            Debug.Log($"[{Time.frameCount}] PCH: ApplyPhysicalStateChange - Activando estado agachado: {activateCrouch}. Actual _isCrouchEffectPhysicallyApplied: {_isCrouchEffectPhysicallyApplied}");

            standingColliderGO.SetActive(!activateCrouch);
            crouchingColliderGO.SetActive(activateCrouch);

            Collider2D newActiveCollider = activateCrouch
                ? crouchingColliderGO.GetComponent<Collider2D>()
                : standingColliderGO.GetComponent<Collider2D>();
            
            if (newActiveCollider == null) {
                Debug.LogError($"PCH: El nuevo collider activo es NULL al intentar cambiar a agachado={activateCrouch}. Verifique los GameObjects de los colliders.");
                // No actualizar _isCrouchEffectPhysicallyApplied si el cambio falló críticamente.
                // Podrías querer revertir SetActive aquí o manejar el error de otra forma.
                return;
            }

            _playerStateManager.UpdateActiveCollider(newActiveCollider);
            _playerStateManager.UpdateCrouchVisualState(activateCrouch); 

            if (firePointTransform != null)
            {
                firePointTransform.localPosition = activateCrouch 
                    ? firePointCrouchingLocalPos 
                    : firePointStandingLocalPos;
            }

            _isCrouchEffectPhysicallyApplied = activateCrouch; 
            Debug.Log($"[{Time.frameCount}] PCH: Estado físico aplicado. Nuevo _isCrouchEffectPhysicallyApplied: {_isCrouchEffectPhysicallyApplied}. VisualState en SM: {activateCrouch}");
        }

        private bool CanStandUp()
        {
            // Esta función es CRÍTICA. Su lógica de OverlapBox/Capsule debe ser robusta.
            // Asegúrate de que la desactivación/reactivación temporal de colliders funcione
            // y que las layers de obstrucción sean correctas.
            // (Usaré tu implementación completa de CanStandUp que ya me pasaste antes, asumiendo que es correcta)

            if (standingColliderGO == null) return false; // No puede levantarse si no hay collider de pie

            bool originalCrouchActive = crouchingColliderGO.activeSelf;
            bool originalStandActive = standingColliderGO.activeSelf;

            crouchingColliderGO.SetActive(false); // Desactivar el de agachado para no interferir con el check
            standingColliderGO.SetActive(true);   // Activar el de pie para que el check use sus dimensiones correctas

            Collider2D standColComponent = standingColliderGO.GetComponent<Collider2D>();
            if (standColComponent == null) {
                crouchingColliderGO.SetActive(originalCrouchActive); // Restaurar
                standingColliderGO.SetActive(originalStandActive);   // Restaurar
                return false; // No puede chequear
            }

            Vector2 checkCenter;
            Vector2 checkSize;
            float checkAngle = standingColliderGO.transform.eulerAngles.z;
            bool isCapsule = false;
            CapsuleDirection2D capsuleDir = CapsuleDirection2D.Vertical;

            if (standColComponent is CapsuleCollider2D standCapsule)
            {
                isCapsule = true;
                // Para OverlapCapsule, el 'size' es el tamaño completo, no extents.
                // El offset ya está en el transform del GO si lo tiene.
                checkCenter = standingColliderGO.transform.TransformPoint(standCapsule.offset);
                checkSize = new Vector2(standCapsule.size.x * standingColliderGO.transform.lossyScale.x, 
                                        standCapsule.size.y * standingColliderGO.transform.lossyScale.y);
                capsuleDir = standCapsule.direction;

            }
            else if (standColComponent is BoxCollider2D standBox)
            {
                isCapsule = false;
                checkCenter = standingColliderGO.transform.TransformPoint(standBox.offset);
                checkSize = new Vector2(standBox.size.x * standingColliderGO.transform.lossyScale.x, 
                                        standBox.size.y * standingColliderGO.transform.lossyScale.y);
            }
            else
            {
                 crouchingColliderGO.SetActive(originalCrouchActive); standingColliderGO.SetActive(originalStandActive);
                 return true; // Tipo no soportado, asumir que puede
            }
            
            // Ligero ajuste para el check
            checkCenter.y += 0.01f;
            // checkSize.y *= 0.98f; // Puede ser demasiado agresivo si el techo está justo

            LayerMask obstructionLayers = LayerMask.GetMask(GameConstants.GroundLayerName, GameConstants.WallLayerName);
            Collider2D[] obstructions;

            if (isCapsule)
            {
                obstructions = Physics2D.OverlapCapsuleAll(checkCenter, checkSize, capsuleDir, checkAngle, obstructionLayers);
            }
            else
            {
                obstructions = Physics2D.OverlapBoxAll(checkCenter, checkSize, checkAngle, obstructionLayers);
            }

            bool canReallyStand = true;
            if (obstructions.Length > 0)
            {
                foreach (Collider2D obstruction in obstructions)
                {
                    if (obstruction.gameObject == standingColliderGO || obstruction.gameObject == crouchingColliderGO || obstruction.transform.IsChildOf(transform.root))
                    {
                        continue; // Ignorar los propios colliders del jugador
                    }
                    // Aquí tu lógica para ignorar el suelo bajo los pies si es necesario,
                    // pero el offset de checkCenter.y += 0.01f debería ayudar.
                    Debug.LogWarning($"PCH: No se puede levantar, OBSTRUIDO por '{obstruction.gameObject.name}'");
                    canReallyStand = false;
                    break;
                }
            }

            crouchingColliderGO.SetActive(originalCrouchActive);
            standingColliderGO.SetActive(originalStandActive);

            if (canReallyStand) Debug.Log($"[{Time.frameCount}] PCH: CanStandUp - Es seguro levantarse.");
            return canReallyStand;
        }

    #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (firePointTransform == null) return;

            // Para dibujar los Gizmos, necesitamos saber el padre del firePointTransform.
            // Usualmente es el ArmPivot. Si firePointTransform es hijo directo del Player_Root,
            // entonces el transform de este componente (PlayerCrouchHandler en PlayerMovementLogic)
            // no es el padre directo del firePointTransform.
            // Necesitamos el transform del objeto al cual las localPositions son relativas.

            Transform firePointParent = firePointTransform.parent;
            if (firePointParent == null)
            {
                // Si no tiene padre, las localPos son posiciones mundiales (poco probable para esta configuración)
                // En este caso, no podemos dibujar el gizmo relativo correctamente sin más información.
                // Gizmos.color = Color.red;
                // Gizmos.DrawSphere(firePointStandingLocalPos, 0.07f);
                // Gizmos.color = Color.blue;
                // Gizmos.DrawSphere(firePointCrouchingLocalPos, 0.07f);
                return;
            }

            // Dibujar Gizmo para FirePoint Estando de Pie
            Gizmos.color = Color.cyan;
            Vector3 worldStandingFirePos = firePointParent.TransformPoint(firePointStandingLocalPos);
            Gizmos.DrawSphere(worldStandingFirePos, 0.05f); // Un pequeño círculo
            Gizmos.DrawLine(worldStandingFirePos, worldStandingFirePos + (firePointParent.right * 0.3f)); // Línea en la dirección X local del padre

            // Dibujar Gizmo para FirePoint Agachado
            Gizmos.color = Color.magenta;
            Vector3 worldCrouchingFirePos = firePointParent.TransformPoint(firePointCrouchingLocalPos);
            Gizmos.DrawSphere(worldCrouchingFirePos, 0.05f);
            Gizmos.DrawLine(worldCrouchingFirePos, worldCrouchingFirePos + (firePointParent.right * 0.3f));

            
            if (Application.isPlaying && firePointTransform.gameObject.activeInHierarchy)
            {
                Gizmos.color = Color.yellow;
                 Gizmos.DrawWireSphere(firePointTransform.position, 0.06f);
            }
        }
    #endif
    }
}
// --- END OF FILE PlayerCrouchHandler.cs ---