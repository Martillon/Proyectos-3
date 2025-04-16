using Scripts.Camera;
using UnityEngine;

namespace Scripts.Player.Movement
{
    /// <summary>
    /// Prevents the player from moving behind the camera's forward limit.
    /// Blocks leftward movement if past a certain X.
    /// </summary>
    public class PlayerMovementLimiter : MonoBehaviour
    {
        [SerializeField] private CameraLimiter2D cameraLimiter;
        private Transform playerTransform;

        private void Awake()
        {
            playerTransform = transform;
        }

        private void LateUpdate()
        {
            if (!cameraLimiter) return;

            float limit = cameraLimiter.CurrentLimit;
            Vector3 pos = playerTransform.position;

            // Clamp player to camera's minimum X
            if (pos.x < limit)
            {
                pos.x = limit;
                playerTransform.position = pos;
            }
        }
    }
}
