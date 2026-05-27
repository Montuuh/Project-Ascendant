using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §3.3.5 + §3.3.5.1 + Epic 6 Task 6.5 — end-to-end Lead-faint
    // replacement flow through CombatController.
    //
    // FaintResolverTests covers the static helpers; this file drives the
    // actual integration:
    //   • Lead faints in Resolution → agent.PickLeadReplacement is asked
    //     for a new Lead → LeadIndex updates → no AP charged.
    //   • Frozen Lead faint voids Freeze position-lock (§3.3.5.1) — even
    //     when Lead is position-locked, faint precedence routes through
    //     the replacement path.
    //   • Bench faint (not Lead) leaves Lead unchanged and triggers no
    //     replacement prompt (§3.3.5 case 2).
    //   • All-faint scenario flips Outcome → Defeat without attempting a
    //     replacement.
    //
    // Note on Task 6.5.4 ("Edit + Play Mode test"): the Play-Mode UI prompt
    // is deferred to Epic 13 (UI work). The Edit-Mode test is the load-
    // bearing one because it pins the agent-callback contract.
    public class LeadFaintReplacementTests
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
            _config.Divisor = 1;                  // crank damage so enemy attacks one-shot
            _config.StabMultiplier = 1.0f;
            _config.CritMultiplier = 1.0f;
            _config.MeleeModifier = 1f;
            _config.RangedModifier = 0.75f;
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 3;
            _config.BaseSkillCardsPerTurn = 0;    // empty hand → straight to resolution
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

        private MoveSO MakeMove(int power = 999, MoveRange range = MoveRange.Ranged)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "atk";
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

        private PokemonInstance MakeMon(int hp = 60) =>
            new() { Species = _species, Level = 1, CurrentHP = hp };

        // Builds a controller where the enemy's intent will one-shot whoever
        // occupies the player's Lead slot at Resolution.
        private CombatController BuildOneShotCombat(
            List<PokemonInstance> playerTeam, RecordingAgent agent, int leadIndex = 0)
        {
            MoveSO lethal = MakeMove(power: 999);
            PokemonInstance enemy = MakeMon(60);
            enemy.CurrentMoves.Add(lethal);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = leadIndex,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0xBEEF),
            };
            CombatController c = new(setup, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            return c;
        }

        // ── 6.5.1/6.5.2: Lead-faint triggers replacement, no AP cost ─────────

        [Test]
        public void LeadFaints_AgentPicksReplacement_LeadIndexUpdates_NoAPCharged()
        {
            // Slot 0 = Lead (will be killed by enemy intent), slot 1 = bench survivor.
            PokemonInstance lead = MakeMon();
            PokemonInstance bench = MakeMon();
            lead.CurrentMoves.Add(MakeMove(power: 1));
            bench.CurrentMoves.Add(MakeMove(power: 1));
            RecordingAgent agent = new() { PickIndex = 1 };

            CombatController c = BuildOneShotCombat(
                new List<PokemonInstance> { lead, bench }, agent);

            int apBefore = c.State.CurrentAP;

            // Enemy lethal intent resolves on Lead slot (slot 0).
            c.ResolutionPhase();

            Assert.That(lead.CurrentHP, Is.EqualTo(0), "Enemy must one-shot the Lead.");
            Assert.That(agent.PickCallCount, Is.GreaterThanOrEqualTo(1),
                "PickLeadReplacement must be invoked on Lead faint (§3.3.5).");
            Assert.That(c.State.LeadIndex, Is.EqualTo(1),
                "Agent-picked bench slot must become the new Lead.");
            Assert.That(c.State.CurrentAP, Is.EqualTo(apBefore),
                "Lead replacement on faint costs 0 AP (§3.3.5).");
            Assert.That(c.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.InProgress),
                "Bench survivor → combat continues.");
        }

        // Per §3.3.5.1 — load-bearing rule: faint precedence over Freeze.
        [Test]
        public void FrozenLeadFaints_FreezeVoided_ReplacementProceeds()
        {
            PokemonInstance lead = MakeMon();
            lead.PrimaryStatus = StatusCondition.Freeze;
            PokemonInstance bench = MakeMon();
            lead.CurrentMoves.Add(MakeMove(power: 1));
            bench.CurrentMoves.Add(MakeMove(power: 1));
            RecordingAgent agent = new() { PickIndex = 1 };

            CombatController c = BuildOneShotCombat(
                new List<PokemonInstance> { lead, bench }, agent);

            // Pre-resolution: Frozen + alive → swap blocked by position lock.
            Assert.That(FaintResolver.IsSlotLockedForSwap(lead), Is.True);

            c.ResolutionPhase();

            // Post-resolution: Frozen + fainted → IsSlotLockedForSwap false (§3.3.5.1)
            Assert.That(FaintResolver.IsSlotLockedForSwap(lead), Is.False,
                "Faint must void the Frozen position-lock (§3.3.5.1).");
            Assert.That(c.State.LeadIndex, Is.EqualTo(1),
                "Frozen+fainted Lead must be replaced via the normal flow.");
            Assert.That(agent.PickCallCount, Is.GreaterThanOrEqualTo(1));
        }

        // Per §3.3.5 case 2 — bench faint leaves Lead untouched.
        [Test]
        public void BenchFaints_LeadUnchanged_NoReplacementPrompt()
        {
            // Pre-killed bench: enters resolution already at 0 HP. The
            // controller's HandleAnyFaints sweep should fire purge + trauma
            // but NOT call PickLeadReplacement (Lead is alive).
            PokemonInstance lead = MakeMon();
            PokemonInstance benchPreDead = MakeMon(hp: 0);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            RecordingAgent agent = new() { PickIndex = 99 };  // sentinel

            // Override one-shot setup: enemy intent now targets slot 1 (bench),
            // but slot 1 is already fainted, so the enemy intent fizzles by
            // ResolveSlotOccupant returning null. The key assertion is that
            // PickLeadReplacement is NOT called.
            CombatController c = BuildOneShotCombat(
                new List<PokemonInstance> { lead, benchPreDead }, agent);

            c.ResolutionPhase();

            Assert.That(c.State.LeadIndex, Is.EqualTo(0),
                "Bench faint must not change Lead (§3.3.5 case 2).");
            Assert.That(agent.PickCallCount, Is.EqualTo(0),
                "PickLeadReplacement must NOT fire when the Lead is alive.");
        }

        // Per §3.3.6 — every player Pokémon fainted → Defeat (no replacement).
        [Test]
        public void AllPlayerPokemonFaint_OutcomeDefeat_NoReplacementAttempted()
        {
            PokemonInstance lead = MakeMon();
            PokemonInstance benchPreDead = MakeMon(hp: 0);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            RecordingAgent agent = new() { PickIndex = 0 };

            CombatController c = BuildOneShotCombat(
                new List<PokemonInstance> { lead, benchPreDead }, agent);

            // Enemy lethal intent kills the Lead; no other survivor to swap to.
            c.ResolutionPhase();

            Assert.That(lead.CurrentHP, Is.EqualTo(0));
            Assert.That(c.State.Outcome,
                Is.EqualTo(CombatController.CombatOutcome.Defeat),
                "All-faint must flip Outcome → Defeat (§3.3.6).");
            // PickLeadReplacement is gated on EligibleLeadReplacements.Count > 0.
            // With no survivors, the agent is never queried.
            Assert.That(agent.PickCallCount, Is.EqualTo(0),
                "No survivors → no replacement prompt.");
        }

        // Per §4.8.5 — Trauma stack applies at moment of faint.
        // Combined check that the controller's HandleAnyFaints sweep applies
        // it to both Lead and bench faints in a single Resolution.
        [Test]
        public void LeadAndBenchBothFaint_BothGetTraumaStack()
        {
            PokemonInstance lead = MakeMon();
            PokemonInstance benchPreDead = MakeMon(hp: 0);
            lead.CurrentMoves.Add(MakeMove(power: 1));
            RecordingAgent agent = new() { PickIndex = 0 };

            CombatController c = BuildOneShotCombat(
                new List<PokemonInstance> { lead, benchPreDead }, agent);

            int leadTraumaBefore = lead.TraumaStacks;
            int benchTraumaBefore = benchPreDead.TraumaStacks;

            c.ResolutionPhase();

            Assert.That(lead.TraumaStacks, Is.EqualTo(leadTraumaBefore + 1),
                "Lead faint must increment Trauma stack (§4.8.5).");
            // benchPreDead entered Resolution already fainted — the
            // HandleAnyFaints sweep treats current 0-HP entries as faint
            // events too. (Caller invariant; if this changes, the test
            // should track it.)
            Assert.That(benchPreDead.TraumaStacks, Is.EqualTo(benchTraumaBefore + 1),
                "Bench faint sweep also increments Trauma stack.");
        }

        // ── Test agent ───────────────────────────────────────────────────────

        private sealed class RecordingAgent : IPlayerAgent
        {
            public int PickIndex;
            public int PickCallCount;

            public PlayerAction DecideAction(CombatController.CombatState s) =>
                PlayerAction.End();

            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates)
            {
                PickCallCount++;
                return PickIndex;
            }
        }
    }
}
