using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Player.Core;
using Scripts.Player.Weapons;

namespace Scripts.UI.InGame
{
    /// <summary>
    /// Controls the in-game HUD (health, armor, weapon display).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealthSystem playerHealth;
        [SerializeField] private WeaponBase weapon;

        [Header("Lives Display")]
        [SerializeField] private Image playerIcon;
        [SerializeField] private TMP_Text livesText;

        [Header("Armor Display")]
        [SerializeField] private Image[] armorIcons; // Should be 3 elements

        [Header("Weapon Display")]
        [SerializeField] private Image weaponFrame;
        [SerializeField] private Sprite defaultWeaponSprite;
        [SerializeField] private Image weaponIcon;

        private void OnEnable()
        {
            PlayerEvents.OnPlayerHealthChanged += UpdateHealthUI;
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerHealthChanged -= UpdateHealthUI;
        }

        private void UpdateHealthUI(int lives, int armor)
        {
            livesText.text = $"x{lives}";
            for (int i = 0; i < armorIcons.Length; i++)
            {
                armorIcons[i].enabled = i < armor;
            }
        }

        private void UpdateWeaponDisplay()
        {
            if (weapon.CurrentUpgrade != null)
            {
                weaponIcon.sprite = weapon.CurrentUpgrade.Icon;
            }
            else
            {
                weaponIcon.sprite = defaultWeaponSprite;
            }
        }
    }
}
