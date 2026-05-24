using System;

namespace ProjectAscendant.Core
{
    // Per §9.3.2 — core game type definitions used across PokemonInstance and related systems.
    // Full SO schemas and deeper enum expansions are authored in Epic 3.

    public enum PokemonType
    {
        Normal, Fire, Water, Grass, Electric, Ice, Fighting,
        Poison, Ground, Flying, Psychic, Bug, Rock, Ghost,
        Dragon, Dark, Steel, Fairy
    }

    public enum Stat { Attack, Defense, SpAttack, SpDefense, Speed }

    // Per §3.3.5.1 — StatusCondition.None is the non-affected state.
    // Confusion is SecondaryStatus only; all others are PrimaryStatus.
    public enum StatusCondition { None, Burn, Freeze, Paralysis, Poison, Sleep, Confusion }

    public enum EvolutionStage { Basic, Stage1, Stage2 }

    public enum MoveRole { Offensive, Defensive, Utility }
    public enum MoveRange { Melee, Ranged }
    public enum PositionalModifier { None, StepForward, StepBackward }
    public enum RarityTier { Common, Uncommon, Rare, Legendary }

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

    [Serializable]
    public struct EvolutionBranch
    {
        // TODO: Epic 3 — add branch archetype (Vanguard/Specialist/Support) and target species SO refs.
        public string BranchId;
    }
}
