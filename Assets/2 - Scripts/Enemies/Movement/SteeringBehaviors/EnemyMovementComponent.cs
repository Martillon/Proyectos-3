using Scripts.Enemies.Core;
using UnityEngine;
using Scripts.Enemies.Movement.SteeringBehaviors;

namespace Scripts.Enemies.Movement
{
    public class EnemyMovementComponent : MonoBehaviour
    {
        private Rigidbody2D rb;
        private EnemyAIController aiController;
        private Collider2D col; // Usaremos este para obtener las dimensiones
        private ISteeringBehavior2D _activeSteeringBehavior;

        [Header("Ground & Edge Detection")]
        [Tooltip("Layer mask defining what is considered ground or an edge/wall.")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Offset Y desde el centro del collider para el inicio del rayo de suelo (usualmente negativo para ir hacia abajo desde el centro, o ajustado si el pivote está en los pies).")]
        [SerializeField] private float groundCheckRayOriginYOffset = 0f; // Ajustar si el pivote no está en el centro exacto del collider
        [Tooltip("Distance downwards from the origin point to check for ground.")]
        [SerializeField] private float groundCheckDistance = 0.5f;

        [Tooltip("Horizontal offset from the enemy's center for the 'front' whisker used for edge detection.")]
        [SerializeField] private float edgeWhiskerHorizontalOffset = 0.5f;
        [Tooltip("Vertical offset from the enemy's center for the 'front' whisker origin Y (similar a groundCheckRayOriginYOffset).")]
        [SerializeField] private float edgeWhiskerOriginYOffset = 0f;
        [Tooltip("Distance downwards for the edge detection whisker.")]
        [SerializeField] private float edgeCheckDownDistance = 1.0f;

        [Tooltip("Horizontal offset from the enemy's center for wall detection raycasts (debería ser un poco más que la mitad del ancho del collider).")]
        [SerializeField] private float wallCheckRayHorizontalOffset = 0.3f; // Renombrado para claridad y consistencia
        [Tooltip("Length of the raycast to detect walls.")]
        [SerializeField] private float wallCheckDistance = 0.1f;


        public bool IsGrounded { get; private set; }
        public bool IsNearEdge { get; private set; }
        public bool IsNearWall { get; private set; }
        public bool CurrentFacingRight => aiController != null ? aiController.IsFacingRight : true;

        private void Awake()
        {
            rb = GetComponentInParent<Rigidbody2D>();
            aiController = GetComponentInParent<EnemyAIController>();
            col = GetComponentInParent<Collider2D>(); // Obtener el collider del Root

            if (rb == null) Debug.LogError("EnemyMovementComponent: Rigidbody2D not found on parent!", this);
            if (aiController == null) Debug.LogError("EnemyMovementComponent: EnemyAIController not found on parent!", this);
            if (col == null) Debug.LogError("EnemyMovementComponent: Collider2D not found on parent! Needed for raycast origins.", this);
        }

        public void SetSteeringBehavior(ISteeringBehavior2D newBehavior)
        {
            _activeSteeringBehavior = newBehavior;
        }

        private void FixedUpdate()
        {
            if (rb == null || aiController == null || col == null) return;
            if (aiController.IsDead)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }

            // Usar aiController.IsFacingRight para la dirección actual, ya que es la fuente de verdad para el flip visual
            UpdateEnvironmentDetection(aiController.IsFacingRight ? 1f : -1f);

            SteeringOutput2D steering = SteeringOutput2D.Zero;
            if (_activeSteeringBehavior != null && aiController.CanMove)
            {
                steering = _activeSteeringBehavior.GetSteering(this);
            }

            rb.linearVelocity = new Vector2(steering.DesiredVelocity.x, rb.linearVelocity.y);

            if (steering.ShouldOrient && aiController.CanMove)
            {
                OrientTowards(steering.DesiredVelocity.x);
            }
        }

        private void UpdateEnvironmentDetection(float facingSign) // facingSign es 1 (derecha) o -1 (izquierda)
        {
            if (col == null) return; // No se puede detectar sin un collider para obtener bounds

            // Usar col.bounds.center puede ser problemático si el objeto está rotado de formas extrañas en 3D,
            // pero para 2D y flip en X, transform.position es más fiable como base si el pivote está bien.
            // Sin embargo, para obtener las "dimensiones" del collider, bounds es útil.
            // Una mezcla: usar transform.position como el "centro" conceptual para los offsets.
            Vector2 basePosition = transform.position; // Posición del GameObject donde está este script (Movement_Logic)
                                                       // Si Movement_Logic está offseteado del Root, esto podría no ser ideal.
                                                       // Mejor obtener la posición del collider del Root.
            if (col.gameObject != gameObject) // Si el collider está en el Root y este script en un hijo
            {
                basePosition = col.transform.position; // Usar la posición del GameObject del Collider
            }


            float colliderHalfHeight = col.bounds.extents.y; // Mitad de la altura del collider
            float colliderHalfWidth = col.bounds.extents.x;  // Mitad del ancho del collider

            // IsGrounded Check
            // Origen: Ligeramente por encima de los pies, basado en el offset y la altura del collider.
            Vector2 groundRayOrigin = new Vector2(basePosition.x, basePosition.y + groundCheckRayOriginYOffset - colliderHalfHeight);
            RaycastHit2D groundHit = Physics2D.Raycast(groundRayOrigin, Vector2.down, groundCheckDistance, groundLayer);
            IsGrounded = groundHit.collider != null;

            // IsNearEdge Check
            Vector2 edgeWhiskerOrigin = new Vector2(basePosition.x + (edgeWhiskerHorizontalOffset * facingSign), basePosition.y + edgeWhiskerOriginYOffset - colliderHalfHeight);
            RaycastHit2D edgeHit = Physics2D.Raycast(edgeWhiskerOrigin, Vector2.down, edgeCheckDownDistance, groundLayer);
            IsNearEdge = edgeHit.collider == null;

            // IsNearWall Check
            // Origen: Desde el centro vertical del collider, desplazado horizontalmente.
            Vector2 wallRayOrigin = new Vector2(basePosition.x + (wallCheckRayHorizontalOffset * facingSign), basePosition.y);
            RaycastHit2D wallHit = Physics2D.Raycast(wallRayOrigin, Vector2.right * facingSign, wallCheckDistance, groundLayer);
            IsNearWall = wallHit.collider != null;
        }

        private void OrientTowards(float horizontalVelocity)
        {
            if (Mathf.Abs(horizontalVelocity) > 0.01f && aiController != null)
            {
                bool wantsToFaceRight = horizontalVelocity > 0;
                if (wantsToFaceRight != aiController.IsFacingRight)
                {
                    aiController.Flip();
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Es importante que los Gizmos se dibujen incluso si el juego no está corriendo para poder ajustar.
            // Necesitamos obtener las referencias de Awake o hacerlas públicas y asignarlas en el Inspector
            // si queremos ver Gizmos precisos antes de Play. Por ahora, asumimos que se actualizan en Play.

            // Si no está en Play, intentar obtener el collider para las dimensiones.
            Collider2D gizmoCollider = col != null ? col : GetComponentInParent<Collider2D>();
            EnemyAIController gizmoAiController = aiController != null ? aiController : GetComponentInParent<EnemyAIController>();

            if (gizmoCollider == null) return; // No se pueden dibujar gizmos sin un collider de referencia

            // Usar la posición actual del transform donde está este script, o la del collider si es diferente
            Vector2 basePos = gizmoCollider.transform.position;
            float halfHeight = gizmoCollider.bounds.extents.y;
            // float halfWidth = gizmoCollider.bounds.extents.x; // No usado directamente en los offsets de raycast aquí

            // Determinar la dirección de "enfrentamiento" para los gizmos
            // Si estamos en Play, usa el estado real. Si no, asume que mira a la derecha.
            float currentFacingSign = Application.isPlaying && gizmoAiController != null ? (gizmoAiController.IsFacingRight ? 1f : -1f) : 1f;

            // Gizmo para Ground Check
            Gizmos.color = Color.green;
            Vector2 groundOrigin = new Vector2(basePos.x, basePos.y + groundCheckRayOriginYOffset - halfHeight);
            Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);

            // Gizmo para Edge Check (Whisker Frontal)
            Gizmos.color = Color.yellow;
            Vector2 edgeOrigin = new Vector2(basePos.x + (edgeWhiskerHorizontalOffset * currentFacingSign), basePos.y + edgeWhiskerOriginYOffset - halfHeight);
            Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * edgeCheckDownDistance);
            
            // Gizmo para Wall Check (Frontal)
            Gizmos.color = Color.magenta;
            Vector2 wallOrigin = new Vector2(basePos.x + (wallCheckRayHorizontalOffset * currentFacingSign), basePos.y);
            Gizmos.DrawLine(wallOrigin, wallOrigin + (Vector2.right * currentFacingSign) * wallCheckDistance);

            // Opcional: Dibujar un pequeño círculo en los orígenes de los rayos para verlos mejor
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(groundOrigin, 0.05f);
            Gizmos.DrawWireSphere(edgeOrigin, 0.05f);
            Gizmos.DrawWireSphere(wallOrigin, 0.05f);

            // Mostrar el estado actual de las detecciones si está en Play
            if (Application.isPlaying)
            {
                if (IsGrounded) Gizmos.color = Color.green; else Gizmos.color = Color.red;
                Gizmos.DrawSphere(groundOrigin + Vector2.up * 0.2f, 0.1f); // Indicador de IsGrounded

                if (IsNearEdge) Gizmos.color = Color.yellow; else Gizmos.color = Color.blue;
                Gizmos.DrawSphere(edgeOrigin + Vector2.up * 0.2f, 0.1f);   // Indicador de IsNearEdge

                if (IsNearWall) Gizmos.color = Color.magenta; else Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(wallOrigin + Vector2.up * 0.2f, 0.1f);  // Indicador de IsNearWall
            }
        }
#endif
    }
}
