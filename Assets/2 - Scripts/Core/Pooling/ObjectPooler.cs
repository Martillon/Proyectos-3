using UnityEngine;
using System.Collections.Generic;
using Scripts.Core; // You need to add this to access SceneLoader

namespace Scripts.Core.Pooling
{
    /// <summary>
    /// A static class for managing global object pooling events.
    /// Systems can subscribe to these events to react to object pool state changes
    /// without needing a direct reference to pool components.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }

        [SerializeField] private List<Pool> pools;
        private Dictionary<string, Queue<GameObject>> _poolDictionary;

        private void Awake()
        {
            Instance = this;
            // Initialize the dictionary here to prevent null reference errors
            // if SpawnFromPool is called before a scene is ready.
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        }

        // Subscribe to the scene ready event when this component is enabled.
        private void OnEnable()
        {
            SceneLoader.OnSceneReady += HandleSceneReady;
        }

        // Unsubscribe to prevent memory leaks when the component is disabled or destroyed.
        private void OnDisable()
        {
            SceneLoader.OnSceneReady -= HandleSceneReady;
        }

        /// <summary>
        /// This method is called by the SceneLoader.OnSceneReady event.
        /// It's the new entry point for creating our pools.
        /// </summary>
        private void HandleSceneReady()
        {
            // First, clear out any objects from a previously loaded scene.
            ClearAllPools();

            // Now, create the pools for the new scene.
            // Since SceneLoader has already set the new scene as active,
            // Instantiate() will place these objects in the correct scene.
            CreatePools();
        }

        private void CreatePools()
        {
            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                _poolDictionary.Add(pool.tag, objectPool);
            }

            Debug.Log("ObjectPooler: Pools created for the new scene.");
        }

        /// <summary>
        /// Destroys all currently pooled GameObjects and clears the dictionary.
        /// Essential for cleaning up between scene loads.
        /// </summary>
        private void ClearAllPools()
        {
            foreach (var pair in _poolDictionary)
            {
                Queue<GameObject> objectQueue = pair.Value;
                foreach (GameObject obj in objectQueue)
                {
                    // Important: check if the object hasn't been destroyed already (e.g. by scene unload)
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }

            _poolDictionary.Clear();
            Debug.Log("ObjectPooler: All previous pools cleared.");
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning(
                    $"Pool with tag '{tag}' doesn't exist. It might not be configured or the scene isn't ready.");
                return null;
            }

            // A problem can occur if you try to spawn more objects than are in the pool.
            // Let's add a check for that.
            if (_poolDictionary[tag].Count == 0)
            {
                Debug.LogWarning($"Pool with tag '{tag}' is empty. Consider increasing its size.");
                // Optional: Instantiate a new one on the fly (can cause performance spikes)
                // Pool pool = pools.Find(p => p.tag == tag);
                // if (pool != null) return Instantiate(pool.prefab, position, rotation);
                return null;
            }

            GameObject objectToSpawn = _poolDictionary[tag].Dequeue();

            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;

            return objectToSpawn;
        }

        public void ReturnToPool(string tag, GameObject objectToReturn)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning(
                    $"Pool with tag '{tag}' doesn't exist. This can happen during a scene transition. Destroying object instead.");
                Destroy(objectToReturn);
                return;
            }

            objectToReturn.SetActive(false);
            _poolDictionary[tag].Enqueue(objectToReturn);
        }
    }

    // An interface for objects that need to reset their state when spawned from a pool.
    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}
