using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.8.3.1 (CL-016) — the 16-modifier launch pool, built in code (mirrors
    // RunBootstrapper.BuildDifficultyChoices). The run layer offers 3-of-pool → pick 1 at each Region
    // start (pre-R1 + City 1 + City 2). All numbers are systems-designer-tunable placeholders.
    public static class RegionModifierPool
    {
        public static List<RegionModifierSO> BuildAll()
        {
            return new List<RegionModifierSO>
            {
                // ── Strong ──
                Rm("hand_of_plenty", "Hand of Plenty", RegionModifierKind.HandOfPlenty, 1f,
                   RegionModifierTier.Strong, "+1 max hand size this Region."),
                Rm("sturdy_lead", "Sturdy Lead", RegionModifierKind.SturdyLead, 0f,
                   RegionModifierTier.Strong, "Your Lead survives one lethal hit at 1 HP (once per combat)."),
                Rm("type_affinity", "Type Affinity", RegionModifierKind.TypeAffinity, 0.10f,
                   RegionModifierTier.Strong, "A chosen type deals +10% damage this Region."),
                Rm("trauma_resistance", "Trauma Resistance", RegionModifierKind.TraumaResistance, 1f,
                   RegionModifierTier.Strong, "Trauma stacks bite 1 point less per stack this Region."),

                // ── Medium ──
                Rm("swap_fuel", "Swap Fuel", RegionModifierKind.SwapFuel, 5f,
                   RegionModifierTier.Medium, "Your Lead heals 5 HP each manual swap."),
                Rm("lucky_draw", "Lucky Draw", RegionModifierKind.LuckyDraw, 1f,
                   RegionModifierTier.Medium, "Draw +1 consumable card on turn 1 of each combat."),
                Rm("status_mastery", "Status Mastery", RegionModifierKind.StatusMastery, 1f,
                   RegionModifierTier.Medium, "Status conditions you apply last +1 turn."),
                Rm("pocket_healer", "Pocket Healer", RegionModifierKind.PocketHealer, 0.05f,
                   RegionModifierTier.Medium, "Heal the team +5% on each node's first combat victory."),
                Rm("coin_purse", "Coin Purse", RegionModifierKind.CoinPurse, 0.5f,
                   RegionModifierTier.Medium, "Poké Dollar drops ×1.5 this Region."),
                Rm("glass_cannon", "Glass Cannon", RegionModifierKind.GlassCannon, 0.20f,
                   RegionModifierTier.Medium, "+20% damage dealt AND +20% damage taken this Region."),
                Rm("quick_study", "Quick Study", RegionModifierKind.QuickStudy, 0.15f,
                   RegionModifierTier.Medium, "All Pokémon gain +15% combat XP this Region."),
                Rm("bargain_hunter", "Bargain Hunter", RegionModifierKind.BargainHunter, 0.20f,
                   RegionModifierTier.Medium, "Shop and Dojo prices −20% this Region."),

                // ── Niche ──
                Rm("iron_skin", "Iron Skin", RegionModifierKind.IronSkin, 1f,
                   RegionModifierTier.Niche, "All Pokémon take −1 damage from Cleave intents."),
                Rm("mass_mobilization", "Mass Mobilization", RegionModifierKind.MassMobilization, 0f,
                   RegionModifierTier.Niche, "Step-Forward and Step-Backward also draw 1 card."),
                Rm("pokedex_whisper", "Pokédex Whisper", RegionModifierKind.PokedexWhisper, 0f,
                   RegionModifierTier.Niche, "The first Unknown intent of each combat is revealed."),
                Rm("field_surveyor", "Field Surveyor", RegionModifierKind.FieldSurveyor, 0f,
                   RegionModifierTier.Niche, "You choose the active Battlefield at the start of each wild/Region combat."),
            };
        }

        // Per §7.8.3.1 (CL-016) — a seeded "3 offered" pick from the pool at a Region start. Returns
        // `count` distinct modifiers (or the whole pool if smaller) via a partial Fisher–Yates shuffle.
        // Deterministic for a given RNG cursor (Engineering Pillar 3). Null rng → the first `count`.
        public static List<RegionModifierSO> BuildOffer(IReadOnlyList<RegionModifierSO> pool, GameRNG rng, int count)
        {
            List<RegionModifierSO> offer = new();
            if (pool == null || pool.Count == 0 || count <= 0) return offer;
            List<RegionModifierSO> src = new(pool);
            int n = count < src.Count ? count : src.Count;
            for (int i = 0; i < n; i++)
            {
                int j = rng != null ? rng.Range(i, src.Count) : i;
                (src[i], src[j]) = (src[j], src[i]);
                offer.Add(src[i]);
            }
            return offer;
        }

        private static RegionModifierSO Rm(string id, string name, RegionModifierKind kind, float magnitude,
                                           RegionModifierTier tier, string desc)
        {
            RegionModifierSO m = ScriptableObject.CreateInstance<RegionModifierSO>();
            m.ModifierId = id;
            m.DisplayName = name;
            m.Kind = kind;
            m.Magnitude = magnitude;
            m.Tier = tier;
            m.EffectDescription = desc;
            m.name = id;
            return m;
        }
    }
}
