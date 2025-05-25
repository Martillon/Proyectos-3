// --- START OF FILE GameConstants.cs ---

namespace Scripts.Core
{
    /// <summary>
    /// Contains constant string values used throughout the game,
    /// such as PlayerPrefs keys, scene names, tags, and audio mixer parameters.
    /// Helps prevent errors from typos and centralizes these values.
    /// </summary>
    public static class GameConstants
    {
        // PlayerPrefs Keys
        public const string PrefsMasterVolume = "Volume_Master";
        public const string PrefsMusicVolume = "Volume_Music";
        public const string PrefsSfxVolume = "Volume_SFX";
        public const string PrefsResolutionWidth = "Resolution_Width";
        public const string PrefsResolutionHeight = "Resolution_Height";
        public const string PrefsVSync = "VSync";
        public const string PrefsDisplayMode = "DisplayMode";
        // Add other PlayerPrefs keys as needed

        // Scene Names (Ensure these match your actual scene names in Build Settings)
        public const string ProgramSceneName = "Program"; // Your persistent scene
        public const string MainMenuSceneName = "MainMenu";
        // Add level scene names if needed, or manage them in SceneLoader array

        // Tags
        public const string PlayerTag = "Player";
        public const string HittableTag = "Hittable"; // For objects player projectiles can damage
        public const string PlatformTag = "Platform"; // For one-way platforms
        // Add other tags as needed

        // AudioMixer Parameter Names (Ensure these match exposed parameters in your AudioMixer)
        public const string MixerMasterVolume = "MasterVolume";
        public const string MixerMusicVolume = "MusicVolume";
        public const string MixerSfxVolume = "SFXVolume";

        // Animator Parameters / Triggers (Examples)
        public const string AnimDieTrigger = "Die";
        public const string AnimRespawnTrigger = "Respawn";
        public const string AnimVictoryTrigger = "Victory"; 
        
        //Layers
        public const string GroundLayerName = "Ground";
        public const string WallLayerName = "Walls";
        public const string PlatformLayerName = "Platform";

        // Input Action Map Names (Less common to need constants for these, but possible)
        // public const string InputMapPlayer = "Player";
        // public const string InputMapUI = "UI";
    }
}
// --- END OF FILE GameConstants.cs ---
