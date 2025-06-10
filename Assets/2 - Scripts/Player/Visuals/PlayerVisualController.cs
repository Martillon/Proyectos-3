using Scripts.Core;
using UnityEngine;
using Scripts.Player.Core;

namespace Scripts.Player.Visuals
{
    /// <summary>
    /// Manages all visual aspects of the player, including body animations,
    /// sprite flipping, and arm visibility/rotation.
    /// Resides on the 'Visuals' container GameObject.
    /// </summary>
    public class PlayerVisualController : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("Animator for the player's body.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("The root transform of the entire visual representation, used for flipping.")]
        [SerializeField] private Transform visualsContainer;
        
        [Header("Arm Control")]
        [Tooltip("The GameObject representing the aimable arm (pivot, sprite, firepoint).")]
        [SerializeField] private GameObject aimableArmObject;
        [Tooltip("The SpriteRenderer of the arm/weapon sprite.")]
        [SerializeField] private SpriteRenderer armSpriteRenderer;
        [Tooltip("The transform of the arm's visual component, used for scale-based flipping.")]
        [SerializeField] private Transform aimableArmVisualTransform;
        
        private PlayerStateManager _stateManager;
        private Rigidbody2D _rb;

        // Animator Parameter Hashes for performance
        private readonly int _animIsMovingHash = Animator.StringToHash(GameConstants.AnimIsMoving);
        private readonly int _animIsGroundedHash = Animator.StringToHash(GameConstants.AnimIsGrounded);
        private readonly int _animIsCrouchingHash = Animator.StringToHash(GameConstants.AnimIsCrouching);
        private readonly int _animVerticalSpeedHash = Animator.StringToHash(GameConstants.AnimVerticalSpeed);

        private void Awake()
        {
            // This script is on a child object, so it gets core components from its parent.
            _stateManager = GetComponentInParent<PlayerStateManager>();
            _rb = GetComponentInParent<Rigidbody2D>();

            // Validations
            if (!_stateManager) Debug.LogError("PVC: PlayerStateManager not found!", this);
            if (!_rb) Debug.LogError("PVC: Rigidbody2D not found!", this);
            if (!bodyAnimator) bodyAnimator = GetComponent<Animator>();
            if (!bodyAnimator) Debug.LogError("PVC: Body Animator not assigned or found!", this);
            if (!visualsContainer) visualsContainer = transform;
            if (!aimableArmObject) Debug.LogWarning("PVC: AimableArmObject not assigned.", this);
            if (!armSpriteRenderer) Debug.LogWarning("PVC: ArmSpriteRenderer not assigned.", this);
        }

        private void OnEnable()
        {
            PlayerEvents.OnLevelCompleted += HandleVictory;
        }

        private void OnDisable()
        {
            PlayerEvents.OnLevelCompleted -= HandleVictory;
        }

        private void Update()
        {
            if (!_stateManager || !bodyAnimator) return;

            UpdateBodyAnimations();
            UpdateVisualOrientation();
            UpdateArmVisibility();
        }

        private void UpdateBodyAnimations()
        {
            bodyAnimator.SetBool(_animIsGroundedHash, _stateManager.IsGrounded);
            bodyAnimator.SetBool(_animIsMovingHash, _stateManager.IsConsideredMovingOnGround);
            bodyAnimator.SetBool(_animIsCrouchingHash, _stateManager.IsCrouching);
            bodyAnimator.SetFloat(_animVerticalSpeedHash, _rb.linearVelocity.y);
        }

        private void UpdateVisualOrientation()
        {
            if (!_stateManager) return;

            float facingDirection = _stateManager.FacingDirection;

            // 1. Flip the body's container (as before)
            if (visualsContainer != null)
            {
                visualsContainer.localScale = new Vector3(facingDirection, 1, 1);
            }

            // 2. Flip the arm visual using your solution
            if (aimableArmVisualTransform)
            {
                if (facingDirection > 0) // Facing Right
                {
                    // Standard orientation
                    aimableArmVisualTransform.localScale = Vector3.one;
                }
                else // Facing Left
                {
                    // Your solution: flip both X and Y to correctly mirror the rotated sprite
                    aimableArmVisualTransform.localScale = new Vector3(1, -1, 1);
                }
            }
        }

        private void UpdateArmVisibility()
        {
            if (!aimableArmObject) return;
            
            // The arm should be hidden if the player is crouching.
            bool shouldShowArm = !_stateManager.IsCrouching;
            
            if (aimableArmObject.activeSelf != shouldShowArm)
            {
                aimableArmObject.SetActive(shouldShowArm);
            }
        }

        public void ChangeArmSprite(Sprite newSprite)
        {
            if (armSpriteRenderer)
            {
                armSpriteRenderer.sprite = newSprite;
            }
        }
        
        public void HideArmObject() => aimableArmObject?.SetActive(false);
        public void ShowArmObject() => aimableArmObject?.SetActive(true);
        public Animator GetBodyAnimator() => bodyAnimator;

        private void HandleVictory(string levelId)
        {
            HideArmObject();
            bodyAnimator?.SetTrigger(GameConstants.AnimVictoryTrigger);
        }
        
        // This is called by the PlayerHealthSystem
        public System.Collections.IEnumerator FlashSpriteCoroutine(float duration, float interval)
        {
            if (!armSpriteRenderer) yield break; // Assuming arm is the primary visual

            SpriteRenderer[] renderersToFlash = GetComponentsInChildren<SpriteRenderer>();
            
            float endTime = Time.time + duration;
            while(Time.time < endTime)
            {
                foreach(var rend in renderersToFlash)
                {
                    rend.enabled = !rend.enabled;
                }
                yield return new WaitForSeconds(interval);
            }
            
            // Ensure all renderers are visible at the end.
            foreach(var rend in renderersToFlash)
            {
                rend.enabled = true;
            }
        }
    }
}