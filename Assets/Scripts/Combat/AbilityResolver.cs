using System.Collections.Generic;
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

        // ── Non-damage hooks (latent in current VS content; mechanisms wired + tested) ──────────

        // §5.5.3.1 Keen Eye — true if any live active-team member reveals Unknown intents at combat
        // start. (No-op while all VS intents are already Witnessed; matters once Hidden intents exist.)
        public static bool TeamRevealsIntents(IReadOnlyList<PokemonInstance> activeTeam)
        {
            if (activeTeam == null) return false;
            for (int i = 0; i < activeTeam.Count; i++)
                if (HasAbility(activeTeam[i], "keen_eye")) return true;
            return false;
        }

        // §5.5.3 Static — roll a Paralysis rider when dealing damage with an Electric move.
        public static bool RollStaticParalysis(PokemonInstance attacker, MoveSO move, BattleConfigSO cfg, GameRNG rng)
        {
            if (move == null || cfg == null || rng == null) return false;
            if (!HasAbility(attacker, "static") || move.Type != PokemonType.Electric) return false;
            return rng.Range(0, 100) < cfg.StaticParalysisChancePercent;
        }

        // §5.5.3 Swift Swim — extra skill cards on turn 1 of a Rain-active combat for a team holding it.
        public static int SwiftSwimDrawBonus(IReadOnlyList<PokemonInstance> activeTeam, FieldState field,
                                             int turnNumber, BattleConfigSO cfg)
        {
            if (cfg == null || turnNumber != 1 || field.Weather != FieldEffectKind.RainDance) return 0;
            if (activeTeam == null) return 0;
            for (int i = 0; i < activeTeam.Count; i++)
                if (HasAbility(activeTeam[i], "swift_swim")) return cfg.SwiftSwimDrawBonus;
            return 0;
        }

        // §5.5.3.5 Intimidate — on entering Lead, lower every live enemy's Attack one stage. Re-applies
        // each entry (manual swap / SF / SB / faint-replacement / combat start).
        public static void ApplyLeadEntryEffects(CombatController.CombatState state)
        {
            if (state == null) return;
            PokemonInstance lead = state.LeadIndex >= 0 && state.LeadIndex < state.PlayerTeam.Count
                ? state.PlayerTeam[state.LeadIndex] : null;
            if (!HasAbility(lead, "intimidate") || state.EnemyTeam == null) return;
            for (int i = 0; i < state.EnemyTeam.Count; i++)
                if (state.EnemyTeam[i] != null && state.EnemyTeam[i].CurrentHP > 0)
                    StatStageManager.Modify(state.EnemyTeam[i], Stat.Attack, -1);
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static bool HasAbility(PokemonInstance p, string abilityId)
            => p != null && p.CurrentHP > 0 && p.Ability != null && p.Ability.AbilityId == abilityId;

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
