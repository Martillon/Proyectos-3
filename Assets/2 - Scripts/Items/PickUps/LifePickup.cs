using UnityEngine;
using Scripts.Core.Interfaces;
using Scripts.Core.Audio;

namespace Scripts.Items.PickUps
{
    /// <summary>
    /// Heals any object that implements IHeallife when triggered.
    /// Supports visual and audio feedback, and optional destruction.
    /// </summary>
    public class LifePickup : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int healAmount = 1;
        [SerializeField] private bool destroyOnHeal = true;

        [Header("Visual & Audio Feedback")]
        [SerializeField] private GameObject pickupVFX;
        [SerializeField] private Sounds pickupSound;
        [SerializeField] private AudioSource audioSource;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out IHealLife healable)) return;

            healable.HealLife(healAmount);

            // Play VFX at this position
            if (pickupVFX != null)
                Instantiate(pickupVFX, transform.position, Quaternion.identity);

            // Play pickup sound using centralized sound system
            if (pickupSound != null && audioSource != null)
                pickupSound.Play(audioSource);

            if (destroyOnHeal)
                Destroy(gameObject);
        }
    }
}
