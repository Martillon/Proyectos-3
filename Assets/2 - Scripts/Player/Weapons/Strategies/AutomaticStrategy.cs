using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons.Strategies
{
    [CreateAssetMenu(fileName = "FS_Automatic", menuName = "My Game/Player/Firing Strategy/Automatic")]
    public class AutomaticStrategy : FiringStrategy
    {
        public override void Execute(Transform firePoint, WeaponStats weaponStats, Vector2 aimDirection, MonoBehaviour owner)
        {
            SpawnProjectiles(firePoint, weaponStats, aimDirection);
        }
    }
}