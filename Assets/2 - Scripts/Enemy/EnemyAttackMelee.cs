using UnityEngine;
using Scripts.Core;
using Scripts.Core.Interfaces;

namespace Scripts.Enemies
{
    /// <summary>
    /// Handles melee attack logic by checking proximity and applying damage.
    /// </summary>
    public class EnemyAttackMelee : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private int damage = 1;

        private float lastAttackTime;

        /// <summary>
        /// Attempts to damage the target if in range and cooldown allows.
        /// </summary>
        public void TryAttack(Transform target)
        {
            if (Time.time - lastAttackTime < attackCooldown) return;

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                if (target.TryGetComponent<IHealLife>(out _))
                {
                    // Prevent healing
                    return;
                }

                if (target.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                    // Debug.Log("Enemy melee attack successful.");
                    lastAttackTime = Time.time;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}
