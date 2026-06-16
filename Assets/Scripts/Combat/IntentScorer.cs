using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.3.3 + Epic 4 Task 4.7.B — context-aware intent scoring + selection.
    //
    // Formula (load-bearing per §4.3.3):
    //   Score = BaseWeight × TypeEff × StatusState × HPState × CooldownGate
    //
    //   • BaseWeight     — MoveSO.BasePower when > 0, else
    //                      Config.DefaultUtilityWeight (covers Buff/Stall/Status).
    //   • TypeEff        — TypeChart.GetMultiplier against the slot's current
    //                      occupant's types (only for damage/status intents).
    //   • StatusState    — ×0 on redundant primary application; full weight
    //                      if target only has a secondary (Confusion). (§4.3.3)
    //   • HPState        — three independent thresholds:
    //                        target HP < 30%  → ×2.0 for Attack/Backstrike
    //                        self   HP < 40%  → ×1.5 for any offensive intent
    //                        self   HP > 70%  → ×1.5 for Buff/Stall
    //   • CooldownGate   — 0 if the attacker has the move on cooldown
    //                      (Move.CooldownTurns > 0 + set on use by
    //                      CombatController). 1 otherwise. Authored on
    //                      signature/ultimate boss/elite moves per §4.3.3.
    //   • PhaseAggression— ×Config.BossPhaseAggressionMultiplier on offensive
    //                      intents when ctx.PhaseAggressive (boss/Elite in
    //                      Phase 2+, §4.4.3 — "plays urgently and aggressively").
    //                      1.0 otherwise. Default off → ordinary encounters
    //                      score exactly as before.
    //
    // Selection (§4.3.3 randomness floor):
    //   PickIntent runs all candidate scores → ranks descending → with
    //   Config.RandomnessFloorChance probability picks a weighted-random
    //   non-top intent instead. Disabled when BossCounterIntelActive is true
    //   (in that mode the top score is reduced by BossCounterIntelTopPenalty
    //   first, then the top is selected deterministically). Also disabled when
    //   PhaseAggressive is true — an aggressive-phase boss commits to its top
    //   pick rather than hedging (§4.4.3 — "plays urgently").
    //
    // RNG: PickIntent takes a GameRNG (CombatRNG stream per §9.7.2). Score
    // alone is RNG-free.
    public static class IntentScorer
    {
        public struct Context
        {
            public PokemonInstance Attacker;
            public IReadOnlyList<PokemonInstance> PlayerTeam; // [0]=Lead, [1..]=Bench
            public BattleConfigSO Config;
            public bool BossCounterIntelActive;               // §4.3.5
            public bool PhaseAggressive;                      // §4.4.3 Phase 2+
        }

        // Per §4.3.3 — full scoring formula. Returns 0 for malformed inputs
        // so PickIntent treats them as non-pickable.
        public static float Score(Intent intent, in Context ctx)
        {
            if (ctx.Config == null) return 0f;
            // Unknown intents are never SCORED — they're a display state, not
            // an executable choice. The scorer only runs over revealed moves.
            if (intent.Kind == IntentKind.Unknown) return 0f;

            float baseWeight = BaseWeight(intent, ctx.Config);
            if (baseWeight <= 0f) return 0f;

            float typeEff = TypeEffMultiplier(intent, ctx);
            if (typeEff == 0f) return 0f;        // immune target → never picked

            float statusState = StatusStateMultiplier(intent, ctx);
            if (statusState == 0f) return 0f;     // redundant primary → skip

            float hpState = HPStateMultiplier(intent, ctx);
            float cooldownGate = CooldownGate(intent, ctx.Attacker);
            if (cooldownGate == 0f) return 0f;

            float phaseAggression = PhaseAggressionMultiplier(intent, ctx);

            return baseWeight * typeEff * statusState * hpState * cooldownGate
                 * phaseAggression;
        }

        // Per §4.3.3 — selection function. Pure given (candidates, ctx, rng).
        // Returns the picked Intent (or the highest-scoring one as fallback).
        // List is treated as read-only; never mutated.
        public static Intent PickIntent(IReadOnlyList<Intent> candidates,
                                        in Context ctx,
                                        GameRNG rng)
        {
            if (candidates == null || candidates.Count == 0)
                return default;

            // Score all candidates once.
            float[] scores = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
                scores[i] = Score(candidates[i], in ctx);

            // Find top.
            int topIdx = 0;
            for (int i = 1; i < candidates.Count; i++)
                if (scores[i] > scores[topIdx]) topIdx = i;

            // Per §4.3.5 + Epic 4.7.7 — boss counter-intel: apply penalty to
            // the top, then re-rank. Randomness floor is DISABLED in this mode.
            if (ctx.BossCounterIntelActive && ctx.Config != null)
            {
                scores[topIdx] *= ctx.Config.BossCounterIntelTopPenalty;
                topIdx = 0;
                for (int i = 1; i < candidates.Count; i++)
                    if (scores[i] > scores[topIdx]) topIdx = i;
                return candidates[topIdx];
            }

            // Per §4.3.3 — randomness floor: with RandomnessFloorChance, pick
            // a weighted-random NON-TOP candidate. Requires >= 2 candidates with
            // > 0 non-top score. Per §4.4.3 — an aggressive-phase boss does not
            // hedge: it commits to the top pick, so the floor is suppressed.
            float floorChance = (ctx.Config != null && !ctx.PhaseAggressive)
                ? ctx.Config.RandomnessFloorChance : 0f;
            if (rng != null && floorChance > 0f && candidates.Count >= 2)
            {
                if (rng.Range01() < floorChance)
                {
                    int nonTopPick = PickWeightedNonTop(scores, topIdx, rng);
                    if (nonTopPick >= 0) return candidates[nonTopPick];
                }
            }

            return candidates[topIdx];
        }

        // ── Scoring components — pure functions over (intent, ctx) ───────────

        // Per §4.3.3 — BaseWeight derivation. BasePower carries the intent's
        // raw "interesting-ness"; utility moves use the config fallback.
        private static float BaseWeight(Intent intent, BattleConfigSO config)
        {
            if (intent.Move != null && intent.Move.BasePower > 0)
                return intent.Move.BasePower;
            return config.DefaultUtilityWeight;
        }

        // Per §4.3.3 TypeEffectivenessMultiplier — looks up the move type
        // vs the slot's current occupant's defensive types.
        //   • super-effective → ×2.0 (already in TypeChart product)
        //   • resisted        → ×0.5
        //   • immune          → ×0.0
        // Returns 1.0 for intents that don't target a slot (Buff/Stall/Cleave),
        // and for slot intents whose target slot has no live occupant.
        private static float TypeEffMultiplier(Intent intent, in Context ctx)
        {
            if (!intent.TargetsSlot) return 1f;
            if (intent.Move == null) return 1f;

            PokemonInstance occ = IntentTargeting.ResolveSlotOccupant(
                intent.TargetSlot, ctx.PlayerTeam);
            if (occ == null || occ.Species == null) return 1f;

            return (float)TypeChart.GetMultiplier(intent.Move.Type, occ.Species.Types);
        }

        // Per §4.3.3 StatusStateModifier — only Status intents are gated:
        // ×0 if the target already has a primary status (Confusion-only is fine).
        private static float StatusStateMultiplier(Intent intent, in Context ctx)
        {
            if (intent.Kind != IntentKind.Status) return 1f;

            PokemonInstance occ = IntentTargeting.ResolveSlotOccupant(
                intent.TargetSlot, ctx.PlayerTeam);
            if (occ == null) return 1f;

            // Per §4.3.3 — "Status intent vs already-statused Pokémon (primary): ×0".
            // Secondary (Confusion) does NOT block a new primary status.
            if (occ.PrimaryStatus != StatusCondition.None) return 0f;

            // Per §4.2.4 type immunity — AI also avoids these for Status moves.
            if (StatusEffectManager.IsImmune(occ, intent.AppliedStatus)) return 0f;

            return 1f;
        }

        // Per §4.3.3 HPStateModifier — three independent thresholds.
        // Multiplicative combination so they stack when multiple apply.
        private static float HPStateMultiplier(Intent intent, in Context ctx)
        {
            BattleConfigSO cfg = ctx.Config;
            float mod = 1f;
            bool isOffensive = intent.Kind == IntentKind.Attack
                            || intent.Kind == IntentKind.Cleave
                            || intent.Kind == IntentKind.Backstrike;
            bool isSetup = intent.Kind == IntentKind.Buff
                        || intent.Kind == IntentKind.Stall;

            // Target HP < threshold → Attack/Backstrike get bonus.
            if (intent.TargetsSlot
                && (intent.Kind == IntentKind.Attack || intent.Kind == IntentKind.Backstrike))
            {
                PokemonInstance occ = IntentTargeting.ResolveSlotOccupant(
                    intent.TargetSlot, ctx.PlayerTeam);
                if (occ != null && HPFraction(occ) < cfg.LowTargetHPThreshold)
                    mod *= cfg.LowTargetHPMultiplier;
            }

            // Self HP < threshold → aggression bonus on any offensive intent.
            if (isOffensive && HPFraction(ctx.Attacker) < cfg.LowSelfHPThreshold)
                mod *= cfg.AggressiveSelfMultiplier;

            // Self HP > threshold → setup bonus.
            if (isSetup && HPFraction(ctx.Attacker) > cfg.HighSelfHPThreshold)
                mod *= cfg.SetupSelfMultiplier;

            return mod;
        }

        // Per §4.4.3 — Phase 2+ aggression bias. An aggressive-phase boss
        // weights its OFFENSIVE intents up by BossPhaseAggressionMultiplier so
        // it favours pressing damage over setup/utility ("plays urgently and
        // aggressively"). 1.0 when not aggressive or for non-offensive intents
        // — so ordinary encounters (PhaseAggressive == false) are unaffected.
        private static float PhaseAggressionMultiplier(Intent intent, in Context ctx)
        {
            if (!ctx.PhaseAggressive || ctx.Config == null) return 1f;
            bool isOffensive = intent.Kind == IntentKind.Attack
                            || intent.Kind == IntentKind.Cleave
                            || intent.Kind == IntentKind.Backstrike;
            return isOffensive ? ctx.Config.BossPhaseAggressionMultiplier : 1f;
        }

        // Per §4.3.3 — 0 if the attacker has this move on cooldown, 1 otherwise.
        // Cooldown set/tick happens in CombatController; this scorer is pure
        // over (intent, attacker) and never mutates state.
        private static float CooldownGate(Intent intent, PokemonInstance attacker)
        {
            if (attacker == null || intent.Move == null) return 1f;
            return attacker.IsMoveOnCooldown(intent.Move) ? 0f : 1f;
        }

        // Per #44 — MaxHP routes through PokemonVitals.MaxHP so the Iron Will enemy-HP
        // multiplier is reflected in the AI's HP-fraction reads (low-HP targeting, etc.).
        private static float HPFraction(PokemonInstance p)
        {
            if (p == null || p.Species == null) return 1f;
            int max = PokemonVitals.MaxHP(p);
            if (max <= 0) return 1f;
            return Mathf.Clamp01((float)p.CurrentHP / max);
        }

        // Weighted random over non-top candidates. Indices with 0 score are
        // excluded so immune/redundant intents never become the "surprise pick".
        private static int PickWeightedNonTop(float[] scores, int topIdx, GameRNG rng)
        {
            float total = 0f;
            for (int i = 0; i < scores.Length; i++)
                if (i != topIdx && scores[i] > 0f) total += scores[i];
            if (total <= 0f) return -1;

            float roll = rng.Range01() * total;
            float acc = 0f;
            for (int i = 0; i < scores.Length; i++)
            {
                if (i == topIdx) continue;
                if (scores[i] <= 0f) continue;
                acc += scores[i];
                if (roll <= acc) return i;
            }
            return -1; // unreachable given total > 0
        }
    }
}
