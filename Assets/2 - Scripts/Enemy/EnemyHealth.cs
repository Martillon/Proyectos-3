// --- START OF FILE EnemyHealth.cs ---
using UnityEngine;
using Scripts.Core.Interfaces; // For IDamageable
using System.Collections;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Ranged;

// Ajusta el namespace si es diferente
namespace Scripts.Enemies.Core 
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        [Header("Damage Feedback")]
        [SerializeField] private float damageInvulnerabilityDuration = 0.5f;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private int damageFlashCount = 3;

        private float currentHealth;
        private bool isDead = false;
        private bool isInvulnerableFromDamage = false;

        private SpriteRenderer spriteRenderer;
        private Color originalSpriteColor;
        private Coroutine damageFeedbackCoroutine;

        // References to disable attacks during hit stun
        private EnemyAttackMelee meleeAttacker;
        private EnemyAttackRanged rangedAttacker;
        // Or a more generic IEnemyAttack attackerComponent;

        private void Awake()
        {
            currentHealth = maxHealth;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalSpriteColor = spriteRenderer.color;
            }
            // else: Error already logged if missing

            meleeAttacker = GetComponent<EnemyAttackMelee>();
            rangedAttacker = GetComponent<EnemyAttackRanged>();
            // attackerComponent = GetComponent<IEnemyAttack>(); // If using a generic interface
        }

        public void TakeDamage(float amount)
        {
            if (isDead || isInvulnerableFromDamage) return;

            currentHealth -= amount;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
            else
            {
                if (damageFeedbackCoroutine != null)
                {
                    StopCoroutine(damageFeedbackCoroutine);
                }
                damageFeedbackCoroutine = StartCoroutine(DamageFeedbackSequence());
            }
        }

        private IEnumerator DamageFeedbackSequence()
        {
            isInvulnerableFromDamage = true;
            bool meleeWasEnabled = meleeAttacker != null && meleeAttacker.enabled;
            bool rangedWasEnabled = rangedAttacker != null && rangedAttacker.enabled;

            if (meleeWasEnabled) meleeAttacker.enabled = false;
            if (rangedWasEnabled) rangedAttacker.enabled = false;

            if (spriteRenderer != null && damageFlashCount > 0 && damageInvulnerabilityDuration > 0)
            {
                float flashPartDuration = damageInvulnerabilityDuration / (damageFlashCount * 2f);
                for (int i = 0; i < damageFlashCount; i++)
                {
                    spriteRenderer.color = damageFlashColor;
                    yield return new WaitForSeconds(flashPartDuration);
                    spriteRenderer.color = originalSpriteColor;
                    yield return new WaitForSeconds(flashPartDuration);
                }
                spriteRenderer.color = originalSpriteColor;
            }
            else
            {
                yield return new WaitForSeconds(damageInvulnerabilityDuration);
            }

            if (meleeWasEnabled && meleeAttacker != null) meleeAttacker.enabled = true;
            if (rangedWasEnabled && rangedAttacker != null) rangedAttacker.enabled = true;
            
            isInvulnerableFromDamage = false;
            damageFeedbackCoroutine = null;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (damageFeedbackCoroutine != null)
            {
                StopCoroutine(damageFeedbackCoroutine);
                if (spriteRenderer != null) spriteRenderer.color = originalSpriteColor;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) 
            {
                rb.linearVelocity = Vector2.zero; // Stop any existing movement
                rb.angularVelocity = 0f;   // Stop any existing rotation
                rb.bodyType = RigidbodyType2D.Static; // Make the body static so it doesn't move or fall
            }

            // Disable attack components explicitly if they exist
            if(meleeAttacker != null) meleeAttacker.enabled = false;
            if(rangedAttacker != null) rangedAttacker.enabled = false;
            // if(attackerComponent != null && attackerComponent is MonoBehaviour mb) mb.enabled = false;

            // TODO: Trigger death animation, play sound, spawn effects

            if (destroyOnDeath)
            {
                Destroy(gameObject, 0.1f); // Short delay for effects
            }
            else
            {
                gameObject.SetActive(false); // For pooling
            }
        }

        public void ResetHealthAndRevive()
        {
            currentHealth = maxHealth;
            isDead = false;
            isInvulnerableFromDamage = false;
            if (spriteRenderer != null) spriteRenderer.color = originalSpriteColor;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) 
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // Restore to dynamic for physics
            }

            if (meleeAttacker != null) meleeAttacker.enabled = true;
            if (rangedAttacker != null) rangedAttacker.enabled = true;
            // if(attackerComponent != null && attackerComponent is MonoBehaviour mb) mb.enabled = true;

            gameObject.SetActive(true);
        }

        public bool IsDead => isDead;
    }
}
// --- END OF FILE EnemyHealth.cs ---