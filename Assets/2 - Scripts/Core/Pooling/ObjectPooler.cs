using UnityEngine;
using System.Collections.Generic;

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
        }

        private void Start()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();

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
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
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
                Debug.LogWarning($"Pool with tag '{tag}' doesn't exist. Destroying object instead.");
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
