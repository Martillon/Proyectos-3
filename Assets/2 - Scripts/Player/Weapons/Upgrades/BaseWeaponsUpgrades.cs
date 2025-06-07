using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core.Audio;
using Scripts.Player.Weapons.Projectiles;

namespace Scripts.Player.Weapons.Upgrades
{
    /// <summary>
    /// An abstract base class for all weapon upgrades. Provides shared functionality
    /// for projectile spawning, damage, and multi-shot/spread configurations.
    /// </summary>
    public abstract class BaseWeaponUpgrade : MonoBehaviour, IWeaponUpgrade
    {
        [Header("Core Stats")]
        [Tooltip("Damage inflicted by each projectile fired from this upgrade.")]
        [SerializeField] protected float damage = 10f;
        [Tooltip("The minimum time (in seconds) between consecutive shots.")]
        [SerializeField] protected float fireCooldown = 0.2f;
        
        [Header("Projectile")]
        [Tooltip("The prefab for the projectile. Must have a PlayerProjectile component.")]
        [SerializeField] protected GameObject projectilePrefab;
        
        [Header("Multi-Shot (for Shotgun-like weapons)")]
        [Tooltip("Number of projectiles fired simultaneously. Use 1 for standard weapons.")]
        [SerializeField] protected int projectilesPerShot = 1;
        [Tooltip("Total angle in degrees over which projectiles spread if 'Projectiles Per Shot' > 1.")]
        [SerializeField] protected float spreadAngle = 0f;
        
        [Header("Feedback")]
        [Tooltip("The sprite to display on the player's arm when this upgrade is equipped.")]
        [SerializeField] private Sprite armSprite;
        [Tooltip("Sounds to play on firing. A random one is chosen if multiple are provided.")]
        [SerializeField] protected Sounds[] fireSounds;
        [Tooltip("Icon representing this upgrade for the HUD.")]
        [SerializeField] private Sprite icon;
        
        public Sprite Icon => icon;

        protected virtual void Awake()
        {
            if (projectilePrefab == null || projectilePrefab.GetComponent<PlayerProjectile>() == null)
            {
                Debug.LogError($"WeaponUpgrade '{name}': Projectile Prefab is invalid or missing a PlayerProjectile component.", this);
            }
        }
        
        public virtual bool CanFire()
        {
            // By default, upgrades don't have extra conditions beyond the cooldown managed by WeaponBase.
            // This can be overridden for more complex weapons (e.g., charge weapons).
            return true;
        }

        public virtual void Fire(Transform firePoint, Vector2 baseDirection)
        {
            if (projectilePrefab == null) return;

            for (int i = 0; i < projectilesPerShot; i++)
            {
                Vector2 shotDirection = baseDirection;
                if (projectilesPerShot > 1 && spreadAngle > 0)
                {
                    // Apply a random spread to the shot direction
                    float angleOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                    shotDirection = Quaternion.Euler(0, 0, angleOffset) * baseDirection;
                }
                SpawnProjectile(firePoint, shotDirection);
            }
        }
        
        protected virtual void SpawnProjectile(Transform spawnPoint, Vector2 direction)
        {
            GameObject projGO = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
            
            // The projectile itself will handle its rotation based on the direction.
            if (projGO.TryGetComponent<PlayerProjectile>(out var projectile))
            {
                projectile.Initialize(direction.normalized);
                projectile.SetDamage(this.damage);
            }
        }

        public float GetFireCooldown() => fireCooldown;
        public Sounds[] GetFireSounds() => fireSounds;
        public Sprite GetArmSprite() => armSprite;
    }
}