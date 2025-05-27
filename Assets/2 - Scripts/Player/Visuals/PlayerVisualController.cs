using UnityEngine;
using Scripts.Core; // For PlayerStateManager, GameConstants
using Scripts.Player.Core; // For PlayerEvents
using Scripts.Player.Weapons; // For AimDirectionResolver

namespace Scripts.Player.Visuals // Nuevo namespace
{
    /// <summary>
    /// Manages all visual aspects of the player, including body animations (idle, run, jump, crouch),
    /// sprite flipping for direction, and visibility of the aimable arm.
    /// Resides on the 'VisualsContainer' GameObject.
    /// </summary>
    public class PlayerVisualController : MonoBehaviour
    {
        [Header("Core Component References")]
        [Tooltip("Animator for the player's body sprites.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("SpriteRenderer for the player's main body (if separate from Animator's GO). Optional if Animator is on the SpriteRenderer GO.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer; // Puede ser el mismo GO que bodyAnimator
        [Tooltip("Transform of this GameObject itself (the VisualsContainer that gets flipped).")]
        [SerializeField] private Transform visualsContainerTransform; // Referencia a su propio Transform para flippear

        [Header("Arm Control References")]
        [Tooltip("The GameObject representing the aimable arm (pivot, sprite, firepoint).")]
        [SerializeField] private GameObject aimableArmObject;
        [Tooltip("The SpriteRenderer of the aimable arm, for flipping its visual independently.")]
        [SerializeField] private SpriteRenderer armSpriteRenderer;
        
        [SerializeField] private Transform armVisualHolderTransform;

        // References to other player systems (obtained from parent)
        private PlayerStateManager _playerStateManager;
        private AimDirectionResolver _aimResolver;
        private Rigidbody2D _rb; // Para obtener velocidad para animaciones

        // Animator Parameter Hashes
        private readonly int animIsMovingHash = Animator.StringToHash("isMoving");
        private readonly int animIsGroundedHash = Animator.StringToHash("isGrounded");
        private readonly int animIsCrouchingHash = Animator.StringToHash("isCrouching");
        private readonly int animDieTriggerHash = Animator.StringToHash(GameConstants.AnimDieTrigger);
        private readonly int animVictoryTriggerHash = Animator.StringToHash(GameConstants.AnimVictoryTrigger); // O AnimVictoryTrigger


        void Awake()
        {
            // Este script está en VisualsContainer, los otros están en Player_Root o LogicContainer (padres)
            _playerStateManager = transform.parent.GetComponentInChildren<PlayerStateManager>(true);
            _rb = transform.parent.GetComponentInChildren<Rigidbody2D>(true);
            _aimResolver = transform.parent.GetComponentInChildren<AimDirectionResolver>(true);

            if (bodyAnimator == null) bodyAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (bodySpriteRenderer == null) bodySpriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (visualsContainerTransform == null) visualsContainerTransform = transform;
            if (_aimResolver == null && transform.parent != null) // transform.parent es Player_Root
            {
                // Asume que AimDirectionResolver está en algún lugar bajo Player_Root
                _aimResolver = transform.parent.GetComponentInChildren<AimDirectionResolver>(true); 
            }

            // Validaciones
            if (_playerStateManager == null) Debug.LogError("PVC: PlayerStateManager not found via parent!", this);
            if (_aimResolver == null) Debug.LogError("PVC: AimDirectionResolver not found via parent's children! Arm visibility logic might fail.", this);
            if (_rb == null) Debug.LogError("PlayerVisualController: Rigidbody2D not found in parent!", this);
            if (bodyAnimator == null) Debug.LogError("PlayerVisualController: Body Animator not assigned/found!", this);
            if (visualsContainerTransform == null) Debug.LogError("PlayerVisualController: VisualsContainerTransform not assigned/found!", this);
            if (aimableArmObject == null) Debug.LogWarning("PlayerVisualController: AimableArmObject not assigned. Arm visibility cannot be controlled.", this);
            if (armSpriteRenderer == null && aimableArmObject != null) Debug.LogWarning("PlayerVisualController: ArmSpriteRenderer not assigned. Arm sprite cannot be flipped.", this);
        }

        private void OnEnable()
        {
            PlayerEvents.OnPlayerDeath += PlayDeathAnimation;
            PlayerEvents.OnLevelCompleted += PlayVictoryAnimation;
        }

        private void OnDisable()
        {
            PlayerEvents.OnPlayerDeath -= PlayDeathAnimation;
            PlayerEvents.OnLevelCompleted -= PlayVictoryAnimation;
        }

        void Update()
        {
            if (_playerStateManager == null || bodyAnimator == null) return;

            HandleBodyAnimations();
            HandleVisualFlip();
            HandleArmVisibilityAndFlip();
        }

        private void HandleBodyAnimations()
        {
            if (bodyAnimator == null || _playerStateManager == null) return;

            bodyAnimator.SetBool(animIsGroundedHash, _playerStateManager.IsGrounded);
    
            // isMoving es true si está en el suelo, hay input horizontal, NO está lógicamente agachado,
            // Y NO está el input de bloqueo de posición activo.
            bool isEffectivelyMoving = _playerStateManager.IsGrounded &&
                                       Mathf.Abs(_playerStateManager.HorizontalInput) > 0.01f &&
                                       !_playerStateManager.IsCrouchingLogic &&
                                       !_playerStateManager.PositionLockInputActive; // <<< AÑADIR ESTA CONDICIÓN
            bodyAnimator.SetBool(animIsMovingHash, isEffectivelyMoving);

            bodyAnimator.SetBool(animIsCrouchingHash, _playerStateManager.IsCrouchVisualApplied && _playerStateManager.IsGrounded);
    
            // if (_rb != null) bodyAnimator.SetFloat(animVerticalSpeedHash, _rb.linearVelocity.y);
        }

        private void HandleVisualFlip()
        {
            if (visualsContainerTransform == null || _playerStateManager == null) return;

            // Leer la dirección a la que se debe mirar desde el StateManager
            float targetFacingDirection = _playerStateManager.CurrentFacingDirection; 

            // Aplicar el flip al contenedor visual del cuerpo
            visualsContainerTransform.localScale = new Vector3(
                targetFacingDirection, 
                visualsContainerTransform.localScale.y, 
                visualsContainerTransform.localScale.z
            );
            // Debug.Log($"VISUAL_CTRL: Body Flipped. CurrentFacingDirection(SM) = {targetFacingDirection}, New ScaleX = {visualsContainerTransform.localScale.x}");


            // Flipear el sprite del brazo para que coincida
            if (armSpriteRenderer != null && aimableArmObject != null && aimableArmObject.activeSelf)
            {
                armSpriteRenderer.flipY = (targetFacingDirection < 0f); // O .flipX
            }
        }
        
        public void HideArmObject()
        {
            Debug.Log($"[{Time.frameCount}] PVC: Hiding Arm Object.");
            if (aimableArmObject != null)
            {
                aimableArmObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("PVC: AimableArmObject not found. Cannot hide it.");
            }
        }

        public void ShowArmObject()
        {
            if (aimableArmObject != null)
            {
                // La lógica en Update/HandleArmVisibilityAndFlip decidirá si realmente debe mostrarse
                // basado en el estado actual (agachado, apuntando abajo en el aire, etc.)
                // Aquí solo lo "preparamos" para ser visible si las condiciones lo permiten.
                // O, si siempre debe mostrarse después de respawn (y no está agachado inmediatamente):
                aimableArmObject.SetActive(true); 
                Debug.Log($"[{Time.frameCount}] PVC: Showing Arm Object.");
            }
        }

        private void HandleArmVisibilityAndFlip()
        {
            if (aimableArmObject == null) return;

            bool showArm = true;
            if (_playerStateManager.IsCrouchVisualApplied) { showArm = false; }
            else if (!_playerStateManager.IsGrounded && _aimResolver.IsAimingDownwards) { showArm = false; }
            // ... (otras condiciones para ocultar el brazo) ...

            if (aimableArmObject.activeSelf != showArm)
            {
                aimableArmObject.SetActive(showArm);
            }

            if (armVisualHolderTransform != null && aimableArmObject.activeSelf)
            {
                float bodyFacing = _playerStateManager.CurrentFacingDirection;
                armVisualHolderTransform.localScale = new Vector3(bodyFacing, 1f, 1f);
            }
        }

        private void PlayDeathAnimation()
        {
            if (bodyAnimator != null) bodyAnimator.SetTrigger(animDieTriggerHash);
            if (aimableArmObject != null) aimableArmObject.SetActive(false); // Ocultar brazo al morir
        }

        private void PlayVictoryAnimation(string levelId) // Parámetro de evento no usado aquí
        {
            HideArmObject();
            if (bodyAnimator != null) bodyAnimator.SetTrigger(animVictoryTriggerHash);
        }
    }
}