using System.Collections;
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
        
        [Header("Feedback")]
        [Tooltip("The color the boss's sprite will flash to when hit.")]
        [SerializeField] private Color hitFlashColor = Color.white;
        [Tooltip("How many times the sprite will flash on a single hit.")]
        [SerializeField] private int hitFlashCount = 2;
        [Tooltip("The total duration of the hit flash effect.")]
        [SerializeField] private float hitFlashDuration = 0.2f;
        
        // --- Private State for Flashing ---
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _originalSpriteColors;
        private Coroutine _hitFlashCoroutine;
        
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
            
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _originalSpriteColors = new Color[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                // We store the original color of each sprite so we can revert back to it after the flash.
                if (_spriteRenderers[i] != null)
                {
                    _originalSpriteColors[i] = _spriteRenderers[i].color;
                }
            }
        }
    
        // --- PUBLIC COMMAND METHODS ---

        /// <summary>
        /// Sets the playback speed of the boss's animator.
        /// </summary>
        /// <param name="speedMultiplier">1 is normal speed, 1.5 is 50% faster, etc.</param>
        public void SetAnimatorSpeed(float speedMultiplier)
        {
            if (bodyAnimator != null)
            {
                bodyAnimator.speed = speedMultiplier;
            }
        }
        
        /// <summary>
        /// Flips the boss's visual representation horizontally.
        /// </summary>
        /// <param name="faceRight">True if the boss should face right, false for left.</param>
        public void Flip(bool faceRight)
        {
            float targetScaleX = faceRight ? -1f : 1f;
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
        
        /// <summary>
        /// Starts the hit flash visual feedback sequence.
        /// </summary>
        public void StartHitFlash()
        {
            // If a flash is already happening, stop it before starting a new one.
            if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = StartCoroutine(HitFlashSequence());
        }

        private IEnumerator HitFlashSequence()
        {
            // Calculate how long each individual flash/revert cycle should last.
            float flashDuration = hitFlashDuration / (hitFlashCount * 2f);
            
            for (int i = 0; i < hitFlashCount; i++)
            {
                SetAllSpriteColors(hitFlashColor);
                yield return new WaitForSeconds(flashDuration);
                RestoreOriginalSpriteColors();
                yield return new WaitForSeconds(flashDuration);
            }
        }

        private void SetAllSpriteColors(Color color)
        {
            foreach(var renderer in _spriteRenderers)
            {
                if (renderer != null) renderer.color = color;
            }
        }

        private void RestoreOriginalSpriteColors()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _spriteRenderers[i].color = _originalSpriteColors[i];
                }
            }
        }
    }
}