using UnityEngine;
using Scripts.Core; // Para GameConstants
using Scripts.Core.Interfaces;
using Scripts.Player.Core; // Para IDamageable

namespace Scripts.Environment // Or Scripts.Hazards, Scripts.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        [Header("Death Zone Settings")]
        [Tooltip("If true, entering this zone will attempt to instakill the entity. If false, it will apply standard damage.")]
        [SerializeField] private bool isInstakill = true;
        [Tooltip("Amount of 'damage' to apply if not instakill.")]
        [SerializeField] private float damageAmountIfNotInstakill = 9999f; // Daño alto para asegurar muerte/pérdida de vida

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null) { /* ... error ... */ enabled = false; return; }
            if (!col.isTrigger) { col.isTrigger = true; }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // No filtramos por tag aquí, dejamos que las interfaces decidan si el objeto puede ser afectado.
            // Podrías añadir un filtro de layer si quieres que la DeathZone solo afecte a ciertas layers (ej. "Player", "Enemies").

            Debug.Log($"DeathZone: OnTriggerEnter2D with {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

            if (isInstakill)
            {
                // Intentar obtener IInstakillable del objeto que colisionó o de sus padres
                IInstakillable instakillableEntity = other.GetComponentInParent<IInstakillable>();
                // GetComponentInParent también revisa el componente en el mismo objeto 'other' primero.

                if (instakillableEntity != null)
                {
                    Debug.Log($"DeathZone: Found IInstakillable on {other.gameObject.name}. Applying Instakill.");
                    instakillableEntity.ApplyInstakill();
                }
                else
                {
                    // Fallback si no es IInstakillable pero podría ser IDamageable (menos común para instakill zones)
                    IDamageable damageableEntity = other.GetComponentInParent<IDamageable>();
                    if (damageableEntity != null)
                    {
                        Debug.LogWarning($"DeathZone: {other.gameObject.name} is not IInstakillable. Applying massive damage as fallback for instakill.");
                        damageableEntity.TakeDamage(damageAmountIfNotInstakill * 10); // Daño muy masivo
                    }
                    else
                    {
                         Debug.Log($"DeathZone: {other.gameObject.name} is neither IInstakillable nor IDamageable. No action taken for instakill.");
                    }
                }
            }
            else // No es instakill, aplicar daño normal
            {
                IDamageable damageableEntity = other.GetComponentInParent<IDamageable>();
                if (damageableEntity != null)
                {
                    Debug.Log($"DeathZone: Applying standard damage ({damageAmountIfNotInstakill}) to {other.gameObject.name}.");
                    damageableEntity.TakeDamage(damageAmountIfNotInstakill);
                }
                else
                {
                    Debug.Log($"DeathZone: {other.gameObject.name} is not IDamageable. No standard damage applied.");
                }
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
