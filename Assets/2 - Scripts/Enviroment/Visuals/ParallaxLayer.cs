using UnityEngine;

namespace Scripts.Environment.Visuals
{
    /// <summary>
    /// Creates a parallax scrolling effect for background layers.
    /// This script should be on a parent object, with each child being a background layer
    /// that has a tiling material.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("The main camera to track for the parallax effect.")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [Tooltip("How much the parallax effect is scaled. Higher values mean faster movement relative to the camera.")]
        [SerializeField] private float parallaxEffectMultiplier = 0.5f;

        private Transform _cameraTransform;
        private Vector3 _lastCameraPosition;
        private float _textureUnitSizeX;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
            if (mainCamera == null)
            {
                Debug.LogError("Parallax: Main Camera not found!", this);
                enabled = false;
                return;
            }
            
            _cameraTransform = mainCamera.transform;
            _lastCameraPosition = _cameraTransform.position;
            
            // Calculate texture unit size based on the first child's sprite.
            // This assumes all layers use similarly scaled sprites.
            Sprite sprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (sprite != null)
            {
                Texture2D texture = sprite.texture;
                _textureUnitSizeX = texture.width / sprite.pixelsPerUnit;
            }
            else
            {
                Debug.LogWarning("Parallax: Could not find a sprite to determine texture size. Parallax may not loop correctly.", this);
                _textureUnitSizeX = 20; // Fallback value
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;

            // Calculate how much the camera has moved since the last frame.
            Vector3 deltaMovement = _cameraTransform.position - _lastCameraPosition;
            
            // Move the parallax layer itself.
            transform.position += new Vector3(deltaMovement.x * parallaxEffectMultiplier, deltaMovement.y * parallaxEffectMultiplier, 0);
            
            _lastCameraPosition = _cameraTransform.position;

            // Check if we need to reposition the background for infinite scrolling.
            if (Mathf.Abs(_cameraTransform.position.x - transform.position.x) >= _textureUnitSizeX)
            {
                float offsetPositionX = (_cameraTransform.position.x - transform.position.x) % _textureUnitSizeX;
                transform.position = new Vector3(_cameraTransform.position.x - offsetPositionX, transform.position.y);
            }
        }
    }
}
