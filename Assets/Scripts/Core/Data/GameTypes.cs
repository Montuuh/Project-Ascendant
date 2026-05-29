using System;

namespace ProjectAscendant.Core
{
    // Per §9.3.2 — core game type definitions shared across all game systems.

    public enum PokemonType
    {
        Normal, Fire, Water, Grass, Electric, Ice, Fighting,
        Poison, Ground, Flying, Psychic, Bug, Rock, Ghost,
        Dragon, Dark, Steel, Fairy
    }

    // Per §4.1.1 — the game uses a simplified 3-stat model (no SpAtk/SpDef split).
    // All damage is Attack vs Defense regardless of move range.
    public enum Stat { Attack, Defense, Speed }

    // Per §3.3.5.1 — None is the non-affected state.
    // Confusion is SecondaryStatus only; all others are PrimaryStatus.
    public enum StatusCondition { None, Burn, Freeze, Paralysis, Poison, Sleep, Confusion }

    public enum EvolutionStage { Basic, Stage1, Stage2 }

    public enum MoveRole { Offensive, Defensive, Utility }
    public enum MoveRange { Melee, Ranged }
    public enum PositionalModifier { None, StepForward, StepBackward }
    public enum RarityTier { Common, Uncommon, Rare, Legendary }

    // Per §5.3.4 — three branch archetypes that guide move-kit design.
    public enum BranchArchetype { Vanguard, Specialist, Support }

    // Per §3.1.13 / §9.3.2.1 — biomes used in species spawn pools and map generation.
    public enum Biome { None, Meadow, Cave, River, Forest, Mountain, Shoreline }

    // Per §8.3.2 — five synergy categories for relic curation (City Shop algorithm).
    public enum SynergyCategory { LeadEconomy, CardEconomy, Combat, MetaAcquisition, Status }

    // Per §5.5.2 + §5.8 — ability categories. §5.8 added Support for team-sustain passives
    // (Healer, Friend Guard). §5.5.2 table predates that addition; consider it authoritative.
    public enum AbilityCategory { Combat, Vision, Positional, Type, Survival, Aura, Support }

    // Per §3.1.20 / §9.5 — node types for map generation weights.
    // Per §7.2.1 — Elite is a distinct map node (always Layer 3). Appended last to keep
    // the serialized int values of Wild..Gym (0..5) stable for existing assets.
    public enum NodeType { Wild, Trainer, Center, Shop, Mystery, Gym, Elite }

    // Per §7.9.3 + Epic 9 Task 9.7.5 — Mystery Event risk badge (🟢 Safe / 🟡 Tradeoff / 🔴 Gamble).
    public enum MysteryRiskProfile { Safe, Tradeoff, Gamble }

    // Per §7.2 / §9.5 + Epic 9 Task 9.2 — terminal result of a resolved map node.
    // Maps to the HSM transition the driving NodeState issues:
    //   Cleared    → NodeComplete (→ MapView)
    //   RunEnded   → RunEnded     (→ RunEndState; Gym victory ends the VS run)
    //   PlayerWiped→ GameOver     (→ GameOverState; §3.3.6 run-failure)
    public enum NodeOutcome { Cleared, PlayerWiped, RunEnded }

    // Per §4.1.1 — simplified stat block: HP, Attack, Defense, Speed only.
    [Serializable]
    public struct BaseStats
    {
        public int BaseHP;
        public int BaseAtk;
        public int BaseDef;
        public int BaseSpd;
    }

    // Per §9.3.2.6 — lightweight branch descriptor kept on PokemonInstance for quick reads.
    // The full branch data lives in EvolutionBranchSO.
    [Serializable]
    public struct EvolutionBranch
    {
        public string BranchId;
        public BranchArchetype Archetype;
    }

    // Per §9.3.2.4 — serializable key-value pair for RunStateSO.EventFlags.
    // Unity cannot serialize Dictionary<string,int> directly.
    [Serializable]
    public struct StringIntPair
    {
        public string Key;
        public int Value;
    }
}
