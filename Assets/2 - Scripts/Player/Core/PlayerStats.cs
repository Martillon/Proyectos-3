using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Core
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "My Game/Player/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Default Starting Stats")]
        public int defaultLives = 3;
        public int maxArmor = 2;
        public WeaponStats defaultWeapon;

        [Header("Current In-Game State (Runtime)")]
        // These values persist between scenes during a single bounty run.
        [HideInInspector] public int currentLives;
        [HideInInspector] public int currentArmor;
        [HideInInspector] public WeaponStats currentWeapon;

        /// <summary>
        /// Resets the player's stats to their default values for a new bounty run.
        /// </summary>
        public void ResetForNewRun()
        {
            currentLives = defaultLives;
            currentArmor = maxArmor;
            currentWeapon = defaultWeapon;
        }
    }
}