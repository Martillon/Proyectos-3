using UnityEngine;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Player.Core;
using Scripts.Player.Visuals;
using Scripts.Player.Weapons.Strategies;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// The core component that manages the player's currently equipped weapon.
    /// It reads all weapon data and behavior from the central PlayerStats Scriptable Object.
    /// </summary>
    public class WeaponBase : MonoBehaviour
    {
        [Header("Data Source")]
        [Tooltip("A reference to the PlayerStats Scriptable Object that holds the current weapon state.")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Core References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform aimableArmPivot;
        [SerializeField] private AimDirectionResolver aimResolver;
        [SerializeField] private PlayerVisualController playerVisualController;
        [SerializeField] private AudioSource weaponAudioSource;
        
        private float _lastFireTime;
        
        private void Start()
        {
            if (!playerStats)
            {
                Debug.LogError("WeaponBase: PlayerStats asset is not assigned!", this);
                enabled = false;
                return;
            }
            // On level start, equip whatever weapon is currently stored in the persistent stats asset.
            EquipWeapon(playerStats.currentWeapon);
        }

        private void Update()
        {
            WeaponStats currentWeapon = playerStats.currentWeapon;
            if (!currentWeapon || !currentWeapon.firingStrategy) return;
            
            RotateArmToAim(aimResolver.CurrentDirection);

            bool shootHeld = InputManager.Instance?.Controls.Player.Shoot.IsPressed() ?? false;
            bool shootPressed = InputManager.Instance?.Controls.Player.Shoot.WasPressedThisFrame() ?? false;
            
            bool canFireSemiAuto = shootPressed;
            bool canFireAutomatic = shootHeld && currentWeapon.firingStrategy is AutomaticStrategy;

            if ((canFireSemiAuto || canFireAutomatic) && Time.time >= _lastFireTime + currentWeapon.fireCooldown)
            {
                currentWeapon.firingStrategy.Execute(
                    firePoint,
                    currentWeapon,
                    aimResolver.CurrentDirection,
                    this
                );
                
                PlayFireSound(currentWeapon);
                _lastFireTime = Time.time;
            }
        }

        /// <summary>
        /// Equips a new weapon by updating the central PlayerStats asset.
        /// </summary>
        public void EquipWeapon(WeaponStats newStats)
        {
            if (!newStats)
            {
                Debug.LogWarning("Attempted to equip null WeaponStats. Reverting to default.", this);
                // Fallback to the default weapon defined in the stats asset
                newStats = playerStats.defaultWeapon;
                if (!newStats)
                {
                    Debug.LogError("No default weapon is assigned in PlayerStats!", this);
                    return;
                }
            }

            playerStats.currentWeapon = newStats;
            
            // Update visuals and notify other systems
            playerVisualController.ChangeArmSprite(playerStats.currentWeapon.armSprite);
            PlayerEvents.RaiseWeaponChanged(playerStats.currentWeapon);
            _lastFireTime = 0; // Reset fire timer to allow immediate firing
        }

        /// <summary>
        /// Reverts the currently equipped weapon to the default one defined in PlayerStats.
        /// Called by the health system when the player takes damage.
        /// </summary>
        public void RevertToDefaultWeapon()
        {
            // Only revert if we don't already have the default weapon.
            if (playerStats.currentWeapon != playerStats.defaultWeapon)
            {
                Debug.Log("Weapon downgraded to default on taking damage!");
                EquipWeapon(playerStats.defaultWeapon);
            }
        }
        
        private void RotateArmToAim(Vector2 direction)
        {
            if (aimableArmPivot && direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                aimableArmPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        
        private void PlayFireSound(WeaponStats weapon)
        {
            if (!weaponAudioSource || weapon.fireSounds == null || weapon.fireSounds.Length == 0) return;
            
            Sounds soundToPlay = weapon.fireSounds[Random.Range(0, weapon.fireSounds.Length)];
            soundToPlay.Play(weaponAudioSource);
        }
    }
}