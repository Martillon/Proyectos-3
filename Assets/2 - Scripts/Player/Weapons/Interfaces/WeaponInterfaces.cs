using UnityEngine;
using Scripts.Core.Audio;

namespace Scripts.Player.Weapons.Interfaces
{
    /// <summary>
    /// Base interface for any equippable weapon module. Defines the core contract
    /// for firing, cooldowns, and providing visual/audio feedback.
    /// </summary>
    public interface IWeaponUpgrade
    {
        /// <summary>
        /// Determines if the weapon can currently fire, based on its internal state (e.g., cooldowns, ammo).
        /// </summary>
        bool CanFire();

        /// <summary>
        /// Executes a single firing action. For non-automatic weapons.
        /// </summary>
        /// <param name="firePoint">The transform where projectiles should spawn.</param>
        /// <param name="direction">The calculated direction to fire.</param>
        void Fire(Transform firePoint, Vector2 direction);

        /// <summary>
        /// Gets the minimum time between shots for this specific upgrade.
        /// </summary>
        float GetFireCooldown();

        /// <summary>
        /// Gets the sound(s) to play when this weapon fires.
        /// </summary>
        Sounds[] GetFireSounds();

        /// <summary>
        /// Gets the sprite that should be displayed on the player's arm for this weapon.
        /// </summary>
        Sprite GetArmSprite();
    }
    
    /// <summary>
    /// An extension interface for weapon upgrades that support continuous fire when the trigger is held.
    /// </summary>
    public interface IAutomaticWeapon
    {
        /// <summary>
        /// Handles the continuous firing logic. Called every frame the fire button is held.
        /// </summary>
        void HandleAutomaticFire(Transform firePoint, Vector2 direction);
    }
    
    /// <summary>
    /// An extension interface for weapon upgrades that fire a multi-shot burst with a single trigger pull.
    /// </summary>
    public interface IBurstWeapon
    {
        /// <summary>
        /// Initiates the burst fire sequence.
        /// </summary>
        void StartBurst(Transform firePoint, Vector2 direction);
    }

    /// <summary>
    /// Interface for any projectile that can have its damage value configured externally after spawning.
    /// </summary>
    public interface IDamagingProjectile
    {
        /// <summary>
        /// Sets the damage this projectile will deal on impact.
        /// </summary>
        /// <param name="damage">The amount of damage.</param>
        void SetDamage(float damage);
    }
    
    // NOTE: IWeaponPickup was removed as it was not used. The UpgradePickup script
    // directly contained the prefab and passed it to WeaponBase, which is a simpler and
    // more direct approach than using an interface for this purpose.
}