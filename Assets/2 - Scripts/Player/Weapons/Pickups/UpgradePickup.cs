// --- START OF FILE UpgradePickup.cs ---
using UnityEngine;
using Scripts.Player.Weapons.Interfaces; // For IWeaponUpgrade (though not strictly needed by this script directly anymore)

namespace Scripts.Player.Weapons.Pickups
{
    /// <summary>
    /// Represents a collectible weapon upgrade in the scene.
    /// When the player touches it, the player's weapon system is updated by equipping
    /// the upgrade defined by the assigned 'Upgrade Prefab'.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpgradePickup : MonoBehaviour
    {
        [Header("Upgrade Configuration")]
        [Tooltip("The Prefab that contains the actual weapon upgrade script (e.g., a DefaultUpgrade or ShotgunUpgrade component). This script must implement IWeaponUpgrade.")]
        [SerializeField] private GameObject upgradePrefab;

        [Header("Visual & Audio Feedback (Optional)")]
        [Tooltip("Visual effect to instantiate when the pickup is collected.")]
        [SerializeField] private GameObject pickupEffectPrefab;
        // [SerializeField] private Sounds pickupSound; // Example for sound
        // [SerializeField] private AudioSource audioSourceForPickupSound; // Example for sound

        private void Awake()
        {
            // Basic validation of the upgradePrefab
            if (upgradePrefab == null)
            {
                Debug.LogError($"UpgradePickup '{gameObject.name}': 'Upgrade Prefab' is not assigned. This pickup will not function.", this);
                enabled = false; // Disable the script if it's not configured correctly
                return;
            }

            // More detailed validation: Check if the prefab actually contains an IWeaponUpgrade component.
            // We check components on the prefab's root and its children, including inactive ones.
            if (upgradePrefab.GetComponentInChildren<IWeaponUpgrade>(true) == null && upgradePrefab.GetComponent<IWeaponUpgrade>() == null)
            {
                Debug.LogError($"UpgradePickup '{gameObject.name}': The assigned 'Upgrade Prefab' ('{upgradePrefab.name}') " +
                                 "does not contain any component that implements IWeaponUpgrade. This pickup will not function.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled) return; // Don't run if misconfigured (checked in Awake)

            if (!other.CompareTag("Player"))
            {
                // Optional: Log collision with non-player for debugging if needed
                // Debug.Log($"UpgradePickup on '{gameObject.name}' collided with non-player: {other.name}", this);
                return;
            }

            WeaponBase playerWeaponSystem = other.GetComponentInChildren<WeaponBase>(); // Find WeaponBase on the player or its children
            if (playerWeaponSystem == null)
            {
                // Debug.LogWarning($"UpgradePickup on '{gameObject.name}': Player collided, but no WeaponBase component was found on the player entity.", this); // Uncomment for debugging
                return;
            }

            // Equip the upgrade using the prefab
            // Debug.Log($"UpgradePickup: Player collected '{upgradePrefab.name}'. Equipping via WeaponBase.", this); // Uncomment for debugging
            playerWeaponSystem.EquipUpgradeFromPrefab(upgradePrefab);

            // Instantiate visual effect if assigned
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            // Play pickup sound if configured
            // pickupSound?.Play(audioSourceForPickupSound);

            // Consume the pickup
            Destroy(gameObject);
        }
    }
}
// --- END OF FILE UpgradePickup.cs ---