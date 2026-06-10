using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.8.3.1 (CL-016) — query API for the active Region Modifier, mirroring RelicResolver.
    // Each system asks the resolver for its effect; the resolver reads Kind + Magnitude off the single
    // active modifier (RunStateSO.ActiveRegionModifiers holds 0..1 per CL-016's per-Region rule).
    // Effects with no active modifier of the relevant kind return the neutral value (0 / 1.0 / false).
    public static class RegionModifierResolver
    {
        public static RegionModifierSO Find(IReadOnlyList<RegionModifierSO> active, RegionModifierKind kind)
        {
            if (active == null) return null;
            for (int i = 0; i < active.Count; i++)
                if (active[i] != null && active[i].Kind == kind) return active[i];
            return null;
        }

        public static bool Has(IReadOnlyList<RegionModifierSO> active, RegionModifierKind kind)
            => Find(active, kind) != null;

        private static float Mag(IReadOnlyList<RegionModifierSO> active, RegionModifierKind kind, float fallback)
        {
            RegionModifierSO m = Find(active, kind);
            return m != null ? m.Magnitude : fallback;
        }

        // HandOfPlenty — extra max skill-hand cards. 0 if absent.
        public static int HandSizeBonus(IReadOnlyList<RegionModifierSO> active)
            => Mathf.RoundToInt(Mag(active, RegionModifierKind.HandOfPlenty, 0f));

        // QuickStudy — combat-XP multiplier. 1.0 if absent.
        public static float XpMultiplier(IReadOnlyList<RegionModifierSO> active)
            => 1f + Mag(active, RegionModifierKind.QuickStudy, 0f);

        // BargainHunter — Shop/Dojo price multiplier. 1.0 if absent.
        public static float ShopPriceMultiplier(IReadOnlyList<RegionModifierSO> active)
            => 1f - Mag(active, RegionModifierKind.BargainHunter, 0f);

        // GlassCannon — symmetric damage band (dealt AND taken). 1.0 if absent.
        public static float DamageDealtMultiplier(IReadOnlyList<RegionModifierSO> active)
            => 1f + Mag(active, RegionModifierKind.GlassCannon, 0f);

        public static float DamageTakenMultiplier(IReadOnlyList<RegionModifierSO> active)
            => 1f + Mag(active, RegionModifierKind.GlassCannon, 0f);

        // CoinPurse — Poké Dollar drop multiplier. 1.0 if absent.
        public static float CoinMultiplier(IReadOnlyList<RegionModifierSO> active)
            => 1f + Mag(active, RegionModifierKind.CoinPurse, 0f);

        // SwapFuel — HP the Lead heals per manual swap. 0 if absent.
        public static int SwapHealAmount(IReadOnlyList<RegionModifierSO> active)
            => Mathf.RoundToInt(Mag(active, RegionModifierKind.SwapFuel, 0f));

        // StatusMastery — extra turns on player-applied status conditions. 0 if absent.
        public static int StatusDurationBonus(IReadOnlyList<RegionModifierSO> active)
            => Mathf.RoundToInt(Mag(active, RegionModifierKind.StatusMastery, 0f));

        // LuckyDraw — extra consumable cards drawn on turn 1. 0 if absent.
        public static int Turn1ConsumableBonus(IReadOnlyList<RegionModifierSO> active)
            => Has(active, RegionModifierKind.LuckyDraw)
                ? Mathf.Max(1, Mathf.RoundToInt(Mag(active, RegionModifierKind.LuckyDraw, 1f))) : 0;

        // IronSkin — flat damage reduction from Cleave intents. 0 if absent.
        public static int CleaveDamageReduction(IReadOnlyList<RegionModifierSO> active)
            => Has(active, RegionModifierKind.IronSkin)
                ? Mathf.Max(1, Mathf.RoundToInt(Mag(active, RegionModifierKind.IronSkin, 1f))) : 0;

        // PocketHealer — fraction (0..1) of EffectiveMaxHP healed to the team on a node's first victory.
        public static float PocketHealerFraction(IReadOnlyList<RegionModifierSO> active)
            => Mag(active, RegionModifierKind.PocketHealer, 0f);

        // TraumaResistance — points shaved off each Trauma per-stack penalty (e.g. 1 → 5%→4% / 10%→9%).
        // 0 if absent (no override).
        public static int TraumaPenaltyReduction(IReadOnlyList<RegionModifierSO> active)
            => Mathf.RoundToInt(Mag(active, RegionModifierKind.TraumaResistance, 0f));

        // SturdyLead — the Lead survives one lethal hit at 1 HP per combat.
        public static bool GrantsSturdyLead(IReadOnlyList<RegionModifierSO> active)
            => Has(active, RegionModifierKind.SturdyLead);

        // MassMobilization — Step-Forward / Step-Backward also draw a card.
        public static bool StepDrawsCard(IReadOnlyList<RegionModifierSO> active)
            => Has(active, RegionModifierKind.MassMobilization);

        // PokedexWhisper — the first Unknown intent of each combat is revealed.
        public static bool RevealsFirstUnknown(IReadOnlyList<RegionModifierSO> active)
            => Has(active, RegionModifierKind.PokedexWhisper);

        // FieldSurveyor — the player chooses the active neutral Battlefield each wild/Region combat.
        public static bool GrantsFieldChoice(IReadOnlyList<RegionModifierSO> active)
            => Has(active, RegionModifierKind.FieldSurveyor);

        // TypeAffinity — the chosen type gets a damage bonus. Returns 0 if absent; the chosen type
        // itself is run-state (player-picked when the modifier is taken).
        public static float TypeAffinityBonus(IReadOnlyList<RegionModifierSO> active)
            => Mag(active, RegionModifierKind.TypeAffinity, 0f);
    }
}
