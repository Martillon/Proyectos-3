// The full file, but only AttackPattern and the new counter field have changed.
using System;
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
    public class BossController : MonoBehaviour
    {
        private enum BossState { Idle, Intro, Fighting, Repositioning, PhaseTransition, Dizzy, Defeated, Chase }
        
        [Header("Scene Setup")]
        [SerializeField] private BossEncounterTrigger encounterTrigger;
        
        [Header("Component References")]
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private BossVisualController visualController;
        [SerializeField] private BossAttack_MeleeSwipe meleeSwipeAttack;
        [SerializeField] private BossAttack_Rush rushAttack;
        [SerializeField] private BossAttack_GroundSmash groundSmashAttack;

        [Header("Fight Parameters")]
        [SerializeField] private float meleeRange = 3.0f;
        [SerializeField] private Transform groundSmashPosition;
        [SerializeField] private Transform rushPositionLeft;
        [SerializeField] private Transform rushPositionRight;
        [SerializeField] private float repositionSpeed = 5f;
        [SerializeField] private float delayBetweenAttacks = 2.0f;
        [SerializeField] private float stunDuration = 4.0f;
        [SerializeField] private float chaseRange = 10.0f;

        private BossState _currentState;
        private int _currentPhase = 1;
        private Coroutine _activeLogicCoroutine;
        private Transform _playerTarget;
        private bool _isFacingRight;
        private List<IBossAttack> _attackComponents;
        private IBossAttack _nextSpecialAttack;
        
        // --- NEW --- AI Combo Counter
        private int _meleeAttackCounter = 0;
        
        public bool IsFacingRight => _isFacingRight;

        // ... (All other methods like Awake, FixedUpdate, ChangeState, etc. are UNCHANGED) ...
        #region Unchanged Methods
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
            _currentState = BossState.Idle;
        }
        
        private void FixedUpdate()
        {
            if (_currentState == BossState.Idle || _currentState == BossState.Defeated || _playerTarget == null) return;

            if (_currentState == BossState.Chase)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, _playerTarget.position);
                if (distanceToPlayer <= meleeRange)
                {
                    ChangeState(BossState.Fighting);
                    return; 
                }
                FacePlayer();
                float moveDirection = _isFacingRight ? 1f : -1f;
                GetComponent<Rigidbody2D>().linearVelocity = new Vector2(moveDirection * repositionSpeed, GetComponent<Rigidbody2D>().linearVelocity.y);
            }
            
        }
        
        public void StartFight()
        {
            if (_currentState != BossState.Idle) return;
            var playerGO = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
            if (playerGO != null) { _playerTarget = playerGO.transform; }
            else { Debug.LogError("BossController could not find the Player!", this); return; }

            Debug.Log("BOSS FIGHT HAS STARTED!");
            _activeLogicCoroutine = StartCoroutine(IntroSequence()); 
        }

        private void ChangeState(BossState newState)
        {
            if (_currentState == newState) return;
            if (_activeLogicCoroutine != null) { StopCoroutine(_activeLogicCoroutine); _activeLogicCoroutine = null; }

            _currentState = newState;
            Debug.Log($"Boss state changed to: {newState}");

            switch (_currentState)
            {
                case BossState.Idle: break;
                case BossState.Intro: bossHealth.SetInvulnerability(true); break;
                case BossState.Fighting:
                    bossHealth.SetInvulnerability(false);
                    bossHealth.SetVulnerability(true);
                    visualController.SetWalking(false);
                    GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    _activeLogicCoroutine = StartCoroutine(AttackPattern());
                    break;
                case BossState.Repositioning: 
                    bossHealth.SetInvulnerability(false);
                    bossHealth.SetVulnerability(true);
                    visualController.SetWalking(true); 
                    break;
                case BossState.PhaseTransition:
                    bossHealth.SetInvulnerability(true);
                    _activeLogicCoroutine = StartCoroutine(PhaseTransitionSequence());
                    break;
                case BossState.Dizzy:
                    bossHealth.SetInvulnerability(false);
                    bossHealth.SetVulnerability(true);
                    _activeLogicCoroutine = StartCoroutine(DizzySequence());
                    break;
                case BossState.Defeated:
                    if (GetComponent<Rigidbody2D>() != null)
                    {
                        var rb = GetComponent<Rigidbody2D>();
                        rb.linearVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic; // Makes it ignore physics forces.
                    }
    
                    // 3. Disable the main collider so it can't be hit or bump into the player.
                    if (GetComponent<Collider2D>() != null)
                    {
                        GetComponent<Collider2D>().enabled = false;
                    }
    
                    // --- Your existing logic is still correct ---
                    visualController.PlayDeathAnimation();
                    FreezeAllMinions();
                    break;
                case BossState.Chase:
                    bossHealth.SetInvulnerability(false);
                    bossHealth.SetVulnerability(true);
                    visualController.SetWalking(true);
                    break;
            }
        }

        private IEnumerator IntroSequence()
        {
            ChangeState(BossState.Intro);
            yield return new WaitForSeconds(2.0f);
            visualController.PlayRoarAnimation();
            yield return new WaitForSeconds(1.5f);
            ChangeState(BossState.Fighting);
        }

        private IEnumerator PhaseTransitionSequence()
        {
            visualController.PlayRoarAnimation();
            groundSmashAttack.UpgradeAttack(_currentPhase);
            rushAttack.UpgradeAttack(_currentPhase);
            encounterTrigger?.SpawnMinionWave(_currentPhase - 2);
            yield return new WaitForSeconds(2.0f);
            ChangeState(BossState.Fighting);
        }
        
        private IEnumerator DizzySequence()
        {
            visualController.PlayStunBeginAnimation();
            yield return new WaitForSeconds(stunDuration);
            bossHealth.SetVulnerability(false);
            visualController.PlayStunEndAnimation();
            yield return new WaitForSeconds(1.0f);
            ChangeState(BossState.Fighting);
        }
        
        private IEnumerator RepositionForAttack(Vector3 targetPosition)
        {
            ChangeState(BossState.Repositioning);
            FacePosition(targetPosition);

            visualController.SetWalking(true);
            while (Mathf.Abs(transform.position.x - targetPosition.x) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(transform.position, new Vector2(targetPosition.x, transform.position.y), repositionSpeed * Time.deltaTime);
                yield return null;
            }
            visualController.SetWalking(false);
            transform.position = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
            
            FacePlayer();
            yield return StartCoroutine(_nextSpecialAttack.Execute());
            ChangeState(BossState.Fighting);
        }
        public void EnterDizzyState()
        {
            if (_currentState == BossState.Fighting || _currentState == BossState.Chase)
            {
                ChangeState(BossState.Dizzy);
            }
        }
        public void FacePlayer()
        {
            if (_playerTarget == null || visualController == null) return;
            
            bool playerIsToTheRight = (_playerTarget.position.x > transform.position.x);
            if (playerIsToTheRight != _isFacingRight)
            {
                _isFacingRight = playerIsToTheRight;
                visualController.Flip(_isFacingRight);
            }
        }
        public void FacePosition(Vector3 targetPosition)
        {
            if (visualController == null) return;
            bool targetIsToTheRight = (targetPosition.x > transform.position.x);
            if (targetIsToTheRight != _isFacingRight)
            {
                _isFacingRight = targetIsToTheRight;
                visualController.Flip(_isFacingRight);
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
        private void HandlePhaseTransition(int phaseIndex)
        {
            _currentPhase = phaseIndex + 1;
            ChangeState(BossState.PhaseTransition);
        }
        private void HandleDeath()
        {
            ChangeState(BossState.Defeated);
            
            if (bossHealth != null)
            {
                bossHealth.OnPhaseThresholdReached -= HandlePhaseTransition;
                bossHealth.OnDeath -= HandleDeath;
            }
        }
        #endregion

        // --- ENTIRELY REWRITTEN METHOD ---
        private IEnumerator AttackPattern()
        {
            while (_currentState == BossState.Fighting || _currentState == BossState.Chase)
            {
                if (_playerTarget == null) yield break;

                float distanceToPlayer = Vector2.Distance(transform.position, _playerTarget.position);

                if (distanceToPlayer > meleeRange)
                {
                    if (distanceToPlayer <= chaseRange)
                    {
                        ChangeState(BossState.Chase);
                        yield break;
                    }
                    else
                    {
                        _meleeAttackCounter = 0;
                        if (UnityEngine.Random.value > 0.5f)
                        {
                            _nextSpecialAttack = groundSmashAttack;
                            _activeLogicCoroutine = StartCoroutine(RepositionForAttack(groundSmashPosition.position));
                        }
                        else
                        {
                            _nextSpecialAttack = rushAttack;
                            bool playerIsOnRightSide = _playerTarget.position.x > transform.position.x;
                            Transform targetRushPosition = playerIsOnRightSide ? rushPositionLeft : rushPositionRight;
                            _activeLogicCoroutine = StartCoroutine(RepositionForAttack(targetRushPosition.position));
                        }
                        yield break;
                    }
                }
                
                if (_meleeAttackCounter < 2)
                {
                    FacePlayer();
                    yield return StartCoroutine(meleeSwipeAttack.Execute());
                    _meleeAttackCounter++;
                }
                else
                {
                    _meleeAttackCounter = 0;
                    FacePlayer();
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        yield return StartCoroutine(groundSmashAttack.Execute());
                    }
                    else
                    {
                        yield return StartCoroutine(rushAttack.Execute());
                    }
                }
                
                yield return new WaitForSeconds(delayBetweenAttacks);
            }
        }
    }
}