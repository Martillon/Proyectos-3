using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Scripts.Player.Core;
using Scripts.Environment.Interfaces;

namespace Scripts.Player.Movement.Abilities
{
    /// <summary>
    /// Handles the player's ability to drop down through one-way platforms.
    /// </summary>
    public class PlayerPlatformHandler : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How long the platform remains non-collidable when dropping through.")]
        [SerializeField] private float platformDisableDuration = 0.3f;
        [Tooltip("A small downward velocity nudge to ensure the player detaches from the platform.")]
        [SerializeField] private float dropVelocityNudge = -2f;

        private PlayerStateManager _stateManager;
        private Rigidbody2D _rb;
        private Coroutine _dropCoroutine;

        private void Awake()
        {
            _stateManager = GetComponentInParent<PlayerStateManager>();
            _rb = GetComponentInParent<Rigidbody2D>();
            if (_stateManager == null) { Debug.LogError("PPH: PlayerStateManager not found!", this); enabled = false; }
            if (_rb == null) { Debug.LogError("PPH: Rigidbody2D not found!", this); enabled = false; }
        }

        private void Update()
        {
            if (_stateManager == null) return;

            // Conditions for initiating a drop-through
            bool canAttemptDrop = _stateManager.JumpInputDown &&
                                  _stateManager.IntendsToPressDown &&
                                  _stateManager.IsGrounded &&
                                  _stateManager.IsOnOneWayPlatform &&
                                  !_stateManager.IsDroppingFromPlatform;

            if (canAttemptDrop)
            {
                // Must consume the jump input to prevent a regular jump from also occurring.
                _stateManager.ConsumeJumpInput();
                
                if (_dropCoroutine != null) StopCoroutine(_dropCoroutine);
                _dropCoroutine = StartCoroutine(DropSequence());
            }
        }

        private IEnumerator DropSequence()
        {
            // 1. Set the dropping state in the manager. This prevents other systems (like Motor) from interfering.
            _stateManager.SetDroppingState(true);

            // 2. Tell all platforms the player is currently on to become traversable.
            // A temporary list is used to avoid issues if the source list changes during iteration.
            List<Collider2D> platformsToDropThrough = new List<Collider2D>(_stateManager.CurrentGroundColliders);
            foreach (var platformCollider in platformsToDropThrough)
            {
                if (platformCollider != null && platformCollider.TryGetComponent<ITraversablePlatform>(out var traversable))
                {
                    traversable.BecomeTemporarilyNonCollidable(platformDisableDuration);
                }
            }

            // 3. Give a small downward nudge to ensure the player clears the platform collider area.
            if (_rb != null)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, dropVelocityNudge);
            }
            
            // 4. Wait for the platform to become solid again.
            yield return new WaitForSeconds(platformDisableDuration);

            // 5. Reset the state.
            _stateManager.SetDroppingState(false);
            _dropCoroutine = null;
        }
    }
}