using UnityEngine;

namespace Scripts.Environment.Visuals
{
    /// <summary>
    /// Manages parallax scrolling for multiple layers based on camera movement.
    /// This component should be placed on a parent object, with each parallax layer as a child.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera cam;
        
        [SerializeField] private Transform subject;
        
        Vector2 startPosition;

        private float startZ;
        
        private Vector2 travel => (Vector2)cam.transform.position - startPosition;
        private float distanceFromSubject => transform.position.z - subject.transform.position.z;
        float clippingPlane => (cam.transform.position.z + (distanceFromSubject > 0 ? cam.farClipPlane : cam.nearClipPlane));

        private float parallaxFactor => Mathf.Abs(distanceFromSubject) / clippingPlane;
        
        public void Start()
        {
            if (cam == null)
            {
                cam = UnityEngine.Camera.main;
            }

            startPosition = transform.position;
            startZ = transform.position.z;
        }

        public void Update()
        {
            Vector2 newPos = startPosition + travel * parallaxFactor;
            transform.position = new Vector3(newPos.x, newPos.y, startZ);
        }
    }
}
