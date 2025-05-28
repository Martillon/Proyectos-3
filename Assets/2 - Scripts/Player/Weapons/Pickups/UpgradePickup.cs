using Scripts.Core.Audio;
using UnityEngine;
using Scripts.Player.Weapons.Interfaces;

namespace Scripts.Player.Weapons.Pickups
{
    [RequireComponent(typeof(Collider2D))]
    public class UpgradePickup : MonoBehaviour
    {
        [Header("Upgrade Configuration")]
        [SerializeField] private GameObject upgradePrefab;

        [Header("Collision Settings")]
        [SerializeField] private LayerMask collidableWithLayers;
        [Tooltip("Time in seconds before the pickup is destroyed after being collected.")]
        [SerializeField] private float timeDelayBeforeDestroy = 5f;

        [Header("Visual & Audio Feedback (Optional)")]
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField] private Sounds[] pickupSounds; 

        private Collider2D _collider;
        private AudioSource _audioSource;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"UpgradePickup '{gameObject.name}': Collider2D is not set to 'Is Trigger'. Setting it now.", this);
                _collider.isTrigger = true;
            }
            
            _audioSource = GetComponent<AudioSource>(); if(!_audioSource)
                Debug.LogWarning($"UpgradePickup '{gameObject.name}': No AudioSource found.", this);
            
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>(); if(!_spriteRenderer)
                Debug.LogWarning($"UpgradePickup '{gameObject.name}': No sprite renderer found.", this);

            if (upgradePrefab == null)
            {
                Debug.LogError($"UpgradePickup '{gameObject.name}': 'Upgrade Prefab' is not assigned.", this);
                enabled = false; 
                return;
            }
            if (collidableWithLayers.value == 0)
            {
                Debug.LogError($"UpgradePickup '{gameObject.name}': 'Collidable With Layers' is not assigned.", this);
                // enabled = false; // Opcional
            }
            if (upgradePrefab.GetComponentInChildren<IWeaponUpgrade>(true) == null && upgradePrefab.GetComponent<IWeaponUpgrade>() == null)
            {
                Debug.LogError($"UpgradePickup '{gameObject.name}': Prefab '{upgradePrefab.name}' no contiene IWeaponUpgrade.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled) return;

            if ((collidableWithLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return; // No es la capa correcta
            }
            
            PlayPickupSound();

            // --- MODIFICACIÓN IMPORTANTE AQUÍ ---
            // En lugar de other.GetComponentInChildren, necesitamos encontrar el "root" del jugador
            // y luego buscar WeaponBase desde allí.
            // Asumimos que el Rigidbody2D está en el objeto raíz del jugador que colisiona,
            // o que el collider tiene una referencia a su entidad "dueña".
            // La forma más común es que el Rigidbody2D esté en el objeto que debe recibir los mensajes.

            WeaponBase playerWeaponSystem = null;
            Rigidbody2D playerRb = other.attachedRigidbody; // Obtiene el Rigidbody2D al que este collider está adjunto.
                                                          // Esto suele ser el objeto "principal" del jugador.

            if (playerRb != null)
            {
                // Ahora busca WeaponBase en el GameObject del Rigidbody y sus hijos.
                playerWeaponSystem = playerRb.GetComponentInChildren<WeaponBase>(true);
                // Debug.Log($"UpgradePickup: Found Rigidbody on '{playerRb.gameObject.name}'. Searching WeaponBase from there.", this);
            }
            else
            {
                // Si no hay Rigidbody adjunto directamente al collider que impactó (raro para un jugador dinámico),
                // podríamos intentar subir en la jerarquía desde 'other' hasta encontrar Player_root.
                // Pero 'attachedRigidbody' es la forma preferida.
                // Como fallback, si 'other' es parte de una jerarquía más grande y no tiene su propio RB.
                Transform rootOfOther = other.transform.root; // Obtiene el transform más alto en la jerarquía de 'other'
                playerWeaponSystem = rootOfOther.GetComponentInChildren<WeaponBase>(true);
                // Debug.LogWarning($"UpgradePickup: No attached Rigidbody found for collider '{other.name}'. Trying search from root '{rootOfOther.name}'.", this);
            }
            // --- FIN DE LA MODIFICACIÓN ---


            if (playerWeaponSystem == null)
            {
                Debug.LogWarning($"UpgradePickup on '{gameObject.name}': Collided with '{other.name}' (Layer: {LayerMask.LayerToName(other.gameObject.layer)}), " +
                                 $"but WeaponBase component was NOT found on its hierarchy (searched from Rigidbody owner or root: {(playerRb != null ? playerRb.gameObject.name : other.transform.root.name)}).", this);
                return;
            }

            // Debug.Log($"UpgradePickup: '{other.name}' collected '{upgradePrefab.name}'. Equipping via WeaponBase on '{playerWeaponSystem.gameObject.name}'.", this);
            playerWeaponSystem.EquipUpgradeFromPrefab(upgradePrefab);

            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }
            
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
            if (_collider != null) _collider.enabled = false;
            
            Destroy(gameObject, timeDelayBeforeDestroy);
        }

        private void PlayPickupSound()
        {
            if (_audioSource == null || pickupSounds == null || pickupSounds.Length == 0) return;

            // Play a random sound from the pickupSounds array
            int randomIndex = Random.Range(0, pickupSounds.Length);
            Sounds soundToPlay = pickupSounds[randomIndex];

            if (soundToPlay != null)
            {
                _audioSource.PlayOneShot(soundToPlay.clip, soundToPlay.volume);
            }
        }
    }
}
// --- END OF MODIFIED FILE UpgradePickup.cs ---