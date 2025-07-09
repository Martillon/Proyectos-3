using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Scripts.Core;
using Scripts.Enemies.Boss.Attacks;
using Scripts.Enemies.Boss.Attacks.Rush;
using Scripts.Enemies.Boss.Attacks.Smash;
using Scripts.Enemies.Boss.Core.Visuals;

namespace Scripts.Enemies.Boss.Core
{
    /// <summary>
    /// The main brain for this specific boss. It acts as a state machine, managing the
    /// flow of the fight from the intro, through different phases, to its defeat.
    /// It coordinates all other boss components like health, attacks, and visuals.
    /// </summary>
    
    public class BossController : MonoBehaviour
    {
        // This enum defines all the possible states the boss can be in.
        private enum BossState { Idle, Intro, Fighting, Repositioning, PhaseTransition, Dizzy, Defeated }
        
        [Header("Scene Setup")]
        [Tooltip("Reference to the BossEncounterTrigger that manages the arena and minion waves.")]
        [SerializeField] private BossEncounterTrigger encounterTrigger;
        
        [Header("Component References")]
        [Tooltip("Reference to the BossHealth component.")]
        [SerializeField] private BossHealth bossHealth;
        [Tooltip("Reference to the boss's main visual controller.")]
        [SerializeField] private BossVisualController visualController;
        [SerializeField] private BossAttack_MeleeSwipe meleeSwipeAttack;
        [SerializeField] private BossAttack_Rush rushAttack;
        [SerializeField] private BossAttack_GroundSmash groundSmashAttack;


        [Header("Fight Parameters")]
        [Tooltip("How close the player must be for the boss to use its melee swipe.")]
        [SerializeField] private float meleeRange = 3.0f;
        [Tooltip("An empty GameObject marking the ideal position for the Ground Smash attack.")]
        [SerializeField] private Transform groundSmashPosition;
        [Tooltip("An empty GameObject marking the ideal position for the Rush attack (left side of arena).")]
        [SerializeField] private Transform rushStartPosition;
        [Tooltip("How fast the boss moves when repositioning for an attack.")]
        [SerializeField] private float repositionSpeed = 5f;
        [Tooltip("Delay between attacks.")]
        [SerializeField] private float delayBetweenAttacks = 2.0f;
        [Tooltip("How long the boss remains dizzy.")]
        [SerializeField] private float stunDuration = 4.0f;

        // --- Private State ---
        private BossState _currentState;
        private int _currentPhase = 1;
        private Coroutine _activeLogicCoroutine;
        private Transform _playerTarget;
        private bool _isFacingRight = true;
        
        private List<IBossAttack> _attackComponents;

        /// <summary>
        /// Awake is used for setting up references and initial state.
        /// </summary>
        private void Awake()
        {
            _attackComponents = new List<IBossAttack> { meleeSwipeAttack, rushAttack, groundSmashAttack };
            foreach (var attack in _attackComponents)
            {
                attack?.Initialize(this);
            }
            if (bossHealth != null)
            {
                bossHealth.OnPhaseThresholdReached += HandlePhaseTransition;
                bossHealth.OnDeath += HandleDeath;
            }
            else
            {
                Debug.LogError("BossController is missing a reference to BossHealth!", this);
            }

            // Set the initial state before the fight starts.
            _currentState = BossState.Idle;
        }
        
        /// <summary>
        /// This is called by the BossEncounterTrigger when the player enters the arena.
        /// It kicks off the entire boss fight sequence.
        /// </summary>
        public void StartFight()
        {
            // Prevent the fight from starting more than once.
            if (_currentState != BossState.Idle) return;
            
            var playerGO = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
            if (playerGO != null)
            {
                _playerTarget = playerGO.transform;
            }
            else
            {
                Debug.LogError("BossController could not find the Player! The fight cannot start correctly.", this);
                // In a real game, you might want to handle this more gracefully.
                return;
            }

            
            Debug.Log("BOSS FIGHT HAS STARTED!");

            // Check the SessionManager for a checkpoint.
            int checkpointPhase = SessionManager.CurrentBossPhaseCheckpoint;

            if (checkpointPhase > 1)
            {
                // If we have a checkpoint, fast-forward to that phase.
                FastForwardToPhase(checkpointPhase);
            }
            else
            {
                // Otherwise, start the normal intro sequence.
                StartCoroutine(IntroSequence());
            }
        }
        
        /// <summary>
        /// Sets the boss state to match a saved checkpoint.
        /// </summary>
        private void FastForwardToPhase(int phase)
        {
            Debug.Log($"Fast-forwarding to Phase {phase} based on session checkpoint.");
            _currentPhase = phase;
            if (phase == 2) bossHealth.SetHealthToPercentage(0.75f);
            else if (phase == 3) bossHealth.SetHealthToPercentage(0.50f);
            else if (phase == 4) bossHealth.SetHealthToPercentage(0.25f);
            
            groundSmashAttack.UpgradeAttack(phase);
            rushAttack.UpgradeAttack(phase);
            
            ChangeState(BossState.PhaseTransition);
        }
        
        /// <summary>
        /// A helper method to cleanly change the boss's state and log it.
        /// </summary>
        private void ChangeState(BossState newState)
        {
            // A guard clause to prevent re-entering the same state, which could cause bugs.
            if (_currentState == newState) return;

            // --- A. Stop any currently running logic from the OLD state ---
            // This is crucial to prevent multiple coroutines from running at once.
            if (_activeLogicCoroutine != null)
            {
                StopCoroutine(_activeLogicCoroutine);
                _activeLogicCoroutine = null;
            }

            // --- B. Update and log the new state ---
            _currentState = newState;
            Debug.Log($"Boss state changed to: {_currentState}");

            // --- C. Execute the entry logic for the NEW state ---
            switch (_currentState)
            {
                case BossState.Idle:
                    // Idle state has no active logic. It just waits.
                    break;

                case BossState.Intro:
                    // The IntroSequence coroutine is started by the StartFight() method,
                    // so we don't need to start it here. We just need to handle entry logic.
                    bossHealth.SetInvulnerability(true);
                    break;

                case BossState.Fighting:
                    // When we enter the fighting state, we become vulnerable again and start the main attack pattern.
                    bossHealth.SetInvulnerability(false);
                    _activeLogicCoroutine = StartCoroutine(AttackPattern());
                    break;

                case BossState.Repositioning:
                    // The repositioning logic is handled by the RepositionForAttack coroutine.
                    // When we enter this state, we just need to ensure the walking animation is on.
                    visualController.SetWalking(true);
                    break;

                case BossState.PhaseTransition:
                    // When transitioning, we become invulnerable and start the sequence.
                    bossHealth.SetInvulnerability(true);
                    _activeLogicCoroutine = StartCoroutine(PhaseTransitionSequence());
                    break;

                case BossState.Dizzy:
                    // When dizzy, we become vulnerable and start the stun sequence.
                    bossHealth.SetVulnerability(true);
                    _activeLogicCoroutine = StartCoroutine(DizzySequence());
                    break;

                case BossState.Defeated:
                    // When defeated, we stop all logic and play the death animation.
                    visualController.PlayDeathAnimation();
                    FreezeAllMinions(); // A helper method for clarity.
                    break;
            }
        }

        // --- State Logic Coroutines ---

        public IEnumerator AttackPattern()
        {
            while (_currentState == BossState.Fighting)
            {
                yield return new WaitForSeconds(delayBetweenAttacks);

                FacePlayer();

                float distanceToPlayer = Vector2.Distance(transform.position, _playerTarget.position);

                // --- Decision Making ---
                if (distanceToPlayer <= meleeRange)
                {
                    // If player is close, do a quick melee swipe.
                    yield return StartCoroutine(meleeSwipeAttack.Execute());
                }
                else
                {
                    // If player is far, choose a special attack.
                    if (Random.value > 0.5f) // 50/50 chance
                    {
                        // Reposition for Ground Smash, then execute.
                        yield return StartCoroutine(RepositionForAttack(groundSmashPosition.position));
                        yield return StartCoroutine(groundSmashAttack.Execute());
                    }
                    else
                    {
                        // Reposition for Rush, then execute.
                        yield return StartCoroutine(RepositionForAttack(rushStartPosition.position));
                        yield return StartCoroutine(rushAttack.Execute());
                    }
                }
            }
        }
        
        private IEnumerator RepositionForAttack(Vector3 targetPosition)
        {
            ChangeState(BossState.Repositioning);
            visualController.SetWalking(true);
            
            // Move towards the target position until close enough.
            while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPosition, repositionSpeed * Time.deltaTime);
                // Optionally, face the direction of movement.
                // FacePosition(targetPosition);
                yield return null;
            }
            
            visualController.SetWalking(false);
            // After repositioning, we go back to the fighting state to execute the attack.
            ChangeState(BossState.Fighting); 
        }

        private IEnumerator IntroSequence()
        {
            ChangeState(BossState.Intro);
            bossHealth.SetInvulnerability(true);

            // --- PLACEHOLDER for entrance animation ---
            Debug.Log("Playing entrance animation...");
            yield return new WaitForSeconds(2.0f); // Simulate animation duration
            
            visualController.PlayRoarAnimation();
            Debug.Log("Playing intro roar...");
            yield return new WaitForSeconds(1.5f); // Simulate roar duration

            ChangeState(BossState.Fighting);
        }

        private IEnumerator PhaseTransitionSequence()
        {
            bossHealth.SetInvulnerability(true);
            visualController.PlayRoarAnimation();
            
            // Upgrade attacks for the new phase.
            groundSmashAttack.UpgradeAttack(_currentPhase);
            rushAttack.UpgradeAttack(_currentPhase);
            
            encounterTrigger?.SpawnMinionWave(_currentPhase - 2);
            SessionManager.SetBossPhaseCheckpoint(_currentPhase);
            
            yield return new WaitForSeconds(2.0f);
            ChangeState(BossState.Fighting);
        }
        
        private IEnumerator DizzySequence()
        {
            Debug.Log("Boss is DIZZY!");
            bossHealth.SetVulnerability(true);
            visualController.PlayStunBeginAnimation(); 

            yield return new WaitForSeconds(stunDuration);

            Debug.Log("Boss has recovered.");
            bossHealth.SetVulnerability(false);
            visualController.PlayStunEndAnimation(); 

            yield return new WaitForSeconds(1.0f); 

            ChangeState(BossState.Fighting);
        }
        
        /// <summary>
        /// A public method that can be called by other components (like an attack script)
        /// to force the boss into the Dizzy state.
        /// </summary>
        public void EnterDizzyState()
        {
            // We only want to enter the dizzy state if we are currently in the middle of a fight.
            // This prevents weird states if, for example, the boss dies mid-rush.
            if (_currentState == BossState.Fighting)
            {
                ChangeState(BossState.Dizzy);
            }
        }
        
        private void FreezeAllMinions()
        {
            if (encounterTrigger != null)
            {
                foreach (var minion in encounterTrigger.ActiveMinions)
                {
                    minion?.ForceFreeze();
                }
            }
        }
        
        /// <summary>
        /// Checks the player's position and tells the Visual Controller to flip if necessary.
        /// The logic for *deciding* to flip is here. The logic for *how* to flip is in the visual controller.
        /// </summary>
        public void FacePlayer()
        {
            if (_playerTarget == null || visualController == null) return;

            bool playerIsToTheRight = (_playerTarget.position.x > transform.position.x);

            if (playerIsToTheRight && !_isFacingRight)
            {
                // We've decided to flip right. Tell the visual controller to execute it.
                _isFacingRight = true;
                visualController.Flip(true); // Command: Face Right
            }
            else if (!playerIsToTheRight && _isFacingRight)
            {
                // We've decided to flip left.
                _isFacingRight = false;
                visualController.Flip(false); // Command: Face Left
            }
        }

        // --- Event Handlers ---

        /// <summary>
        /// This method is called by the BossHealth's OnPhaseThresholdReached event.
        /// </summary>
        private void HandlePhaseTransition(int phaseIndex)
        {
            // We only transition if we are currently in the fighting state.
            if (_currentState == BossState.Fighting)
            {
                _currentPhase = phaseIndex + 1; // Our phase is 1-based, index is 0-based
                ChangeState(BossState.PhaseTransition);
            }
        }

        /// <summary>
        /// This method is called by the BossHealth's OnDeath event.
        /// </summary>
        private void HandleDeath()
        {
            ChangeState(BossState.Defeated);
            
            if (encounterTrigger != null)
            {
                foreach (var minion in encounterTrigger.ActiveMinions)
                {
                    minion?.ForceFreeze();
                }
            }
            
            // Unsubscribe from events to prevent memory leaks.
            bossHealth.OnPhaseThresholdReached -= HandlePhaseTransition;
            bossHealth.OnDeath -= HandleDeath;
        }
    }
}