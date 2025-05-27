// En Scripts/Player/Movement/Abilities/PlayerCrouchHandler.cs
using UnityEngine;
using Scripts.Core; // Para GameConstants
using Scripts.Player.Core;

namespace Scripts.Player.Movement.Abilities
{
    public class PlayerCrouchHandler : MonoBehaviour
    {
        [Header("Collider GameObjects")]
        [SerializeField] private GameObject standingColliderGO;
        [SerializeField] private GameObject crouchingColliderGO;

        [Header("Arm Pivot Adjustment")]
        [SerializeField] private Transform aimableArmPivotTransform; 
        [SerializeField] private Vector3 armPivotStandingLocalPos = new Vector3(0.2f, 0.3f, 0f);
        [SerializeField] private Vector3 armPivotCrouchingLocalPos = new Vector3(0.2f, 0.1f, 0f);

        [Header("Firepoint Visualization (Relative to Arm Pivot)")]
        [Tooltip("La posición local del Firepoint real relativa al AimableArmPivotTransform. Asume que X es 'hacia adelante'.")]
        [SerializeField] private Vector3 firepointOffsetFromArmPivot = new Vector3(0.5f, 0f, 0f); // Ejemplo: punta del cañón

        private PlayerStateManager _playerStateManager;
        private bool _isCrouchEffectPhysicallyApplied = false;

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();

            if (_playerStateManager == null) { Debug.LogError("PCH: PlayerStateManager not found!", this); enabled = false; return; }
            if (standingColliderGO == null || standingColliderGO.GetComponent<Collider2D>() == null) { Debug.LogError("PCH: Standing Collider GO o su Collider2D no asignado!", this); enabled = false; return; }
            if (crouchingColliderGO == null || crouchingColliderGO.GetComponent<Collider2D>() == null) { Debug.LogError("PCH: Crouching Collider GO o su Collider2D no asignado!", this); enabled = false; return; }
            if (aimableArmPivotTransform == null) Debug.LogWarning("PCH: AimableArmPivotTransform no asignado. No se ajustará la posición del brazo.", this);

            // Estado inicial: Empezar de pie
            ApplyPhysicalStateChange(false); 
            _playerStateManager.UpdateCrouchLogicState(false);
        }

        void Update()
        {
            if (_playerStateManager == null) return;

            bool intendsToCrouchLogically = _playerStateManager.IntendsToPressDown &&
                                            _playerStateManager.IsGrounded &&
                                            !_playerStateManager.IsDroppingFromPlatform &&
                                            !_playerStateManager.PositionLockInputActive;
            
            _playerStateManager.UpdateCrouchLogicState(intendsToCrouchLogically);

            if (intendsToCrouchLogically && !_isCrouchEffectPhysicallyApplied)
            {
                ApplyPhysicalStateChange(true);
            }
            else if (!intendsToCrouchLogically && _isCrouchEffectPhysicallyApplied)
            {
                if (CanStandUp())
                {
                    ApplyPhysicalStateChange(false);
                }
                else
                {
                    _playerStateManager.UpdateCrouchLogicState(true); 
                }
            }
        }

        private void ApplyPhysicalStateChange(bool activateCrouch)
        {
            if (standingColliderGO == null || crouchingColliderGO == null) return;
            if (_isCrouchEffectPhysicallyApplied == activateCrouch) return;

            standingColliderGO.SetActive(!activateCrouch);
            crouchingColliderGO.SetActive(activateCrouch);

            Collider2D newActiveCollider = activateCrouch
                ? crouchingColliderGO.GetComponent<Collider2D>()
                : standingColliderGO.GetComponent<Collider2D>();
            
            if (newActiveCollider == null) {
                Debug.LogError($"PCH: El nuevo collider activo es NULL al cambiar a agachado={activateCrouch}.");
                // Revertir SetActive si es posible o manejar el error
                standingColliderGO.SetActive(_isCrouchEffectPhysicallyApplied ? false : true); // Estado anterior
                crouchingColliderGO.SetActive(_isCrouchEffectPhysicallyApplied ? true : false); // Estado anterior
                return;
            }

            _playerStateManager.UpdateActiveCollider(newActiveCollider);
            _playerStateManager.UpdateCrouchVisualState(activateCrouch); 

            AdjustArmPivotPosition(activateCrouch); // Llamar al método que ajusta el pivote del brazo

            _isCrouchEffectPhysicallyApplied = activateCrouch; 
            //Debug.Log($"[{Time.frameCount}] PCH: Estado físico aplicado. Agachado: {activateCrouch}. VisualState en SM: {activateCrouch}");
        }

        /// <summary>
        /// Ajusta la posición local del AimableArm_pivot basado en el estado de agachado y la dirección del jugador.
        /// </summary>
        private void AdjustArmPivotPosition(bool isCrouching)
        {
            if (aimableArmPivotTransform == null || _playerStateManager == null) return;

            Vector3 baseTargetLocalPos = isCrouching ? armPivotCrouchingLocalPos : armPivotStandingLocalPos;
            float facingDirection = _playerStateManager.CurrentFacingDirection; // 1 para derecha, -1 para izquierda

            // Asumimos que los valores X en armPivotCrouchingLocalPos y armPivotStandingLocalPos
            // están definidos como si el jugador mirara a la DERECHA (es decir, son positivos o cero).
            // Multiplicamos por facingDirection para ponerlo en el lado correcto.
            Vector3 adjustedLocalPos = new Vector3(
                Mathf.Abs(baseTargetLocalPos.x) * facingDirection,
                baseTargetLocalPos.y,
                baseTargetLocalPos.z
            );

            aimableArmPivotTransform.localPosition = adjustedLocalPos;
            // Debug.Log($"[{Time.frameCount}] PCH: AimableArm_pivot localPosition set to {aimableArmPivotTransform.localPosition} (Crouching: {isCrouching}, Facing: {facingDirection})");
        }

        private bool CanStandUp()
        {
            // ... (Tu lógica completa y robusta de CanStandUp como la tenías antes,
            //      con la desactivación/activación temporal de colliders para el Overlap check) ...
            // Esta función es vital.

            if (standingColliderGO == null) return false; 
            bool originalCrouchActive = crouchingColliderGO.activeSelf;
            bool originalStandActive = standingColliderGO.activeSelf;
            crouchingColliderGO.SetActive(false); 
            standingColliderGO.SetActive(true);   
            Collider2D standColComponent = standingColliderGO.GetComponent<Collider2D>();
            if (standColComponent == null) {
                crouchingColliderGO.SetActive(originalCrouchActive); 
                standingColliderGO.SetActive(originalStandActive);   
                return false; 
            }
            Vector2 checkCenter; Vector2 checkSize; float checkAngle = standingColliderGO.transform.eulerAngles.z;
            bool isCapsule = false; CapsuleDirection2D capsuleDir = CapsuleDirection2D.Vertical;
            if (standColComponent is CapsuleCollider2D standCapsule) {
                isCapsule = true;
                checkCenter = standingColliderGO.transform.TransformPoint(standCapsule.offset);
                checkSize = new Vector2(standCapsule.size.x * standingColliderGO.transform.lossyScale.x, standCapsule.size.y * standingColliderGO.transform.lossyScale.y);
                capsuleDir = standCapsule.direction;
            } else if (standColComponent is BoxCollider2D standBox) {
                isCapsule = false;
                checkCenter = standingColliderGO.transform.TransformPoint(standBox.offset);
                checkSize = new Vector2(standBox.size.x * standingColliderGO.transform.lossyScale.x, standBox.size.y * standingColliderGO.transform.lossyScale.y);
            } else {
                 crouchingColliderGO.SetActive(originalCrouchActive); standingColliderGO.SetActive(originalStandActive);
                 return true; 
            }
            checkCenter.y += 0.01f;
            LayerMask obstructionLayers = LayerMask.GetMask(GameConstants.GroundLayerName, GameConstants.WallLayerName); // Asegúrate que estos nombres sean correctos
            Collider2D[] obstructions = isCapsule ? 
                Physics2D.OverlapCapsuleAll(checkCenter, checkSize, capsuleDir, checkAngle, obstructionLayers) : 
                Physics2D.OverlapBoxAll(checkCenter, checkSize, checkAngle, obstructionLayers);
            bool canReallyStand = true;
            if (obstructions.Length > 0) {
                foreach (Collider2D obstruction in obstructions) {
                    if (obstruction.transform.IsChildOf(transform.root) || obstruction.gameObject == gameObject) continue; // Ignorar partes del propio jugador
                    Debug.LogWarning($"PCH: No se puede levantar, OBSTRUIDO por '{obstruction.gameObject.name}'");
                    canReallyStand = false; break;
                }
            }
            crouchingColliderGO.SetActive(originalCrouchActive);
            standingColliderGO.SetActive(originalStandActive);
            return canReallyStand;
        }

    #if UNITY_EDITOR
         private void OnDrawGizmosSelected()
        {
            if (_playerStateManager == null && Application.isPlaying)
            {
                // Intentar obtenerlo si estamos en play mode y es null (para Gizmos dinámicos)
                // No es ideal hacerlo aquí, Awake es mejor, pero como fallback para el Gizmo.
                _playerStateManager = GetComponentInParent<PlayerStateManager>();
            }
            
            // Determinar la dirección de "enfrentamiento" para los gizmos
            float facingDirGizmo = 1f; 
            if (Application.isPlaying && _playerStateManager != null)
            {
                facingDirGizmo = _playerStateManager.CurrentFacingDirection;
            }
            // Si _playerStateManager sigue siendo null, facingDirGizmo será 1f (derecha por defecto)

            // --- Gizmos para la posición del AimableArmPivotTransform ---
            Transform pivotParentForGizmo = null;
            if (aimableArmPivotTransform != null && aimableArmPivotTransform.parent != null)
            {
                pivotParentForGizmo = aimableArmPivotTransform.parent;
            }
            else if (aimableArmPivotTransform == null && _playerStateManager != null) // Si no hay pivote asignado, usar el root del jugador como padre
            {
                 pivotParentForGizmo = _playerStateManager.transform; // O transform.root si PCH está en un hijo
            }


            if (pivotParentForGizmo != null)
            {
                // Posición del Pivote del Brazo - De Pie
                Gizmos.color = Color.cyan; // Color para el pivote del brazo de pie
                Vector3 standPivotLocal = armPivotStandingLocalPos;
                standPivotLocal.x = Mathf.Abs(standPivotLocal.x) * facingDirGizmo;
                Vector3 worldStandingPivotPos = pivotParentForGizmo.TransformPoint(standPivotLocal);
                Gizmos.DrawSphere(worldStandingPivotPos, 0.05f);
                Gizmos.DrawLine(worldStandingPivotPos, worldStandingPivotPos + (pivotParentForGizmo.TransformDirection(Vector3.right * facingDirGizmo) * 0.2f)); // Pequeña línea de dirección

                // Posición del Pivote del Brazo - Agachado
                Gizmos.color = Color.magenta; // Color para el pivote del brazo agachado
                Vector3 crouchPivotLocal = armPivotCrouchingLocalPos;
                crouchPivotLocal.x = Mathf.Abs(crouchPivotLocal.x) * facingDirGizmo;
                Vector3 worldCrouchingPivotPos = pivotParentForGizmo.TransformPoint(crouchPivotLocal);
                Gizmos.DrawSphere(worldCrouchingPivotPos, 0.05f);
                Gizmos.DrawLine(worldCrouchingPivotPos, worldCrouchingPivotPos + (pivotParentForGizmo.TransformDirection(Vector3.right * facingDirGizmo) * 0.2f));


                // --- Gizmos para la posición FINAL del Firepoint (relativa al pivote del brazo) ---
                // Estos simulan dónde estaría el firepoint si el brazo estuviera en esa pose.
                // Asumen que aimableArmPivotTransform.rotation es la rotación de apuntado.
                // Si no está en play, la rotación de apuntado es desconocida, así que solo mostramos el offset.

                Quaternion armRotationGizmo = Quaternion.identity;
                if(Application.isPlaying && aimableArmPivotTransform != null)
                {
                    armRotationGizmo = aimableArmPivotTransform.rotation;
                } else if (pivotParentForGizmo != null) { // Editor mode, simular rotación horizontal
                    armRotationGizmo = pivotParentForGizmo.rotation * Quaternion.LookRotation(Vector3.forward, Vector3.up); 
                                        // Esto es una aproximación, LookRotation necesita un forward y un up.
                                        // Para 2D, si el sprite apunta a la derecha por defecto:
                    if (facingDirGizmo < 0) armRotationGizmo = pivotParentForGizmo.rotation * Quaternion.Euler(0,180,0);
                    else armRotationGizmo = pivotParentForGizmo.rotation;
                }
                
                // Firepoint Final - De Pie
                Gizmos.color = new Color(0f, 0.8f, 0.8f, 0.8f); // Cyan más oscuro/transparente
                // El firepointOffsetFromArmPivot.x también debe considerar el facing si el brazo se flipea visualmente
                // pero aquí el pivote del brazo ya está en el lado correcto. El offset es siempre "hacia adelante" del pivote.
                Vector3 firepointWorldStanding = worldStandingPivotPos + (armRotationGizmo * firepointOffsetFromArmPivot);
                Gizmos.DrawSphere(firepointWorldStanding, 0.04f);
                Gizmos.DrawLine(worldStandingPivotPos, firepointWorldStanding); // Línea del pivote al firepoint

                // Firepoint Final - Agachado
                Gizmos.color = new Color(0.8f, 0f, 0.8f, 0.8f); // Magenta más oscuro/transparente
                Vector3 firepointWorldCrouching = worldCrouchingPivotPos + (armRotationGizmo * firepointOffsetFromArmPivot);
                Gizmos.DrawSphere(firepointWorldCrouching, 0.04f);
                Gizmos.DrawLine(worldCrouchingPivotPos, firepointWorldCrouching);


                // Mostrar la posición actual del pivote del brazo REAL en amarillo si está en play
                if (Application.isPlaying && aimableArmPivotTransform != null && aimableArmPivotTransform.gameObject.activeInHierarchy)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(aimableArmPivotTransform.position, 0.06f);

                    // Y la posición actual del Firepoint REAL
                    // Asumiendo que tienes una referencia al Firepoint real o puedes encontrarlo
                    Transform actualFirepoint = null;
                    if(aimableArmPivotTransform.Find("Firepoint") != null) // Si se llama "Firepoint" y es hijo directo
                        actualFirepoint = aimableArmPivotTransform.Find("Firepoint"); 
                    // O si tienes un campo [SerializeField] private Transform actualFirepointReference;

                    if (actualFirepoint != null)
                    {
                         Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Naranja para el Firepoint real
                         Gizmos.DrawSphere(actualFirepoint.position, 0.045f);
                    }
                }
            }
        }
    #endif
    }
}