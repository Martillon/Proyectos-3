using UnityEngine;
using System.Collections;
using Scripts.Environment.Interfaces; // For ITraversablePlatform
using Scripts.Core; // For GameConstants

namespace Scripts.Environment.Platforms 
{
    /// <summary>
    /// Makes this platform traversable from above by temporarily changing its layer
    /// to one that does not collide with the player.
    /// Implements ITraversablePlatform.
    /// </summary>
    [RequireComponent(typeof(Collider2D))] // Collider is still needed for normal interaction
    public class TraversablePlatform : MonoBehaviour, ITraversablePlatform
    {
        [Header("Layer Configuration")]
        [Tooltip("Name of the layer to switch to temporarily, which should NOT collide with the Player layer. Define this layer in Unity's Tags and Layers settings and configure Physics2D matrix.")]
        [SerializeField] private string playerNonCollidingLayerName = "IgnorePlayer"; // Example name from GameConstants if defined, or direct string
        
        private int originalLayer;
        private int playerIgnoreLayerValue = -1; // Cached layer value for efficiency

        private Coroutine temporaryLayerChangeCoroutine;
        private Collider2D _platformCollider; // Sólo para asegurar que existe, no lo desactivaremos

        void Awake()
        {
            _platformCollider = GetComponent<Collider2D>();
            if (_platformCollider == null)
            {
                Debug.LogError($"TraversablePlatform on '{gameObject.name}' is missing a Collider2D component. This script cannot function.", this);
                enabled = false;
                return;
            }
            // Aunque no lo desactivemos, el PlatformEffector2D y el Collider2D siguen siendo necesarios para
            // el comportamiento normal de la plataforma (pararse encima, saltar a través hacia arriba).

            originalLayer = gameObject.layer; // Store the original layer of this platform GameObject

            // Convert layer name to layer value once
            playerIgnoreLayerValue = LayerMask.NameToLayer(playerNonCollidingLayerName);
            if (playerIgnoreLayerValue == -1) // Layer name not found in Tags and Layers settings
            {
                Debug.LogError($"TraversablePlatform on '{gameObject.name}': Layer named '{playerNonCollidingLayerName}' not found. Please define it in Tags and Layers and configure Physics2D settings. Drop-through will not work correctly.", this);
                // No deshabilitar el script, pero el drop-through no funcionará como cambio de capa.
                // Podríamos añadir un fallback a desactivar el collider aquí si se prefiere.
            }
            
            playerIgnoreLayerValue = LayerMask.NameToLayer(playerNonCollidingLayerName);
            if (playerIgnoreLayerValue == -1)
            {
                Debug.LogError($"TraversablePlatform on '{gameObject.name}': Layer named '{playerNonCollidingLayerName}' (valor en Inspector) NOT FOUND in Tags and Layers. Drop-through by layer change will fail.", this);
            }
            else
            {
                Debug.Log($"TraversablePlatform on '{gameObject.name}': Target layer '{playerNonCollidingLayerName}' found. Value: {playerIgnoreLayerValue}. Original layer: {LayerMask.LayerToName(originalLayer)} ({originalLayer})", this);
            }
        }

        /// <summary>
        /// Makes the platform temporarily non-collidable with the player by changing its layer.
        /// </summary>
        /// <param name="duration">How long the platform should remain in the non-colliding layer.</param>
        public void BecomeTemporarilyNonCollidable(float duration)
        {
            Debug.Log($"TRAVERSABLE_PLATFORM '{gameObject.name}': BecomeTemporarilyNonCollidable CALLED. Enabled: {enabled}, TargetLayerValid: {playerIgnoreLayerValue != -1}", this);
            if (!enabled || playerIgnoreLayerValue == -1) // Do nothing if script disabled or target layer is invalid
            {
                if (playerIgnoreLayerValue == -1)
                    Debug.LogWarning($"TraversablePlatform '{gameObject.name}': Called BecomeTemporarilyNonCollidable but target layer '{playerNonCollidingLayerName}' is invalid.", this);
                return;
            }

            if (temporaryLayerChangeCoroutine != null)
            {
                StopCoroutine(temporaryLayerChangeCoroutine);
                // Ensure layer is restored to original if interrupted
                gameObject.layer = originalLayer; 
                // Debug.Log($"TraversablePlatform '{gameObject.name}': Interrupted previous layer change, layer restored to '{LayerMask.LayerToName(originalLayer)}'.", this); // Uncomment for debugging
            }
            temporaryLayerChangeCoroutine = StartCoroutine(ChangeLayerTemporarilyCoroutine(duration));
            Debug.Log($"TRAVERSABLE_PLATFORM '{gameObject.name}': Coroutine for layer change STARTED.", this);
        }

        private IEnumerator ChangeLayerTemporarilyCoroutine(float duration)
        {
            Debug.Log($"TRAVERSABLE_PLATFORM '{gameObject.name}': Coroutine RUNNING. Target Layer Value: {playerIgnoreLayerValue}. Current GO Layer: {gameObject.layer}", this);
            gameObject.layer = playerIgnoreLayerValue;
            Debug.Log($"TRAVERSABLE_PLATFORM '{gameObject.name}': gameObject.layer attempted to be set to {playerIgnoreLayerValue}. NEW GO Layer: {gameObject.layer}. Intended Name: {playerNonCollidingLayerName}", this);

            yield return new WaitForSeconds(duration);

            if (this != null && gameObject != null) 
            {
                gameObject.layer = originalLayer;
                Debug.Log($"TRAVERSABLE_PLATFORM '{gameObject.name}': Layer restored to original '{LayerMask.LayerToName(originalLayer)}' ({gameObject.layer})", this);
            }
            temporaryLayerChangeCoroutine = null;
        }
        
        // Optional: If the platform's original layer could change dynamically (unlikely for this setup)
        // or if used with object pooling where 'Awake' isn't always called on reuse.
        // private void OnEnable()
        // {
        //     originalLayer = gameObject.layer; // Re-cache original layer in case it changed while disabled
        //     if (temporaryLayerChangeCoroutine == null && gameObject.layer == playerIgnoreLayerValue)
        //     {
        //         // If re-enabled and stuck on ignore layer with no coroutine running, reset it.
        //         gameObject.layer = originalLayer;
        //     }
        // }
    }
}