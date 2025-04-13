using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// DefaultUpgrade
    /// 
    /// Basic pistol-like weapon upgrade using raycast firing.
    /// No custom behavior, uses all logic from BaseWeaponUpgrade.
    /// Configure damage, firing rate, and effects in the inspector.
    /// </summary>
    public class DefaultUpgrade : BaseWeaponUpgrade
    {
        // No additional logic required for default behavior.
        // All functionality is inherited from BaseWeaponUpgrade.
    }
}
