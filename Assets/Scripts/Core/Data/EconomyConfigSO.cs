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

        [Header("Pokémon Center — §7.6.1 / §6.2.4")]
        // Per §6.2.4 — Therapy removes 1 Trauma stack for TherapyBaseCost × (1 + stack count).
        [Tooltip("Base Trauma-therapy cost. Total = this × (1 + current stacks). Per §6.2.4 — 100.")]
        public int TherapyBaseCost = 100;
    }
}
