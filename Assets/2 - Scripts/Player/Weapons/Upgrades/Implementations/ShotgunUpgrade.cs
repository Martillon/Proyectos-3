namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// A shotgun-style weapon that fires multiple projectiles in a spread.
    /// All behavior is inherited from BaseWeaponUpgrade. Configure its stats (damage, cooldown, etc.)
    /// and spread properties on the prefab in the Inspector.
    /// For this weapon, ensure:
    /// - Projectiles Per Shot > 1 (e.g., 5)
    /// - Spread Angle > 0 (e.g., 20)
    /// </summary>
    public class ShotgunUpgrade : BaseWeaponUpgrade
    {
        // This class is intentionally empty.
        // It inherits all necessary functionality from BaseWeaponUpgrade.
    }
}