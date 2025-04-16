using UnityEngine;
using Scripts.Core.Interfaces;

namespace Scripts.Enemies.Core
{
    /// <summary>
    /// EnemyHealth
    /// 
    /// Handles enemy health, damage intake, and death behavior.
    /// Supports death animation, destruction, or pooling. Debug logs and hooks prepared.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        [Header("References")]
        [SerializeField] private Renderer[] renderersToDisable;

        private float currentHealth;
        private bool isDead = false;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Applies damage to the enemy. Triggers death if health reaches zero.
        /// </summary>
        /// <param name="amount">Amount of damage to apply.</param>
        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            // Debug.Log($"Enemy took {amount} damage. Current health: {currentHealth}");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Called when the enemy's health reaches zero.
        /// Disables visuals and optionally destroys or disables the GameObject.
        /// </summary>
        private void Die()
        {
            isDead = true;

            // TODO: Trigger death animation here
            // e.g., animator.SetTrigger("Die");

            foreach (Renderer r in renderersToDisable)
            {
                r.enabled = false;
            }

            // Future: Broadcast death event here
            // e.g., OnEnemyDeath?.Invoke(this);

            if (destroyOnDeath)
            {
                // Debug.Log("Enemy destroyed on death.");
                Destroy(gameObject);
            }
            else
            {
                // Debug.Log("Enemy disabled on death (pooling).");
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Resets enemy health. Can be used for pooling or respawn.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;

            foreach (Renderer r in renderersToDisable)
            {
                r.enabled = true;
            }

            // Debug.Log("Enemy health reset.");
        }

        public bool IsDead => isDead;
    }
}
