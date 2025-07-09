using UnityEngine;
using System.Collections.Generic; 

namespace Scripts.Enemies.Boss.Core
{
    
    /// <summary>
    /// A generic health component designed for boss characters. It manages health,
    /// vulnerability states, phase transitions, and communicates its state via events.
    /// </summary>
    
    public class BossHealth : MonoBehaviour
    {
        // This enum defines the different ways a boss can take damage.
        // We can select the desired behavior in the Inspector.
        public enum VulnerabilityType
        {
            AlwaysVulnerable,           // Takes full damage at all times.
            ReducedDamageWhenNotVulnerable, // Takes "chip" damage normally, full damage when vulnerable.
            InvulnerableWhenNotVulnerable // Takes zero damage unless vulnerable.
        }

        [Header("Health & Phases")]
        [Tooltip("The boss's maximum health.")]
        [SerializeField] private float maxHealth = 5000f;
        [Tooltip("Health percentages (0.0 to 1.0) that trigger a phase change. E.g., 0.75 for 75%.")]
        [SerializeField] private List<float> phaseHealthThresholds = new List<float> { 0.75f, 0.50f, 0.25f };

        [Header("Vulnerability")]
        [Tooltip("Determines how the boss takes damage when not in a 'vulnerable' state.")]
        [SerializeField] private VulnerabilityType vulnerabilityType = VulnerabilityType.ReducedDamageWhenNotVulnerable;
        [Tooltip("The damage multiplier to apply when NOT vulnerable (e.g., 0.1 for 10% damage taken). Only used if type is 'ReducedDamage...'.")]
        [Range(0f, 1f)]
        [SerializeField] private float damageReductionMultiplier = 0.1f;

        // --- Events ---
        // Other scripts (like the UI or BossController) can subscribe to these events
        // to react to changes in the boss's health state.

        // Fired whenever health changes. Passes (currentHealth, maxHealth). Useful for a UI health bar.
        public event System.Action<float, float> OnHealthChanged;
        // Fired ONCE when a health threshold is crossed. Passes the phase index (e.g., 1, 2, 3).
        public event System.Action<int> OnPhaseThresholdReached;
        // Fired ONCE when health reaches zero.
        public event System.Action OnDeath;

        // --- Private State ---
        private float _currentHealth;
        private bool _isVulnerable = false;     // Is the boss in a "stunned" or "dizzy" state?
        private bool _isInvulnerable = false;   // A hard override for cutscenes or phase transitions.
        private bool _isDead = false;
        // We use a copy of the thresholds list so we can safely remove them as they are triggered.
        private List<float> _remainingThresholds;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            // Initialize health and create a modifiable copy of the thresholds.
            _currentHealth = maxHealth;
            _remainingThresholds = new List<float>(phaseHealthThresholds);
            // Sort in descending order so we check for the highest threshold first.
            _remainingThresholds.Sort((a, b) => b.CompareTo(a)); 
        }

        /// <summary>
        /// The main method for dealing damage to the boss.
        /// Handles all vulnerability and reduction logic.
        /// </summary>
        /// <param name="amount">The base amount of damage to deal.</param>
        public void TakeDamage(float amount)
        {
            // Ignore all damage if the boss is dead or in a hard invulnerable state (like an intro).
            if (_isDead || _isInvulnerable)
            {
                return;
            }

            float damageToDeal = amount;

            // Apply damage modifications based on the selected vulnerability type.
            if (!_isVulnerable) // If the boss is NOT in its special "dizzy" state...
            {
                switch (vulnerabilityType)
                {
                    case VulnerabilityType.ReducedDamageWhenNotVulnerable:
                        damageToDeal *= damageReductionMultiplier; // Apply chip damage.
                        break;
                    case VulnerabilityType.InvulnerableWhenNotVulnerable:
                        return; // Take no damage and exit the method.
                }
            }
            
            // Subtract the final calculated damage from current health.
            _currentHealth -= damageToDeal;
            _currentHealth = Mathf.Max(_currentHealth, 0); // Ensure health doesn't go below zero.

            // Fire the health changed event so the UI can update.
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Check if we've crossed a phase threshold.
            CheckForPhaseTransition();
            
            // Check if the boss has been defeated.
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Allows other scripts (like the BossController or an attack script) to make the boss
        /// enter or exit its "dizzy" state where it takes full damage.
        /// </summary>
        public void SetVulnerability(bool isVulnerable)
        {
            _isVulnerable = isVulnerable;
        }
        
        /// <summary>
        /// Allows the BossController to set a hard invulnerability flag, typically for
        /// intros, cutscenes, or phase transitions.
        /// </summary>
        public void SetInvulnerability(bool isInvulnerable)
        {
            _isInvulnerable = isInvulnerable;
        }

        /// <summary>
        /// Checks if the current health has dropped below any of the remaining phase thresholds.
        /// </summary>
        private void CheckForPhaseTransition()
        {
            // We only check if there are any thresholds left to trigger.
            if (_remainingThresholds.Count == 0) return;

            // Check if current health percentage is below the NEXT threshold.
            float currentHealthPercent = _currentHealth / maxHealth;
            if (currentHealthPercent <= _remainingThresholds[0])
            {
                // Calculate which phase we just entered.
                int phaseIndex = phaseHealthThresholds.Count - _remainingThresholds.Count + 1;
                
                // Fire the event to notify the BossController.
                OnPhaseThresholdReached?.Invoke(phaseIndex);
                
                // Remove the threshold so it cannot be triggered again.
                _remainingThresholds.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Forcibly sets the boss's health to a specific percentage of its maximum.
        /// Used for loading from a phase checkpoint.
        /// </summary>
        /// <param name="percentage">The target health percentage (e.g., 0.75 for 75%).</param>
        public void SetHealthToPercentage(float percentage)
        {
            _currentHealth = maxHealth * Mathf.Clamp01(percentage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Re-evaluate which phase thresholds have been passed.
            // We clear the remaining list and rebuild it.
            _remainingThresholds.Clear();
            _remainingThresholds = new List<float>(phaseHealthThresholds);
            _remainingThresholds.Sort((a, b) => b.CompareTo(a)); 

            // Remove thresholds that are higher than our new health percentage.
            _remainingThresholds.RemoveAll(t => t >= percentage);
        }

        /// <summary>
        /// Handles the death of the boss.
        /// </summary>
        private void Die()
        {
            // A "guard clause" to ensure the Die logic only ever runs once.
            if (_isDead) return;
            _isDead = true;

            Debug.Log(gameObject.name + " has been defeated!");

            // Fire the death event so the BossController and EncounterTrigger can react.
            OnDeath?.Invoke();
        }
    }
}