using UnityEngine;
using System.Collections;

namespace Scripts.Items.Visuals
{
    public class PickupAnimator : MonoBehaviour
    {
        [Header("Vertical Bobbing Settings")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobbingHeight = 0.1f;
        [SerializeField] private float bobbingSpeed = 2f;

        [Header("Horizontal Flip Settings")]
        [SerializeField] private bool enableFlipping = true;
        [Tooltip("Time (in seconds) the item stays facing one direction before starting to flip.")]
        [SerializeField] private float holdDurationBeforeFlip = 1.0f; 
        [Tooltip("Duration (in seconds) for the visual flip animation (squash to zero and expand to new scale).")]
        [SerializeField] private float flipAnimationTime = 0.25f; // Total time for 1 -> 0 -> -1

        private Vector3 initialLocalPosition;
        private float currentBobbingOffset;

        private float timeUntilNextFlipAction;
        private Coroutine flipCoroutine;
        private float currentVisualScaleX = 1f; // Represents the sign of the X scale
        private Vector3 originalScaleMagnitude;

        void Start()
        {
            initialLocalPosition = transform.localPosition;
            originalScaleMagnitude = new Vector3(
                Mathf.Abs(transform.localScale.x),
                Mathf.Abs(transform.localScale.y),
                Mathf.Abs(transform.localScale.z)
            );
            currentVisualScaleX = Mathf.Sign(transform.localScale.x); // Respect initial flip
            ApplyScale(); // Apply initial scale correctly
            timeUntilNextFlipAction = holdDurationBeforeFlip;
        }

        void Update()
        {
            if (enableBobbing)
            {
                HandleBobbing();
            }

            if (enableFlipping)
            {
                if (flipCoroutine == null) // Only countdown if not already flipping
                {
                    timeUntilNextFlipAction -= Time.deltaTime;
                    if (timeUntilNextFlipAction <= 0f)
                    {
                        flipCoroutine = StartCoroutine(SmoothFlipCoroutine());
                    }
                }
            }
        }

        private void HandleBobbing()
        {
            currentBobbingOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.localPosition = new Vector3(
                initialLocalPosition.x,
                initialLocalPosition.y + currentBobbingOffset,
                initialLocalPosition.z
            );
        }
        
        private void ApplyScale()
        {
            transform.localScale = new Vector3(
                originalScaleMagnitude.x * currentVisualScaleX,
                originalScaleMagnitude.y, // Use original Y magnitude
                originalScaleMagnitude.z  // Use original Z magnitude
            );
        }

        private IEnumerator SmoothFlipCoroutine()
        {
            float targetScaleSign = currentVisualScaleX * -1;
            float halfFlipTime = flipAnimationTime / 2f; // Time to scale to 0, and time to scale from 0

            Vector3 startScale = transform.localScale;
            Vector3 midScale = new Vector3(0, originalScaleMagnitude.y, originalScaleMagnitude.z);
            Vector3 endScale = new Vector3(originalScaleMagnitude.x * targetScaleSign, originalScaleMagnitude.y, originalScaleMagnitude.z);

            float elapsedTime = 0f;

            // Phase 1: Scale X to 0
            while (elapsedTime < halfFlipTime)
            {
                float newX = Mathf.Lerp(startScale.x, midScale.x, elapsedTime / halfFlipTime);
                transform.localScale = new Vector3(newX, startScale.y, startScale.z);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = midScale;

            // Update currentVisualScaleX logically for the expansion, though visually it's 0
            // The actual visual flip happens when expanding to the new targetScaleSign
            currentVisualScaleX = targetScaleSign; 

            elapsedTime = 0f;
            // Phase 2: Scale X from 0 to target
            while (elapsedTime < halfFlipTime)
            {
                float newX = Mathf.Lerp(midScale.x, endScale.x, elapsedTime / halfFlipTime);
                transform.localScale = new Vector3(newX, endScale.y, endScale.z);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = endScale;

            timeUntilNextFlipAction = holdDurationBeforeFlip;
            flipCoroutine = null;
        }

        public void ResetVisualState()
        {
            if (flipCoroutine != null)
            {
                StopCoroutine(flipCoroutine);
                flipCoroutine = null;
            }
            transform.localPosition = initialLocalPosition;
            currentVisualScaleX = Mathf.Sign(originalScaleMagnitude.x); // Or always 1f if you want it to reset to non-flipped
            ApplyScale();
            timeUntilNextFlipAction = holdDurationBeforeFlip;
        }
        
        // private void OnEnable() // Consider if you need this for robust pooling
        // {
        //     if (originalScaleMagnitude == Vector3.zero) // If Start hasn't run (e.g. pooled object first time)
        //     {
        //         originalScaleMagnitude = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y),Mathf.Abs(transform.localScale.z));
        //         if (originalScaleMagnitude.x == 0) originalScaleMagnitude.x = 1f; // Avoid zero scale
        //         if (originalScaleMagnitude.y == 0) originalScaleMagnitude.y = 1f;
        //         if (originalScaleMagnitude.z == 0) originalScaleMagnitude.z = 1f;
        //     }
        //     ResetVisualState();
        // }
    }
}
// --- END OF FILE PickupAnimator.cs ---
