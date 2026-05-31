using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §5.5 / §5.8 + Epic 10 Task 10.9 — runtime application of passive abilities at the combat
    // damage hook points. VS implementation: a central resolver that dispatches by AbilityId at the
    // seams already present in CombatController.ResolveDamage. This is faithful to each ability's
    // effect while deferring the generic ScriptableHook event-bus (AbilitySO.EffectHook, §10.9.1) to
    // post-VS, when the full ~30-ability catalog + modding (§9.13) justify the infrastructure.
    //
    // Damage-axis abilities handled here (one hook location):
    //   • Overgrow / Blaze / Torrent (§5.5.3.4) — matching-type moves +X% while HP < threshold.
    //   • Shell Armor (§5.5.3)                  — flat incoming reduction on the Lead.
    //   • Levitate (§5.5.3.3)                    — immune to Ground-type moves.
    //   • Sturdy (§5.5.3 / §4.4.3)               — survive one otherwise-lethal hit at 1 HP per combat.
    // Rider/positional/vision abilities (Static / Intimidate / Keen Eye / Swift Swim) hook elsewhere —
    // a later slice.
    public static class AbilityResolver
    {
        // §5.5.3.4 — the attacker's outgoing damage multiplier for a given move (1.0 = no change).
        public static float OutgoingDamageMultiplier(PokemonInstance attacker, MoveSO move, BattleConfigSO cfg)
        {
            if (attacker?.Ability == null || move == null || cfg == null) return 1f;
            PokemonType? boostType = LowHpBoostType(attacker.Ability.AbilityId);
            if (boostType == null || move.Type != boostType.Value) return 1f;
            return IsBelowHpFraction(attacker, cfg.AbilityLowHpThreshold) ? cfg.AbilityLowHpBoostMultiplier : 1f;
        }

        // §5.5.3 Shell Armor — flat incoming-damage reduction granted by the defender's ability.
        public static int IncomingFlatReduction(PokemonInstance defender, BattleConfigSO cfg)
            => cfg != null && defender?.Ability != null && defender.Ability.AbilityId == "shell_armor"
                ? cfg.ShellArmorFlatReduction : 0;

        // §5.5.3.3 Levitate — the defender is immune to Ground-type moves.
        public static bool IsImmuneTo(PokemonInstance defender, MoveSO move)
            => move != null && move.Type == PokemonType.Ground
               && defender?.Ability != null && defender.Ability.AbilityId == "levitate";

        // §5.5.3 / §4.4.3 Sturdy — survive one otherwise-lethal hit at 1 HP per combat. The ability
        // grants the same protection as the boss-ace HasSturdy flag (consumed via SturdyConsumed).
        public static bool HasSturdy(PokemonInstance p)
            => p != null && (p.HasSturdy || (p.Ability != null && p.Ability.AbilityId == "sturdy"));

        // ── helpers ───────────────────────────────────────────────────────────

        private static PokemonType? LowHpBoostType(string abilityId) => abilityId switch
        {
            "overgrow" => PokemonType.Grass,
            "blaze" => PokemonType.Fire,
            "torrent" => PokemonType.Water,
            _ => null,
        };

        private static bool IsBelowHpFraction(PokemonInstance p, float frac)
        {
            int max = PokemonVitals.MaxHP(p);
            return max > 0 && (float)p.CurrentHP / max < frac;
        }
    }
}
