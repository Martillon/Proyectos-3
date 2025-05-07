using UnityEngine;

namespace Scripts.Enemies
{
    /// <summary>
    /// Controls enemy behavior depending on the assigned type (melee or ranged).
    /// Manages player detection, movement, and attack.
    /// Uses direct Raycast for ground detection to prevent falling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAIController : MonoBehaviour
    {
        public enum EnemyType { Melee, Ranged }

        [Header("Enemy Settings")]
        [SerializeField] private EnemyType enemyType = EnemyType.Melee;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private PhysicsMaterial2D noFrictionMaterial;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.5f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Player Detection")]
        [SerializeField] private Transform player;

        private EnemyAttackMelee meleeAttack;
        private EnemyAttackRanged rangedAttack;
        private Rigidbody2D rb;
        private bool isGroundedLeft;
        private bool isGroundedRight;

        private void Awake()
        {
            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            meleeAttack = GetComponent<EnemyAttackMelee>();
            rangedAttack = GetComponent<EnemyAttackRanged>();
            rb = GetComponent<Rigidbody2D>();

            if (noFrictionMaterial != null)
                rb.sharedMaterial = noFrictionMaterial;

            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (player == null) return;

            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > detectionRange) return;

            CheckGround();

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

        private void CheckGround()
        {
            Vector2 position = transform.position;

            // Left and right raycasts for ground detection
            isGroundedLeft = Physics2D.Raycast(position + Vector2.left * 0.25f, Vector2.down, groundCheckDistance, groundLayer);
            isGroundedRight = Physics2D.Raycast(position + Vector2.right * 0.25f, Vector2.down, groundCheckDistance, groundLayer);

            Debug.DrawRay(position + Vector2.left * 0.25f, Vector2.down * groundCheckDistance, isGroundedLeft ? Color.green : Color.red);
            Debug.DrawRay(position + Vector2.right * 0.25f, Vector2.down * groundCheckDistance, isGroundedRight ? Color.green : Color.red);
        }

        private void HandleMeleeBehavior()
        {
            if (!(isGroundedLeft || isGroundedRight) || meleeAttack == null) return;

            bool inRange = meleeAttack.IsInAttackRange(player);

            if (!inRange)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                direction.y = 0;

                // Movimiento instantáneo
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            meleeAttack.TryAttack(player);
        }

        private void HandleRangedBehavior()
        {
            if (!(isGroundedLeft || isGroundedRight) || rangedAttack == null) return;

            bool inRange = rangedAttack.IsInAttackRange(player);

            if (!inRange)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                direction.y = 0;

                // Movimiento instantáneo
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            rangedAttack.TryAttack(player);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Vector2 position = transform.position;

            // Draw ground check rays
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(position + Vector2.left * 0.25f, position + Vector2.left * 0.25f + Vector2.down * groundCheckDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(position + Vector2.right * 0.25f, position + Vector2.right * 0.25f + Vector2.down * groundCheckDistance);
        }
#endif
    }
}