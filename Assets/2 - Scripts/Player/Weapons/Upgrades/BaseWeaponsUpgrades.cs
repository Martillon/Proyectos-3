using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core.Interfaces;
using Scripts.Core.Audio;

namespace Scripts.Player.Weapons.Upgrades
{
    /// <summary>
    /// BaseWeaponUpgrade
    /// 
    /// Abstract class for modular weapon upgrades.
    /// Supports both Raycast-based and Projectile-based firing modes.
    /// Encapsulates shared logic for damage, direction, effects, audio, and firing mode configuration.
    /// </summary>
    public abstract class BaseWeaponUpgrade : MonoBehaviour, IWeaponUpgrade
    {
        protected enum FiringMode
        {
            Raycast,
            Projectile
        }

        [Header("General Settings")]
        [SerializeField] protected FiringMode firingMode = FiringMode.Raycast;
        [SerializeField] protected float damage = 1f;
        [SerializeField] protected Sounds[] fireSound;
        [SerializeField] protected AudioSource audioSource;

        [Header("Raycast Settings")]
        [SerializeField] protected float maxDistance = 20f;
        [SerializeField] protected LayerMask hitMask;
        [SerializeField] protected GameObject hitVFX;

        [Header("Projectile Settings")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 10f;
        
        [Header("UI")]
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        public virtual bool CanFire() => true;

        public virtual void Fire(Transform firePoint, Vector2 direction)
        {
            if (fireSound.Length > 0)
            {
                PlayAudio(fireSound[Random.Range(0, fireSound.Length)]);
            }

            switch (firingMode)
            {
                case FiringMode.Raycast:
                    FireRaycast(firePoint, direction);
                    break;
                case FiringMode.Projectile:
                    FireProjectile(firePoint, direction);
                    break;
            }
        }

        protected virtual void FireRaycast(Transform firePoint, Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, direction.normalized, maxDistance, hitMask);

            if (hit.collider)
            {
                if (hit.collider.TryGetComponent(out IDamageable target))
                {
                    target.TakeDamage(damage);
                }

                if (hitVFX)
                {
                    Instantiate(hitVFX, hit.point, Quaternion.identity);
                }
            }
        }

        protected virtual void FireProjectile(Transform firePoint, Vector2 direction)
        {
            if (projectilePrefab == null) return;

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            if (bullet.TryGetComponent(out IDamagingProjectile damaging))
            {
                damaging.SetDamage(damage);
            }

            if (bullet.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = direction.normalized * projectileSpeed;
            }
        }

        protected virtual void PlayAudio(Sounds sound)
        {
            if (sound == null || sound.clip == null || audioSource == null)
                return;

            audioSource.clip = sound.clip;
            audioSource.loop = sound.loop;
            audioSource.pitch = sound.randomPitch ? Random.Range(0.95f, 1.05f) : sound.pitch;
            audioSource.volume = sound.randomVolume ? Random.Range(0.9f, 1.1f) : sound.volume;
            audioSource.Play();
        }
    }
}
