using UnityEngine;
using Unity.Cinemachine;

namespace Scripts.Camera
{
    /// <summary>
    /// Prevents the camera from moving backwards past the furthest position reached by the player.
    /// Can be used for side-scrolling games where the camera only advances forward.
    /// </summary>
    public class CameraLimiter2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform player;

        [Header("Settings")]
        [SerializeField] private float backwardMargin = 1.5f;

        private float furthestX;

        public float CurrentLimit => furthestX - backwardMargin;

        private void LateUpdate()
        {
            if (!player) return;

            Vector3 current = transform.position;
            float playerX = player.position.x;

            if (playerX > furthestX)
                furthestX = playerX;

            // Forcing the camera to stay at least at this position
            float minX = furthestX - backwardMargin;
            transform.position = new Vector3(Mathf.Max(minX, playerX), current.y, current.z);
        }
    }
}