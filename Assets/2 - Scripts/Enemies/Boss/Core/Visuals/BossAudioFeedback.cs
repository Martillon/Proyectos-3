using Scripts.Core.Audio;
using UnityEngine;

namespace Scripts.Enemies.Boss.Core.Visuals
{
    /// <summary>
    /// A centralized component for playing all boss-specific sound effects.
    /// It acts as a "soundboard" that receives simple commands from other boss scripts.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BossAudioFeedback : MonoBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("The AudioSource used to play all boss sounds. If null, one will be added automatically.")]
        [SerializeField] private AudioSource audioSource;

        [Header("Boss Sound Definitions")]
        [Tooltip("The sound played when the boss performs its main roar (intro/phase transition).")]
        [SerializeField] private Sounds roarSound;
        [Tooltip("The sound of the ground impact from the smash attack.")]
        [SerializeField] private Sounds groundSmashImpactSound;
        [Tooltip("The sound of the boss rushing/charging.")]
        [SerializeField] private Sounds rushSound;
        [Tooltip("The sound played when the boss is in its dizzy/stunned state (can be a looping sound).")]
        [SerializeField] private Sounds stunLoopSound;
        [Tooltip("A generic footstep sound, often called by animation events.")]
        [SerializeField] private Sounds footstepSound;
        [Tooltip("The sound of the boss's final defeat.")]
        [SerializeField] private Sounds deathSound;
        [Tooltip("A generic sound for a melee weapon swing or 'whoosh'.")]
        [SerializeField] private Sounds swingSound;

        /// <summary>
        /// Get the AudioSource component on Awake.
        /// </summary>
        private void Awake()
        {
            // Ensure we have an AudioSource to work with.
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            // It's good practice to configure the AudioSource for 2D sounds.
            audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
        }
        
        // --- PUBLIC METHODS (The "Buttons" on our Soundboard) ---
        // These methods can be called from the BossController, attack scripts,
        // or from Animation Events via the BossAnimationEventRelay.

        public void PlayRoar() => roarSound?.Play(audioSource);
        public void PlayGroundSmashImpact() => groundSmashImpactSound?.Play(audioSource);
        public void PlayRush() => rushSound?.Play(audioSource);
        public void PlayFootstep() => footstepSound?.Play(audioSource);
        public void PlayDeath() => deathSound?.Play(audioSource);
        public void PlaySwing() => swingSound?.Play(audioSource);

        /// <summary>
        /// Plays the stun sound, which is likely configured to loop.
        /// </summary>
        public void PlayStunLoop()
        {
            // We check if it's already playing the stun sound to avoid restarting it every frame.
            if (audioSource.clip == stunLoopSound.clip && audioSource.isPlaying) return;
            
            stunLoopSound?.Play(audioSource);
        }
        
        /// <summary>
        /// Stops any currently playing sound. Useful for ending looping sounds like the stun.
        /// </summary>
        public void StopAllSounds()
        {
            audioSource?.Stop();
        }
    }
}