using UnityEngine;

namespace Scripts.Enemies
{
    /// <summary>
    /// Handles ranged attacks by spawning a projectile in 8 fixed directions.
    /// Only fires if the player is within attack range and visible via raycast.
    /// </summary>
    public class EnemyAttackRanged : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackRange = 6f;

        [Header("Detection")]
        [SerializeField] private LayerMask playerLayer;

        private float lastAttackTime;

        /// <summary>
        /// Attempts to fire a projectile if the player is within attack range and line of sight.
        /// </summary>
        public void TryAttack(Transform target)
        {
            if (projectilePrefab == null || firePoint == null || target == null) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            Vector2 toTarget = target.position - firePoint.position;
            float distance = toTarget.magnitude;

            if (distance > attackRange) return;

            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, toTarget.normalized, distance, playerLayer);
            if (hit.collider == null || !hit.collider.CompareTag("Player")) return;

            Vector2 direction = GetSnappedDirection(toTarget);

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = direction; // Velocity defined inside projectile script

            lastAttackTime = Time.time;

            // Debug.Log("Ranged enemy fired at player.");
        }

        /// <summary>
        /// Converts a raw direction vector into one of 8 fixed directions.
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
                Gizmos.DrawWireSphere(firePoint.position, attackRange);
                Gizmos.DrawWireSphere(firePoint.position, 0.05f);
            }
        }
#endif
    }
}
