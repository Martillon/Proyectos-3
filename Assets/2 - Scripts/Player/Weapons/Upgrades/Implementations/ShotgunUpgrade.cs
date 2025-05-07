// --- START OF FILE ShotgunUpgrade.cs ---
using UnityEngine;
// Namespace: Scripts.Player.Weapons.Upgrades.Implementations
// Inherits from: Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// Represents a shotgun-style weapon upgrade.
    /// Fires multiple projectiles simultaneously with a defined spread angle.
    /// Relies on WeaponBase for semi-automatic cooldown.
    /// </summary>
    public class ShotgunUpgrade : BaseWeaponUpgrade
    {
        // This weapon type is considered semi-automatic by default in WeaponBase.
        // The multi-shot and spread logic is handled by BaseWeaponUpgrade.Fire().

        // Configuration for this weapon (damage, projectilePrefab, fireSounds, icon, etc.)
        // should be done in the Inspector on the Prefab that has this component.
        // For a shotgun:
        // - projectilesPerShot = (e.g., 5)
        // - spreadAngle = (e.g., 15 to 30 degrees)

        public override bool CanFire()
        {
            return true; // Relies on WeaponBase's semiAutoFireTimer.
        }

        public override float GetFireCooldown()
        {
            return 0.1f; // Example: a small internal technical cooldown.
            // WeaponBase.semiAutoFireCooldown will likely be the effective rate.
        }

        // The Fire() method is inherited from BaseWeaponUpgrade.
        // It will use 'projectilesPerShot' and 'spreadAngle' (configured in Inspector)
        // to fire multiple projectiles.
    }
}
// --- END OF FILE ShotgunUpgrade.cs ---
