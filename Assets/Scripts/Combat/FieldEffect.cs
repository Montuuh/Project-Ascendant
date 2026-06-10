using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.3.8 (CL-012) + Epic 4 Task 4.9 — field effect kinds. Three independent
    // categories (Weather, Terrain, Hazard) can ALL be active at once per §4.8.2.
    // Within a category, applying a second effect overwrites the first.
    //
    // Launch set (per §4.3.8):
    //   Weather: SunnyDay, RainDance
    //   Terrain: ElectricTerrain
    //   Hazard:  Sandstorm (CL-012)
    public enum FieldEffectKind
    {
        None,
        SunnyDay,         // Weather — Fire ×1.5, Water ×0.5
        RainDance,        // Weather — Water ×1.5, Fire ×0.5
        ElectricTerrain,  // Terrain — Electric ×1.3 to grounded; blocks
                          // Paralysis on grounded targets (§4.3.8.3)
        Sandstorm         // Hazard (CL-012, §4.3.8.4) — non-Rock/Ground/Steel lose
                          // 5% max HP at end of their turn. No type multiplier.
    }

    public enum FieldCategory { Weather, Terrain, Hazard }

    // Holds the active field state per encounter. Both slots can be set
    // simultaneously (§4.8.2). Combat-end clears both back to None.
    // Value-semantic struct so it can be passed by value through the
    // scoring/resolution pipeline without GC.
    public struct FieldState
    {
        public FieldEffectKind Weather;
        public FieldEffectKind Terrain;
        public FieldEffectKind Hazard;   // CL-012 (§4.3.8.4) — independent slot (e.g. Sandstorm)

        // Per §4.3.8.5 (CL-012) — Home Field: a Gym Leader / Elite sets an enemy-owned
        // type field at encounter start, persisting the whole fight. ENEMY moves of
        // GymTypeField get ×HomeFieldTypeMultiplier (config); player moves of that type
        // get no boost (one-sided). Closes the old gap #33 (was an inert marker).
        // PokemonType has no "None" member, so HasGymField gates GymTypeField.
        public bool HasGymField;
        public PokemonType GymTypeField;

        public static FieldState Empty => new()
        {
            Weather = FieldEffectKind.None,
            Terrain = FieldEffectKind.None,
            Hazard = FieldEffectKind.None,
            HasGymField = false,
            GymTypeField = default,
        };
    }
}
