using UnityEngine;

namespace Core.Pause_Menu.Scripts
{
    public static class PlayerPrefsReferences
    {
     
        /// <summary> Stored as percentage </summary>
        public static readonly (string key, float percentage) MainVolume = ("MainVolume", 0.5f);

        /// <summary> Stored as percentage </summary>
        public static readonly (string key, float percentage) MusicVolume = ("MusicVolume", 0.5f);

        /// <summary> Stored as percentage </summary>
        public static readonly (string key, float percentage) SfxVolume = ("SfxVolume", 0.5f);

        /// <summary> Stored as percentage </summary>
        public static readonly (string key, float percentage) VoiceVolume = ("VoiceVolume", 0.5f);

        /// <summary> Format: 1920x1080 @ 60 </summary>
        public static readonly (string key, string resolution) Resolution = ("Resolution", "1280x720@60");
        
        public static readonly (string key, FullScreenMode mode) ScreenMode = ("ScreenMode", FullScreenMode.Windowed);

        public static readonly (string key, bool active) Vsync = ("Vsync", true);
        
        public static readonly (string key, bool active) TypeWriterSound = ("TypeWriterSound", true);
        
        public static readonly (string key, float delaySeconds) TextDelay = ("TextDelay", 0.2f);
        
        public static readonly (string key, int frameRate) TargetFrameRate = ("TargetFrameRate", 60);
        
        public static readonly (string key, int language) Language = ("Language", 0);
        
        public static readonly (string key, float speedMultiplier) CombatAnimationSpeed = ("CombatAnimationSpeed", 1f);
        public static float ClampCombatAnimationSpeed(float input) => Mathf.Clamp(input, 0.25f, 4f);

        public static readonly (string key, int tickRate) CombatTickRate = ("CombatTickRate", 40);
        public static int ClampCombatTickRate(int input) => Mathf.Clamp(input, 10, 60);
        
        public static readonly (string key, int setting) SkillOverlayMode = ("SkillOverlayMode", 0);
        public static readonly (string key, float duration) SkillOverlayAutoDuration = ("SkillOverlayAutoDuration", 3f);

        public static float ClampSkillOverlayAutoDuration(float memorizedDuration) => Mathf.Clamp(memorizedDuration, 0f, 20f);
    }
}