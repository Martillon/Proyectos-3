using Scripts.Core.Interfaces;
using UnityEngine;

namespace Scripts.Player.Core
{
    /// <summary>
    /// Manages the player's lives and armor.
    /// Each life allows a defined number of hits (armor).
    /// Healing can restore armor or full lives, with limits.
    /// </summary>
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int maxArmorPerLife = 3;

        [Header("Debug")]
        [SerializeField] private bool destroyOnDeath = true;

        private int currentLives;
        private int currentArmor;

        private void Awake()
        {
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
        }

        /// <summary>
        /// Applies 1 hit to the player. If armor is depleted, a life is lost.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (currentLives <= 0) return;

            if (currentArmor > 0)
            {
                currentArmor--;
                // Debug.Log($"Damage taken. Armor reduced to {currentArmor}.");
            }
            else
            {
                currentLives--;
                currentArmor = maxArmorPerLife;

                // Debug.Log($"Armor broken. Life lost. Lives left: {currentLives}.");

                if (currentLives <= 0)
                {
                    HandleDeath();
                }
            }
        }

        /// <summary>
        /// Heals armor up to its maximum, but does not restore lives.
        /// </summary>
        public void HealArmor(int amount)
        {
            if (amount <= 0 || currentLives <= 0) return;

            currentArmor = Mathf.Min(currentArmor + amount, maxArmorPerLife);
            // Debug.Log($"Armor healed. New armor: {currentArmor}");
        }

        /// <summary>
        /// Restores life and fully restores armor. Limited by maxLives.
        /// </summary>
        public void HealLife(int amount)
        {
            if (amount <= 0) return;

            currentLives = Mathf.Min(currentLives + amount, maxLives);
            currentArmor = maxArmorPerLife;

            // Debug.Log($"Life healed. Lives: {currentLives}, Armor reset to: {currentArmor}");
        }

        /// <summary>
        /// Called when player runs out of lives.
        /// </summary>
        private void HandleDeath()
        {
            // TODO: Trigger death animation, camera effect, respawn, etc.
            // Debug.Log("Player died.");

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        // Getters for UI or external logic
        public int CurrentLives => currentLives;
        public int CurrentArmor => currentArmor;
        public int MaxLives => maxLives;
        public int MaxArmorPerLife => maxArmorPerLife;

        /// <summary>
        /// Debug-only method to fully restore health.
        /// </summary>
        public void RestoreAll()
        {
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
            // Debug.Log("Health fully restored (cheat/debug).");
        }
    }
}
