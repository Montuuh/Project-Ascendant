using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per §4.3.7 + CL-024 — mid-fight evolution mechanic for Rival/Champion aces.
    // At Phase 2 (≤50% HP) the flagged ace evolves via EvolutionExecutor.Evolve,
    // carrying HP fraction across, swapping species/moveset, and logging the event.
    // One-shot per combat, never re-triggers, never happens for non-flagged bosses.
    public class MidFightEvolutionTests
    {
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.Divisor = 50;
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BossPhase2HPThreshold = 0.5f;
            _config.BossPhase3HPThreshold = 0.2f;
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // Per §4.3.7 — when an ace with MidFightEvolutionBranch crosses the Phase 2
        // threshold (≤50% HP) for the first time, it evolves via EvolutionExecutor.
        // HP fraction is preserved, species changes, moveset upgrades per the branch.
        [Test]
        public void MidFightEvolution_EvolvesOnceAtPhase2_HPFractionPreserved()
        {
            // Arrange: pre-evolved species (Wartortle) + evolved species (Blastoise).
            PokemonSpeciesSO wartortle = MakeSpecies("Wartortle", 80, 50, 50, PokemonType.Water);
            PokemonSpeciesSO blastoise = MakeSpecies("Blastoise", 120, 70, 70, PokemonType.Water);
            MoveSO waterGun = MakeMove("Water Gun", PokemonType.Water, 40, 1);
            MoveSO hydroPump = MakeMove("Hydro Pump", PokemonType.Water, 110, 3);

            // Branch: Wartortle → Blastoise, upgrading Water Gun → Hydro Pump.
            EvolutionBranchSO branch = ScriptableObject.CreateInstance<EvolutionBranchSO>();
            branch.EvolvedSpecies = blastoise;
            branch.MoveUpgrades = new List<MoveUpgradePair>
            {
                new MoveUpgradePair { OldMove = waterGun, NewMove = hydroPump }
            };
            branch.NewMoves = new List<MoveSO>();
            _disposables.Add(branch);

            // Enemy ace: Wartortle with 80 HP, starts at full, flagged for mid-fight evo.
            PokemonInstance enemy = new PokemonInstance
            {
                Species = wartortle,
                Level = 30,
                CurrentHP = 80,
                PhaseCount = 3,
                MidFightEvolutionBranch = branch,
                HasEvolvedMidFight = false,
                LastObservedPhase = 1
            };
            enemy.CurrentMoves.Add(waterGun);
            enemy.LearnedMoves.Add(waterGun);

            // Player team.
            PokemonSpeciesSO playerSp = MakeSpecies("Bulbasaur", 100, 60, 50, PokemonType.Grass);
            MoveSO tackle = MakeMove("Tackle", PokemonType.Normal, 40, 1);
            PokemonInstance player = new PokemonInstance
            {
                Species = playerSp,
                Level = 30,
                CurrentHP = 100
            };
            player.CurrentMoves.Add(tackle);

            List<PokemonInstance> playerTeam = new List<PokemonInstance> { player };
            List<PokemonInstance> enemyTeam = new List<PokemonInstance> { enemy };

            CombatController.CombatSetup setup = new CombatController.CombatSetup
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = 0,
                EnemyTeam = enemyTeam,
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = default,
                Config = _config,
                Reinforcements = null,
                Rng = new GameRNG(12345)
            };

            CombatController ctrl = new CombatController(setup, new TestCombatAgent());

            // Act: damage enemy to 50% HP exactly → should trigger evolution at IntentPhase.
            int preDamage = enemy.CurrentHP;
            enemy.CurrentHP = 40; // exactly 50% of 80
            Assert.AreEqual(wartortle, enemy.Species, "Pre-evo species should be Wartortle");

            ctrl.IntentPhase();

            // Assert: evolved to Blastoise, HP fraction preserved.
            Assert.AreEqual(blastoise, enemy.Species, "Should evolve to Blastoise at Phase 2");
            Assert.IsTrue(enemy.HasEvolvedMidFight, "HasEvolvedMidFight should be true");
            // HP fraction = 40/80 = 50%. New max = 120 → 50% = 60.
            int expectedHP = 60;
            Assert.AreEqual(expectedHP, enemy.CurrentHP, "HP should be 50% of new max (120)");
            Assert.AreEqual(2, enemy.LastObservedPhase, "LastObservedPhase should advance to 2");
            // Moveset upgrade: Water Gun → Hydro Pump.
            Assert.IsTrue(enemy.CurrentMoves.Contains(hydroPump), "Move should upgrade to Hydro Pump");
            Assert.IsFalse(enemy.CurrentMoves.Contains(waterGun), "Old move should be replaced");
        }

        // Per §4.3.7 — evolution does not trigger above 50% HP.
        [Test]
        public void MidFightEvolution_DoesNotEvolveAbove50Percent()
        {
            PokemonSpeciesSO wartortle = MakeSpecies("Wartortle", 80, 50, 50, PokemonType.Water);
            PokemonSpeciesSO blastoise = MakeSpecies("Blastoise", 120, 70, 70, PokemonType.Water);
            EvolutionBranchSO branch = ScriptableObject.CreateInstance<EvolutionBranchSO>();
            branch.EvolvedSpecies = blastoise;
            branch.MoveUpgrades = new List<MoveUpgradePair>();
            branch.NewMoves = new List<MoveSO>();
            _disposables.Add(branch);

            PokemonInstance enemy = new PokemonInstance
            {
                Species = wartortle,
                Level = 30,
                CurrentHP = 60, // 75% HP
                PhaseCount = 3,
                MidFightEvolutionBranch = branch,
                HasEvolvedMidFight = false,
                LastObservedPhase = 1
            };

            PokemonSpeciesSO playerSp = MakeSpecies("Bulbasaur", 100, 60, 50, PokemonType.Grass);
            PokemonInstance player = new PokemonInstance { Species = playerSp, Level = 30, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove("Tackle", PokemonType.Normal, 40, 1));

            CombatController.CombatSetup setup = new CombatController.CombatSetup
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = default,
                Config = _config,
                Reinforcements = null,
                Rng = new GameRNG(12346)
            };

            CombatController ctrl = new CombatController(setup, new TestCombatAgent());
            ctrl.IntentPhase();

            Assert.AreEqual(wartortle, enemy.Species, "Should NOT evolve above 50% HP");
            Assert.IsFalse(enemy.HasEvolvedMidFight, "HasEvolvedMidFight should remain false");
        }

        // Per §4.3.7 — evolution happens at most once per combat.
        [Test]
        public void MidFightEvolution_DoesNotReEvolve()
        {
            PokemonSpeciesSO wartortle = MakeSpecies("Wartortle", 80, 50, 50, PokemonType.Water);
            PokemonSpeciesSO blastoise = MakeSpecies("Blastoise", 120, 70, 70, PokemonType.Water);
            EvolutionBranchSO branch = ScriptableObject.CreateInstance<EvolutionBranchSO>();
            branch.EvolvedSpecies = blastoise;
            branch.MoveUpgrades = new List<MoveUpgradePair>();
            branch.NewMoves = new List<MoveSO>();
            _disposables.Add(branch);

            PokemonInstance enemy = new PokemonInstance
            {
                Species = wartortle,
                Level = 30,
                CurrentHP = 40, // 50% HP
                PhaseCount = 3,
                MidFightEvolutionBranch = branch,
                HasEvolvedMidFight = false,
                LastObservedPhase = 1
            };

            PokemonSpeciesSO playerSp = MakeSpecies("Bulbasaur", 100, 60, 50, PokemonType.Grass);
            PokemonInstance player = new PokemonInstance { Species = playerSp, Level = 30, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove("Tackle", PokemonType.Normal, 40, 1));

            CombatController.CombatSetup setup = new CombatController.CombatSetup
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = default,
                Config = _config,
                Reinforcements = null,
                Rng = new GameRNG(12347)
            };

            CombatController ctrl = new CombatController(setup, new TestCombatAgent());
            ctrl.IntentPhase();

            Assert.AreEqual(blastoise, enemy.Species, "Should evolve once");
            Assert.IsTrue(enemy.HasEvolvedMidFight);

            // Heal back above 50%, then drop again.
            enemy.CurrentHP = 80; // 66% of new 120 max
            ctrl.IntentPhase();
            Assert.AreEqual(blastoise, enemy.Species, "Should still be Blastoise");

            enemy.CurrentHP = 30; // below 50% again
            ctrl.IntentPhase();
            Assert.AreEqual(blastoise, enemy.Species, "Should NOT re-evolve");
        }

        // Per §4.3.7 — non-flagged bosses never evolve.
        [Test]
        public void MidFightEvolution_NonFlaggedBoss_NeverEvolves()
        {
            PokemonSpeciesSO wartortle = MakeSpecies("Wartortle", 80, 50, 50, PokemonType.Water);

            PokemonInstance enemy = new PokemonInstance
            {
                Species = wartortle,
                Level = 30,
                CurrentHP = 40, // 50% HP
                PhaseCount = 3,
                MidFightEvolutionBranch = null, // NO branch
                HasEvolvedMidFight = false,
                LastObservedPhase = 1
            };

            PokemonSpeciesSO playerSp = MakeSpecies("Bulbasaur", 100, 60, 50, PokemonType.Grass);
            PokemonInstance player = new PokemonInstance { Species = playerSp, Level = 30, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove("Tackle", PokemonType.Normal, 40, 1));

            CombatController.CombatSetup setup = new CombatController.CombatSetup
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = default,
                Config = _config,
                Reinforcements = null,
                Rng = new GameRNG(12348)
            };

            CombatController ctrl = new CombatController(setup, new TestCombatAgent());
            ctrl.IntentPhase();

            Assert.AreEqual(wartortle, enemy.Species, "Should NOT evolve without a branch");
            Assert.IsFalse(enemy.HasEvolvedMidFight);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private PokemonSpeciesSO MakeSpecies(string name, int hp, int atk, int def, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.name = name;
            s.DisplayName = name;
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private MoveSO MakeMove(string name, PokemonType type, int power, int apCost)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = name;
            m.DisplayName = name;
            m.Type = type;
            m.BasePower = power;
            m.APCost = apCost;
            m.Range = MoveRange.Melee;
            m.Modifier = PositionalModifier.None;
            m.Effects = new List<MoveEffectSO>();
            _disposables.Add(m);
            return m;
        }
    }

    // Minimal player agent for tests — always ends the turn.
    public class TestCombatAgent : IPlayerAgent
    {
        public int PickLeadReplacement(CombatController.CombatState s, System.Collections.Generic.IReadOnlyList<PokemonInstance> c) => -1;
        public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
    }
}
