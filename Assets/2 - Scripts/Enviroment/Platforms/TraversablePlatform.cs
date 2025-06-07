using UnityEngine;
using System.Collections;
using Scripts.Environment.Interfaces;
using Scripts.Core;

namespace Scripts.Environment.Platforms
{
    /// <summary>
    /// A platform that the player can drop through. It works by temporarily switching
    /// its layer to one that does not collide with the player.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TraversablePlatform : MonoBehaviour, ITraversablePlatform
    {
        // The layer name is now defined in GameConstants to ensure consistency.
        private int _originalLayer;
        private int _playerIgnoreLayer;
        private Coroutine _layerChangeCoroutine;

        private void Awake()
        {
            // Cache the layer integer values for performance.
            _originalLayer = gameObject.layer;
            _playerIgnoreLayer = LayerMask.NameToLayer(GameConstants.PlayerNonCollidingLayerName);

            if (_playerIgnoreLayer == -1)
            {
                Debug.LogError($"TraversablePlatform on '{name}': The layer '{GameConstants.PlayerNonCollidingLayerName}' is not defined in the Tags and Layers settings. Drop-through will not work.", this);
                enabled = false; // Disable the component if misconfigured.
            }
        }

        /// <summary>
        /// Initiates the process to make the platform non-collidable for a set duration.
        /// </summary>
        /// <param name="duration">How long the platform should ignore the player.</param>
        public void BecomeTemporarilyNonCollidable(float duration)
        {
            if (!enabled) return;

            if (_layerChangeCoroutine != null)
            {
                StopCoroutine(_layerChangeCoroutine);
                // If interrupted, immediately restore the original layer.
                gameObject.layer = _originalLayer;
            }
            _layerChangeCoroutine = StartCoroutine(ChangeLayerRoutine(duration));
        }

        private IEnumerator ChangeLayerRoutine(float duration)
        {
            gameObject.layer = _playerIgnoreLayer;
            yield return new WaitForSeconds(duration);
            // Check if the object still exists before changing the layer back.
            if (this != null && gameObject != null)
            {
                gameObject.layer = _originalLayer;
            }
            _layerChangeCoroutine = null;
        }
    }
}