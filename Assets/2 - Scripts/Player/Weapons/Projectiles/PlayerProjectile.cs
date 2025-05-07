// --- START OF FILE PlayerProjectile.cs ---
using UnityEngine;
using Scripts.Core.Interfaces; // For IDamageable
using Scripts.Player.Weapons.Interfaces; // For IDamagingProjectile

namespace Scripts.Player.Weapons.Projectiles
{
    /// <summary>
    /// Handles the behavior of a projectile fired by the player.
    /// Moves in a given direction by manually updating its transform, applies damage on hit, and has a limited lifetime.
    /// Requires a Collider2D (set to Trigger) for hit detection.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerProjectile : MonoBehaviour, IDamagingProjectile
    {
        [Header("Movement Settings")]
        [Tooltip("Speed of the projectile in units per second.")]
        [SerializeField] private float speed = 20f;
        [Tooltip("Maximum lifetime of the projectile in seconds before it's automatically destroyed.")]
        [SerializeField] private float lifetime = 3f;

        [Header("Collision Settings")]
        [Tooltip("Tag identifying objects that this projectile can damage (e.g., enemies, destructibles).")]
        [SerializeField] private string hittableTag = "Hittable";
        [Tooltip("If true, this projectile will not collide with the player who fired it.")]
        [SerializeField] private bool ignorePlayerCollision = true; // Default to true

        // Damage will be set by the weapon that fires this projectile
        private float currentDamage = 1f;

        [Header("Visual Effects")]
        [Tooltip("Prefab instantiated on impact with a non-hittable surface (e.g., a wall).")]
        [SerializeField] private GameObject impactVFXPrefab;
        [Tooltip("Prefab instantiated on impact with a 'Hittable' target.")]
        [SerializeField] private GameObject hitTargetVFXPrefab;

        private Vector2 moveDirection;
        private bool initialized = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogError("PlayerProjectile: Collider2D component is missing!", this);
                enabled = false; // Disable script if no collider
                return;
            }
            if (!col.isTrigger)
            {
                // Debug.LogWarning("PlayerProjectile: Collider2D on " + gameObject.name + " is not set to IsTrigger. Setting it now.", this); // Uncomment for debugging
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// Initializes the projectile with a direction and prepares it for movement.
        /// </summary>
        /// <param name="direction">The normalized direction vector for the projectile's movement.</param>
        public void Initialize(Vector2 direction)
        {
            this.moveDirection = direction.normalized;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, moveDirection); // Orient sprite to direction
            
            Destroy(gameObject, lifetime);
            initialized = true;
            // Debug.Log($"PlayerProjectile Initialized. Direction: {moveDirection}, Speed: {speed}", this); // Uncomment for debugging
        }

        private void Update()
        {
            if (!initialized) return;

            // Manual movement
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// Sets the damage this projectile will inflict upon hitting a valid target.
        /// This is typically called by the weapon system immediately after instantiation.
        /// </summary>
        /// <param name="amount">The amount of damage.</param>
        public void SetDamage(float amount)
        {
            currentDamage = Mathf.Max(0, amount); // Ensure damage is not negative
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized) return;

            // Debug.Log($"PlayerProjectile OnTriggerEnter2D with: {other.gameObject.name}, Tag: {other.tag}", this); // Uncomment for debugging

            // Optional: Ignore collision with the player object itself
            if (ignorePlayerCollision && other.CompareTag("Player"))
            {
                // Debug.Log("PlayerProjectile: Ignored collision with Player.", this); // Uncomment for debugging
                return;
            }

            // Avoid self-collision or collision with other player projectiles
            if (other.GetComponent<PlayerProjectile>() != null)
            {
                // Debug.Log("PlayerProjectile: Ignored collision with another PlayerProjectile.", this); // Uncomment for debugging
                return; 
            }

            bool projectileConsumed = false;

            // Check if the collided object has the "Hittable" tag
            if (other.CompareTag(hittableTag))
            {
                if (other.TryGetComponent<IDamageable>(out var damageableTarget))
                {
                    damageableTarget.TakeDamage(currentDamage);
                    // Debug.Log($"PlayerProjectile: Dealt {currentDamage} damage to {other.name}.", this); // Uncomment for debugging
                    if (hitTargetVFXPrefab != null)
                    {
                        Instantiate(hitTargetVFXPrefab, transform.position, transform.rotation);
                    }
                    projectileConsumed = true;
                }
                // else Debug.LogWarning($"PlayerProjectile: Object {other.name} has tag '{hittableTag}' but no IDamageable component.", this); // Uncomment for debugging
            }
            else
            {
                // If it's not a hittable target, but a solid object (like a wall), consume the projectile.
                // We assume walls and environment are not triggers.
                // This check helps projectiles disappear on hitting walls rather than passing through if the wall has no specific interaction.
                if (!other.isTrigger) 
                {
                    // Debug.Log($"PlayerProjectile: Hit a non-trigger collider {other.name}. Consuming projectile.", this); // Uncomment for debugging
                    if (impactVFXPrefab != null)
                    {
                        Instantiate(impactVFXPrefab, transform.position, transform.rotation);
                    }
                    projectileConsumed = true;
                }
            }

            if (projectileConsumed)
            {
                Destroy(gameObject);
            }
        }
    }
}
// --- END OF FILE PlayerProjectile.cs ---
