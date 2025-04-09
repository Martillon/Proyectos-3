namespace Scripts.Player.Weapons.Interfaces
{
    using UnityEngine;

    /// <summary>
    /// IWeaponUpgrade
    /// 
    /// Defines a modular weapon upgrade that controls how a weapon fires.
    /// Used by WeaponBase to trigger firing logic, damage, effects, etc.
    /// </summary>
    public interface IWeaponUpgrade
    {
        /// <summary>
        /// Whether this upgrade is currently able to fire.
        /// </summary>
        bool CanFire();

        /// <summary>
        /// Executes the firing behavior from a given point and direction.
        /// </summary>
        void Fire(Transform firePoint, Vector2 direction);
    }

    /// <summary>
    /// IDamagingProjectile
    /// 
    /// Interface for projectiles that apply damage when hitting a target.
    /// </summary>
    public interface IDamagingProjectile
    {
        /// <summary>
        /// Sets the damage that this projectile will apply on hit.
        /// </summary>
        void SetDamage(float amount);
    }

    /// <summary>
    /// IWeaponPickup
    /// 
    /// Interface for collectible weapon upgrades in the scene.
    /// </summary>
    public interface IWeaponPickup
    {
        /// <summary>
        /// Returns the weapon upgrade contained in this pickup.
        /// </summary>
        IWeaponUpgrade GetUpgrade();
    }
}