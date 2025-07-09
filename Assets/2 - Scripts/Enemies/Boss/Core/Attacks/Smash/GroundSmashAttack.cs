using System.Collections;
using System.Collections.Generic;
using Scripts.Enemies.Boss.Core;
using Scripts.Enemies.Boss.Core.Visuals;
using UnityEngine;

namespace Scripts.Enemies.Boss.Attacks.Smash
{
    /// <summary>
    /// A modular boss attack that performs a ground smash, triggering a volley of
    /// falling objects from the ceiling. It can be upgraded for later phases.
    /// </summary>
    public class BossAttack_GroundSmash : MonoBehaviour, IBossAttack
    {
        [Header("Phase 1 Settings")]
        [Tooltip("How many hazards to drop in Phase 1.")]
        [SerializeField] private int phase1_hazardCount = 3;
        [Tooltip("How many power-ups to drop in Phase 1.")]
        [SerializeField] private int phase1_powerupCount = 1;
        [Tooltip("The speed of the smash animation in Phase 1.")]
        [SerializeField] private float phase1_animationSpeed = 1.0f;

        [Header("Attack Configuration")]
        [Tooltip("The total duration of the base ground smash animation.")]
        [SerializeField] private float baseAnimationDuration = 2.5f;
        [Tooltip("A list of harmless decoration prefabs that can be dropped as hazards.")]
        [SerializeField] private List<GameObject> hazardPrefabs;
        [Tooltip("A list of power-up prefabs that can be dropped.")]
        [SerializeField] private List<GameObject> powerupPrefabs;
        [Tooltip("A list of all possible ceiling spawn points for hazards.")]
        [SerializeField] private List<Transform> hazardSpawnPoints;
        [Tooltip("A list of all possible ceiling spawn points for power-ups.")]
        [SerializeField] private List<Transform> powerupSpawnPoints;
        [Tooltip("How fast the falling objects will travel downwards.")]
        [SerializeField] private float objectFallSpeed = 15f;
        [Tooltip("Max lifetime of a falling object if it never hits anything.")]
        [SerializeField] private float objectLifetime = 5f;

        // --- Private References & State ---
        private BossController _bossController;
        private BossVisualController _visualController;
        private BossAnimationEventRelay _animationRelay; // We will create this next.

        // Current values that can be upgraded by the BossController.
        private int _currentHazardCount;
        private int _currentPowerupCount;
        private float _currentAnimationSpeed;

        /// <summary>
        /// Called by the BossController to provide necessary references.
        /// </summary>
        public void Initialize(BossController controller)
        {
            _bossController = controller;
            // Get references from the main boss object.
            _visualController = _bossController.GetComponentInChildren<BossVisualController>();
            _animationRelay = _bossController.GetComponentInChildren<BossAnimationEventRelay>();
            
            // Set the initial attack parameters to Phase 1 values.
            UpgradeAttack(1);
        }

        /// <summary>
        /// This is the main coroutine called by the BossController to execute the attack.
        /// </summary>
        public IEnumerator Execute()
        {
            Debug.Log("Executing Ground Smash Attack...");

            // Ensure the boss is facing the player before attacking.
            _bossController.FacePlayer();
            
            // Set the animator speed for this attack. Faster in later phases.
            // _visualController.SetAnimatorSpeed(_currentAnimationSpeed); // We will add this method later.
            
            // Tell the visual controller to play the animation.
            _visualController.PlayGroundSmashAnimation();

            // Wait for the duration of the animation, adjusted for speed.
            // The actual spawning is triggered by an Animation Event during this time.
            yield return new WaitForSeconds(baseAnimationDuration / _currentAnimationSpeed);
            
            // Reset animator speed after the attack.
            // _visualController.SetAnimatorSpeed(1.0f);

            Debug.Log("Ground Smash Attack Finished.");
        }

        /// <summary>
        /// This method is called by the BossAnimationEventRelay at the exact moment of impact
        /// during the smash animation.
        /// </summary>
        public void PerformSmashEffect()
        {
            Debug.Log($"Smash Impact! Spawning {_currentHazardCount} hazards and {_currentPowerupCount} power-ups.");
            
            // Trigger a camera shake. (We would need a CameraShaker singleton for this).
            // CameraShaker.Instance?.Shake(0.3f, 5f);
            
            // Tell the FallingObjectManager to start spawning objects.
            if (FallingObjectManager.Instance != null)
            {
                // Spawn the hazards.
                if (_currentHazardCount > 0 && hazardPrefabs.Count > 0)
                {
                    FallingObjectManager.Instance.SpawnHazardVolley(hazardPrefabs, hazardSpawnPoints, _currentHazardCount, objectFallSpeed, objectLifetime);
                }
                // Spawn the power-ups.
                if (_currentPowerupCount > 0 && powerupPrefabs.Count > 0)
                {
                    FallingObjectManager.Instance.SpawnPowerupVolley(powerupPrefabs, powerupSpawnPoints, _currentPowerupCount, objectFallSpeed, objectLifetime);
                }
            }
        }

        /// <summary>
        /// A public method for the BossController to call during phase transitions to make this attack stronger.
        /// </summary>
        /// <param name="phase">The new phase number (e.g., 2 or 3).</param>
        public void UpgradeAttack(int phase)
        {
            // We can use a switch statement to define the properties for each phase.
            switch (phase)
            {
                case 2:
                    _currentHazardCount = 5;
                    _currentPowerupCount = 1;
                    _currentAnimationSpeed = 1.2f;
                    break;
                case 3:
                    _currentHazardCount = 8;
                    _currentPowerupCount = 0; // Maybe no power-ups in the final, desperate phase!
                    _currentAnimationSpeed = 1.5f;
                    break;
                default: // Phase 1 and any other case
                    _currentHazardCount = phase1_hazardCount;
                    _currentPowerupCount = phase1_powerupCount;
                    _currentAnimationSpeed = phase1_animationSpeed;
                    break;
            }
        }
    }
}