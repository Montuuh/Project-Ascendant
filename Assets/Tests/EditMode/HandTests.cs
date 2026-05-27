using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.4 + §3.5 + §3.7 + Epic 5 Task 5.2 — Hand structure, hand-size
    // calculator (Task 5.2.3), and deterministic-draw integration
    // (Task 5.2.4 — identical seed → identical hand sequence).
    public class HandTests
    {
        private readonly List<Object> _disposables = new();
        private PokemonSpeciesSO _species;
        private BattleConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            _species.Types = new List<PokemonType> { PokemonType.Normal };
            _species.BaseStats = new BaseStats { BaseHP = 60, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            _species.GrowthCurve = null;
            _species.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(_species);

            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 5;  // per §3.7 — VS spec is 5+2
            _config.BaseConsumableCardsPerTurn = 2;
            _config.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        private MoveSO Mk(string id)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = id;
            m.Type = PokemonType.Normal;
            m.BasePower = 40;
            m.APCost = 1;
            m.Role = MoveRole.Offensive;
            m.Range = MoveRange.Ranged;
            m.Modifier = PositionalModifier.None;
            _disposables.Add(m);
            return m;
        }

        // ── 5.2.1 — Hand class structure ─────────────────────────────────────

        [Test]
        public void Hand_New_BothCompartmentsEmpty()
        {
            Hand h = new();
            Assert.That(h.SkillCount, Is.EqualTo(0));
            Assert.That(h.ConsumableCount, Is.EqualTo(0));
            Assert.That(h.Skill, Is.Not.Null);
            Assert.That(h.Consumables, Is.Not.Null);
        }

        [Test]
        public void Hand_AddSkillAndConsumable_CountsAccurate()
        {
            Hand h = new();
            h.Skill.Add(new MoveCardInstance { Move = Mk("m1") });
            h.Skill.Add(new MoveCardInstance { Move = Mk("m2") });
            h.Consumables.Add(ScriptableObject.CreateInstance<ConsumableSO>());
            _disposables.Add(h.Consumables[0]);
            Assert.That(h.SkillCount, Is.EqualTo(2));
            Assert.That(h.ConsumableCount, Is.EqualTo(1));
        }

        [Test]
        public void Hand_Clear_EmptiesBoth()
        {
            Hand h = new();
            h.Skill.Add(new MoveCardInstance { Move = Mk("m1") });
            ConsumableSO c = ScriptableObject.CreateInstance<ConsumableSO>();
            _disposables.Add(c);
            h.Consumables.Add(c);
            h.Clear();
            Assert.That(h.SkillCount, Is.EqualTo(0));
            Assert.That(h.ConsumableCount, Is.EqualTo(0));
        }

        // ── 5.2.3 — HandSizeCalculator with modifier bonus ───────────────────

        [Test]
        public void EffectiveSkillCount_BaseOnly_ReturnsBase()
        {
            Assert.That(HandSizeCalculator.EffectiveSkillCount(_config), Is.EqualTo(5));
        }

        [Test]
        public void EffectiveSkillCount_PositiveBonus_AddsToBase()
        {
            Assert.That(HandSizeCalculator.EffectiveSkillCount(_config, bonus: 2),
                Is.EqualTo(7));
        }

        [Test]
        public void EffectiveSkillCount_NegativeBonus_FlooredAtZero()
        {
            Assert.That(HandSizeCalculator.EffectiveSkillCount(_config, bonus: -10),
                Is.EqualTo(0));
        }

        [Test]
        public void EffectiveSkillCount_NullConfig_ReturnsZero()
        {
            Assert.That(HandSizeCalculator.EffectiveSkillCount(null, bonus: 5),
                Is.EqualTo(0));
        }

        [Test]
        public void EffectiveConsumableCount_BaseOnly_ReturnsBase()
        {
            Assert.That(HandSizeCalculator.EffectiveConsumableCount(_config),
                Is.EqualTo(2));
        }

        [Test]
        public void EffectiveConsumableCount_BonusStacks()
        {
            Assert.That(HandSizeCalculator.EffectiveConsumableCount(_config, bonus: 1),
                Is.EqualTo(3));
        }

        // ── 5.2.4 — Deterministic seed → identical hand sequence ─────────────

        private CombatController BuildSingleMonCombat(uint seed, int handSizeBonus = 0)
        {
            PokemonInstance lead = new() { Species = _species, Level = 1, CurrentHP = 60 };
            lead.CurrentMoves.Add(Mk("a"));
            lead.CurrentMoves.Add(Mk("b"));
            lead.CurrentMoves.Add(Mk("c"));
            lead.CurrentMoves.Add(Mk("d"));
            PokemonInstance enemy = new() { Species = _species, Level = 1, CurrentHP = 200 };
            enemy.CurrentMoves.Add(Mk("e"));

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed),
            };
            CombatController c = new(setup, new EndAgent());
            c.State.SkillHandSizeBonus = handSizeBonus;
            c.Start();
            return c;
        }

        private sealed class EndAgent : IPlayerAgent
        {
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => s.LeadIndex;
        }

        [Test]
        public void DrawPhase_SameSeed_ProducesIdenticalSkillHandSequence()
        {
            CombatController c1 = BuildSingleMonCombat(seed: 0xABCDEF);
            CombatController c2 = BuildSingleMonCombat(seed: 0xABCDEF);
            // Run 3 turns and compare hand contents each turn.
            for (int turn = 0; turn < 3; turn++)
            {
                c1.DrawPhase();
                c2.DrawPhase();
                Assert.That(c1.State.SkillHand.Count, Is.EqualTo(c2.State.SkillHand.Count),
                    $"Turn {turn}: hand sizes diverged");
                for (int i = 0; i < c1.State.SkillHand.Count; i++)
                {
                    Assert.That(c1.State.SkillHand[i].Move.name,
                                Is.EqualTo(c2.State.SkillHand[i].Move.name),
                        $"Turn {turn} hand[{i}]: card identity diverged");
                }
                // End the turn so the next DrawPhase doesn't double up.
                c1.IntentPhase(); c1.ResolutionPhase(); c1.TurnEnd();
                c2.IntentPhase(); c2.ResolutionPhase(); c2.TurnEnd();
            }
        }

        // ── 5.2.3 integration — bonus changes the actual draw count ─────────

        [Test]
        public void DrawPhase_PositiveSkillBonus_DrawsExtraCards()
        {
            // Base 5 + bonus 2 = 7. But deck only has 4 cards (one Pokémon × 4
            // moves). Hand size is bounded by available deck + discard size.
            // First-turn draw: deck has 4, discard 0 → hand = 4.
            // This test confirms the bonus is requested (not silently dropped):
            // we observe that when the deck is large enough, the request goes
            // through. Use a larger deck by stacking moves.
            CombatController c = BuildSingleMonCombat(seed: 0x12345, handSizeBonus: 2);
            // Add more moves to deck via a second Pokémon (or extend lead's pool
            // — CurrentMoves cap is 4 hard but the SkillDeck can hold more if
            // multiple Pokémon contribute). Adding a 2nd alive Pokémon:
            PokemonInstance second = new()
            {
                Species = _species, Level = 1, CurrentHP = 60
            };
            for (int i = 0; i < 4; i++) second.CurrentMoves.Add(Mk($"x{i}"));
            c.State.PlayerTeam.Add(second);
            c.State.Deck.Build(c.State.PlayerTeam); // 4 + 4 = 8 in deck
            c.DrawPhase();
            Assert.That(c.State.SkillHand.Count, Is.EqualTo(7),
                "Base 5 + bonus 2 = 7 cards drawn from an 8-card deck.");
        }
    }
}
