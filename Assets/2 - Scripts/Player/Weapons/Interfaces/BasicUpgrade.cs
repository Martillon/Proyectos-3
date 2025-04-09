using UnityEngine;
using Scripts.Player.Weapons.Interfaces;

namespace Scripts.Player.Weapons.Upgrades
{
    /// <summary>
    /// BasicUpgrade
    /// 
    /// Simple weapon upgrade that fires a single damaging projectile in a straight line.
    /// Designed as a default or fallback upgrade for basic weapon functionality.
    /// </summary>
    public class BasicUpgrade : MonoBehaviour, IWeaponUpgrade
    {
        [Header("Projectile Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 10f;
        [SerializeField] private float damage = 1f;

        public bool CanFire() => true;

        public void Fire(Transform firePoint, Vector2 direction)
        {
            if (bulletPrefab == null)
            {
                Debug.LogWarning("Bullet prefab is not assigned in BasicUpgrade.");
                return;
            }

            GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(Vector3.forward, direction));

            if (bulletInstance.TryGetComponent(out IDamagingProjectile damaging))
            {
                damaging.SetDamage(damage);
            }

            if (bulletInstance.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = direction.normalized * bulletSpeed;
            }
        }
    }
}
