using UnityEngine;
using Scripts.Player.Weapons.Interfaces;

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// A fully automatic weapon that fires continuously while the trigger is held.
    /// Implements IAutomaticWeapon to be controlled by WeaponBase.
    /// </summary>
    public class MinigunUpgrade : BaseWeaponUpgrade, IAutomaticWeapon
    {
        // This weapon's fire rate is determined by the 'Fire Cooldown' field in BaseWeaponUpgrade.
        // WeaponBase will repeatedly call HandleAutomaticFire as long as the cooldown has passed.

        public void HandleAutomaticFire(Transform firePoint, Vector2 direction)
        {
            // The base Fire() method handles the actual projectile spawning.
            base.Fire(firePoint, direction);
        }
    }
}