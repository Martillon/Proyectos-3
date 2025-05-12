// --- START OF FILE DeathZone.cs ---
using UnityEngine;
using Scripts.Core; // For GameConstants
using Scripts.Player.Core; // For PlayerHealthSystem
// No es estrictamente necesario acceder a PlayerHealthSystem directamente si usamos una interfaz o un método más genérico.

namespace Scripts.Environment // Or Scripts.Hazards, Scripts.Gameplay
{
    /// <summary>
    /// Represents a zone that causes the player to "die" (lose a life or instakill) upon entry.
    /// Typically placed below the playable area to handle falls.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        [Header("Death Zone Settings")]
        [Tooltip("If true, entering this zone will instantly trigger the player's final death sequence, regardless of remaining lives. If false, it will behave like taking a hit (lose armor/life).")]
        [SerializeField] private bool isInstakill = false;

        [Tooltip("Optional: Amount of 'damage' to apply if not instakill. PlayerHealthSystem might interpret any damage as '1 hit'.")]
        [SerializeField] private float damageAmountIfNotInstakill = 1; // PHS typically treats any hit as 1 armor/life loss

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogError($"DeathZone on '{gameObject.name}' is missing a Collider2D component.", this);
                enabled = false; // Disable script if no collider
                return;
            }
            if (!col.isTrigger)
            {
                // Debug.LogWarning($"DeathZone on '{gameObject.name}': Collider2D is not set to 'Is Trigger'. Forcing it to true for proper functionality.", this); // Uncomment for debugging
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(GameConstants.PlayerTag))
            {
                // Debug.Log($"DeathZone: Player '{other.name}' entered.", this); // Uncomment for debugging
                PlayerHealthSystem playerHealth = other.GetComponent<PlayerHealthSystem>();

                if (playerHealth != null)
                {
                    if (isInstakill)
                    {
                        // To trigger an instakill, we need a way for PlayerHealthSystem
                        // to go directly to the final death sequence.
                        // Let's add a new public method to PlayerHealthSystem for this.
                        playerHealth.TriggerInstakill();
                        // Debug.Log($"DeathZone: Instakill triggered for Player '{other.name}'.", this); // Uncomment for debugging
                    }
                    else
                    {
                        // Behave like a normal hit: lose armor, then life, then game over.
                        // PlayerHealthSystem's TakeDamage will handle the logic.
                        playerHealth.TakeDamage(damageAmountIfNotInstakill); // Amount might be ignored by PHS if it's "1 hit" logic
                        // Debug.Log($"DeathZone: Standard damage/life loss triggered for Player '{other.name}'.", this); // Uncomment for debugging
                    }
                }
                // else Debug.LogWarning($"DeathZone: Player '{other.name}' entered, but no PlayerHealthSystem component found.", this); // Uncomment for debugging
            }
        }

#if UNITY_EDITOR
        // Optional: Gizmo to visualize the death zone in the editor
        private void OnDrawGizmos()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
                if (col is BoxCollider2D box)
                {
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    Gizmos.DrawCube(box.offset, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    Gizmos.DrawSphere(circle.offset, circle.radius);
                }
                // Add more for PolygonCollider2D, EdgeCollider2D if needed
                else {
                     Gizmos.DrawCube(Vector3.zero, col.bounds.size); // Fallback to bounds
                }
            }
        }
        private void OnDrawGizmosSelected()
        {
            // Could draw a more opaque version when selected
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // More opaque red
                 if (col is BoxCollider2D box)
                {
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    Gizmos.DrawCube(box.offset, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    Gizmos.DrawSphere(circle.offset, circle.radius);
                }
                else {
                     Gizmos.DrawCube(Vector3.zero, col.bounds.size);
                }
            }
        }
#endif
    }
}
// --- END OF FILE DeathZone.cs ---
