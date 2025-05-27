// En Scripts/Enemies/Core/EnemyHealth.cs
using UnityEngine;
using Scripts.Core.Interfaces; // Para IDamageable e IInstakillable
using System.Collections;
using Scripts.Enemies.Melee;
using Scripts.Enemies.Movement;
using Scripts.Enemies.Ranged;

namespace Scripts.Enemies.Core 
{
    // Asegúrate que la referencia al Animator para vulnerabilidad sea correcta
    // Si este script está en el Root, y el Animator en Visuals (hijo), GetComponentInChildren está bien.
    public class EnemyHealth : MonoBehaviour, IDamageable, IInstakillable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Si es true, el GameObject se destruye al morir. Si es false, se desactiva (útil para pooling o enemigos que dejan un 'cadáver').")]
        [SerializeField] private bool destroyOnDeath = true;
        [Tooltip("Específico para enemigos como el de ventana: si es true, permanece visible (animación de derrota) en lugar de destruirse/desactivarse completamente.")]
        [SerializeField] private bool remainVisibleAsDefeated = false;


        [Header("Damage Feedback")]
        [SerializeField] private float damageInvulnerabilityDuration = 0.5f;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private int damageFlashCount = 3;

        [Header("Vulnerability (Optional - e.g., for Window Enemy)")]
        [Tooltip("Si es true, el enemigo solo puede recibir daño cuando su Animator está en uno de los estados especificados.")]
        [SerializeField] private bool vulnerableOnlyInSpecificAnimStates = false;
        [Tooltip("Nombres de los estados del Animator en los que este enemigo es vulnerable.")]
        [SerializeField] private string[] vulnerableAnimStateNames = { "Open - Aim", "Attack" }; 
        [Tooltip("Referencia al Animator usado para chequear los estados de vulnerabilidad. Si es null, intentará encontrar uno en los hijos.")]
        [SerializeField] private Animator animatorForVulnerabilityCheck;


        private float currentHealth;
        private bool isDead = false;
        private bool isInvulnerableFromDamage = false;

        private SpriteRenderer _spriteRenderer; // Referencia al sprite principal para el flash
        private Color _originalSpriteColor;
        private Coroutine _damageFeedbackCoroutine;

        // Referencias a componentes de ataque para deshabilitarlos durante hit stun
        private EnemyAttackMelee _meleeAttacker;
        private EnemyAttackRanged _rangedAttacker;
        private EnemyAIController _aiController; // Para deshabilitar IA en Die

        private void Awake()
        {
            currentHealth = maxHealth;

            // Intentar obtener el SpriteRenderer principal (usualmente en Visuals)
            // Si este script está en el Root, y Visuals es un hijo con el sprite:
            Transform visualsTransform = transform.Find("Visuals"); // Asumiendo un hijo llamado "Visuals"
            if (visualsTransform != null)
            {
                _spriteRenderer = visualsTransform.GetComponentInChildren<SpriteRenderer>(true);
            }
            if (_spriteRenderer == null) // Fallback
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (_spriteRenderer != null) _originalSpriteColor = _spriteRenderer.color;
            else Debug.LogWarning($"EnemyHealth on {gameObject.name}: SpriteRenderer not found for damage flash.", this);

            _meleeAttacker = GetComponent<EnemyAttackMelee>() ?? GetComponentInChildren<EnemyAttackMelee>(true);
            _rangedAttacker = GetComponent<EnemyAttackRanged>() ?? GetComponentInChildren<EnemyAttackRanged>(true);
            _aiController = GetComponent<EnemyAIController>();


            if (vulnerableOnlyInSpecificAnimStates && animatorForVulnerabilityCheck == null)
            {
                // Si es vulnerable condicionalmente y no se asignó Animator, intentar encontrarlo
                if (visualsTransform != null) animatorForVulnerabilityCheck = visualsTransform.GetComponentInChildren<Animator>(true);
                if (animatorForVulnerabilityCheck == null) animatorForVulnerabilityCheck = GetComponentInChildren<Animator>(true);

                if (animatorForVulnerabilityCheck == null)
                {
                    Debug.LogError($"EnemyHealth on {gameObject.name}: 'vulnerableOnlyInSpecificAnimStates' es true, pero 'animatorForVulnerabilityCheck' no está asignado y no se pudo encontrar. Se deshabilitará la vulnerabilidad condicional.", this);
                    vulnerableOnlyInSpecificAnimStates = false;
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDead || isInvulnerableFromDamage) return;

            if (vulnerableOnlyInSpecificAnimStates && animatorForVulnerabilityCheck != null)
            {
                AnimatorStateInfo stateInfo = animatorForVulnerabilityCheck.GetCurrentAnimatorStateInfo(0);
                bool isInVulnerableState = false;
                string currentAnimStateNameForDebug = GetCurrentAnimatorStateName(animatorForVulnerabilityCheck); // Usar tu helper

                Debug.Log($"[{Time.frameCount}] EnemyHealth: Checking vulnerability. Current Animator State: '{currentAnimStateNameForDebug}' (Hash: {stateInfo.fullPathHash}, ShortNameHash: {stateInfo.shortNameHash})");

                foreach (string vulnerableStateName in vulnerableAnimStateNames)
                {
                    // Debug.Log($"Comparing with vulnerable state: '{vulnerableStateName}'"); // Mucho log, usar con cuidado
                    if (stateInfo.IsName(vulnerableStateName)) // IsName compara con el nombre completo del estado (ej. Base Layer.Open - Aim)
                    {
                        isInVulnerableState = true;
                        Debug.Log($"[{Time.frameCount}] EnemyHealth: MATCH! Enemy IS in vulnerable state: '{vulnerableStateName}'");
                        break;
                    }
                }
                if (!isInVulnerableState)
                {
                    string vulnerableStatesList = string.Join(", ", vulnerableAnimStateNames);
                    Debug.Log($"EnemyHealth on {gameObject.name}: Damage IGNORED. Not in a vulnerable animation state. Current: '{currentAnimStateNameForDebug}'. Vulnerable states are: [{vulnerableStatesList}]");
                    return; 
                }
            }

            //Debug.Log($"EnemyHealth on {gameObject.name}: Taking {amount} damage. Current health: {currentHealth}");
            currentHealth -= amount;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
            else
            {
                if (_damageFeedbackCoroutine != null) StopCoroutine(_damageFeedbackCoroutine);
                _damageFeedbackCoroutine = StartCoroutine(DamageFeedbackSequence());
            }
        }

        private IEnumerator DamageFeedbackSequence()
        {
            isInvulnerableFromDamage = true;
            // Guardar estado original de los componentes de ataque
            bool meleeWasEnabled = _meleeAttacker != null && _meleeAttacker.enabled;
            bool rangedWasEnabled = _rangedAttacker != null && _rangedAttacker.enabled;

            if (meleeWasEnabled) _meleeAttacker.enabled = false;
            if (rangedWasEnabled) _rangedAttacker.enabled = false;

            if (_spriteRenderer != null && damageFlashCount > 0 && damageInvulnerabilityDuration > 0)
            {
                float flashPartDuration = damageInvulnerabilityDuration / (damageFlashCount * 2f);
                for (int i = 0; i < damageFlashCount; i++)
                {
                    _spriteRenderer.color = damageFlashColor;
                    yield return new WaitForSeconds(flashPartDuration);
                    _spriteRenderer.color = _originalSpriteColor;
                    yield return new WaitForSeconds(flashPartDuration);
                }
                _spriteRenderer.color = _originalSpriteColor; // Asegurar color original al final
            }
            else // Si no hay flash, solo esperar la duración de invulnerabilidad
            {
                yield return new WaitForSeconds(damageInvulnerabilityDuration);
            }

            // Restaurar estado de los componentes de ataque
            if (meleeWasEnabled && _meleeAttacker != null) _meleeAttacker.enabled = true;
            if (rangedWasEnabled && _rangedAttacker != null) _rangedAttacker.enabled = true;
            
            isInvulnerableFromDamage = false;
            _damageFeedbackCoroutine = null;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true; 
            Debug.Log($"EnemyHealth on '{gameObject.name}': DIE sequence initiated.");

            if (_damageFeedbackCoroutine != null) { StopCoroutine(_damageFeedbackCoroutine); }
            if (_spriteRenderer != null) { _spriteRenderer.color = _originalSpriteColor; }

            // Deshabilitar IA y componentes de ataque
            if (_aiController != null) _aiController.enabled = false;
            if (_meleeAttacker != null) _meleeAttacker.enabled = false;
            if (_rangedAttacker != null) _rangedAttacker.enabled = false;
            
            // Deshabilitar movimiento y física de forma segura
            EnemyMovementComponent movComp = GetComponentInChildren<EnemyMovementComponent>(true); // Buscar en hijos por si está allí
            if (movComp != null) movComp.enabled = false; // Deshabilitar el componente que controla el RB

            Collider2D mainCollider = GetComponent<Collider2D>();
            if (mainCollider != null) mainCollider.enabled = false; 
            
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) 
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic; // Hacerlo Kinematic para que no caiga ni sea afectado por física, pero permita que la animación de muerte lo mueva si es necesario. Static lo congela completamente.
            }
            
            // Disparar animación de muerte (Animator debería estar en Visuals)
            Animator animatorToUse = animatorForVulnerabilityCheck != null ? animatorForVulnerabilityCheck : GetComponentInChildren<Animator>(true);
            if (animatorToUse != null) {
                animatorToUse.SetTrigger("Die"); // Asumiendo un trigger "Die"
            }


            // Decidir qué hacer con el GameObject
            if (remainVisibleAsDefeated)
            {
                Debug.Log($"EnemyHealth on '{gameObject.name}': Marked as defeated, will remain visible.");
                // El objeto permanece, pero está lógicamente muerto y sus componentes principales desactivados.
                // Podrías desactivar este script de EnemyHealth también para evitar más interacciones:
                // this.enabled = false; 
            }
            else if (destroyOnDeath)
            {
                Destroy(gameObject, 2f); // Dar tiempo para la animación de muerte
            }
            else // Desactivar para pooling
            {
                gameObject.SetActive(false);
            }
        }

        public void ApplyInstakill() // Implementación de IInstakillable
        {
            Debug.Log($"EnemyHealth on '{gameObject.name}': ApplyInstakill called.");
            if (isDead) return; // Ya está muerto/muriendo

            currentHealth = 0; // Asegurar que la salud sea cero
            Die(); // Llamar a la secuencia de muerte normal
        }
        
        public bool IsDeadPublic => isDead; // Propiedad pública si otros necesitan leerlo fácilmente

        // Helper para debug
        private string GetCurrentAnimatorStateName(Animator anim) {
            if (anim == null || !anim.gameObject.activeInHierarchy || anim.runtimeAnimatorController == null || anim.IsInTransition(0)) return "Unknown/Transitioning";
            try {
                if (anim.GetCurrentAnimatorClipInfoCount(0) > 0 && anim.GetCurrentAnimatorClipInfo(0).Length > 0) {
                    return anim.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                } return anim.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString(); // Fallback a hash si no hay clip info
            } catch { return "ErrorGettingStateName"; }
        }
    }
}