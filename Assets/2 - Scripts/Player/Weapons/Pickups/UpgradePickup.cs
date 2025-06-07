using UnityEngine;
using Scripts.Core.Audio;
using Scripts.Player.Weapons;

namespace Scripts.Player.Weapons.Pickups
{
    /// <summary>
    /// Represents a collectible item that grants the player a new weapon upgrade.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpgradePickup : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The weapon upgrade prefab to be equipped when this pickup is collected.")]
        [SerializeField] private GameObject upgradePrefab;
        
        [Header("Feedback")]
        [Tooltip("Optional: A visual effect prefab to instantiate upon collection.")]
        [SerializeField] private GameObject pickupVFX;
        [Tooltip("Optional: A sound to play upon collection.")]
        [SerializeField] private Sounds pickupSound;
        
        private AudioSource _audioSource;
        private bool _isCollected = false;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _audioSource = GetComponent<AudioSource>();

            if (upgradePrefab == null)
            {
                Debug.LogError($"UpgradePickup '{name}': Upgrade Prefab is not assigned!", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            // Attempt to get the WeaponBase component from the root of the colliding object.
            if (other.transform.root.TryGetComponent<WeaponBase>(out var weaponSystem))
            {
                _isCollected = true;
                weaponSystem.EquipUpgradeFromPrefab(upgradePrefab);
                
                // Provide feedback
                if (pickupVFX != null) Instantiate(pickupVFX, transform.position, Quaternion.identity);
                if (pickupSound != null && _audioSource != null) pickupSound.Play(_audioSource);
                
                // Hide the pickup and schedule it for destruction.
                gameObject.SetActive(false);
                Destroy(gameObject, 2f); // Destroy after a delay to allow sound to play.
            }
        }
    }
}