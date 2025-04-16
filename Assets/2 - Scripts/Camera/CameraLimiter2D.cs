using UnityEngine;
using Unity.Cinemachine;

namespace Scripts.Camera
{
    /// <summary>
    /// Limits the horizontal movement of the camera to only follow the player forward (one direction).
    /// Allows slight movement backwards for visual buffer, but blocks full retraction.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraLimiter2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform player;

        [Header("Settings")]
        [Tooltip("Axis to limit (true = horizontal, false = vertical)")]
        [SerializeField] private bool limitX = true;

        [Tooltip("Allow slight camera movement backwards when player retreats")]
        [SerializeField] private float backwardMargin = 2f;

        private float furthestX;

        private void LateUpdate()
        {
            if (!player) return;

            Vector3 cameraPos = transform.position;
            float playerX = player.position.x;

            if (limitX)
            {
                if (playerX > furthestX)
                {
                    furthestX = playerX;
                }

                float targetX = Mathf.Max(furthestX - backwardMargin, playerX);
                cameraPos.x = targetX;
            }

            transform.position = new Vector3(cameraPos.x, player.position.y, cameraPos.z);
        }
    }
}
