using System;
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
        [SerializeField] private GameObject standingColliderGo;
        [Tooltip("The GameObject containing the player's crouching collider.")]
        [SerializeField] private GameObject crouchingColliderGo;

        [Header("Arm Pivot Adjustment")]
        [Tooltip("The transform of the aimable arm pivot, which will be repositioned.")]
        [SerializeField] private Transform aimableArmPivot;
        [Tooltip("The local position of the arm pivot when standing.")]
        [SerializeField] private Vector3 armPivotStandingLocalPos = new Vector3(0.2f, 0.3f, 0f);
        [Tooltip("The local position of the arm pivot when crouching.")]
        [SerializeField] private Vector3 armPivotCrouchingLocalPos = new Vector3(0.2f, 0.1f, 0f);
        
        [Header("Obstruction Check")]
        [Tooltip("An empty GameObject positioned at the top of the standing collider, used to check for overhead obstacles.")]
        [SerializeField] private Transform ceilingCheckPoint;
        [Tooltip("The radius of the circle check for detecting the ceiling.")]
        [SerializeField] private float ceilingCheckRadius = 0.2f;

        private PlayerStateManager _stateManager;
        private Collider2D _standingCollider;
        private Collider2D _crouchingCollider;
        private bool _isPhysicallyCrouched;
        private LayerMask _obstacleLayers;

        private void Awake()
        {
            _stateManager = GetComponentInParent<PlayerStateManager>();
            // --- Validations ---
            if (!_stateManager) { Debug.LogError("PCH: PlayerStateManager not found!", this); enabled = false; return; }
            if (!standingColliderGo) { Debug.LogError("PCH: Standing Collider GameObject not assigned!", this); enabled = false; return; }
            if (!crouchingColliderGo) { Debug.LogError("PCH: Crouching Collider GameObject not assigned!", this); enabled = false; return; }
            if (!aimableArmPivot) Debug.LogWarning("PCH: AimableArmPivot not assigned. Arm position will not be adjusted.", this);
            if (!ceilingCheckPoint) { Debug.LogError("PCH: Ceiling Check Point transform not assigned!", this); enabled = false; return; }
            
            _standingCollider = standingColliderGo.GetComponent<Collider2D>();
            _crouchingCollider = crouchingColliderGo.GetComponent<Collider2D>();
            
            if (!_standingCollider) { Debug.LogError("PCH: No Collider2D found on Standing Collider GameObject!", this); enabled = false; return; }
            if (!_crouchingCollider) { Debug.LogError("PCH: No Collider2D found on Crouching Collider GameObject!", this); enabled = false; return; }

            _obstacleLayers = LayerMask.GetMask(GameConstants.GroundLayerName, GameConstants.WallLayerName, GameConstants.PlatformLayerName);
            
            // --- Initial State ---
            ApplyCrouchState(false);
        }

        private void Update()
        {
            if (!_stateManager) return;
            
            var canStandUp = CheckIfCanStandUp();
            var intendsToCrouch = _stateManager.IntendsToPressDown && _stateManager.IsGrounded && !_stateManager.IsDroppingFromPlatform;
            
            // The logical state of crouching depends on player intent OR being forced by an obstacle.
            var shouldBeCrouching = intendsToCrouch || !canStandUp;
            _stateManager.SetCrouchState(shouldBeCrouching, canStandUp);

            /* Debug.Log("Crouch State Update: " +
                      $"Intends to Crouch: {intendsToCrouch}, " +
                      $"Can Stand Up: {canStandUp}, " +
                      $"Should Be Crouching: {shouldBeCrouching}"); */
            
            switch (shouldBeCrouching)
            {
                // Apply the physical state change if it doesn't match the logical state.
                case true when !_isPhysicallyCrouched:
                    ApplyCrouchState(true);
                    break;
                case false when _isPhysicallyCrouched:
                    ApplyCrouchState(false);
                    break;
            }
        }

        private void ApplyCrouchState(bool shouldCrouch)
        {
            if (_isPhysicallyCrouched == shouldCrouch) return;

            standingColliderGo.SetActive(!shouldCrouch);
            crouchingColliderGo.SetActive(shouldCrouch);

            _stateManager.SetActiveCollider(shouldCrouch ? _crouchingCollider : _standingCollider);
            AdjustArmPivotPosition(shouldCrouch);
            
            _isPhysicallyCrouched = shouldCrouch;
        }

        private void AdjustArmPivotPosition(bool isCrouching)
        {
            if (!aimableArmPivot) return;

            Vector3 baseTargetPos = isCrouching ? armPivotCrouchingLocalPos : armPivotStandingLocalPos;
    
            // The pivot's X position is manually set based on facing direction
            // because it's a direct child of Player_Root.
            baseTargetPos.x = Mathf.Abs(baseTargetPos.x) * _stateManager.FacingDirection;

            aimableArmPivot.localPosition = baseTargetPos;
        }

        private bool CheckIfCanStandUp()
        {
            // We now perform a small circle cast at the ceiling check point.
            // This checks for any colliders in the "head area" without ever detecting the ground.
            var ceilingHit = Physics2D.OverlapCircle(ceilingCheckPoint.position, ceilingCheckRadius, _obstacleLayers);

            // If ceilingHit is null, there are no obstacles overhead, so we can stand.
            return !ceilingHit;
        }
        
#if UNITY_EDITOR
        // Optional: Add a gizmo to see your ceiling check area
        private void OnDrawGizmosSelected()
        {
            if (!ceilingCheckPoint) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ceilingCheckPoint.position, ceilingCheckRadius);
        }
#endif
    }
}