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
        // Singleton pattern for easy access from any script.
        public static FallingObjectManager Instance { get; private set; }

        [Header("Core Prefabs")]
        [Tooltip("The prefab for the warning indicator that appears on the ground.")]
        [SerializeField] private GameObject warningIndicatorPrefab;
        [Tooltip("The prefab for the power-up indicator that appears on the ground.")]
        [SerializeField] private GameObject powerupIndicatorPrefab;

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        // --- PUBLIC SPAWNING METHODS ---

        /// <summary>
        /// Initiates a volley of dangerous falling hazards.
        /// </summary>
        /// <param name="hazardPrefabs">A list of possible decoration prefabs to use as hazards.</param>
        /// <param name="spawnPoints">A list of potential ceiling locations to drop from.</param>
        /// <param name="count">How many hazards to drop.</param>
        /// <param name="fallSpeed">How fast the hazards will fall.</param>
        /// <param name="lifetime">Max lifetime of the hazard if it hits nothing.</param>
        public void SpawnHazardVolley(List<GameObject> hazardPrefabs, List<Transform> spawnPoints, int count, float fallSpeed, float lifetime)
        {
            // This is a "wrapper" method that validates the input and then starts the generic spawning process.
            SpawnObjectVolley(hazardPrefabs, spawnPoints, count, fallSpeed, lifetime, true);
        }

        /// <summary>
        /// Initiates a volley of helpful falling power-ups.
        /// </summary>
        public void SpawnPowerupVolley(List<GameObject> powerupPrefabs, List<Transform> spawnPoints, int count, float fallSpeed, float lifetime)
        {
            SpawnObjectVolley(powerupPrefabs, spawnPoints, count, fallSpeed, lifetime, false);
        }
        
        // --- CORE LOGIC ---

        /// <summary>
        /// The generic internal method that handles the spawning logic for any type of object.
        /// </summary>
        private void SpawnObjectVolley(List<GameObject> prefabs, List<Transform> spawnPoints, int count, float fallSpeed, float lifetime, bool isHazard)
        {
            // Validate all inputs to prevent errors.
            if (prefabs == null || prefabs.Count == 0) { Debug.LogError("Spawn Volley called with no prefabs!", this); return; }
            if (spawnPoints == null || spawnPoints.Count == 0) { Debug.LogError("Spawn Volley called with no spawn points!", this); return; }
            
            // Randomly select a subset of the available spawn points.
            List<Transform> chosenSpawnPoints = spawnPoints.OrderBy(x => Random.value).Take(count).ToList();
            
            // Start a separate coroutine for each object so they all drop concurrently.
            foreach (Transform spawnPoint in chosenSpawnPoints)
            {
                // Randomly pick a prefab from the provided list for this specific drop.
                GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Count)];
                StartCoroutine(HandleSingleFallingObject(prefabToSpawn, spawnPoint, fallSpeed, lifetime, isHazard));
            }
        }

        /// <summary>
        /// The coroutine that manages the full sequence for ONE falling object, from telegraph to drop.
        /// </summary>
        private IEnumerator HandleSingleFallingObject(GameObject objectPrefab, Transform spawnPoint, float fallSpeed, float lifetime, bool isHazard)
        {
            // --- 1. Find Landing Spot & Telegraph ---
            Vector3 landingPosition = spawnPoint.position;
            RaycastHit2D hit = Physics2D.Raycast(spawnPoint.position, Vector2.down, 100f, groundLayer);
            if (hit.collider != null)
            {
                landingPosition = hit.point;
            }

            GameObject indicatorInstance = Instantiate(warningIndicatorPrefab, landingPosition, Quaternion.identity);

            // --- 2. Wait for Telegraph Duration ---
            yield return new WaitForSeconds(defaultWarningDuration);
            
            // --- 3. Cleanup Warning & Spawn Object ---
            Destroy(indicatorInstance);
            GameObject newInstance = Instantiate(objectPrefab, spawnPoint.position, spawnPoint.rotation);

            // --- 4. "Weaponize" or "Activate" the Object ---
            // Add a Rigidbody so it can fall.
            Collider2D col = newInstance.GetComponent<Collider2D>();
            if (col == null)
            {
                // If it doesn't have one, add a simple BoxCollider2D as a fallback.
                col = newInstance.AddComponent<BoxCollider2D>();
                Debug.LogWarning($"Prefab '{objectPrefab.name}' was missing a Collider2D. A BoxCollider2D was added automatically.", newInstance);
            }
            
            Rigidbody2D rb = newInstance.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f; // A good default gravity.

            if (isHazard)
            {
                FallingHazard hazardScript = newInstance.AddComponent<FallingHazard>();
                hazardScript.Drop(fallSpeed, lifetime);
            }
            else // It's a power-up
            {
                col.isTrigger = true; // Make sure the collider is a trigger for pickup.
                FallingPowerup powerupScript = newInstance.AddComponent<FallingPowerup>();
                powerupScript.Drop(fallSpeed, lifetime);
            }
        }
    }
}