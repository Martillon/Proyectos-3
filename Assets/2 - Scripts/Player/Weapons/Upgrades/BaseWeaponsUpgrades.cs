// --- START OF FILE BaseWeaponUpgrade.cs ---
using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core.Audio;
using Scripts.Player.Weapons.Projectiles; // For PlayerProjectile

namespace Scripts.Player.Weapons.Upgrades
{
    /// <summary>
    /// Abstract base class for all player weapon upgrades.
    /// Handles common projectile spawning, sound playback, and multi-shot/spread logic.
    /// </summary>
    public abstract class BaseWeaponUpgrade : MonoBehaviour, IWeaponUpgrade
    {
        [Header("General Settings")]
        [Tooltip("Base damage inflicted by each projectile from this weapon upgrade.")]
        [SerializeField] protected float damage = 1f;
        [Tooltip("Audio clips to play when firing. If multiple, one is chosen randomly.")]
        [SerializeField] protected Sounds[] fireSounds;
        [Tooltip("AudioSource component for playing weapon sounds. If null, attempts to find one on this GameObject or its parent.")]
        [SerializeField] protected AudioSource audioSource;

        [Header("Projectile Settings")]
        [Tooltip("The prefab for the projectile this weapon fires. Must have a PlayerProjectile component.")]
        [SerializeField] protected GameObject projectilePrefab;
        // Note: Projectile speed is now managed by the PlayerProjectile prefab itself.

        [Header("Multi-Shot Configuration (e.g., for Shotguns)")]
        [Tooltip("Number of projectiles fired simultaneously per shot. Set to 1 for single projectile weapons.")]
        [SerializeField] protected int projectilesPerShot = 1;
        [Tooltip("Total angle (in degrees) over which projectiles will spread if 'Projectiles Per Shot' is greater than 1. Use 0 for no spread.")]
        [SerializeField] protected float spreadAngle = 0f;

        [Header("UI Display")]
        [Tooltip("Icon representing this weapon upgrade, typically shown on the HUD.")]
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        /// <summary>
        /// Timestamp of the last time this weapon was fired. Used for internal cooldown management.
        /// </summary>
        protected float lastFireTimeInternal; // Renamed for clarity vs WeaponBase's semiAutoFireTimer

        protected virtual void Awake()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"WeaponUpgrade '{this.GetType().Name}': Projectile Prefab is not assigned!", this);
            }
            else if (projectilePrefab.GetComponent<PlayerProjectile>() == null)
            {
                Debug.LogError($"WeaponUpgrade '{this.GetType().Name}': Assigned Projectile Prefab '{projectilePrefab.name}' is missing a PlayerProjectile component!", this);
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && transform.parent != null) // Check parent if not on self
                {
                    audioSource = GetComponentInParent<AudioSource>();
                }
                // if (audioSource == null) Debug.LogWarning($"WeaponUpgrade '{this.GetType().Name}': AudioSource not found.", this); // Uncomment for debugging
            }
        }

        /// <summary>
        /// Determines if the weapon upgrade can fire based on its internal state (e.g., specific cooldowns for automatic weapons).
        /// This is called by WeaponBase after its own general checks (like semi-auto cooldown).
        /// </summary>
        public virtual bool CanFire()
        {
            // Default implementation for semi-auto weapons; they don't have an additional internal cooldown beyond WeaponBase's.
            // Automatic/Burst weapons will override this for their specific fire rate logic.
            return true;
        }

        /// <summary>
        /// Core firing logic. Spawns projectiles according to configuration (single, multi-shot, spread).
        /// Called by WeaponBase when a shot is authorized.
        /// </summary>
        /// <param name="firePoint">The transform defining the origin and initial rotation of the projectile(s).</param>
        /// <param name="baseDirection">The primary aiming direction.</param>
        public virtual void Fire(Transform firePoint, Vector2 baseDirection)
        {
            if (projectilePrefab == null || firePoint == null)
            {
                // Debug.LogError($"WeaponUpgrade '{this.GetType().Name}': Cannot fire, ProjectilePrefab or FirePoint is null.", this); // Uncomment for debugging
                return;
            }

            PlayPrimaryFireSound(); // Play sound once for the "shot action"

            for (int i = 0; i < projectilesPerShot; i++)
            {
                Vector2 currentShotDirection = baseDirection;
                if (projectilesPerShot > 1 && spreadAngle > 0)
                {
                    float randomOffsetAngle = (projectilesPerShot == 1) ? 0 : Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                    currentShotDirection = Quaternion.Euler(0, 0, randomOffsetAngle) * baseDirection;
                }
                SpawnConfiguredProjectile(firePoint, currentShotDirection.normalized);
            }

            lastFireTimeInternal = Time.time; // Record the time of this firing action
        }
        
        /// <summary>
        /// Instantiates and initializes a single projectile.
        /// </summary>
        /// <param name="spawnPoint">The transform from which the projectile is spawned.</param>
        /// <param name="direction">The normalized direction for the projectile.</param>
        protected virtual void SpawnConfiguredProjectile(Transform spawnPoint, Vector2 direction)
        {
            // Projectile's rotation will be set by its Initialize method based on direction.
            // We use spawnPoint.position for spawn location.
            GameObject projectileGO = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity); 
            PlayerProjectile projectileScript = projectileGO.GetComponent<PlayerProjectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(direction);
                projectileScript.SetDamage(this.damage); // Use the damage from this weapon upgrade
            }
            // else: Error already logged in Awake if prefab is misconfigured.
        }

        /// <summary>
        /// Plays a fire sound from the configured list.
        /// </summary>
        protected void PlayPrimaryFireSound()
        {
            if (fireSounds != null && fireSounds.Length > 0 && audioSource != null)
            {
                Sounds soundToPlay = fireSounds[Random.Range(0, fireSounds.Length)];
                soundToPlay?.Play(audioSource); // Uses the Play method from your Sounds class
            }
        }

        /// <summary>
        /// Gets the fire cooldown specific to this weapon upgrade.
        /// For semi-auto weapons, this might be minimal as WeaponBase handles the main semi-auto cooldown.
        /// For automatic weapons, this dictates their rate of fire.
        /// </summary>
        /// <returns>The cooldown duration in seconds.</returns>
        public virtual float GetFireCooldown()
        {
            // Default for semi-auto weapons that rely on WeaponBase's semiAutoFireCooldown.
            // Automatic weapons (like Minigun) will override this to return their specific calculated cooldown (1f / fireRate).
            return 0.05f; // A small internal cooldown, mostly as a fallback.
        }
    }
}
// --- END OF FILE BaseWeaponUpgrade.cs ---
