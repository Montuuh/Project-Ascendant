using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 5 Task 5.4 — CardPlayService integration coverage:
    //   • 5.4.2 Step-Forward promotes bench owner to Lead before resolve.
    //   • 5.4.3 Step-Backward resolves, then Lead swaps to player-chosen bench.
    //   • 5.4.4 Melee-from-bench rejected (delegated to CardPlayValidator;
    //           re-asserted here at the service level).
    //   • 5.4.5 Paralysis +1 AP cost integration.
    //   • 5.4.6 Immediate combat-end check (lethal play short-circuits SB).
    //   • SF/SB do NOT increment SwapCounter nor grant Defensive discount.
    public class CardPlayServiceTests
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
            _config.Divisor = 10;             // produce visible double-digit damage at L1
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.BaseAPPerTurn = 6;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 6;  // draw whole deck so tests can find specific cards
            _config.BaseConsumableCardsPerTurn = 0;
            _config.ParalysisAPCostBonus = 1;
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

        private MoveSO Mk(string name, MoveRole role, MoveRange range,
                          PositionalModifier mod, int apCost, int power = 40)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = name;
            m.Type = PokemonType.Normal;
            m.BasePower = power;
            m.APCost = apCost;
            m.Role = role;
            m.Range = range;
            m.Modifier = mod;
            m.RangeModifierMultiplier = range == MoveRange.Ranged ? 0.75f : 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonInstance MakeMon(int hp, params MoveSO[] moves)
        {
            PokemonInstance p = new() { Species = _species, Level = 1, CurrentHP = hp };
            for (int i = 0; i < moves.Length; i++) p.CurrentMoves.Add(moves[i]);
            return p;
        }

        private CombatController Build(List<PokemonInstance> playerTeam,
                                       PokemonInstance enemy,
                                       IPlayerAgent agent,
                                       int leadIndex = 0)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = leadIndex,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0x5050),
            };
            CombatController c = new(setup, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            return c;
        }

        private static int FindHandIndex(CombatController c, MoveSO move)
        {
            for (int i = 0; i < c.State.SkillHand.Count; i++)
            {
                MoveCardInstance card = c.State.SkillHand[i];
                if (card != null && card.Move == move) return i;
            }
            return -1;
        }

        // Find a hand card with the given Move AND a specific Owner — RNG
        // can otherwise hand us the bench's copy, which validates differently
        // (Melee owner != Lead → rejected for non-SF cards).
        private static int FindHandIndex(CombatController c, MoveSO move, PokemonInstance owner)
        {
            for (int i = 0; i < c.State.SkillHand.Count; i++)
            {
                MoveCardInstance card = c.State.SkillHand[i];
                if (card != null && card.Move == move
                    && ReferenceEquals(card.Owner, owner)) return i;
            }
            return -1;
        }

        private sealed class FixedSwapAgent : IPlayerAgent
        {
            public int FixedTeamIndex;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => FixedTeamIndex;
        }

        // ── 5.4.2: Step-Forward ─────────────────────────────────────────────

        [Test]
        public void StepForward_BenchOwner_PromotedToLeadBeforeResolve()
        {
            // Lead has a vanilla move; bench has SF Melee. Playing the SF card
            // from bench promotes the bench Pokémon to Lead, then the strike
            // lands.
            MoveSO vanilla = Mk("vanilla", MoveRole.Offensive, MoveRange.Ranged,
                                PositionalModifier.None, apCost: 1);
            MoveSO sfMelee = Mk("rush", MoveRole.Offensive, MoveRange.Melee,
                                PositionalModifier.StepForward, apCost: 2);
            PokemonInstance lead = MakeMon(60, vanilla);
            PokemonInstance bench = MakeMon(60, sfMelee);
            PokemonInstance enemy = MakeMon(200, vanilla);

            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy,
                                       new FixedSwapAgent { FixedTeamIndex = 1 });

            int handIdx = FindHandIndex(c, sfMelee);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0));

            int swapBefore = c.State.SwapCounter;
            bool discountBefore = c.State.DefensiveSwapDiscountAvailable;
            int enemyHPBefore = enemy.CurrentHP;

            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));

            Assert.That(c.State.LeadIndex, Is.EqualTo(1),
                "Step-Forward must promote bench owner to Lead.");
            Assert.That(c.State.SwapCounter, Is.EqualTo(swapBefore),
                "Step-Forward must NOT increment swap counter (§3.3.2).");
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.EqualTo(discountBefore),
                "Step-Forward must NOT grant defensive-swap discount (§3.3.2).");
            Assert.That(enemy.CurrentHP, Is.LessThan(enemyHPBefore),
                "Strike must land after SF promotion.");
        }

        [Test]
        public void StepForward_AlreadyLead_NoPositionChange_StillResolves()
        {
            MoveSO sfMelee = Mk("rush", MoveRole.Offensive, MoveRange.Melee,
                                PositionalModifier.StepForward, apCost: 2);
            PokemonInstance lead = MakeMon(60, sfMelee);
            PokemonInstance enemy = MakeMon(200, sfMelee);
            CombatController c = Build(new List<PokemonInstance> { lead }, enemy,
                                       new FixedSwapAgent { FixedTeamIndex = 0 });

            int handIdx = FindHandIndex(c, sfMelee);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0));
            int enemyHPBefore = enemy.CurrentHP;
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(c.State.LeadIndex, Is.EqualTo(0));
            Assert.That(enemy.CurrentHP, Is.LessThan(enemyHPBefore));
        }

        // ── 5.4.3: Step-Backward ────────────────────────────────────────────

        [Test]
        public void StepBackward_LeadOwner_ResolvesFirst_ThenSwapsToBench()
        {
            // Lead plays SB → strike lands → agent picks bench slot 1 as new Lead.
            MoveSO sbMelee = Mk("retreat", MoveRole.Defensive, MoveRange.Melee,
                                PositionalModifier.StepBackward, apCost: 2);
            MoveSO ranged = Mk("range", MoveRole.Offensive, MoveRange.Ranged,
                               PositionalModifier.None, apCost: 1);
            PokemonInstance lead = MakeMon(60, sbMelee);
            PokemonInstance bench = MakeMon(60, ranged);
            PokemonInstance enemy = MakeMon(200, ranged);

            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy,
                                       new FixedSwapAgent { FixedTeamIndex = 1 });
            int handIdx = FindHandIndex(c, sbMelee);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0));

            int swapBefore = c.State.SwapCounter;
            bool discountBefore = c.State.DefensiveSwapDiscountAvailable;
            int enemyHPBefore = enemy.CurrentHP;

            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));

            Assert.That(enemy.CurrentHP, Is.LessThan(enemyHPBefore),
                "SB strike must resolve before the swap.");
            Assert.That(c.State.LeadIndex, Is.EqualTo(1),
                "SB must promote the agent-chosen bench Pokémon to Lead.");
            Assert.That(c.State.SwapCounter, Is.EqualTo(swapBefore),
                "SB must NOT increment swap counter (§3.3.3).");
            Assert.That(c.State.DefensiveSwapDiscountAvailable, Is.EqualTo(discountBefore),
                "SB must NOT grant defensive-swap discount (§3.3.3).");
        }

        [Test]
        public void StepBackward_NoEligibleBench_EffectResolves_LeadStays()
        {
            // Bench is fainted → SB still resolves the effect, Lead stays Lead.
            MoveSO sb = Mk("retreat", MoveRole.Defensive, MoveRange.Melee,
                           PositionalModifier.StepBackward, apCost: 2);
            PokemonInstance lead = MakeMon(60, sb);
            PokemonInstance benchDead = MakeMon(0, sb);  // already fainted
            PokemonInstance enemy = MakeMon(200, sb);

            CombatController c = Build(new List<PokemonInstance> { lead, benchDead }, enemy,
                                       new FixedSwapAgent { FixedTeamIndex = 1 });
            int handIdx = FindHandIndex(c, sb, lead);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0));
            int enemyHPBefore = enemy.CurrentHP;
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(enemy.CurrentHP, Is.LessThan(enemyHPBefore));
            Assert.That(c.State.LeadIndex, Is.EqualTo(0),
                "No eligible bench → Lead remains (§3.3.3).");
        }

        [Test]
        public void StepBackward_FrozenBench_Excluded()
        {
            MoveSO sb = Mk("retreat", MoveRole.Defensive, MoveRange.Melee,
                           PositionalModifier.StepBackward, apCost: 2);
            PokemonInstance lead = MakeMon(60, sb);
            PokemonInstance bench = MakeMon(60, sb);
            bench.PrimaryStatus = StatusCondition.Freeze;
            PokemonInstance enemy = MakeMon(200, sb);

            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy,
                                       new FixedSwapAgent { FixedTeamIndex = 1 });
            int handIdx = FindHandIndex(c, sb);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0));
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(c.State.LeadIndex, Is.EqualTo(0),
                "Frozen bench is ineligible for SB target (§3.3.3 + §3.3.5.1).");
        }

        // ── 5.4.4: Melee-from-bench rejection (re-asserted at service level) ─

        [Test]
        public void Play_MeleeFromBench_NoSF_Rejected_HandUnchanged()
        {
            MoveSO meleeNoSF = Mk("punch", MoveRole.Offensive, MoveRange.Melee,
                                  PositionalModifier.None, apCost: 1);
            PokemonInstance lead = MakeMon(60, meleeNoSF);
            PokemonInstance bench = MakeMon(60, meleeNoSF);
            PokemonInstance enemy = MakeMon(200, meleeNoSF);
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy,
                                       new FixedSwapAgent());
            // Find a card whose Owner is bench.
            int benchCardIdx = -1;
            for (int i = 0; i < c.State.SkillHand.Count; i++)
                if (ReferenceEquals(c.State.SkillHand[i].Owner, bench))
                { benchCardIdx = i; break; }
            Assume.That(benchCardIdx, Is.GreaterThanOrEqualTo(0));

            int handCountBefore = c.State.SkillHand.Count;
            int apBefore = c.State.CurrentAP;
            c.ExecuteAction(PlayerAction.PlaySkill(benchCardIdx, enemySlot: 0));
            Assert.That(c.State.SkillHand.Count, Is.EqualTo(handCountBefore),
                "Rejected play must NOT remove the card from hand.");
            Assert.That(c.State.CurrentAP, Is.EqualTo(apBefore),
                "Rejected play must NOT spend AP.");
        }

        // ── 5.4.5: Paralysis +1 AP cost integration ─────────────────────────

        [Test]
        public void Paralyzed_Lead_PlaysCardAtBaseCostPlusOne()
        {
            MoveSO ranged = Mk("zap", MoveRole.Offensive, MoveRange.Ranged,
                               PositionalModifier.None, apCost: 1);
            PokemonInstance lead = MakeMon(60, ranged);
            lead.PrimaryStatus = StatusCondition.Paralysis;
            PokemonInstance enemy = MakeMon(200, ranged);
            CombatController c = Build(new List<PokemonInstance> { lead }, enemy,
                                       new FixedSwapAgent());
            int handIdx = FindHandIndex(c, ranged);
            int apBefore = c.State.CurrentAP;
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(c.State.CurrentAP, Is.EqualTo(apBefore - 2),
                "Paralysis +1 AP per §4.2.2.3: 1 base + 1 = 2.");
        }

        // ── 5.4.6: Immediate combat-end check ───────────────────────────────

        [Test]
        public void LethalPlay_SetsVictoryImmediately_SBSkipped()
        {
            // Damage tuning: lead Atk × power × STAB / Divisor at L1 should kill
            // a fragile enemy. Enemy HP = 5.
            MoveSO sbLethal = Mk("finisher", MoveRole.Defensive, MoveRange.Melee,
                                 PositionalModifier.StepBackward, apCost: 2, power: 80);
            PokemonInstance lead = MakeMon(60, sbLethal);
            PokemonInstance bench = MakeMon(60, sbLethal);
            PokemonInstance enemy = MakeMon(5, sbLethal);
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy,
                                       new FixedSwapAgent { FixedTeamIndex = 1 });
            int handIdx = FindHandIndex(c, sbLethal, lead);
            Assume.That(handIdx, Is.GreaterThanOrEqualTo(0));
            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 0));
            Assert.That(c.State.Outcome,
                Is.EqualTo(CombatController.CombatOutcome.Victory),
                "Lethal play must flip Outcome → Victory immediately (§3.2.4).");
            Assert.That(c.State.LeadIndex, Is.EqualTo(0),
                "SB swap must NOT execute after a combat-ending play.");
        }
    }
}
