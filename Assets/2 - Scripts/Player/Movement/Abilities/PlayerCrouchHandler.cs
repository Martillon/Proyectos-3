using UnityEngine;
using Scripts.Core;
using Scripts.Player.Core;

namespace Scripts.Player.Movement.Abilities
{
    /// <summary>
    /// Manages the player's crouching ability.
    /// Handles switching between standing and crouching colliders, checking for overhead
    /// obstructions, and adjusting the arm pivot position.
    /// </summary>
    public class PlayerCrouchHandler : MonoBehaviour
    {
        [Header("Collider Setup")]
        [Tooltip("The GameObject containing the player's standing collider.")]
        [SerializeField] private GameObject standingColliderGO;
        [Tooltip("The GameObject containing the player's crouching collider.")]
        [SerializeField] private GameObject crouchingColliderGO;

        [Header("Arm Pivot Adjustment")]
        [Tooltip("The transform of the aimable arm pivot, which will be repositioned.")]
        [SerializeField] private Transform aimableArmPivot;
        [Tooltip("The local position of the arm pivot when standing.")]
        [SerializeField] private Vector3 armPivotStandingLocalPos = new Vector3(0.2f, 0.3f, 0f);
        [Tooltip("The local position of the arm pivot when crouching.")]
        [SerializeField] private Vector3 armPivotCrouchingLocalPos = new Vector3(0.2f, 0.1f, 0f);

        private PlayerStateManager _stateManager;
        private Collider2D _standingCollider;
        private Collider2D _crouchingCollider;
        private bool _isPhysicallyCrouched = false;

        private void Awake()
        {
            _stateManager = GetComponentInParent<PlayerStateManager>();
            // --- Validations ---
            if (_stateManager == null) { Debug.LogError("PCH: PlayerStateManager not found!", this); enabled = false; return; }
            if (standingColliderGO == null) { Debug.LogError("PCH: Standing Collider GameObject not assigned!", this); enabled = false; return; }
            if (crouchingColliderGO == null) { Debug.LogError("PCH: Crouching Collider GameObject not assigned!", this); enabled = false; return; }
            if (aimableArmPivot == null) Debug.LogWarning("PCH: AimableArmPivot not assigned. Arm position will not be adjusted.", this);
            
            _standingCollider = standingColliderGO.GetComponent<Collider2D>();
            _crouchingCollider = crouchingColliderGO.GetComponent<Collider2D>();
            
            if (_standingCollider == null) { Debug.LogError("PCH: No Collider2D found on Standing Collider GameObject!", this); enabled = false; return; }
            if (_crouchingCollider == null) { Debug.LogError("PCH: No Collider2D found on Crouching Collider GameObject!", this); enabled = false; return; }

            // --- Initial State ---
            ApplyCrouchState(false);
        }

        private void Update()
        {
            if (_stateManager == null) return;
            
            bool canStandUp = CheckIfCanStandUp();
            bool intendsToCrouch = _stateManager.IntendsToPressDown && _stateManager.IsGrounded && !_stateManager.IsDroppingFromPlatform;
            
            // The logical state of crouching depends on player intent OR being forced by an obstacle.
            bool shouldBeCrouching = intendsToCrouch || !canStandUp;
            _stateManager.SetCrouchState(shouldBeCrouching, canStandUp);
            
            // Apply the physical state change if it doesn't match the logical state.
            if (shouldBeCrouching && !_isPhysicallyCrouched)
            {
                ApplyCrouchState(true);
            }
            else if (!shouldBeCrouching && _isPhysicallyCrouched)
            {
                ApplyCrouchState(false);
            }
        }

        private void ApplyCrouchState(bool shouldCrouch)
        {
            if (_isPhysicallyCrouched == shouldCrouch) return;

            standingColliderGO.SetActive(!shouldCrouch);
            crouchingColliderGO.SetActive(shouldCrouch);

            _stateManager.SetActiveCollider(shouldCrouch ? _crouchingCollider : _standingCollider);
            AdjustArmPivotPosition(shouldCrouch);
            
            _isPhysicallyCrouched = shouldCrouch;
        }

        private void AdjustArmPivotPosition(bool isCrouching)
        {
            if (aimableArmPivot == null) return;

            Vector3 baseTargetPos = isCrouching ? armPivotCrouchingLocalPos : armPivotStandingLocalPos;
            
            // The arm pivot's X position should flip based on the player's facing direction.
            // We use Abs to ensure the configured value is treated as a magnitude.
            baseTargetPos.x = Mathf.Abs(baseTargetPos.x) * _stateManager.FacingDirection;

            aimableArmPivot.localPosition = baseTargetPos;
        }

        private bool CheckIfCanStandUp()
        {
            if (_standingCollider == null) return false;

            // To check for space, we need to temporarily disable the current crouch collider
            // and use the OverlapCollider of the standing collider to see if it hits anything.
            _crouchingCollider.enabled = false;
            
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = false,
                layerMask = LayerMask.GetMask(GameConstants.GroundLayerName, GameConstants.WallLayerName), // Check against solid layers
                useLayerMask = true
            };
            
            Collider2D[] results = new Collider2D[1]; // We only need to know if there's at least one overlap.
            int overlapCount = _standingCollider.Overlap(filter, results);

            _crouchingCollider.enabled = true; // IMPORTANT: Re-enable the crouch collider immediately.

            return overlapCount == 0;
        }
    }
}