using UnityEngine;
using Scripts.Core.Interfaces; // Para IDamageable

namespace Scripts.Player.Core // O el namespace que uses
{
    /// <summary>
    /// Este script se coloca en GameObjects con Colliders que deben recibir daño
    /// y redirigirlo a un sistema de salud central del jugador (PlayerHealthSystem).
    /// </summary>
    public class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        [Header("Reference")]
        [Tooltip("Referencia al PlayerHealthSystem principal. Si es null, intentará encontrarlo en el Root del jugador.")]
        [SerializeField] private PlayerHealthSystem mainHealthSystem;

        private void Awake()
        {
            if (mainHealthSystem == null)
            {
                // Asumimos que este GameObject (ej. StandUpCollider) es hijo de Player_root
                // o de una estructura donde PlayerHealthSystem está en un ancestro.
                Transform root = transform.root; // Obtiene el Player_root
                if (root != null)
                {
                    mainHealthSystem = root.GetComponentInChildren<PlayerHealthSystem>(true);
                }
            }

            if (mainHealthSystem == null)
            {
                Debug.LogError($"PlayerDamageReceiver on '{gameObject.name}': No se pudo encontrar PlayerHealthSystem en la jerarquía del root. El jugador no podrá recibir daño a través de este collider.", this);
                enabled = false; // Deshabilitar si no puede funcionar
            }
        }

        public void TakeDamage(float amount)
        {
            if (mainHealthSystem != null)
            {
                // Redirigir la llamada de daño al sistema de salud principal
                mainHealthSystem.TakeDamage(amount);
                // Debug.Log($"PlayerDamageReceiver on '{gameObject.name}' redirigió {amount} de daño a PlayerHealthSystem.");
            }
            else
            {
                Debug.LogWarning($"PlayerDamageReceiver on '{gameObject.name}': mainHealthSystem es null. No se puede redirigir el daño.", this);
            }
        }
    }
}
