using UnityEngine;
using Scripts.Player.Weapons.Interfaces;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Player.Core;
using Scripts.Player.Weapons.Upgrades;
using UnityEngine.InputSystem;

namespace Scripts.Player.Weapons
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private AimDirectionResolver aimResolver;
        [SerializeField] private Transform aimableArmTransform;

        [Header("Initial Weapon Setup")]
        [SerializeField] private GameObject initialUpgradePrefab;

        [Header("Upgrade Management")]
        [SerializeField] private Transform upgradeContainer;
        [SerializeField] private GameObject currentInstantiatedUpgradeGameObject; 
        private IWeaponUpgrade currentUpgradeInterface;

        // NUEVO CAMPO: Layer para los Pickups
        [Header("External Object Layers (Optional)")]
        [Tooltip("The layer on which weapon upgrade pickups are expected to be. (For reference or future systems)")]
        [SerializeField] private LayerMask pickupObjectLayer; // Puedes usar LayerMask si quieres seleccionar múltiples, o int si es solo una.
                                                            // Usaré LayerMask por consistencia con cómo se suelen manejar las capas en el Inspector.
                                                            // Si solo es UNA capa, un int y LayerMask.NameToLayer sería más conciso en código,
                                                            // pero LayerMask es más amigable en el Inspector.

        [Header("Firing Control (Semi-Automatic)")]
        [SerializeField] private float semiAutoFireCooldown = 0.2f;
        private float semiAutoFireTimer;

        private bool isShootActionPressed;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>(); if(!audioSource) Debug.LogWarning("WB: AudioSource not found on WeaponBase, sound playback will not work.", this);
            if (firePoint == null) Debug.LogError($"WeaponBase ({gameObject.name}): 'FirePoint' is not assigned!", this);
            if (aimResolver == null)
            {
                aimResolver = GetComponentInParent<AimDirectionResolver>() ?? GetComponent<AimDirectionResolver>();
                if (aimResolver == null) Debug.LogError($"WeaponBase ({gameObject.name}): 'AimDirectionResolver' could not be found.", this);
            }
            if (upgradeContainer == null) upgradeContainer = transform;

            if (InputManager.Instance?.Controls?.Player.Shoot != null)
            {
                InputManager.Instance.Controls.Player.Shoot.started += OnShootActionPerformed;
                InputManager.Instance.Controls.Player.Shoot.canceled += OnShootActionPerformed;
            }
            else Debug.LogWarning($"WeaponBase ({gameObject.name}): Could not subscribe to Shoot action in Awake. InputManager or Controls might be missing or not enabled yet.", this);

            EquipInitialUpgrade();
            
            if (aimableArmTransform == null)
            {
                if (firePoint != null && firePoint.parent != transform && firePoint.parent != upgradeContainer) 
                {
                    aimableArmTransform = firePoint.parent;
                }
                else
                {
                    aimableArmTransform = firePoint; 
                    if (aimableArmTransform == null) Debug.LogError($"WeaponBase ({gameObject.name}): 'FirePoint' is not assigned, cannot default Aimable Arm Transform!", this);
                    else Debug.LogWarning($"WeaponBase ({gameObject.name}): 'Aimable Arm Transform' not assigned. Defaulting to rotating the FirePoint itself.", this);
                }
            }

            // Ejemplo de uso (opcional): Validar si la capa de pickup está configurada
            if (pickupObjectLayer.value == 0) // Ninguna capa seleccionada
            {
                Debug.LogWarning($"WeaponBase ({gameObject.name}): 'Pickup Object Layer' is not configured (set to 'Nothing'). This might be intentional or an oversight.", this);
            }
        }

        // ... (OnDestroy, OnShootActionPerformed, Update sin cambios relevantes) ...

        public void EquipUpgradeFromPrefab(GameObject upgradePrefabToEquip)
        {
            // ... (sin cambios relevantes a esta adición) ...
            if (upgradePrefabToEquip == null)
            {
                Debug.LogWarning($"WeaponBase ({gameObject.name}): EquipUpgradeFromPrefab called with null prefab.", this);
                return;
            }

            IWeaponUpgrade prospectiveUpgrade = upgradePrefabToEquip.GetComponentInChildren<IWeaponUpgrade>(true) ?? upgradePrefabToEquip.GetComponent<IWeaponUpgrade>();
            if (prospectiveUpgrade == null)
            {
                Debug.LogError($"WeaponBase ({gameObject.name}): Prefab '{upgradePrefabToEquip.name}' does not contain a valid IWeaponUpgrade component. Cannot equip.", this);
                return;
            }

            if (currentInstantiatedUpgradeGameObject != null)
            {
                Destroy(currentInstantiatedUpgradeGameObject);
                currentInstantiatedUpgradeGameObject = null; 
            }

            currentInstantiatedUpgradeGameObject = Instantiate(upgradePrefabToEquip, upgradeContainer);
            currentInstantiatedUpgradeGameObject.transform.localPosition = Vector3.zero;
            currentInstantiatedUpgradeGameObject.transform.localRotation = Quaternion.identity;
            currentInstantiatedUpgradeGameObject.name = upgradePrefabToEquip.name + "_Instance"; 

            IWeaponUpgrade newEquippedUpgrade = currentInstantiatedUpgradeGameObject.GetComponentInChildren<IWeaponUpgrade>(true) ?? currentInstantiatedUpgradeGameObject.GetComponent<IWeaponUpgrade>();
            
            SetInternalUpgradeState(newEquippedUpgrade);
        }

        public void EquipInitialUpgrade()
        {
            // ... (sin cambios relevantes a esta adición) ...
            if (initialUpgradePrefab != null)
            {
                EquipUpgradeFromPrefab(initialUpgradePrefab);
            }
            else
            {
                if (currentInstantiatedUpgradeGameObject != null)
                {
                    Destroy(currentInstantiatedUpgradeGameObject);
                    currentInstantiatedUpgradeGameObject = null;
                }
                SetInternalUpgradeState(null);
                Debug.LogWarning($"WeaponBase ({gameObject.name}): No initial upgrade prefab assigned. Player will be unarmed if no other upgrade is equipped.", this);
            }
        }

        private void SetInternalUpgradeState(IWeaponUpgrade newUpgrade)
        {
            currentUpgradeInterface = newUpgrade;

            if (currentUpgradeInterface is MonoBehaviour newMonoBehaviour && newMonoBehaviour != null)
            {
                if (!newMonoBehaviour.gameObject.activeSelf)
                {
                    newMonoBehaviour.gameObject.SetActive(true);
                }
                if (!newMonoBehaviour.enabled)
                {
                    newMonoBehaviour.enabled = true;
                }
            }
            
            PlayerEvents.RaisePlayerWeaponChanged(currentUpgradeInterface as Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade);
            semiAutoFireTimer = 0; 
        }
        
        private void RotateTransformToAim(Transform objectToRotate, Vector2 direction)
        {
            if (objectToRotate != null && direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                objectToRotate.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        
        public Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade CurrentUpgradeAsBaseType => currentUpgradeInterface as Scripts.Player.Weapons.Upgrades.BaseWeaponUpgrade;
        public IWeaponUpgrade GetCurrentUpgradeInterface() => currentUpgradeInterface;

        // Getter para la capa de pickups si otros scripts necesitan consultarla
        public LayerMask GetPickupObjectLayer() => pickupObjectLayer;


        private void OnDestroy()
        {
            if (InputManager.Instance?.Controls?.Player.Shoot != null)
            {
                InputManager.Instance.Controls.Player.Shoot.started -= OnShootActionPerformed;
                InputManager.Instance.Controls.Player.Shoot.canceled -= OnShootActionPerformed;
            }
            if (currentInstantiatedUpgradeGameObject != null)
            {
                Destroy(currentInstantiatedUpgradeGameObject);
                currentInstantiatedUpgradeGameObject = null;
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

            RotateTransformToAim(aimableArmTransform, aimResolver.CurrentDirection);

            bool singleShotRequestedThisFrame = InputManager.Instance?.Controls?.Player.Shoot.WasPressedThisFrame() ?? false;

            if (currentUpgradeInterface is IAutomaticWeapon automaticWeapon)
            {
                if (isShootActionPressed && currentUpgradeInterface.CanFire())
                {
                    PlayWeaponFireSound();
                    automaticWeapon.HandleAutomaticFire(firePoint, aimResolver.CurrentDirection);
                }
            }
            else 
            {
                if (singleShotRequestedThisFrame && semiAutoFireTimer <= 0 && currentUpgradeInterface.CanFire())
                {
                    PlayWeaponFireSound();
                    if (currentUpgradeInterface is IBurstWeapon burstWeapon)
                    {
                        burstWeapon.StartBurst(firePoint, aimResolver.CurrentDirection);
                    }
                    else 
                    {
                        currentUpgradeInterface.Fire(firePoint, aimResolver.CurrentDirection);
                    }
                    semiAutoFireTimer = Mathf.Max(semiAutoFireCooldown, currentUpgradeInterface.GetFireCooldown());
                }
            }
        }
        
        /// <summary>
        /// Reproduce el sonido de disparo configurado en la mejora de arma actual.
        /// </summary>
        private void PlayWeaponFireSound()
        {
            if (currentUpgradeInterface == null || audioSource == null)
            {
                if (currentUpgradeInterface != null && audioSource == null)
                    Debug.LogWarning($"WB: Intentando reproducir sonido pero 'weaponFireAudioSource' es null.", this);
                return;
            }

            // currentUpgradeInterface debe ser casteado a BaseWeaponUpgrade para acceder a GetFireSounds()
            // o IWeaponUpgrade debe tener GetFireSounds().
            // Es mejor si IWeaponUpgrade define el contrato para los sonidos.
            // Vamos a añadir GetFireSounds() a IWeaponUpgrade.

            Sounds[] fireSounds = null;
            if (currentUpgradeInterface is BaseWeaponUpgrade baseUpgrade) // Chequeo seguro
            {
                fireSounds = baseUpgrade.GetFireSounds();
            }
            // else if (currentUpgradeInterface is ISoundProviderForWeapon soundProvider) // Alternativa con otra interfaz
            // {
            //     fireSounds = soundProvider.GetSounds();
            // }


            if (fireSounds != null && fireSounds.Length > 0)
            {
                Sounds soundToPlay = fireSounds[Random.Range(0, fireSounds.Length)];
                audioSource.clip = soundToPlay.clip;
                audioSource.volume = soundToPlay.volume;
                audioSource.pitch = soundToPlay.pitch;
                audioSource.PlayOneShot(audioSource.clip);
            }
            // else Debug.LogWarning($"WB: No fire sounds defined for current upgrade '{currentUpgradeInterface.GetType().Name}' or AudioSource missing.", this);
        }

        private void OnValidate()
        {
            if (Application.isPlaying || !gameObject.scene.IsValid()) return;

            if (aimResolver == null) aimResolver = GetComponentInParent<AimDirectionResolver>() ?? GetComponent<AimDirectionResolver>();
            if (upgradeContainer == null) upgradeContainer = transform;

            if (initialUpgradePrefab != null)
            {
                IWeaponUpgrade potentialInitialUpgrade = initialUpgradePrefab.GetComponentInChildren<IWeaponUpgrade>(true) ?? initialUpgradePrefab.GetComponent<IWeaponUpgrade>();
                if (potentialInitialUpgrade == null)
                {
                    Debug.LogError($"WeaponBase ({gameObject.name}): OnValidate - The 'Initial Upgrade Prefab' ('{initialUpgradePrefab.name}') " +
                                     "does not contain any component that implements IWeaponUpgrade.", this);
                }
            }
            // Opcional: Validar que pickupObjectLayer esté configurado si es crítico
            // if (pickupObjectLayer.value == 0) {
            //     Debug.LogWarning($"WeaponBase ({gameObject.name}): OnValidate - 'Pickup Object Layer' is not configured.", this);
            // }
        }
    }
}