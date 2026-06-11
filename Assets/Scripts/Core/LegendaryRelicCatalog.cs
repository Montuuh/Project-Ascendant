using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.3.7 (CL-021 — Q10) — the 10 Legendary relics, built in code (mirrors RegionModifierPool /
    // AchievementCatalog: content lives here until authored into .asset files). Legendary is a 4th rarity
    // class above Rare, NEVER in the random drop pool — these are choice-only (the 1-of-3 Legendary picks
    // at Gym victories / Victory Road / Black Market, max 2/run — LegendaryPickService).
    //
    // The 6 ported Boons are retuned ~⅔ for permanent run-long scope + 4 new, spread across all 5 synergy
    // categories. VS scope: this builds the relic DATA (id / name / description / category / rarity). The
    // EFFECT hooks (ScriptableHook bindings) are the follow-up — most compose existing relic/field/status
    // systems but need wiring like any relic; flagged with the relic-tier UI + League (both post-VS, CL-004).
    public static class LegendaryRelicCatalog
    {
        public static List<RelicSO> BuildAll()
        {
            return new List<RelicSO>
            {
                // ── 6 ported Boons (retuned ~⅔) ──
                R("battle_hardened", "Battle Hardened",
                  "Each Active Team Pokémon starts every combat with a Shield = 10% of its max HP.", SynergyCategory.Combat),
                R("flow_state", "Flow State",
                  "The first manual swap each combat costs 0 AP.", SynergyCategory.LeadEconomy),
                R("last_stand", "Last Stand",
                  "Once per combat, the first Active Team Pokémon that would faint survives at 1 HP instead.", SynergyCategory.Combat),
                R("type_mastery", "Type Mastery",
                  "Super-Effective moves deal +0.15× bonus damage.", SynergyCategory.Combat),
                R("clear_mind", "Clear Mind",
                  "All Unknown enemy intents are revealed for every combat.", SynergyCategory.Combat),
                R("evolutions_edge", "Evolution's Edge",
                  "Fully-evolved Pokémon deal +10% damage.", SynergyCategory.Combat),

                // ── 4 new ──
                R("grandmasters_tempo", "Grandmaster's Tempo",
                  "+1 max hand size; the first skill card each turn costs 0 AP.", SynergyCategory.CardEconomy),
                R("living_legend", "Living Legend",
                  "All in-run XP ×1.3; recruited wild Pokémon enter at +2 levels with 0 Trauma.", SynergyCategory.MetaAcquisition),
                R("unbreakable_will", "Unbreakable Will",
                  "Immune to the first status condition each combat; status conditions you apply last +1 turn.", SynergyCategory.Status),
                R("apex_predator", "Apex Predator",
                  "While the Lead is at full HP its moves deal +20% damage; taking any damage disables this until the Lead is healed back to full.", SynergyCategory.Combat),
            };
        }

        private static RelicSO R(string id, string name, string desc, SynergyCategory category)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>();
            r.Id = id;
            r.DisplayName = name;
            r.EffectDescription = desc;
            r.Rarity = RarityTier.Legendary;
            r.MetaTier = 1; // §6.6.1 — available from run 1 (rarity class ≠ meta-tier)
            r.Categories = new List<SynergyCategory> { category };
            r.GDDReference = "§8.3.7";
            r.name = id;
            return r;
        }
    }
}
