using UnityEngine;
using Scripts.Enemies.Ranged; // We need this for the AimingStyle enum

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "My Game/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "New Enemy";

    // --- NEW BEHAVIOR SECTION ---
    [Header("Behavior & Type")]
    [Tooltip("Is this enemy static (like a turret or window) or can it move around?")]
    public bool isStatic = false;
    [Tooltip("Does this enemy use melee or ranged attacks? This determines which attack component is used.")]
    public bool isRanged = false;
    [Tooltip("For mobile enemies, can they patrol when idle?")]
    public bool canPatrol = true;

    [Header("Health & Defense")]
    public float maxHealth = 100f;

    [Header("AI & Movement")]
    public float moveSpeed = 3f;
    public float detectionRange = 12f;
    public float engagementRange = 2f;

    [Header("Attack Properties")]
    public int attackDamage = 15;
    public float attackCooldown = 1.5f;
    [Tooltip("For ranged enemies, how does it aim?")]
    public AimingStyle aimingStyle = AimingStyle.Horizontal;
    
    // You could even add more complex attack data here
    // public int projectilesPerBurst = 1;
    // public float spreadAngle = 0;
}