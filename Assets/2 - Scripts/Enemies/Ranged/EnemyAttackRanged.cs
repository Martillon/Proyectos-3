// En Scripts/Enemies/Ranged/EnemyAttackRanged.cs
using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core; // Para EnemyAIController

namespace Scripts.Enemies.Ranged
{
    public class EnemyAttackRanged : MonoBehaviour
    {
        // ... (Campos de Ranged Attack Settings, Line of Sight sin cambios) ...
        [Header("Ranged Attack Settings")]
        [Tooltip("Prefab of the projectile to be fired. Must have an EnemyProjectile component.")]
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("Transform from which the projectile is spawned.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Range within which the enemy will attempt to fire.")]
        [SerializeField] private float attackRange = 8f;
        [Tooltip("Cooldown time (in seconds) between ranged attacks.")]
        [SerializeField] private float attackCooldown = 2f;
        [Tooltip("Fallback duration if animation events are not used or an animation is missing.")]
        [SerializeField] private float fallbackFireSequenceDuration = 1f; 

        [Header("Line of Sight (For AimAtTargetFreely Mode)")]
        [Tooltip("If true AND AimAtTargetFreely is true, enemy will only fire if there's a clear line of sight to the player.")]
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Layers that can block line of sight (e.g., 'Ground', 'Obstacles'). Player layer should NOT be in this mask.")]
        [SerializeField] private LayerMask lineOfSightBlockers;


        
        [Tooltip("Primary aiming mode for this enemy.")]
        public enum AimingStyle { HorizontalOnly, AimFreelyAtTarget, UseFixedDirections }
        [SerializeField] private AimingStyle aimingStyle = AimingStyle.HorizontalOnly;
        
        [Tooltip("Optional: Transform of the weapon pivot that should be rotated if AimingStyle is not HorizontalOnly.")]
        [SerializeField] private Transform weaponPivotToRotate; 

        [Tooltip("Fixed directions to shoot in if AimingStyle is UseFixedDirections. Directions are in world space or relative to a non-flipping weapon pivot.")]
        [SerializeField] private Vector2[] fixedAimDirections = { Vector2.down, new Vector2(-1, -1).normalized, new Vector2(1, -1).normalized };
        
        [Header("Burst Settings")]
        [SerializeField] private int projectilesPerBurst = 1;
        [SerializeField] private float delayBetweenBurstShots = 0.1f;

        [Header("Animation")]
        [SerializeField] private string attackAnimationTriggerName = "triggerAttack"; 
        private Animator _enemyAnimator;

        // Internal State
        private float _lastAttackTimestamp;
        private EnemyAIController _aiController;
        private bool _isCurrentlyInAttackSequence = false;
        private Coroutine _attackSequenceCoroutine;
        private Transform _currentTargetForSequence;

        private void Awake()
        {
            _aiController = GetComponentInParent<EnemyAIController>();
            _enemyAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);

            // Validaciones
            if (_aiController == null) Debug.LogError($"...", this);
            if (projectilePrefab == null) Debug.LogError($"...", this);
            // ... (resto de validaciones de Awake) ...
            if (aimingStyle == AimingStyle.UseFixedDirections && (fixedAimDirections == null || fixedAimDirections.Length == 0))
                Debug.LogWarning($"EnemyAttackRanged on {gameObject.name}: AimingStyle is UseFixedDirections but no fixedAimDirections are set. Will default to horizontal.", this);
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (target == null || _isCurrentlyInAttackSequence || Time.time < _lastAttackTimestamp + attackCooldown || firePoint == null)
                return false;
            
            if (Vector2.Distance(firePoint.position, target.position) > attackRange)
                return false;

            if (aimingStyle == AimingStyle.AimFreelyAtTarget && requireLineOfSight)
            {
                if (!HasLineOfSight(target)) return false;
            }
            // Para HorizontalOnly o UseFixedDirections, el LoS es implícito o no estrictamente necesario para iniciar.
            return true;
        }

        public void TryAttack(Transform target)
        {
            if (target == null || _isCurrentlyInAttackSequence || firePoint == null) return;
            
            _currentTargetForSequence = target; // Guardar para los eventos/lógica de disparo
            if (_attackSequenceCoroutine != null) StopCoroutine(_attackSequenceCoroutine);
            _attackSequenceCoroutine = StartCoroutine(RangedAttackSequenceCoroutine());
        }

        private IEnumerator RangedAttackSequenceCoroutine()
        {
            _isCurrentlyInAttackSequence = true;
            _aiController.SetCanMove(false);

            bool useAnimationEvents = _enemyAnimator != null && !string.IsNullOrEmpty(attackAnimationTriggerName);

            if (useAnimationEvents)
            {
                _enemyAnimator.SetTrigger(attackAnimationTriggerName);
            }
            else // Fallback si no hay animación
            {
                yield return new WaitForSeconds(fallbackFireSequenceDuration * 0.3f); 
                SpawnProjectilesLogic(_currentTargetForSequence); 
                yield return new WaitForSeconds(fallbackFireSequenceDuration * 0.7f); 
                FinishRangedAttack();
                yield break; 
            }

            float animationTimeout = fallbackFireSequenceDuration + 2f; 
            float timer = 0f;
            while (_isCurrentlyInAttackSequence && timer < animationTimeout)
            {
                timer += Time.deltaTime;
                yield return null; 
            }

            if (_isCurrentlyInAttackSequence) { FinishRangedAttack(); }
        }
        
        public void SpawnProjectileEvent() // Llamado por Evento de Animación
        {
            if (!_isCurrentlyInAttackSequence || _currentTargetForSequence == null) return;
            SpawnProjectilesLogic(_currentTargetForSequence);
        }

        public void OnRangedAttackAnimationFinished() // Llamado por Evento de Animación
        {
            FinishRangedAttack();
        }

        private void SpawnProjectilesLogic(Transform target)
        {
            StartCoroutine(FireBurstCoroutine(target));
        }

        private IEnumerator FireBurstCoroutine(Transform target)
        {
            if (projectilePrefab == null || firePoint == null || _aiController == null) yield break;

            for (int i = 0; i < projectilesPerBurst; i++)
            {
                if (target == null || _aiController.IsDead) yield break;

                Vector2 finalFireDirection;
                Transform transformToRotate = weaponPivotToRotate != null ? weaponPivotToRotate : firePoint;

                switch (aimingStyle)
                {
                    case AimingStyle.HorizontalOnly:
                        finalFireDirection = _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                        if (weaponPivotToRotate != null) // Asegurar que pivote esté horizontal
                            weaponPivotToRotate.localRotation = Quaternion.identity;
                        // firePoint (si es hijo de Visuals) ya estará orientado por el flip del cuerpo
                        break;

                    case AimingStyle.AimFreelyAtTarget:
                        finalFireDirection = (target.position - firePoint.position).normalized;
                        if (transformToRotate != null) {
                            float angle = Mathf.Atan2(finalFireDirection.y, finalFireDirection.x) * Mathf.Rad2Deg;
                            transformToRotate.rotation = Quaternion.Euler(0, 0, angle);
                        }
                        break;

                    case AimingStyle.UseFixedDirections:
                        finalFireDirection = GetBestFixedDirection(target.position);
                         if (transformToRotate != null) { // Opcional: rotar el pivote a esta dirección fija
                            float angle = Mathf.Atan2(finalFireDirection.y, finalFireDirection.x) * Mathf.Rad2Deg;
                            transformToRotate.rotation = Quaternion.Euler(0, 0, angle);
                        }
                        break;
                    
                    default: // Fallback a horizontal
                        finalFireDirection = _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                         if (weaponPivotToRotate != null) weaponPivotToRotate.localRotation = Quaternion.identity;
                        break;
                }
                
                GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                EnemyProjectile projectileScript = projGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null)
                {
                    projectileScript.Initialize(finalFireDirection);
                }

                if (projectilesPerBurst > 1 && i < projectilesPerBurst - 1)
                {
                    yield return new WaitForSeconds(delayBetweenBurstShots);
                }
            }
        }

        private void FinishRangedAttack()
        {
            if (!_isCurrentlyInAttackSequence && _attackSequenceCoroutine == null && Time.time > _lastAttackTimestamp + 0.1f) return;
            _aiController.SetCanMove(true);
            _lastAttackTimestamp = Time.time;
            _isCurrentlyInAttackSequence = false; 
            _currentTargetForSequence = null; 
            if (_attackSequenceCoroutine != null) { StopCoroutine(_attackSequenceCoroutine); _attackSequenceCoroutine = null; }
        }

        private bool HasLineOfSight(Transform target) { /* ... como antes ... */ 
            if (target == null || firePoint == null) return false;
            Vector2 dir = (target.position - firePoint.position).normalized;
            float dist = Vector2.Distance(firePoint.position, target.position);
            return !Physics2D.Raycast(firePoint.position, dir, dist, lineOfSightBlockers);
        }

        private Vector2 GetBestFixedDirection(Vector3 targetPosition)
        {
            if (fixedAimDirections == null || fixedAimDirections.Length == 0)
                return (_aiController.IsFacingRight ? Vector2.right : Vector2.left); // Fallback a horizontal

            Vector2 bestDir = fixedAimDirections[0];
            if (fixedAimDirections.Length > 1)
            {
                float smallestAngleDiff = float.MaxValue;
                Vector2 directionToTargetActual = (targetPosition - firePoint.position).normalized;
                foreach (Vector2 fixedDir in fixedAimDirections)
                {
                    // Asumimos direcciones fijas en espacio del mundo
                    float angleDiff = Vector2.Angle(fixedDir, directionToTargetActual);
                    if (angleDiff < smallestAngleDiff) { smallestAngleDiff = angleDiff; bestDir = fixedDir; }
                }
            }
             // Si después de todo, bestDir es cero (ej. fixedAimDirections[0] era Vector2.zero), fallback
            return bestDir.sqrMagnitude > 0.001f ? bestDir.normalized : (_aiController.IsFacingRight ? Vector2.right : Vector2.left);
        }

        public bool IsFiring() => _isCurrentlyInAttackSequence;

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (firePoint == null || _aiController == null) return; // Necesitamos aiController para IsFacingRight

            // Rango de Ataque general
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, attackRange);

            // Origen para los gizmos de dirección (pivote del arma o firepoint)
            Vector3 gizmoOrigin = weaponPivotToRotate != null ? weaponPivotToRotate.position : firePoint.position;

            // Dibujar direcciones específicas según el modo
            switch (aimingStyle)
            {
                case AimingStyle.HorizontalOnly:
                    Gizmos.color = Color.green;
                    Vector2 horizontalDir = _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                    Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)horizontalDir * 2f);
                    break;

                case AimingStyle.AimFreelyAtTarget:
                    // Para este modo, la dirección es dinámica, no hay mucho que dibujar estáticamente
                    // a menos que tengas un target asignado en editor para debug.
                    Gizmos.color = Color.yellow;
                    // Podrías dibujar una línea hacia playerTarget si está asignado y el juego no corre
                    if (!Application.isPlaying && _aiController.playerTarget != null)
                    {
                         Gizmos.DrawLine(gizmoOrigin, _aiController.playerTarget.position);
                    } else if (Application.isPlaying && _currentTargetForSequence != null){
                         Gizmos.DrawLine(gizmoOrigin, _currentTargetForSequence.position);
                    }
                    break;

                case AimingStyle.UseFixedDirections:
                    Gizmos.color = Color.magenta;
                    if (fixedAimDirections != null && fixedAimDirections.Length > 0)
                    {
                        foreach (Vector2 dir in fixedAimDirections)
                        {
                            if (dir.sqrMagnitude > 0.001f)
                                Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)dir.normalized * 2f);
                        }
                    }
                    else // Fallback si no hay direcciones fijas, dibujar la horizontal
                    {
                        Gizmos.color = new Color(0.5f,0,0.5f); // Magenta oscuro para indicar fallback
                        Vector2 fallbackHorizontalDir = _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                        Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)fallbackHorizontalDir * 1.5f);
                    }
                    break;
            }
        }
        #endif
    }
}