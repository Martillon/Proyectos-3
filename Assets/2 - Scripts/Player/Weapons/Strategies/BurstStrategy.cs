using UnityEngine;
using System.Collections;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons.Strategies
{
    [CreateAssetMenu(fileName = "FS_Burst", menuName = "My Game/Player/Firing Strategy/Burst")]
    public class BurstStrategy : FiringStrategy
    {
        [Header("Burst Settings")]
        [Tooltip("Number of shots in a single burst.")]
        [SerializeField] private int shotsPerBurst = 3;
        [Tooltip("Time delay between each shot within the burst.")]
        [SerializeField] private float timeBetweenShots = 0.08f;

        public override void Execute(Transform firePoint, WeaponStats weaponStats, Vector2 aimDirection, MonoBehaviour owner)
        {
            owner.StartCoroutine(BurstSequence(firePoint, weaponStats, aimDirection));
        }

        private IEnumerator BurstSequence(Transform firePoint, WeaponStats stats, Vector2 aimDirection)
        {
            for (int i = 0; i < shotsPerBurst; i++)
            {
                // Use the shared helper method to do the actual spawning
                base.SpawnProjectiles(firePoint, stats, aimDirection);
            
                if (i < shotsPerBurst - 1)
                {
                    yield return new WaitForSeconds(timeBetweenShots);
                }
            }
        }
    }

}