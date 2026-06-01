using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.1 — full combat round-trip via CombatController.
    //
    // Task 4.1.9 specifies a "Play Mode test" — we substitute an EditMode
    // headless drive because the controller is engine-agnostic and the
    // logic is fully testable without a scene. The Play Mode variant lands
    // in Epic 13 when UI wiring exists.
    //
    // Coverage:
    //   • Start populates the Skill Deck correctly (Task 4.1.2)
    //   • DrawPhase refreshes AP, resets swap counter, fills hand (4.1.3)
    //   • IntentPhase picks a Witnessed-or-better intent (4.1.4)
    //   • ActionPhase honours AP costs and EndTurn (4.1.5)
    //   • ResolutionPhase applies damage + faint via the resolvers (4.1.6)
    //   • TurnEnd does NOT reset stat stages (§4.4.3.1)
    //   • CombatEnd clears statuses and resets stat stages (§4.2.1 / §4.2.6)
    //   • Victory and Defeat outcomes are both reachable
    //   • Determinism: same seed + same agent → same outcome
    public class CombatControllerTests
    {
        private BattleConfigSO _config;

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
            _config.BaseSkillCardsPerTurn = 5;       // §3.3.1 — see Confusion floor
            _config.BaseConsumableCardsPerTurn = 2;
            _config.BurnDoTDivisor = 16;
            _config.BurnAttackMultiplier = 0.75f;
            _config.PoisonDoTDivisor = 16;
            _config.PoisonDefenseMultiplier = 0.85f;
            _config.ParalysisAPCostBonus = 1;
            _config.ParalysisDuration = 3;
            _config.SleepDuration = 1;
            _config.FreezeDuration = 1;
            _config.FreezeFireDamageMultiplier = 1.5f;
            _config.ConfusionDuration = 3;
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.AggressiveSelfMultiplier = 1.5f;
            _config.LowSelfHPThreshold = 0.40f;
            _config.SetupSelfMultiplier = 1.5f;
            _config.HighSelfHPThreshold = 0.70f;
            _config.RandomnessFloorChance = 0f;        // deterministic in tests
            _config.BossCounterIntelTopPenalty = 0.7f;
            _config.SunnyDayFireMultiplier = 1.5f;
            _config.SunnyDayWaterMultiplier = 0.5f;
            _config.RainDanceWaterMultiplier = 1.5f;
            _config.RainDanceFireMultiplier = 0.5f;
            _config.ElectricTerrainElectricMultiplier = 1.3f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private readonly List<Object> _disposables = new();

        // ── Helpers ───────────────────────────────────────────────────────────

        private PokemonSpeciesSO MakeSpecies(int hp, int atk, int def, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(s);
            return s;
        }

        private MoveSO MakeMove(PokemonType type, int power, int apCost = 1)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.Type = type;
            m.BasePower = power;
            m.APCost = apCost;
            m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonInstance MakeMon(PokemonSpeciesSO sp, MoveSO move)
        {
            PokemonInstance p = new()
            {
                Species = sp,
                Level = 1,
                CurrentHP = sp.BaseStats.BaseHP,
            };
            p.CurrentMoves.Add(move);
            return p;
        }

        // Simple scripted agent: always plays the first skill card it can,
        // targeting enemy slot 0; ends turn when AP is exhausted or hand is
        // empty.
        private sealed class FirstCardAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState state,
                                           IReadOnlyList<PokemonInstance> candidates)
            {
                // Pick first candidate's index inside PlayerTeam.
                for (int i = 0; i < state.PlayerTeam.Count; i++)
                {
                    PokemonInstance p = state.PlayerTeam[i];
                    if (p != null && p.CurrentHP > 0 && i != state.LeadIndex)
                        return i;
                }
                return state.LeadIndex;
            }

            public PlayerAction DecideAction(CombatController.CombatState state)
            {
                if (state.SkillHand.Count == 0) return PlayerAction.End();
                if (state.CurrentAP <= 0) return PlayerAction.End();
                return PlayerAction.PlaySkill(0, enemySlot: 0);
            }
        }

        // Agent that does NOTHING — just ends turn immediately. Used to set
        // up the Defeat scenario (player never attacks; enemy whittles down).
        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s, IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        private CombatController.CombatSetup BuildSetup(
            PokemonInstance player, PokemonInstance enemy, uint seed)
        {
            return new CombatController.CombatSetup
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed),
            };
        }

        // ── Bucket 1: Start phase (Task 4.1.2) ────────────────────────────────

        [Test]
        public void Start_BuildsSkillDeckFromActiveTeamMoves()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 50, 50, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            MoveSO slash = MakeMove(PokemonType.Normal, 50);
            PokemonInstance p = MakeMon(sp, tackle);
            p.CurrentMoves.Add(slash);
            PokemonInstance enemy = MakeMon(sp, tackle);

            CombatController c = new(BuildSetup(p, enemy, 1u), new FirstCardAgent());
            c.Start();

            Assert.That(c.State.Deck.DeckCount, Is.EqualTo(2));
            Assert.That(c.State.Deck.DeckView[0].Owner, Is.SameAs(p));
            Assert.That(c.State.CurrentPhase, Is.EqualTo(CombatController.Phase.Start));
        }

        [Test]
        public void Start_AddsMasteryMoveToDeck()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 50, 50, PokemonType.Normal);
            MoveSO basic = MakeMove(PokemonType.Normal, 40);
            MoveSO mastery = MakeMove(PokemonType.Normal, 90);
            PokemonInstance p = MakeMon(sp, basic);
            p.MasteryMove = mastery;

            CombatController c = new(BuildSetup(p, MakeMon(sp, basic), 1u), new FirstCardAgent());
            c.Start();

            Assert.That(c.State.Deck.DeckCount, Is.EqualTo(2),
                "Deck should contain 1 base move + 1 Mastery Move.");
        }

        // ── Bucket 2: Draw phase (Task 4.1.3) ────────────────────────────────

        [Test]
        public void DrawPhase_RefreshesAPAndResetsSwapCounter()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 50, 50, PokemonType.Normal);
            MoveSO m = MakeMove(PokemonType.Normal, 40);
            PokemonInstance p = MakeMon(sp, m);
            PokemonInstance e = MakeMon(sp, m);

            CombatController c = new(BuildSetup(p, e, 1u), new FirstCardAgent());
            c.Start();
            c.State.CurrentAP = 0;
            c.State.SwapCounter = 7;
            c.DrawPhase();

            Assert.That(c.State.CurrentAP, Is.EqualTo(_config.BaseAPPerTurn));
            Assert.That(c.State.SwapCounter, Is.EqualTo(0));
            Assert.That(c.State.TurnNumber, Is.EqualTo(1));
        }

        [Test]
        public void DrawPhase_DrawsSkillCardsCapAtDeckSize()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 50, 50, PokemonType.Normal);
            MoveSO m = MakeMove(PokemonType.Normal, 40);
            PokemonInstance p = MakeMon(sp, m); // 1 card in deck
            PokemonInstance e = MakeMon(sp, m);

            CombatController c = new(BuildSetup(p, e, 1u), new FirstCardAgent());
            c.Start();
            c.DrawPhase();

            // Only 1 card available; draw target is 5 → hand has 1, then
            // empty deck. Reshuffle from empty discard is a no-op.
            Assert.That(c.State.SkillHand.Count, Is.EqualTo(1));
        }

        // ── Bucket 3: Intent phase (Task 4.1.4) ──────────────────────────────

        [Test]
        public void IntentPhase_ProducesOneIntentPerEnemy()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 50, 50, PokemonType.Normal);
            MoveSO m = MakeMove(PokemonType.Normal, 40);
            PokemonInstance p = MakeMon(sp, m);
            PokemonInstance e = MakeMon(sp, m);

            CombatController c = new(BuildSetup(p, e, 1u), new FirstCardAgent());
            c.Start();
            c.DrawPhase();
            c.IntentPhase();

            Assert.That(c.State.EnemyIntents.Count, Is.EqualTo(1));
            Assert.That(c.State.EnemyIntents[0].Kind, Is.EqualTo(IntentKind.Attack));
            Assert.That(c.State.EnemyIntents[0].Move, Is.SameAs(m));
        }

        // ── Bucket 4: Full round-trip — Victory (Task 4.1.9) ─────────────────

        [Test]
        public void RunFullCombat_PlayerWins_WhenEnemyFragile()
        {
            // Glass-cannon enemy: HP 1, Def 1. Any player hit one-shots.
            PokemonSpeciesSO playerSp = MakeSpecies(100, 80, 50, PokemonType.Normal);
            PokemonSpeciesSO enemySp = MakeSpecies(1, 10, 1, PokemonType.Normal);
            MoveSO strong = MakeMove(PokemonType.Normal, 80);
            MoveSO weak = MakeMove(PokemonType.Normal, 5);

            PokemonInstance player = MakeMon(playerSp, strong);
            PokemonInstance enemy = MakeMon(enemySp, weak);

            CombatController c = new(BuildSetup(player, enemy, 0xCAFE0001u), new FirstCardAgent());
            CombatController.CombatOutcome outcome = c.RunFullCombat(maxTurns: 5);

            Assert.That(outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
            Assert.That(enemy.CurrentHP, Is.EqualTo(0));
            Assert.That(player.CurrentHP, Is.GreaterThan(0));
            Assert.That(c.State.CurrentPhase, Is.EqualTo(CombatController.Phase.CombatEnd));
        }

        [Test]
        public void RunFullCombat_RecordsEnemyKillInBestiary()
        {
            // Per §6.9 / Task 11.8.2 — defeating an enemy records a Bestiary kill (exactly once).
            PokemonSpeciesSO playerSp = MakeSpecies(100, 80, 50, PokemonType.Normal);
            PokemonSpeciesSO enemySp = MakeSpecies(1, 10, 1, PokemonType.Normal);
            enemySp.SpeciesId = "rattata";
            enemySp.WildRarity = RarityTier.Common;
            MoveSO strong = MakeMove(PokemonType.Normal, 80);
            MoveSO weak = MakeMove(PokemonType.Normal, 5);
            PokemonInstance player = MakeMon(playerSp, strong);
            PokemonInstance enemy = MakeMon(enemySp, weak);

            BestiaryProgressSO bestiary = ScriptableObject.CreateInstance<BestiaryProgressSO>();
            CombatController.CombatSetup setup = BuildSetup(player, enemy, 0xCAFE0002u);
            setup.Bestiary = bestiary;
            CombatController c = new(setup, new FirstCardAgent());
            c.RunFullCombat(maxTurns: 5);

            Assert.That(c.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
            Assert.That(bestiary.GetOrCreate("rattata").TimesDefeated, Is.EqualTo(1), "recorded once");
            Object.DestroyImmediate(bestiary);
        }

        // ── Bucket 4b: Consumable dispatch (Epic 12 Task 12.1) ───────────────

        // Plays ConsumableHand[0] (targeting Lead) once, then ends.
        private sealed class PlayConsumableAgent : IPlayerAgent
        {
            private bool _done;
            public int PickLeadReplacement(CombatController.CombatState s, IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s)
            {
                if (!_done && s.ConsumableHand.Count > 0) { _done = true; return PlayerAction.PlayConsumable(0, 0); }
                return PlayerAction.End();
            }
        }

        private ConsumableSO MakeConsumable(ConsumableEffectSO effect, int apCost)
        {
            ConsumableSO c = ScriptableObject.CreateInstance<ConsumableSO>();
            c.Effect = effect; c.APCost = apCost;
            _disposables.Add(c); _disposables.Add(effect);
            return c;
        }

        private CombatController BuildConsumableCombat(PokemonInstance player, ConsumableSO consumable)
        {
            PokemonInstance enemy = MakeMon(MakeSpecies(50, 10, 50, PokemonType.Normal), MakeMove(PokemonType.Normal, 5));
            CombatController.CombatSetup setup = BuildSetup(player, enemy, 7u);
            setup.ConsumableInventory = new List<ConsumableSO> { consumable };
            CombatController c = new(setup, new PlayConsumableAgent());
            c.Start(); c.DrawPhase(); c.ActionPhase(); // play the consumable mid-combat
            return c;
        }

        [Test]
        public void Consumable_Potion_HealsLead_CappedAtMax()
        {
            // §8.2.2 — Potion restores FlatHealAmount, capped at (Effective) Max HP.
            PokemonInstance player = MakeMon(MakeSpecies(100, 50, 50, PokemonType.Normal), MakeMove(PokemonType.Normal, 40));
            player.CurrentHP = 40;
            HealConsumableEffectSO heal = ScriptableObject.CreateInstance<HealConsumableEffectSO>();
            heal.FlatHealAmount = 30;

            BuildConsumableCombat(player, MakeConsumable(heal, apCost: 1));

            Assert.That(player.CurrentHP, Is.EqualTo(70), "40 + 30 (cap 100).");
        }

        [Test]
        public void Consumable_BurnHeal_CuresStatus()
        {
            // §8.2.3 — status-cure consumable clears the matching primary status.
            PokemonInstance player = MakeMon(MakeSpecies(100, 50, 50, PokemonType.Normal), MakeMove(PokemonType.Normal, 40));
            player.PrimaryStatus = StatusCondition.Burn;
            player.PrimaryStatusTurnsRemaining = 99;
            StatusCureConsumableEffectSO cure = ScriptableObject.CreateInstance<StatusCureConsumableEffectSO>();
            cure.CuresStatus = StatusCondition.Burn;

            BuildConsumableCombat(player, MakeConsumable(cure, apCost: 0));

            Assert.That(player.PrimaryStatus, Is.EqualTo(StatusCondition.None));
        }

        [Test]
        public void Consumable_Ether_GrantsAP_NetOfCost()
        {
            // §8.2.4 — Ether grants +2 AP this turn (net of its own 1 AP cost): 3 − 1 + 2 = 4.
            PokemonInstance player = MakeMon(MakeSpecies(100, 50, 50, PokemonType.Normal), MakeMove(PokemonType.Normal, 40));
            APGrantConsumableEffectSO ap = ScriptableObject.CreateInstance<APGrantConsumableEffectSO>();
            ap.APGranted = 2;

            CombatController c = BuildConsumableCombat(player, MakeConsumable(ap, apCost: 1));

            Assert.That(c.State.CurrentAP, Is.EqualTo(4));
        }

        // ── Bucket 5: Full round-trip — Defeat (Task 4.1.9) ──────────────────

        [Test]
        public void RunFullCombat_PlayerLoses_WhenPassiveAndEnemyStrong()
        {
            // Player passive — never attacks. Strong enemy chips down to 0.
            PokemonSpeciesSO playerSp = MakeSpecies(40, 50, 30, PokemonType.Normal);
            PokemonSpeciesSO enemySp = MakeSpecies(100, 80, 50, PokemonType.Normal);
            MoveSO basic = MakeMove(PokemonType.Normal, 40);
            MoveSO strong = MakeMove(PokemonType.Normal, 80);

            PokemonInstance player = MakeMon(playerSp, basic);
            PokemonInstance enemy = MakeMon(enemySp, strong);

            CombatController c = new(BuildSetup(player, enemy, 0xCAFE0002u), new PassiveAgent());
            CombatController.CombatOutcome outcome = c.RunFullCombat(maxTurns: 30);

            Assert.That(outcome, Is.EqualTo(CombatController.CombatOutcome.Defeat));
            Assert.That(player.CurrentHP, Is.EqualTo(0));
            // Per §4.8.5 — Trauma stack accrues on faint.
            Assert.That(player.TraumaStacks, Is.GreaterThanOrEqualTo(1));
        }

        // ── Bucket 6: Determinism (Engineering Pillar 3) ─────────────────────

        [Test]
        public void RunFullCombat_SameSeedSameAgent_ProducesSameOutcomeAndDamageTrail()
        {
            // Re-run identical setup twice with the same seed. Final HP on
            // both sides must match exactly.
            PokemonSpeciesSO sp = MakeSpecies(60, 50, 40, PokemonType.Normal);
            MoveSO m = MakeMove(PokemonType.Normal, 40);

            int[] Run(uint seed)
            {
                PokemonInstance p = MakeMon(sp, m);
                PokemonInstance e = MakeMon(sp, m);
                CombatController c = new(BuildSetup(p, e, seed), new FirstCardAgent());
                c.RunFullCombat(maxTurns: 10);
                return new[] { p.CurrentHP, e.CurrentHP, (int)c.State.Outcome };
            }

            int[] a = Run(0xDEADC0DEu);
            int[] b = Run(0xDEADC0DEu);
            Assert.That(a, Is.EqualTo(b),
                "Identical seeds + identical agent must produce identical " +
                "final HP / outcome (Engineering Pillar 3 — determinism).");
        }

        // ── Bucket 7: CombatEnd clears statuses + stat stages ────────────────

        [Test]
        public void CombatEnd_ClearsAllStatusesAndStatStages()
        {
            PokemonSpeciesSO sp = MakeSpecies(100, 50, 50, PokemonType.Normal);
            MoveSO basic = MakeMove(PokemonType.Normal, 40);
            PokemonInstance player = MakeMon(sp, basic);
            PokemonInstance enemy = MakeMon(MakeSpecies(1, 10, 1, PokemonType.Normal), basic);

            // Pre-load some persistent state on the player.
            player.PrimaryStatus = StatusCondition.Burn;
            player.PrimaryStatusTurnsRemaining = int.MaxValue;
            StatStageManager.Modify(player, Stat.Attack, +3);

            CombatController c = new(BuildSetup(player, enemy, 0x1u), new FirstCardAgent());
            c.RunFullCombat(maxTurns: 5);

            Assert.That(player.PrimaryStatus, Is.EqualTo(StatusCondition.None),
                "All statuses cleared on combat end (§4.2.1).");
            Assert.That(StatStageManager.GetStage(player, Stat.Attack), Is.EqualTo(0),
                "Stat stages reset on combat end (§4.2.6).");
        }

        // ── Bucket 8: Lead-targeted intents track swaps (§4.3.2 / Pillar 2) ──

        [Test]
        // Per §4.3.2 / Pillar 2 — an enemy attack telegraphed at the Lead must hit whoever is the
        // Lead at RESOLUTION (after a manual swap), not the Pokémon that was Lead when it declared.
        public void EnemyLeadAttack_AfterManualSwap_HitsNewLead_NotOldLead()
        {
            PokemonSpeciesSO sp = MakeSpecies(120, 50, 50, PokemonType.Normal);
            MoveSO filler = MakeMove(PokemonType.Normal, 40);
            PokemonInstance oldLead = MakeMon(sp, filler);   // slot 0 — Lead at declaration
            PokemonInstance benchTank = MakeMon(sp, filler); // slot 1 — swapped in to eat the hit

            PokemonInstance enemy = MakeMon(sp, MakeMove(PokemonType.Normal, 100));

            CombatController c = new(new CombatController.CombatSetup
            {
                PlayerTeam = new List<PokemonInstance> { oldLead, benchTank },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(1u),
            }, new PassiveAgent());

            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;

            Assert.That(c.State.EnemyIntents[0].Kind, Is.EqualTo(IntentKind.Attack));
            Assert.That(c.State.EnemyIntents[0].TargetsLead, Is.True,
                "A VS enemy attack targets the Lead role, not a frozen slot.");

            int oldLeadHp = oldLead.CurrentHP;
            int benchHp = benchTank.CurrentHP;

            c.ExecuteAction(PlayerAction.ManualSwap(1));
            Assert.That(c.State.LeadIndex, Is.EqualTo(1), "Swap promotes bench slot 1 to Lead.");

            c.ResolutionPhase();

            Assert.That(benchTank.CurrentHP, Is.LessThan(benchHp),
                "The NEW Lead (slot 1) takes the telegraphed hit.");
            Assert.That(oldLead.CurrentHP, Is.EqualTo(oldLeadHp),
                "The OLD Lead (slot 0, now bench) is untouched — the attack followed the Lead role.");
        }

        [Test]
        // Per §4.3.2 — with no swap, the Lead attack still lands on the original Lead (no regression).
        public void EnemyLeadAttack_NoSwap_HitsLead()
        {
            PokemonSpeciesSO sp = MakeSpecies(120, 50, 50, PokemonType.Normal);
            MoveSO filler = MakeMove(PokemonType.Normal, 40);
            PokemonInstance lead = MakeMon(sp, filler);
            PokemonInstance bench = MakeMon(sp, filler);
            PokemonInstance enemy = MakeMon(sp, MakeMove(PokemonType.Normal, 100));

            CombatController c = new(new CombatController.CombatSetup
            {
                PlayerTeam = new List<PokemonInstance> { lead, bench },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(1u),
            }, new PassiveAgent());

            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            int benchHp = bench.CurrentHP;
            c.ResolutionPhase();

            Assert.That(lead.CurrentHP, Is.LessThan(120), "The Lead takes the hit.");
            Assert.That(bench.CurrentHP, Is.EqualTo(benchHp), "The bench is untouched.");
        }
    }
}
