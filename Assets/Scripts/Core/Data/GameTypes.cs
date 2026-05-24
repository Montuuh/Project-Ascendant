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

    public enum Stat { Attack, Defense, SpAttack, SpDefense, Speed }

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

    // Per §5.5.2 — six passive ability categories.
    public enum AbilityCategory { Combat, Vision, Positional, Type, Survival, Aura }

    // Per §3.1.20 / §9.5 — node types for map generation weights.
    public enum NodeType { Wild, Trainer, Center, Shop, Mystery, Gym }

    [Serializable]
    public struct BaseStats
    {
        public int BaseHP;
        public int BaseAtk;
        public int BaseDef;
        public int BaseSpAtk;
        public int BaseSpDef;
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
