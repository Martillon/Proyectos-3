using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List
using Scripts.Player.Core;
using Scripts.Environment.Interfaces; // Para ITraversablePlatform

namespace Scripts.Player.Movement.Abilities
{
    public class PlayerPlatformHandler : MonoBehaviour
    {
        [Header("Platform Drop Settings")]
        [Tooltip("Duration for which the platform will become non-collidable when dropping through.")]
        [SerializeField] private float platformDisableDuration = 0.3f;

        private PlayerStateManager _playerStateManager;
        private Rigidbody2D _rb;
        private Coroutine _dropProcessCoroutine;

        void Awake()
        {
            _playerStateManager = GetComponentInParent<PlayerStateManager>();
            _rb = GetComponentInParent<Rigidbody2D>();

            if (_playerStateManager == null) Debug.LogError("PlayerPlatformHandler: PlayerStateManager not found!", this);
            if (_rb == null) Debug.LogError("PlayerPlatformHandler: Rigidbody2D not found on parent!", this);
        }

        void Update()
        {
            if (_playerStateManager == null) return;

            bool canAttemptDrop = _playerStateManager.JumpInputDown && // <<< Input de salto está presionado
                                 !_playerStateManager.IsDroppingFromPlatform &&
                                 _playerStateManager.IsGrounded &&
                                 _playerStateManager.IntendsToPressDown &&
                                 _playerStateManager.IsOnOneWayPlatform &&
                                 _playerStateManager.CurrentGroundPlatformColliders != null &&
                                 _playerStateManager.CurrentGroundPlatformColliders.Count > 0;

            if (canAttemptDrop)
            {
                // *** ¡NUEVO! Consumir el input de salto inmediatamente ***
                _playerStateManager.ConsumeJumpInput();
                // Debug.Log("PlayerPlatformHandler: Consuming JumpInput due to platform drop initiation.");


                List<ITraversablePlatform> platformsToDropThrough = new List<ITraversablePlatform>();
                foreach (Collider2D groundCollider in _playerStateManager.CurrentGroundPlatformColliders)
                {
                    ITraversablePlatform platform = groundCollider.GetComponent<ITraversablePlatform>();
                    if (platform != null)
                    {
                        platformsToDropThrough.Add(platform);
                    }
                }

                if (platformsToDropThrough.Count > 0)
                {
                    if (_dropProcessCoroutine != null) StopCoroutine(_dropProcessCoroutine);
                    _dropProcessCoroutine = StartCoroutine(ProcessDropSequence(platformsToDropThrough));
                }
            }
        }

        private IEnumerator ProcessDropSequence(List<ITraversablePlatform> platforms)
        {
            _playerStateManager.UpdateDroppingState(true);

            if (_playerStateManager.IsCrouchVisualApplied)
            {
                _playerStateManager.UpdateCrouchLogicState(false);
                yield return null; 
            }

            foreach (ITraversablePlatform platform in platforms)
            {
                platform.BecomeTemporarilyNonCollidable(platformDisableDuration);
            }

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -1.5f);

            yield return new WaitForSeconds(platformDisableDuration + 0.1f);

            _playerStateManager.UpdateDroppingState(false);
            _dropProcessCoroutine = null;
        }
    }
}