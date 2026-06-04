using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.19 + §5.2 + §6.2 — global XP, Poké Dollar, and Trauma economy constants.
    // All balance values here — no inline literals in Progression or Combat scripts.
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Project Ascendant/Config/Economy Config")]
    public class EconomyConfigSO : ScriptableObject
    {
        [Header("XP Curves — §5.2")]
        [Tooltip("Cumulative XP required to reach each level. Index 0 = XP for Level 2, etc.")]
        public int[] LevelUpThresholds;

        [Tooltip("XP multiplier for wild encounter victories.")]
        public float WildXPMultiplier = 1.0f;

        [Tooltip("XP multiplier for trainer victories.")]
        public float TrainerXPMultiplier = 1.5f;

        [Tooltip("XP multiplier for elite trainer victories.")]
        public float EliteXPMultiplier = 2.0f;

        [Tooltip("XP multiplier for Gym Leader victories.")]
        public float GymLeaderXPMultiplier = 3.0f;

        [Header("Poké Dollars")]
        [Tooltip("Token cost per XP unit at the Trainer Hub shop. Post-VS.")]
        public float TokenPerXP = 1.0f;
        [Tooltip("§8.3.3 Coin Pouch relic — all Poké Dollar drops ×this multiplier.")]
        public float CoinPouchPokeDollarMultiplier = 1.25f;

        [Header("Trauma System — §6.2")]
        [Tooltip("Percentage damage penalty per Trauma stack. E.g. 5 = -5% per stack.")]
        public int TraumaStackPenaltyPercent = 5;

        [Tooltip("Maximum Trauma stacks a single Pokémon can accumulate.")]
        public int TraumaStackCap = 5;

        [Tooltip("Trauma stacks gained per faint in combat.")]
        public int TraumaStacksPerFaint = 1;

        [Header("Box Capacity")]
        // Per §2.3 — Box default capacity = 6, upgradable to 8 via relic/meta-unlock.
        [Tooltip("Base Box capacity. Per §2.3 — 6 by default; relic/meta raise it to 8 at runtime.")]
        public int BoxCapacity = 6;

        [Header("Pokéball Economy — §7.3.4 (Option 1: scarcity, playtest 2026-06-05)")]
        // Playtest override of §7.3.4.1 (which gave a free ball per encounter): Pokéballs are now a
        // scarce counted resource — a starting stock + a per-region grant + shop purchases, spent on
        // each catch attempt. GDD §7.3.4 to be updated to match.
        [Tooltip("Pokéballs the player starts a run with. Per §7.3.4 (Option 1).")]
        public int StartingPokeballs = 3;
        [Tooltip("Pokéballs granted on entering each region. Per §7.3.4 (Option 1).")]
        public int PokeballsPerRegion = 1;

        [Header("Pokémon Center — §7.6.1 / §6.2.4")]
        // Per §6.2.4 — Therapy removes 1 Trauma stack for TherapyBaseCost × (1 + stack count).
        [Tooltip("Base Trauma-therapy cost. Total = this × (1 + current stacks). Per §6.2.4 — 100.")]
        public int TherapyBaseCost = 100;

        [Header("Move Loadout — §5.10")]
        // Per §5.10 (approved 2026-06-02, pending Notion lock) — paid reconfigure cost per Pokémon
        // per Map View session. Free at Center and post-evolution; paid otherwise.
        [Tooltip("Poké Dollar cost per Pokémon for paid move reconfiguration from Map View. Per §5.10 — 50.")]
        public int MoveReconfigCost = 50;
    }
}
