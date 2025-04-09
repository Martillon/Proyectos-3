using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// WeaponBase
    /// 
    /// Core weapon script responsible for handling fire input, cooldown, and upgrade delegation.
    /// Does not define bullet behavior directly. Instead, it delegates firing logic to the active weapon upgrade.
    /// </summary>
    public class WeaponBase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform firePoint;

        [Header("Upgrade Settings")]
        [SerializeField] private MonoBehaviour initialUpgrade;
        private IWeaponUpgrade currentUpgrade;

        [Header("Firing Control")]
        [SerializeField] private float fireCooldown = 0.2f;
        private float fireTimer;

        private void Awake()
        {
            // Attempt to cast initial upgrade
            if (initialUpgrade is IWeaponUpgrade upgrade)
            {
                currentUpgrade = upgrade;
            }
            else
            {
                Debug.LogWarning("Initial upgrade must implement IWeaponUpgrade.");
            }
        }

        private void Update()
        {
            fireTimer -= Time.deltaTime;

            if (InputManager.Instance.Controls.Player.Shoot.IsPressed() && currentUpgrade != null && fireTimer <= 0f && currentUpgrade.CanFire())
            {
                Vector2 direction = transform.right; // Default firing direction (can be changed later)
                currentUpgrade.Fire(firePoint, direction);
                fireTimer = fireCooldown;
            }
        }

        /// <summary>
        /// Swaps the current upgrade with a new one.
        /// </summary>
        public void SetUpgrade(IWeaponUpgrade newUpgrade)
        {
            currentUpgrade = newUpgrade;
        }

        /// <summary>
        /// Returns the currently assigned weapon upgrade.
        /// </summary>
        public IWeaponUpgrade GetCurrentUpgrade() => currentUpgrade;
    }
}
