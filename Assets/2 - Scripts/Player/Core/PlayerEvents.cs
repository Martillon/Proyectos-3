using System;
using Scripts.Player.Weapons.Upgrades;

namespace Scripts.Player.Core
{
    /// <summary>
    /// A static class for managing global player-related events.
    /// Systems can subscribe to these events to react to player state changes
    /// without needing a direct reference to player components.
    /// </summary>
    public static class PlayerEvents
    {
        /// <summary>
        /// Invoked when the player runs out of lives, triggering the final death sequence.
        /// </summary>
        public static event Action OnPlayerDeath;

        /// <summary>
        /// Invoked when the player's health or armor changes.
        /// Parameters: (currentLives, currentArmor)
        /// </summary>
        public static event Action<int, int> OnHealthChanged;

        /// <summary>
        /// Invoked when the player successfully completes a level by reaching an exit.
        /// Parameter: string levelIdentifier (e.g., scene name of the completed level).
        /// </summary>
        public static event Action<string> OnLevelCompleted;
        
        /// <summary>
        /// Invoked when the player's equipped weapon changes.
        /// Parameter: (newStats) - The WeaponStats Scriptable Object for the new weapon.
        /// </summary>
        public static event Action<WeaponStats> OnWeaponChanged;


        public static void RaisePlayerDeath() => OnPlayerDeath?.Invoke();
        public static void RaiseHealthChanged(int lives, int armor) => OnHealthChanged?.Invoke(lives, armor);
        public static void RaiseWeaponChanged(WeaponStats newStats) => OnWeaponChanged?.Invoke(newStats);
        public static void RaiseLevelCompleted(string levelIdentifier) => OnLevelCompleted?.Invoke(levelIdentifier);
    }
}
