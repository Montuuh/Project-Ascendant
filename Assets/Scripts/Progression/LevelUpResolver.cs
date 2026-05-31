using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.2.1 + Epic 10 Task 10.1 — award per-Pokémon combat XP. Every Active-Team Pokémon that
    // participated earns the same tier-scaled amount (Box-only Pokémon earn nothing — team selection
    // has a progression consequence). Pure C#; the XP→level conversion is LevelUpResolver (below).
    // Distinct from meta Trainer XP (Topic 6, run-end).
    public static class XPAwarder
    {
        // Credits `xp` to every non-null member of the Active Team. Returns how many were credited.
        public static int Award(IReadOnlyList<PokemonInstance> activeTeam, int xp)
        {
            if (activeTeam == null || xp <= 0) return 0;
            int credited = 0;
            for (int i = 0; i < activeTeam.Count; i++)
            {
                PokemonInstance p = activeTeam[i];
                if (p == null) continue;
                p.CurrentXP += xp;
                credited++;
            }
            return credited;
        }

        // Convenience: resolve the tier XP from the cleared node's type, then award it.
        public static int AwardForNode(IReadOnlyList<PokemonInstance> activeTeam, NodeType node, ProgressionConfigSO cfg)
            => cfg == null ? 0 : Award(activeTeam, cfg.XPForNode(node));
    }

    // Per §5.2.2 / §5.2.3 + Epic 10 Task 10.1 — convert accumulated CurrentXP into levels BETWEEN
    // nodes (never mid-combat). Stat growth is automatic: MaxHP / Attack / Defense derive from the
    // species StatGrowthCurveSO at the current level, so incrementing Level raises them; the per-level
    // HP gain is also credited to CurrentHP (a level-up heals by its HP growth) — but never revives a
    // fainted Pokémon (§2.4.1: 0 HP stays fainted until a Center). Pure C#.
    public static class LevelUpResolver
    {
        public struct Result
        {
            public int LevelsGained;
            public int NewLevel;
            public int HPGained;
            public bool EvolutionUnlocked; // crossed Species.EvolveLevel this pass (§5.3.1)
        }

        // Apply all pending level-ups for one Pokémon. Idempotent once CurrentXP < the next threshold.
        public static Result Process(PokemonInstance p, ProgressionConfigSO cfg)
        {
            Result r = default;
            if (p == null || p.Species == null || cfg == null)
            {
                if (p != null) r.NewLevel = p.Level;
                return r;
            }

            bool wasEligible = IsEvolutionEligible(p);
            int guard = 0;
            while (guard++ < 500)
            {
                int cost = cfg.XPToNext(p.Level);
                if (cost <= 0 || p.CurrentXP < cost) break;

                int before = PokemonVitals.MaxHP(p);
                p.CurrentXP -= cost;
                p.Level++;
                int delta = PokemonVitals.MaxHP(p) - before;
                // Heal by the HP growth — but never revive a fainted Pokémon.
                if (delta > 0 && p.CurrentHP > 0) { p.CurrentHP += delta; r.HPGained += delta; }
                r.LevelsGained++;
            }

            r.NewLevel = p.Level;
            r.EvolutionUnlocked = !wasEligible && IsEvolutionEligible(p);
            return r;
        }

        // §5.3.1 — eligible iff the species has evolution branches and the Pokémon reached EvolveLevel.
        public static bool IsEvolutionEligible(PokemonInstance p)
        {
            if (p?.Species == null) return false;
            if (p.Species.Branches == null || p.Species.Branches.Count == 0) return false;
            return p.Species.EvolveLevel > 0 && p.Level >= p.Species.EvolveLevel;
        }
    }
}
