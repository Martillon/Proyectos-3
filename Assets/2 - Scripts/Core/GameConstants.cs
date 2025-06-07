namespace Scripts.Core
{
    /// <summary>
    /// Contains constant string values used throughout the game,
    /// such as PlayerPrefs keys, scene names, tags, and audio mixer parameters.
    /// This helps prevent errors from typos and centralizes these values for easy management.
    /// </summary>
    public static class GameConstants
    {
        // --- PlayerPrefs Keys ---
        public const string PrefsMasterVolume = "Volume_Master";
        public const string PrefsMusicVolume = "Volume_Music";
        public const string PrefsSfxVolume = "Volume_SFX";
        public const string PrefsResolutionWidth = "Resolution_Width";
        public const string PrefsResolutionHeight = "Resolution_Height";
        public const string PrefsVSync = "VSync";
        public const string PrefsDisplayMode = "DisplayMode";
        // Add other PlayerPrefs keys as needed...

        // --- Scene Names ---
        // Ensure these match your actual scene names in Build Settings.
        public const string ProgramSceneName = "Program";
        public const string MainMenuSceneName = "MainMenu";
        // Level scene names are managed in the SceneLoader's 'levels' array.

        // --- Tags ---
        public const string PlayerTag = "Player";
        public const string HittableTag = "Hittable";
        public const string PlatformTag = "Platform";
        // Add other tags as needed...

        // --- Layers ---
        public const string GroundLayerName = "Ground";
        public const string WallLayerName = "Walls";
        public const string PlatformLayerName = "Platform";
        public const string PlayerNonCollidingLayerName = "IgnorePlayer"; // For TraversablePlatform
        // Add other layer names as needed...

        // --- AudioMixer Parameter Names ---
        // Ensure these match exposed parameters in your AudioMixer.
        public const string MixerMasterVolume = "MasterVolume";
        public const string MixerMusicVolume = "MusicVolume";
        public const string MixerSfxVolume = "SFXVolume";

        // --- Animator Parameters / Triggers ---
        // Using strings here is fine, but for performance-critical animations,
        // it's often better to cache these with Animator.StringToHash() in the respective controllers.
        // Player
        public const string AnimIsMoving = "isMoving";
        public const string AnimIsGrounded = "isGrounded";
        public const string AnimIsCrouching = "isCrouching";
        public const string AnimVerticalSpeed = "verticalSpeed";
        public const string AnimArmorHitTrigger = "ArmorHitTrigger";
        public const string AnimLoseLifeTrigger = "Die"; // Can reuse 'Die' for losing a life
        public const string AnimRespawnTrigger = "Respawn";
        public const string AnimVictoryTrigger = "Victory";

        // Enemy
        public const string AnimDieTrigger = "Die";
        public const string AnimMeleeAttackTrigger = "MeleeAttackTrigger";
        public const string AnimRangedAttackTrigger = "RangedAttackTrigger";
        // Window Enemy specific (example names)
        public const string AnimWindowIsOpen = "isOpen";
        public const string AnimWindowAimDirX = "aimDirectionX";
        public const string AnimWindowAttack = "Attack";
    }
}
