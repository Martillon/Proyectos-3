using Scripts.Core;
using Scripts.Core.Interfaces;
using UnityEngine;

namespace Scripts.Enemies.Boss.Attacks.Smash
{
    /// <summary>
    /// Controls the behavior of a single falling object, such as a rock or debris.
    /// It handles its own movement, collision, and destruction once it has been "dropped"
    /// by the FallingObjectManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FallingHazard : MonoBehaviour
    {
        [Header("Impact")]
        [Tooltip("The amount of damage this hazard deals on impact with the player.")]
        [SerializeField] private int damage = 10;
        [Tooltip("(Optional) A prefab for a particle effect to spawn on impact with any surface.")]
        [SerializeField] private GameObject impactVFX;
        
        // --- Private References & State ---
        private Rigidbody2D _rb;
        private bool _hasImpacted = false;

        /// <summary>
        /// Get component references on creation.
        /// </summary>
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Ensure the Rigidbody is set up correctly for this kind of object.
            // It shouldn't move until we tell it to.
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        /// <summary>
        /// This is the activation command, called by the FallingObjectManager.
        /// It starts the object's fall.
        /// </summary>
        /// <param name="fallSpeed">How fast the object should fall (a positive number).</param>
        /// <param name="lifetime">How long the object should exist before self-destructing if it hits nothing.</param>
        public void Drop(float fallSpeed, float lifetime)
        {
            // Change the Rigidbody to Dynamic so it can be moved and affected by gravity.
            _rb.bodyType = RigidbodyType2D.Dynamic;
            // Apply the initial downward velocity.
            _rb.linearVelocity = Vector2.down * fallSpeed;

            // Set the object's layer so it can interact with the player.
            // Make sure you have a "DamagePlayer" layer set up in your Project Settings.
            this.gameObject.layer = LayerMask.NameToLayer("DamagePlayer");
            
            // Start a timer to automatically destroy this object if it falls forever and hits nothing.
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Called by the physics engine when this object's collider makes contact with another.
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // A "guard clause" to ensure the impact logic only ever runs once.
            if (_hasImpacted) return;
            _hasImpacted = true;
            
            // Check if we hit the player.
            if (collision.gameObject.CompareTag(GameConstants.PlayerTag))
            {
                // Try to get the IDamageable interface from the player object and deal damage.
                if (collision.gameObject.TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    playerDamageable.TakeDamage(damage);
                }
            }
            
            // --- Impact Logic (for any collision) ---
            
            // Spawn a visual effect at the point of impact.
            if (impactVFX != null)
            {
                Instantiate(impactVFX, collision.contacts[0].point, Quaternion.identity);
            }

            // Destroy this hazard immediately upon any impact.
            Destroy(gameObject);
        }
    }
}