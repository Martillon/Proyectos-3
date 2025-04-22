namespace Scripts.Core.Interfaces
{
    /// <summary>
    /// IDamageable
    /// 
    /// Interface for objects that can receive damage, such as players, enemies or destructibles.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies a specific amount of damage to the object.
        /// </summary>
        void TakeDamage(float amount);
    }

    /// <summary>
    /// IDamage
    /// 
    /// Interface for objects that deal damage (e.g. projectiles, traps).
    /// Optional, used to retrieve damage info from damage-dealing objects.
    /// </summary>
    public interface IDamage
    {
        /// <summary>
        /// Returns the amount of damage this object inflicts.
        /// </summary>
        float GetDamage();
    }
    
    /// <summary>
    /// Interface for objects that can restore life completely.
    /// Typically used by life pickups or checkpoints.
    /// </summary>
    public interface IHealLife
    {
        void HealLife(int amount);
    }
    
    /// <summary>
    /// Interface for objects that can restore partial protection (armor).
    /// Used by pickups that recover 1 or more hits.
    /// </summary>
    public interface IHealArmor
    {
        void HealArmor(int amount);
    }
}
