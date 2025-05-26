// En Scripts/Levels/LevelExit.cs
using UnityEngine;
using Scripts.Core; // Para GameConstants, InputManager
using Scripts.Player.Core;
using Scripts.Player.Visuals; // Para PlayerEvents
using UnityEngine.SceneManagement; // Para SceneManager

namespace Scripts.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        private bool hasBeenTriggered = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"LevelExit on '{gameObject.name}': Collider is not set to 'Is Trigger'. Forcing it.", this);
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasBeenTriggered || !other.CompareTag(GameConstants.PlayerTag))
            {
                return;
            }

            hasBeenTriggered = true;
            Debug.Log($"LevelExit: Player '{other.name}' entered exit for level '{SceneManager.GetActiveScene().name}'.");

            // 1. Deshabilitar input del jugador
            InputManager.Instance?.DisableAllControls(); // O DisablePlayerControls si quieres que la UI siga funcionando para algo más

            // 2. Detener movimiento físico del jugador
            Rigidbody2D playerRb = GetPlayerRigidbody(other);
            if (playerRb != null)
            {
                Debug.Log($"LevelExit: Zeroing player Rigidbody velocity. Was: {playerRb.linearVelocity}");
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                // Opcional: Podrías intentar forzar al jugador a una posición "grounded" aquí si es necesario
                // para la pose de victoria, pero suele ser mejor que la animación no lo requiera estrictamente.
            }

            // 3. Marcar nivel como completado
            string currentLevelIdentifier = SceneManager.GetActiveScene().name;
            LevelProgressionManager.Instance?.CompleteLevel(currentLevelIdentifier);

            // 4. Lanzar el evento global de nivel completado
            // La UI y otros sistemas reaccionarán a esto.
            PlayerEvents.RaiseLevelCompleted(currentLevelIdentifier);

            // Opcional: Desactivar este objeto para que no se pueda triggerear de nuevo en esta sesión del nivel
            // gameObject.SetActive(false); 
        }

        // Helper para obtener el Rigidbody del jugador de forma más robusta
        private Rigidbody2D GetPlayerRigidbody(Collider2D playerCollider)
        {
            if (playerCollider.attachedRigidbody != null) return playerCollider.attachedRigidbody;
            return playerCollider.GetComponentInParent<Rigidbody2D>();
        }
    }
}