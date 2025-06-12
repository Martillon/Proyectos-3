using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Scripts.Core;
using Scripts.Core.Pooling;
using Scripts.Enemies.Core;

namespace Scripts.Gameplay.Encounters
{
    /// <summary>
    /// Manages a combat encounter by spawning waves of enemies when triggered.
    /// Supports various activation methods, completion conditions, and events.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class EncounterTrigger : MonoBehaviour
    {
        #region Inner Classes
        [System.Serializable]
        public class SpawnInstruction
        {
            [Tooltip("The tag of the enemy prefab from the ObjectPooler (e.g., 'MeleeGrunt').")]
            public string enemyPoolTag;
            [Tooltip("The stats to apply to this spawned enemy.")]
            public EnemyStats enemyStats;
            [Tooltip("(Optional) A specific transform where this enemy will spawn. If empty, a random point from the trigger's list will be used.")]
            public Transform spawnPoint;
            [Tooltip("Delay in seconds after the wave begins before this enemy spawns.")]
            public float spawnDelay = 0f;
        }

        [System.Serializable]
        public class SpawnWave
        {
            public string waveName = "Wave";
            public List<SpawnInstruction> spawnInstructions;
            [Tooltip("Time in seconds to wait after this wave is cleared before starting the next one.")]
            public float delayAfterWave = 2.0f;
        }
        #endregion

        #region Inspector Fields
        //[Header("1. Activation")]
        public enum ActivationType { OnPlayerEnter, OnEvent, OnLevelStart }
        [Tooltip("How this encounter is triggered.")]
        [SerializeField] private ActivationType activationType = ActivationType.OnPlayerEnter;
        [Tooltip("If true, this encounter will not run again after being completed once.")]
        [SerializeField] private bool activateOnlyOnce = true;

        [Header("2. Spawning Logic")]
        [Tooltip("The waves of enemies to spawn in this encounter.")]
        [SerializeField] private List<SpawnWave> waves;
        [Tooltip("A list of potential spawn points to be used by any Spawn Instruction that doesn't have a specific point assigned.")]
        [SerializeField] private List<Transform> randomSpawnPoints;

        //[Header("3. Completion Condition")]
        private enum CompletionCondition { DefeatAllEnemies, PlayerLeavesArea }
        [Tooltip("What the player must do to complete this encounter.")]
        [SerializeField] private CompletionCondition completionCondition = CompletionCondition.DefeatAllEnemies;

        [Header("4. Actions & Events")]
        [Tooltip("(Optional) A Cinemachine Confiner to enable, locking the camera to an area.")]
        [SerializeField] private MonoBehaviour cameraConfiner; // Using MonoBehaviour to accept Confiner or Confiner2D
        [Tooltip("GameObjects to activate when the encounter starts (e.g., virtual doors).")]
        [SerializeField] private GameObject[] objectsToActivateOnStart;
        [Tooltip("GameObjects to deactivate when the encounter is completed.")]
        [SerializeField] private GameObject[] objectsToDeactivateOnComplete;
        [Space]
        public UnityEvent onEncounterStart;
        public UnityEvent onEncounterComplete;
        #endregion

        #region Private State
        private readonly List<EnemyHealth> _activeEnemies = new List<EnemyHealth>();
        private List<Transform> _availableRandomSpawns;
        private int _currentWaveIndex = -1;
        private bool _isEncounterActive = false;
        private bool _hasCompleted = false;
        #endregion
        
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Start()
        {
            if (activationType == ActivationType.OnLevelStart)
            {
                StartEncounter();
            }
        }
        
        // This allows other scripts or events to manually start the encounter
        private void StartEncounter()
        {
            if (_isEncounterActive || (activateOnlyOnce && _hasCompleted)) return;
            
            Debug.Log($"Encounter '{gameObject.name}' Started!", this);
            _isEncounterActive = true;
            _hasCompleted = false;
            
            // --- Execute Start Actions ---
            if (cameraConfiner) cameraConfiner.enabled = true;
            foreach (var obj in objectsToActivateOnStart) obj?.SetActive(true);
            onEncounterStart.Invoke();
            
            // --- Begin Spawning ---
            _currentWaveIndex = -1;
            StartNextWave();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activationType != ActivationType.OnPlayerEnter) return;
            if (!other.CompareTag(GameConstants.PlayerTag)) return;

            StartEncounter();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_isEncounterActive || completionCondition != CompletionCondition.PlayerLeavesArea) return;
            if (!other.CompareTag(GameConstants.PlayerTag)) return;
            
            // If completion is based on leaving, we end the encounter here.
            CompleteEncounter(true);
        }

        private void StartNextWave()
        {
            _currentWaveIndex++;
            if (_currentWaveIndex >= waves.Count)
            {
                // If the completion condition is defeating all enemies, and we've run out of waves, the encounter is over.
                if (completionCondition == CompletionCondition.DefeatAllEnemies)
                {
                    CompleteEncounter(true);
                }
                return;
            }

            // Reset the list of available random spawn points for the new wave
            _availableRandomSpawns = new List<Transform>(randomSpawnPoints);

            SpawnWave currentWave = waves[_currentWaveIndex];
            foreach (var instruction in currentWave.spawnInstructions)
            {
                StartCoroutine(SpawnEnemyRoutine(instruction));
            }
        }

        private IEnumerator SpawnEnemyRoutine(SpawnInstruction instruction)
        {
            yield return new WaitForSeconds(instruction.spawnDelay);

            Transform spawnPoint = instruction.spawnPoint;
            // If no specific spawn point is assigned, try to pick a random one
            if (!spawnPoint)
            {
                if (_availableRandomSpawns != null && _availableRandomSpawns.Count > 0)
                {
                    int randomIndex = Random.Range(0, _availableRandomSpawns.Count);
                    spawnPoint = _availableRandomSpawns[randomIndex];
                    _availableRandomSpawns.RemoveAt(randomIndex); // Prevent re-using the same random spot in the same wave
                }
                else
                {
                    Debug.LogWarning($"Encounter '{name}' tried to spawn an enemy randomly, but no random spawn points are available.", this);
                    yield break;
                }
            }

            GameObject enemyInstance = ObjectPooler.Instance.SpawnFromPool(
                instruction.enemyPoolTag,
                spawnPoint.position,
                spawnPoint.rotation
            );
            
            if (!enemyInstance) yield break;

            if (enemyInstance.TryGetComponent<EnemyHealth>(out var health))
            {
                var aiController = enemyInstance.GetComponent<EnemyAIController>();
                aiController?.Configure(instruction.enemyStats); // Configure injects stats into all necessary components

                _activeEnemies.Add(health);
                health.OnDeath += OnTrackedEnemyDied;
            }
        }

        private void OnTrackedEnemyDied(EnemyHealth deadEnemy)
        {
            if (deadEnemy) deadEnemy.OnDeath -= OnTrackedEnemyDied;
            _activeEnemies.Remove(deadEnemy);

            // If we are waiting for all enemies to be defeated and the list is now empty, start the next wave.
            if (completionCondition == CompletionCondition.DefeatAllEnemies && _activeEnemies.Count == 0 && _isEncounterActive)
            {
                StartCoroutine(NextWaveRoutine());
            }
        }
        
        private IEnumerator NextWaveRoutine()
        {
            // Do not proceed if the current wave is the last one
            if (_currentWaveIndex >= waves.Count - 1) yield break;

            yield return new WaitForSeconds(waves[_currentWaveIndex].delayAfterWave);
            StartNextWave();
        }

        private void CompleteEncounter(bool success)
        {
            if (!_isEncounterActive) return;

            Debug.Log($"Encounter '{gameObject.name}' Completed (Success: {success})", this);
            _isEncounterActive = false;
            _hasCompleted = true;
            
            // Clean up any remaining tracked enemies (e.g., if player ran away)
            foreach (var enemy in _activeEnemies)
            {
                if(enemy) enemy.OnDeath -= OnTrackedEnemyDied;
            }
            _activeEnemies.Clear();
            
            if (success)
            {
                // --- Execute Completion Actions ---
                if (cameraConfiner) cameraConfiner.enabled = false;
                foreach (var obj in objectsToDeactivateOnComplete) obj?.SetActive(false);
                onEncounterComplete.Invoke();
            }
        }
    }
}