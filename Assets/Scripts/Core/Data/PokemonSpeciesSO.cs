using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.1 — Definition SO for a Pokémon species. Immutable at runtime.
    // One SO per evolution stage per branch: e.g. Squirtle, Wartortle_Vanguard, Blastoise_VanguardA1.
    [CreateAssetMenu(fileName = "New Pokemon Species", menuName = "Project Ascendant/Pokemon/Species")]
    public class PokemonSpeciesSO : ScriptableObject
    {
        [Header("Identity")]
        public string SpeciesId;
        public string DisplayName;

        [Header("Type")]
        // Per §9.3.2.1 — 1-2 types.
        public List<PokemonType> Types;

        [Header("Stats")]
        public BaseStats BaseStats;
        public StatGrowthCurveSO GrowthCurve;

        [Header("Evolution")]
        // Per §5.3.3 — 2-4 branches (empty list if this is a final-form SO).
        public List<EvolutionBranchSO> Branches;

        // Per §5.2.2 / §5.3.1 — the level at which this species becomes evolution-eligible. 0 = no
        // level-gated evolution (final form, or single-stage). INTERIM per-species values seeded
        // pending systems-designer calibration (BACKLOG gap #41).
        public int EvolveLevel;

        [Header("Learnset")]
        // Per §9.3.2.1 — base-level moves. Pre-CL-006 this was the full 4-move kit; under the
        // §5.12.1 redesign base forms start with 2 and the rest are gated by LevelUpLearnset.
        // Retained as the legacy fallback (see KnownMovesAtLevel) until species are re-authored.
        public List<MoveSO> BaseLearnset;

        // Per §5.12.1 (CL-006) — level-gated learnset. A Pokémon knows every entry whose Level is
        // <= its current level. Empty = legacy behaviour (all BaseLearnset known), so existing
        // assets are unaffected until re-authored to the 2-start + learn-up curve.
        public List<LevelUpEntry> LevelUpLearnset;

        // Per §5.4.2 — moves available from Move Tutors.
        public List<MoveSO> TutorLearnset;

        // Per §5.4.1 — TM compatibility list.
        public List<TMSO> TMCompatibility;

        [Header("Ability & Mastery")]
        // Per §5.12.3 (CL-008) — the abilities this species can learn at a Dojo (§7.14).
        // One entry = one ability the player can pay to teach. Replaces the old auto-grant
        // model (§5.5.1 pre-CL-008). Null/empty = species has no Dojo abilities.
        public List<AbilitySO> AvailableAbilities;

        // LEGACY — was the auto-granted ability at first evolution (§5.5.1 pre-CL-008).
        // Nulled on all VS species by CL-007 / CL-008. Kept for serialisation compatibility
        // only; new species must use AvailableAbilities instead. Do not read at runtime.
        public AbilitySO PrimaryAbility;

        // Per §4.3.9.2 — this stage's Mastery tier card (Lv1/Lv2/Lv3 depending on evolution stage). Unlocked via meta-progression achievements. Cannot be replaced by TM/Tutor.
        public MoveSO MasteryMove;

        [Header("Wild Encounter")]
        public List<StatusCondition> StatusImmunities;
        public RarityTier WildRarity;
        public List<Biome> SpawnBiomes;

        [Header("Presentation")]
        public Sprite Portrait;

        [Tooltip("GDD section for this species. Per §9.15.")]
        public string GDDReference;

        // Per §5.12.1 (CL-006) — the moves this species knows at a given level: every
        // LevelUpLearnset entry with Level <= level, de-duplicated, order-preserved.
        // Legacy fallback: if no LevelUpLearnset is authored, returns BaseLearnset unchanged
        // (pre-CL-006 behaviour) so the redesign rolls out per-species without breaking others.
        // Pure read helper — no mutation; the active-4 cap (min(known,4)) is applied by the deck
        // builder, not here.
        public List<MoveSO> KnownMovesAtLevel(int level)
        {
            if (LevelUpLearnset == null || LevelUpLearnset.Count == 0)
                return BaseLearnset != null ? new List<MoveSO>(BaseLearnset) : new List<MoveSO>();

            List<MoveSO> known = new();
            foreach (LevelUpEntry entry in LevelUpLearnset)
            {
                if (entry.Move != null && entry.Level <= level && !known.Contains(entry.Move))
                    known.Add(entry.Move);
            }
            return known;
        }
    }

    // Per §5.12.1 (CL-006) — one move learned at a level threshold.
    [System.Serializable]
    public struct LevelUpEntry
    {
        public int Level;
        public MoveSO Move;
    }
}
