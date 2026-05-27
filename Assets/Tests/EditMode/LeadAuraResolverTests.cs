using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §5.5.4 + Epic 6 Task 6.6 — Lead Aura runtime coverage.
    //
    // Two layers:
    //   • LeadAuraResolver unit tests (pure-function multiplier).
    //   • CombatController integration: confirm the aura multiplier lands
    //     in ResolveDamage alongside fieldMul and freezeFireMul.
    public class LeadAuraResolverTests
    {
        private PokemonSpeciesSO _fireSpecies;
        private PokemonSpeciesSO _waterSpecies;
        private PokemonSpeciesSO _normalSpecies;
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _fireSpecies = MakeSpecies(PokemonType.Fire);
            _waterSpecies = MakeSpecies(PokemonType.Water);
            _normalSpecies = MakeSpecies(PokemonType.Normal);

            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.LeadAuraMatchingTypeBonus = 0.05f;
            _config.BaseAPPerTurn = 3;
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

        private PokemonSpeciesSO MakeSpecies(PokemonType primary)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType> { primary };
            s.BaseStats = new BaseStats { BaseHP = 60, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(s);
            return s;
        }

        private AbilitySO MakeAuraAbility(PokemonType type)
        {
            AbilitySO a = ScriptableObject.CreateInstance<AbilitySO>();
            a.AbilityId = "test-aura";
            a.Category = AbilityCategory.Aura;
            a.GrantsLeadAura = true;
            a.LeadAuraType = type;
            _disposables.Add(a);
            return a;
        }

        private HeldItemSO MakeAuraItem(PokemonType type)
        {
            HeldItemSO i = ScriptableObject.CreateInstance<HeldItemSO>();
            i.Id = "test-plate";
            i.GrantsLeadAura = true;
            i.LeadAuraType = type;
            _disposables.Add(i);
            return i;
        }

        private MoveSO MakeMove(PokemonType type, MoveRange range = MoveRange.Ranged,
                                int power = 40, int ap = 1)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "m";
            m.Type = type;
            m.BasePower = power;
            m.APCost = ap;
            m.Role = MoveRole.Offensive;
            m.Range = range;
            m.RangeModifierMultiplier = range == MoveRange.Ranged ? 0.75f : 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonInstance MakeMon(PokemonSpeciesSO species, int hp = 60) =>
            new() { Species = species, Level = 1, CurrentHP = hp };

        // ── Unit tests ───────────────────────────────────────────────────────

        [Test]
        public void Multiplier_NoAura_Returns1()
        {
            PokemonInstance lead = MakeMon(_normalSpecies);
            PokemonInstance bench = MakeMon(_normalSpecies);
            MoveSO move = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                bench, move, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(mul, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Multiplier_AbilityAuraMatchesMoveType_Plus5Percent()
        {
            // Charizard-like: Fire ability aura. Bench plays Fire move → +5%.
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            PokemonInstance bench = MakeMon(_normalSpecies);
            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                bench, fireMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(mul, Is.EqualTo(1.05f).Within(1e-4f));
        }

        [Test]
        public void Multiplier_HeldItemAuraMatchesMoveType_Plus5Percent()
        {
            // Charcoal-like: Fire item aura.
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.HeldItem = MakeAuraItem(PokemonType.Fire);
            PokemonInstance bench = MakeMon(_normalSpecies);
            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                bench, fireMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(mul, Is.EqualTo(1.05f).Within(1e-4f));
        }

        // Per the design call confirmed via AskUserQuestion — additive stacking.
        [Test]
        public void Multiplier_AbilityAndItemBothFire_StackToPlus10Percent()
        {
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            lead.HeldItem = MakeAuraItem(PokemonType.Fire);
            PokemonInstance bench = MakeMon(_normalSpecies);
            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                bench, fireMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(mul, Is.EqualTo(1.10f).Within(1e-4f),
                "Two same-type Aura sources stack additively (+0.10).");
        }

        [Test]
        public void Multiplier_AbilityFireItemWater_FireMoveOnlyGetsAbilityBonus()
        {
            // Different types → each applies to its own type independently.
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            lead.HeldItem = MakeAuraItem(PokemonType.Water);
            PokemonInstance bench = MakeMon(_normalSpecies);

            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float fireMul = LeadAuraResolver.GetDamageMultiplier(
                bench, fireMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(fireMul, Is.EqualTo(1.05f).Within(1e-4f));

            MoveSO waterMove = MakeMove(PokemonType.Water);
            float waterMul = LeadAuraResolver.GetDamageMultiplier(
                bench, waterMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(waterMul, Is.EqualTo(1.05f).Within(1e-4f));

            MoveSO grassMove = MakeMove(PokemonType.Grass);
            float grassMul = LeadAuraResolver.GetDamageMultiplier(
                bench, grassMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(grassMul, Is.EqualTo(1f).Within(1e-4f),
                "Unmatched move type gets no bonus.");
        }

        [Test]
        public void Multiplier_LeadIsAttacker_NoBonus()
        {
            // The Lead playing their own Fire move does NOT receive the aura
            // — auras buff bench attackers only (§5.5.4 design rationale).
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                lead, fireMove, lead, new List<PokemonInstance> { lead }, _config);
            Assert.That(mul, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Multiplier_AttackerNotInTeam_NoBonus()
        {
            // Enemy attacker: not a member of attackerTeam → returns 1.
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            PokemonInstance enemy = MakeMon(_fireSpecies);
            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                enemy, fireMove, lead, new List<PokemonInstance> { lead }, _config);
            Assert.That(mul, Is.EqualTo(1f).Within(1e-4f),
                "Enemy attackers are never buffed by the player's Lead Aura.");
        }

        [Test]
        public void Multiplier_FaintedLead_GrantsNoAura()
        {
            PokemonInstance lead = MakeMon(_fireSpecies, hp: 0);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            PokemonInstance bench = MakeMon(_normalSpecies);
            MoveSO fireMove = MakeMove(PokemonType.Fire);
            float mul = LeadAuraResolver.GetDamageMultiplier(
                bench, fireMove, lead, new List<PokemonInstance> { lead, bench }, _config);
            Assert.That(mul, Is.EqualTo(1f).Within(1e-4f),
                "Fainted Lead must not broadcast an aura (§2.4.1).");
        }

        [Test]
        public void Multiplier_NullArgs_Returns1()
        {
            PokemonInstance lead = MakeMon(_fireSpecies);
            PokemonInstance bench = MakeMon(_normalSpecies);
            MoveSO move = MakeMove(PokemonType.Fire);
            List<PokemonInstance> team = new() { lead, bench };

            Assert.That(LeadAuraResolver.GetDamageMultiplier(null, move, lead, team, _config),
                Is.EqualTo(1f));
            Assert.That(LeadAuraResolver.GetDamageMultiplier(bench, null, lead, team, _config),
                Is.EqualTo(1f));
            Assert.That(LeadAuraResolver.GetDamageMultiplier(bench, move, null, team, _config),
                Is.EqualTo(1f));
            Assert.That(LeadAuraResolver.GetDamageMultiplier(bench, move, lead, null, _config),
                Is.EqualTo(1f));
            Assert.That(LeadAuraResolver.GetDamageMultiplier(bench, move, lead, team, null),
                Is.EqualTo(1f));
        }

        // ── GetActiveAuraTypes (UI / debug accessor) ─────────────────────────

        [Test]
        public void GetActiveAuraTypes_TwoSameTypeSources_CountIsTwo()
        {
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            lead.HeldItem = MakeAuraItem(PokemonType.Fire);
            Dictionary<PokemonType, int> map = LeadAuraResolver.GetActiveAuraTypes(lead);
            Assert.That(map.ContainsKey(PokemonType.Fire), Is.True);
            Assert.That(map[PokemonType.Fire], Is.EqualTo(2));
        }

        [Test]
        public void GetActiveAuraTypes_TwoDifferentTypes_BothEntriesPresent()
        {
            PokemonInstance lead = MakeMon(_fireSpecies);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            lead.HeldItem = MakeAuraItem(PokemonType.Water);
            Dictionary<PokemonType, int> map = LeadAuraResolver.GetActiveAuraTypes(lead);
            Assert.That(map.Count, Is.EqualTo(2));
            Assert.That(map[PokemonType.Fire], Is.EqualTo(1));
            Assert.That(map[PokemonType.Water], Is.EqualTo(1));
        }

        [Test]
        public void GetActiveAuraTypes_FaintedLead_ReturnsEmpty()
        {
            PokemonInstance lead = MakeMon(_fireSpecies, hp: 0);
            lead.Ability = MakeAuraAbility(PokemonType.Fire);
            Assert.That(LeadAuraResolver.GetActiveAuraTypes(lead), Is.Empty);
        }

        [Test]
        public void GetActiveAuraTypes_NullLead_ReturnsEmpty()
        {
            Assert.That(LeadAuraResolver.GetActiveAuraTypes(null), Is.Empty);
        }

        // ── CombatController integration ────────────────────────────────────

        // Pins the wiring: with the aura present, the BENCH attacker's
        // damage strictly exceeds the no-aura baseline by the configured
        // multiplier. Uses a min-power Ranged move to keep arithmetic
        // dominated by the multiplier rather than rounding noise.
        [Test]
        public void Integration_BenchAttackerWithAura_DealsMoreDamage()
        {
            // Two enemies, identical HP, identical attacker. One run with
            // Aura, one without. The aura'd run must do strictly more damage.
            int dmgWithoutAura = SimulateBenchStrike(applyAura: false);
            int dmgWithAura = SimulateBenchStrike(applyAura: true);
            Assert.That(dmgWithAura, Is.GreaterThan(dmgWithoutAura),
                "Lead Aura must boost bench attacker damage (§5.5.4).");
        }

        [Test]
        public void Integration_LeadAttackerWithAura_DealsBaselineDamage()
        {
            // Lead playing its own move while it has an aura → no buff.
            int dmgLeadAttacker = SimulateLeadStrike(applyAura: true);
            int dmgLeadAttackerNoAura = SimulateLeadStrike(applyAura: false);
            Assert.That(dmgLeadAttacker, Is.EqualTo(dmgLeadAttackerNoAura),
                "Lead's own moves bypass the aura (§5.5.4 design rationale).");
        }

        // Helper that builds a 2-mon combat where the Lead has an aura,
        // the bench plays a matching Fire move, and we record the damage.
        private int SimulateBenchStrike(bool applyAura)
        {
            // Pristine config per call (BattleConfigSO is mutable).
            BattleConfigSO cfg = ScriptableObject.CreateInstance<BattleConfigSO>();
            cfg.Divisor = 1;
            cfg.StabMultiplier = 1f;
            cfg.CritMultiplier = 1f;
            cfg.MeleeModifier = 1f;
            cfg.RangedModifier = 0.75f;
            cfg.BaseAPPerTurn = 6;
            cfg.MaxAPPerTurn = 6;
            cfg.BaseSkillCardsPerTurn = 6;
            cfg.BaseConsumableCardsPerTurn = 0;
            cfg.LeadAuraMatchingTypeBonus = 0.20f;  // amplify so floor() can resolve it
            cfg.StatStageMultipliers = new float[] {
                0.25f,0.29f,0.33f,0.40f,0.50f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f };
            _disposables.Add(cfg);

            MoveSO fireMove = MakeMove(PokemonType.Fire, MoveRange.Ranged, power: 20, ap: 1);

            PokemonInstance lead = MakeMon(_normalSpecies);
            PokemonInstance bench = MakeMon(_normalSpecies);
            bench.CurrentMoves.Add(fireMove);
            if (applyAura) lead.Ability = MakeAuraAbility(PokemonType.Fire);
            PokemonInstance enemy = MakeMon(_normalSpecies, hp: 1000);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead, bench },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = cfg,
                Rng = new GameRNG(seed: 0x42),
            };
            CombatController c = new(setup, new StubAgent());
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            int idx = FindHandIndex(c, fireMove, bench);
            Assume.That(idx, Is.GreaterThanOrEqualTo(0));
            int hpBefore = enemy.CurrentHP;
            c.ExecuteAction(PlayerAction.PlaySkill(idx, enemySlot: 0));
            return hpBefore - enemy.CurrentHP;
        }

        private int SimulateLeadStrike(bool applyAura)
        {
            BattleConfigSO cfg = ScriptableObject.CreateInstance<BattleConfigSO>();
            cfg.Divisor = 1;
            cfg.StabMultiplier = 1f;
            cfg.CritMultiplier = 1f;
            cfg.MeleeModifier = 1f;
            cfg.RangedModifier = 0.75f;
            cfg.BaseAPPerTurn = 6;
            cfg.MaxAPPerTurn = 6;
            cfg.BaseSkillCardsPerTurn = 6;
            cfg.BaseConsumableCardsPerTurn = 0;
            cfg.LeadAuraMatchingTypeBonus = 0.20f;
            cfg.StatStageMultipliers = new float[] {
                0.25f,0.29f,0.33f,0.40f,0.50f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f };
            _disposables.Add(cfg);

            MoveSO fireMove = MakeMove(PokemonType.Fire, MoveRange.Ranged, power: 20, ap: 1);

            PokemonInstance lead = MakeMon(_normalSpecies);
            lead.CurrentMoves.Add(fireMove);
            if (applyAura) lead.Ability = MakeAuraAbility(PokemonType.Fire);
            PokemonInstance enemy = MakeMon(_normalSpecies, hp: 1000);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = cfg,
                Rng = new GameRNG(seed: 0x43),
            };
            CombatController c = new(setup, new StubAgent());
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            int idx = FindHandIndex(c, fireMove, lead);
            Assume.That(idx, Is.GreaterThanOrEqualTo(0));
            int hpBefore = enemy.CurrentHP;
            c.ExecuteAction(PlayerAction.PlaySkill(idx, enemySlot: 0));
            return hpBefore - enemy.CurrentHP;
        }

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

        private sealed class StubAgent : IPlayerAgent
        {
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => s.LeadIndex;
        }
    }
}
