// --- START OF FILE WeaponInterfaces.cs ---
using UnityEngine;

namespace Scripts.Player.Weapons.Interfaces
{
    /// <summary>
    /// Defines a modular weapon upgrade that controls how a weapon fires.
    /// Used by WeaponBase to trigger firing logic, damage, effects, etc.
    /// </summary>
    public interface IWeaponUpgrade
    {
        /// <summary>
        /// Determines if this upgrade is currently able to fire (e.g., cooldown, ammo).
        /// </summary>
        /// <returns>True if the weapon can fire, false otherwise.</returns>
        bool CanFire();

        /// <summary>
        /// Executes the firing behavior from a given point and direction.
        /// </summary>
        /// <param name="firePoint">The Transform from which projectiles are spawned.</param>
        /// <param name="direction">The base direction of the shot.</param>
        void Fire(Transform firePoint, Vector2 direction);

        /// <summary>
        /// Gets the effective fire cooldown for this weapon upgrade.
        /// This might be used by WeaponBase or internally by the upgrade itself.
        /// </summary>
        /// <returns>The fire cooldown in seconds.</returns>
        float GetFireCooldown();
    }

    /// <summary>
    /// Interface for projectiles that can have their damage value set externally.
    /// </summary>
    public interface IDamagingProjectile
    {
        /// <summary>
        /// Sets the amount of damage this projectile will apply on hit.
        /// </summary>
        /// <param name="amount">The damage amount.</param>
        void SetDamage(float amount);
    }

    /// <summary>
    /// Interface for collectible weapon upgrades found in the game world.
    /// </summary>
    public interface IWeaponPickup
    {
        /// <summary>
        /// Retrieves the weapon upgrade instance contained within this pickup.
        /// </summary>
        /// <returns>The IWeaponUpgrade instance.</returns>
        IWeaponUpgrade GetUpgrade();
    }

    /// <summary>
    /// Interface for weapon upgrades that support continuous automatic fire
    /// while the fire button is held.
    /// </summary>
    public interface IAutomaticWeapon
    {
        /// <summary>
        /// Handles the logic for continuous automatic firing.
        /// Typically called each frame the fire button is held and CanFire() is true.
        /// </summary>
        /// <param name="firePoint">The Transform from which projectiles are spawned.</param>
        /// <param name="direction">The current aiming direction.</param>
        void HandleAutomaticFire(Transform firePoint, Vector2 direction);
    }

    /// <summary>
    /// Interface for weapon upgrades that fire a burst of projectiles
    /// upon a single press of the fire button.
    /// </summary>
    public interface IBurstWeapon
    {
        /// <summary>
        /// Initiates the burst firing sequence.
        /// </summary>
        /// <param name="firePoint">The Transform from which projectiles are spawned.</param>
        /// <param name="direction">The base direction of the burst.</param>
        void StartBurst(Transform firePoint, Vector2 direction);
    }
}
// --- END OF FILE WeaponInterfaces.cs ---