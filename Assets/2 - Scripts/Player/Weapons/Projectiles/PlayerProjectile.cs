using UnityEngine;
using Scripts.Core.Interfaces;
using Scripts.Player.Weapons.Interfaces;

namespace Scripts.Player.Weapons.Projectiles
{
    /// <summary>
    /// Defines the behavior of a projectile fired by the player.
    /// It moves, detects collisions, applies damage, and spawns impact effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerProjectile : MonoBehaviour, IDamagingProjectile
    {
        [Header("Movement")]
        [Tooltip("Speed of the projectile in units per second.")]
        [SerializeField] private float speed = 25f;
        [Tooltip("Maximum lifetime in seconds before the projectile is automatically destroyed.")]
        [SerializeField] private float lifetime = 2f;

        [Header("Collision & Damage")]
        [Tooltip("Layers that this projectile will collide with and be destroyed by.")]
        [SerializeField] private LayerMask collisionLayers;
        
        [Header("Visual Effects")]
        [Tooltip("Prefab to instantiate on impact with a damageable target.")]
        [SerializeField] private GameObject hitVFX;
        [Tooltip("Prefab to instantiate on impact with a non-damageable surface (e.g., a wall).")]
        [SerializeField] private GameObject impactVFX;

        private Rigidbody2D _rb;
        private float _damage;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Configure Rigidbody for projectile behavior
            _rb.bodyType = RigidbodyType2D.Kinematic; // We will control movement manually for precision
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Best for fast-moving objects
            
            GetComponent<Collider2D>().isTrigger = true;
            
            // Automatically destroy the projectile after its lifetime expires.
            Destroy(gameObject, lifetime);
        }

        public void Initialize(Vector2 direction)
        {
            // Set the projectile's velocity.
            _rb.linearVelocity = direction.normalized * speed;
            
            // Orient the sprite to face the direction of movement.
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }
        
        public void SetDamage(float damage)
        {
            _damage = damage;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Ignore collisions with other triggers (e.g., other projectiles, pickup zones).
            if (other.isTrigger) return;
            
            // Check if the collided layer is one of our designated collision layers.
            if ((collisionLayers.value & (1 << other.gameObject.layer)) > 0)
            {
                // Try to apply damage if the object is damageable.
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_damage);
                    SpawnVFX(hitVFX);
                }
                else // It's a solid, non-damageable object (like a wall).
                {
                    SpawnVFX(impactVFX);
                }
                
                // Destroy the projectile on any valid impact.
                Destroy(gameObject);
            }
        }

        private void SpawnVFX(GameObject vfxPrefab)
        {
            if (vfxPrefab)
            {
                Instantiate(vfxPrefab, transform.position, transform.rotation);
            }
        }
    }
}
