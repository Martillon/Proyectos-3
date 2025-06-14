using UnityEngine;
using Scripts.Core.Interfaces;

namespace Scripts.Environment.Hazards
{
    /// <summary>
    /// A trigger volume that applies damage or instakills entities that enter it.
    /// It interacts with any object that implements IDamageable or IInstakillable.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, this zone will attempt to use the IInstakillable interface on objects.")]
        [SerializeField] private bool isInstakill = true;
        [Tooltip("Damage to apply if isInstakill is false, or as a fallback if the object is not IInstakillable.")]
        [SerializeField] private float damageAmount = 9999f;

        private void Awake()
        {
            // Ensure the collider is a trigger.
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Search for interfaces on the root of the object that entered the trigger.
            // This ensures we get the main controller (e.g., Player_Root, Enemy_Root)
            // even if a child collider (like a foot) enters the zone.
            Transform targetRoot = other.transform.root;

            if (isInstakill)
            {
                if (targetRoot.TryGetComponent<IInstakillable>(out var instakillable))
                {
                    instakillable.ApplyInstakill();
                    return; // Done
                }
            }
            
            // If not instakill, or as a fallback, try to apply standard damage.
            if (targetRoot.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damageAmount);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw a semi-transparent red box to visualize the zone in the editor.
            if (TryGetComponent<BoxCollider2D>(out var boxCollider))
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
        }
#endif
    }
}
