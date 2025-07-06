using UnityEngine;

namespace Scripts.Environment.Visuals
{
    /// <summary>
    /// Manages parallax scrolling for multiple layers based on camera movement.
    /// This component should be placed on a parent object, with each parallax layer as a child.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform transform;
            [Range(0f, 1f)]
            public float parallaxFactor;
        }

        [Header("References")]
        [Tooltip("The main camera the parallax effect will follow.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Layers")]
        [Tooltip("The layers to be affected by the parallax effect. Order does not matter.")]
        [SerializeField] private ParallaxLayer[] layers;

        // The camera's position in the previous frame.
        private Vector3 _lastCameraPosition;
        // The width of each layer's visual element (Quad/Sprite).
        private float[] _layerWidths;

        private void Start()
        {
            // Default to the main camera if none is assigned
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                if (UnityEngine.Camera.main != null) cameraTransform = UnityEngine.Camera.main.transform;
            }

            if (cameraTransform == null)
            {
                Debug.LogError("ParallaxController requires a camera to function.", this);
                enabled = false;
                return;
            }

            _lastCameraPosition = cameraTransform.position;

            // Store the width of each layer for the wrapping calculation.
            // This assumes each layer has a Renderer component (like a SpriteRenderer or MeshRenderer on a Quad).
            _layerWidths = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                Renderer renderer = layers[i].transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    _layerWidths[i] = renderer.bounds.size.x;
                }
                else
                {
                    Debug.LogWarning($"Parallax layer '{layers[i].transform.name}' does not have a Renderer component. Wrapping may not work correctly.", layers[i].transform);
                }
            }
        }

        private void LateUpdate()
        {
            // Calculate how much the camera has moved since the last frame
            Vector3 deltaMovement = cameraTransform.position - _lastCameraPosition;

            // Move each layer and handle wrapping
            for (int i = 0; i < layers.Length; i++)
            {
                ParallaxLayer layer = layers[i];
                if (layer.transform == null) continue;

                // Move the layer by a fraction of the camera's movement
                Vector3 parallaxMovement = deltaMovement * layer.parallaxFactor;
                layer.transform.position += parallaxMovement;

                // --- Wrapping Logic ---
                // If the layer has moved more than its width away from the camera, it needs to be repositioned
                // to create an infinite scrolling effect.
                float layerWidth = _layerWidths[i];
                if (layerWidth > 0 && Mathf.Abs(cameraTransform.position.x - layer.transform.position.x) >= layerWidth)
                {
                    float offsetPositionX = (cameraTransform.position.x - layer.transform.position.x) % layerWidth;
                    layer.transform.position = new Vector3(cameraTransform.position.x - offsetPositionX, layer.transform.position.y);
                }
            }

            // Update the last camera position for the next frame
            _lastCameraPosition = cameraTransform.position;
        }
    }
}
