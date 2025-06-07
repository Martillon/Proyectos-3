using Scripts.Core;
using UnityEngine;
using Scripts.Player.Core;
using Scripts.Player.Weapons;

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
            if (_stateManager == null) Debug.LogError("PVC: PlayerStateManager not found!", this);
            if (_rb == null) Debug.LogError("PVC: Rigidbody2D not found!", this);
            if (bodyAnimator == null) bodyAnimator = GetComponent<Animator>();
            if (bodyAnimator == null) Debug.LogError("PVC: Body Animator not assigned or found!", this);
            if (visualsContainer == null) visualsContainer = transform;
            if (aimableArmObject == null) Debug.LogWarning("PVC: AimableArmObject not assigned.", this);
            if (armSpriteRenderer == null) Debug.LogWarning("PVC: ArmSpriteRenderer not assigned.", this);
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
            if (_stateManager == null || bodyAnimator == null) return;

            UpdateBodyAnimations();
            FlipVisualsContainer();
            UpdateArmVisibility();
        }

        private void UpdateBodyAnimations()
        {
            bodyAnimator.SetBool(_animIsGroundedHash, _stateManager.IsGrounded);
            bodyAnimator.SetBool(_animIsMovingHash, _stateManager.IsConsideredMovingOnGround);
            bodyAnimator.SetBool(_animIsCrouchingHash, _stateManager.IsCrouching);
            bodyAnimator.SetFloat(_animVerticalSpeedHash, _rb.linearVelocity.y);
        }

        private void FlipVisualsContainer()
        {
            if (visualsContainer == null) return;

            float targetDirection = _stateManager.FacingDirection;
            visualsContainer.localScale = new Vector3(targetDirection, 1, 1);
        }

        private void UpdateArmVisibility()
        {
            if (aimableArmObject == null) return;
            
            // The arm should be hidden if the player is crouching.
            bool shouldShowArm = !_stateManager.IsCrouching;
            
            if (aimableArmObject.activeSelf != shouldShowArm)
            {
                aimableArmObject.SetActive(shouldShowArm);
            }
        }

        public void ChangeArmSprite(Sprite newSprite)
        {
            if (armSpriteRenderer != null)
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
            if (armSpriteRenderer == null) yield break; // Assuming arm is the primary visual

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