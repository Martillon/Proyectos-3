using UnityEngine;

namespace Scripts.Environment.Visuals
{
    /// <summary>
    /// Creates a parallax scrolling effect for background layers.
    /// This script should be on a parent object, with each child being a background layer
    /// that has a tiling material.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        // A simple class to hold settings for each layer.
        // This will show up nicely in the Inspector.
        [System.Serializable]
        public class ParallaxLayer
        {
            [Tooltip("The Transform of the GameObject for this layer (e.g., the Quad with the background material).")]
            public Transform layerTransform;
            [Tooltip("The speed of this layer relative to the camera. 0 = stays with camera. 1 = moves with foreground. Values between 0 and 1 move slower than the camera. Values greater than 1 move faster (for foreground parallax).")]
            public float parallaxSpeed;

            // Private fields for internal calculations
            [HideInInspector] public Vector3 initialPosition;
            [HideInInspector] public Material material;
        }

        [Header("Core Settings")]
        [Tooltip("The main camera that the parallax effect will follow.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Layers")]
        [Tooltip("The list of all background layers to be controlled by this script.")]
        [SerializeField] private ParallaxLayer[] layers;

        private void Start()
        {
            if (!cameraTransform)
            {
                if (UnityEngine.Camera.main) cameraTransform = UnityEngine.Camera.main.transform;
                else 
                {
                    Debug.LogError("ParallaxController: No camera assigned and no main camera found in the scene.", this);
                    return;
                }
            }

            // Store the initial position of each layer and get its material.
            foreach (var layer in layers)
            {
                if (layer.layerTransform == null) continue;
                
                layer.initialPosition = layer.layerTransform.position;
                
                var renderer = layer.layerTransform.GetComponent<Renderer>();
                if(renderer != null)
                {
                    // We create a new material instance to ensure each layer can offset
                    // its texture independently without affecting other objects using the same material.
                    layer.material = renderer.material;
                }
                else
                {
                     Debug.LogWarning($"Parallax layer '{layer.layerTransform.name}' is missing a Renderer component.", this);
                }
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || layers == null) return;

            // Calculate the camera's travel distance from its starting point.
            Vector3 cameraTravelDistance = cameraTransform.position - Vector3.zero; // Assuming camera starts at or near origin

            foreach (var layer in layers)
            {
                if (layer.layerTransform == null || layer.material == null) continue;

                // Calculate the parallax displacement. This is how far the layer should move.
                float parallaxDisplacementX = cameraTravelDistance.x * layer.parallaxSpeed;
                
                // --- PIXEL PERFECT ADJUSTMENT ---
                // This is the magic for pixel art. We need to know our "Pixels Per Unit" setting.
                // Let's assume a common value like 64. You can make this a public field.
                float pixelsPerUnit = 64.0f; 
                // Calculate how many "world units" one pixel represents.
                float pixelUnitSize = 1.0f / pixelsPerUnit;
                // Round the displacement to the nearest pixel-perfect increment.
                parallaxDisplacementX = (Mathf.Round(parallaxDisplacementX / pixelUnitSize) * pixelUnitSize);
                // --- END OF PIXEL PERFECT ADJUSTMENT ---

                // Move the layer transform itself for the primary parallax effect.
                // This handles the Y-axis movement automatically.
                float newY = layer.initialPosition.y + (cameraTravelDistance.y * layer.parallaxSpeed);
                layer.layerTransform.position = new Vector3(layer.initialPosition.x + parallaxDisplacementX, newY, layer.layerTransform.position.z);

                // Calculate the texture offset for infinite looping.
                // This creates the illusion of an endless background by scrolling the texture itself.
                float textureOffset = (parallaxDisplacementX / layer.layerTransform.localScale.x) % 1;
                layer.material.mainTextureOffset = new Vector2(textureOffset, 0);
            }
        }
    }
}
