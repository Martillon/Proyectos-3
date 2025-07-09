using System.Collections;
using Scripts.Enemies.Boss.Core;
using Scripts.Enemies.Boss.Core.Visuals;
using UnityEngine;

namespace Scripts.Enemies.Boss.Attacks.Rush
{
    /// <summary>
    /// A modular boss attack that performs a horizontal charge across the arena.
    /// Upon completion, it triggers the boss's "Dizzy" state, creating a
    /// window of vulnerability for the player.
    /// </summary>
    public class BossAttack_Rush : MonoBehaviour, IBossAttack
    {
        [Header("Phase 1 Settings")]
        [Tooltip("The speed of the rush in Phase 1.")]
        [SerializeField] private float phase1_rushSpeed = 20f;

        [Header("Attack Configuration")]
        [Tooltip("The LayerMask representing walls or obstacles that will stop the rush.")]
        [SerializeField] private LayerMask wallLayer;
        [Tooltip("How long the 'tell' animation plays before the rush begins.")]
        [SerializeField] private float tellDuration = 0.75f;
        [Tooltip("A reference to the hitbox that is active during the rush.")]
        [SerializeField] private GameObject rushHitbox;
        [Tooltip("How much damage the rush deals on impact.")]
        [SerializeField] private int damage = 20;

        // --- Private References & State ---
        private BossController _bossController;
        private BossVisualController _visualController;
        private Rigidbody2D _rb;
        private Collider2D _bossCollider; // The main collider of the boss

        // Current values that can be upgraded
        private float _currentRushSpeed;

        /// <summary>
        /// Called by the BossController to provide necessary references.
        /// </summary>
        public void Initialize(BossController controller)
        {
            _bossController = controller;
            // Get references from the main boss object.
            _visualController = _bossController.GetComponentInChildren<BossVisualController>();
            _rb = _bossController.GetComponent<Rigidbody2D>();
            _bossCollider = _bossController.GetComponent<Collider2D>();
            
            // Set initial attack parameters.
            UpgradeAttack(1);
        }

        /// <summary>
        /// The main coroutine that manages the entire Rush attack sequence.
        /// </summary>
        public IEnumerator Execute()
        {
            Debug.Log("Executing Rush Attack...");

            // 1. Preparation Phase
            // Ensure the boss is facing the player for the charge.
            _bossController.FacePlayer();
            // The BossController is responsible for stopping its own logic while an attack is running.
            // We can do this by having the controller wait for this coroutine to finish.

            // 2. Telegraph Phase
            // Play the "tell" animation (e.g., getting into a stance).
            _visualController.PlayRushAnimation();
            yield return new WaitForSeconds(tellDuration);

            // 3. Execution Phase
            // Activate the damage hitbox for the duration of the rush.
            // In a real game, you might pass damage to the hitbox here.
            rushHitbox?.SetActive(true);

            // Determine the direction of the rush based on where the boss is facing.
            float rushDirection = _bossController.transform.localScale.x > 0 ? 1f : -1f; // A simple way to check facing direction
            Vector2 rushVelocity = new Vector2(rushDirection * _currentRushSpeed, 0);
            _rb.linearVelocity = rushVelocity;
            
            // 4. Wait for Completion
            // The loop will continue as long as the boss is moving. We wait until it hits a wall.
            // We'll use a simple timer as a fallback to prevent getting stuck forever.
            float maxRushDuration = 5f; 
            float rushTimer = 0f;
            while(rushTimer < maxRushDuration)
            {
                // The IsTouchingLayers check is a simple way to see if the main collider is hitting a wall.
                if (_bossCollider.IsTouchingLayers(wallLayer))
                {
                    Debug.Log("Rush attack hit a wall.");
                    break; // Exit the loop
                }
                rushTimer += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // 5. Cleanup and Handover
            // Stop all movement.
            _rb.linearVelocity = Vector2.zero;
            // Deactivate the hitbox.
            rushHitbox?.SetActive(false);

            // --- THE CRITICAL HANDOVER ---
            // Tell the main BossController to enter its Dizzy state.
            // The BossController is now back in charge.
            _bossController.EnterDizzyState();

            Debug.Log("Rush Attack Finished. Handing control back to BossController.");
        }

        /// <summary>
        /// A public method for the BossController to call to make this attack stronger.
        /// </summary>
        public void UpgradeAttack(int phase)
        {
            switch (phase)
            {
                case 2:
                    _currentRushSpeed = phase1_rushSpeed * 1.2f;
                    break;
                case 3:
                    _currentRushSpeed = phase1_rushSpeed * 1.5f;
                    break;
                default: // Phase 1
                    _currentRushSpeed = phase1_rushSpeed;
                    break;
            }
        }
    }
}