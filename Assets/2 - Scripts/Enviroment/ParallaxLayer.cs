using UnityEngine;

namespace Scripts.Enviroment
{
    public class ParallaxLayer : MonoBehaviour
    {
        private Transform _cam;
        private Vector3 _startCameraPosition;
        private float _distance;

        public GameObject[] backgroundObjects;
        private Material[] _mat;
        private float[] _backSpeed;

        private float _furthestBack;
        
        [Header("Parallax Settings")]
        [Tooltip("Distance from the camera to the furthest background layer.")]
        [SerializeField] private float parallaxSpeed = 10f;

        void Start()
        {
            _cam = UnityEngine.Camera.main.transform;
            _startCameraPosition = _cam.position;
            
            int backgroundCount = transform.childCount;
            _mat = new Material[backgroundCount];
            _backSpeed = new float[backgroundCount];
            backgroundObjects = new GameObject[backgroundCount];
            
            for(int i = 0; i < backgroundCount; i++)
            {
                backgroundObjects[i] = transform.GetChild(i).gameObject;
                _mat[i] = backgroundObjects[i].GetComponent<Renderer>().material;
            }
            
            BackSpeedCalculate(backgroundCount);
        }
        
        void BackSpeedCalculate(int backgroundCount)
        {
            for (int i = 0; i < backgroundCount; i++)
            {
                if ((backgroundObjects[i].transform.position.z - _cam.position.z) > _furthestBack)
                {
                    _furthestBack = backgroundObjects[i].transform.position.z - _cam.position.z;
                }
            }
            
            for (int i = 0; i < backgroundCount; i++)
            {
                _backSpeed[i] = 1 - (backgroundObjects[i].transform.position.z - _cam.position.z);
            }
        }

        void LateUpdate()
        {
            _distance = _cam.position.x - _startCameraPosition.x;
            
            transform.position = new Vector3(_cam.position.x - 1, transform.position.y, -5);
            
            for ( int i=0; i < backgroundObjects.Length; i++)
            {
                float speed = _backSpeed[i] * parallaxSpeed;
                
                _mat[i].SetTextureOffset("_MainTex", new Vector2(_distance, 0) * speed);
            }
        }
    }
}
