using UnityEngine;
using System.Collections;
using Scripts.Enemies.Boss.Core;
using Scripts.Enemies.Boss.Core.Visuals;
using Scripts.Enemies.Melee;

namespace Scripts.Enemies.Boss.Attacks
{
    /// <summary>
    /// A modular boss attack that performs a simple, fast melee swipe.
    /// </summary>
    public class BossAttack_MeleeSwipe : MonoBehaviour, IBossAttack
    {
        [Header("Configuration")]
        [Tooltip("The total duration of the melee swipe animation.")]
        [SerializeField] private float attackDuration = 0.8f;
        [Tooltip("A reference to the hitbox GameObject to be enabled during the attack.")]
        [SerializeField] private GameObject meleeHitbox;

        [Header("Data")]
        [Tooltip("How much damage this swipe will deal.")]
        [SerializeField] private int damage = 10;
        
        // --- Private References ---
        private BossController _bossController;
        
        // Animator hash for performance
        private readonly int _doMeleeSwipeParam = Animator.StringToHash("DoMeleeSwipe");

        /// <summary>
        /// Called by the BossController to provide necessary references.
        /// </summary>
        public void Initialize(BossController controller)
        {
            _bossController = controller;
        }

        /// <summary>
        /// The main logic for the attack, run as a coroutine by the BossController.
        /// </summary>
        public IEnumerator Execute()
        {
            Debug.Log("Executing Melee Swipe Attack...");
            
            // Tell the animator to start the swipe animation.
            _bossController.GetComponentInChildren<BossVisualController>().PlayMeleeSwipeAnimation();


            // Wait for the duration of the animation. During this time, Animation Events
            // will be responsible for activating and deactivating the hitbox.
            yield return new WaitForSeconds(attackDuration);

            Debug.Log("Melee Swipe Attack Finished.");
        }
        
        // --- Public methods for the Animation Relay to call ---

        /// <summary>
        /// Called by an Animation Event to activate the hitbox at the correct moment.
        /// </summary>
        public void Animation_ActivateHitbox()
        {
            if (meleeHitbox != null)
            {
                var hitboxScript = meleeHitbox.GetComponent<EnemyMeleeHitbox>();
                hitboxScript?.Activate(damage);
            }
        }


        /// <summary>
        /// Called by an Animation Event to deactivate the hitbox after the swing.
        /// </summary>
        public void Animation_DeactivateHitbox()
        {
            if (meleeHitbox != null)
            {
                meleeHitbox.GetComponent<EnemyMeleeHitbox>()?.Deactivate();
            }
        }
    }
}