using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.5.2 (CL-024) — Elite Wild boss-wild data asset.
    // A catchable boss Pokémon with a catch-vs-kill dilemma: catch → recruit, defeat → single Rare relic.
    // Profile: boss-tier HP, 2-phase escalation, NO evolution (it's wild, not a trainer ace).
    // Seeded placement: ≤1 per Region, not guaranteed (modelled on Apex node §4.5.1.2).
    [CreateAssetMenu(fileName = "EliteWild_", menuName = "Project Ascendant/Elite Wild", order = 160)]
    public class EliteWildSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID for this Elite Wild (e.g., 'SNORLAX_R1', 'MAROWAK_SPIRIT_R1').")]
        public string EliteWildId;

        [Tooltip("Display name (e.g., 'Snorlax', 'Marowak's Spirit').")]
        public string DisplayName;

        [Header("Pokémon Profile")]
        [Tooltip("The wild species this boss represents.")]
        public PokemonSpeciesSO Species;

        [Tooltip("Level for this boss encounter.")]
        public int Level = 15;

        [Tooltip("Phase count (fixed at 2 per §7.5.2 — boss-tier defensive escalation but no evolution).")]
        public int PhaseCount = 2;

        [Header("Rewards — Catch vs Kill (§7.5.2)")]
        [Tooltip("Trainer XP awarded on catch (full combat XP per CL-003/CL-004).")]
        public int CatchRewardXP = 25;

        [Tooltip("Single Rare relic awarded on defeat (HP≤0, not caught). No choice.")]
        public RelicSO DefeatRelic;

        [Tooltip("Poké Dollar reward on defeat.")]
        public int PokeDollarReward = 100;

        [Header("GDD Reference")]
        [Tooltip("§7.5.2 / §7.12 — Elite Wild node reward table.")]
        public string GDDReference = "§7.5.2";
    }
}
