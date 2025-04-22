using UnityEngine;
using Scripts.Core;
using Scripts.Core.Audio;
using Scripts.Core.Interfaces;

namespace Scripts.Checkpoints
{
    /// <summary>
    /// When triggered by the player, this checkpoint becomes the current respawn point.
    /// Optionally restores health or plays VFX/SFX.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool healOnActivate = false;
        [SerializeField] private int healLifeAmount = 1;
        [SerializeField] private int healArmorAmount = 0;

        [Header("Feedback")]
        [SerializeField] private GameObject activateVFX;
        [SerializeField] private Sounds activateSFX;
        [SerializeField] private AudioSource audioSource;

        private static Transform currentRespawnPoint;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            currentRespawnPoint = transform;

            if (healOnActivate)
            {
                if (other.TryGetComponent(out IHealLife life))
                {
                    life.HealLife(healLifeAmount);
                }

                if (other.TryGetComponent(out IHealArmor armor))
                {
                    armor.HealArmor(healArmorAmount);
                }
            }

            if (activateVFX != null)
                Instantiate(activateVFX, transform.position, Quaternion.identity);

            if (activateSFX != null && audioSource != null)
                activateSFX.Play(audioSource);

            // Debug.Log("Checkpoint activated at position: " + transform.position);
        }

        /// <summary>
        /// Returns the current respawn position, or Vector3.zero if none.
        /// </summary>
        public static Vector3 GetRespawnPosition()
        {
            return currentRespawnPoint != null ? currentRespawnPoint.position : Vector3.zero;
        }
    }
}
