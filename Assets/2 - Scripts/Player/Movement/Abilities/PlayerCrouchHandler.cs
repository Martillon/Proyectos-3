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
        [Header("Collider References")]
        [Tooltip("GameObject containing the collider used when the player is standing.")]
        [SerializeField] private GameObject standingColliderObject;
        [Tooltip("GameObject containing the collider used when the player is crouching.")]
        [SerializeField] private GameObject crouchingColliderObject;

        [Header("FirePoint Adjustment")]
        [Tooltip("Reference to the player's fire point Transform that will be moved.")]
        [SerializeField] private Transform firePointTransform; // Renombrado para claridad
        [Tooltip("Local position of the fire point when standing, relative to its parent (likely ArmPivot or Player_Root).")]
        [SerializeField] private Vector3 firePointStandingLocalPos = new Vector3(0.5f, 0.3f, 0f);
        [Tooltip("Local position of the fire point when crouching, relative to its parent.")]
        [SerializeField] private Vector3 firePointCrouchingLocalPos = new Vector3(0.5f, 0.1f, 0f);

        private PlayerStateManager _playerStateManager;
        private bool _isCrouchEffectCurrentlyApplied = false; // Renombrado desde _isCrouchVisualApplied

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            if (_playerStateManager == null) { /* ... error ... */ enabled = false; return; }
            if (standingColliderObject == null || standingColliderObject.GetComponent<Collider2D>() == null) Debug.LogError("...", this);
            if (crouchingColliderObject == null || crouchingColliderObject.GetComponent<Collider2D>() == null) Debug.LogError("...", this);
            if (firePointTransform == null) Debug.LogWarning("...", this);

            if (standingColliderObject != null && crouchingColliderObject != null)
            {
                _isCrouchEffectCurrentlyApplied = crouchingColliderObject.activeSelf; 
                _playerStateManager.UpdateCrouchVisualState(_isCrouchEffectCurrentlyApplied);
                if(firePointTransform != null) firePointTransform.localPosition = firePointStandingLocalPos;
            }
        }

        void Update()
        {
            if (_playerStateManager == null) return;
            bool shouldBeCrouchingLogically = _playerStateManager.IntendsToPressDown &&
                                              _playerStateManager.IsGrounded &&
                                              !_playerStateManager.IsDroppingFromPlatform &&
                                              !_playerStateManager.PositionLockInputActive; 
            _playerStateManager.UpdateCrouchLogicState(shouldBeCrouchingLogically);            
            
            ApplyCrouchStateChanges();
        }

        private void ApplyCrouchStateChanges()
        {
            // Debug.Log($"ApplyCrouchVisuals :: Entrando. LógicaAgachado: {_playerStateManager.IsCrouchingLogic}, EfectoAplicado: {_isCrouchEffectCurrentlyApplied}");
            if (_playerStateManager.IsCrouchingLogic && !_isCrouchEffectCurrentlyApplied)
            {
                _isCrouchEffectCurrentlyApplied = true;
                SwitchPlayerColliderAndFirePoint(true); 
                _playerStateManager.UpdateCrouchVisualState(true);
                // Debug.Log("PlayerCrouchHandler: Switched TO Crouch State");
            }
            else if (!_playerStateManager.IsCrouchingLogic && _isCrouchEffectCurrentlyApplied)
            {
                if (CanStandUp())
                {
                    _isCrouchEffectCurrentlyApplied = false;
                    SwitchPlayerColliderAndFirePoint(false); 
                    _playerStateManager.UpdateCrouchVisualState(false);
                    // Debug.Log("PlayerCrouchHandler: Switched TO Standing State");
                }
                // else Debug.LogWarning("PlayerCrouchHandler: Cannot stand up, effect remains applied.");
            }
        }

        private void SwitchPlayerColliderAndFirePoint(bool activateCrouchState)
        {
            Collider2D newActiveCollider = activateCrouchState
                ? crouchingColliderObject?.GetComponent<Collider2D>()
                : standingColliderObject?.GetComponent<Collider2D>();

            _playerStateManager.UpdateActiveCollider(newActiveCollider); // ESTO ES CRUCIAL
            Debug.Log(
                $"CROUCH_HANDLER SwitchCollider: Active Collider set in StateManager to: {newActiveCollider?.gameObject.name}");
        }

        private bool CanStandUp()
        {
            if (!_isCrouchEffectCurrentlyApplied) return true;
            if (standingColliderObject == null) return false;

            Collider2D standColComponent = standingColliderObject.GetComponent<Collider2D>();
            if (standColComponent == null) return false;

            Vector2 standUpCenterWorld;
            Vector2 standUpSize;
            float standUpAngle = standingColliderObject.transform.eulerAngles.z;
            bool isStandCapsule = false;
            CapsuleDirection2D standUpCapsuleDir = CapsuleDirection2D.Vertical;

            if (standColComponent is CapsuleCollider2D standCapsule)
            {
                isStandCapsule = true;
                standUpSize = standCapsule.size;
                standUpCenterWorld = standingColliderObject.transform.TransformPoint(standCapsule.offset);
                standUpCapsuleDir = standCapsule.direction;
            }
            else if (standColComponent is BoxCollider2D standBox)
            {
                isStandCapsule = false;
                standUpSize = standBox.size;
                standUpCenterWorld = standingColliderObject.transform.TransformPoint(standBox.offset);
            }
            else
            {
                return true;
            }

            Vector2 checkCenter = standUpCenterWorld + new Vector2(0, 0.01f);
            Vector2 checkSize = new Vector2(standUpSize.x, standUpSize.y * 0.98f);

            // Obtener máscaras de capa
            LayerMask
                groundMask =
                    LayerMask.GetMask(GameConstants.GroundLayerName); // Asume que GameConstants.GroundLayerName es "Ground"
            LayerMask
                wallMask = LayerMask.GetMask(GameConstants.WallLayerName); // Asume que GameConstants.WallLayerName es "Walls" o tu capa de techos
            LayerMask obstructionLayers = groundMask | wallMask; // O las capas que definiste antes

            Collider2D[] obstructions;
            if (isStandCapsule)
            {
                obstructions = Physics2D.OverlapCapsuleAll(checkCenter, checkSize, standUpCapsuleDir, standUpAngle,
                    obstructionLayers);
            }
            else
            {
                obstructions = Physics2D.OverlapBoxAll(checkCenter, checkSize, standUpAngle, obstructionLayers);
            }

            if (obstructions.Length > 0)
            {
                Transform playerRoot = _playerStateManager.transform;
                foreach (Collider2D obstruction in obstructions)
                {
                    int obstructionLayerBit = 1 << obstruction.gameObject.layer;
                    bool isGround = (LayerMask.GetMask(GameConstants.GroundLayerName).GetHashCode() & obstructionLayerBit) > 0;
                    bool isWallOrCeiling = (LayerMask.GetMask(GameConstants.WallLayerName).GetHashCode() & obstructionLayerBit) > 0;

                    if (isGround && !isWallOrCeiling) // Si es suelo, pero no una pared/techo
                    {
                        // Obtener la parte superior del collider de agachado (el que está activo)
                        Collider2D activeCrouchCol = crouchingColliderObject.GetComponent<Collider2D>();
                        if (activeCrouchCol != null)
                        {
                            // Centro mundial del collider de agachado
                            Vector2 crouchCenterWorld = crouchingColliderObject.transform.TransformPoint(activeCrouchCol.offset);
                            // Bounds del collider de agachado para obtener su altura actual
                            float crouchColliderCurrentHeight = 0f;
                            if(activeCrouchCol is CapsuleCollider2D cap) crouchColliderCurrentHeight = cap.size.y * crouchingColliderObject.transform.lossyScale.y; // Considerar escala del GO
                            else if(activeCrouchCol is BoxCollider2D box) crouchColliderCurrentHeight = box.size.y * crouchingColliderObject.transform.lossyScale.y;
                            
                            // Parte superior del collider de agachado
                            float crouchTopY = crouchCenterWorld.y + (crouchColliderCurrentHeight / 2f);
                            float obstructionTopY = obstruction.bounds.max.y; // Parte superior del suelo detectado

                            // Si la parte superior del "suelo" que obstruye NO está significativamente por encima
                            // de la cabeza del personaje agachado, entonces es el suelo bajo los pies, no un techo.
                            float tolerance = 0.1f; // Margen pequeño
                            if (obstructionTopY < crouchTopY + tolerance) 
                            {
                                // Debug.Log($"CanStandUp: Ignoring ground obstruction: {obstruction.name} (TopY: {obstructionTopY} vs CrouchTopY: {crouchTopY})");
                                continue; // Ignorar esta obstrucción, es solo el suelo
                            }
                        }
                    }

                    // Si no es suelo ignorable, o es una pared/techo, entonces es una obstrucción real.
                    Debug.LogWarning($"P2DM: Cannot stand up, OBSTRUCTED by '{obstruction.gameObject.name}' (Layer: {LayerMask.LayerToName(obstruction.gameObject.layer)})", this);
                    return false;
                }
            }
            // Debug.Log("PlayerCrouchHandler: Can stand up safely.");
            return true;
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