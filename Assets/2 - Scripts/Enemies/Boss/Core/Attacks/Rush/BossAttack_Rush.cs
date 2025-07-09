using System.Collections;
using Scripts.Enemies.Boss.Core;
using Scripts.Enemies.Boss.Core.Visuals;
using Scripts.Enemies.Melee;
using UnityEngine;

namespace Scripts.Enemies.Boss.Attacks.Rush
{
    public class BossAttack_Rush : MonoBehaviour, IBossAttack
    {
        [Header("Phase 1 Settings")]
        [SerializeField] private float phase1_rushSpeed = 20f;
        [Header("Attack Configuration")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float tellDuration = 0.75f;
        [SerializeField] private GameObject rushHitbox;
        [SerializeField] private int damage = 20;

        private BossController _bossController;
        private BossVisualController _visualController;
        private Rigidbody2D _rb;
        private Collider2D _bossCollider;
        private float _currentRushSpeed;

        public void Initialize(BossController controller)
        {
            _bossController = controller;
            _visualController = _bossController.GetComponentInChildren<BossVisualController>();
            _rb = _bossController.GetComponent<Rigidbody2D>();
            _bossCollider = _bossController.GetComponent<Collider2D>();
            UpgradeAttack(1);
        }

        public IEnumerator Execute()
        {
            Debug.Log("Executing Rush Attack...");
            _visualController.PlayRushAnimation();
            yield return new WaitForSeconds(tellDuration);

            rushHitbox?.GetComponent<EnemyMeleeHitbox>()?.Activate(damage);

            // --- FIXED ---
            // Determine rush direction based on the controller's state, not localScale.
            float rushDirection = _bossController.IsFacingRight ? 1f : -1f;
            Vector2 rushVelocity = new Vector2(rushDirection * _currentRushSpeed, 0);
            _rb.linearVelocity = rushVelocity;
            
            float maxRushDuration = 5f; 
            float rushTimer = 0f;
            while(rushTimer < maxRushDuration)
            {
                if (_bossCollider.IsTouchingLayers(wallLayer))
                {
                    Debug.Log("Rush attack hit a wall.");
                    break;
                }
                rushTimer += Time.deltaTime;
                yield return null;
            }

            _rb.linearVelocity = Vector2.zero;
            rushHitbox?.GetComponent<EnemyMeleeHitbox>()?.Deactivate();
            _bossController.EnterDizzyState();

            Debug.Log("Rush Attack Finished. Handing control back to BossController.");
        }

        public void UpgradeAttack(int phase)
        {
            switch (phase)
            {
                case 2: _currentRushSpeed = phase1_rushSpeed * 1.2f; break;
                case 3: _currentRushSpeed = phase1_rushSpeed * 1.5f; break;
                default: _currentRushSpeed = phase1_rushSpeed; break;
            }
        }
    }
}