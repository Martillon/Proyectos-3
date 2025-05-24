using UnityEngine;
using System.Collections;
using Scripts.Enemies.Core; // Para EnemyAIController

namespace Scripts.Enemies.Ranged
{
    [RequireComponent(typeof(EnemyAIController))]
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
        [Tooltip("Duration (in seconds) the enemy might pause movement to fire. Set to 0 if enemy can fire while moving.")]
        [SerializeField] private float fireAnimationDuration = 0.3f; // Para SetCanMove

        [Header("Line of Sight (For AimAtTargetFreely Mode)")]
        [Tooltip("If true AND AimAtTargetFreely is true, enemy will only fire if there's a clear line of sight to the player.")]
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Layers that can block line of sight (e.g., 'Ground', 'Obstacles'). Player layer should NOT be in this mask.")]
        [SerializeField] private LayerMask lineOfSightBlockers;

        [Header("Aiming Style & Configuration")]
        [Tooltip("If true, the weapon/firepoint attempts to aim directly at the target. If false, fires horizontally based on facing direction.")]
        [SerializeField] private bool aimAtTargetFreely = false;
        [Tooltip("Optional: Transform of the weapon pivot that should be rotated if AimAtTargetFreely is true. If null, FirePoint itself might be rotated (less ideal if FirePoint is child of flippable Visuals).")]
        [SerializeField] private Transform weaponPivotToRotate; // Asignar el Weapon_Pivot aquí

        // Para el Enemigo de Ventana (AimingMode.FixedDirections)
        public enum AimingMode { TowardsTarget, FixedDirections, HorizontalOnly } // HorizontalOnly es el nuevo por defecto implícito si aimAtTargetFreely=false
        [Tooltip("Defines how the enemy aims if AimAtTargetFreely is true.")]
        [SerializeField] private AimingMode aimingModeWhenFree = AimingMode.TowardsTarget; // Usado si aimAtTargetFreely = true
        [Tooltip("Fixed directions to shoot in if AimingMode is FixedDirections (and AimAtTargetFreely is true). Relative to world or weapon pivot's forward.")]
        [SerializeField] private Vector2[] fixedAimDirections = { Vector2.down, new Vector2(-1, -1).normalized, new Vector2(1, -1).normalized };
        [Tooltip("Number of projectiles per burst. Set to 1 for single shot.")]
        [SerializeField] private int projectilesPerBurst = 1;
        [Tooltip("Delay between shots in a burst (if projectilesPerBurst > 1).")]
        [SerializeField] private float delayBetweenBurstShots = 0.1f;


        private float lastAttackTimestamp;
        private EnemyAIController aiController;
        private bool isCurrentlyFiring = false;
        // private Animator enemyAnimator; // Si necesitas controlar animaciones de disparo desde aquí

        private void Awake()
        {
            aiController = GetComponent<EnemyAIController>();
            // enemyAnimator = GetComponentInChildren<Animator>(); // O donde esté

            if (aiController == null) Debug.LogError($"...", this);
            if (projectilePrefab == null) Debug.LogError($"...", this);
            if (projectilePrefab != null && projectilePrefab.GetComponent<EnemyProjectile>() == null) Debug.LogError($"...", this);
            if (firePoint == null) Debug.LogError($"...", this);
            if (aimAtTargetFreely && aimingModeWhenFree == AimingMode.FixedDirections && (fixedAimDirections == null || fixedAimDirections.Length == 0))
                Debug.LogWarning($"EnemyAttackRanged on {gameObject.name}: AimingMode is FixedDirections but no fixedAimDirections are set.", this);
        }

        public bool CanInitiateAttack(Transform target)
        {
            if (target == null || isCurrentlyFiring || Time.time < lastAttackTimestamp + attackCooldown || firePoint == null)
            {
                return false;
            }

            float distanceToTarget = Vector2.Distance(firePoint.position, target.position);
            if (distanceToTarget > attackRange)
            {
                return false;
            }

            // Line of Sight check:
            // - Si aimAtTargetFreely es true Y aimingModeWhenFree es TowardsTarget Y requireLineOfSight es true.
            // - Para HorizontalOnly, LoS es menos directo, el jugador podría estar detrás de una cobertura delgada pero aún en la línea horizontal.
            // - Para FixedDirections, LoS podría o no ser requerido dependiendo del diseño.
            if (aimAtTargetFreely && aimingModeWhenFree == AimingMode.TowardsTarget && requireLineOfSight)
            {
                if (!HasLineOfSight(target)) return false;
            }
            // Para HorizontalOnly o FixedDirections, asumimos que si está en rango, puede intentar.
            // El AIController podría tener su propia lógica de LoS más general si es necesario antes de llamar a TryAttack.

            return true;
        }

        public void TryAttack(Transform target)
        {
            // CanInitiateAttack ya fue llamado por AIController, así que aquí solo verificamos estado interno y target.
            if (target == null || isCurrentlyFiring || firePoint == null) // Cooldown y rango ya chequeados por CanInitiateAttack
            {
                return;
            }
            StartCoroutine(PerformRangedAttackSequence(target));
        }

        private bool HasLineOfSight(Transform target) // Usado principalmente por AimingMode.TowardsTarget
        {
            if (target == null || firePoint == null) return false;
            Vector2 directionToTarget = (target.position - firePoint.position).normalized;
            float distanceToTarget = Vector2.Distance(firePoint.position, target.position);
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToTarget, distanceToTarget, lineOfSightBlockers);
            return hit.collider == null;
        }
        
        private Vector2 GetBestFixedDirection(Vector3 targetPosition)
        {
            if (fixedAimDirections == null || fixedAimDirections.Length == 0)
                return (aiController.IsFacingRight ? Vector2.right : Vector2.left); // Fallback

            Vector2 bestDir = fixedAimDirections[0];
            float smallestAngleDiff = float.MaxValue;
            Vector2 directionToTargetActual = (targetPosition - firePoint.position).normalized;

            Transform pivot = weaponPivotToRotate != null ? weaponPivotToRotate : firePoint;

            foreach (Vector2 fixedDirLocal in fixedAimDirections)
            {
                // Asumimos que fixedAimDirections son locales al 'forward' del pivot (si existe) o al 'forward' del mundo (X+)
                // Si el enemigo puede flipear, y el weaponPivot NO es hijo de Visuals, su 'right' es la referencia.
                // Si el weaponPivot ES hijo de Visuals, se complica.
                // Por ahora, asumamos que fixedDirLocal son relativas a la orientación no flipeada del weaponPivot (generalmente Vector2.right).
                // Y que weaponPivotToRotate se orienta (si es necesario) antes de este cálculo o que estas son direcciones globales.
                // Esta parte necesita ser clara según la jerarquía del prefab.
                // Opción simple: fixedAimDirections son en espacio del mundo si el pivot no rota para apuntar, o relativas al facing.
                
                Vector2 worldFixedDir;
                // Si el pivot rota libremente, las direcciones fijas son relativas a su "reposo" o una base.
                // Si el pivot NO rota (como en el de ventana), estas son las direcciones globales.
                // Si el pivot solo flipea con el cuerpo, son relativas al facing del cuerpo.
                if (weaponPivotToRotate != null && !aimAtTargetFreely) // Si el pivot existe pero el apuntado es horizontal
                {
                     worldFixedDir = (aiController.IsFacingRight ? Vector2.right : Vector2.left); // Esto no tiene sentido para fixed directions
                     // Aquí las fixed directions deberían ser relativas a la orientación del enemigo.
                     // Ejemplo: Vector2.down es siempre abajo. new Vector2(1,-1) es siempre diag-der-abajo.
                     // Si el enemigo flipea, (1,-1) se convertiría en (-1,-1) si es local al flip.
                     // Por ahora, asumamos que fixedAimDirections son como se definen, y el sprite del enemigo se orienta si es necesario.
                     worldFixedDir = fixedDirLocal; // Asumir que son direcciones "conceptuales" y el enemigo se orienta
                                                  // O que son relativas al "forward" del arma que puede ser flipeado.
                                                  // Ejemplo: Si el enemigo mira izq, Vector2.right local es Vector2.left global.
                                                  // Para el de ventana, son direcciones globales/fijas.

                } else {
                    worldFixedDir = fixedDirLocal; // Para el de ventana, son globales.
                }


                float angleDiff = Vector2.Angle(worldFixedDir, directionToTargetActual);
                if (angleDiff < smallestAngleDiff)
                {
                    smallestAngleDiff = angleDiff;
                    bestDir = worldFixedDir;
                }
            }
            return bestDir.normalized;
        }


        private IEnumerator PerformRangedAttackSequence(Transform target)
        {
            isCurrentlyFiring = true;
            if (fireAnimationDuration > 0)
            {
                aiController.SetCanMove(false);
            }
            // enemyAnimator?.SetTrigger("RangedAttackTrigger");

            // Wind-up antes de la primera bala de la ráfaga
            if (fireAnimationDuration > 0.1f) // Un pequeño delay si hay animación
            {
                yield return new WaitForSeconds(fireAnimationDuration * 0.5f); // Disparar a mitad de la animación
            }

            for (int i = 0; i < projectilesPerBurst; i++)
            {
                if (target == null || aiController.IsDead) // Comprobación por si el target muere o el enemigo muere durante la ráfaga
                {
                    break; 
                }

                Vector2 finalFireDirection;

                if (aimAtTargetFreely)
                {
                    if (aimingModeWhenFree == AimingMode.TowardsTarget)
                    {
                        finalFireDirection = (target.position - firePoint.position).normalized;
                    }
                    else // FixedDirections
                    {
                        finalFireDirection = GetBestFixedDirection(target.position);
                    }

                    Transform transformToRotate = weaponPivotToRotate != null ? weaponPivotToRotate : firePoint;
                    float angle = Mathf.Atan2(finalFireDirection.y, finalFireDirection.x) * Mathf.Rad2Deg;
                    transformToRotate.rotation = Quaternion.Euler(0, 0, angle);
                }
                else // HorizontalOnly
                {
                    finalFireDirection = aiController.IsFacingRight ? Vector2.right : Vector2.left;
                    // No se rota el pivote ni el firepoint, ya están orientados por el flip de Visuals.
                    // Asegurar que weaponPivotToRotate (si existe) esté en su rotación "horizontal" por defecto.
                    if (weaponPivotToRotate != null)
                    {
                        weaponPivotToRotate.localRotation = Quaternion.identity; // O la rotación local base
                    }
                }

                // Spawnear el proyectil
                if (projectilePrefab != null && firePoint != null)
                {
                    // Usar firePoint.position y firePoint.rotation.
                    // firePoint.rotation será la rotación global correcta porque:
                    // - Si aimAtTargetFreely, su padre (weaponPivotToRotate o el root) ya está rotado.
                    // - Si HorizontalOnly, su padre (Visuals o el root) ya está flipeado/orientado.
                    GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                    EnemyProjectile projectileScript = projGO.GetComponent<EnemyProjectile>();
                    if (projectileScript != null)
                    {
                        projectileScript.Initialize(finalFireDirection);
                    }
                }

                if (projectilesPerBurst > 1 && i < projectilesPerBurst - 1)
                {
                    yield return new WaitForSeconds(delayBetweenBurstShots);
                }
            }

            // Resto de la duración de la animación
            if (fireAnimationDuration > 0.1f)
            {
                yield return new WaitForSeconds(fireAnimationDuration * 0.5f);
            }

            if (fireAnimationDuration > 0) // Solo re-habilitar movimiento si se deshabilitó
            {
                 aiController.SetCanMove(true);
            }
            lastAttackTimestamp = Time.time;
            isCurrentlyFiring = false;
        }

        public bool IsFiring() => isCurrentlyFiring;

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(firePoint.position, attackRange);

                if (aimAtTargetFreely && aimingModeWhenFree == AimingMode.FixedDirections)
                {
                    Gizmos.color = Color.magenta;
                    Transform pivot = weaponPivotToRotate != null ? weaponPivotToRotate : firePoint;
                    foreach (Vector2 dir in fixedAimDirections)
                    {
                        // Esto dibuja las direcciones fijas como si fueran globales o relativas al mundo/pivot base
                        Gizmos.DrawLine(pivot.position, pivot.position + (Vector3)dir.normalized * 2f);
                    }
                }
            }
        }
        #endif
    }
}