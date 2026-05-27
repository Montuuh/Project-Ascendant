using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.3.8 + §4.8.2 + Epic 4 Task 4.9 — pure query helpers for the
    // active FieldState. Stateless; CombatController owns the FieldState
    // and consults this for damage/status decisions.
    //
    // Composition rule (§4.8.2): Weather and Terrain are independent and
    // their multipliers stack MULTIPLICATIVELY (Sunny Day Fire ×1.5 × any
    // future Terrain-Fire boost = product).
    //
    // Grounded gate: Electric Terrain only affects "grounded" Pokémon.
    // VS treats Flying types as non-grounded. (TODO: Levitate ability is
    // not authored yet — when it is, add a check on
    // PokemonInstance.Ability.IsLevitate or similar.)
    public static class FieldEffectResolver
    {
        // Per §4.3.8 — multiplicative damage multiplier for a move type
        // under the current FieldState. Returns 1.0 when no effect applies.
        public static float GetDamageMultiplier(in FieldState field,
                                                PokemonType moveType,
                                                PokemonInstance target,
                                                BattleConfigSO config)
        {
            if (config == null) return 1f;

            float mul = 1f;

            // Weather slot
            switch (field.Weather)
            {
                case FieldEffectKind.SunnyDay:
                    if (moveType == PokemonType.Fire)  mul *= config.SunnyDayFireMultiplier;
                    if (moveType == PokemonType.Water) mul *= config.SunnyDayWaterMultiplier;
                    break;
                case FieldEffectKind.RainDance:
                    if (moveType == PokemonType.Water) mul *= config.RainDanceWaterMultiplier;
                    if (moveType == PokemonType.Fire)  mul *= config.RainDanceFireMultiplier;
                    break;
            }

            // Terrain slot — Electric Terrain only boosts Electric against
            // grounded targets (§4.3.8.3).
            switch (field.Terrain)
            {
                case FieldEffectKind.ElectricTerrain:
                    if (moveType == PokemonType.Electric && IsGrounded(target))
                        mul *= config.ElectricTerrainElectricMultiplier;
                    break;
            }

            return mul;
        }

        // Per §4.3.8.3 — Paralysis cannot be applied to grounded Pokémon
        // while Electric Terrain is active. The §4.2.4 type-immunity check
        // still applies independently (Electric-types are immune anyway).
        public static bool CanApplyParalysis(in FieldState field, PokemonInstance target)
        {
            if (field.Terrain == FieldEffectKind.ElectricTerrain && IsGrounded(target))
                return false;
            return true;
        }

        // Per §4.3.8.3 (implicit) — VS rule: Flying types are non-grounded.
        // Levitate ability and floating Pokémon (Magnezone, etc.) will be
        // added once those species ship.
        public static bool IsGrounded(PokemonInstance p)
        {
            if (p == null || p.Species == null) return true; // safe default
            if (p.Species.Types == null) return true;
            for (int i = 0; i < p.Species.Types.Count; i++)
                if (p.Species.Types[i] == PokemonType.Flying) return false;
            return true;
        }

        // Per §4.3.8.* — single-source category lookup for applying new effects.
        public static FieldCategory CategoryOf(FieldEffectKind kind)
        {
            switch (kind)
            {
                case FieldEffectKind.SunnyDay:
                case FieldEffectKind.RainDance:
                    return FieldCategory.Weather;
                case FieldEffectKind.ElectricTerrain:
                    return FieldCategory.Terrain;
                default:
                    return FieldCategory.Weather; // None — arbitrary
            }
        }

        // Per §4.8.2 — applying a new effect overwrites within its category.
        // Returns the new FieldState (value copy).
        public static FieldState Apply(in FieldState current, FieldEffectKind incoming)
        {
            FieldState next = current;
            if (incoming == FieldEffectKind.None) return next;
            FieldCategory cat = CategoryOf(incoming);
            if (cat == FieldCategory.Weather) next.Weather = incoming;
            else                              next.Terrain = incoming;
            return next;
        }
    }
}
