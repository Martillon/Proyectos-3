using UnityEngine;
using System.Collections;
using Scripts.Player.Weapons.Interfaces;

namespace Scripts.Player.Weapons.Upgrades.Implementations
{
    /// <summary>
    /// A burst-fire weapon that fires a quick series of shots with one trigger pull.
    /// Implements IBurstWeapon to be controlled by WeaponBase.
    /// </summary>
    public class BurstRifleUpgrade : BaseWeaponUpgrade, IBurstWeapon
    {
        [Header("Burst Settings")]
        [Tooltip("Number of projectiles to fire in a single burst.")]
        [SerializeField] private int shotsPerBurst = 3;
        [Tooltip("Time delay between each shot within the burst.")]
        [SerializeField] private float timeBetweenBurstShots = 0.08f;
        
        private Coroutine _burstCoroutine;

        public void StartBurst(Transform firePoint, Vector2 direction)
        {
            if (_burstCoroutine != null) StopCoroutine(_burstCoroutine);
            _burstCoroutine = StartCoroutine(BurstSequence(firePoint, direction));
        }

        private IEnumerator BurstSequence(Transform firePoint, Vector2 direction)
        {
            for (int i = 0; i < shotsPerBurst; i++)
            {
                // The base Fire() method handles spawning with spread, etc.
                base.Fire(firePoint, direction);
                
                if (i < shotsPerBurst - 1)
                {
                    yield return new WaitForSeconds(timeBetweenBurstShots);
                }
            }
            _burstCoroutine = null;
        }
        
        public override bool CanFire()
        {
            // A new burst can only start if one is not already in progress.
            return _burstCoroutine == null;
        }
    }
}
