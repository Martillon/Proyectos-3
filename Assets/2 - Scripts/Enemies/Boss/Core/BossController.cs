using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Scripts.Core;
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
        private enum BossState { Idle, Intro, Fighting, PhaseTransition, Dizzy, Defeated }
        
        [Header("Scene Setup")]
        [Tooltip("Reference to the BossEncounterTrigger that manages the arena and minion waves.")]
        [SerializeField] private BossEncounterTrigger encounterTrigger;
        
        [Header("Component References")]
        [Tooltip("Reference to the BossHealth component.")]
        [SerializeField] private BossHealth bossHealth;
        [Tooltip("Reference to the boss's main visual controller.")]
        [SerializeField] private BossVisualController visualController;
        // We will add references to the attack scripts here later.
        // [SerializeField] private BossAttack_Rush rushAttack;
        // [SerializeField] private BossAttack_GroundSmash groundSmashAttack;

        [Header("Fight Parameters")]
        [Tooltip("Delay in seconds between attacks during the main fighting loop.")]
        [SerializeField] private float delayBetweenAttacks = 2.0f;
        [Tooltip("How long the boss remains in the 'Dizzy' state after a rush, in seconds.")]
        [SerializeField] private float stunDuration = 4.0f;

        // --- Private State ---
        private BossState _currentState;
        private int _currentPhase = 1;
        private Coroutine _attackPatternCoroutine;
        private Transform _playerTarget;
        private bool _isFacingRight = true;

        /// <summary>
        /// Awake is used for setting up references and initial state.
        /// </summary>
        private void Awake()
        {
            // Subscribe to the events from the BossHealth component.
            // This is how this controller "listens" for health changes.
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
            // Here you would also instantly set the boss's health to the phase's starting percentage.
            // bossHealth.SetHealthPercentage(...); // We would need to add this method to BossHealth.
            
            // Skip the intro and go straight into the phase transition roar.
            ChangeState(BossState.PhaseTransition);
        }
        
        /// <summary>
        /// A helper method to cleanly change the boss's state and log it.
        /// </summary>
        private void ChangeState(BossState newState)
        {
            _currentState = newState;
            Debug.Log($"Boss state changed to: {_currentState}");

            // Execute logic based on the new state.
            switch (_currentState)
            {
                case BossState.Intro:
                    // The IntroSequence coroutine handles this.
                    break;
                case BossState.Fighting:
                    bossHealth.SetInvulnerability(false);
                    // Start the attack pattern for the current phase.
                    _attackPatternCoroutine = StartCoroutine(AttackPattern());
                    break;
                case BossState.PhaseTransition:
                    // Stop the current attack pattern to interrupt it.
                    if (_attackPatternCoroutine != null) StopCoroutine(_attackPatternCoroutine);
                    StartCoroutine(PhaseTransitionSequence());
                    break;
                case BossState.Dizzy:
                    if (_attackPatternCoroutine != null) StopCoroutine(_attackPatternCoroutine);
                    StartCoroutine(DizzySequence());
                    break;
                case BossState.Defeated:
                    if (_attackPatternCoroutine != null) StopCoroutine(_attackPatternCoroutine);
                    visualController.PlayDeathAnimation();
                    // Here we would also tell all active minions to freeze.
                    break;
            }
        }

        // --- State Logic Coroutines ---

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
            Debug.Log($"PHASE {_currentPhase} START! Roaring and spawning minions.");
            
            if (encounterTrigger != null)
            {
                encounterTrigger.SpawnMinionWave(_currentPhase - 2);
            }
            
            // Save the checkpoint progress.
            SessionManager.SetBossPhaseCheckpoint(_currentPhase);

            yield return new WaitForSeconds(2.0f); // Duration of roar/spawn sequence

            // After transition, go back to fighting with upgraded attacks.
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
        /// The main coroutine that dictates the boss's attack sequence for the current phase.
        /// </summary>
        private IEnumerator AttackPattern()
        {
            // This loop will run continuously while in the 'Fighting' state.
            while (_currentState == BossState.Fighting)
            {
                Debug.Log("--- Starting new attack sequence ---");

                // --- PLACEHOLDER for attack logic ---
                // In the future, this will call the Execute() coroutines of the attack scripts.
                Debug.Log("Executing Ground Smash attack...");
                yield return new WaitForSeconds(2.5f); // Simulate attack duration

                yield return new WaitForSeconds(delayBetweenAttacks);

                Debug.Log("Executing Rush attack...");
                yield return new WaitForSeconds(3.0f); // Simulate attack duration

                // The Rush attack itself would trigger the Dizzy state, breaking this loop.
                // For now, we'll just add a delay and loop again.
                yield return new WaitForSeconds(delayBetweenAttacks);
            }
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