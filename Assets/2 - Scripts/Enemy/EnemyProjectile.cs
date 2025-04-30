using UnityEngine;
using Scripts.Core;
using Scripts.Core.Interfaces;

namespace Scripts.Enemies
{
    /// <summary>
    /// Basic projectile logic for enemy ranged attacks.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifeTime = 5f;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(damage);
                // Debug.Log("Enemy projectile hit.");
                Destroy(gameObject);
            }
        }
    }
}
