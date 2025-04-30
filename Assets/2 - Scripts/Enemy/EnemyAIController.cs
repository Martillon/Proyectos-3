using UnityEngine;

namespace Scripts.Enemies
{
    /// <summary>
    /// Controls enemy behavior depending on the assigned type (melee or ranged).
    /// Manages player detection, movement, and attack.
    /// Stops movement if no ground is detected ahead to prevent falling.
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        public enum EnemyType { Melee, Ranged }

        [Header("Enemy Settings")]
        [SerializeField] private EnemyType enemyType = EnemyType.Melee;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float moveSpeed = 2f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private float groundCheckDistance = 1f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Player Detection")]
        [SerializeField] private Transform player;

        private EnemyAttackMelee meleeAttack;
        private EnemyAttackRanged rangedAttack;

        private void Awake()
        {
            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            meleeAttack = GetComponent<EnemyAttackMelee>();
            rangedAttack = GetComponent<EnemyAttackRanged>();
        }

        private void Update()
        {
            if (player == null) return;

            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > detectionRange) return;

            switch (enemyType)
            {
                case EnemyType.Melee:
                    HandleMeleeBehavior();
                    break;
                case EnemyType.Ranged:
                    HandleRangedBehavior();
                    break;
            }
        }

        /// <summary>
        /// Moves toward the player and attacks in close range.
        /// </summary>
        private void HandleMeleeBehavior()
        {
            if (!IsGroundAhead()) return;
            if (meleeAttack == null) return;

            Vector2 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);

            meleeAttack.TryAttack(player);
        }

        /// <summary>
        /// Ranged enemies stay in place and attack from a distance.
        /// </summary>
        private void HandleRangedBehavior()
        {
            if (rangedAttack == null) return;

            rangedAttack.TryAttack(player);
        }

        /// <summary>
        /// Checks if there is ground ahead using a downward-diagonal raycast.
        /// Prevents movement if no ground is found.
        /// </summary>
        private bool IsGroundAhead()
        {
            if (groundCheckOrigin == null) return true;

            float directionX = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 rayDirection = new Vector2(directionX, -1f).normalized;

            RaycastHit2D hit = Physics2D.Raycast(groundCheckOrigin.position, rayDirection, groundCheckDistance, groundLayer);

            Debug.DrawRay(groundCheckOrigin.position, rayDirection * groundCheckDistance, Color.magenta);

            return hit.collider != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            if (groundCheckOrigin != null)
            {
                float directionX = transform.localScale.x >= 0 ? 1f : -1f;
                Vector2 rayDirection = new Vector2(directionX, -1f).normalized;
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(groundCheckOrigin.position, rayDirection * groundCheckDistance);
            }
        }
#endif
    }
}