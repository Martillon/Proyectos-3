using System.Collections;
using Scripts.Core;
using Scripts.Core.Interfaces;
using UnityEngine;

namespace Scripts.Enemies.Boss.Attacks.Smash
{
    /// <summary>
    /// <summary>
    /// A generic component that handles the physics of a falling object.
    /// Its only job is to fall, land on the ground, and then activate other
    /// components on the same GameObject before self-destructing.
    /// </summary>
    public class FallingPowerup : MonoBehaviour
    {
        [Header("Landing Detection")]
        [Tooltip("How close to the ground the item needs to be to 'land'.")]
        [SerializeField] private float landingDistanceThreshold = 0.1f;
        [Tooltip("The LayerMask representing the ground.")]
        [SerializeField] private LayerMask groundLayer;

        // --- Private State ---
        private Rigidbody2D _rb;
        private bool _isFalling = false;
        private Coroutine _landingCheckCoroutine;
        
        // We will find any potential pickup script on the object.
        // This is flexible; it could be 'PowerupPickup' or any other pickup script you have.
        private MonoBehaviour _pickupScript;
        private Collider2D _pickupCollider;

        /// <summary>
        /// The activation command, called by the FallingObjectManager.
        /// </summary>
        public void Drop(float fallSpeed, float lifetime)
        {
            if (_isFalling) return;
            _isFalling = true;

            _rb = GetComponent<Rigidbody2D>();

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.linearVelocity = Vector2.down * fallSpeed;
            }

            // Start checking for the ground.
            _landingCheckCoroutine = StartCoroutine(CheckForGroundRoutine());
            
            // Fallback destruction timer.
            Destroy(gameObject, lifetime);
        }
        
        /// <summary>
        /// Continuously checks the distance to the ground below.
        /// </summary>
        private IEnumerator CheckForGroundRoutine()
        {
            while (_isFalling)
            {
                // Perform a raycast straight down from the item's position.
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, landingDistanceThreshold, groundLayer);

                // If the raycast hits the ground within our threshold...
                if (hit.collider != null)
                {
                    LandOnGround(hit.point);
                    // Stop the coroutine since we've landed.
                    yield break;
                }
                
                // Wait for the next physics update to check again.
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Logic to execute upon landing.
        /// </summary>
        private void LandOnGround(Vector3 landingPoint)
        {
            _isFalling = false; // Stop the ground check loop.

            // Stop all physical movement.
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Static;
                transform.position = landingPoint; // Snap precisely to the ground.
            }

            // --- THE HANDOVER ---
            // Enable the pre-existing pickup script and its collider.
            if (_pickupScript != null)
            {
                _pickupScript.enabled = true;
                if (_pickupCollider) _pickupCollider.enabled = true;

                // Optional: If your pickup script has a despawn timer, call a method to start it.
                // _pickupScript.StartDespawnTimer(); 
            }
            
            // This component has done its job. Remove it from the GameObject.
            Destroy(this);
        }
    }
}