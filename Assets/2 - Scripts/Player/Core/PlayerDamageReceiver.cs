using UnityEngine;
using Scripts.Core.Interfaces;

namespace Scripts.Player.Core
{
    public class PlayerDamageReceiver : MonoBehaviour, IDamageable, IInstakillable, IHealLife , IHealArmor
    {
        [Header("Reference")]
        [SerializeField] private PlayerHealthSystem mainHealthSystem;

        private void Awake()
        {
            if (mainHealthSystem == null)
            {
                Transform root = transform.root; 
                if (root != null)
                {
                    mainHealthSystem = root.GetComponentInChildren<PlayerHealthSystem>(true);
                }
            }
            if (mainHealthSystem == null)
            {
                Debug.LogError($"PDR on '{gameObject.name}': PlayerHealthSystem not found.", this);
                enabled = false; 
            }
        }

        public void TakeDamage(float amount)
        {
            mainHealthSystem?.TakeDamage(amount);
        }
        
        public void ApplyInstakill()
        {
            Debug.Log($"PDR on '{gameObject.name}': ApplyInstakill received, forwarding to mainHealthSystem.");
            mainHealthSystem?.ApplyInstakill();
        }
        
        public void HealLife(int amount)
        {
            mainHealthSystem?.HealLife(amount);
        }

        public void HealArmor(int amount)
        {
            mainHealthSystem?.HealArmor(amount);
        }
    }
}
