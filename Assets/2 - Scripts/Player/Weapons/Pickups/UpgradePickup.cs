using Scripts.Core.Audio;
using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Weapons.Pickups
{
    [RequireComponent(typeof(Collider2D), typeof(AudioSource))]
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The Weapon Stat Sheet to grant to the player on pickup.")]
        [SerializeField] private WeaponStats weaponToGrant;

        [Header("Feedback")]
        [Tooltip("The sound effect to play when the item is collected.")]
        [SerializeField] private Sounds pickupSound;
        [Tooltip("The visual effect to spawn when the item is collected.")]
        [SerializeField] private GameObject pickupVFX;

        private AudioSource _audioSource;
        private Collider2D _collider;
        private SpriteRenderer _visual;
        private bool _isCollected = false;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _collider = GetComponent<Collider2D>();
            _visual = GetComponentInChildren<SpriteRenderer>(); // Get the visual part

            _collider.isTrigger = true;
            _audioSource.playOnAwake = false;

            if (weaponToGrant != null) return;
            Debug.LogError($"WeaponPickup '{name}' has no WeaponStats assigned!", this);
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected || !other.transform.root.CompareTag("Player")) return;

            // Step 1: Get the absolute root of the object that entered the trigger.
            Transform playerRoot = other.transform.root;

            // Step 2: Search for the WeaponBase component within the root and ALL its children.
            WeaponBase weaponSystem =
                playerRoot.GetComponentInChildren<WeaponBase>(true); // 'true' includes inactive objects

            // Step 3: If we found it, proceed.
            if (weaponSystem)
            {
                _isCollected = true;

                // Grant the weapon to the player
                weaponSystem.EquipWeapon(weaponToGrant);

                // Play Feedback
                pickupSound?.Play(_audioSource);
                if (pickupVFX)
                {
                    Instantiate(pickupVFX, transform.position, Quaternion.identity);
                }

                // Hide and schedule destruction
                if (_visual) _visual.enabled = false;
                _collider.enabled = false;
                Destroy(gameObject, pickupSound?.clip.length ?? 2.0f);
            }
        }
    }
}