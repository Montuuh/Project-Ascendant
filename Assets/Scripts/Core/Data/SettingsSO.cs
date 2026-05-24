using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.8.1 — user settings; serialized to settings.json on change.
    // TODO: Epic 13 (§10.6) — full schema: ColorblindMode, ReducedMotion, MasterVolume,
    //       MusicVolume, SFXVolume, KeyBindings, SubtitlesEnabled.
    [CreateAssetMenu(menuName = "ProjectAscendant/Settings")]
    public class SettingsSO : ScriptableObject { }
}
