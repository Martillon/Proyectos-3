// --- START OF FILE WeaponBase.cs ---
using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core; // For InputManager
using Scripts.Player.Weapons.Upgrades; // For BaseWeaponUpgrade
using Scripts.Player.Core; // For PlayerEvents
using UnityEngine.InputSystem; // For InputAction Callbacks

namespace Scripts.Player.Weapons
{
    /// <summary>
    /// Manages the player's active weapon upgrade and handles firing input.
    /// It instantiates upgrade prefabs and delegates firing mechanics to the equipped IWeaponUpgrade.
    /// Differentiates between semi-automatic, burst, and fully automatic firing modes.
    /// </summary>
    public class WeaponBase : MonoBehaviour
    {
        [Header("Core References")]
        [Tooltip("The transform from which projectiles are spawned. Its rotation is updated by AimDirectionResolver.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Reference to the AimDirectionResolver to get the current aiming direction.")]
        [SerializeField] private AimDirectionResolver aimResolver;

        [Header("Initial Weapon Setup")]
        [Tooltip("The PREFAB for the starting weapon upgrade. Must contain a component implementing IWeaponUpgrade.")]
        [SerializeField] private GameObject initialUpgradePrefab; // Changed from MonoBehaviour to GameObject

        [Header("Upgrade Management")]
        [Tooltip("Transform under which instantiated weapon upgrade GameObjects will be parented. If null, defaults to this GameObject's transform.")]
        [SerializeField] private Transform upgradeContainer;
        private GameObject currentInstantiatedUpgradeGameObject; // Tracks the GO of the currently active upgrade
        private IWeaponUpgrade currentUpgradeInterface; // The interface reference to the active upgrade component

        [Header("Firing Control (Semi-Automatic)")]
        [Tooltip("Minimum time (in seconds) between shots for semi-automatic weapons (e.g., pistol, shotgun).")]
        [SerializeField] private float semiAutoFireCooldown = 0.2f;
        private float semiAutoFireTimer;

        private bool isShootActionPressed; // True if the shoot input action is currently held down

        private void Awake()
        {
            // Validate essential references
            if (firePoint == null) Debug.LogError("WeaponBase: 'FirePoint' is not assigned!", this);
            if (aimResolver == null)
            {
                aimResolver = GetComponentInParent<AimDirectionResolver>() ?? GetComponent<AimDirectionResolver>();
                if (aimResolver == null) Debug.LogError("WeaponBase: 'AimDirectionResolver' could not be found. Weapon aiming will fail.", this);
            }
            if (upgradeContainer == null)
            {
                upgradeContainer = transform; // Default to this object's transform if not specified
                // Debug.LogWarning("WeaponBase: 'Upgrade Container' not assigned. Defaulting to this transform for instantiating upgrades.", this); // Uncomment for debugging
            }

            // Subscribe to Input System events for the Shoot action
            if (InputManager.Instance?.Controls?.Player.Shoot != null)
            {
                InputManager.Instance.Controls.Player.Shoot.started += OnShootActionPerformed;
                InputManager.Instance.Controls.Player.Shoot.canceled += OnShootActionPerformed;
            }
            // else Debug.LogError("WeaponBase: Could not subscribe to Shoot action. InputManager or its Controls might be missing.", this); // Uncomment for debugging

            // Set up the initial weapon upgrade from prefab
            if (initialUpgradePrefab != null)
            {
                EquipUpgradeFromPrefab(initialUpgradePrefab);
            }
            else
            {
                // No initial weapon, notify HUD (or other systems)
                PlayerEvents.RaisePlayerWeaponChanged(null);
                // Debug.Log("WeaponBase: No initial upgrade prefab assigned.", this); // Uncomment for debugging
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from Input System events
            if (InputManager.Instance?.Controls?.Player.Shoot != null)
            {
                InputManager.Instance.Controls.Player.Shoot.started -= OnShootActionPerformed;
                InputManager.Instance.Controls.Player.Shoot.canceled -= OnShootActionPerformed;
            }
            // Clean up any instantiated upgrade GameObject
            if (currentInstantiatedUpgradeGameObject != null)
            {
                Destroy(currentInstantiatedUpgradeGameObject);
            }
        }

        private void OnShootActionPerformed(InputAction.CallbackContext context)
        {
            isShootActionPressed = context.ReadValueAsButton();
        }

        private void Update()
        {
            if (currentUpgradeInterface == null || aimResolver == null || firePoint == null) return;

            if (semiAutoFireTimer > 0)
            {
                semiAutoFireTimer -= Time.deltaTime;
            }

            RotateFirePointToAim(aimResolver.CurrentDirection);

            bool singleShotRequestedThisFrame = InputManager.Instance.Controls.Player.Shoot.WasPressedThisFrame();

            if (currentUpgradeInterface is IAutomaticWeapon automaticWeapon)
            {
                if (isShootActionPressed && currentUpgradeInterface.CanFire())
                {
                    automaticWeapon.HandleAutomaticFire(firePoint, aimResolver.CurrentDirection);
                }
            }
            else if (currentUpgradeInterface is IBurstWeapon burstWeapon)
            {
                if (singleShotRequestedThisFrame && semiAutoFireTimer <= 0 && currentUpgradeInterface.CanFire())
                {
                    burstWeapon.StartBurst(firePoint, aimResolver.CurrentDirection);
                    semiAutoFireTimer = Mathf.Max(semiAutoFireCooldown, currentUpgradeInterface.GetFireCooldown());
                }
            }
            else // Default to semi-automatic
            {
                if (singleShotRequestedThisFrame && semiAutoFireTimer <= 0 && currentUpgradeInterface.CanFire())
                {
                    currentUpgradeInterface.Fire(firePoint, aimResolver.CurrentDirection);
                    semiAutoFireTimer = Mathf.Max(semiAutoFireCooldown, currentUpgradeInterface.GetFireCooldown());
                }
            }
        }

        private void RotateFirePointToAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                firePoint.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// Instantiates an upgrade from a prefab, parents it under the 'Upgrade Container',
        /// and sets it as the current weapon upgrade. Destroys any previously active upgrade GameObject.
        /// </summary>
        /// <param name="upgradePrefab">The GameObject prefab that contains a component implementing IWeaponUpgrade.</param>
        public void EquipUpgradeFromPrefab(GameObject upgradePrefabToEquip)
        {
            if (upgradePrefabToEquip == null)
            {
                // Debug.LogError("WeaponBase: EquipUpgradeFromPrefab called with a null prefab.", this); // Uncomment for debugging
                return;
            }

            // Check if the prefab contains a valid IWeaponUpgrade component before proceeding
            IWeaponUpgrade testUpgradeInterface = upgradePrefabToEquip.GetComponentInChildren<IWeaponUpgrade>(true) ?? upgradePrefabToEquip.GetComponent<IWeaponUpgrade>();
            if (testUpgradeInterface == null)
            {
                Debug.LogError($"WeaponBase: The prefab '{upgradePrefabToEquip.name}' does not contain a usable IWeaponUpgrade component.", this);
                return;
            }

            // Destroy the previously instantiated upgrade's GameObject, if one exists
            if (currentInstantiatedUpgradeGameObject != null)
            {
                // Debug.Log($"WeaponBase: Destroying old upgrade object: {currentInstantiatedUpgradeGameObject.name}", this); // Uncomment for debugging
                Destroy(currentInstantiatedUpgradeGameObject);
            }

            // Instantiate the new upgrade prefab under the designated container
            // Debug.Log($"WeaponBase: Instantiating new upgrade prefab: {upgradePrefabToEquip.name} under {upgradeContainer.name}", this); // Uncomment for debugging
            currentInstantiatedUpgradeGameObject = Instantiate(upgradePrefabToEquip, upgradeContainer);
            currentInstantiatedUpgradeGameObject.transform.localPosition = Vector3.zero;
            currentInstantiatedUpgradeGameObject.transform.localRotation = Quaternion.identity;
            // It's good practice to ensure the instantiated upgrade object has a clear name for debugging
            currentInstantiatedUpgradeGameObject.name = upgradePrefabToEquip.name + "_Instance";


            // Get the IWeaponUpgrade component from the newly instantiated GameObject
            IWeaponUpgrade newEquippedUpgrade = currentInstantiatedUpgradeGameObject.GetComponentInChildren<IWeaponUpgrade>(true) ?? currentInstantiatedUpgradeGameObject.GetComponent<IWeaponUpgrade>();
            
            SetInternalUpgradeState(newEquippedUpgrade);
        }

        /// <summary>
        /// Internal method to set the current upgrade interface and manage its state.
        /// This is called by EquipUpgradeFromPrefab or if an upgrade is set directly.
        /// </summary>
        private void SetInternalUpgradeState(IWeaponUpgrade newUpgrade)
        {
            // Note: The old currentUpgradeInterface (if it was a MonoBehaviour) would be on currentInstantiatedUpgradeGameObject,
            // which is destroyed by EquipUpgradeFromPrefab. So, no need to manually disable the old MonoBehaviour here.
            
            currentUpgradeInterface = newUpgrade;

            if (currentUpgradeInterface is MonoBehaviour newMonoBehaviour && newMonoBehaviour != null)
            {
                newMonoBehaviour.enabled = true; // Ensure the component is enabled
                if (!newMonoBehaviour.gameObject.activeSelf)
                {
                    newMonoBehaviour.gameObject.SetActive(true); // Ensure its GameObject is active
                }
                // Debug.Log($"WeaponBase: SetInternalUpgradeState - Active upgrade is '{newMonoBehaviour.name}', enabled: {newMonoBehaviour.enabled}, GO active: {newMonoBehaviour.gameObject.activeSelf}", this); // Uncomment for debugging
            }
            // else Debug.Log($"WeaponBase: SetInternalUpgradeState - Active upgrade is of type '{currentUpgradeInterface?.GetType().Name}' (not a MonoBehaviour or null).", this); // Uncomment for debugging


            PlayerEvents.RaisePlayerWeaponChanged(currentUpgradeInterface as BaseWeaponUpgrade);
            semiAutoFireTimer = 0; // Reset semi-auto cooldown
        }
        
        public BaseWeaponUpgrade CurrentUpgradeAsBase => currentUpgradeInterface as BaseWeaponUpgrade;
        public IWeaponUpgrade GetCurrentUpgrade() => currentUpgradeInterface;

        private void OnValidate()
        {
            if (aimResolver == null) aimResolver = GetComponentInParent<AimDirectionResolver>() ?? GetComponent<AimDirectionResolver>();
            if (firePoint == null && transform.childCount > 0) firePoint = transform.GetChild(0);
            if (upgradeContainer == null) upgradeContainer = transform;
        }
    }
}
// --- END OF FILE WeaponBase.cs ---