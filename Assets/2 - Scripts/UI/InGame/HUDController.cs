// --- START OF FILE HUDController.cs ---

using Scripts.Core;
using UnityEngine;
using UnityEngine.UI; // For Image
using TMPro; // For TextMeshProUGUI
using Scripts.Player.Core;
using Scripts.Player.Weapons; // For PlayerEvents
using Scripts.Player.Weapons.Upgrades; // For BaseWeaponUpgrade

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Controls the Heads-Up Display (HUD) elements such as player health (lives and armor)
    /// and the currently equipped weapon's icon. Updates based on events from PlayerEvents.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health Display References")]
        [Tooltip("TextMeshProUGUI element to display the number of remaining lives.")]
        [SerializeField] private TMP_Text livesCountText;

        [Header("Armor Display Configuration")]
        [Tooltip("Array of Image elements representing armor points. These will have their sprites changed.")]
        [SerializeField] private Image[] armorPointIconImages; // Renamed for clarity
        [Tooltip("Sprite to use when an armor point is full/active.")]
        [SerializeField] private Sprite armorFullSprite;
        [Tooltip("Sprite to use when an armor point is empty/lost.")]
        [SerializeField] private Sprite armorEmptySprite;

        [Header("Weapon Display References")]
        [Tooltip("Image element that will display the icon of the currently equipped weapon upgrade.")]
        [SerializeField] private Image currentWeaponIconImage;
        [Tooltip("Sprite to display when no weapon upgrade is equipped or if the upgrade has no icon.")]
        [SerializeField] private Sprite defaultNoWeaponSprite;

        private void Awake()
        {
            // Validate essential UI references
            if (livesCountText == null) Debug.LogError("HUDController: LivesCountText is not assigned!", this);
            if (currentWeaponIconImage == null) Debug.LogError("HUDController: CurrentWeaponIconImage is not assigned!", this);
            if (defaultNoWeaponSprite == null) Debug.LogWarning("HUDController: DefaultNoWeaponSprite is not assigned. Weapon display might be blank initially.", this);

            // Validate armor display configuration
            if (armorPointIconImages == null || armorPointIconImages.Length == 0)
            {
                Debug.LogWarning("HUDController: ArmorPointIconImages array is not assigned or is empty. Armor will not be displayed.", this);
            }
            else
            {
                if (armorFullSprite == null) Debug.LogError("HUDController: ArmorFullSprite is not assigned! Armor display will be incorrect.", this);
                if (armorEmptySprite == null) Debug.LogError("HUDController: ArmorEmptySprite is not assigned! Armor display will be incorrect.", this);
            }
        }

        private void OnEnable()
        {
            PlayerEvents.OnPlayerHealthChanged += UpdateHealthDisplay;
            PlayerEvents.OnPlayerWeaponChanged += UpdateWeaponIconDisplay;
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerHealthChanged -= UpdateHealthDisplay;
            PlayerEvents.OnPlayerWeaponChanged -= UpdateWeaponIconDisplay;
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag); // Using GameConstants
            if (playerObject != null)
            {
                PlayerHealthSystem health = playerObject.GetComponent<PlayerHealthSystem>();
                if (health != null)
                {
                    UpdateHealthDisplay(health.GetCurrentLives(), health.GetCurrentArmor());
                }

                WeaponBase weaponSys = playerObject.GetComponentInChildren<WeaponBase>();
                if (weaponSys != null)
                {
                    UpdateWeaponIconDisplay(weaponSys.CurrentUpgradeAsBaseType);
                }
            }
            else
            {
                UpdateHealthDisplay(0, 0);
                UpdateWeaponIconDisplay(null);
            }
        }

        /// <summary>
        /// Updates the health-related UI elements (lives count and armor icons by changing sprites).
        /// Called by PlayerEvents.OnPlayerHealthChanged.
        /// </summary>
        private void UpdateHealthDisplay(int currentLives, int currentArmor)
        {
            if (livesCountText != null)
            {
                livesCountText.text = $"x{currentLives}";
            }

            if (armorPointIconImages != null && armorFullSprite != null && armorEmptySprite != null)
            {
                for (int i = 0; i < armorPointIconImages.Length; i++)
                {
                    if (armorPointIconImages[i] != null)
                    {
                        // If 'i' (0-indexed) is less than currentArmor, it's a full/active point.
                        // Otherwise, it's an empty/lost point.
                        armorPointIconImages[i].sprite = (i < currentArmor) ? armorFullSprite : armorEmptySprite;
                        armorPointIconImages[i].enabled = true; // Ensure the Image component itself is visible
                    }
                }
            }
        }

        private void UpdateWeaponIconDisplay(BaseWeaponUpgrade activeUpgrade)
        {
            if (currentWeaponIconImage != null)
            {
                if (activeUpgrade != null && activeUpgrade.Icon != null)
                {
                    currentWeaponIconImage.sprite = activeUpgrade.Icon;
                    currentWeaponIconImage.enabled = true;
                }
                else
                {
                    currentWeaponIconImage.sprite = defaultNoWeaponSprite;
                    currentWeaponIconImage.enabled = (defaultNoWeaponSprite != null);
                }
            }
        }
    }
}
// --- END OF FILE HUDController.cs ---
