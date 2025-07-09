using Scripts.Enemies.Boss.Attacks;
using Scripts.Enemies.Boss.Attacks.Smash;
using UnityEngine;

namespace Scripts.Enemies.Boss.Core.Visuals
{
    /// <summary>
/// A "linker" component that sits on the same GameObject as the Animator.
/// Its sole purpose is to provide simple, public methods that Animation Events can call.
/// It then relays these events to the appropriate logic components (attacks, audio, etc.).
/// This decouples the Animator from the game's core logic.
/// </summary>
public class BossAnimationEventRelay : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Reference to the main BossController.")]
    [SerializeField] private BossController bossController;
    [Tooltip("Reference to the script that handles the Ground Smash attack.")]
    [SerializeField] private BossAttack_GroundSmash groundSmashAttack;
    [Tooltip("Reference to the script that handles the Melee Swipe attack.")]
    [SerializeField] private BossAttack_MeleeSwipe meleeSwipeAttack;
    // The Rush attack activates its own hitbox, so we don't need a reference for it here.
    
    [Tooltip("Reference to the script that handles boss-specific audio.")]
    [SerializeField] private BossAudioFeedback bossAudio;

    // We can add references to other systems like VFX controllers here later.

    // --- MELEE SWIPE ATTACK EVENTS ---

    /// <summary>
    /// Called by an Animation Event during the swipe animation to activate the hitbox.
    /// </summary>
    public void Animation_ActivateMeleeHitbox()
    {
        // Relay the command to the specific attack script.
        meleeSwipeAttack?.Animation_ActivateHitbox();
    }

    /// <summary>
    /// Called by an Animation Event after the swipe to deactivate the hitbox.
    /// </summary>
    public void Animation_DeactivateMeleeHitbox()
    {
        meleeSwipeAttack?.Animation_DeactivateHitbox();
    }

    // --- GROUND SMASH ATTACK EVENTS ---

    /// <summary>
    /// Called by an Animation Event at the moment of impact during the ground smash.
    /// </summary>
    public void Animation_TriggerGroundSmashEffect()
    {
        // Relay the command to the Ground Smash attack script.
        groundSmashAttack?.PerformSmashEffect();
    }
    
    // --- GENERIC AUDIO EVENTS ---
    // These can be called from any animation clip (Roar, Walk, etc.)

    /// <summary>
    /// Plays a footstep sound. Can be placed on the frames where a foot hits the ground.
    /// </summary>
    public void Animation_PlayFootstepSound()
    {
        // We would add a PlayFootstep() method to BossAudioFeedback for this to work.
        // bossAudio?.PlayFootstep();
        Debug.Log("Animation Event: PlayFootstepSound");
    }

    /// <summary>
    /// Plays a generic "whoosh" or "swing" sound.
    /// </summary>
    public void Animation_PlaySwingSound()
    {
        // bossAudio?.PlaySwing();
        Debug.Log("Animation Event: PlaySwingSound");
    }

    // --- STATE MACHINE EVENTS ---

    /// <summary>
    /// Can be placed at the end of an animation (like the Intro Roar) to signal
    /// to the BossController that the sequence is complete.
    /// </summary>
    public void Animation_NotifyControllerEvent(string eventName)
    {
        // The BossController could have a method to handle these generic events.
        // bossController?.HandleAnimationEvent(eventName);
        Debug.Log($"Animation Event Fired: {eventName}");
    }
}
}