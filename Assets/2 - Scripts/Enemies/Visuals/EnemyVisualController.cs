using System.Collections;
using Scripts.Core;
using Scripts.Enemies.Core;
using UnityEngine;

namespace Scripts.Enemies.Visuals
{
    /// <summary>
    /// Manages all visual aspects of an enemy, including animations and sprite flipping.
    /// Acts as the interface between the AI/Health systems and the Animator component.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyVisualController : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("The Animator component for this enemy's body.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("The root transform of the enemy's visual representation, used for flipping.")]
        [SerializeField] private Transform visualsContainer;
        
        [Header("Feedback")]
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private int hitFlashCount = 2;
        
        // --- Cached Components & State ---
        private Rigidbody2D _rb;
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _originalSpriteColors;
        private Coroutine _hitFlashCoroutine;
        private EnemyAIController _aiController;
        
        // --- Animator Hashes ---
        private readonly int _animIsMovingHash = Animator.StringToHash("isMoving");
        private readonly int _animMeleeAttackHash = Animator.StringToHash(GameConstants.AnimMeleeAttackTrigger);
        private readonly int _animRangedAttackHash = Animator.StringToHash(GameConstants.AnimRangedAttackTrigger);
        private readonly int _animDieHash = Animator.StringToHash(GameConstants.AnimDieTrigger);
        private readonly int _animForceIdleTrigger = Animator.StringToHash("forceIdle");
        // Window Enemy Hashes
        private readonly int _animWindowPlayerDetectedHash = Animator.StringToHash("isPlayerDetected");
        private readonly int _animWindowAttackHash = Animator.StringToHash(GameConstants.AnimWindowAttack);
        
        private void Awake()
        {
            if (bodyAnimator == null) bodyAnimator = GetComponent<Animator>();
            if (visualsContainer == null) visualsContainer = transform;
            _rb = GetComponentInParent<Rigidbody2D>();
            
            // Cache all sprite renderers and their original colors for the flash effect.
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _originalSpriteColors = new Color[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _originalSpriteColors[i] = _spriteRenderers[i].color;
            }
            
            _aiController = GetComponentInParent<EnemyAIController>();
            if (_aiController == null) Debug.LogError($"EVC on {name}: Missing EnemyAIController on parent!", this);
        }

        private void Update()
        {
            // Update movement animation for mobile enemies
            if (_rb != null)
            {
                bool isMoving = Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
                bodyAnimator.SetBool(_animIsMovingHash, isMoving);
            }
        }
        
        /// <summary>
        /// Called by the AI Controller to visually freeze the enemy.
        /// It stops the movement animation and forces the animator into an idle state.
        /// </summary>
        public void ForceIdleState()
        {
            // Ensure the 'isMoving' parameter is false to stop any walking/running animations.
            bodyAnimator.SetBool(_animIsMovingHash, false);

            // Set a trigger that can be used in the Animator to transition
            // from any state (like an attack wind-up) back to the Idle state.
            bodyAnimator.SetTrigger(_animForceIdleTrigger);
        }
        
        public void FlipVisuals(bool faceRight)
        {
            visualsContainer.localScale = new Vector3(faceRight ? 1 : -1, 1, 1);
        }
        
        public void StartHitFlash(float duration)
        {
            if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = StartCoroutine(HitFlashSequence(duration));
        }

        private IEnumerator HitFlashSequence(float duration)
        {
            float flashDuration = duration / (hitFlashCount * 2f);
            for (int i = 0; i < hitFlashCount; i++)
            {
                SetSpriteColor(hitFlashColor);
                yield return new WaitForSeconds(flashDuration);
                RestoreOriginalSpriteColor();
                yield return new WaitForSeconds(flashDuration);
            }
        }

        private void SetSpriteColor(Color color)
        {
            foreach(var renderer in _spriteRenderers)
            {
                renderer.color = color;
            }
        }

        private void RestoreOriginalSpriteColor()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _spriteRenderers[i].color = _originalSpriteColors[i];
            }
        }
        
        /// <summary>
        /// Generic event called by an animation at the moment an attack should deal damage.
        /// </summary>
        public void OnAnimationAttackAction()
        {
            _aiController?.HandleAnimationAttackAction();
        }

        /// <summary>
        /// Generic event called by an animation when the attack sequence has finished.
        /// </summary>
        public void OnAnimationAttackFinished()
        {
            _aiController?.HandleAnimationAttackFinished();
        }

        #region Animation Triggers
        
        public void TriggerMeleeAttack() => bodyAnimator.SetTrigger(_animMeleeAttackHash);
        public void TriggerRangedAttack() => bodyAnimator.SetTrigger(_animRangedAttackHash);
        public void TriggerDeathAnimation() => bodyAnimator.SetTrigger(_animDieHash);
        
        // --- Window Enemy Specific ---
        public void SetWindowPlayerDetected(bool isDetected) => bodyAnimator.SetBool(_animWindowPlayerDetectedHash, isDetected);
        public void TriggerWindowAttack() => bodyAnimator.SetTrigger(_animWindowAttackHash);
        public Animator GetBodyAnimator() => bodyAnimator;

        #endregion
    }
}