using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §5.5 / §5.8 + Epic 10 Task 10.9 — passive-ability runtime (damage-axis abilities).
    public class AbilityResolverTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>(); _disposables.Add(o); return o;
        }

        private BattleConfigSO Cfg()
        {
            BattleConfigSO c = Make<BattleConfigSO>();
            c.AbilityLowHpBoostMultiplier = 1.2f;
            c.AbilityLowHpThreshold = 0.30f;
            c.ShellArmorFlatReduction = 2;
            return c;
        }

        private AbilitySO Ability(string id)
        {
            AbilitySO a = Make<AbilitySO>(); a.AbilityId = id; return a;
        }

        private MoveSO Move(PokemonType type)
        {
            MoveSO m = Make<MoveSO>(); m.Type = type; m.BasePower = 40; return m;
        }

        // CurrentHP out of a 100 MaxHP (no growth curve).
        private PokemonInstance Mon(AbilitySO ability, int currentHp)
        {
            PokemonSpeciesSO sp = Make<PokemonSpeciesSO>();
            sp.BaseStats = new BaseStats { BaseHP = 100, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            sp.Types = new List<PokemonType> { PokemonType.Normal };
            return new PokemonInstance { Species = sp, Level = 10, CurrentHP = currentHp, Ability = ability };
        }

        // ── Overgrow / Blaze / Torrent (§5.5.3.4) ─────────────────────────────

        [Test]
        public void Overgrow_BoostsGrass_OnlyBelowThreshold()
        {
            BattleConfigSO cfg = Cfg();
            MoveSO grass = Move(PokemonType.Grass);

            PokemonInstance low = Mon(Ability("overgrow"), 20);  // 20% < 30%
            PokemonInstance high = Mon(Ability("overgrow"), 50); // 50% ≥ 30%

            Assert.That(AbilityResolver.OutgoingDamageMultiplier(low, grass, cfg), Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(AbilityResolver.OutgoingDamageMultiplier(high, grass, cfg), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Overgrow_DoesNotBoostOtherTypes()
        {
            BattleConfigSO cfg = Cfg();
            PokemonInstance low = Mon(Ability("overgrow"), 10);
            Assert.That(AbilityResolver.OutgoingDamageMultiplier(low, Move(PokemonType.Fire), cfg), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void BlazeAndTorrent_BoostTheirTypes()
        {
            BattleConfigSO cfg = Cfg();
            PokemonInstance blaze = Mon(Ability("blaze"), 10);
            PokemonInstance torrent = Mon(Ability("torrent"), 10);
            Assert.That(AbilityResolver.OutgoingDamageMultiplier(blaze, Move(PokemonType.Fire), cfg), Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(AbilityResolver.OutgoingDamageMultiplier(torrent, Move(PokemonType.Water), cfg), Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void NoAbility_NoBoost()
        {
            Assert.That(AbilityResolver.OutgoingDamageMultiplier(Mon(null, 5), Move(PokemonType.Grass), Cfg()), Is.EqualTo(1f).Within(0.0001f));
        }

        // ── Shell Armor (§5.5.3) ──────────────────────────────────────────────

        [Test]
        public void ShellArmor_FlatReduction()
        {
            BattleConfigSO cfg = Cfg();
            Assert.That(AbilityResolver.IncomingFlatReduction(Mon(Ability("shell_armor"), 100), cfg), Is.EqualTo(2));
            Assert.That(AbilityResolver.IncomingFlatReduction(Mon(Ability("overgrow"), 100), cfg), Is.EqualTo(0));
        }

        // ── Levitate (§5.5.3.3) ───────────────────────────────────────────────

        [Test]
        public void Levitate_ImmuneToGroundOnly()
        {
            PokemonInstance lev = Mon(Ability("levitate"), 100);
            Assert.That(AbilityResolver.IsImmuneTo(lev, Move(PokemonType.Ground)), Is.True);
            Assert.That(AbilityResolver.IsImmuneTo(lev, Move(PokemonType.Water)), Is.False);
            Assert.That(AbilityResolver.IsImmuneTo(Mon(null, 100), Move(PokemonType.Ground)), Is.False);
        }

        // ── Sturdy (§5.5.3 / §4.4.3) ──────────────────────────────────────────

        [Test]
        public void Sturdy_AbilityOrFlag()
        {
            Assert.That(AbilityResolver.HasSturdy(Mon(Ability("sturdy"), 100)), Is.True);
            PokemonInstance flagged = Mon(null, 100); flagged.HasSturdy = true;
            Assert.That(AbilityResolver.HasSturdy(flagged), Is.True);
            Assert.That(AbilityResolver.HasSturdy(Mon(null, 100)), Is.False);
        }

        // ── Non-damage hooks (§5.5.3) ─────────────────────────────────────────

        [Test]
        public void KeenEye_TeamRevealDetection()
        {
            var team = new List<PokemonInstance> { Mon(Ability("overgrow"), 100), Mon(Ability("keen_eye"), 100) };
            Assert.That(AbilityResolver.TeamRevealsIntents(team), Is.True);
            Assert.That(AbilityResolver.TeamRevealsIntents(new List<PokemonInstance> { Mon(Ability("overgrow"), 100) }), Is.False);
        }

        [Test]
        // §5.5.3 Static — gated to (ability == static) AND (move type == Electric); chance via config.
        public void Static_GatedByAbilityAndType()
        {
            BattleConfigSO always = Cfg(); always.StaticParalysisChancePercent = 100;
            BattleConfigSO never = Cfg(); never.StaticParalysisChancePercent = 0;
            GameRNG rng = new(1u);

            PokemonInstance stat = Mon(Ability("static"), 100);
            Assert.That(AbilityResolver.RollStaticParalysis(stat, Move(PokemonType.Electric), always, rng), Is.True);
            Assert.That(AbilityResolver.RollStaticParalysis(stat, Move(PokemonType.Electric), never, rng), Is.False);
            Assert.That(AbilityResolver.RollStaticParalysis(stat, Move(PokemonType.Water), always, rng), Is.False);
            Assert.That(AbilityResolver.RollStaticParalysis(Mon(Ability("overgrow"), 100), Move(PokemonType.Electric), always, rng), Is.False);
        }

        [Test]
        // §5.5.3 Swift Swim — turn-1 Rain only.
        public void SwiftSwim_Turn1RainOnly()
        {
            BattleConfigSO cfg = Cfg(); cfg.SwiftSwimDrawBonus = 1;
            var team = new List<PokemonInstance> { Mon(Ability("swift_swim"), 100) };
            FieldState rain = new() { Weather = FieldEffectKind.RainDance };
            FieldState clear = FieldState.Empty;

            Assert.That(AbilityResolver.SwiftSwimDrawBonus(team, rain, 1, cfg), Is.EqualTo(1));
            Assert.That(AbilityResolver.SwiftSwimDrawBonus(team, rain, 2, cfg), Is.EqualTo(0), "Only turn 1.");
            Assert.That(AbilityResolver.SwiftSwimDrawBonus(team, clear, 1, cfg), Is.EqualTo(0), "Only in Rain.");
            var noSwim = new List<PokemonInstance> { Mon(Ability("overgrow"), 100) };
            Assert.That(AbilityResolver.SwiftSwimDrawBonus(noSwim, rain, 1, cfg), Is.EqualTo(0));
        }

        [Test]
        // §5.5.3.5 Intimidate — entering Lead lowers every live enemy's Attack one stage.
        public void Intimidate_LowersEnemyAttackOnLeadEntry()
        {
            PokemonInstance e1 = Mon(null, 50), e2 = Mon(null, 50);
            var state = new CombatController.CombatState
            {
                PlayerTeam = new List<PokemonInstance> { Mon(Ability("intimidate"), 100) },
                LeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { e1, e2 },
            };
            AbilityResolver.ApplyLeadEntryEffects(state);
            Assert.That(StatStageManager.GetStage(e1, Stat.Attack), Is.EqualTo(-1));
            Assert.That(StatStageManager.GetStage(e2, Stat.Attack), Is.EqualTo(-1));

            // A non-Intimidate lead does nothing.
            PokemonInstance e3 = Mon(null, 50);
            var state2 = new CombatController.CombatState
            {
                PlayerTeam = new List<PokemonInstance> { Mon(Ability("overgrow"), 100) },
                LeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { e3 },
            };
            AbilityResolver.ApplyLeadEntryEffects(state2);
            Assert.That(StatStageManager.GetStage(e3, Stat.Attack), Is.EqualTo(0));
        }
    }
}
