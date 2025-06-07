using UnityEngine;
using System.Collections.Generic;
using Scripts.Core;
using Scripts.Core.Interfaces;

namespace Scripts.Enemies.Melee
{
    /// <summary>
    /// Represents the damage-dealing trigger area for an enemy's melee attack.
    /// It activates for a short duration and damages any IDamageable targets it hits.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyMeleeHitbox : MonoBehaviour
    {
        private int _damageToDeal;
        // This list prevents a single swing from hitting the same target multiple times.
        private readonly List<Collider2D> _targetsHitThisSwing = new List<Collider2D>();

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            gameObject.SetActive(false); // Ensure it starts disabled.
        }
        
        /// <summary>
        /// Activates the hitbox for a single attack.
        /// </summary>
        /// <param name="damage">The amount of damage to deal on hit.</param>
        public void Activate(int damage)
        {
            _damageToDeal = damage;
            _targetsHitThisSwing.Clear();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates the hitbox, ending the attack.
        /// </summary>
        public void Deactivate()
        {
            gameObject.SetActive(false);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_targetsHitThisSwing.Contains(other))
            {
                return; // Already hit this target during this swing.
            }
            
            if (other.CompareTag(GameConstants.PlayerTag))
            {
                if (other.TryGetComponent<IDamageable>(out var damageableTarget))
                {
                    damageableTarget.TakeDamage(_damageToDeal);
                    _targetsHitThisSwing.Add(other); // Add to list to prevent multi-hit.
                }
            }
        }
    }
}
