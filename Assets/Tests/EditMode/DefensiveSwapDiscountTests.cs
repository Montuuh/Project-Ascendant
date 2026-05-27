using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.3.1 + Epic 5 Task 5.6.2 / 5.6.3 — Defensive-swap discount edge
    // cases. Both pure (CardPlayValidator) and integrated (CombatController
    // state mutations through TryManualSwap + TryPlaySkillCard) coverage.
    public class DefensiveSwapDiscountTests
    {
        private PokemonSpeciesSO _species;
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

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
            _config.BaseAPPerTurn = 5;          // generous so AP isn't the failure mode
            _config.MaxAPPerTurn = 6;
            // Draw the full deck so tests can locate a specific move by index
            // instead of depending on which card RNG happened to deal.
            _config.BaseSkillCardsPerTurn = 4;
            _config.BaseConsumableCardsPerTurn = 0;
            _config.ParalysisAPCostBonus = 1;
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        private MoveSO Mk(string name, MoveRole role, int apCost, MoveRange range = MoveRange.Ranged)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = name;
            m.Type = PokemonType.Normal;
            m.BasePower = role == MoveRole.Offensive ? 40 : 0;
            m.APCost = apCost;
            m.Role = role;
            m.Range = range;
            m.Modifier = PositionalModifier.None;
            _disposables.Add(m);
            return m;
        }

        // ── Pure helpers (CardPlayValidator) ─────────────────────────────────

        [Test]
        public void Apply_NoDiscountAvailable_NoReduction()
        {
            MoveSO def = Mk("d", MoveRole.Defensive, 2);
            Assert.That(CardPlayValidator.ApplyDefensiveDiscount(2, def, discountAvailable: false),
                Is.EqualTo(2));
        }

        [Test]
        public void Apply_OffensiveCard_NoReduction()
        {
            MoveSO off = Mk("o", MoveRole.Offensive, 2);
            Assert.That(CardPlayValidator.ApplyDefensiveDiscount(2, off, discountAvailable: true),
                Is.EqualTo(2));
        }

        [Test]
        public void Apply_UtilityCard_NoReduction()
        {
            MoveSO ut = Mk("u", MoveRole.Utility, 2);
            Assert.That(CardPlayValidator.ApplyDefensiveDiscount(2, ut, discountAvailable: true),
                Is.EqualTo(2));
        }

        [Test]
        public void Apply_Defensive_ReducesByOne()
        {
            MoveSO def = Mk("d", MoveRole.Defensive, 2);
            Assert.That(CardPlayValidator.ApplyDefensiveDiscount(2, def, discountAvailable: true),
                Is.EqualTo(1));
        }

        [Test]
        public void Apply_Defensive_FlooredAtZero()
        {
            // §3.3.1 — "minimum 0". A 0-cost Defensive card stays at 0.
            MoveSO def = Mk("d0", MoveRole.Defensive, 0);
            Assert.That(CardPlayValidator.ApplyDefensiveDiscount(0, def, discountAvailable: true),
                Is.EqualTo(0));
        }

        [Test]
        public void ShouldConsume_Defensive_True()
        {
            MoveSO def = Mk("d", MoveRole.Defensive, 1);
            Assert.That(CardPlayValidator.ShouldConsumeDefensiveDiscount(def, discountAvailable: true),
                Is.True);
        }

        [Test]
        public void ShouldConsume_DefensiveButNoDiscount_False()
        {
            MoveSO def = Mk("d", MoveRole.Defensive, 1);
            Assert.That(CardPlayValidator.ShouldConsumeDefensiveDiscount(def, discountAvailable: false),
                Is.False);
        }

        [Test]
        public void ShouldConsume_NonDefensive_False()
        {
            MoveSO off = Mk("o", MoveRole.Offensive, 1);
            Assert.That(CardPlayValidator.ShouldConsumeDefensiveDiscount(off, discountAvailable: true),
                Is.False);
        }

        // ── Integration (CombatController state) ─────────────────────────────

        private CombatController BuildTwoMonCombat(MoveSO leadMove, MoveSO benchMove)
        {
            PokemonInstance lead = new() { Species = _species, Level = 1, CurrentHP = 60 };
            lead.CurrentMoves.Add(leadMove);
            PokemonInstance bench = new() { Species = _species, Level = 1, CurrentHP = 60 };
            bench.CurrentMoves.Add(benchMove);
            PokemonInstance enemy = new() { Species = _species, Level = 1, CurrentHP = 60 };
            enemy.CurrentMoves.Add(leadMove);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead, bench },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0xA5),
            };
            CombatController c = new(setup, new StubAgent());
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            return c;
        }

        private sealed class StubAgent : IPlayerAgent
        {
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => s.LeadIndex;
        }

        [Test]
        public void ManualSwap_SetsDiscountFlag()
        {
            // The hand carries one Ranged Defensive card; swap to bench should
            // arm the discount.
            MoveSO defensive = Mk("guard", MoveRole.Defensive, 2);
            CombatController c = BuildTwoMonCombat(defensive, defensive);
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False);
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 1
            });
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.True);
        }

        [Test]
        public void DefensiveCardAfterSwap_PaysOneFewerAP_ConsumesDiscount()
        {
            // Setup: bench has the Defensive card; manual swap promotes bench
            // to lead, then we play the (now-lead) Defensive card.
            MoveSO defensive = Mk("guard", MoveRole.Defensive, 2);
            CombatController c = BuildTwoMonCombat(defensive, defensive);
            int apBeforeSwap = c.State.CurrentAP;
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 1
            });
            // Swap cost = 1 (first swap). AP after swap = apBeforeSwap - 1.
            int apAfterSwap = c.State.CurrentAP;
            Assert.That(apAfterSwap, Is.EqualTo(apBeforeSwap - 1));
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.True);

            c.ExecuteAction(PlayerAction.PlaySkill(0, enemySlot: 0));
            // Card cost: 2 base − 1 discount = 1.
            Assert.That(c.State.CurrentAP, Is.EqualTo(apAfterSwap - 1));
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False);
        }

        [Test]
        public void OffensiveCardAfterSwap_DoesNotConsumeDiscount()
        {
            MoveSO defensive = Mk("guard", MoveRole.Defensive, 2);
            MoveSO offensive = Mk("strike", MoveRole.Offensive, 2);
            CombatController c = BuildTwoMonCombat(offensive, defensive);
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 1
            });
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.True);
            // Hand contains both cards (BaseSkillCardsPerTurn=4 drains the
            // 2-card deck). Find the offensive card by Role and play it.
            int offIdx = FindHandIndexByRole(c, MoveRole.Offensive);
            Assume.That(offIdx, Is.GreaterThanOrEqualTo(0));
            int apBefore = c.State.CurrentAP;
            c.ExecuteAction(PlayerAction.PlaySkill(offIdx, enemySlot: 0));
            Assert.That(c.State.CurrentAP, Is.EqualTo(apBefore - 2));
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.True,
                "Offensive card must not consume the defensive discount.");
        }

        // Helper: locate the first card in hand whose move matches the given role.
        // Returns -1 if no match.
        private static int FindHandIndexByRole(CombatController c, MoveRole role)
        {
            for (int i = 0; i < c.State.SkillHand.Count; i++)
            {
                MoveCardInstance card = c.State.SkillHand[i];
                if (card != null && card.Move != null && card.Move.Role == role) return i;
            }
            return -1;
        }

        [Test]
        public void DefensiveCardBeforeAnySwap_NoDiscount()
        {
            MoveSO defensive = Mk("guard", MoveRole.Defensive, 2);
            CombatController c = BuildTwoMonCombat(defensive, defensive);
            int apBefore = c.State.CurrentAP;
            c.ExecuteAction(PlayerAction.PlaySkill(0, enemySlot: 0));
            Assert.That(c.State.CurrentAP, Is.EqualTo(apBefore - 2),
                "No prior swap → no discount → full 2 AP cost.");
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False);
        }

        [Test]
        public void DiscountDoesNotStackAcrossMultipleSwaps()
        {
            // Two manual swaps both set the flag, but it's a single boolean
            // — the second swap is idempotent w.r.t. the flag. Only one
            // Defensive card is discounted.
            MoveSO defensive = Mk("guard", MoveRole.Defensive, 2);
            CombatController c = BuildTwoMonCombat(defensive, defensive);
            // 1st swap (cost 1) → flag set.
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 1
            });
            // 2nd swap (cost 2) — back to slot 0.
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 0
            });
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.True);
            int apBeforePlay = c.State.CurrentAP;
            c.ExecuteAction(PlayerAction.PlaySkill(0, enemySlot: 0));
            // Discount applied once (cost 2 - 1 = 1) and consumed.
            Assert.That(c.State.CurrentAP, Is.EqualTo(apBeforePlay - 1));
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False);
        }

        [Test]
        public void DiscountResetsAtNextDrawPhase()
        {
            // Swap, no Defensive play, TurnEnd → next DrawPhase clears flag.
            MoveSO defensive = Mk("guard", MoveRole.Defensive, 2);
            CombatController c = BuildTwoMonCombat(defensive, defensive);
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 1
            });
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.True);
            c.ResolutionPhase();
            c.TurnEnd();
            c.DrawPhase();
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False);
        }
    }
}
