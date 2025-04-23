using System.Collections;
using Scripts.Core;
using Scripts.Core.Interfaces;
using Unity.Cinemachine;
using UnityEngine;

namespace Scripts.Player.Core
{
    /// <summary>
    /// Manages the player's lives and armor.
    /// Each life allows a defined number of hits (armor).
    /// Healing can restore armor or full lives, with limits.
    /// </summary>
    public class PlayerHealthSystem : MonoBehaviour, IHealLife, IHealArmor, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int maxArmorPerLife = 3;

        [Header("Debug")]
        [SerializeField] private bool destroyOnDeath = true;
        
        [Header("Death Sequence")]
        [SerializeField] private Animator animator;
        [SerializeField] private string deathTrigger = "Die";
        [SerializeField] private float buttonDelay = 1.5f;
        [SerializeField] private GameObject deathScreen;
        [SerializeField] private GameObject deathButtons;
        [SerializeField] private CinemachineCamera cameraToZoom;
        [SerializeField] private float zoomAmount = 3f;
        [SerializeField] private float zoomDuration = 1f;

        private float originalZoom;

        private int currentLives;
        private int currentArmor;

        private void Awake()
        {
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
            
            if (cameraToZoom != null)
            {
                originalZoom = cameraToZoom.Lens.OrthographicSize;
            }
        }

        /// <summary>
        /// Applies 1 hit to the player. If armor is depleted, a life is lost.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (currentLives <= 0) return;

            if (currentArmor > 0)
            {
                currentArmor--;
                // Debug.Log($"Damage taken. Armor reduced to {currentArmor}.");
            }
            else
            {
                currentLives--;
                currentArmor = maxArmorPerLife;

                // Debug.Log($"Armor broken. Life lost. Lives left: {currentLives}.");

                if (currentLives <= 0)
                {
                    HandleDeath();
                }
            }
            
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        /// <summary>
        /// Heals armor up to its maximum, but does not restore lives.
        /// </summary>
        public void HealArmor(int amount)
        {
            if (amount <= 0 || currentLives <= 0) return;

            currentArmor = Mathf.Min(currentArmor + amount, maxArmorPerLife);
            // Debug.Log($"Armor healed. New armor: {currentArmor}");
            
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);
        }

        /// <summary>
        /// Restores life and fully restores armor. Limited by maxLives.
        /// </summary>
        public void HealLife(int amount)
        {
            if (amount <= 0) return;

            currentLives = Mathf.Min(currentLives + amount, maxLives);
            currentArmor = maxArmorPerLife;
            
            PlayerEvents.RaiseHealthChanged(currentLives, currentArmor);

            // Debug.Log($"Life healed. Lives: {currentLives}, Armor reset to: {currentArmor}");
        }

        /// <summary>
        /// Called when player runs out of lives.
        /// </summary>
        private void HandleDeath()
        {
            PlayerEvents.RaisePlayerDeath();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(DeathSequence());
        }
        
        private IEnumerator DeathSequence()
        {
            // Disable input
            if (InputManager.Instance != null && InputManager.Instance.Controls != null)
            {
                InputManager.Instance.DisableAllControls();
                // Debug.Log("Player input disabled.");
            }

            // Play animation
            if (animator != null)
            {
                animator.SetTrigger(deathTrigger);
                // Debug.Log("Death animation triggered.");
            }

            // Zoom camera
            if (cameraToZoom != null)
            {
                float t = 0f;
                while (t < zoomDuration)
                {
                    float zoom = Mathf.Lerp(originalZoom, zoomAmount, t / zoomDuration);
                    cameraToZoom.Lens.OrthographicSize = zoom;
                    t += Time.deltaTime;
                    yield return null;
                }

                cameraToZoom.Lens.OrthographicSize = zoomAmount;
                // Debug.Log("Camera zoom completed.");
            }

            // Show death UI (no buttons)
            if (deathScreen != null)
            {
                deathScreen.SetActive(true);
                // Debug.Log("Death screen activated.");
            }

            // Wait for animation end
            if (animator != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                while (!state.IsName(deathTrigger) || state.normalizedTime < 1f)
                {
                    yield return null;
                    state = animator.GetCurrentAnimatorStateInfo(0);
                }

                // Debug.Log("Death animation finished.");
            }

            // Fade in buttons (delayed)
            yield return new WaitForSeconds(buttonDelay);

            if (deathButtons != null)
            {
                deathButtons.SetActive(true);
                InputManager.Instance.EnableUIControls();
                // Debug.Log("Death buttons shown.");
            }

            Time.timeScale = 0f;
            
            // Debug.Log("Player death sequence completed. Buttons active, time frozen.");
        }

        // Getters for UI or external logic
        public int CurrentLives => currentLives;
        public int CurrentArmor => currentArmor;
        public int MaxLives => maxLives;
        public int MaxArmorPerLife => maxArmorPerLife;

        /// <summary>
        /// Debug-only method to fully restore health.
        /// </summary>
        public void RestoreAll()
        {
            currentLives = maxLives;
            currentArmor = maxArmorPerLife;
            // Debug.Log("Health fully restored (cheat/debug).");
        }
    }
}
