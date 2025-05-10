// --- START OF FILE EnemyProjectile.cs ---
using UnityEngine;
using Scripts.Core.Interfaces; // For IDamageable

namespace Scripts.Enemies.Ranged
{
    /// <summary>
    /// Handles behavior for projectiles fired by enemies.
    /// Moves in a straight line, damages the player on impact, and has a limited lifetime.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour // Does not need IDamagingProjectile if damage is fixed on prefab
    {
        [Header("Movement Settings")]
        [Tooltip("Speed of the projectile in units per second.")]
        [SerializeField] private float speed = 10f;
        [Tooltip("Maximum lifetime of the projectile in seconds.")]
        [SerializeField] private float lifetime = 5f;

        [Header("Damage Settings")]
        [Tooltip("Amount of damage this projectile inflicts on the player.")]
        [SerializeField] private int damageAmount = 1; // Fixed damage for this projectile type

        [Header("Collision Settings")]
        [Tooltip("Tag identifying the player GameObject.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Visual Effects")]
        [Tooltip("Prefab instantiated on impact (e.g., with player or environment).")]
        [SerializeField] private GameObject impactVFXPrefab;

        private Vector2 moveDirection;
        private bool initialized = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogError("EnemyProjectile: Collider2D component is missing!", this);
                enabled = false;
                return;
            }
            if (!col.isTrigger)
            {
                // Debug.LogWarning($"EnemyProjectile on '{gameObject.name}': Collider2D is not set to IsTrigger. Forcing it.", this); // Uncomment for debugging
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
            // Orient sprite to direction if it's not a perfectly round projectile
            transform.rotation = Quaternion.LookRotation(Vector3.forward, moveDirection); 
            
            Destroy(gameObject, lifetime);
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized) return;

            // Debug.Log($"EnemyProjectile OnTriggerEnter2D with: {other.gameObject.name}, Tag: {other.tag}", this); // Uncomment for debugging

            // Check if it hit the player
            if (other.CompareTag(playerTag))
            {
                if (other.TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    playerDamageable.TakeDamage(damageAmount);
                    // Debug.Log($"EnemyProjectile: Dealt {damageAmount} damage to Player.", this); // Uncomment for debugging
                }
                // else Debug.LogWarning($"EnemyProjectile: Collided with object tagged '{playerTag}' but it has no IDamageable component.", this); // Uncomment for debugging

                SpawnImpactVFX();
                Destroy(gameObject); // Projectile is consumed after hitting the player
                return; // Stop further processing
            }

            // Optionally, destroy projectile if it hits environment (non-trigger colliders)
            // but not other enemies or their projectiles
            if (other.GetComponent<EnemyAIController>() == null && other.GetComponent<EnemyProjectile>() == null && !other.isTrigger)
            {
                // Debug.Log($"EnemyProjectile: Hit environment ({other.name}). Destroying.", this); // Uncomment for debugging
                SpawnImpactVFX();
                Destroy(gameObject);
            }
        }

        private void SpawnImpactVFX()
        {
            if (impactVFXPrefab != null)
            {
                Instantiate(impactVFXPrefab, transform.position, transform.rotation);
            }
        }
    }
}
// --- END OF FILE EnemyProjectile.cs ---
