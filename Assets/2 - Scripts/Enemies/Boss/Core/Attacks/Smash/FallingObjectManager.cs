using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Enemies.Boss.Attacks.Smash
{
    /// <summary>
    /// A singleton manager that orchestrates the spawning of falling objects. It receives
    /// high-level commands to drop either hazards or power-ups, handling the telegraphing
    /// and dynamic component injection for each.
    /// </summary>
    public class FallingObjectManager : MonoBehaviour
    {
        // Singleton pattern for easy access from any script (e.g., FallingObjectManager.Instance).
        public static FallingObjectManager Instance { get; private set; }

        [Header("Core Prefabs")]
        [Tooltip("The prefab for the warning indicator that appears on the ground to signal a hazard.")]
        [SerializeField] private GameObject warningIndicatorPrefab;
        // You could add a different indicator for power-ups if desired.
        // [SerializeField] private GameObject powerupIndicatorPrefab;

        [Header("Default Drop Settings")]
        [Tooltip("How long the warning indicator is visible before the object drops.")]
        [SerializeField] private float defaultWarningDuration = 1.5f;
        [Tooltip("The layer mask representing solid ground, used for finding the landing spot.")]
        [SerializeField] private LayerMask groundLayer;
        
        /// <summary>
        /// Standard singleton setup. Ensures only one instance of this manager exists.
        /// </summary>
        private void Awake()
        {
            // If an instance already exists and it's not this one, destroy this new one.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                // Otherwise, set this as the one and only instance.
                Instance = this;
            }
        }

        // --- PUBLIC SPAWNING METHODS ---

        /// <summary>
        /// Initiates a volley of dangerous falling hazards. This is the method the boss attack script will call.
        /// </summary>
        /// <param name="hazardPrefabs">A list of possible decoration prefabs to use as hazards.</param>
        /// <param name="spawnPoints">A list of potential ceiling locations to drop from.</param>
        /// <param name="count">How many hazards to drop in this volley.</param>
        /// <param name="fallSpeed">How fast the hazards will fall.</param>
        /// <param name="lifetime">Max lifetime of the hazard if it hits nothing.</param>
        public void SpawnHazardVolley(List<GameObject> hazardPrefabs, List<Transform> spawnPoints, int count, float fallSpeed, float lifetime)
        {
            // This method simply calls the generic internal method, telling it that these are hazards.
            SpawnObjectVolley(hazardPrefabs, spawnPoints, count, fallSpeed, lifetime, true);
        }

        /// <summary>
        /// Initiates a volley of helpful falling power-ups.
        /// </summary>
        public void SpawnPowerupVolley(List<GameObject> powerupPrefabs, List<Transform> spawnPoints, int count, float fallSpeed, float lifetime)
        {
            // This method calls the same internal method, but tells it these are NOT hazards (i.e., they are power-ups).
            SpawnObjectVolley(powerupPrefabs, spawnPoints, count, fallSpeed, lifetime, false);
        }
        
        // --- CORE LOGIC ---

        /// <summary>
        /// The generic internal method that handles the spawning logic for any type of object.
        /// </summary>
        private void SpawnObjectVolley(List<GameObject> prefabs, List<Transform> spawnPoints, int count, float fallSpeed, float lifetime, bool isHazard)
        {
            // Validate all inputs to prevent errors from misconfiguration in the Inspector.
            if (prefabs == null || prefabs.Count == 0) { Debug.LogError("Spawn Volley called with no prefabs!", this); return; }
            if (spawnPoints == null || spawnPoints.Count == 0) { Debug.LogError("Spawn Volley called with no spawn points!", this); return; }
            
            // This is a clean way to get a random subset of spawn points without duplicates.
            List<Transform> chosenSpawnPoints = spawnPoints.OrderBy(x => Random.value).Take(count).ToList();
            
            // Start a separate coroutine for each object so they all drop concurrently and independently.
            foreach (Transform spawnPoint in chosenSpawnPoints)
            {
                // Randomly pick a prefab from the provided list for this specific drop (e.g., a crate or a pipe).
                GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Count)];
                StartCoroutine(HandleSingleFallingObject(prefabToSpawn, spawnPoint, fallSpeed, lifetime, isHazard));
            }
        }

        /// <summary>
        /// The coroutine that manages the full sequence for ONE falling object, from telegraph to drop.
        /// </summary>
        private IEnumerator HandleSingleFallingObject(GameObject objectPrefab, Transform spawnPoint, float fallSpeed, float lifetime, bool isHazard)
        {
            // --- 1. FIND LANDING SPOT & TELEGRAPH ---
            Vector3 landingPosition = spawnPoint.position;
            // Raycast downwards from the ceiling spawn point to find the exact landing spot on the ground.
            RaycastHit2D hit = Physics2D.Raycast(spawnPoint.position, Vector2.down, 100f, groundLayer);
            if (hit.collider != null)
            {
                landingPosition = hit.point;
            }

            // Instantiate the warning indicator at the landing spot.
            // Note: You could use a different prefab for power-ups here if desired.
            GameObject indicatorInstance = Instantiate(warningIndicatorPrefab, landingPosition, Quaternion.identity);

            // --- 2. WAIT FOR TELEGRAPH DURATION ---
            yield return new WaitForSeconds(defaultWarningDuration);
            
            // --- 3. CLEANUP WARNING & SPAWN OBJECT ---
            Destroy(indicatorInstance);
            GameObject newInstance = Instantiate(objectPrefab, spawnPoint.position, spawnPoint.rotation);

            // --- 4. ACTIVATE THE OBJECT ---
            // This is where the logic splits based on whether we're spawning a hazard or a power-up.

            if (isHazard)
            {
                // "Weaponize" the harmless decoration prefab.
                newInstance.AddComponent<Rigidbody2D>().gravityScale = 3f;
                FallingHazard hazardScript = newInstance.AddComponent<FallingHazard>();
                hazardScript.Drop(fallSpeed, lifetime);
            }
            else // It's a power-up
            {
                // The power-up is already set up. We just need to give it physics to make it fall.
                if (newInstance.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = Vector2.down * fallSpeed;
                }
                else
                {
                    // As a safety fallback, add a Rigidbody if the prefab is missing one.
                    rb = newInstance.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 3f;
                    rb.linearVelocity = Vector2.down * fallSpeed;
                }
                
                // Set a lifetime so it disappears if the player misses it.
                Destroy(newInstance, lifetime);
            }
        }
    }
}