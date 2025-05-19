// --- START OF FILE WeaponBase.cs ---
using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core; 
using Scripts.Player.Weapons.Upgrades; 
using Scripts.Player.Core; 
using UnityEngine.InputSystem; 

namespace Scripts.Player.Weapons
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Core References")]
        [Tooltip("The transform from which projectiles are spawned. Should be a child of the object that rotates with aim.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Reference to the AimDirectionResolver to get the current aiming direction.")]
        [SerializeField] private AimDirectionResolver aimResolver;
        [Tooltip("The Transform of the GameObject that visually represents the arm and weapon, " +
                 "and which rotates with aim. FirePoint should be a child of this.")]
        [SerializeField] private Transform aimableArmTransform;

        [Header("Initial Weapon Setup")]
        [SerializeField] private GameObject initialUpgradePrefab;

        [Header("Upgrade Management")]
        [SerializeField] private Transform upgradeContainer;
        private GameObject currentInstantiatedUpgradeGameObject;
        private IWeaponUpgrade currentUpgradeInterface;

        [Header("Firing Control (Semi-Automatic)")]
        [SerializeField] private float semiAutoFireCooldown = 0.2f;
        private float semiAutoFireTimer;

        private bool isShootActionPressed;

        private void Awake()
        {
            //Debug.Log($"WeaponBase ({gameObject.name}): Awake called. Scene: {gameObject.scene.name}", this); // CHIVATO ESCENA

            if (firePoint == null) Debug.LogError("WeaponBase: 'FirePoint' is not assigned!", this);
            if (aimResolver == null)
            {
                aimResolver = GetComponentInParent<AimDirectionResolver>() ?? GetComponent<AimDirectionResolver>();
                if (aimResolver == null) Debug.LogError("WeaponBase: 'AimDirectionResolver' could not be found.", this);
            }
            if (upgradeContainer == null) upgradeContainer = transform;

            if (InputManager.Instance?.Controls?.Player.Shoot != null)
            {
                InputManager.Instance.Controls.Player.Shoot.started += OnShootActionPerformed;
                InputManager.Instance.Controls.Player.Shoot.canceled += OnShootActionPerformed;
                //Debug.Log($"WeaponBase ({gameObject.name}): Subscribed to Shoot action.", this); // CHIVATO
            }
            //else Debug.LogError("WeaponBase: Could not subscribe to Shoot action. InputManager or Controls might be missing.", this);

            if (initialUpgradePrefab != null)
            {
                EquipUpgradeFromPrefab(initialUpgradePrefab);
            }
            else
            {
                PlayerEvents.RaisePlayerWeaponChanged(null);
                //Debug.Log($"WeaponBase ({gameObject.name}): No initial upgrade prefab assigned.", this); // CHIVATO
            }
            
            if (aimableArmTransform == null)
            {
                if (firePoint != null && firePoint.parent != transform) // Asume que firePoint está anidado bajo el brazo
                {
                    aimableArmTransform = firePoint.parent;
                    //Debug.LogWarning($"WeaponBase: 'Aimable Arm Transform' not assigned. Assuming FirePoint's parent: {aimableArmTransform?.name}", this);
                }
                else
                {
                    aimableArmTransform = firePoint; // Fallback: rotar el firePoint mismo si no hay una jerarquía de brazo clara
                    if (aimableArmTransform == null) Debug.LogError("WeaponBase: 'FirePoint' (and thus Aimable Arm Transform) is not assigned!", this);
                    else Debug.LogWarning("WeaponBase: 'Aimable Arm Transform' not assigned. Defaulting to rotating the FirePoint itself.", this);
                }
            }
        }

        private void OnDestroy()
        {
            //Debug.Log($"WeaponBase ({gameObject.name}): OnDestroy called. Scene: {gameObject.scene.name}", this); // CHIVATO ESCENA
            if (InputManager.Instance?.Controls?.Player.Shoot != null)
            {
                InputManager.Instance.Controls.Player.Shoot.started -= OnShootActionPerformed;
                InputManager.Instance.Controls.Player.Shoot.canceled -= OnShootActionPerformed;
                //Debug.Log($"WeaponBase ({gameObject.name}): Unsubscribed from Shoot action.", this); // CHIVATO
            }
            if (currentInstantiatedUpgradeGameObject != null)
            {
                Destroy(currentInstantiatedUpgradeGameObject);
            }
        }

        private void OnShootActionPerformed(InputAction.CallbackContext context)
        {
            isShootActionPressed = context.ReadValueAsButton();
            //Debug.Log($"WeaponBase ({gameObject.name}): OnShootActionPerformed - Phase: {context.phase}, IsPressed: {isShootActionPressed}", this); // CHIVATO
        }

        private void Update()
        {
            if (currentUpgradeInterface == null) { /* Debug.Log($"WeaponBase ({gameObject.name}): Update - currentUpgradeInterface is NULL.", this); */ return; } // CHIVATO (comentado para no spamear)
            if (aimResolver == null) { /* Debug.Log($"WeaponBase ({gameObject.name}): Update - aimResolver is NULL.", this); */ return; } // CHIVATO
            if (firePoint == null) { /* Debug.Log($"WeaponBase ({gameObject.name}): Update - firePoint is NULL.", this); */ return; } // CHIVATO

            if (semiAutoFireTimer > 0)
            {
                semiAutoFireTimer -= Time.deltaTime;
            }

            RotateTransformToAim(aimableArmTransform, aimResolver.CurrentDirection);

            bool singleShotRequestedThisFrame = InputManager.Instance.Controls.Player.Shoot.WasPressedThisFrame();
            if (singleShotRequestedThisFrame) Debug.Log($"WeaponBase ({gameObject.name}): SingleShotRequestedThisFrame = TRUE", this); // CHIVATO

            // Check current action map
            // if (InputManager.Instance != null && !InputManager.Instance.Controls.Player.enabled) {
            //     Debug.LogWarning($"WeaponBase ({gameObject.name}): Player action map is DISABLED in Update!", this); // CHIVATO IMPORTANTE
            // }


            if (currentUpgradeInterface is IAutomaticWeapon automaticWeapon)
            {
                if (isShootActionPressed) // CHIVATO: ¿Llega aquí cuando mantienes pulsado?
                {
                    // Debug.Log($"WeaponBase ({gameObject.name}): Update - Auto - isShootActionPressed: {isShootActionPressed}", this); // Uncomment for spammy log
                    if (currentUpgradeInterface.CanFire())
                    {
                        //Debug.Log($"WeaponBase ({gameObject.name}): Update - Auto - FIRING!", this); // CHIVATO
                        automaticWeapon.HandleAutomaticFire(firePoint, aimResolver.CurrentDirection);
                    }
                    // else Debug.Log($"WeaponBase ({gameObject.name}): Update - Auto - CanFire returned FALSE.", this); // CHIVATO
                }
            }
            else if (currentUpgradeInterface is IBurstWeapon burstWeapon)
            {
                if (singleShotRequestedThisFrame) // CHIVATO: ¿Llega aquí cuando pulsas una vez?
                {
                    // Debug.Log($"WeaponBase ({gameObject.name}): Update - Burst - singleShotRequestedThisFrame: {singleShotRequestedThisFrame}, semiAutoFireTimer: {semiAutoFireTimer}", this); // Uncomment for spammy log
                    if (semiAutoFireTimer <= 0 && currentUpgradeInterface.CanFire())
                    {
                         Debug.Log($"WeaponBase ({gameObject.name}): Update - Burst - FIRING!", this); // CHIVATO
                        burstWeapon.StartBurst(firePoint, aimResolver.CurrentDirection);
                        semiAutoFireTimer = Mathf.Max(semiAutoFireCooldown, currentUpgradeInterface.GetFireCooldown());
                    }
                    // else Debug.Log($"WeaponBase ({gameObject.name}): Update - Burst - Cooldown or CanFire check failed.", this); // CHIVATO
                }
            }
            else // Default to semi-automatic
            {
                if (singleShotRequestedThisFrame) // CHIVATO: ¿Llega aquí cuando pulsas una vez?
                {
                    // Debug.Log($"WeaponBase ({gameObject.name}): Update - Semi - singleShotRequestedThisFrame: {singleShotRequestedThisFrame}, semiAutoFireTimer: {semiAutoFireTimer}", this); // Uncomment for spammy log
                    if (semiAutoFireTimer <= 0 && currentUpgradeInterface.CanFire())
                    {
                        //Debug.Log($"WeaponBase ({gameObject.name}): Update - Semi - FIRING!", this); // CHIVATO
                        currentUpgradeInterface.Fire(firePoint, aimResolver.CurrentDirection);
                        semiAutoFireTimer = Mathf.Max(semiAutoFireCooldown, currentUpgradeInterface.GetFireCooldown());
                    }
                    // else Debug.Log($"WeaponBase ({gameObject.name}): Update - Semi - Cooldown or CanFire check failed.", this); // CHIVATO
                }
            }
            
        }
        
        public void EquipUpgradeFromPrefab(GameObject upgradePrefabToEquip)
        {
            if (upgradePrefabToEquip == null) return;
            IWeaponUpgrade testUpgradeInterface = upgradePrefabToEquip.GetComponentInChildren<IWeaponUpgrade>(true) ?? upgradePrefabToEquip.GetComponent<IWeaponUpgrade>();
            if (testUpgradeInterface == null) { Debug.LogError($"WeaponBase: Prefab '{upgradePrefabToEquip.name}' invalid.", this); return; }
            if (currentInstantiatedUpgradeGameObject != null) Destroy(currentInstantiatedUpgradeGameObject);
            currentInstantiatedUpgradeGameObject = Instantiate(upgradePrefabToEquip, upgradeContainer);
            currentInstantiatedUpgradeGameObject.transform.localPosition = Vector3.zero;
            currentInstantiatedUpgradeGameObject.transform.localRotation = Quaternion.identity;
            currentInstantiatedUpgradeGameObject.name = upgradePrefabToEquip.name + "_Instance";
            IWeaponUpgrade newEquippedUpgrade = currentInstantiatedUpgradeGameObject.GetComponentInChildren<IWeaponUpgrade>(true) ?? currentInstantiatedUpgradeGameObject.GetComponent<IWeaponUpgrade>();
            SetInternalUpgradeState(newEquippedUpgrade);
            //Debug.Log($"WeaponBase ({gameObject.name}): Equipped upgrade from prefab '{upgradePrefabToEquip.name}'.", this); // CHIVATO
        }

        private void SetInternalUpgradeState(IWeaponUpgrade newUpgrade)
        {
            currentUpgradeInterface = newUpgrade;
            if (currentUpgradeInterface is MonoBehaviour newMonoBehaviour && newMonoBehaviour != null) { newMonoBehaviour.enabled = true; if (!newMonoBehaviour.gameObject.activeSelf) newMonoBehaviour.gameObject.SetActive(true); }
            PlayerEvents.RaisePlayerWeaponChanged(currentUpgradeInterface as BaseWeaponUpgrade);
            semiAutoFireTimer = 0; 
            //Debug.Log($"WeaponBase ({gameObject.name}): Internal upgrade state set to '{(newUpgrade as MonoBehaviour)?.name ?? "None"}'.", this); // CHIVATO
        }
        
        private void RotateTransformToAim(Transform objectToRotate, Vector2 direction)
        {
            if (objectToRotate != null && direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                objectToRotate.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        
        public BaseWeaponUpgrade CurrentUpgradeAsBase => currentUpgradeInterface as BaseWeaponUpgrade;
        public IWeaponUpgrade GetCurrentUpgrade() => currentUpgradeInterface;

        private void OnValidate()
        {
            // ... (sin cambios) ...
            if (aimResolver == null) aimResolver = GetComponentInParent<AimDirectionResolver>() ?? GetComponent<AimDirectionResolver>(); if (firePoint == null && transform.childCount > 0) firePoint = transform.GetChild(0); if (upgradeContainer == null) upgradeContainer = transform;
        }
    }
}
// --- END OF FILE WeaponBase.cs ---