// --- START OF FILE MinigunUpgrade.cs ---
using UnityEngine;
using Scripts.Player.Weapons.Interfaces; // Required for IAutomaticWeapon

// Namespace: Scripts.Player.Weapons.Upgrades.Implementations
// Inherits from: Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade
// Implements: Scripts.Player.Weapons.Interfaces.IAutomaticWeapon

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// Represents a minigun-style weapon upgrade.
    /// Fires projectiles continuously at a defined rate while the fire button is held.
    /// </summary>
    public class MinigunUpgrade : BaseWeaponUpgrade, IAutomaticWeapon
    {
        [Header("Mini gun Specific Settings")]
        [Tooltip("Rate of fire in projectiles per second.")]
        [SerializeField] private float projectilesPerSecond = 10f;
        
        private float actualFireCooldown; // Calculated from projectilesPerSecond

        protected override void Awake()
        {
            base.Awake(); // Call BaseWeaponUpgrade's Awake
            CalculateFireCooldown();
        }

        private void OnValidate()
        {
            // Recalculate in editor if projectilesPerSecond changes
            if (Application.isPlaying) CalculateFireCooldown();
        }

        private void CalculateFireCooldown()
        {
            if (projectilesPerSecond <= 0)
            {
                // Debug.LogError("MinigunUpgrade: 'Projectiles Per Second' must be a positive value. Defaulting to 1.", this); // Uncomment for debugging
                projectilesPerSecond = 1f; // Prevent division by zero or negative cooldown
            }
            actualFireCooldown = 1f / projectilesPerSecond;
        }

        /// <summary>
        /// Handles the continuous firing logic for the minigun. Called by WeaponBase.
        /// </summary>
        public void HandleAutomaticFire(Transform firePoint, Vector2 direction)
        {
            // The CanFire() method (overridden below) ensures this is only called when the minigun's internal cooldown has passed.
            // WeaponBase ensures the shoot button is held and calls currentUpgrade.CanFire().
            
            // SpawnProjectile is inherited from BaseWeaponUpgrade.
            // The firePoint should already be rotated by WeaponBase before this is called.
            SpawnConfiguredProjectile(firePoint, direction);
            
            LastFireTimeInternal = Time.time; // Update the internal last fire time for this upgrade's own cooldown
        }

        /// <summary>
        /// Determines if the minigun can fire based on its specific fire rate.
        /// </summary>
        public override bool CanFire()
        {
            return Time.time >= LastFireTimeInternal + actualFireCooldown;
        }

        public override float GetFireCooldown()
        {
            return actualFireCooldown; // Returns the specific cooldown for this minigun's rate of fire.
        }

        // The Fire() method from BaseWeaponUpgrade is not directly used by WeaponBase for IAutomaticWeapon.
        // WeaponBase calls HandleAutomaticFire() instead.
        public override void Fire(Transform firePoint, Vector2 direction)
        {
            // This would be called if an IAutomaticWeapon was somehow treated as a generic IWeaponUpgrade.
            // For standard operation, WeaponBase directly calls HandleAutomaticFire.
            // if (CanFire()) // Check internal readiness
            // {
            //    HandleAutomaticFire(firePoint, direction);
            // }
        }
    }
}
// --- END OF FILE MinigunUpgrade.cs ---
