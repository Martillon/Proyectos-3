// --- START OF FILE EnemyMeleeHitbox.cs ---

using System.Collections.Generic;
using UnityEngine;
using Scripts.Core.Interfaces; // For IDamageable
using Scripts.Core; // For GameConstants

namespace Scripts.Enemies.Melee
{
    /// <summary>
    /// This component resides on a trigger collider GameObject that represents the enemy's melee attack area.
    /// It detects collisions with damageable targets (like the player) when its GameObject is active
    /// and applies damage passed to it by the enemy's main attack script.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyMeleeHitbox : MonoBehaviour
    {
        private int damageToDeal = 0;
        private bool canDamageThisSwing = false; // Prevents multiple hits from a single activation/swing
        private Collider2D hitboxCollider;

        // Optional: Store a list of targets already hit in this swing to avoid multi-hits on same target from one swing
        private List<Collider2D> targetsHitThisSwing; 

        private void Awake()
        {
            hitboxCollider = GetComponent<Collider2D>();
            if (hitboxCollider == null)
            {
                Debug.LogError($"EnemyMeleeHitbox on '{gameObject.name}' is missing its Collider2D.", this);
                enabled = false;
                return;
            }
            if (!hitboxCollider.isTrigger)
            {
                // Debug.LogWarning($"EnemyMeleeHitbox on '{gameObject.name}': Collider2D is not set to 'Is Trigger'. Forcing it to true.", this); // Uncomment for debugging
                hitboxCollider.isTrigger = true;
            }
            targetsHitThisSwing = new List<Collider2D>();
            gameObject.SetActive(false); // Ensure it's off by default
        }

        /// <summary>
        /// Activates the hitbox for a single attack swing and sets the damage it will deal.
        /// </summary>
        /// <param name="damage">The amount of damage to deal on hit.</param>
        public void Activate(int damage)
        {
            // Debug.Log($"EnemyMeleeHitbox '{gameObject.name}' Activated with damage: {damage}", this); // Uncomment for debugging
            this.damageToDeal = damage;
            this.canDamageThisSwing = true;
            targetsHitThisSwing.Clear(); // Clear list for new swing
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates the hitbox, typically after the attack swing is complete.
        /// </summary>
        public void Deactivate()
        {
            // Debug.Log($"EnemyMeleeHitbox '{gameObject.name}' Deactivated.", this); // Uncomment for debugging
            gameObject.SetActive(false);
            this.canDamageThisSwing = false; // Reset for next activation
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!canDamageThisSwing || !gameObject.activeSelf) return; // Only process if active and meant to damage

            // Prevent hitting the same target multiple times in one swing
            if (targetsHitThisSwing.Contains(other))
            {
                return;
            }

            if (other.CompareTag(GameConstants.PlayerTag)) // Using GameConstants
            {
                // Debug.Log($"EnemyMeleeHitbox '{gameObject.name}' hit Player: {other.name}", this); // Uncomment for debugging
                if (other.TryGetComponent<IDamageable>(out var damageableTarget))
                {
                    damageableTarget.TakeDamage(damageToDeal);
                    targetsHitThisSwing.Add(other); // Add to list of hit targets for this swing

                    // Optional: Play hit sound or spawn hit particle effect here or in TakeDamage

                    // If your melee attack should only hit one target per swing, you might deactivate early:
                    // Deactivate(); 
                }
            }
        }
    }
}
// --- END OF FILE EnemyMeleeHitbox.cs ---
