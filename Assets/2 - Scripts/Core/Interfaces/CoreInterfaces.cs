namespace Scripts.Core.Interfaces
{
    /// <summary>
    /// Interface for any object that can receive damage (e.g., players, enemies, destructibles).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies a specified amount of damage to the object.
        /// </summary>
        /// <param name="amount">The amount of damage to inflict.</param>
        void TakeDamage(float amount);
    }

    /// <summary>
    /// Interface for any object that can be instantly killed, bypassing normal damage calculations.
    /// </summary>
    public interface IInstakillable
    {
        /// <summary>
        /// Triggers the instant death sequence for the object.
        /// </summary>
        void ApplyInstakill();
    }

    /// <summary>
    /// Interface for objects that can have their primary health/lives restored.
    /// Typically used by the player.
    /// </summary>
    public interface IHealLife
    {
        /// <summary>
        /// Restores a specified number of life points.
        /// </summary>
        /// <param name="amount">The number of lives to restore.</param>
        void HealLife(int amount);
    }

    /// <summary>
    /// Interface for objects that can have their secondary protection (armor) restored.
    /// Typically used by the player.
    /// </summary>
    public interface IHealArmor
    {
        /// <summary>
        /// Restores a specified amount of armor points.
        /// </summary>
        /// <param name="amount">The amount of armor to restore.</param>
        void HealArmor(int amount);
    }
    
    // NOTE: IDamage was removed as it was not used in the provided scripts.
    // The damage value was consistently held by the damage-dealer (e.g., projectile, hitbox)
    // rather than being retrieved via an interface. If needed, it can be re-added:
    /*
    /// <summary>
    /// Optional interface for objects that deal damage (e.g., projectiles, traps)
    /// to allow other systems to query their damage value.
    /// </summary>
    public interface IDamageSource
    {
        /// <summary>
        /// Gets the amount of damage this object inflicts.
        /// </summary>
        float GetDamageAmount();
    }
    */
}
