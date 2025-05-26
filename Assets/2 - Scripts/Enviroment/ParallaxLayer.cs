using UnityEngine;

namespace Scripts.Enviroment
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Effect Strength")]
        [Tooltip("Factor de parallax. 0 = no se mueve. <1 para fondos (más lento que la cámara). >1 para primer plano (más rápido).")]
        [SerializeField] private Vector2 parallaxFactor = new Vector2(0.5f, 0.1f);

        [Header("Infinite Scrolling")]
        [Tooltip("Habilitar scroll horizontal infinito.")]
        [SerializeField] private bool infiniteHorizontalScroll = true;
        [Tooltip("Habilitar scroll vertical infinito (menos común para fondos de cielo/montañas).")]
        [SerializeField] private bool infiniteVerticalScroll = false;
        
        private Transform _cameraTransform;
        private Vector3 _lastCameraPosition;
        private float _spriteWidth; // Ancho efectivo del sprite para el reposicionamiento
        private float _spriteHeight; // Alto efectivo del sprite para el reposicionamiento

        void Start()
        {
            if (UnityEngine.Camera.main != null)
            {
                _cameraTransform = UnityEngine.Camera.main.transform;
                _lastCameraPosition = _cameraTransform.position;
            }
            else
            {
                Debug.LogError("ParallaxLayer: No Main Camera found!", this);
                enabled = false;
                return;
            }

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // Usar spriteRenderer.bounds.size para obtener el tamaño visible en el mundo,
                // ya que esto tiene en cuenta la escala del transform y el pixelsPerUnit del sprite.
                _spriteWidth = spriteRenderer.bounds.size.x;
                _spriteHeight = spriteRenderer.bounds.size.y;

                if (_spriteWidth <= 0 && infiniteHorizontalScroll)
                    Debug.LogWarning($"ParallaxLayer on {gameObject.name}: Sprite width is zero or negative, infinite horizontal scroll may not work as expected.", this);
                if (_spriteHeight <= 0 && infiniteVerticalScroll)
                    Debug.LogWarning($"ParallaxLayer on {gameObject.name}: Sprite height is zero or negative, infinite vertical scroll may not work as expected.", this);
            }
            else if (infiniteHorizontalScroll || infiniteVerticalScroll)
            {
                Debug.LogWarning($"ParallaxLayer on {gameObject.name}: Infinite scroll enabled but no SpriteRenderer with a valid sprite found. Cannot determine size for repositioning.", this);
                // Deshabilitar infinite scroll si no se puede determinar el tamaño
                infiniteHorizontalScroll = false;
                infiniteVerticalScroll = false;
            }
        }

        void LateUpdate()
        {
            if (_cameraTransform == null) return;

            Vector3 deltaCameraMovement = _cameraTransform.position - _lastCameraPosition;
            
            // Aplicar movimiento parallax
            float parallaxMoveX = deltaCameraMovement.x * parallaxFactor.x;
            float parallaxMoveY = deltaCameraMovement.y * parallaxFactor.y;
            transform.position += new Vector3(parallaxMoveX, parallaxMoveY, 0);
            
            _lastCameraPosition = _cameraTransform.position;

            // Lógica para Infinite Scrolling Horizontal
            if (infiniteHorizontalScroll && _spriteWidth > 0)
            {
                // Distancia relativa entre el centro de la cámara y el centro de esta capa parallax
                float relativeCamDistX = _cameraTransform.position.x - transform.position.x;

                // Si la cámara se ha movido más de la mitad del ancho del sprite hacia la derecha de esta capa
                if (relativeCamDistX > _spriteWidth / 2f)
                {
                    transform.position += new Vector3(_spriteWidth, 0, 0); // Mover la capa un ancho de sprite a la derecha
                }
                // Si la cámara se ha movido más de la mitad del ancho del sprite hacia la izquierda de esta capa
                else if (relativeCamDistX < -_spriteWidth / 2f)
                {
                    transform.position -= new Vector3(_spriteWidth, 0, 0); // Mover la capa un ancho de sprite a la izquierda
                }
            }

            // Lógica para Infinite Scrolling Vertical
            if (infiniteVerticalScroll && _spriteHeight > 0)
            {
                float relativeCamDistY = _cameraTransform.position.y - transform.position.y;
                if (relativeCamDistY > _spriteHeight / 2f)
                {
                    transform.position += new Vector3(0, _spriteHeight, 0);
                }
                else if (relativeCamDistY < -_spriteHeight / 2f)
                {
                    transform.position -= new Vector3(0, _spriteHeight, 0);
                }
            }
        }
    }
}
