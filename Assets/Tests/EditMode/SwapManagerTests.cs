using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §3.3.1 + Epic 6 Task 6.1.4 — focused coverage for SwapManager.
    //
    // Why these tests live separately from DefensiveSwapDiscountTests:
    // those tests cover the discount flag end-to-end. These tests cover the
    // cost-ladder + counter-reset semantics in isolation, plus the explicit
    // "SF/SB must not increment the counter" rule (cross-verified at the
    // service layer in CardPlayServiceTests, but the contract belongs here
    // too because SwapManager is the only legitimate counter-incrementer).
    public class SwapManagerTests
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
            _config.BaseAPPerTurn = 6;        // enough headroom for 3 swaps (1+2+3)
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 0;
            _config.BaseConsumableCardsPerTurn = 0;
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        private PokemonInstance MakeMon(int hp = 60) =>
            new() { Species = _species, Level = 1, CurrentHP = hp };

        // Build a minimal CombatState — bypasses CombatController so the tests
        // are unambiguous about what they cover. Three-mon team, slot 0 lead.
        private CombatController.CombatState MakeState(int initialAP = 6)
        {
            return new CombatController.CombatState
            {
                PlayerTeam = new List<PokemonInstance> { MakeMon(), MakeMon(), MakeMon() },
                LeadIndex = 0,
                CurrentAP = initialAP,
                SwapCounter = 0,
                DefensiveSwapDiscountAvailable = false,
                Config = _config,
            };
        }

        // ── NextSwapCost / cost ladder ───────────────────────────────────────

        [Test]
        public void NextSwapCost_LadderIs_1_2_3()
        {
            // Per §3.3.1 — 1st=1AP, 2nd=2AP, 3rd=3AP.
            Assert.That(SwapManager.NextSwapCost(0), Is.EqualTo(1));
            Assert.That(SwapManager.NextSwapCost(1), Is.EqualTo(2));
            Assert.That(SwapManager.NextSwapCost(2), Is.EqualTo(3));
        }

        [Test]
        public void NextSwapCost_NegativeCounter_TreatedAsZero()
        {
            // Defensive: an underflowed counter still yields the 1 AP base.
            Assert.That(SwapManager.NextSwapCost(-1), Is.EqualTo(1));
            Assert.That(SwapManager.NextSwapCost(-99), Is.EqualTo(1));
        }

        // ── TryManualSwap — mutation contract ────────────────────────────────

        [Test]
        public void TryManualSwap_FirstSwap_Costs1AP_SetsDiscount_IncrementsCounter()
        {
            CombatController.CombatState s = MakeState(initialAP: 6);
            bool ok = SwapManager.TryManualSwap(s, benchSlot: 1);
            Assert.That(ok, Is.True);
            Assert.That(s.CurrentAP, Is.EqualTo(5));            // 6 − 1
            Assert.That(s.SwapCounter, Is.EqualTo(1));
            Assert.That(s.LeadIndex, Is.EqualTo(1));
            Assert.That(s.DefensiveSwapDiscountAvailable, Is.True);
        }

        // ── Relic interactions (Epic 12 §8.3.4 / §8.3.3) ─────────────────────

        private RelicSO Relic(string id)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>(); r.Id = id; _disposables.Add(r); return r;
        }

        [Test]
        public void TryManualSwap_TacticiansCoin_FirstSwapFree()
        {
            CombatController.CombatState s = MakeState(initialAP: 6);
            s.ActiveRelics.Add(Relic("tacticians_coin"));
            SwapManager.TryManualSwap(s, benchSlot: 1);
            Assert.That(s.CurrentAP, Is.EqualTo(6), "§8.3.4 — first swap free.");
            Assert.That(s.ManualSwapsThisCombat, Is.EqualTo(1));
            SwapManager.TryManualSwap(s, benchSlot: 0); // second swap pays NextSwapCost(1)=2
            Assert.That(s.CurrentAP, Is.EqualTo(4));
        }

        [Test]
        public void TryManualSwap_DefenseCurl_BuffsNewLeadEveryThirdSwap()
        {
            CombatController.CombatState s = MakeState(initialAP: 6);
            s.ActiveRelics.Add(Relic("defense_curl_charm"));
            SwapManager.TryManualSwap(s, benchSlot: 1); // 1
            SwapManager.TryManualSwap(s, benchSlot: 2); // 2
            SwapManager.TryManualSwap(s, benchSlot: 0); // 3 → +1 Def on the new Lead (slot 0)
            Assert.That(StatStageManager.GetStage(s.PlayerTeam[0], Stat.Defense), Is.EqualTo(1));
        }

        [Test]
        public void TryManualSwap_SecondSwap_Costs2AP()
        {
            CombatController.CombatState s = MakeState(initialAP: 6);
            SwapManager.TryManualSwap(s, 1);  // cost 1
            SwapManager.TryManualSwap(s, 2);  // cost 2
            Assert.That(s.CurrentAP, Is.EqualTo(3));            // 6 − 1 − 2
            Assert.That(s.SwapCounter, Is.EqualTo(2));
            Assert.That(s.LeadIndex, Is.EqualTo(2));
        }

        [Test]
        public void TryManualSwap_ThirdSwap_Costs3AP()
        {
            CombatController.CombatState s = MakeState(initialAP: 6);
            SwapManager.TryManualSwap(s, 1);  // cost 1
            SwapManager.TryManualSwap(s, 2);  // cost 2
            SwapManager.TryManualSwap(s, 0);  // cost 3
            Assert.That(s.CurrentAP, Is.EqualTo(0));            // 6 − 1 − 2 − 3
            Assert.That(s.SwapCounter, Is.EqualTo(3));
            Assert.That(s.LeadIndex, Is.EqualTo(0));
        }

        [Test]
        public void TryManualSwap_InsufficientAP_RejectedAndNoMutation()
        {
            CombatController.CombatState s = MakeState(initialAP: 0);
            bool ok = SwapManager.TryManualSwap(s, 1);
            Assert.That(ok, Is.False);
            Assert.That(s.CurrentAP, Is.EqualTo(0));
            Assert.That(s.SwapCounter, Is.EqualTo(0));
            Assert.That(s.LeadIndex, Is.EqualTo(0));
            Assert.That(s.DefensiveSwapDiscountAvailable, Is.False);
        }

        [Test]
        public void TryManualSwap_TargetIsCurrentLead_Rejected()
        {
            CombatController.CombatState s = MakeState();
            bool ok = SwapManager.TryManualSwap(s, benchSlot: 0); // already Lead
            Assert.That(ok, Is.False);
            Assert.That(s.SwapCounter, Is.EqualTo(0));
            Assert.That(s.DefensiveSwapDiscountAvailable, Is.False);
        }

        [Test]
        public void TryManualSwap_TargetIsFainted_Rejected()
        {
            CombatController.CombatState s = MakeState();
            s.PlayerTeam[1].CurrentHP = 0;
            bool ok = SwapManager.TryManualSwap(s, 1);
            Assert.That(ok, Is.False);
            Assert.That(s.SwapCounter, Is.EqualTo(0));
        }

        [Test]
        public void TryManualSwap_OutOfRange_Rejected()
        {
            CombatController.CombatState s = MakeState();
            Assert.That(SwapManager.TryManualSwap(s, -1), Is.False);
            Assert.That(SwapManager.TryManualSwap(s, 99), Is.False);
            Assert.That(s.SwapCounter, Is.EqualTo(0));
        }

        // Per §3.3.5.1 — Frozen Lead is position-locked; manual swap blocked
        // (unless the Lead has also fainted, in which case faint precedence
        // routes through Lead-replacement, not through SwapManager).
        [Test]
        public void TryManualSwap_FrozenLead_NotFainted_Rejected()
        {
            CombatController.CombatState s = MakeState();
            s.PlayerTeam[0].PrimaryStatus = StatusCondition.Freeze;
            bool ok = SwapManager.TryManualSwap(s, 1);
            Assert.That(ok, Is.False);
            Assert.That(s.LeadIndex, Is.EqualTo(0));
            Assert.That(s.SwapCounter, Is.EqualTo(0));
        }

        // ── Counter-reset (controller-owned, but verified through full loop) ─

        [Test]
        public void SwapCounter_ResetsAtNextDrawPhase()
        {
            // Build a real CombatController so we exercise the DrawPhase
            // reset path (CombatController.DrawPhase sets SwapCounter = 0).
            CombatController c = BuildController();
            // First swap (cost 1) → counter == 1.
            c.ExecuteAction(new PlayerAction
            {
                Kind = PlayerActionKind.ManualSwap, SwapToBenchSlot = 1
            });
            Assert.That(c.State.SwapCounter, Is.EqualTo(1));
            // Wind through Resolution → TurnEnd → next DrawPhase.
            c.ResolutionPhase();
            c.TurnEnd();
            c.DrawPhase();
            Assert.That(c.State.SwapCounter, Is.EqualTo(0),
                "DrawPhase must reset the per-turn swap counter (§3.3.1).");
        }

        // ── SF/SB do NOT increment SwapCounter ───────────────────────────────
        //
        // The CardPlayServiceTests already cover this end-to-end via real card
        // plays. Here we lock down the contract at the SwapManager surface:
        // CardPlayService NEVER calls into SwapManager for SF/SB, so the
        // counter cannot move except through TryManualSwap. We assert this
        // by direct read: a freshly-built state with a SF or SB card played
        // via CardPlayService leaves SwapCounter == 0.

        [Test]
        public void StepForward_DoesNotIncrementSwapCounter()
        {
            CombatController c = BuildControllerWithSFBenchCard();
            int handIdx = FindFirstHandIndexWithModifier(c, PositionalModifier.StepForward);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0),
                "Expected the SF card to be in hand.");
            int swapBefore = c.State.SwapCounter;
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(c.State.SwapCounter, Is.EqualTo(swapBefore),
                "Step-Forward must NOT increment SwapCounter (§3.3.2).");
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False,
                "Step-Forward must NOT arm the defensive discount (§3.3.2).");
        }

        [Test]
        public void StepBackward_DoesNotIncrementSwapCounter()
        {
            CombatController c = BuildControllerWithSBLeadCard();
            int handIdx = FindFirstHandIndexWithModifier(c, PositionalModifier.StepBackward);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0),
                "Expected the SB card to be in hand.");
            int swapBefore = c.State.SwapCounter;
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(c.State.SwapCounter, Is.EqualTo(swapBefore),
                "Step-Backward must NOT increment SwapCounter (§3.3.3).");
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.False,
                "Step-Backward must NOT arm the defensive discount (§3.3.3).");
        }

        // ── Test helpers ─────────────────────────────────────────────────────

        private CombatController BuildController()
        {
            // 3-mon team, one Ranged move per mon so DrawPhase succeeds.
            MoveSO ranged = ScriptableObject.CreateInstance<MoveSO>();
            ranged.name = "ranged";
            ranged.Type = PokemonType.Normal;
            ranged.BasePower = 10;
            ranged.APCost = 1;
            ranged.Role = MoveRole.Offensive;
            ranged.Range = MoveRange.Ranged;
            ranged.Modifier = PositionalModifier.None;
            ranged.RangeModifierMultiplier = 0.75f;
            _disposables.Add(ranged);

            PokemonInstance lead = MakeMon(); lead.CurrentMoves.Add(ranged);
            PokemonInstance bench1 = MakeMon(); bench1.CurrentMoves.Add(ranged);
            PokemonInstance bench2 = MakeMon(); bench2.CurrentMoves.Add(ranged);
            PokemonInstance enemy = MakeMon(); enemy.CurrentMoves.Add(ranged);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead, bench1, bench2 },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0x10),
            };
            CombatController c = new(setup, new StubAgent());
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            return c;
        }

        private CombatController BuildControllerWithSFBenchCard()
        {
            // Owner of the SF card is the bench Pokémon; SF play should
            // promote them to Lead.
            MoveSO sf = ScriptableObject.CreateInstance<MoveSO>();
            sf.name = "sf";
            sf.Type = PokemonType.Normal;
            sf.BasePower = 10;
            sf.APCost = 1;
            sf.Role = MoveRole.Offensive;
            sf.Range = MoveRange.Melee;
            sf.Modifier = PositionalModifier.StepForward;
            sf.RangeModifierMultiplier = 1f;
            _disposables.Add(sf);
            MoveSO ranged = ScriptableObject.CreateInstance<MoveSO>();
            ranged.name = "r";
            ranged.Type = PokemonType.Normal; ranged.BasePower = 10; ranged.APCost = 1;
            ranged.Role = MoveRole.Offensive; ranged.Range = MoveRange.Ranged;
            ranged.RangeModifierMultiplier = 0.75f;
            _disposables.Add(ranged);

            PokemonInstance lead = MakeMon(); lead.CurrentMoves.Add(ranged);
            PokemonInstance bench = MakeMon(); bench.CurrentMoves.Add(sf);
            PokemonInstance enemy = MakeMon(200); enemy.CurrentMoves.Add(ranged);

            _config.BaseSkillCardsPerTurn = 4;  // drain the 2-card deck
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead, bench },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0x11),
            };
            CombatController c = new(setup, new StubAgent { FixedTeamIndex = 1 });
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            return c;
        }

        private CombatController BuildControllerWithSBLeadCard()
        {
            // Lead owns the SB card; SB play should swap to bench AFTER damage.
            MoveSO sb = ScriptableObject.CreateInstance<MoveSO>();
            sb.name = "sb";
            sb.Type = PokemonType.Normal;
            sb.BasePower = 10;
            sb.APCost = 1;
            sb.Role = MoveRole.Defensive;
            sb.Range = MoveRange.Melee;
            sb.Modifier = PositionalModifier.StepBackward;
            sb.RangeModifierMultiplier = 1f;
            _disposables.Add(sb);
            MoveSO ranged = ScriptableObject.CreateInstance<MoveSO>();
            ranged.name = "r";
            ranged.Type = PokemonType.Normal; ranged.BasePower = 10; ranged.APCost = 1;
            ranged.Role = MoveRole.Offensive; ranged.Range = MoveRange.Ranged;
            ranged.RangeModifierMultiplier = 0.75f;
            _disposables.Add(ranged);

            PokemonInstance lead = MakeMon(); lead.CurrentMoves.Add(sb);
            PokemonInstance bench = MakeMon(); bench.CurrentMoves.Add(ranged);
            PokemonInstance enemy = MakeMon(200); enemy.CurrentMoves.Add(ranged);

            _config.BaseSkillCardsPerTurn = 4;
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead, bench },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0x12),
            };
            CombatController c = new(setup, new StubAgent { FixedTeamIndex = 1 });
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            return c;
        }

        private static int FindFirstHandIndexWithModifier(CombatController c,
                                                          PositionalModifier mod)
        {
            for (int i = 0; i < c.State.SkillHand.Count; i++)
            {
                MoveCardInstance card = c.State.SkillHand[i];
                if (card != null && card.Move != null && card.Move.Modifier == mod) return i;
            }
            return -1;
        }

        private sealed class StubAgent : IPlayerAgent
        {
            public int FixedTeamIndex;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => FixedTeamIndex;
        }
    }
}
