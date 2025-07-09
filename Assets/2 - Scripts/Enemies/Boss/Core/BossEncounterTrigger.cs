using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events; 
using Scripts.Core;
using Scripts.Core.Pooling;
using Scripts.Core.Progression;
using Scripts.Enemies.Core;
using Scripts.Player.Core;

namespace Scripts.Enemies.Boss.Core
{
    
    /// <summary>
    /// Manages a dedicated boss battle encounter. It controls the arena state (doors, camera),
    /// activates the boss, and listens for the boss's defeat to complete the encounter.
    /// It can also be instructed to spawn waves of minions during the fight.
    /// </summary>
    
    public class BossEncounterTrigger : MonoBehaviour
    {
        // These inner classes define the structure for spawning optional minion waves.
        // They are identical to the ones in the original EncounterTrigger.
        #region Inner Classes
        [System.Serializable]
        public class SpawnInstruction
        {
            public string enemyPoolTag;
            public EnemyStats enemyStats;
            [Min(1)] public int count = 1;
            public Transform spawnPoint;
            public float spawnRadius = 0f;
            public float spawnDelay = 0f;
        }

        [System.Serializable]
        public class SpawnWave
        {
            public string waveName = "Wave";
            public List<SpawnInstruction> spawnInstructions;
        }
        #endregion

        //[Header("1. Activation")]
        public enum ActivationType { OnPlayerEnter, OnLevelStart }
        [Tooltip("How this encounter is triggered. 'OnLevelStart' is typical for boss fights.")]
        [SerializeField] private ActivationType activationType = ActivationType.OnLevelStart;

        [Header("2. Boss & Minions")]
        [Tooltip("The main BossController in the scene to be activated for this encounter.")]
        [SerializeField] private BossController bossToActivate;
        [Tooltip("(Optional) Waves of minions that can be spawned during the boss fight.")]
        [SerializeField] private List<SpawnWave> minionWaves;
        [Tooltip("(Optional) A list of spawn points to be used if an instruction doesn't specify one.")]
        [SerializeField] private List<Transform> randomSpawnPoints;

        [Header("3. Arena Control & Events")]
        [Tooltip("(Optional) A Cinemachine Confiner to lock the camera to the arena.")]
        [SerializeField] private MonoBehaviour cameraConfiner;
        [Tooltip("GameObjects to activate when the encounter starts (e.g., virtual doors).")]
        [SerializeField] private GameObject[] objectsToActivateOnStart;
        [Tooltip("GameObjects to activate when the encounter is completed (e.g., the LevelExit).")]
        [SerializeField] private GameObject[] objectsToActivateOnComplete;
        [Space]
        
        [Header("4. Victory Sequence")]
        [Tooltip("How long to wait after the boss dies for its death animation to complete.")]
        [SerializeField] private float bossDeathAnimationDuration = 3.0f;
        [Tooltip("How long the player's victory pose animation lasts.")]
        [SerializeField] private float playerVictoryAnimationDuration = 2.0f;
        
        public UnityEvent onEncounterStart;
        public UnityEvent onEncounterComplete;

        // --- Private State ---
        // We will need to track spawned minions if we want the BossController to be able to freeze them.
        private readonly List<EnemyAIController> _activeMinions = new List<EnemyAIController>();
        public IReadOnlyList<EnemyAIController> ActiveMinions => _activeMinions;
        private bool _isEncounterActive = false;
        private BoxCollider2D _triggerCollider;

        private void Awake()
        {
            // Get the trigger collider for 'OnPlayerEnter' activation.
            _triggerCollider = GetComponent<BoxCollider2D>();
            _triggerCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            // Subscribe to the SceneLoader event if we're starting automatically.
            if (activationType == ActivationType.OnLevelStart)
            {
                SceneLoader.OnSceneReady += StartEncounter;
            }
        }

        private void OnDisable()
        {
            // Always unsubscribe from static events to prevent memory leaks.
            if (activationType == ActivationType.OnLevelStart)
            {
                SceneLoader.OnSceneReady -= StartEncounter;
            }
        }

        /// <summary>
        /// This is the main entry point for the encounter.
        /// It can be called by an event or a trigger collider.
        /// </summary>
        public void StartEncounter()
        {
            if (_isEncounterActive) return;

            Debug.Log($"BOSS ENCOUNTER '{gameObject.name}' Started!", this);
            _isEncounterActive = true;
            
            // --- Execute Start Actions ---
            // Disable the entry trigger so it can't be fired again.
            _triggerCollider.enabled = false;
            
            if (cameraConfiner) cameraConfiner.enabled = true;
            foreach (var obj in objectsToActivateOnStart) obj?.SetActive(true);
            onEncounterStart.Invoke();
            
            // --- Activate the Boss ---
            if (bossToActivate != null)
            {
                // Subscribe to the boss's death event. This is our win condition.
                bossToActivate.GetComponent<BossHealth>().OnDeath += OnBossDefeated;
                
                // Tell the boss to begin its intro sequence.
                bossToActivate.StartFight();
            }
            else
            {
                Debug.LogError("BossEncounterTrigger has no 'bossToActivate' assigned!", this);
            }
        }

        /// <summary>
        /// Handles the OnPlayerEnter trigger activation method.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activationType != ActivationType.OnPlayerEnter) return;
            if (!other.CompareTag(GameConstants.PlayerTag)) return;

            StartEncounter();
        }
        
        /// <summary>
        /// A public method that the BossController can call to trigger a minion wave.
        /// </summary>
        /// <param name="waveIndex">The index of the wave to spawn from the 'minionWaves' list.</param>
        public void SpawnMinionWave(int waveIndex)
        {
            if (!_isEncounterActive) return;
            if (waveIndex < 0 || waveIndex >= minionWaves.Count)
            {
                Debug.LogWarning($"Attempted to spawn invalid minion wave index: {waveIndex}", this);
                return;
            }

            Debug.Log($"Boss is commanding minion wave '{minionWaves[waveIndex].waveName}' to spawn.");

            SpawnWave currentWave = minionWaves[waveIndex];
            foreach (var instruction in currentWave.spawnInstructions)
            {
                StartCoroutine(SpawnEnemyRoutine(instruction));
            }
        }


        // This coroutine is largely the same as the original EncounterTrigger.
        private IEnumerator SpawnEnemyRoutine(SpawnInstruction instruction)
        {
            yield return new WaitForSeconds(instruction.spawnDelay);

            for (int i = 0; i < instruction.count; i++)
            {
                Transform spawnPoint = instruction.spawnPoint ?? GetRandomSpawnPoint();
                if (spawnPoint == null)
                {
                    Debug.LogWarning($"No spawn point available for minion '{instruction.enemyPoolTag}'. Skipping.", this);
                    continue;
                }

                Vector3 spawnPosition = spawnPoint.position + (Vector3)(Random.insideUnitCircle * instruction.spawnRadius);

                // 1. GET the object from the pool. The pooler no longer calls OnObjectSpawn.
                GameObject enemyInstance = ObjectPooler.Instance.SpawnFromPool(instruction.enemyPoolTag, spawnPosition, spawnPoint.rotation);
                if (enemyInstance == null) continue;

                if (enemyInstance.TryGetComponent<EnemyHealth>(out var health))
                {
                    var aiController = enemyInstance.GetComponent<EnemyAIController>();

                    // 2. CONFIGURE all components with the necessary data first.
                    aiController?.Configure(instruction.enemyStats);
                    health.Configure(instruction.enemyStats, instruction.enemyPoolTag);

                    // 3. RESET the object now that its data is ready.
                    // We find all components that implement IPooledObject and call their reset method.
                    var pooledComponents = enemyInstance.GetComponentsInChildren<IPooledObject>(true);
                    foreach (var component in pooledComponents)
                    {
                        component.OnObjectSpawn();
                    }

                    // 4. TRACK the minion for freezing and completion logic.
                    _activeMinions.Add(aiController); // We track the AI controller to call Freeze().
                    health.OnDeath += (deadEnemy) => OnMinionDied(aiController);
                }
            }
        }
        
        private void OnMinionDied(EnemyAIController deadMinionAI)
        {
            if (_activeMinions.Contains(deadMinionAI))
            {
                _activeMinions.Remove(deadMinionAI);
            }
        }
        
        private Transform GetRandomSpawnPoint()
        {
            if (randomSpawnPoints == null || randomSpawnPoints.Count == 0) return null;
            return randomSpawnPoints[Random.Range(0, randomSpawnPoints.Count)];
        }

        /// <summary>
        /// This method is called by the OnDeath event from the BossHealth component.
        /// It now starts the fully automatic victory cinematic.
        /// </summary>
        private void OnBossDefeated()
        {
            StartCoroutine(VictorySequence());
        }

        private IEnumerator VictorySequence()
        {
            if (!_isEncounterActive) yield break;
            _isEncounterActive = false;

            Debug.Log("BOSS DEFEATED! Starting victory sequence.");

            // --- Step A: Wait for the boss's death animation ---
            // This gives time for the boss's on-screen explosion or collapse to finish.
            yield return new WaitForSeconds(bossDeathAnimationDuration);

            // --- Step B: Trigger the player's victory animation ---
            GameObject player = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
            if (player != null)
            {
                // We assume the player has an Animator and a trigger named "TriggerVictory".
                // This is a good example of decoupling; this script doesn't need to know
                // about a specific "PlayerAnimationController" script.
                Animator playerAnimator = player.GetComponentInChildren<Animator>();
                playerAnimator?.SetTrigger("Victory");
                Debug.Log("Triggering player victory animation.");
            }

            // --- Step C: Wait for the player's animation to finish ---
            yield return new WaitForSeconds(playerVictoryAnimationDuration);

            // --- Step D: Execute the core game logic ---
            // This is the logic that was previously in LevelExit.cs.
            Debug.Log("Updating progression and session data.");
            if (SessionManager.IsOnBounty)
            {
                Bounty currentBounty = SessionManager.ActiveBounty;
                // Save permanent progress.
                ProgressionManager.Instance.CompleteBounty(currentBounty.bountyID);
                // End the temporary session data.
                SessionManager.EndSession();

                // --- Step E: Fire the event to show the UI ---
                // The LevelCompleteUIController is listening for this event.
                Debug.Log("Raising LevelCompleted event to show UI.");
                PlayerEvents.RaiseLevelCompleted(currentBounty.title);
            }
            else
            {
                Debug.LogError("Tried to complete victory sequence, but no bounty was active in SessionManager!");
            }

            // --- Final Cleanup ---
            if (cameraConfiner) cameraConfiner.enabled = false;
            onEncounterComplete.Invoke(); // Fire this for any extra scene logic (e.g., turning on lights).

            // Unsubscribe from the boss death event.
            if (bossToActivate != null)
            {
                bossToActivate.GetComponent<BossHealth>().OnDeath -= OnBossDefeated;
            }
        }
    }
}