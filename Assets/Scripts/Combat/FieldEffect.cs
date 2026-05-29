using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.3.8 + Epic 4 Task 4.9 — field effect kinds. Two categories
    // (Weather and Terrain) are independent and can BOTH be active per
    // §4.8.2. Within a category, applying a second effect overwrites the
    // first.
    //
    // VS scope (per §4.3.8):
    //   Weather: SunnyDay, RainDance
    //   Terrain: ElectricTerrain
    public enum FieldEffectKind
    {
        None,
        SunnyDay,         // Weather — Fire ×1.5, Water ×0.5
        RainDance,        // Weather — Water ×1.5, Fire ×0.5
        ElectricTerrain   // Terrain — Electric ×1.3 to grounded; blocks
                          // Paralysis on grounded targets (§4.3.8.3)
    }

    public enum FieldCategory { Weather, Terrain }

    // Holds the active field state per encounter. Both slots can be set
    // simultaneously (§4.8.2). Combat-end clears both back to None.
    // Value-semantic struct so it can be passed by value through the
    // scoring/resolution pipeline without GC.
    public struct FieldState
    {
        public FieldEffectKind Weather;
        public FieldEffectKind Terrain;

        // Per §4.4.4.3 + Task 8.5.5 — a Gym Leader sets a field effect matching
        // its type at encounter start; it persists the whole fight. The VS
        // ships this as a PLACEHOLDER marker only: it is set, persists, and is
        // cleared at combat end, but applies no damage multiplier yet — the
        // mechanical effect of a type field (e.g. Rock) is unspecified in the
        // GDD. See the ⚠ OPEN flag on §4.4.4.3 / BACKLOG gap #33.
        // PokemonType has no "None" member, so HasGymField gates GymTypeField.
        public bool HasGymField;
        public PokemonType GymTypeField;

        public static FieldState Empty => new()
        {
            Weather = FieldEffectKind.None,
            Terrain = FieldEffectKind.None,
            HasGymField = false,
            GymTypeField = default,
        };
    }
}
