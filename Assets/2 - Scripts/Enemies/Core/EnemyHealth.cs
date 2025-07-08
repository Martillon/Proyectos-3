using UnityEngine;
using System.Collections;
using System.Linq;
using Scripts.Core.Interfaces;
using Scripts.Core.Pooling;

namespace Scripts.Enemies.Core
{
    public class EnemyHealth : MonoBehaviour, IDamageable, IInstakillable, IPooledObject
    {
        public event System.Action<EnemyHealth> OnDeath;
        
        private EnemyStats _stats;
        private float _currentHealth;
        private bool _isDead;
        
        // Component references
        private EnemyAIController _aiController;
        private Collider2D _collider;
        
        private string _poolTag;

        private void Awake()
        {
            _aiController = GetComponent<EnemyAIController>();
            _collider = GetComponent<Collider2D>();
        }

        // This is called by the spawner to inject stats
        public void Configure(EnemyStats stats, string poolTag) 
        {
            this._stats = stats;
            this._poolTag = poolTag; 
        }

        // This is called by the ObjectPooler when this enemy is spawned
        public void OnObjectSpawn()
        {
            _isDead = false;
            if (_collider) _collider.enabled = true;

            if (_stats != null)
            {
                _currentHealth = _stats.maxHealth;
            }
        
            // This is a great idea, but let's have each component handle its own reset.
            // We'll call their OnObjectSpawn methods directly from the EnemyAIController.
            // So we can simplify this part.
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            if (_currentHealth <= 0) Die();
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            OnDeath?.Invoke(this);
            OnDeath = null;

            _aiController.NotifyOfDeath();
        
            // Disable the collider immediately so it can't be hit again
            if(_collider) _collider.enabled = false;

            StartCoroutine(ReturnToPoolAfterDelay(2.0f));
        }
        
        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
        
            // --- FIX ---
            // We don't call SpawnFromPool here. We call a dedicated ReturnToPool method.
            // And we use the tag we saved earlier.
            ObjectPooler.Instance.ReturnToPool(_poolTag, this.gameObject);
        }

        public void ApplyInstakill() => Die();
    }
}