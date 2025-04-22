using Scripts.Core.Audio;
using Scripts.Core.Interfaces;
using UnityEngine;

/// <summary>
/// Heals armor any object that implements IHealarmor when triggered.
/// Supports visual and audio feedback, and optional destruction.
/// </summary>

namespace Scripts.Items.PickUps
{
    public class ArmorPickup : MonoBehaviour
    {

        [Header("Settings")] [SerializeField] private int armorAmount = 1;
        [SerializeField] private bool destroyOnHeal = true;

        [Header("Visual & Audio Feedback")] [SerializeField]
        private GameObject pickupVFX;

        [SerializeField] private Sounds pickupSound;
        [SerializeField] private AudioSource audioSource;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out IHealArmor armorHeal)) return;

            armorHeal.HealArmor(armorAmount);

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
