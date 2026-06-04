using System;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.1.1 + Epic 4 Tasks 4.2.1–4.2.4 — pure-function damage resolver.
    //
    // Operational formula (matches BattleConfigSO.cs header comment):
    //
    //     base    = Power × (EffAtk / EffDef) × Range / Divisor
    //     postCrit= base × Crit             (Crit applied here — see OPEN G3)
    //     final   = floor( postCrit × STAB × TypeEff )
    //
    // OPEN G1 — algebraic combination not written in §4.1.1; default per
    //           BattleConfigSO comment.
    // OPEN G2 — Range bundled into BaseDamage (intrinsic to move power profile).
    // OPEN G3 — Ordering is presentational; floor only at end → commutative.
    // OPEN G4 — No min-damage clamp. Only immunity (TypeEff=0) yields 0.
    //
    // No RNG inside this calculator. Crit is a resolved input (Task 4.4 owns
    // the roll). Move.AlwaysCrit forces crit=true regardless of input (§4.1.3).
    //
    // Per §4.4.5.1 — Normal Badge: +10% to base stats (applied in CombatStatResolver).
    public static class DamageCalculator
    {
        public static DamageBreakdown Compute(in MoveContext ctx,
            System.Collections.Generic.IReadOnlyList<BadgeSO> attackerBadges = null,
            System.Collections.Generic.IReadOnlyList<BadgeSO> targetBadges = null)
        {
            int power = ctx.Move != null ? ctx.Move.BasePower : 0;
            // Per §4.4.5.1 — Normal Badge boosts only the PLAYER's Pokémon: the caller passes its badges
            // for the attacker side only when the attacker is player-owned, and for the target side only
            // when the target is player-owned. So a player badge never buffs an enemy's Atk/Def.
            int atk   = CombatStatResolver.EffectiveAttack(ctx.Attacker, ctx.Config, attackerBadges);
            int def   = Mathf.Max(1, CombatStatResolver.EffectiveDefense(ctx.Target, ctx.Config, targetBadges));
            double range = ctx.Move != null ? ctx.Move.RangeModifierMultiplier : 1.0;

            // Per §4.1.3 — AlwaysCrit is independent and forces crit regardless of
            // external chance roll. Stacks (rule-wise) by short-circuiting the bool.
            bool alwaysCrit = ctx.Move != null && ctx.Move.AlwaysCrit;
            bool isCrit = ctx.Crit || alwaysCrit;
            double crit = isCrit ? ctx.Config.CritMultiplier : 1.0;

            bool hasStab = HasStab(ctx.Attacker?.Species, ctx.Move != null ? ctx.Move.Type : default);
            double stab  = hasStab ? ctx.Config.StabMultiplier : 1.0;

            double typeEff = ctx.Move != null
                ? TypeChart.GetMultiplier(ctx.Move.Type, ctx.Target?.Species?.Types)
                : 1.0;

            int divisor = Mathf.Max(1, ctx.Config.Divisor);

            // BaseDamage rolls Range in (OPEN G2). All arithmetic in double until
            // the single floor at the end (Task 4.2.4).
            double baseDamage = (double)power * atk / def * range / divisor;
            double postCrit   = baseDamage * crit;
            double scaled     = postCrit * stab * typeEff;
            int final         = (int)Math.Floor(scaled);

            return new DamageBreakdown(
                power, atk, def,
                baseDamage,
                crit, stab, typeEff, range,
                final,
                isCrit, hasStab);
        }

        // Hover-UI alias per Task 4.2.2 — same calculation, identical output.
        public static DamageBreakdown Preview(in MoveContext ctx,
            System.Collections.Generic.IReadOnlyList<BadgeSO> attackerBadges = null,
            System.Collections.Generic.IReadOnlyList<BadgeSO> targetBadges = null)
            => Compute(ctx, attackerBadges, targetBadges);

        // Per §4.1.2 — STAB on either type for dual-type Pokémon.
        private static bool HasStab(PokemonSpeciesSO species, PokemonType moveType)
        {
            if (species == null || species.Types == null) return false;
            for (int i = 0; i < species.Types.Count; i++)
                if (species.Types[i] == moveType)
                    return true;
            return false;
        }
    }
}
