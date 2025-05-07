// --- START OF FILE DefaultUpgrade.cs ---
using UnityEngine;
// Namespace: Scripts.Player.Weapons.Upgrades.Implementations
// Inherits from: Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// Represents a basic, single-shot weapon upgrade (e.g., a pistol).
    /// Relies on WeaponBase for semi-automatic cooldown and BaseWeaponUpgrade for firing logic.
    /// </summary>
    public class DefaultUpgrade : BaseWeaponUpgrade
    {
        // This weapon type is considered semi-automatic by default in WeaponBase.
        // All core firing logic (single projectile, no special behavior) is handled by BaseWeaponUpgrade.

        // Configuration for this weapon (damage, projectilePrefab, fireSounds, icon, etc.)
        // should be done in the Inspector on the Prefab that has this component.
        // For a pistol:
        // - projectilesPerShot = 1
        // - spreadAngle = 0

        /// <summary>
        /// For a simple semi-automatic weapon, CanFire primarily relies on WeaponBase's semiAutoFireTimer.
        /// This method can be overridden if the specific upgrade has additional conditions (e.g., ammo).
        /// </summary>
        public override bool CanFire()
        {
            // Default semi-auto weapons usually don't have an internal cooldown separate from WeaponBase's timer.
            // If they did, it would be checked here: Time.time >= lastFireTimeInternal + GetFireCooldown()
            return true; // WeaponBase will check its own semiAutoFireTimer.
        }

        /// <summary>
        /// The actual cooldown rate for this specific upgrade if it were, for example, automatic.
        /// For semi-auto, WeaponBase.semiAutoFireCooldown is dominant.
        /// </summary>
        public override float GetFireCooldown()
        {
            // This value can be used by WeaponBase to determine the effective cooldown
            // (e.g., Mathf.Max(WeaponBase.semiAutoFireCooldown, this.GetFireCooldown())).
            // For a standard pistol, a small value or 0 is fine if WeaponBase's cooldown is sufficient.
            return 0.1f; // Example: a very small internal technical cooldown.
        }

        // The Fire() method is inherited from BaseWeaponUpgrade and will correctly fire
        // a single projectile if projectilesPerShot is 1.
    }
}
// --- END OF FILE DefaultUpgrade.cs ---