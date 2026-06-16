using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.8.1 + §10.6 — user settings. Serialized to settings.json on every change.
    // Epic 13 (UI/UX) and Epic 15 (Accessibility) wire the full settings screen.
    [CreateAssetMenu(menuName = "Project Ascendant/Runtime/Settings")]
    public class SettingsSO : ScriptableObject
    {
        [Header("Audio — §10.X")]
        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Range(0f, 1f)] public float MusicVolume = 0.8f;
        [Range(0f, 1f)] public float SFXVolume = 1f;

        [Header("Accessibility — §10.5 (Epic 15)")]
        // Per §10.5 — colorblind mode replaces colour-only type cues with icon overlays.
        public bool ColorblindMode;

        // Per §10.5 — disables screen shake, flash, and particle bursts.
        public bool ReducedMotion;

        // Per §10.5 — shows dialogue subtitles and move-resolution captions.
        public bool SubtitlesEnabled = true;

        [Header("Display")]
        public bool FullScreen = true;

        [Tooltip("Target frame rate. -1 = platform default.")]
        public int TargetFrameRate = 60;

        // Per §9.10 — key binding overrides serialized as JSON via Epic 13.
        // Stored as a raw JSON string for New Input System compatibility.
        // TODO: Epic 13 — replace with structured InputActionAsset override.
        [HideInInspector]
        public string KeyBindingsJson = string.Empty;

        // Per §9.8.7.6 / §10.6 — push the engine-level knobs to runtime state at boot
        // (after SaveSystem.LoadSettings restores them). The full settings screen,
        // audio-mixer routing for Music/SFX, and input rebinds are Epic 13/14/15; this
        // covers the values that only take effect via engine APIs so a restored setting
        // is actually live on launch. Part of BACKLOG #47.
        public void ApplyToEngine()
        {
            AudioListener.volume = MasterVolume;
            Application.targetFrameRate = TargetFrameRate;
            Screen.fullScreen = FullScreen;
        }
    }
}
