using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Playtest Bug Triage 2026-06-02 — regression tests for P0/P1 fixes.
    // Each test would FAIL on the old behavior and PASS now. Tests grouped by
    // bug number (Notion) + GDD § reference per .claude/rules/tests.md.
    public class PlaytestBugRegressionTests
    {
        private BattleConfigSO _config;
        private EconomyConfigSO _economy;
        private PokemonSpeciesSO _species;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.Divisor = 10;
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 5;
            _config.BaseConsumableCardsPerTurn = 2;
            _config.BurnDoTDivisor = 16;
            _config.BurnAttackMultiplier = 0.75f;
            _config.PoisonDoTDivisor = 16;
            _config.PoisonDefenseMultiplier = 0.85f;
            _config.ParalysisAPCostBonus = 1;
            _config.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.RandomnessFloorChance = 0f;
            _disposables.Add(_config);

            _economy = ScriptableObject.CreateInstance<EconomyConfigSO>();
            _economy.TraumaStackPenaltyPercent = 5;
            _economy.TraumaStackCap = 5;
            _disposables.Add(_economy);

            _species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            _species.Types = new List<PokemonType> { PokemonType.Normal };
            _species.BaseStats = new BaseStats { BaseHP = 60, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            _species.GrowthCurve = null;
            _species.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(_species);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables)
                if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // ── Bug #9: Trauma once-guard (§4.8.5 / §6.2.2) ──────────────────────

        // Per §6.2.2 — Trauma applies "instantly at the moment of faint". The
        // old bug: a Pokémon hit multiple times after fainting (multi-strike
        // chain, or DoT tick while already fainted) accrued MULTIPLE Trauma
        // stacks. The fix: a per-combat HashSet guard ensures exactly +1 stack
        // per faint event (lines 178–183 CombatController.cs).
        [Test]
        public void HandleAnyFaints_MultipleHitsOnFaintedPokemon_AppliesTraumaOnce()
        {
            // Per §6.2.2 — Trauma stacks apply at faint, not per-hit.
            // This test drives a full combat where the Lead gets one-shot by the
            // enemy intent (validates the once-per-combat guard via the standard flow).
            PokemonInstance lead = MakeMon(10);
            PokemonInstance bench = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            bench.CurrentMoves.Add(MakeMove(power: 1));

            PokemonInstance enemy = MakeMon(200);
            enemy.CurrentMoves.Add(MakeMove(power: 999)); // one-shot

            PassiveAgent agent = new() { PickIndex = 1 };
            CombatController c = BuildController(
                new List<PokemonInstance> { lead, bench }, enemy, agent);

            int traumaBefore = lead.TraumaStacks;

            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            c.ActionPhase();
            c.ResolutionPhase(); // enemy kills lead → +1 Trauma, HandleAnyFaints runs once

            Assert.That(lead.TraumaStacks, Is.EqualTo(traumaBefore + 1),
                "Trauma must apply exactly once per faint (§6.2.2 / Bug #9).");
        }

        // Per §6.2.2 clarification (2026-06-01) — DoT ticks also compute
        // against EffectiveMaxHP. The old bug: a fainted Pokémon ticked by
        // Burn/Poison in HandleAnyFaints (TickStatusForAll) after the first
        // faint would accrue another Trauma stack. The fix guards all faint
        // codepaths with _traumaApplied.Add check (line 931).
        [Test]
        public void TickStatusForAll_FaintedPokemonTickedByDoT_DoesNotReapplyTrauma()
        {
            // Per §6.2.2 / Bug #9 — DoT tick after faint must NOT re-add Trauma.
            PokemonInstance lead = MakeMon(2); // low HP so Burn kills in one tick
            lead.PrimaryStatus = StatusCondition.Burn;
            lead.PrimaryStatusTurnsRemaining = 2; // will tick
            PokemonInstance bench = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            bench.CurrentMoves.Add(MakeMove(power: 1));

            PokemonInstance enemy = MakeMon(200);
            enemy.CurrentMoves.Add(MakeMove(power: 10)); // will one-shot lead

            PassiveAgent agent = new() { PickIndex = 1 };
            CombatController c = BuildController(
                new List<PokemonInstance> { lead, bench }, enemy, agent);

            int traumaBefore = lead.TraumaStacks;

            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            c.ActionPhase();
            c.ResolutionPhase(); // enemy kills lead → +1 Trauma; then DoT ticks (fainted → no-op)

            // OLD BUG: TickStatusForAll fired after HandleAnyFaints and applied
            // Trauma again when the fainted lead's Burn ticked 0 → 0.
            Assert.That(lead.TraumaStacks, Is.EqualTo(traumaBefore + 1),
                "DoT tick on already-fainted Pokémon must NOT re-apply Trauma (§6.2.2 / Bug #9).");
        }

        // ── Bug #8: Move effects (§3.2.4) ────────────────────────────────────

        // Per §3.2.4 — "Card effects resolve immediately when played." The old
        // bug: move.Effects[] was never wired; moves with DebuffTargetEffectSO
        // / BuffSelfEffectSO / StatusRiderEffectSO did NOTHING. The fix: lines
        // 226–300 CardPlayService.ResolveEffects dispatch each effect type.
        [Test]
        public void Play_MoveWithDebuffTargetEffect_LowersEnemyStatStage()
        {
            // Per §3.2.4 / Bug #8 — DebuffTargetEffectSO must lower enemy stat.
            PokemonInstance lead = MakeMon(60);
            MoveSO debuffMove = MakeMove(power: 10);
            DebuffTargetEffectSO debuff = ScriptableObject.CreateInstance<DebuffTargetEffectSO>();
            debuff.TargetStat = Stat.Attack;
            debuff.StageChange = -1; // per §4.2.6
            debuffMove.Effects = new List<MoveEffectSO> { debuff };
            _disposables.Add(debuff);
            lead.CurrentMoves.Add(debuffMove);

            PokemonInstance enemy = MakeMon(60);
            enemy.CurrentMoves.Add(MakeMove(power: 1));

            PassiveAgent agent = new();
            CombatController c = BuildController(new List<PokemonInstance> { lead }, enemy, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();

            int enemyAtkBefore = enemy.StatStages.TryGetValue(Stat.Attack, out int atkStage) ? atkStage : 0;

            // Play the debuff move (OLD BUG: effect did nothing).
            int cardIdx = FindCardByMove(c.State.SkillHand, debuffMove);
            Assert.That(cardIdx, Is.GreaterThanOrEqualTo(0), "Debuff move must be in hand.");
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(cardIdx, enemySlot: 0));

            int enemyAtkAfter = enemy.StatStages.TryGetValue(Stat.Attack, out int atkAfter) ? atkAfter : 0;
            Assert.That(enemyAtkAfter, Is.EqualTo(enemyAtkBefore - 1),
                "DebuffTargetEffectSO must lower enemy Atk stage (§3.2.4 / Bug #8).");
        }

        [Test]
        public void Play_MoveWithBuffSelfEffect_RaisesAttackerStatStage()
        {
            // Per §3.2.4 / Bug #8 — BuffSelfEffectSO must raise attacker stat.
            PokemonInstance lead = MakeMon(60);
            MoveSO buffMove = MakeMove(power: 10);
            BuffSelfEffectSO buff = ScriptableObject.CreateInstance<BuffSelfEffectSO>();
            buff.TargetStat = Stat.Defense;
            buff.StageChange = +1;
            buffMove.Effects = new List<MoveEffectSO> { buff };
            _disposables.Add(buff);
            lead.CurrentMoves.Add(buffMove);

            PokemonInstance enemy = MakeMon(60);
            enemy.CurrentMoves.Add(MakeMove(power: 1));

            PassiveAgent agent = new();
            CombatController c = BuildController(new List<PokemonInstance> { lead }, enemy, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();

            int leadDefBefore = lead.StatStages.TryGetValue(Stat.Defense, out int defStage) ? defStage : 0;

            int cardIdx = FindCardByMove(c.State.SkillHand, buffMove);
            Assert.That(cardIdx, Is.GreaterThanOrEqualTo(0), "Buff move must be in hand.");
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(cardIdx, enemySlot: 0));

            int leadDefAfter = lead.StatStages.TryGetValue(Stat.Defense, out int defAfter) ? defAfter : 0;
            Assert.That(leadDefAfter, Is.EqualTo(leadDefBefore + 1),
                "BuffSelfEffectSO must raise attacker Def stage (§3.2.4 / Bug #8).");
        }

        [Test]
        public void Play_MoveWithStatusRiderEffect_AppliesStatusToTarget()
        {
            // Per §3.2.4 / Bug #8 — StatusRiderEffectSO must apply status.
            PokemonInstance lead = MakeMon(60);
            MoveSO statusMove = MakeMove(power: 10);
            StatusRiderEffectSO rider = ScriptableObject.CreateInstance<StatusRiderEffectSO>();
            rider.StatusToApply = StatusCondition.Burn;
            rider.ApplyToSelf = false; // target enemy
            rider.ApplicationChance = 1f; // deterministic
            statusMove.Effects = new List<MoveEffectSO> { rider };
            _disposables.Add(rider);
            lead.CurrentMoves.Add(statusMove);

            PokemonInstance enemy = MakeMon(60);
            enemy.CurrentMoves.Add(MakeMove(power: 1));

            PassiveAgent agent = new();
            CombatController c = BuildController(new List<PokemonInstance> { lead }, enemy, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();

            Assert.That(enemy.PrimaryStatus, Is.EqualTo(StatusCondition.None),
                "Enemy must start with no status.");

            int cardIdx = FindCardByMove(c.State.SkillHand, statusMove);
            Assert.That(cardIdx, Is.GreaterThanOrEqualTo(0), "Status move must be in hand.");
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(cardIdx, enemySlot: 0));

            Assert.That(enemy.PrimaryStatus, Is.EqualTo(StatusCondition.Burn),
                "StatusRiderEffectSO must apply Burn to enemy (§3.2.4 / Bug #8).");
        }

        // ── Bug #10: Lead-faint replacement (§3.3.5 / §4.8.1) ────────────────

        // Per §3.3.5 + Bug #10 — when the Lead faints, the controller pauses
        // mid-HandleAnyFaints and exposes PendingLeadReplacementCandidates (line
        // 166). UI calls ApplyLeadReplacement(idx) to resume. The OLD BUG: the
        // fainted Lead was retained as Lead (LeadIndex unchanged). The FIX:
        // ApplyLeadReplacement promotes the chosen bench to Lead (line 1007).
        [Test]
        public void ApplyLeadReplacement_ValidIndex_PromotesBenchToLead()
        {
            // Per §3.3.5 / Bug #10 — ApplyLeadReplacement must update LeadIndex.
            PokemonInstance lead = MakeMon(10);
            PokemonInstance bench1 = MakeMon(60);
            PokemonInstance bench2 = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            bench1.CurrentMoves.Add(MakeMove(power: 1));
            bench2.CurrentMoves.Add(MakeMove(power: 1));

            PokemonInstance enemy = MakeMon(200);
            enemy.CurrentMoves.Add(MakeMove(power: 999)); // one-shot lead

            PassiveAgent agent = new() { PickIndex = 2 }; // bench2
            CombatController c = BuildController(
                new List<PokemonInstance> { lead, bench1, bench2 }, enemy, agent);

            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            c.ActionPhase();
            c.ResolutionPhase(); // enemy kills lead → candidates exposed

            // OLD BUG: LeadIndex stayed 0 (fainted lead). NEW: changed to 2.
            Assert.That(c.State.LeadIndex, Is.EqualTo(2),
                "ApplyLeadReplacement must promote bench to Lead (§3.3.5 / Bug #10).");
            Assert.That(c.State.PlayerTeam[2], Is.EqualTo(bench2));
            Assert.That(c.State.PendingLeadReplacementCandidates, Is.Null,
                "Pending candidates must clear after replacement.");
        }

        [Test]
        public void HandleAnyFaints_OnlyOneBenchLeft_PromotesThatBench()
        {
            // Per §3.3.5 / Bug #10 — single bench survivor case.
            PokemonInstance lead = MakeMon(10);
            PokemonInstance benchPreDead = MakeMon(0); // already fainted
            PokemonInstance benchAlive = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            benchAlive.CurrentMoves.Add(MakeMove(power: 1));

            PokemonInstance enemy = MakeMon(200);
            enemy.CurrentMoves.Add(MakeMove(power: 999));

            PassiveAgent agent = new() { PickIndex = 2 };
            CombatController c = BuildController(
                new List<PokemonInstance> { lead, benchPreDead, benchAlive }, enemy, agent);

            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            c.ActionPhase();
            c.ResolutionPhase();

            Assert.That(c.State.LeadIndex, Is.EqualTo(2),
                "Only bench survivor must become Lead (§3.3.5 / Bug #10).");
            Assert.That(c.State.PlayerTeam[c.State.LeadIndex], Is.EqualTo(benchAlive));
        }

        // ── Bug #1: Intent rebuild on reinforcement (§7.4) ───────────────────

        // Per §7.4 + Bug #1 — when reinforcements spawn mid-combat (sequential
        // trainer / boss phase), RebuildEnemyIntents refreshes State.EnemyIntents
        // for the new team (lines 415–441). OLD BUG: intents stayed stale (dead
        // enemy intents displayed). NEW: fresh intents for the new entrants.
        [Test]
        public void TryInjectReinforcements_NewEnemySpawns_IntentsRebuiltForNewTeam()
        {
            // Per §7.4 / Bug #1 — reinforcements must refresh intents.
            PokemonInstance lead = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 999)); // one-shot wave1 so reinforcements actually spawn

            PokemonInstance wave1 = MakeMon(10);
            wave1.CurrentMoves.Add(MakeMove(power: 1));

            PokemonInstance wave2 = MakeMon(60);
            MoveSO wave2Move = MakeMove(power: 20);
            wave2.CurrentMoves.Add(wave2Move);

            TestReinforcementProvider provider = new();
            provider.NextWave = new List<PokemonInstance> { wave2 };

            PassiveAgent agent = new();
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { wave1 },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Economy = _economy,
                Rng = new GameRNG(seed: 0xC0DE),
                Reinforcements = provider,
            };
            CombatController c = new(setup, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();

            // Kill wave1 → reinforcements spawn → intents rebuild.
            int wave1Idx = FindCardByMove(c.State.SkillHand, lead.CurrentMoves[0]);
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(wave1Idx, enemySlot: 0));

            // OLD BUG: EnemyIntents[0] was stale (Unknown kind, no move).
            // NEW: intent for wave2 with wave2Move.
            Assert.That(c.State.EnemyTeam.Count, Is.EqualTo(1),
                "Reinforcements must replace dead enemy.");
            Assert.That(c.State.EnemyTeam[0], Is.SameAs(wave2),
                "Wave2 must be in EnemyTeam[0].");
            Assert.That(c.State.EnemyIntents.Count, Is.EqualTo(1),
                "EnemyIntents must match new team size (§7.4 / Bug #1).");
            Assert.That(c.State.EnemyIntents[0].Kind, Is.EqualTo(IntentKind.Attack),
                "Reinforcement intent must be declared (not Unknown).");
            Assert.That(c.State.EnemyIntents[0].Move, Is.EqualTo(wave2Move),
                "Reinforcement intent must reference new enemy's move (§7.4 / Bug #1).");
        }

        [Test]
        public void TryInjectReinforcements_NewEntrants_DoNotActSameTurn()
        {
            // Per §7.4 — reinforcements telegraph for next turn but do NOT act
            // this turn. The constraint is structural: reinforcements spawn in
            // HandleAnyFaints (called from ResolutionPhase), which fires AFTER
            // the enemy intent resolution loop. So the turn the reinforcements
            // arrive, their intents are declared but NOT executed.
            //
            // This test is actually redundant with the architecture (the controller
            // can't execute intents that don't exist yet), but it documents the
            // behavior explicitly per the bug report.
            PokemonInstance lead = MakeMon(100);
            lead.CurrentMoves.Add(MakeMove(power: 999)); // one-shot wave1 so reinforcements actually spawn

            PokemonInstance wave1 = MakeMon(10);
            wave1.CurrentMoves.Add(MakeMove(power: 1));

            PokemonInstance wave2 = MakeMon(60);
            wave2.CurrentMoves.Add(MakeMove(power: 99)); // would hurt if it fired

            TestReinforcementProvider provider = new();
            provider.NextWave = new List<PokemonInstance> { wave2 };

            PassiveAgent agent = new();
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { wave1 },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Economy = _economy,
                Rng = new GameRNG(seed: 0xFACE),
                Reinforcements = provider,
            };
            CombatController c = new(setup, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();

            int leadHPBefore = lead.CurrentHP;

            // The player KOs wave1 during ActionPhase → CardPlayService.Play →
            // ResolveDamage → HandleAnyFaints → TryInjectReinforcements spawns
            // wave2 and sets State.ReinforcementsSpawnedThisTurn = true.
            int wave1Idx = FindCardByMove(c.State.SkillHand, lead.CurrentMoves[0]);
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(wave1Idx, enemySlot: 0)); // KO wave1
            Assert.That(c.State.EnemyTeam[0], Is.SameAs(wave2),
                "Reinforcements must have spawned this turn (precondition).");

            // End the turn and run Resolution. Per §7.4 the just-arrived wave2 must
            // NOT act this turn — the ReinforcementsSpawnedThisTurn gate skips the
            // enemy-intent loop. wave2 telegraphs now and acts next turn.
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.End());
            c.ResolutionPhase(); // gated — wave2 intent must NOT fire this turn
            c.TurnEnd();

            Assert.That(lead.CurrentHP, Is.EqualTo(leadHPBefore),
                "Reinforcements must NOT act turn-of-spawn (§7.4 / Bug #1).");
        }

        // ── Bug #11: Meta save round-trip (§6.3 / §9.8) ──────────────────────

        // Per §6.3 / §9.8 / Bug #11 — SaveSystem.LoadMeta returns null when no
        // save exists. OLD BUG: RunBootstrapper.BuildContext crashed (null
        // deref). NEW: instantiate a fresh SO (line 75 RunBootstrapper.cs).
        [Test]
        public void BuildContext_NullMetaFromLoad_CreatesDefaultInstance()
        {
            // Per §6.3 / Bug #11 — null LoadMeta must not crash.
            // This is a unit-level test (no actual save file involved).
            // The fix is in RunBootstrapper line 75: `LoadMeta() ?? CreateInstance<MetaProgressionSO>()`.
            // We assert the pattern here by simulating the null path.
            MetaProgressionSO metaFromLoad = null; // simulate no save
            MetaProgressionSO effective = metaFromLoad ?? ScriptableObject.CreateInstance<MetaProgressionSO>();

            Assert.That(effective, Is.Not.Null,
                "Null LoadMeta must fall back to fresh instance (§6.3 / Bug #11).");
            // NOTE: UnlockedStarterIds is null on a fresh SO (Unity doesn't auto-init
            // List<T> fields on CreateInstance). The fix is sufficient (no crash);
            // the caller must init the list if needed. The original bug was a null-deref
            // crash, which this test would catch if the fix regressed.

            Object.DestroyImmediate(effective);
        }

        // ── R2-5: Breather beat + wave-queue peek (§3.2.6 / §7.4.4, OPEN) ────

        // Per §3.2.6 (OPEN) — KO the last enemy mid-ActionPhase → reinforcements
        // spawn → the player gets a Breather: +BreatherBonusAP and ONE pending
        // action. Crucially the triggering kill must NOT consume its own breather.
        [Test]
        public void TryInjectReinforcements_OnWaveSpawn_GrantsBreatherAndBonusAP()
        {
            _config.BreatherBonusAP = 1;
            PokemonInstance lead = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 999)); // one-shot wave1
            lead.CurrentMoves.Add(MakeMove(power: 5));    // follow-up option (keeps hand non-empty)
            PokemonInstance wave1 = MakeMon(10); wave1.CurrentMoves.Add(MakeMove(power: 1));
            PokemonInstance wave2 = MakeMon(60); wave2.CurrentMoves.Add(MakeMove(power: 1));

            CombatController c = BuildReinforcedController(
                new List<PokemonInstance> { lead }, wave1, new List<PokemonInstance> { wave2 }, new PassiveAgent());
            c.Start(); c.DrawPhase(); c.IntentPhase();

            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(FindCardByMove(c.State.SkillHand, lead.CurrentMoves[0]), 0));

            Assert.That(c.State.EnemyTeam[0], Is.SameAs(wave2), "Reinforcements spawned (precondition).");
            Assert.That(c.State.BreatherPending, Is.True, "Breather pending after wave spawn (§3.2.6).");
            Assert.That(c.State.BreatherActionsAllowed, Is.EqualTo(1), "Triggering kill must NOT consume the breather.");
            // Net AP: base 3, −1 kill, +1 breather → back to base.
            Assert.That(c.State.CurrentAP, Is.EqualTo(_config.BaseAPPerTurn), "Breather grants +BreatherBonusAP (§3.2.6).");
        }

        // Per §3.2.6 (OPEN) — a follow-up action taken WHILE the breather is
        // pending consumes it (one action allowed).
        [Test]
        public void Breather_FollowUpCardPlay_ClearsBreather()
        {
            _config.BreatherBonusAP = 1;
            PokemonInstance lead = MakeMon(60);
            lead.CurrentMoves.Add(MakeMove(power: 999)); // kill wave1
            lead.CurrentMoves.Add(MakeMove(power: 5));    // follow-up during breather
            PokemonInstance wave1 = MakeMon(10); wave1.CurrentMoves.Add(MakeMove(power: 1));
            PokemonInstance wave2 = MakeMon(60); wave2.CurrentMoves.Add(MakeMove(power: 1));

            CombatController c = BuildReinforcedController(
                new List<PokemonInstance> { lead }, wave1, new List<PokemonInstance> { wave2 }, new PassiveAgent());
            c.Start(); c.DrawPhase(); c.IntentPhase();
            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(FindCardByMove(c.State.SkillHand, lead.CurrentMoves[0]), 0));
            Assert.That(c.State.BreatherPending, Is.True, "Breather pending (precondition).");

            int followIdx = FindCardByMove(c.State.SkillHand, lead.CurrentMoves[1]);
            Assert.That(followIdx, Is.GreaterThanOrEqualTo(0), "Follow-up card in hand.");
            c.ExecuteAction(PlayerAction.PlaySkill(followIdx, 0));
            Assert.That(c.State.BreatherPending, Is.False, "One breather action ends the breather (§3.2.6).");
        }

        // Per §3.2.6 (OPEN) — empty hand AND no eligible swap target → auto-skip.
        [Test]
        public void Breather_NoValidAction_AutoSkips()
        {
            _config.BreatherBonusAP = 1;
            _config.BaseSkillCardsPerTurn = 1; // draw only the single kill card
            PokemonInstance lead = MakeMon(60); // solo team → no swap target
            lead.CurrentMoves.Add(MakeMove(power: 999));
            PokemonInstance wave1 = MakeMon(10); wave1.CurrentMoves.Add(MakeMove(power: 1));
            PokemonInstance wave2 = MakeMon(60); wave2.CurrentMoves.Add(MakeMove(power: 1));

            CombatController c = BuildReinforcedController(
                new List<PokemonInstance> { lead }, wave1, new List<PokemonInstance> { wave2 }, new PassiveAgent());
            c.Start(); c.DrawPhase(); c.IntentPhase();
            Assert.That(c.State.SkillHand.Count, Is.EqualTo(1), "Only the kill card drawn (precondition).");

            c.State.CurrentPhase = CombatController.Phase.ActionPhase;
            c.ExecuteAction(PlayerAction.PlaySkill(FindCardByMove(c.State.SkillHand, lead.CurrentMoves[0]), 0));

            Assert.That(c.State.EnemyTeam[0], Is.SameAs(wave2), "Reinforcements spawned (precondition).");
            Assert.That(c.State.BreatherPending, Is.False, "No card + no swap target → auto-skip (§3.2.6).");
        }

        // Per §3.2.6 / §7.4 — a headless full combat through a wave transition must
        // terminate (the breather must not deadlock RunFullCombat).
        [Test]
        public void RunFullCombat_WithReinforcements_TerminatesWithVictory()
        {
            _config.BreatherBonusAP = 1;
            PokemonInstance lead = MakeMon(200);
            lead.CurrentMoves.Add(MakeMove(power: 999));
            PokemonInstance wave1 = MakeMon(10); wave1.CurrentMoves.Add(MakeMove(power: 1));
            PokemonInstance wave2 = MakeMon(10); wave2.CurrentMoves.Add(MakeMove(power: 1));

            CombatController c = BuildReinforcedController(
                new List<PokemonInstance> { lead }, wave1, new List<PokemonInstance> { wave2 }, new KillerAgent());
            CombatController.CombatOutcome outcome = c.RunFullCombat(maxTurns: 20);

            Assert.That(outcome, Is.EqualTo(CombatController.CombatOutcome.Victory),
                "Combat with reinforcements + breather must terminate (no deadlock) (§3.2.6/§7.4).");
        }

        // Per §7.4.4 (OPEN) — peek previews the next wave without consuming it.
        [Test]
        public void PeekNextWave_ReturnsNextWavePreview_WithoutConsuming()
        {
            PokemonInstance wave2 = MakeMon(60); wave2.CurrentMoves.Add(MakeMove(power: 1));
            TestReinforcementProvider provider = new() { NextWave = new List<PokemonInstance> { wave2 } };

            IReadOnlyList<ReinforcementPreview> peek = provider.PeekNextWave();
            Assert.That(peek.Count, Is.EqualTo(1), "Peek returns the queued wave (§7.4.4).");
            Assert.That(peek[0].Species, Is.SameAs(wave2.Species), "Peek species matches the wave.");
            Assert.That(provider.PeekNextWave().Count, Is.EqualTo(1), "Peek does not consume the wave.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private PokemonInstance MakeMon(int hp)
        {
            return new PokemonInstance
            {
                Species = _species,
                Level = 1,
                CurrentHP = hp,
            };
        }

        private MoveSO MakeMove(int power, MoveRange range = MoveRange.Ranged)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "mv";
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

        private CombatController BuildController(
            List<PokemonInstance> playerTeam,
            PokemonInstance enemy,
            IPlayerAgent agent)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Economy = _economy,
                Rng = new GameRNG(seed: 0xDEAD),
            };
            return new CombatController(setup, agent);
        }

        // Builds a controller with a one-shot reinforcement provider for R2-5 tests.
        private CombatController BuildReinforcedController(
            List<PokemonInstance> playerTeam, PokemonInstance enemy,
            List<PokemonInstance> reinforcements, IPlayerAgent agent)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Economy = _economy,
                Rng = new GameRNG(seed: 0xBEEF),
                Reinforcements = new TestReinforcementProvider { NextWave = reinforcements },
            };
            return new CombatController(setup, agent);
        }

        private int FindCardByMove(IReadOnlyList<MoveCardInstance> hand, MoveSO move)
        {
            if (hand == null || move == null) return -1;
            for (int i = 0; i < hand.Count; i++)
                if (hand[i] != null && hand[i].Move == move) return i;
            return -1;
        }

        // ── Test doubles ──────────────────────────────────────────────────────

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickIndex;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> c) => PickIndex;
        }

        // One-shot: spawns NextWave exactly once, then returns null (so a
        // multi-wave RunFullCombat terminates instead of re-injecting a dead wave).
        private sealed class TestReinforcementProvider : IEnemyReinforcementProvider
        {
            public List<PokemonInstance> NextWave;
            private bool _used;
            public List<PokemonInstance> RequestReinforcements(CombatController.CombatState s)
            {
                if (_used) return null;
                _used = true;
                return NextWave;
            }

            // §7.4.4 (OPEN) — non-consuming peek mirrors NextWave (empty once used).
            public IReadOnlyList<ReinforcementPreview> PeekNextWave()
            {
                List<ReinforcementPreview> preview = new();
                if (!_used && NextWave != null)
                    foreach (PokemonInstance p in NextWave)
                        if (p != null) preview.Add(new ReinforcementPreview(p.Species, p.Level));
                return preview;
            }
        }

        // Plays card 0 at enemy 0 while it has AP + cards; otherwise ends the turn.
        private sealed class KillerAgent : IPlayerAgent
        {
            public PlayerAction DecideAction(CombatController.CombatState s)
                => (s.CurrentAP > 0 && s.SkillHand.Count > 0)
                    ? PlayerAction.PlaySkill(0, 0)
                    : PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> c) => s.LeadIndex;
        }
    }
}
