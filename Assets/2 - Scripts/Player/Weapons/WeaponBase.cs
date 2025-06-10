using UnityEngine;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Player.Core;
using Scripts.Player.Visuals;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// The core component on the player that manages the currently equipped weapon.
    /// It handles aiming rotation, firing triggers, and swapping weapon upgrades.
    /// </summary>
    public class WeaponBase : MonoBehaviour
    {
        [Header("Core References")]
        [Tooltip("The transform where projectiles will be spawned.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("The transform of the entire aimable arm, which will be rotated.")]
        [SerializeField] private Transform aimableArmPivot;
        [Tooltip("The AimDirectionResolver that provides the current aim direction.")]
        [SerializeField] private AimDirectionResolver aimResolver;
        [Tooltip("The PlayerVisualController to update the arm sprite.")]
        [SerializeField] private PlayerVisualController playerVisualController;
        [Tooltip("The AudioSource used for playing weapon sounds.")]
        [SerializeField] private AudioSource weaponAudioSource;

        [Header("Initial Setup")]
        [Tooltip("The prefab of the weapon upgrade the player starts with.")]
        [SerializeField] private GameObject initialUpgradePrefab;
        [Tooltip("The parent transform for instantiated upgrade GameObjects.")]
        [SerializeField] private Transform upgradeContainer;

        private IWeaponUpgrade _currentUpgrade;
        private GameObject _currentUpgradeInstance;
        private float _lastFireTime;

        private void Awake()
        {
            // --- Validate all essential references ---
            if (firePoint == null) Debug.LogError("WB: FirePoint is not assigned!", this);
            if (aimableArmPivot == null) Debug.LogError("WB: AimableArmPivot is not assigned!", this);
            if (aimResolver == null) Debug.LogError("WB: AimDirectionResolver is not assigned!", this);
            if (playerVisualController == null) Debug.LogError("WB: PlayerVisualController is not assigned!", this);
            if (weaponAudioSource == null) Debug.LogError("WB: WeaponAudioSource is not assigned!", this);
            if (upgradeContainer == null) upgradeContainer = transform;
        }

        private void Start()
        {
            // Equip the starting weapon
            if (initialUpgradePrefab != null)
            {
                EquipUpgradeFromPrefab(initialUpgradePrefab);
            }
        }

        private void Update()
        {
            if (_currentUpgrade == null || aimResolver == null) return;
            
            // Rotate the arm to match the aim direction.
            RotateArmToAim(aimResolver.CurrentDirection);

            // Check for firing input.
            bool shootHeld = InputManager.Instance?.Controls.Player.Shoot.IsPressed() ?? false;
            
            // Handle different weapon firing types.
            if (shootHeld && Time.time >= _lastFireTime + _currentUpgrade.GetFireCooldown())
            {
                // Automatic weapons fire continuously.
                if (_currentUpgrade is IAutomaticWeapon automaticWeapon)
                {
                    FireEquippedWeapon(automaticWeapon.HandleAutomaticFire);
                }
                // Single-shot or burst weapons fire once on press.
                else if (InputManager.Instance.Controls.Player.Shoot.WasPressedThisFrame())
                {
                    if (_currentUpgrade is IBurstWeapon burstWeapon)
                    {
                        FireEquippedWeapon(burstWeapon.StartBurst);
                    }
                    else
                    {
                        FireEquippedWeapon(_currentUpgrade.Fire);
                    }
                }
            }
        }
        
        // A generic delegate-based firing method to reduce code duplication.
        private void FireEquippedWeapon(System.Action<Transform, Vector2> fireAction)
        {
            if (!_currentUpgrade.CanFire()) return;
            
            fireAction?.Invoke(firePoint, aimResolver.CurrentDirection);
            PlayFireSound();
            _lastFireTime = Time.time;
        }
        
        /// <summary>
        /// Equips a new weapon by instantiating its prefab.
        /// </summary>
        public void EquipUpgradeFromPrefab(GameObject upgradePrefab)
        {
            if (upgradePrefab == null)
            {
                Debug.LogWarning("WB: Tried to equip a null upgrade prefab.", this);
                return;
            }

            // Clean up the old upgrade instance
            if (_currentUpgradeInstance != null)
            {
                Destroy(_currentUpgradeInstance);
            }

            // Instantiate the new upgrade and get its interface
            _currentUpgradeInstance = Instantiate(upgradePrefab, upgradeContainer);
            _currentUpgrade = _currentUpgradeInstance.GetComponent<IWeaponUpgrade>();

            if (_currentUpgrade == null)
            {
                Debug.LogError($"WB: Prefab '{upgradePrefab.name}' does not have a component implementing IWeaponUpgrade!", this);
                Destroy(_currentUpgradeInstance);
                _currentUpgradeInstance = null;
                return;
            }
            
            // Update visuals and notify other systems
            playerVisualController.ChangeArmSprite(_currentUpgrade.GetArmSprite());
            PlayerEvents.RaiseWeaponChanged(_currentUpgrade as BaseWeaponUpgrade);
            _lastFireTime = 0; // Reset fire timer
        }
        
        private void RotateArmToAim(Vector2 direction)
        {
            if (aimableArmPivot != null && direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                aimableArmPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        
        private void PlayFireSound()
        {
            if (weaponAudioSource == null) return;
            
            Sounds[] fireSounds = _currentUpgrade.GetFireSounds();
            if (fireSounds != null && fireSounds.Length > 0)
            {
                Sounds soundToPlay = fireSounds[Random.Range(0, fireSounds.Length)];
                soundToPlay.Play(weaponAudioSource); // Use the Play method from the Sounds class
            }
        }
    }
}