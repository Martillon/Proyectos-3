/// En Scripts/Enemies/Ranged/EnemyAttackRanged.cs
using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core; 

namespace Scripts.Enemies.Ranged
{
    
    public class EnemyAttackRanged : MonoBehaviour
    {
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

        [Header("Line of Sight (For AimFreelyAtTarget Style)")]
        [Tooltip("If true AND AimingStyle is AimFreelyAtTarget, enemy will only fire if there's a clear line of sight to the player.")]
        [SerializeField] private bool requireLineOfSightForFreeAim = true;
        [Tooltip("Layers that can block line of sight.")]
        [SerializeField] private LayerMask lineOfSightBlockers;

        
        public enum AimingStyleOptions { HorizontalOnly, AimFreelyAtTarget, UseFixedDirections }
        [Tooltip("Primary aiming mode for this enemy.")]
        [SerializeField] private AimingStyleOptions aimingStyle = AimingStyleOptions.HorizontalOnly;
        
        [Tooltip("Optional: Transform of the weapon pivot that should be rotated if AimingStyle is not HorizontalOnly.")]
        [SerializeField] private Transform weaponPivotToRotate; 

        [Tooltip("Fixed directions to shoot in if AimingStyle is UseFixedDirections. Directions are in world space or relative to a non-flipping weapon pivot if assigned, otherwise relative to enemy's forward.")]
        [SerializeField] private Vector2[] fixedAimDirections = { Vector2.down, new Vector2(-0.707f, -0.707f), new Vector2(0.707f, -0.707f) }; // Normalizados
        
        [Header("Burst Settings")]
        [SerializeField] private int projectilesPerBurst = 1;
        [SerializeField] private float delayBetweenBurstShots = 0.1f;

        [Header("Animation (Optional)")]
        [Tooltip("Name of the animation trigger parameter in the Animator (used if NOT a static window enemy whose AIController handles Attack trigger).")]
        [SerializeField] private string attackAnimationTriggerName = "triggerAttack"; 
        private Animator _enemyAnimator; // Animator en este GO o en un hijo (ej. en Visuals)

        // Internal State
        private float _lastAttackTimestamp;
        private EnemyAIController _aiController; // Referencia al AIController principal en el Root
        private bool _isCurrentlyInAttackSequence = false;
        private Coroutine _attackSequenceCoroutine;
        private Transform _currentTargetForSequence;

        private void Awake()
        {
            _aiController = GetComponentInParent<EnemyAIController>();
            // Si este script está en Visuals (junto con el Animator):
            _enemyAnimator = GetComponent<Animator>(); 
            // Si el Animator está en un hijo de donde está este script:
            // if (_enemyAnimator == null) _enemyAnimator = GetComponentInChildren<Animator>(true);
            // Si el Animator está en el VisualsContainer y este script está en el Root (menos probable para el de ventana):
            // if (_aiController != null && _aiController.visualsContainerTransform != null) 
            // _enemyAnimator = _aiController.visualsContainerTransform.GetComponentInChildren<Animator>(true);


            if (_aiController == null) Debug.LogError($"EAR on '{gameObject.name}': EnemyAIController not found on parent!", this);
            if (projectilePrefab == null) Debug.LogError($"EAR on '{gameObject.name}': Projectile Prefab is not assigned.", this);
            if (projectilePrefab != null && projectilePrefab.GetComponent<EnemyProjectile>() == null) Debug.LogError($"EAR on '{gameObject.name}': ProjectilePrefab is missing EnemyProjectile.", this);
            if (firePoint == null) Debug.LogError($"EAR on '{gameObject.name}': Fire Point is not assigned.", this);
            
            if (string.IsNullOrEmpty(attackAnimationTriggerName) && _enemyAnimator != null && !_aiController.isStaticWindowEnemy) // Log si es un enemigo móvil que debería tener anim de ataque pero no trigger
                Debug.LogWarning($"EAR on '{gameObject.name}': Animator found but attackAnimationTriggerName not set for non-window enemy. Animation trigger might be missed.", this);
            
            if (aimingStyle == AimingStyleOptions.UseFixedDirections && (fixedAimDirections == null || fixedAimDirections.Length == 0))
                Debug.LogWarning($"EAR on {gameObject.name}: AimingStyle is UseFixedDirections but no fixedAimDirections are set. Will default to horizontal.", this);
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (target == null || _isCurrentlyInAttackSequence || Time.time < _lastAttackTimestamp + attackCooldown || firePoint == null)
                return false;
            
            if (Vector2.Distance(firePoint.position, target.position) > attackRange)
                return false;

            if (aimingStyle == AimingStyleOptions.AimFreelyAtTarget && requireLineOfSightForFreeAim)
            {
                if (!HasLineOfSight(target)) return false;
            }
            return true;
        }

        public void TryAttack(Transform target)
        {
            if (target == null || _isCurrentlyInAttackSequence || firePoint == null) return;
            
            _currentTargetForSequence = target;
            if (_attackSequenceCoroutine != null) StopCoroutine(_attackSequenceCoroutine);
            _attackSequenceCoroutine = StartCoroutine(RangedAttackSequenceCoroutine());
        }

        private IEnumerator RangedAttackSequenceCoroutine()
        {
            _isCurrentlyInAttackSequence = true;
            _aiController.SetCanMove(false);

            // Para enemigos de rango móviles, este script dispara su propia animación de ataque.
            // Para el enemigo de ventana, EnemyAIController maneja los triggers de abrir/apuntar/atacar.
            bool triggerOwnAnimation = !_aiController.isStaticWindowEnemy && _enemyAnimator != null && !string.IsNullOrEmpty(attackAnimationTriggerName);

            if (triggerOwnAnimation)
            {
                _enemyAnimator.SetTrigger(attackAnimationTriggerName);
            }
            else if (!_aiController.isStaticWindowEnemy) // Si es móvil pero sin anim/trigger configurado aquí
            {
                Debug.LogWarning($"[{Time.frameCount}] EAR: Mobile Ranged Enemy - No Animator/trigger for attack. Using fallback timing.");
                yield return new WaitForSeconds(fallbackFireSequenceDuration * 0.3f); 
                SpawnProjectilesLogic(_currentTargetForSequence); 
                yield return new WaitForSeconds(fallbackFireSequenceDuration * 0.7f); 
                FinishRangedAttack();
                yield break; 
            }
            // Si es isStaticWindowEnemy, la corrutina solo espera los eventos de su Animator específico.

            float animationTimeout = (_aiController.isStaticWindowEnemy ? 5f : fallbackFireSequenceDuration) + 2f; 
            float timer = 0f;
            while (_isCurrentlyInAttackSequence && timer < animationTimeout)
            {
                timer += Time.deltaTime;
                yield return null; 
            }

            if (_isCurrentlyInAttackSequence) { Debug.LogWarning($"EAR: Attack TIMED OUT for {gameObject.name}."); FinishRangedAttack(); }
        }
        
        public void SpawnProjectileEvent() 
        {
            if (!_isCurrentlyInAttackSequence || _currentTargetForSequence == null) return;
            SpawnProjectilesLogic(_currentTargetForSequence);
        }

        public void OnRangedAttackAnimationFinished() { FinishRangedAttack(); }

        private void SpawnProjectilesLogic(Transform target) { StartCoroutine(FireBurstCoroutine(target)); }

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
                    case AimingStyleOptions.HorizontalOnly:
                        finalFireDirection = _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                        if (weaponPivotToRotate != null) weaponPivotToRotate.localRotation = Quaternion.identity;
                        break;
                    case AimingStyleOptions.AimFreelyAtTarget:
                        finalFireDirection = (target.position - firePoint.position).normalized;
                        if (transformToRotate != null) {
                            float angle = Mathf.Atan2(finalFireDirection.y, finalFireDirection.x) * Mathf.Rad2Deg;
                            transformToRotate.rotation = Quaternion.Euler(0, 0, angle); }
                        break;
                    case AimingStyleOptions.UseFixedDirections:
                        finalFireDirection = GetCalculatedBestFixedDirection(target.position);
                        if (transformToRotate != null) { // Opcional: rotar el pivote a esta dirección fija
                            float angle = Mathf.Atan2(finalFireDirection.y, finalFireDirection.x) * Mathf.Rad2Deg;
                            transformToRotate.rotation = Quaternion.Euler(0, 0, angle); }
                        break;
                    default: 
                        finalFireDirection = _aiController.IsFacingRight ? Vector2.right : Vector2.left;
                        if (weaponPivotToRotate != null) weaponPivotToRotate.localRotation = Quaternion.identity;
                        break;
                }
                
                GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                EnemyProjectile projectileScript = projGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null) projectileScript.Initialize(finalFireDirection);

                if (projectilesPerBurst > 1 && i < projectilesPerBurst - 1) yield return new WaitForSeconds(delayBetweenBurstShots);
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
            if (target == null || firePoint == null) return false; Vector2 dir = (target.position - firePoint.position).normalized; float dist = Vector2.Distance(firePoint.position, target.position); return !Physics2D.Raycast(firePoint.position, dir, dist, lineOfSightBlockers);
        }

        public Vector2 GetCalculatedBestFixedDirection(Vector3 targetPosition) // Público para AIController
        {
            if (fixedAimDirections == null || fixedAimDirections.Length == 0)
                return (_aiController.IsFacingRight ? Vector2.right : Vector2.left); 

            Vector2 bestDir = fixedAimDirections[0];
            if (fixedAimDirections.Length > 1) {
                float smallestAngleDiff = float.MaxValue;
                Vector2 dirToTarget = ((Vector2)targetPosition - (Vector2)firePoint.position).normalized;
                foreach (Vector2 fixedDir in fixedAimDirections) {
                    // Asumimos que fixedAimDirections son en espacio del mundo para el de ventana,
                    // o relativas al "forward" del arma si el arma puede rotar independientemente del cuerpo.
                    // Si weaponPivotToRotate existe, podríamos transformar fixedDir a world space usando su rotación.
                    // Por ahora, para el de ventana, son globales. Para uno móvil, serían relativas a su facing.
                    Vector2 actualFixedDir = fixedDir;
                    if (weaponPivotToRotate != null) { // Si hay un pivote, las direcciones podrían ser relativas a él
                        // Esto se complica si el pivote mismo rota. Mejor definir fixedAimDirections en world space
                        // o en un espacio local consistente (ej. siempre relativas a Vector2.down del enemigo).
                    } else if (!_aiController.IsFacingRight && aimingStyle != AimingStyleOptions.UseFixedDirections) { 
                        // Para un enemigo móvil que flipea y NO usa FixedDirections explícitamente para apuntar
                        // (sino para un ataque especial por ej.), y si fixedAimDirections fueran locales:
                        // actualFixedDir.x *= -1; 
                        // PERO para el de ventana, esto no aplica.
                    }

                    float angleDiff = Vector2.Angle(actualFixedDir.normalized, dirToTarget);
                    if (angleDiff < smallestAngleDiff) { smallestAngleDiff = angleDiff; bestDir = actualFixedDir; }
                }
            }
            return bestDir.sqrMagnitude > 0.001f ? bestDir.normalized : (_aiController.IsFacingRight ? Vector2.right : Vector2.left);
        }

        public bool IsFiring() => _isCurrentlyInAttackSequence;

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Intentar obtener aiController si es null y estamos en editor (no en play mode)
            // para que los gizmos que dependen de IsFacingRight funcionen al seleccionar.
            EnemyAIController controllerForGizmo = _aiController;
            if (!Application.isPlaying && controllerForGizmo == null)
            {
                controllerForGizmo = GetComponentInParent<EnemyAIController>();
            }

            // Rango de Ataque general
            if (firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(firePoint.position, attackRange);
            } else {
                // Si no hay firePoint, dibujar desde el transform de este GO como fallback
                Gizmos.color = new Color(0, 0.5f, 0.5f); // Cyan oscuro
                Gizmos.DrawWireSphere(transform.position, attackRange);
            }


            // Origen del Gizmo para las direcciones
            // Usar firePoint si está asignado, sino el transform de este GO.
            // weaponPivotToRotate es más para la rotación en sí, el origen del disparo es firePoint.
            Vector3 gizmoOrigin = firePoint != null ? firePoint.position : transform.position;
            float gizmoLineLength = Mathf.Min(attackRange * 0.5f, 2f); // Longitud de las líneas de dirección

            // Dibujar direcciones específicas según el modo
            // Solo dibujar si tenemos el AIController (para IsFacingRight en HorizontalOnly)
            if (controllerForGizmo != null) 
            {
                switch (aimingStyle)
                {
                    case AimingStyleOptions.HorizontalOnly:
                        Gizmos.color = Color.green;
                        // Usar transform.right del GO del AIController (Root) para la dirección base
                        Vector2 horizontalDir = controllerForGizmo.IsFacingRight ? controllerForGizmo.transform.right : -controllerForGizmo.transform.right;
                        Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)horizontalDir.normalized * gizmoLineLength);
                        break;

                    case AimingStyleOptions.UseFixedDirections:
                        Gizmos.color = Color.magenta;
                        if (fixedAimDirections != null && fixedAimDirections.Length > 0)
                        {
                            foreach (Vector2 dir in fixedAimDirections)
                            {
                                if (dir.sqrMagnitude > 0.001f)
                                {
                                    // Asumimos que fixedAimDirections son en espacio del MUNDO
                                    // o relativas a una orientación base que no cambia con el flip del sprite.
                                    // Si weaponPivotToRotate se usara para orientar a estas direcciones,
                                    // el gizmo debería reflejar eso, pero es complejo para OnDrawGizmos.
                                    Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)dir.normalized * gizmoLineLength);
                                }
                            }
                        }
                        else // Fallback si no hay direcciones fijas definidas, dibujar la horizontal
                        {
                            Gizmos.color = new Color(0.5f,0,0.5f); 
                            Vector2 fallbackHorizontalDir = controllerForGizmo.IsFacingRight ? controllerForGizmo.transform.right : -controllerForGizmo.transform.right;
                            Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)fallbackHorizontalDir.normalized * gizmoLineLength * 0.75f); // Un poco más corta
                        }
                        break;
                    
                    case AimingStyleOptions.AimFreelyAtTarget:
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(gizmoOrigin, gizmoLineLength * 0.25f); // Solo un pequeño círculo para indicar que apunta libre
                        if (!Application.isPlaying && controllerForGizmo.playerTarget != null)
                        {
                             Gizmos.DrawLine(gizmoOrigin, controllerForGizmo.playerTarget.position);
                        }
                        break;
                }
            }
            else if (aimingStyle == AimingStyleOptions.UseFixedDirections) // Si no hay AIController pero tenemos fixed directions
            {
                Gizmos.color = Color.gray; // Indicar que no se puede determinar la orientación base
                 if (fixedAimDirections != null && fixedAimDirections.Length > 0)
                 {
                    foreach (Vector2 dir in fixedAimDirections)
                    {
                        if (dir.sqrMagnitude > 0.001f)
                            Gizmos.DrawLine(gizmoOrigin, gizmoOrigin + (Vector3)dir.normalized * gizmoLineLength);
                    }
                 }
            }
        }
        #endif
    }
}