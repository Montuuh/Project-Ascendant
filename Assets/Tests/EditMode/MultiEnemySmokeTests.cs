using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §4.3.6 + Epic 8 Task 8.6 — multi-enemy architecture SMOKE.
    // True multi-enemy (1 Lead + 1–2 supports, simultaneous) is a Region-3
    // accent; the VS only proves the architecture is compatible:
    //   • 8.6.1 data structure — EnemyTeam holds N enemies; intents are built
    //     one-per-enemy (revealed simultaneously in the Intent Phase).
    //   • 8.6.2 resolution order — supports (slots 1+) first in slot order,
    //     Lead enemy (slot 0) last.
    //   • 8.6.3 per-enemy target selection — the player addresses a specific
    //     enemy slot per offensive card (UI binding is Epic 13).
    //   • 8.6.4 smoke test only — no R1 multi-enemy content is authored.
    public class MultiEnemySmokeTests
    {
        private BattleConfigSO _config;
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
            _config.BurnDoTDivisor = 16;
            _config.BurnAttackMultiplier = 0.75f;
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

        private MoveSO Mk(int power)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "mv"; m.Type = PokemonType.Normal; m.BasePower = power; m.APCost = 1;
            m.Role = MoveRole.Offensive; m.Range = MoveRange.Melee; m.Modifier = PositionalModifier.None;
            m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonSpeciesSO Sp(int hp)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "sp"; s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 40, BaseDef = 40, BaseSpd = 40 };
            s.GrowthCurve = null; s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private PokemonInstance Enemy(int hp, MoveSO move)
        {
            PokemonInstance e = new() { Species = Sp(hp), Level = 1, CurrentHP = hp };
            if (move != null) e.CurrentMoves.Add(move);
            return e;
        }

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        private CombatController Build(List<PokemonInstance> player, List<PokemonInstance> enemies)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = player,
                InitialLeadIndex = 0,
                EnemyTeam = enemies,
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(0x6060u),
            };
            CombatController c = new(setup, new PassiveAgent());
            c.Start();
            return c;
        }

        // ── 8.6.1: simultaneous intents over an N-enemy team ─────────────────

        [Test]
        public void ThreeEnemies_AllReceiveIntentsSimultaneously()
        {
            MoveSO atk = Mk(20);
            var enemies = new List<PokemonInstance> { Enemy(100, atk), Enemy(100, atk), Enemy(100, atk) };
            PokemonInstance lead = new() { Species = Sp(500), Level = 1, CurrentHP = 500 };
            CombatController c = Build(new List<PokemonInstance> { lead }, enemies);

            c.DrawPhase();
            c.IntentPhase();

            Assert.That(c.State.EnemyIntents.Count, Is.EqualTo(3), "One intent per enemy.");
            foreach (Intent it in c.State.EnemyIntents)
                Assert.That(it.Kind, Is.EqualTo(IntentKind.Attack));

            int before = lead.CurrentHP;
            c.ResolutionPhase();
            Assert.That(lead.CurrentHP, Is.LessThan(before), "All three enemies acted on the Lead.");
        }

        // ── 8.6.2: supports first, Lead enemy last ───────────────────────────

        [Test]
        public void ResolutionOrder_SupportResolvesBeforeLeadEnemy()
        {
            // Probe: the support (slot 1) Burns the player Lead; the Lead enemy
            // (slot 0) lands a lethal hit that ends combat. If supports resolve
            // FIRST, the Burn is applied before the combat-ending hit — so the
            // fainted Lead carries Burn. Lead-first would end combat before the
            // support ever acts (no Burn).
            MoveSO lethal = Mk(100); // ≥5 dmg at Divisor 10
            PokemonInstance leadEnemy = Enemy(100, lethal); // slot 0 = Lead enemy
            PokemonInstance support = Enemy(100, Mk(10));   // slot 1 = support
            PokemonInstance pLead = new() { Species = Sp(100), Level = 1, CurrentHP = 5 };

            CombatController c = Build(new List<PokemonInstance> { pLead },
                                       new List<PokemonInstance> { leadEnemy, support });

            // Hand-author intents (the AI builder only emits Attacks): index is
            // parallel to EnemyTeam — [0]=Lead enemy lethal Attack, [1]=support
            // Status(Burn). ResolutionPhase must run slot 1 before slot 0.
            c.State.EnemyIntents.Clear();
            c.State.EnemyIntents.Add(new Intent
            {
                Kind = IntentKind.Attack, Move = lethal, TargetSlot = 0, Reveal = IntentReveal.Witnessed
            });
            c.State.EnemyIntents.Add(new Intent
            {
                Kind = IntentKind.Status, AppliedStatus = StatusCondition.Burn,
                TargetSlot = 0, Reveal = IntentReveal.Witnessed
            });

            c.ResolutionPhase();

            Assert.That(pLead.CurrentHP, Is.EqualTo(0), "Lead enemy's lethal hit resolved.");
            Assert.That(pLead.PrimaryStatus, Is.EqualTo(StatusCondition.Burn),
                "Per §4.3.6 — the support resolved BEFORE the Lead enemy's combat-ending hit.");
        }

        // ── 8.6.3: per-enemy target selection ────────────────────────────────

        [Test]
        public void PlayerCard_TargetsSpecificEnemySlot_OnlyThatEnemyTakesDamage()
        {
            MoveSO atk = Mk(40);
            PokemonInstance lead = new() { Species = Sp(100), Level = 1, CurrentHP = 100 };
            lead.CurrentMoves.Add(atk);
            var enemies = new List<PokemonInstance> { Enemy(100, Mk(10)), Enemy(100, Mk(10)), Enemy(100, Mk(10)) };

            CombatController c = Build(new List<PokemonInstance> { lead }, enemies);
            c.DrawPhase();
            c.IntentPhase();

            int handIdx = -1;
            for (int i = 0; i < c.State.SkillHand.Count; i++)
                if (c.State.SkillHand[i] != null && c.State.SkillHand[i].Move == atk) { handIdx = i; break; }
            Assert.That(handIdx, Is.GreaterThanOrEqualTo(0), "Lead's move must be in hand.");

            c.ExecuteAction(PlayerAction.PlaySkill(handIdx, enemySlot: 2));

            Assert.That(enemies[2].CurrentHP, Is.LessThan(100), "Targeted enemy (slot 2) took damage.");
            Assert.That(enemies[0].CurrentHP, Is.EqualTo(100), "Slot 0 untouched.");
            Assert.That(enemies[1].CurrentHP, Is.EqualTo(100), "Slot 1 untouched.");
        }

        // ── Regression: single-enemy resolution unchanged ────────────────────

        [Test]
        public void SingleEnemy_StillResolvesNormally()
        {
            MoveSO atk = Mk(40);
            PokemonInstance lead = new() { Species = Sp(500), Level = 1, CurrentHP = 500 };
            CombatController c = Build(new List<PokemonInstance> { lead },
                                       new List<PokemonInstance> { Enemy(100, atk) });
            c.DrawPhase();
            c.IntentPhase();
            int before = lead.CurrentHP;
            c.ResolutionPhase();
            Assert.That(lead.CurrentHP, Is.LessThan(before),
                "Single-enemy (slot-0-only) resolution is unchanged by the supports-first reorder.");
        }
    }
}
