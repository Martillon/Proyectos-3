// --- START OF FILE BurstRifleUpgrade.cs ---
using UnityEngine;
using System.Collections; // Required for Coroutines
using Scripts.Player.Weapons.Interfaces; // Required for IBurstWeapon

// Namespace: Scripts.Player.Weapons.Upgrades.Implementations
// Inherits from: Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade
// Implements: Scripts.Player.Weapons.Interfaces.IBurstWeapon

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// Represents a burst-fire weapon upgrade.
    /// Fires a quick succession of projectiles with a single trigger press.
    /// </summary>
    public class BurstRifleUpgrade : BaseWeaponUpgrade, IBurstWeapon
    {
        [Header("Burst Settings")]
        [Tooltip("Number of projectiles fired in a single burst.")]
        [SerializeField] private int bulletsInBurst = 3;
        [Tooltip("Time (in seconds) between each projectile within a single burst.")]
        [SerializeField] private float timeBetweenBurstShots = 0.07f;

        private Coroutine activeBurstCoroutine;

        /// <summary>
        /// Initiates the burst firing sequence. Called by WeaponBase.
        /// </summary>
        public void StartBurst(Transform firePoint, Vector2 direction)
        {
            if (!gameObject.activeInHierarchy || !enabled) return; // Ensure component is active

            if (activeBurstCoroutine != null)
            {
                StopCoroutine(activeBurstCoroutine); // Stop previous burst if somehow still running
            }
            activeBurstCoroutine = StartCoroutine(BurstFireSequenceCoroutine(firePoint, direction));
        }

        private IEnumerator BurstFireSequenceCoroutine(Transform firePoint, Vector2 direction)
        {
            for (int i = 0; i < bulletsInBurst; i++)
            {
                // SpawnProjectile is inherited from BaseWeaponUpgrade.
                // It handles instantiating, initializing, and setting damage on the projectile.
                // The firePoint should already be rotated correctly by WeaponBase before StartBurst is called.
                SpawnConfiguredProjectile(firePoint, direction);
                
                if (i < bulletsInBurst - 1) // Don't wait after the last bullet in the burst
                {
                    yield return new WaitForSeconds(timeBetweenBurstShots);
                }
            }
            activeBurstCoroutine = null; // Mark coroutine as finished
        }

        /// <summary>
        /// Determines if a new burst can be initiated.
        /// Relies on WeaponBase for the cooldown between full bursts (semiAutoFireTimer)
        /// and ensures no burst is currently active.
        /// </summary>
        public override bool CanFire()
        {
            // WeaponBase checks its semiAutoFireTimer.
            // This checks if a burst is already in progress.
            return activeBurstCoroutine == null;
        }

        public override float GetFireCooldown()
        {
            // This is the cooldown *after* a full burst is completed,
            // before another burst can be initiated. WeaponBase.semiAutoFireCooldown handles this.
            // This method could return the total duration of a burst if needed elsewhere,
            // but for CanFire, WeaponBase's timer is key for inter-burst cooldown.
            float burstDuration = (bulletsInBurst -1) * timeBetweenBurstShots; // Approximate duration of firing part
            return burstDuration > 0 ? burstDuration : 0.1f ; // Example: an internal cooldown related to burst itself
        }

        // The Fire() method from BaseWeaponUpgrade is not directly used by WeaponBase for IBurstWeapon.
        // WeaponBase calls StartBurst() instead.
        // However, to fulfill the IWeaponUpgrade contract, we can have it call StartBurst.
        public override void Fire(Transform firePoint, Vector2 direction)
        {
            // This would be called if an IBurstWeapon was somehow treated as a generic IWeaponUpgrade by mistake.
            // Or if you want to allow a single "tap fire" for a burst weapon.
            // For standard operation, WeaponBase directly calls StartBurst.
            // Debug.LogWarning("BurstRifleUpgrade.Fire() called. Intended use is via StartBurst().", this); // Uncomment for debugging
            // if (CanFire()) // Check internal readiness (no active coroutine)
            // {
            //    StartBurst(firePoint, direction);
            // }
        }
    }
}
// --- END OF FILE BurstRifleUpgrade.cs ---
