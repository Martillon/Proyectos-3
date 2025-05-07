// --- START OF FILE HUDController.cs ---
using UnityEngine;
using UnityEngine.UI; // For Image
using TMPro; // For TextMeshProUGUI
using Scripts.Player.Core;
using Scripts.Player.Weapons; // For PlayerEvents and potentially direct access if needed (though events are preferred)
using Scripts.Player.Weapons.Upgrades; // For BaseWeaponUpgrade (to get Icon)
// using Scripts.Player.Weapons; // No longer strictly needed if WeaponBase isn't directly referenced

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Controls the Heads-Up Display (HUD) elements such as player health (lives and armor)
    /// and the currently equipped weapon's icon. Updates based on events from PlayerEvents.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health Display References")]
        // [SerializeField] private Image playerAvatarIcon; // Optional: if you have an avatar image for lives
        [Tooltip("TextMeshProUGUI element to display the number of remaining lives.")]
        [SerializeField] private TMP_Text livesCountText;
        [Tooltip("Array of Image elements representing armor points. Should match maxArmorPerLife.")]
        [SerializeField] private Image[] armorPointIcons;

        [Header("Weapon Display References")]
        [Tooltip("Image element that will display the icon of the currently equipped weapon upgrade.")]
        [SerializeField] private Image currentWeaponIconImage;
        [Tooltip("Sprite to display when no weapon upgrade is equipped or if the upgrade has no icon.")]
        [SerializeField] private Sprite defaultNoWeaponSprite; // e.g., an empty slot or a basic pistol icon

        // Optional: Direct reference to PlayerHealthSystem if needed for initial state, though events cover most cases.
        // [SerializeField] private PlayerHealthSystem playerHealthSystem;
        // Optional: Direct reference to WeaponBase for initial weapon state.
        // [SerializeField] private WeaponBase playerWeaponBase;


        private void Awake()
        {
            // Validate essential UI references
            if (livesCountText == null) Debug.LogError("HUDController: LivesCountText is not assigned!", this);
            if (armorPointIcons == null || armorPointIcons.Length == 0) Debug.LogWarning("HUDController: ArmorPointIcons array is not assigned or is empty. Armor will not be displayed.", this);
            if (currentWeaponIconImage == null) Debug.LogError("HUDController: CurrentWeaponIconImage is not assigned!", this);
            if (defaultNoWeaponSprite == null) Debug.LogWarning("HUDController: DefaultNoWeaponSprite is not assigned. Weapon display might be blank initially.", this);
        }

        private void OnEnable()
        {
            // Subscribe to player events
            PlayerEvents.OnPlayerHealthChanged += UpdateHealthDisplay;
            PlayerEvents.OnPlayerWeaponChanged += UpdateWeaponIconDisplay;
            // Debug.Log("HUDController: Subscribed to PlayerEvents.", this); // Uncomment for debugging
        }

        private void OnDisable()
        {
            // Unsubscribe from player events to prevent errors when this object is disabled/destroyed
            PlayerEvents.OnPlayerHealthChanged -= UpdateHealthDisplay;
            PlayerEvents.OnPlayerWeaponChanged -= UpdateWeaponIconDisplay;
            // Debug.Log("HUDController: Unsubscribed from PlayerEvents.", this); // Uncomment for debugging
        }

        private void Start()
        {
            // Attempt to set initial UI state in case events were missed (e.g., HUD enabled after player setup)
            // This requires finding the player and its components.
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PlayerHealthSystem health = playerObject.GetComponent<PlayerHealthSystem>();
                if (health != null)
                {
                    UpdateHealthDisplay(health.GetCurrentLives(), health.GetCurrentArmor());
                }
                // else Debug.LogWarning("HUDController (Start): PlayerHealthSystem not found on Player object for initial setup.", this); // Uncomment for debugging

                WeaponBase weaponSys = playerObject.GetComponentInChildren<WeaponBase>(); // WeaponBase might be a child
                if (weaponSys != null)
                {
                    UpdateWeaponIconDisplay(weaponSys.CurrentUpgradeAsBase);
                }
                // else Debug.LogWarning("HUDController (Start): WeaponBase not found on Player object for initial setup.", this); // Uncomment for debugging
            }
            else
            {
                // No player found, set UI to a default "empty" state if necessary
                UpdateHealthDisplay(0, 0); // Display 0 lives, 0 armor
                UpdateWeaponIconDisplay(null); // Display default weapon icon
                // Debug.LogWarning("HUDController (Start): Player object not found. HUD initialized to default state.", this); // Uncomment for debugging
            }
        }

        /// <summary>
        /// Updates the health-related UI elements (lives count and armor icons).
        /// Called by PlayerEvents.OnPlayerHealthChanged.
        /// </summary>
        /// <param name="currentLives">The player's current number of lives.</param>
        /// <param name="currentArmor">The player's current armor points for the active life.</param>
        private void UpdateHealthDisplay(int currentLives, int currentArmor)
        {
            // Debug.Log($"HUDController: Updating Health Display - Lives: {currentLives}, Armor: {currentArmor}", this); // Uncomment for debugging
            if (livesCountText != null)
            {
                livesCountText.text = $"x{currentLives}";
            }

            if (armorPointIcons != null)
            {
                for (int i = 0; i < armorPointIcons.Length; i++)
                {
                    if (armorPointIcons[i] != null) // Check if the Image element itself exists
                    {
                        // Show icon if 'i' is less than currentArmor, hide otherwise
                        armorPointIcons[i].enabled = (i < currentArmor);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the weapon icon UI element based on the currently equipped weapon upgrade.
        /// Called by PlayerEvents.OnPlayerWeaponChanged.
        /// </summary>
        /// <param name="activeUpgrade">The BaseWeaponUpgrade of the currently active weapon. Can be null.</param>
        private void UpdateWeaponIconDisplay(BaseWeaponUpgrade activeUpgrade)
        {
            // Debug.Log($"HUDController: Updating Weapon Icon. Active Upgrade: {(activeUpgrade != null ? activeUpgrade.name : "None")}", this); // Uncomment for debugging
            if (currentWeaponIconImage != null)
            {
                if (activeUpgrade != null && activeUpgrade.Icon != null)
                {
                    currentWeaponIconImage.sprite = activeUpgrade.Icon;
                    currentWeaponIconImage.enabled = true; // Ensure image component is visible
                }
                else // No active upgrade or the upgrade has no icon assigned
                {
                    currentWeaponIconImage.sprite = defaultNoWeaponSprite;
                    // Enable or disable based on whether a default sprite exists
                    currentWeaponIconImage.enabled = (defaultNoWeaponSprite != null);
                }
            }
        }
    }
}
// --- END OF FILE HUDController.cs ---
