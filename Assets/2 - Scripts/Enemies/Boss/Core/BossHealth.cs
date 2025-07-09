using UnityEngine;
using System.Collections.Generic;
using Scripts.Core.Interfaces;
using Scripts.Enemies.Boss.Core.Visuals; // --- NEW: Add this using statement ---

namespace Scripts.Enemies.Boss.Core
{
    public class BossHealth : MonoBehaviour, IDamageable
    {
        public enum VulnerabilityType
        {
            AlwaysVulnerable,
            ReducedDamageWhenNotVulnerable,
            InvulnerableWhenNotVulnerable
        }

        [Header("Health & Phases")]
        [SerializeField] private float maxHealth = 5000f;
        [SerializeField] private List<float> phaseHealthThresholds = new List<float> { 0.75f, 0.50f, 0.25f };

        [Header("Vulnerability")]
        [SerializeField] private VulnerabilityType vulnerabilityType = VulnerabilityType.ReducedDamageWhenNotVulnerable;
        [Range(0f, 1f)]
        [SerializeField] private float damageReductionMultiplier = 0.1f;
        
        [Header("Visuals")]
        [Tooltip("The visual controller for the boss, responsible for hit flashes and animations.")]
        [SerializeField] private BossVisualController _visualController;
        
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<int> OnPhaseThresholdReached;
        public event System.Action OnDeath;

        private float _currentHealth;
        private bool _isVulnerable = false;
        private bool _isInvulnerable = false;
        private bool _isDead = false;
        private List<float> _remainingThresholds;
        
        

        private void Awake()
        {
            _currentHealth = maxHealth;
            _remainingThresholds = new List<float>(phaseHealthThresholds);
            _remainingThresholds.Sort((a, b) => b.CompareTo(a)); 
            
            // --- NEW: Find the visual controller ---
            _visualController = GetComponentInChildren<BossVisualController>();
            if (_visualController == null)
            {
                Debug.LogError("BossHealth could not find a BossVisualController!", this);
            }
        }

        public void TakeDamage(float amount)
        {
            if (_isDead || _isInvulnerable)
            {
                return;
            }
            
            //Debug.Log(gameObject.name + " has been damaged!");

            float damageToDeal = amount;

            if (!_isVulnerable)
            {
                switch (vulnerabilityType)
                {
                    case VulnerabilityType.ReducedDamageWhenNotVulnerable:
                        damageToDeal *= damageReductionMultiplier;
                        break;
                    case VulnerabilityType.InvulnerableWhenNotVulnerable:
                        return;
                }
            }
            
            if (damageToDeal <= 0) return;

            // --- THE FIX: Trigger the hit flash ---
            _visualController?.StartHitFlash();
            
            _currentHealth -= damageToDeal;
            _currentHealth = Mathf.Max(_currentHealth, 0);

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            CheckForPhaseTransition();
            
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        
        // --- (Rest of the file is unchanged) ---
        #region Unchanged Methods
        public void SetVulnerability(bool isVulnerable)
        {
            _isVulnerable = isVulnerable;
        }
        
        public void SetInvulnerability(bool isInvulnerable)
        {
            _isInvulnerable = isInvulnerable;
        }

        private void CheckForPhaseTransition()
        {
            if (_remainingThresholds.Count == 0) return;

            float currentHealthPercent = _currentHealth / maxHealth;
            if (currentHealthPercent <= _remainingThresholds[0])
            {
                int phaseIndex = phaseHealthThresholds.Count - _remainingThresholds.Count + 1;
                OnPhaseThresholdReached?.Invoke(phaseIndex);
                _remainingThresholds.RemoveAt(0);
            }
        }
        
        public void SetHealthToPercentage(float percentage)
        {
            _currentHealth = maxHealth * Mathf.Clamp01(percentage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            _remainingThresholds.Clear();
            _remainingThresholds = new List<float>(phaseHealthThresholds);
            _remainingThresholds.Sort((a, b) => b.CompareTo(a)); 
            _remainingThresholds.RemoveAll(t => t >= percentage);
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log(gameObject.name + " has been defeated!");
            OnDeath?.Invoke();
        }
        #endregion
    }
}