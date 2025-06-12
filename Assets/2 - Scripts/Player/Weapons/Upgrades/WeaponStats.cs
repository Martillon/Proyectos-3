using UnityEngine;
using Scripts.Core.Audio;
using Scripts.Player.Weapons.Strategies;

namespace Scripts.Player.Weapons.Upgrades
{
    [CreateAssetMenu(fileName = "NewWeaponStats", menuName = "My Game/Player/Weapon Stats")]
    public class WeaponStats : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "New Weapon";

        [Header("Behavior")]
        [Tooltip("The firing behavior logic this weapon will use (e.g., Semi-Auto, Burst, Automatic).")]
        public FiringStrategy firingStrategy;

        [Header("Firing Mechanics")]
        [Tooltip("Damage inflicted by each individual projectile.")]
        public float damage = 10f;
        [Tooltip("The minimum time (in seconds) between firing actions. For burst/auto, this is the cooldown *between* bursts/trigger pulls.")]
        public float fireCooldown = 0.5f;
    
        [Header("Projectile")]
        [Tooltip("The prefab for the projectile this weapon fires. Must have a PlayerProjectile component.")]
        public GameObject projectilePrefab;

        [Header("Multi-Shot & Spread (for Shotgun-like effects)")]
        [Tooltip("Number of projectiles fired at once. Use 1 for a standard weapon.")]
        public int projectilesPerShot = 1;
        [Tooltip("Total angle in degrees over which projectiles spread if 'Projectiles Per Shot' > 1.")]
        public float spreadAngle = 0f;

        [Header("Feedback")]
        [Tooltip("The sprite for the player's arm when this weapon is equipped.")]
        public Sprite armSprite;
        [Tooltip("The icon for this weapon to be displayed on the HUD.")]
        public Sprite hudIcon;
        [Tooltip("Sounds to play when firing. A random one is chosen if multiple are provided.")]
        public Sounds[] fireSounds;
    }
}