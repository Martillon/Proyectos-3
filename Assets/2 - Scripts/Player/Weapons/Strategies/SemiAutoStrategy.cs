using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons.Strategies
{
    [CreateAssetMenu(fileName = "FS_SemiAuto", menuName = "My Game/Player/Firing Strategy/Semi-Automatic")]
    public class SemiAutoStrategy : FiringStrategy
    {
        public override void Execute(Transform firePoint, WeaponStats weaponStats, Vector2 aimDirection, MonoBehaviour owner)
        {
            SpawnProjectiles(firePoint, weaponStats, aimDirection);
        }
    }
}