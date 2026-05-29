using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §4.4.3 / §4.4.4.3 + Epic 8 Task 8.5 — boss phase-transition director
    // and Phase-3 mechanics, driven through CombatController.
    //   • Bucket 1: mid-fight evolution @ Phase 2 (ace, once, HP-fraction kept)
    //   • Bucket 2: Phase-3 cooldown reset
    //   • Bucket 3: Sturdy last-stand (survive first lethal hit at 1 HP, once)
    public class GymBossPhaseTests
    {
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.Divisor = 10;             // visible damage at L1
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.StatStageMultipliers = new float[]
            { 0.25f,0.29f,0.33f,0.40f,0.50f,0.67f,1.00f,1.50f,2.00f,2.50f,3.00f,3.50f,4.00f };
            _config.BaseAPPerTurn = 6;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 6;
            _config.BaseConsumableCardsPerTurn = 0;
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.AggressiveSelfMultiplier = 1.5f;
            _config.LowSelfHPThreshold = 0.40f;
            _config.SetupSelfMultiplier = 1.5f;
            _config.HighSelfHPThreshold = 0.70f;
            _config.RandomnessFloorChance = 0f;
            _config.BossPhase2HPThreshold = 0.5f;
            _config.BossPhase3HPThreshold = 0.2f;
            _config.BossPhaseAggressionMultiplier = 1.5f;
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private MoveSO Mk(int power, MoveRange range = MoveRange.Melee)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "mv" + power;
            m.Type = PokemonType.Normal;
            m.BasePower = power;
            m.APCost = 1;
            m.Role = MoveRole.Offensive;
            m.Range = range;
            m.Modifier = PositionalModifier.None;
            m.RangeModifierMultiplier = range == MoveRange.Ranged ? 0.75f : 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonSpeciesSO Species(int hp, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "sp" + hp;
            s.Types = new List<PokemonType>(types.Length == 0 ? new[] { PokemonType.Normal } : types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 40, BaseDef = 40, BaseSpd = 40 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        private CombatController BuildWithEnemy(PokemonInstance enemy, PokemonInstance lead)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(0x5151u),
            };
            CombatController c = new(setup, new PassiveAgent());
            c.Start();
            return c;
        }

        // ── Bucket 1: mid-fight evolution @ Phase 2 ──────────────────────────

        [Test]
        public void IntentPhase_AceCrossesFiftyPercent_EvolvesAndPreservesHPFraction()
        {
            // Per §4.4.4.3 — ace evolves into its target at 50% HP, as a power
            // spike: HP fraction is preserved across the (larger) new max.
            PokemonSpeciesSO baseSp = Species(100);   // max 100
            PokemonSpeciesSO evolved = Species(200);  // max 200
            PokemonInstance ace = new()
            {
                Species = baseSp, Level = 1, CurrentHP = 50, // 50% → Phase 2
                PhaseCount = 3, MidFightEvolutionTarget = evolved,
            };
            ace.CurrentMoves.Add(Mk(40));
            PokemonInstance lead = new() { Species = Species(100), Level = 1, CurrentHP = 100 };

            CombatController c = BuildWithEnemy(ace, lead);
            c.IntentPhase();

            Assert.That(ace.Species, Is.SameAs(evolved), "Ace must evolve at Phase 2.");
            Assert.That(ace.HasEvolvedMidFight, Is.True);
            Assert.That(ace.CurrentHP, Is.EqualTo(100), "50% of new 200 max = 100 (fraction kept).");
            Assert.That(ace.LastObservedPhase, Is.EqualTo(2));
        }

        [Test]
        public void IntentPhase_Evolution_FiresOnlyOnce()
        {
            PokemonSpeciesSO baseSp = Species(100);
            PokemonSpeciesSO evolved = Species(200);
            PokemonInstance ace = new()
            {
                Species = baseSp, Level = 1, CurrentHP = 50,
                PhaseCount = 3, MidFightEvolutionTarget = evolved,
            };
            ace.CurrentMoves.Add(Mk(40));
            CombatController c = BuildWithEnemy(ace, new() { Species = Species(100), Level = 1, CurrentHP = 100 });

            c.IntentPhase();
            ace.Species = baseSp;          // pretend a later effect reverted species
            ace.CurrentHP = 40;            // still ≤50%
            c.IntentPhase();               // must NOT evolve again
            Assert.That(ace.Species, Is.SameAs(baseSp), "Evolution is one-shot (HasEvolvedMidFight).");
        }

        [Test]
        public void IntentPhase_AboveFiftyPercent_DoesNotEvolve()
        {
            PokemonSpeciesSO baseSp = Species(100);
            PokemonInstance ace = new()
            {
                Species = baseSp, Level = 1, CurrentHP = 80, // 80% → Phase 1
                PhaseCount = 3, MidFightEvolutionTarget = Species(200),
            };
            ace.CurrentMoves.Add(Mk(40));
            CombatController c = BuildWithEnemy(ace, new() { Species = Species(100), Level = 1, CurrentHP = 100 });
            c.IntentPhase();
            Assert.That(ace.Species, Is.SameAs(baseSp));
            Assert.That(ace.HasEvolvedMidFight, Is.False);
        }

        // ── Bucket 2: Phase-3 cooldown reset ─────────────────────────────────

        [Test]
        public void IntentPhase_AceEntersPhaseThree_ResetsCooldowns()
        {
            // Per §4.4.3 Phase 3 — cooldowns reset so the signature fires.
            MoveSO sig = Mk(80);
            sig.CooldownTurns = 3;
            PokemonInstance ace = new()
            {
                Species = Species(100), Level = 1, CurrentHP = 20, // 20% → Phase 3
                PhaseCount = 3, // no evolution target → isolate cooldown reset
            };
            ace.CurrentMoves.Add(sig);
            ace.SetMoveCooldown(sig, 3);
            Assert.That(ace.IsMoveOnCooldown(sig), Is.True, "Precondition: on cooldown.");

            CombatController c = BuildWithEnemy(ace, new() { Species = Species(100), Level = 1, CurrentHP = 100 });
            c.IntentPhase();

            Assert.That(ace.IsMoveOnCooldown(sig), Is.False, "Phase 3 resets cooldowns.");
            Assert.That(ace.LastObservedPhase, Is.EqualTo(3));
        }

        [Test]
        public void IntentPhase_SinglePhaseEnemy_NoTransitionEffects()
        {
            // Regression: ordinary enemy (PhaseCount 1) never evolves or resets.
            MoveSO sig = Mk(80); sig.CooldownTurns = 2;
            PokemonInstance e = new()
            {
                Species = Species(100), Level = 1, CurrentHP = 10, // 10%
                PhaseCount = 1, MidFightEvolutionTarget = Species(200),
            };
            e.CurrentMoves.Add(sig);
            e.SetMoveCooldown(sig, 2);
            CombatController c = BuildWithEnemy(e, new() { Species = Species(100), Level = 1, CurrentHP = 100 });
            c.IntentPhase();
            Assert.That(e.HasEvolvedMidFight, Is.False);
            Assert.That(e.IsMoveOnCooldown(sig), Is.True, "Single-phase enemy keeps cooldowns.");
        }

        // ── Bucket 3: Sturdy last-stand (real player attack) ─────────────────

        private static int FindHandIndex(CombatController c, MoveSO move)
        {
            for (int i = 0; i < c.State.SkillHand.Count; i++)
                if (c.State.SkillHand[i] != null && c.State.SkillHand[i].Move == move) return i;
            return -1;
        }

        [Test]
        public void Sturdy_FirstLethalHit_SurvivesAtOneHP_ThenConsumed()
        {
            // Per §4.4.3 Phase 3 — ace survives the first lethal hit at 1 HP.
            MoveSO strong = Mk(100); // lethal vs a 5-HP enemy at Divisor 10
            PokemonInstance lead = new() { Species = Species(100), Level = 1, CurrentHP = 100 };
            lead.CurrentMoves.Add(strong);
            lead.CurrentMoves.Add(strong); // two cards → two hits this turn
            PokemonInstance ace = new()
            {
                Species = Species(100), Level = 1, CurrentHP = 5,
                PhaseCount = 3, HasSturdy = true,
            };
            ace.CurrentMoves.Add(Mk(10));

            CombatController c = BuildWithEnemy(ace, lead);
            c.DrawPhase();
            c.IntentPhase();

            int idx = FindHandIndex(c, strong);
            c.ExecuteAction(PlayerAction.PlaySkill(idx, enemySlot: 0));
            Assert.That(ace.CurrentHP, Is.EqualTo(1), "Sturdy: survive first lethal hit at 1 HP.");
            Assert.That(ace.SturdyConsumed, Is.True);

            // Second lethal hit — Sturdy already spent → faints.
            int idx2 = FindHandIndex(c, strong);
            c.ExecuteAction(PlayerAction.PlaySkill(idx2, enemySlot: 0));
            Assert.That(ace.CurrentHP, Is.EqualTo(0), "Sturdy is once per combat.");
        }

        [Test]
        public void Sturdy_AtOneHP_DoesNotProtect()
        {
            // A boss already at 1 HP is not saved (only >1 → 1 is protected).
            MoveSO strong = Mk(100);
            PokemonInstance lead = new() { Species = Species(100), Level = 1, CurrentHP = 100 };
            lead.CurrentMoves.Add(strong);
            PokemonInstance ace = new()
            {
                Species = Species(100), Level = 1, CurrentHP = 1,
                PhaseCount = 3, HasSturdy = true,
            };
            ace.CurrentMoves.Add(Mk(10));
            CombatController c = BuildWithEnemy(ace, lead);
            c.DrawPhase();
            c.IntentPhase();
            c.ExecuteAction(PlayerAction.PlaySkill(FindHandIndex(c, strong), enemySlot: 0));
            Assert.That(ace.CurrentHP, Is.EqualTo(0));
        }
    }
}
