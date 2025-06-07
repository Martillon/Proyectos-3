using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Player.Core;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Controls the Heads-Up Display, updating elements like health, armor, and weapon icon
    /// by subscribing to events from PlayerEvents.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health & Lives")]
        [Tooltip("Text element to display the player's remaining lives.")]
        [SerializeField] private TMP_Text livesCountText;

        [Header("Armor Display")]
        [Tooltip("An array of Image elements representing armor points.")]
        [SerializeField] private Image[] armorPointImages;
        [Tooltip("Sprite for a full/active armor point.")]
        [SerializeField] private Sprite armorFullSprite;
        [Tooltip("Sprite for an empty/lost armor point.")]
        [SerializeField] private Sprite armorEmptySprite;

        [Header("Weapon Display")]
        [Tooltip("Image element to display the current weapon's icon.")]
        [SerializeField] private Image currentWeaponIcon;
        [Tooltip("Sprite to show when no weapon is equipped or the weapon has no icon.")]
        [SerializeField] private Sprite defaultWeaponSprite;

        private void OnEnable()
        {
            // Subscribe to player events to receive updates.
            PlayerEvents.OnHealthChanged += UpdateHealthDisplay;
            PlayerEvents.OnWeaponChanged += UpdateWeaponDisplay;
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks.
            PlayerEvents.OnHealthChanged -= UpdateHealthDisplay;
            PlayerEvents.OnWeaponChanged -= UpdateWeaponDisplay;
        }

        private void Start()
        {
            // Initialize the HUD with default/empty state to avoid showing stale data.
            UpdateHealthDisplay(0, 0);
            UpdateWeaponDisplay(null);
        }

        private void UpdateHealthDisplay(int currentLives, int currentArmor)
        {
            if (livesCountText != null)
            {
                livesCountText.text = $"x{currentLives}";
            }

            if (armorPointImages != null && armorFullSprite != null && armorEmptySprite != null)
            {
                for (int i = 0; i < armorPointImages.Length; i++)
                {
                    if (armorPointImages[i] != null)
                    {
                        // Enable the image and set its sprite based on whether this armor point is "filled".
                        armorPointImages[i].enabled = true;
                        armorPointImages[i].sprite = (i < currentArmor) ? armorFullSprite : armorEmptySprite;
                    }
                }
            }
        }

        private void UpdateWeaponDisplay(BaseWeaponUpgrade activeUpgrade)
        {
            if (currentWeaponIcon != null)
            {
                if (activeUpgrade != null && activeUpgrade.Icon != null)
                {
                    currentWeaponIcon.sprite = activeUpgrade.Icon;
                    currentWeaponIcon.enabled = true;
                }
                else // No upgrade or upgrade has no icon.
                {
                    currentWeaponIcon.sprite = defaultWeaponSprite;
                    // Only show the default sprite if one is actually assigned.
                    currentWeaponIcon.enabled = (defaultWeaponSprite != null);
                }
            }
        }
    }
}
