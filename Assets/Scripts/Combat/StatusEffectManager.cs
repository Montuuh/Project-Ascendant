using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.2 + Epic 4 Task 4.5 — central API for applying, ticking, curing,
    // and clearing status conditions. Pure static; no combat-loop dependency.
    //
    // Duration model: applied in turn N takes effect starting turn N+1, per OPEN G7.
    // Sentinel int.MaxValue = permanent (Burn/Poison). Decremented by
    // TickAtEndOfTurn; cleared on reaching 0.
    //
    // No RNG inside the apply/tick path — RNG only enters ResolveConfusionDiscard,
    // which the combat loop will call at Draw Phase with the CombatRNG stream.
    public static class StatusEffectManager
    {
        // Per §4.2.2 — apply a primary or secondary status. Returns false if
        // the target is immune (per §4.2.4) or the inputs are null.
        // Confusion goes to the secondary slot; everything else to primary.
        // Re-applying refreshes duration (per §4.2.2 "replaces existing").
        public static bool TryApply(PokemonInstance target, StatusCondition status, BattleConfigSO config)
        {
            if (target == null || config == null || status == StatusCondition.None) return false;
            if (IsImmune(target, status)) return false;

            if (status == StatusCondition.Confusion)
            {
                target.SecondaryStatus = StatusCondition.Confusion;
                target.SecondaryStatusTurnsRemaining = config.ConfusionDuration;
                return true;
            }

            // Primary: replace any existing primary status.
            target.PrimaryStatus = status;
            target.PrimaryStatusTurnsRemaining = GetDurationFor(status, config);
            return true;
        }

        // Per §4.2.4 — type-based immunity + per-species override list.
        public static bool IsImmune(PokemonInstance target, StatusCondition status)
        {
            if (target?.Species == null) return false;

            // Explicit species override list (PokemonSpeciesSO.StatusImmunities).
            List<StatusCondition> explicitList = target.Species.StatusImmunities;
            if (explicitList != null)
            {
                for (int i = 0; i < explicitList.Count; i++)
                    if (explicitList[i] == status) return true;
            }

            // Type-based immunity table per §4.2.4.
            List<PokemonType> types = target.Species.Types;
            if (types == null) return false;
            for (int i = 0; i < types.Count; i++)
            {
                PokemonType t = types[i];
                if (t == PokemonType.Fire && (status == StatusCondition.Burn || status == StatusCondition.Freeze)) return true;
                if (t == PokemonType.Ice && status == StatusCondition.Freeze) return true;
                if (t == PokemonType.Electric && status == StatusCondition.Paralysis) return true;
                if (t == PokemonType.Poison && status == StatusCondition.Poison) return true;
                if (t == PokemonType.Steel && status == StatusCondition.Poison) return true;
            }
            return false;
        }

        // Per §4.2.7 — single-status cure (Antidote, Burn Heal, etc.). Clears
        // the matching slot; leaves the other slot untouched.
        public static void Cure(PokemonInstance target, StatusCondition status)
        {
            if (target == null || status == StatusCondition.None) return;

            if (target.PrimaryStatus == status)
            {
                target.PrimaryStatus = StatusCondition.None;
                target.PrimaryStatusTurnsRemaining = 0;
            }
            if (target.SecondaryStatus == status)
            {
                target.SecondaryStatus = StatusCondition.None;
                target.SecondaryStatusTurnsRemaining = 0;
            }
        }

        // Per §4.2.7 — Full Heal behaviour: clear primary AND Confusion.
        public static void CureAll(PokemonInstance target)
        {
            if (target == null) return;
            target.PrimaryStatus = StatusCondition.None;
            target.PrimaryStatusTurnsRemaining = 0;
            target.SecondaryStatus = StatusCondition.None;
            target.SecondaryStatusTurnsRemaining = 0;
        }

        // Per §4.2.1 — combat end clears all status conditions automatically.
        // Equivalent to CureAll for now; named separately so combat-end
        // semantics stay explicit at call sites.
        public static void ClearOnCombatEnd(PokemonInstance target) => CureAll(target);

        // Per §4.2.5 — decrement both duration counters; clear slots that hit 0.
        // Permanent (int.MaxValue) durations are left untouched. Call once per
        // turn end (after DoT has been applied).
        public static void TickAtEndOfTurn(PokemonInstance target)
        {
            if (target == null) return;

            if (target.PrimaryStatus != StatusCondition.None && target.PrimaryStatusTurnsRemaining != int.MaxValue)
            {
                target.PrimaryStatusTurnsRemaining = Mathf.Max(0, target.PrimaryStatusTurnsRemaining - 1);
                if (target.PrimaryStatusTurnsRemaining == 0)
                    target.PrimaryStatus = StatusCondition.None;
            }

            if (target.SecondaryStatus != StatusCondition.None && target.SecondaryStatusTurnsRemaining != int.MaxValue)
            {
                target.SecondaryStatusTurnsRemaining = Mathf.Max(0, target.SecondaryStatusTurnsRemaining - 1);
                if (target.SecondaryStatusTurnsRemaining == 0)
                    target.SecondaryStatus = StatusCondition.None;
            }
        }

        // Per §4.2.2.1/2 — DoT damage from Burn (Atk-debuffing) or Poison
        // (Def-debuffing). Returns the damage value; caller (combat loop)
        // subtracts from CurrentHP. Min 1 per spec; 0 if no DoT status.
        public static int ComputeDoTDamage(PokemonInstance target, BattleConfigSO config)
        {
            if (target?.Species == null || config == null) return 0;

            int divisor;
            switch (target.PrimaryStatus)
            {
                case StatusCondition.Burn:   divisor = config.BurnDoTDivisor;   break;
                case StatusCondition.Poison: divisor = config.PoisonDoTDivisor; break;
                default: return 0;
            }
            if (divisor <= 0) return 0;

            int maxHP = target.Species.BaseStats.BaseHP;
            if (target.Species.GrowthCurve != null)
                maxHP += target.Species.GrowthCurve.GetHPAt(target.Level);

            int dot = maxHP / divisor;
            return Mathf.Max(1, dot);
        }

        // Per §4.2.3.1 — at Draw Phase, each Confused Pokémon discards 1 random
        // skill card from the drawn hand. Consumable cards are immune (caller
        // passes only skill cards into this method). RNG comes from CombatRNG
        // (per §9.7.2). Returns the index that was discarded (and removes it),
        // or -1 if no Confusion / empty hand.
        public static int ResolveConfusionDiscard(PokemonInstance owner, IList<MoveSO> drawnSkillCards, GameRNG rng)
        {
            if (owner == null || drawnSkillCards == null || drawnSkillCards.Count == 0 || rng == null) return -1;
            if (owner.SecondaryStatus != StatusCondition.Confusion) return -1;

            int idx = rng.Range(0, drawnSkillCards.Count);
            drawnSkillCards.RemoveAt(idx);
            return idx;
        }

        // ── Duration resolver per §4.2.5 ──────────────────────────────────────

        private static int GetDurationFor(StatusCondition status, BattleConfigSO config)
        {
            switch (status)
            {
                case StatusCondition.Burn:
                case StatusCondition.Poison:
                    return int.MaxValue;                 // permanent
                case StatusCondition.Paralysis: return config.ParalysisDuration;
                case StatusCondition.Sleep:     return config.SleepDuration;
                case StatusCondition.Freeze:    return config.FreezeDuration;
                case StatusCondition.Confusion: return config.ConfusionDuration;
                default: return 0;
            }
        }
    }
}
