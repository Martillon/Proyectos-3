namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// A basic, single-shot, semi-automatic weapon upgrade.
    /// All behavior is inherited from BaseWeaponUpgrade. Configure its stats (damage, cooldown, etc.)
    /// on the prefab in the Inspector.
    /// For this weapon, ensure:
    /// - Projectiles Per Shot = 1
    ///  - Spread Angle = 0
    /// </summary>
    public class DefaultUpgrade : BaseWeaponUpgrade
    {
        // This class is intentionally empty.
        // It inherits all necessary functionality from BaseWeaponUpgrade.
        // Its purpose is to exist as a distinct component that can be attached
        // to a prefab and configured in the Inspector.
    }
}