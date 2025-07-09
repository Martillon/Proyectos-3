using UnityEngine;

namespace Scripts.Enemies.Boss.Core.Visuals
{
    /// <summary>
    /// Manages all visual aspects of the boss, including its Animator and sprite flipping.
    /// It receives commands from the BossController.
    /// </summary>
    public class BossVisualController : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("The root transform of all visual elements that should be flipped horizontally.")]
        [SerializeField] private Transform visualsContainer;
        [Tooltip("The main Animator for the boss.")]
        [SerializeField] private Animator bodyAnimator;
        
        // We store hash IDs for performance instead of using strings every time.
        private readonly int _isWalkingParam = Animator.StringToHash("IsWalking");
        private readonly int _doRoarParam = Animator.StringToHash("DoRoar");
        private readonly int _doGroundSmashParam = Animator.StringToHash("DoGroundSmash");
        private readonly int _doRushParam = Animator.StringToHash("DoRush");
        private readonly int _doMeleeSwipeParam = Animator.StringToHash("DoMeleeSwipe");
        private readonly int _doStunParam = Animator.StringToHash("DoStun");
        private readonly int _endStunParam = Animator.StringToHash("EndStun");
        private readonly int _doDeathParam = Animator.StringToHash("DoDeath");

        private void Awake()
        {
            // Auto-find components if not assigned for convenience.
            if (visualsContainer == null)
            {
                visualsContainer = this.transform;
            }
            if (bodyAnimator == null)
            {
                bodyAnimator = GetComponent<Animator>();
            }
        }
    
        // --- PUBLIC COMMAND METHODS ---

        /// <summary>
        /// Flips the boss's visual representation horizontally.
        /// </summary>
        /// <param name="faceRight">True if the boss should face right, false for left.</param>
        public void Flip(bool faceRight)
        {
            float targetScaleX = faceRight ? 1f : -1f;
            visualsContainer.localScale = new Vector3(targetScaleX, 1f, 1f);
        }
    
        /// <summary>
        /// Sets the walking state of the boss's animation.
        /// </summary>
        public void SetWalking(bool isWalking)
        {
            bodyAnimator.SetBool(_isWalkingParam, isWalking);
        }

        /// <summary>
        /// Triggers the intro/phase transition roar animation.
        /// </summary>
        public void PlayRoarAnimation()
        {
            bodyAnimator.SetTrigger(_doRoarParam);
        }
    
        /// <summary>
        /// Triggers the melee swipe attack animation.
        /// </summary>
        public void PlayMeleeSwipeAnimation()
        {
            bodyAnimator.SetTrigger(_doMeleeSwipeParam);
        }

        /// <summary>
        /// Triggers the ground smash attack animation.
        /// </summary>
        public void PlayGroundSmashAnimation()
        {
            bodyAnimator.SetTrigger(_doGroundSmashParam);
        }

        /// <summary>
        /// Triggers the rush attack animation.
        /// </summary>
        public void PlayRushAnimation()
        {
            bodyAnimator.SetTrigger(_doRushParam);
        }

        /// <summary>
        /// Triggers the animation to enter the stunned state.
        /// </summary>
        public void PlayStunBeginAnimation()
        {
            bodyAnimator.SetTrigger(_doStunParam);
        }

        /// <summary>
        /// Triggers the animation to exit the stunned state.
        /// </summary>
        public void PlayStunEndAnimation()
        {
            bodyAnimator.SetTrigger(_endStunParam);
        }

        /// <summary>
        /// Triggers the final death animation.
        /// </summary>
        public void PlayDeathAnimation()
        {
            bodyAnimator.SetTrigger(_doDeathParam);
        }
    }
}