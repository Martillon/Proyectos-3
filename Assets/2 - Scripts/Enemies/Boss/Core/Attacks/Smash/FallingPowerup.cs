using System.Collections;
using Scripts.Core;
using Scripts.Core.Interfaces;
using UnityEngine;

namespace Scripts.Enemies.Boss.Attacks.Smash
{
    /// <summary>
/// A component that is dynamically added to a GameObject to make it a temporary,
/// helpful falling item. It handles its fall, sits on the ground for a duration,
/// and can be collected by the player.
/// </summary>
public class FallingPowerup : MonoBehaviour
{
    // We can use an enum to make the power-up type selectable and clear.
    public enum PowerupType { HealArmor, HealLife }

    [Header("Power-up Settings")]
    [Tooltip("What kind of benefit this power-up provides.")]
    [SerializeField] private PowerupType type = PowerupType.HealArmor;
    [Tooltip("The amount of healing or benefit this power-up provides.")]
    [SerializeField] private int amount = 1;
    [Tooltip("How long the power-up will stay on the ground before despawning.")]
    [SerializeField] private float groundLifetime = 10.0f;

    [Header("Feedback")]
    [Tooltip("(Optional) A prefab for a particle effect to spawn when collected.")]
    [SerializeField] private GameObject collectionVFX;
    
    // --- Private State ---
    private Rigidbody2D _rb;
    private Collider2D _mainCollider;
    private bool _hasBeenDropped = false;
    private bool _isLanded = false;

    /// <summary>
    /// Get component references.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// The activation command, called by the FallingObjectManager.
    /// </summary>
    public void Drop(float fallSpeed, float lifetime)
    {
        if (_hasBeenDropped) return;
        _hasBeenDropped = true;

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.down * fallSpeed;
        }

        // We can keep this on a default layer while it falls, as it's not dangerous.
        // Or create a "Powerup" layer if you need specific interactions.
        
        // This lifetime is for if it falls off-screen without ever landing.
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Handles collision with the ground.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // We only care about the first time it hits something solid.
        if (_isLanded) return;
        
        // Check if the object we hit is on the "Ground" layer.
        if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Ground")) != 0)
        {
            _isLanded = true;
            LandOnGround();
        }
    }
    
    /// <summary>
    /// Handles collision with the player (for pickup).
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // The player can pick it up either while it's falling or after it has landed.
        if (other.CompareTag(GameConstants.PlayerTag))
        {
            Collect(other.gameObject);
        }
    }

    /// <summary>
    /// Called when the power-up hits the ground. It stops moving and waits to be collected.
    /// </summary>
    private void LandOnGround()
    {
        // Stop all physical movement by making the Rigidbody Kinematic.
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        // Change the main collider to a trigger so the player can walk "through" it
        // to collect it, rather than bumping into it.
        if (_mainCollider != null)
        {
            _mainCollider.isTrigger = true;
        }
        
        // Start the despawn timer. If the player doesn't collect it in time, it disappears.
        StartCoroutine(DespawnTimer());
    }

    /// <summary>
    /// A simple timer that destroys the object after it has been on the ground for a while.
    /// </summary>
    private IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(groundLifetime);
        
        // Optional: Add a fade-out animation here before destroying.
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the player touches the power-up. Applies the benefit and cleans up.
    /// </summary>
    private void Collect(GameObject player)
    {
        // Use a switch statement to apply the correct benefit based on the 'type' enum.
        switch (type)
        {
            case PowerupType.HealArmor:
                if (player.TryGetComponent<IHealArmor>(out var armorHealable))
                {
                    armorHealable.HealArmor(amount);
                }
                break;
            case PowerupType.HealLife:
                if (player.TryGetComponent<IHealLife>(out var lifeHealable))
                {
                    lifeHealable.HealLife(amount);
                }
                break;
        }
        
        // Play feedback.
        if (collectionVFX != null)
        {
            Instantiate(collectionVFX, transform.position, Quaternion.identity);
        }

        // Immediately destroy the power-up object after it's been collected.
        Destroy(gameObject);
    }
}
}