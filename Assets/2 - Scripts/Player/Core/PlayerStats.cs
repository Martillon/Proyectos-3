using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Core
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "My Game/Player/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Default Starting Stats")]
        [Tooltip("The default number of lives the player starts a new game with.")]
        public int defaultLives = 3;
        [Tooltip("The maximum armor points per life.")]
        public int maxArmor = 2;
        [Tooltip("The player's starting weapon.")]
        public WeaponStats defaultWeapon;

        [Header("Current In-Game State")]
        [Tooltip("The player's current number of lives. This value will change during gameplay.")]
        [HideInInspector] public int currentLives; // HideInInspector so we don't accidentally change it.
        [Tooltip("The player's current armor points. This value will change during gameplay.")]
        [HideInInspector] public int currentArmor;
        [Tooltip("The player's currently equipped weapon. This will change during gameplay.")]
        [HideInInspector] public WeaponStats currentWeapon;

        /// <summary>
        /// Resets the player's current state to the default starting values.
        /// Call this when starting a new game or after a game over.
        /// </summary>
        public void ResetToDefaults()
        {
            currentLives = defaultLives;
            currentArmor = maxArmor;
            currentWeapon = defaultWeapon;
            Debug.Log("PlayerStats have been reset to defaults.");
        }
    }
}