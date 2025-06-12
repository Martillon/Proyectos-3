using UnityEngine;
using Scripts.Player.Weapons.Projectiles;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons.Strategies
{
    public abstract class FiringStrategy : ScriptableObject
    {
        /// <summary>
        /// The core method that executes this firing behavior.
        /// </summary>
        /// <param name="firePoint">The transform where projectiles spawn.</param>
        /// <param name="weaponStats">The stats of the currently equipped weapon.</param>
        /// <param name="aimDirection">The direction the weapon is aiming.</param>
        /// <param name="owner">The MonoBehaviour (usually WeaponBase) that can run coroutines.</param>
        public abstract void Execute(
            Transform firePoint,
            WeaponStats weaponStats,
            Vector2 aimDirection,
            MonoBehaviour owner
        );

        /// <summary>
        /// A shared helper method to spawn projectiles according to the weapon's stats.
        /// </summary>
        protected void SpawnProjectiles(Transform firePoint, WeaponStats stats, Vector2 baseDirection)
        {
            if (stats.projectilePrefab == null) return;

            for (int i = 0; i < stats.projectilesPerShot; i++)
            {
                Vector2 shotDirection = baseDirection;
                if (stats.projectilesPerShot > 1 && stats.spreadAngle > 0)
                {
                    float angleOffset = Random.Range(-stats.spreadAngle / 2f, stats.spreadAngle / 2f);
                    shotDirection = Quaternion.Euler(0, 0, angleOffset) * baseDirection;
                }

                GameObject projGO = Instantiate(stats.projectilePrefab, firePoint.position, Quaternion.identity);
                if (projGO.TryGetComponent<PlayerProjectile>(out var projectile))
                {
                    projectile.Initialize(shotDirection.normalized);
                    projectile.SetDamage(stats.damage);
                }
            }
        }
    }
}
