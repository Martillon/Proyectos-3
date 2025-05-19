// --- START OF FILE PlayerMovement2D.cs ---

using Scripts.Core;
using System.Collections;
using Scripts.Player.Core;
using Scripts.Player.Weapons; // Necesario si vas a referenciar AimDirectionResolver
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// PlayerMovement2D
    /// Handles 2D player movement, jumping, crouching (hold-to-crouch), gravity control,
    /// wall detection, coyote time, position lock, directional input handling, and platform drop-through.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Vertical jump impulse force.")]
        [SerializeField] private float jumpForce = 10f;
        [Tooltip("Multiplier applied to gravity when the player is falling to make falls feel snappier.")]
        [SerializeField] private float fallMultiplier = 2.5f;
        [Tooltip("Short time window (in seconds) after leaving a platform where the player can still jump.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Ground Check")]
        [Tooltip("Transform representing the origin point for ground detection (usually at player's feet).")]
        [SerializeField] private Transform groundCheckOrigin;
        [Tooltip("Radius of the circle used for ground detection.")]
        [SerializeField] private float groundCheckRadius = 0.15f;
        [Tooltip("LayerMask defining what layers are considered 'Ground'.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [Tooltip("Transform representing the origin point for wall detection raycasts (usually player's center).")]
        [SerializeField] private Transform wallCheckOrigin;
        [Tooltip("Distance of the raycasts used to detect walls on either side.")]
        [SerializeField] private float wallCheckDistance = 0.1f;
        [Tooltip("LayerMask defining what layers are considered 'Walls'.")]
        [SerializeField] private LayerMask wallLayer;

        [Header("Platform Drop Settings")]
        [Tooltip("Duration (in seconds) for which collision with the dropped platform is ignored.")]
        [SerializeField] private float dropThroughTime = 0.25f;
        [Tooltip("Tag used to identify platforms that the player can drop through.")]
        [SerializeField] private string oneWayPlatformTag = "Platform";

        [Header("Crouch Settings")]
        [Tooltip("Multiplier applied to the player's collider height when crouching (e.g., 0.5 for half height).")]
        [SerializeField] private float crouchHeightMultiplier = 0.5f;
        [Tooltip("Optional: Reference to the player's fire point transform, to be adjusted during crouch.")]
        [SerializeField] private Transform firePoint;
        
        [Header("Colliders")]
        [Tooltip("GameObject containing the collider used when the player is standing.")]
        [SerializeField] private GameObject standingColliderObject;
        [Tooltip("GameObject containing the collider used when the player is crouching.")]
        [SerializeField] private GameObject crouchingColliderObject;
        
        [Header("FirePoint Positions")]
        [Tooltip("Local position of the fire point when standing.")]
        [SerializeField] private Vector3 firePointStandingLocalPos = new Vector3(0.5f, 0.3f, 0f); // Ejemplo, ajusta estos valores
        [Tooltip("Local position of the fire point when crouching.")]
        [SerializeField] private Vector3 firePointCrouchingLocalPos = new Vector3(0.5f, 0.1f, 0f);
        
        [Header("Animation & Visuals")]
        [Tooltip("Animator component for the player's body animations.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("Transform of the child GameObject that contains the player's main visuals (SpriteRenderer & Body Animator). This object will be flipped.")]
        [SerializeField] private Transform playerVisualsContainer;
        [Tooltip("Optional: GameObject representing the player's aimable arm/weapon mount. Will be handled by HandleAimableArmVisibility.")]
        [SerializeField] private GameObject aimableArmObject;
        [Tooltip("SpriteRenderer of the player's aimable arm.")]
        [SerializeField] private SpriteRenderer aimableArmSpriteRenderer;

        [Header("Aiming Components (Reference needed for arm visibility logic)")]
        [SerializeField] private AimDirectionResolver aimResolver; // <<< VERIFICACIÓN: Asegúrate que está asignado


        // --- Animator Parameter Hashes (for efficiency) ---
        private readonly int animIsMovingHash = Animator.StringToHash("isMoving");
        private readonly int animIsGroundedHash = Animator.StringToHash("isGrounded");
        private readonly int animIsCrouchingHash = Animator.StringToHash("isCrouching");
        private readonly int animDieTriggerHash = Animator.StringToHash("Die");
        private readonly int animVictoryTriggerHash = Animator.StringToHash("Victory");

        private float facingDirection = 1f;

        [Header("Debug Gizmos")]
        [SerializeField] private Color groundCheckGizmoColor = Color.green;
        [SerializeField] private Color wallCheckGizmoColor = Color.cyan;

        // Component references
        private Rigidbody2D _rb;
        private Collider2D _playerCollider; // El collider general del jugador (podría ser el mismo que _capsuleCollider)
        private CapsuleCollider2D _capsuleCollider; // Específicamente el CapsuleCollider2D si existe

        // Internal State Variables
        private Vector2 _moveInputVector;
        private bool _isGrounded;
        private bool _jumpInputRequested;
        private bool _isCrouchingLogic;
        private bool _isCrouchVisualApplied;
        private bool _isPositionLockedInput;
        private bool _isTouchingWallState;
        private bool _isDroppingFromPlatform = false;
        private float _coyoteTimeRemaining;

        // Original values
        private Vector2 _originalCapsuleSize;
        private Vector2 _originalCapsuleOffset;
        private Vector3 _originalFirePointLocalPos;
        
        private Collider2D currentActivePlayerCollider;
        
        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouchingLogic; 
        public bool IsDropping => _isDroppingFromPlatform;
        public bool IsTouchingWall => _isTouchingWallState;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // _playerCollider = GetComponent<Collider2D>(); // Aún puedes querer una referencia general si es útil
                                                         // pero para el cambio de estado, usaremos los GO específicos.

            if (_rb == null) Debug.LogError("P2DM: Rigidbody2D component not found!", this);
            // if (_playerCollider == null) Debug.LogError("P2DM: A Collider2D component is required on the root!", this); // Opcional si los colliders están solo en hijos

            if (groundCheckOrigin == null) Debug.LogError("P2DM: 'Ground Check Origin' not assigned!", this);
            if (wallCheckOrigin == null) Debug.LogError("P2DM: 'Wall Check Origin' not assigned!", this);
            if (bodyAnimator == null) Debug.LogWarning("P2DM: 'Body Animator' not assigned.", this);
            if (playerVisualsContainer == null) Debug.LogWarning("P2DM: 'Player Visuals Container' not assigned.", this);
            if (aimResolver == null) aimResolver = GetComponentInChildren<AimDirectionResolver>(true) ?? GetComponent<AimDirectionResolver>();
            if (aimableArmObject == null && aimResolver != null) Debug.LogWarning("P2DM: 'Aimable Arm Object' not assigned.", this);
            
            if (standingColliderObject == null || standingColliderObject.GetComponent<Collider2D>() == null)
                Debug.LogError("P2DM: 'Standing Collider Object' or its Collider2D is not assigned! Crouching will fail.", this);
            if (crouchingColliderObject == null || crouchingColliderObject.GetComponent<Collider2D>() == null)
                Debug.LogError("P2DM: 'Crouching Collider Object' or its Collider2D is not assigned! Crouching will fail.", this);
            
            if (standingColliderObject != null)
            {
                standingColliderObject.SetActive(true);
                currentActivePlayerCollider = standingColliderObject.GetComponent<Collider2D>();
            }
            if (crouchingColliderObject != null)
            {
                crouchingColliderObject.SetActive(false);
            }

            if(playerVisualsContainer != null) facingDirection = Mathf.Sign(playerVisualsContainer.localScale.x);
        }
        
        // ... (OnEnable, OnDisable sin cambios) ...
        private void OnEnable() { PlayerEvents.OnPlayerDeath += HandlePlayerDeathAnimation; PlayerEvents.OnLevelCompleted += HandlePlayerVictoryAnimation; }
        private void OnDisable() { PlayerEvents.OnPlayerDeath -= HandlePlayerDeathAnimation; PlayerEvents.OnLevelCompleted -= HandlePlayerVictoryAnimation; }


        private void Update()
        {
            if (InputManager.Instance?.Controls == null) return;

            HandleInput();
            PerformGroundCheck(); 
            UpdateLogicalStates(); 
            HandleJumpAndDropActions();
            
            ApplyCrouchVisualsAndCollider(); 
            HandleAimableArmVisibility();   
            UpdateBodyAnimatorParameters(); 
            HandleVisualFlip();             
        }
        
        // ... (HandleInput, UpdateLogicalStates con su Debug.Log, HandleJumpAndDropActions, HandleVisualFlip sin cambios) ...
        private void HandleInput() { Vector2 rawInput = InputManager.Instance.Controls.Player.Move.ReadValue<Vector2>(); _moveInputVector.x = Mathf.Abs(rawInput.x) > 0.1f ? Mathf.Sign(rawInput.x) : 0f; _moveInputVector.y = Mathf.Abs(rawInput.y) > 0.1f ? Mathf.Sign(rawInput.y) : 0f; _isPositionLockedInput = InputManager.Instance.Controls.Player.PositionLock.IsPressed(); }
        private void UpdateLogicalStates() { bool intendsToPressDown = _moveInputVector.y < -0.5f; _isCrouchingLogic = intendsToPressDown && _isGrounded && !_isDroppingFromPlatform; Debug.Log($"P2DM.Update :: intendsToPressDown: {intendsToPressDown}, isGrounded: {_isGrounded}, isDropping: {_isDroppingFromPlatform} => isCrouchingLogic: {_isCrouchingLogic}"); if (_isGrounded) _coyoteTimeRemaining = coyoteTime; else _coyoteTimeRemaining -= Time.deltaTime; }
        private void HandleJumpAndDropActions() { bool jumpButtonPressedThisFrame = InputManager.Instance.Controls.Player.Jump.WasPressedThisFrame(); bool intendsToPressDown = _moveInputVector.y < -0.5f; if (jumpButtonPressedThisFrame && !_isDroppingFromPlatform) { if (_isGrounded && intendsToPressDown && IsOnTaggedPlatform(oneWayPlatformTag)) { StartCoroutine(DropThroughPlatformCoroutine()); } else if (_coyoteTimeRemaining > 0f && !_isPositionLockedInput && !_isCrouchingLogic && !_isTouchingWallState) { _jumpInputRequested = true; _coyoteTimeRemaining = 0f; } } }
        private void HandleVisualFlip() { if (playerVisualsContainer == null && aimableArmSpriteRenderer == null) return; if (!_isPositionLockedInput && Mathf.Abs(_moveInputVector.x) > 0.01f) { facingDirection = _moveInputVector.x; } if (playerVisualsContainer != null) { playerVisualsContainer.localScale = new Vector3(facingDirection, 1f, 1f); } if (aimableArmSpriteRenderer != null) { aimableArmSpriteRenderer.flipY = (facingDirection < 0f); } } // <<< VERIFICACIÓN: flipY fue tu cambio


        private void FixedUpdate()
        {
            PerformWallCheck(); 
            
            Vector2 currentVelocity = _rb.linearVelocity;
            bool lockHorizontalMovement = _isPositionLockedInput || (_isCrouchingLogic && _isGrounded);
            Debug.Log($"P2DM.FixedUpdate :: lockHorizontal: {lockHorizontalMovement} (isPosLocked: {_isPositionLockedInput}, isCrouchLogic: {_isCrouchingLogic}, isGrounded: {_isGrounded})");


            if (!lockHorizontalMovement && !_isDroppingFromPlatform)
            {
                currentVelocity.x = _moveInputVector.x * moveSpeed;
            }
            else
            {
                currentVelocity.x = 0f;
                 if (lockHorizontalMovement) Debug.Log("P2DM.FixedUpdate :: Horizontal movement FORCED TO 0.");
            }

            if (_jumpInputRequested)
            {
                currentVelocity.y = jumpForce;
                _jumpInputRequested = false;
                _isGrounded = false;
            }

            if (_rb.linearVelocity.y < 0 && !_jumpInputRequested) 
            {
                currentVelocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
            
            _rb.linearVelocity = currentVelocity;
        }

        // ... (PerformGroundCheck con su Debug.Log, PerformWallCheck, IsOnTaggedPlatform sin cambios) ...
        private void PerformGroundCheck() { if (groundCheckOrigin == null) { _isGrounded = false; return; } if (_isDroppingFromPlatform) { _isGrounded = false; return; } bool previouslyGrounded = _isGrounded; _isGrounded = Physics2D.OverlapCircle(groundCheckOrigin.position, groundCheckRadius, groundLayer); if (_isGrounded != previouslyGrounded) { Debug.Log($"Player Grounded State Changed: {_isGrounded} at Time: {Time.time}, PosY: {transform.position.y}"); } } // Añadido PosY
        private void PerformWallCheck() { if (wallCheckOrigin == null) { _isTouchingWallState = false; return; } Vector2 origin = wallCheckOrigin.position; _isTouchingWallState = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer) || Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer); }
        private bool IsOnTaggedPlatform(string tag) { if (groundCheckOrigin == null || !_isGrounded) return false; Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer); foreach (Collider2D hitCollider in colliders) { if (hitCollider.CompareTag(tag) && hitCollider.GetComponent<PlatformEffector2D>() != null) { return true; } } return false; }


        private void ApplyCrouchVisualsAndCollider()
        {
            // Debug.Log($"ApplyCrouchVisuals :: Entrando. LógicaAgachado: {_isCrouchingLogic}, VisualAplicado: {_isCrouchVisualApplied}");

            if (_isCrouchingLogic && !_isCrouchVisualApplied)
            {
                // Debug.Log("ApplyCrouchVisuals :: CONDICIÓN PARA AGACHARSE CUMPLIDA. Aplicando agachado...");
                _isCrouchVisualApplied = true;
                SwitchPlayerCollider(true); // true para activar collider de agachado
            }
            else if (!_isCrouchingLogic && _isCrouchVisualApplied)
            {
                // Debug.Log("ApplyCrouchVisuals :: CONDICIÓN PARA LEVANTARSE CUMPLIDA. Intentando levantarse...");
                if (CanStandUpSafelyNow())
                {
                    // Debug.Log("ApplyCrouchVisuals :: Puede levantarse con seguridad. Revirtiendo agachado...");
                    _isCrouchVisualApplied = false;
                    SwitchPlayerCollider(false); // false para activar collider de pie
                }
                // else Debug.LogWarning("ApplyCrouchVisuals :: NO PUEDE levantarse con seguridad. Permanece agachado visualmente.");
            }
        }

        // Renombrado y con nueva lógica para activar/desactivar GameObjects
        private void SwitchPlayerCollider(bool enableCrouchCollider)
        {
            if (standingColliderObject != null)
            {
                standingColliderObject.SetActive(!enableCrouchCollider);
            }
            if (crouchingColliderObject != null)
            {
                crouchingColliderObject.SetActive(enableCrouchCollider);
            }

            // Actualizar la referencia al collider activo
            currentActivePlayerCollider = enableCrouchCollider ? 
                crouchingColliderObject?.GetComponent<Collider2D>() : 
                standingColliderObject?.GetComponent<Collider2D>();
            
            if (currentActivePlayerCollider == null)
            {
                Debug.LogError("P2DM: No active collider found after switching state!", this);
            }

            // Ajuste del FirePoint usando los nuevos campos Vector3
            if (firePoint != null)
            {
                firePoint.localPosition = enableCrouchCollider ? firePointCrouchingLocalPos : firePointStandingLocalPos;
            }
        }
        
        private void SetCrouchStateOnColliderProperties(bool applyCrouch)
        {
            // Intentar con CapsuleCollider primero
            if (_capsuleCollider != null)
            {
                if (applyCrouch)
                {
                    float newColliderHeight = _originalCapsuleSize.y * crouchHeightMultiplier;
                    _capsuleCollider.size = new Vector2(_originalCapsuleSize.x, newColliderHeight);
                    // Pivote del GO en la BASE: nuevo offset Y = nueva altura / 2
                    _capsuleCollider.offset = new Vector2(_originalCapsuleOffset.x, newColliderHeight / 2f);
                    Debug.Log($"P2DM Crouching (Capsule): New Size Y: {newColliderHeight}, New Offset Y: {newColliderHeight / 2f}. Original Offset Y was: {_originalCapsuleOffset.y}");
                }
                else // Revertir a de pie
                {
                    _capsuleCollider.size = _originalCapsuleSize;
                    _capsuleCollider.offset = _originalCapsuleOffset;
                    Debug.Log($"P2DM Standing (Capsule): Original Size Y: {_originalCapsuleSize.y}, Original Offset Y: {_originalCapsuleOffset.y}");
                }
            }
            // Fallback a BoxCollider2D si no hay CapsuleCollider
            else if (_playerCollider is BoxCollider2D boxCol)
            {
                // Asumimos que _originalCapsuleSize y _originalCapsuleOffset se populó con datos del BoxCollider en Awake
                if (applyCrouch)
                {
                    float newColliderHeight = _originalCapsuleSize.y * crouchHeightMultiplier; 
                    boxCol.size = new Vector2(_originalCapsuleSize.x, newColliderHeight);
                    float newOffsetY = (_originalCapsuleOffset.y - (_originalCapsuleSize.y / 2f)) + (newColliderHeight / 2f);
                    // ^ Esta línea es para el caso general donde _originalCapsuleOffset.y puede no ser _originalCapsuleSize.y / 2 (si el pivote del GO no estaba en la base)
                    // Si el pivote del GO está en la base, _originalCapsuleOffset.y ES _originalCapsuleSize.y / 2, entonces:
                    // newOffsetY = (_originalCapsuleSize.y / 2f - _originalCapsuleSize.y / 2f) + (newColliderHeight / 2f) = newColliderHeight / 2f;
                    // Por seguridad, si sabes que el pivote del GO está en la base, usa directamente:
                    // newOffsetY = newColliderHeight / 2f;
                    boxCol.offset = new Vector2(_originalCapsuleOffset.x, newColliderHeight / 2f); // Asumiendo pivote del GO en la base
                    Debug.Log($"P2DM Crouching (Box): New Size Y: {newColliderHeight}, New Offset Y: {newColliderHeight / 2f}. Original Offset Y was: {_originalCapsuleOffset.y}");
                }
                else
                {
                    boxCol.size = _originalCapsuleSize;
                    boxCol.offset = _originalCapsuleOffset;
                    Debug.Log($"P2DM Standing (Box): Original Size Y: {_originalCapsuleSize.y}, Original Offset Y: {_originalCapsuleOffset.y}");
                }
            }
            // else: No se pudo encontrar un collider adecuado para modificar.

            // Ajuste del FirePoint
            if (firePoint != null)
            {
                if (applyCrouch)
                {
                    // Este cálculo asume que _originalFirePointLocalPos.y es relativo al pivote del Player_Root.
                    // Si el personaje visualmente se encoge manteniendo los pies en el sitio,
                    // el firepoint debería bajar proporcionalmente.
                    firePoint.localPosition = new Vector3(
                        _originalFirePointLocalPos.x,
                        _originalFirePointLocalPos.y * crouchHeightMultiplier, // Simple escalado, ajustar si es necesario
                        _originalFirePointLocalPos.z
                    );
                }
                else
                {
                    firePoint.localPosition = _originalFirePointLocalPos;
                }
            }
        }
        
        // <<< VERIFICACIÓN: Lógica de CanStandUpSafely con Debug.Log activado >>>
        private bool CanStandUpSafelyNow()
        {
            if (!_isCrouchVisualApplied) { /*Debug.Log("CanStandUp: Already standing.");*/ return true; }
            if (standingColliderObject == null) { /* ... error ... */ return false; }

            Collider2D standColComponent = standingColliderObject.GetComponent<Collider2D>();
            if (standColComponent == null) { /* ... error ... */ return false; }

            // --- Obtener las propiedades del collider "de pie" ---
            // Esto asume que standingColliderObject está posicionado correctamente en la jerarquía
            // para que su transform.position sea el centro deseado del collider de pie si su offset local es (0,0).
            // O, si su offset local no es (0,0), TransformPoint lo calculará bien.
            Vector2 standCenterWorld;
            Vector2 standSize;
            float standAngle = standingColliderObject.transform.eulerAngles.z;
            bool isStandCapsule = false;
            CapsuleDirection2D standCapsuleDir = CapsuleDirection2D.Vertical;

            if (standColComponent is CapsuleCollider2D standCapsule)
            {
                isStandCapsule = true;
                standSize = standCapsule.size;
                standCenterWorld = standingColliderObject.transform.TransformPoint(standCapsule.offset);
                standCapsuleDir = standCapsule.direction;
            }
            else if (standColComponent is BoxCollider2D standBox)
            {
                isStandCapsule = false;
                standSize = standBox.size;
                standCenterWorld = standingColliderObject.transform.TransformPoint(standBox.offset);
            }
            else { return true; } // Tipo de collider no soportado para el chequeo

            // --- Realizar el chequeo de Overlap ---
            LayerMask obstructionLayers = groundLayer | wallLayer; // Capas que pueden obstruir
            Collider2D[] obstructions;

            // Para el chequeo, es crucial que el 'standCenterWorld' y 'standSize'
            // no hagan que el collider "de prueba" se clave demasiado en el suelo actual.
            // Si el pivote del Player_Root está en la base, y el standingColliderObject está
            // posicionado con su base también en y=0 local del Player_Root, entonces
            // standCenterWorld.y será standSize.y / 2 (relativo al Player_Root.position.y).
            // A lo mejor necesitamos levantar un poquito el centro del chequeo.
            Vector2 checkCenter = standCenterWorld + new Vector2(0, 0.01f); // <<< PEQUEÑO AJUSTE HACIA ARRIBA

            if (isStandCapsule)
            {
                obstructions = Physics2D.OverlapCapsuleAll(checkCenter, standSize, standCapsuleDir, standAngle, obstructionLayers);
            }
            else // Box
            {
                obstructions = Physics2D.OverlapBoxAll(checkCenter, standSize, standAngle, obstructionLayers);
            }
            
            if (obstructions.Length > 0)
            {
                foreach (Collider2D obstruction in obstructions)
                {
                    // Ignorar si la "obstrucción" es el collider de agachado que está a punto de desactivarse.
                    if (crouchingColliderObject != null && obstruction.gameObject == crouchingColliderObject)
                    {
                        continue; 
                    }

                    // Ignorar otros colliders que son parte integral del jugador (pero no el de agachado)
                    // Esto es delicado. Si tienes, por ejemplo, un "hurtbox" como hijo, no debería contar.
                    bool isPartOfPlayerSelf = false;
                    if (obstruction.transform == this.transform) isPartOfPlayerSelf = true; // Es el collider raíz (si lo hubiera)
                    else if (obstruction.transform.IsChildOf(this.transform))
                    {
                        // Es un hijo, pero no el crouchingColliderObject (ya filtrado) ni el standingColliderObject (que estamos probando)
                        if (obstruction.gameObject != standingColliderObject)
                            isPartOfPlayerSelf = true; 
                    }
                    if(isPartOfPlayerSelf) continue;


                    // Si la obstrucción es el suelo Y está *principalmente debajo* del jugador,
                    // podría no ser una obstrucción real para levantarse (a menos que sea un techo bajo).
                    // Este filtro es el más complicado.
                    // Por ahora, cualquier cosa en obstructionLayers que no sea parte del jugador es una obstrucción.
                    Debug.LogWarning($"P2DM: Cannot stand up, OBSTRUCTED by '{obstruction.gameObject.name}' (Layer: {LayerMask.LayerToName(obstruction.gameObject.layer)}) at {obstruction.transform.position}", obstruction.gameObject);
                    return false; 
                }
            }
            // Debug.Log("P2DM: Can stand up safely (collider switch).");
            return true;
        }

        private IEnumerator DropThroughPlatformCoroutine()
        {
            _isDroppingFromPlatform = true;
            _isCrouchingLogic = false; 
            if (_isCrouchVisualApplied) 
            {
                SwitchPlayerCollider(false); // Activa el collider de pie
                _isCrouchVisualApplied = false;
            }

            Collider2D platformColliderToIgnore = null;
            if (currentActivePlayerCollider == null) // Comprobación de seguridad
            {
                Debug.LogError("DropThroughPlatformCoroutine: currentActivePlayerCollider is null! Aborting drop.", this);
                _isDroppingFromPlatform = false;
                yield break;
            }

            Collider2D[] collidersUnderPlayer = Physics2D.OverlapCircleAll(groundCheckOrigin.position, groundCheckRadius, groundLayer);
            // ... (resto de la lógica para encontrar platformColliderToIgnore sin cambios) ...
            foreach(Collider2D col in collidersUnderPlayer) { if (col.CompareTag(oneWayPlatformTag) && col.GetComponent<PlatformEffector2D>() != null) { platformColliderToIgnore = col; break; } }
            
            if (platformColliderToIgnore != null)
            {
                Physics2D.IgnoreCollision(currentActivePlayerCollider, platformColliderToIgnore, true); // Usa el collider activo
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -0.5f); 
                yield return new WaitForSeconds(dropThroughTime);
                Physics2D.IgnoreCollision(currentActivePlayerCollider, platformColliderToIgnore, false);
            }
            else
            {
                yield return new WaitForSeconds(dropThroughTime * 0.5f); 
            }
            _isDroppingFromPlatform = false;
        }
        
        // --- Animation and Arm Visibility ---
        private void UpdateBodyAnimatorParameters()
        {
            if (bodyAnimator == null) return;
            bodyAnimator.SetBool(animIsGroundedHash, _isGrounded);
            bodyAnimator.SetBool(animIsMovingHash, Mathf.Abs(_moveInputVector.x) > 0.01f && _isGrounded && !_isCrouchingLogic);
            bodyAnimator.SetBool(animIsCrouchingHash, _isCrouchingLogic && _isGrounded);
        }

        private void HandleAimableArmVisibility()
        {
            if (aimableArmObject == null) return;
            bool showArm = true;

            if (_isCrouchVisualApplied) { showArm = false; }
            else if (!_isGrounded && aimResolver != null && aimResolver.IsAimingDownwards) { showArm = false; }

            Debug.Log($"HandleArmVisibility :: showArmDecision: {showArm}, currentArmActiveState: {aimableArmObject.activeSelf}, _isCrouchVisualApplied: {_isCrouchVisualApplied}, isGrounded: {_isGrounded}, aimResolverNull: {aimResolver == null}, isAimingDown: {aimResolver?.IsAimingDownwards}");

            if (aimableArmObject.activeSelf != showArm)
            {
                aimableArmObject.SetActive(showArm);
            }
        }

        // ... (HandlePlayerDeathAnimation, HandlePlayerVictoryAnimation, OnDrawGizmosSelected sin cambios) ...
        private void HandlePlayerDeathAnimation()
        {
            if (bodyAnimator != null) bodyAnimator.SetTrigger(animDieTriggerHash);
        }

        private void HandlePlayerVictoryAnimation(string levelIdContext)
        {
            if (bodyAnimator != null) bodyAnimator.SetTrigger(animVictoryTriggerHash);
        }

        #if UNITY_EDITOR // Solo compilar este código en el editor
        private void OnDrawGizmosSelected()
        {
            // Gizmo para el Ground Check (ya lo tienes)
            if (groundCheckOrigin != null)
            {
                Gizmos.color = groundCheckGizmoColor;
                Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckRadius);
            }

            // Gizmo para el Wall Check (ya lo tienes)
            if (wallCheckOrigin != null)
            {
                Gizmos.color = wallCheckGizmoColor;
                Gizmos.DrawLine(wallCheckOrigin.position, wallCheckOrigin.position + Vector3.left * wallCheckDistance);
                Gizmos.DrawLine(wallCheckOrigin.position, wallCheckOrigin.position + Vector3.right * wallCheckDistance);
            }

            // --- NUEVO: Gizmos para las Posiciones del FirePoint ---
            if (firePoint != null) // Solo dibujar si hay una referencia al Transform del firePoint
            {
                // Necesitamos la rotación actual del AimableArm para orientar los gizmos del firePoint correctamente.
                // Si AimableArm_Pivot es el que rota y firePoint es su hijo, la rotación del firePoint ya será correcta.
                // Si firePoint es hijo directo de Player_Root, y AimableArm_Pivot es otro objeto que rota,
                // entonces necesitaríamos la rotación del AimableArm_Pivot.
                // Por ahora, asumimos que firePoint.parent es el AimableArm_Pivot o que firePoint rota directamente.

                Quaternion armRotation = Quaternion.identity;
                if (aimableArmObject != null) // Si tenemos referencia al objeto del brazo que rota
                {
                    armRotation = aimableArmObject.transform.rotation;
                }
                else if (firePoint.parent != transform) // Si el firePoint tiene un padre que no es el Player_Root (podría ser el brazo)
                {
                    armRotation = firePoint.parent.rotation;
                }
                // else: el firePoint es hijo directo del Player_Root y rota por sí mismo (menos común para esta estructura)


                // Gizmo para FirePoint Estando de Pie
                Gizmos.color = Color.cyan;
                // Calculamos la posición mundial del firePoint estando de pie
                // El firePoint está en el espacio local del Player_Root (si es hijo directo)
                // o en el espacio local de AimableArm_Pivot (si es hijo de este).
                // Si firePoint es hijo de AimableArm_Pivot, su localPosition es relativa a AimableArm_Pivot.
                // Si AimableArm_Pivot es hijo de Player_Root, entonces:
                // WorldPos = Player_Root.TransformPoint(AimableArm_Pivot.localPosition + AimableArm_Pivot.localRotation * firePointStandingLocalPos)
                // Es más simple si el firePoint ES el objeto que tiene su localPosition cambiada.
                
                // Asumiendo que firePoint.parent es el Player_Root o el AimableArm_Pivot (que está en (0,0,0) local si es solo pivote)
                // y que firePointStandingLocalPos es la posición local del firePoint relativa a su padre inmediato.
                Vector3 worldStandingFirePos = transform.TransformPoint(firePointStandingLocalPos); 
                // Si firePoint es hijo de aimableArmObject y este último es el que rota:
                if (firePoint != null && aimableArmObject != null && firePoint.IsChildOf(aimableArmObject.transform))
                {
                    worldStandingFirePos = aimableArmObject.transform.TransformPoint(firePointStandingLocalPos);
                }
                else if (firePoint != null) // Si firePoint es hijo directo del Player_Root
                {
                     worldStandingFirePos = transform.TransformPoint(firePointStandingLocalPos);
                }


                Gizmos.DrawSphere(worldStandingFirePos, 0.05f); // Un pequeño círculo
                Gizmos.DrawLine(worldStandingFirePos, worldStandingFirePos + (armRotation * Vector3.right * 0.3f)); // Una línea indicando la dirección "adelante"

                // Gizmo para FirePoint Agachado
                Gizmos.color = Color.magenta;
                Vector3 worldCrouchingFirePos = transform.TransformPoint(firePointCrouchingLocalPos);
                if (firePoint != null && aimableArmObject != null && firePoint.IsChildOf(aimableArmObject.transform))
                {
                     worldCrouchingFirePos = aimableArmObject.transform.TransformPoint(firePointCrouchingLocalPos);
                }
                else if (firePoint != null)
                {
                      worldCrouchingFirePos = transform.TransformPoint(firePointCrouchingLocalPos);
                }

                Gizmos.DrawSphere(worldCrouchingFirePos, 0.05f);
                Gizmos.DrawLine(worldCrouchingFirePos, worldCrouchingFirePos + (armRotation * Vector3.right * 0.3f));

                // Dibujar el firePoint actual si está activo y es diferente
                if (Application.isPlaying && firePoint.gameObject.activeInHierarchy) // O aimableArmObject.activeSelf
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(firePoint.position, 0.06f); // Ligeramente más grande o diferente
                }
            }
        }
        #endif

    }
}
// --- END OF FILE PlayerMovement2D.cs ---