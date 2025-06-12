using UnityEngine;
using Scripts.Core.Interfaces;
using Scripts.Core;

namespace Scripts.Enemies.Ranged
{
    /// <summary>
    /// Behavior for a projectile fired by an enemy. Moves in a straight line, damages the player on impact.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private int damage = 10;
        
        [Header("Feedback")]
        [Tooltip("Prefab instantiated on impact.")]
        [SerializeField] private GameObject impactVFX;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // Use Kinematic for manual control
            Destroy(gameObject, lifetime);
        }

        public void Initialize(Vector2 direction, int statsAttackDamage)
        {
            GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(GameConstants.PlayerTag))
            {
                if (other.TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    playerDamageable.TakeDamage(damage);
                }
                
                if (impactVFX != null) Instantiate(impactVFX, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
            // Optionally, destroy on hitting solid environment
            else if (!other.isTrigger)
            {
                if (impactVFX != null) Instantiate(impactVFX, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}