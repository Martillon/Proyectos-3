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
            [Tooltip("How many copies of this enemy to spawn.")]
            [Min(1)] public int count = 1;
            [Tooltip("The main spawn point. If spawning multiple, they will spawn in a cluster around this point.")]
            public Transform spawnPoint;
            [Tooltip("If Count > 1, enemies will spawn in a random radius around the main Spawn Point. 0 means they all spawn at the exact same spot.")]
            public float spawnRadius = 0f;
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

            // Loop for the number of enemies specified in this single instruction
            for (int i = 0; i < instruction.count; i++)
            {
                Transform finalSpawnPoint = instruction.spawnPoint;

                // If no specific spawn point is assigned, try to pick a random one
                if (finalSpawnPoint == null)
                {
                    if (_availableRandomSpawns != null && _availableRandomSpawns.Count > 0)
                    {
                        int randomIndex = Random.Range(0, _availableRandomSpawns.Count);
                        finalSpawnPoint = _availableRandomSpawns[randomIndex];
                    }
                    else
                    {
                        Debug.LogWarning($"Encounter '{name}' needs a random spawn point but none are available.", this);
                        continue; // Skip this spawn if no point is available
                    }
                }
                
                // Calculate the final spawn position with the radius offset
                Vector3 spawnPosition = finalSpawnPoint.position;
                if (instruction.spawnRadius > 0)
                {
                    // Calculate a random offset ONLY on the X-axis.
                    float randomXOffset = Random.Range(-instruction.spawnRadius, instruction.spawnRadius);
                    
                    // Create the offset vector with Y and Z as zero.
                    Vector3 offset = new Vector3(randomXOffset, 0, 0);
                    
                    // Add the horizontal offset to the base spawn position.
                    spawnPosition += offset;
                }

                GameObject enemyInstance = ObjectPooler.Instance.SpawnFromPool(
                    instruction.enemyPoolTag,
                    spawnPosition,
                    finalSpawnPoint.rotation
                );
                
                if(enemyInstance == null) continue;
                
                if (enemyInstance.TryGetComponent<EnemyHealth>(out var health))
                {
                    // 1. CONFIGURE FIRST: Give all components the data they need.
                    var aiController = enemyInstance.GetComponent<EnemyAIController>();
                    aiController?.Configure(instruction.enemyStats);
                    health.Configure(instruction.enemyStats, instruction.enemyPoolTag);
            
                    // 2. RESET SECOND: Now that they have their data, tell them to run their OnObjectSpawn logic.
                    var pooledComponents = enemyInstance.GetComponentsInChildren<IPooledObject>(true);
                    foreach(var component in pooledComponents)
                    {
                        component.OnObjectSpawn();
                    }
                    
                    _activeEnemies.Add(health);
                    health.OnDeath += OnTrackedEnemyDied;
                }
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