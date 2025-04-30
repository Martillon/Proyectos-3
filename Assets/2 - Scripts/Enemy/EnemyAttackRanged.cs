using UnityEngine;

namespace Scripts.Enemies
{
    /// <summary>
    /// Handles ranged attack by instantiating projectiles in 8 fixed directions.
    /// </summary>
    public class EnemyAttackRanged : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float projectileSpeed = 10f;

        private float lastAttackTime;

        public void TryAttack(Transform target)
        {
            if (projectilePrefab == null || firePoint == null) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            Vector2 direction = GetSnappedDirection(target.position - firePoint.position);

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = direction * projectileSpeed;

            lastAttackTime = Time.time;

            // Debug.Log("Enemy fired projectile.");
        }

        /// <summary>
        /// Converts a raw direction into one of 8 possible directions.
        /// </summary>
        private Vector2 GetSnappedDirection(Vector2 input)
        {
            input.Normalize();
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            angle = Mathf.Round(angle / 45f) * 45f;

            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.right * 0.5f);
                Gizmos.DrawWireSphere(firePoint.position, 0.05f);
            }
        }
#endif
    }
}
