using UnityEngine;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Core
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "My Game/Player/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Default Starting Stats")]
        public int defaultLives = 3;
        public int maxArmor = 3;
        public WeaponStats defaultWeapon;

        [Header("Current In-Game State (Runtime)")]
        // These values persist between scenes during a single bounty run.
        public int currentLives;
        public int currentArmor;
        public WeaponStats currentWeapon;
        
        [System.NonSerialized]
        private bool _isInitialized = false;

        /// <summary>
        /// Resets the player's stats to their default values for a new bounty run.
        /// </summary>
        public void ResetForNewRun()
        {
            currentLives = defaultLives;
            currentArmor = maxArmor;
            currentWeapon = defaultWeapon;
            
            _isInitialized = true;
            Debug.Log("PlayerStats have been reset for a new run.");
        }
        
        /// <summary>
        /// Checks if the stats have been initialized for the current session.
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized;
        }
    }
}