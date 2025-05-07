// --- START OF FILE PlayerEvents.cs ---

using System;
using UnityEngine;
using Scripts.Player.Weapons.Upgrades; // Asegúrate de que este using esté presente

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

        /// <summary>
        /// Invoked when the player's equipped weapon upgrade changes.
        /// Parameter: (newUpgrade) - Can be null if no upgrade is equipped.
        /// </summary>
        public static event Action<BaseWeaponUpgrade> OnPlayerWeaponChanged; // <--- NUEVO EVENTO

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

        public static void RaisePlayerWeaponChanged(BaseWeaponUpgrade newUpgrade) // <--- NUEVO MÉTODO
        {
            OnPlayerWeaponChanged?.Invoke(newUpgrade);
            // Debug.Log($"Player weapon changed to: {newUpgrade?.name ?? "None"}");
        }
    }
}
// --- END OF FILE PlayerEvents.cs ---
