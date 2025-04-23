using System;
using UnityEngine;

namespace Scripts.Player.Core
{
    public static class PlayerEvents
    {
        /// <summary>
        /// Invoked when the player dies.
        /// Used to trigger camera effects, stop music, show game over UI, etc.
        /// </summary>
        public static event Action OnPlayerDeath;
        
        /// <summary>
        /// Invoked when the player's health or armor changes.
        /// Parameters: (currentLives, currentArmor)
        /// </summary>
        public static event Action<int, int> OnPlayerHealthChanged;

        public static void RaisePlayerDeath()
        {
            OnPlayerDeath?.Invoke();
            // Debug.Log("Player death event raised.");
        }
        
        public static void RaiseHealthChanged(int lives, int armor)
        {
            OnPlayerHealthChanged?.Invoke(lives, armor);
            // Debug.Log($"Health updated → Lives: {lives} | Armor: {armor}");
        }
    }
}
