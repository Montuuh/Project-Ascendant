using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.7 — full coverage for the intent system:
    //   • Intent struct invariants + TargetsSlot helper
    //   • IntentTargeting Cleave (never fizzles) + Backstrike (fizzles on empty)
    //   • IntentScorer score formula components (Type, Status, HP, Cooldown)
    //   • PickIntent — top selection, boss counter-intel, randomness floor
    public class IntentTests
    {
        private BattleConfigSO _config;
        private PokemonSpeciesSO _normalSpecies;
        private PokemonSpeciesSO _waterSpecies;
        private PokemonSpeciesSO _fireSpecies;
        private PokemonSpeciesSO _grassSpecies;
        private PokemonSpeciesSO _electricSpecies;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            // Pin every field used by the scorer (independent of asset values).
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.AggressiveSelfMultiplier = 1.5f;
            _config.LowSelfHPThreshold = 0.40f;
            _config.SetupSelfMultiplier = 1.5f;
            _config.HighSelfHPThreshold = 0.70f;
            _config.RandomnessFloorChance = 0.125f;
            _config.BossCounterIntelTopPenalty = 0.7f;

            _normalSpecies = MakeSpecies(PokemonType.Normal);
            _waterSpecies = MakeSpecies(PokemonType.Water);
            _fireSpecies = MakeSpecies(PokemonType.Fire);
            _grassSpecies = MakeSpecies(PokemonType.Grass);
            _electricSpecies = MakeSpecies(PokemonType.Electric);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_normalSpecies);
            Object.DestroyImmediate(_waterSpecies);
            Object.DestroyImmediate(_fireSpecies);
            Object.DestroyImmediate(_grassSpecies);
            Object.DestroyImmediate(_electricSpecies);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PokemonSpeciesSO MakeSpecies(params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = 100, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            return s;
        }

        private static PokemonInstance MakeInstance(PokemonSpeciesSO sp, int hp = -1)
        {
            return new PokemonInstance
            {
                Species = sp,
                Level = 1,
                CurrentHP = hp < 0 ? sp.BaseStats.BaseHP : hp
            };
        }

        private static MoveSO MakeMove(PokemonType type, int power)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.Type = type;
            m.BasePower = power;
            m.APCost = 1;
            m.RangeModifierMultiplier = 1f;
            return m;
        }

        // ── Bucket 1: Intent struct + TargetsSlot helper ─────────────────────

        [Test]
        public void TargetsSlot_True_ForAttackBackstrikeStatus()
        {
            Assert.That(new Intent { Kind = IntentKind.Attack }.TargetsSlot, Is.True);
            Assert.That(new Intent { Kind = IntentKind.Backstrike }.TargetsSlot, Is.True);
            Assert.That(new Intent { Kind = IntentKind.Status }.TargetsSlot, Is.True);
        }

        [Test]
        public void TargetsSlot_False_ForCleaveBuffStallUnknown()
        {
            Assert.That(new Intent { Kind = IntentKind.Cleave }.TargetsSlot, Is.False);
            Assert.That(new Intent { Kind = IntentKind.Buff }.TargetsSlot, Is.False);
            Assert.That(new Intent { Kind = IntentKind.Stall }.TargetsSlot, Is.False);
            Assert.That(new Intent { Kind = IntentKind.Unknown }.TargetsSlot, Is.False);
        }

        // ── Bucket 2: IntentTargeting — Cleave never fizzles (§4.3.4.1) ──────

        [Test]
        public void ResolveCleave_AllAlive_HitsEverySlot()
        {
            List<PokemonInstance> team = new()
            {
                MakeInstance(_normalSpecies),
                MakeInstance(_normalSpecies),
                MakeInstance(_normalSpecies),
            };
            List<int> targets = IntentTargeting.ResolveCleaveTargets(team);
            Assert.That(targets, Is.EqualTo(new List<int> { 0, 1, 2 }));
        }

        [Test]
        public void ResolveCleave_OnlyOneAlive_HitsExactlyThatOne()
        {
            // Per §4.3.4.1 — "Cleave never fizzles ... minimum 1 target"
            List<PokemonInstance> team = new()
            {
                MakeInstance(_normalSpecies, 0), // fainted
                null,                            // empty
                MakeInstance(_normalSpecies),    // alive
            };
            List<int> targets = IntentTargeting.ResolveCleaveTargets(team);
            Assert.That(targets, Is.EqualTo(new List<int> { 2 }));
        }

        [Test]
        public void ResolveCleave_EmptyOrAllDead_ReturnsEmpty()
        {
            Assert.That(IntentTargeting.ResolveCleaveTargets(null), Is.Empty);
            List<PokemonInstance> dead = new()
            {
                MakeInstance(_normalSpecies, 0),
                null,
            };
            Assert.That(IntentTargeting.ResolveCleaveTargets(dead), Is.Empty);
        }

        // ── Bucket 3: IntentTargeting — Backstrike fizzles on empty (§4.3.4.1)

        [Test]
        public void ResolveBackstrike_AliveOccupant_ReturnsSlot()
        {
            List<PokemonInstance> team = new()
            {
                MakeInstance(_normalSpecies),
                MakeInstance(_normalSpecies),
            };
            Assert.That(IntentTargeting.ResolveBackstrikeTarget(1, team), Is.EqualTo(1));
        }

        [Test]
        public void ResolveBackstrike_EmptySlot_FizzlesNoRedirect()
        {
            // Per §4.3.4.1 — "Backstrike does not redirect to Lead" on empty
            List<PokemonInstance> team = new()
            {
                MakeInstance(_normalSpecies), // Lead
                null,                          // empty bench
            };
            Assert.That(IntentTargeting.ResolveBackstrikeTarget(1, team), Is.EqualTo(-1));
        }

        [Test]
        public void ResolveBackstrike_FaintedOccupant_Fizzles()
        {
            List<PokemonInstance> team = new()
            {
                MakeInstance(_normalSpecies),
                MakeInstance(_normalSpecies, 0),
            };
            Assert.That(IntentTargeting.ResolveBackstrikeTarget(1, team), Is.EqualTo(-1));
        }

        [Test]
        public void ResolveBackstrike_OutOfRangeSlot_ReturnsMinusOne()
        {
            List<PokemonInstance> team = new() { MakeInstance(_normalSpecies) };
            Assert.That(IntentTargeting.ResolveBackstrikeTarget(99, team), Is.EqualTo(-1));
            Assert.That(IntentTargeting.ResolveBackstrikeTarget(-1, team), Is.EqualTo(-1));
        }

        // ── Bucket 4: Scoring — BaseWeight derivation ────────────────────────

        [Test]
        public void Score_DamageMove_BaseWeightEqualsBasePower()
        {
            MoveSO water60 = MakeMove(PokemonType.Water, 60);
            PokemonInstance enemy = MakeInstance(_waterSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);
            Intent intent = new() { Kind = IntentKind.Attack, Move = water60, TargetSlot = 0 };
            IntentScorer.Context ctx = new()
            {
                Attacker = enemy,
                PlayerTeam = new[] { lead },
                Config = _config,
            };

            // Water vs Normal = 1.0× (neutral). No status/HP modifiers triggered.
            // Expected score = 60 (BasePower) × 1.0 × 1.0 × 1.0 × 1.0 = 60.
            float s = IntentScorer.Score(intent, ctx);
            Assert.That(s, Is.EqualTo(60f).Within(0.001f));
            Object.DestroyImmediate(water60);
        }

        [Test]
        public void Score_ZeroPowerMove_UsesDefaultUtilityWeight()
        {
            MoveSO util = MakeMove(PokemonType.Normal, 0);
            // Attacker at 50% HP — between LowSelfHPThreshold (40%) and
            // HighSelfHPThreshold (70%), so no HP-based modifier fires.
            PokemonInstance enemy = MakeInstance(_normalSpecies, 50);
            Intent intent = new() { Kind = IntentKind.Buff, Move = util, BuffStat = Stat.Attack };
            IntentScorer.Context ctx = new()
            {
                Attacker = enemy,
                PlayerTeam = new List<PokemonInstance>(),
                Config = _config,
            };
            // BaseWeight 50, no slot, no HP triggers → score = 50.
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(50f).Within(0.001f));
            Object.DestroyImmediate(util);
        }

        // ── Bucket 5: Scoring — TypeEffectivenessMultiplier ──────────────────

        [Test]
        public void Score_SuperEffective_DoublesScore()
        {
            // Water move vs Fire defender → 2.0×.
            MoveSO water = MakeMove(PokemonType.Water, 60);
            PokemonInstance enemy = MakeInstance(_waterSpecies);
            PokemonInstance lead = MakeInstance(_fireSpecies);
            Intent intent = new() { Kind = IntentKind.Attack, Move = water, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(120f).Within(0.001f));
            Object.DestroyImmediate(water);
        }

        [Test]
        public void Score_Resisted_HalvesScore()
        {
            // Water vs Grass → 0.5×.
            MoveSO water = MakeMove(PokemonType.Water, 60);
            PokemonInstance enemy = MakeInstance(_waterSpecies);
            PokemonInstance lead = MakeInstance(_grassSpecies);
            Intent intent = new() { Kind = IntentKind.Attack, Move = water, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(30f).Within(0.001f));
            Object.DestroyImmediate(water);
        }

        [Test]
        public void Score_Immune_ReturnsZero()
        {
            // Electric vs Ground would be 0× — but we don't have a Ground species
            // helper, so use Normal-vs-Ghost: Normal → Ghost = 0× (Gen I).
            MoveSO normalMove = MakeMove(PokemonType.Normal, 60);
            PokemonSpeciesSO ghost = MakeSpecies(PokemonType.Ghost);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(ghost);
            Intent intent = new() { Kind = IntentKind.Attack, Move = normalMove, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
            Object.DestroyImmediate(normalMove);
            Object.DestroyImmediate(ghost);
        }

        // ── Bucket 6: Scoring — StatusStateModifier (§4.3.3) ─────────────────

        [Test]
        public void Score_StatusOnAlreadyStatused_ReturnsZero()
        {
            // Per §4.3.3 — "Status intent vs already-statused (primary): ×0".
            MoveSO burnMove = MakeMove(PokemonType.Fire, 0);
            PokemonInstance enemy = MakeInstance(_fireSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);
            lead.PrimaryStatus = StatusCondition.Poison;
            Intent intent = new()
            {
                Kind = IntentKind.Status,
                Move = burnMove,
                TargetSlot = 0,
                AppliedStatus = StatusCondition.Burn,
            };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
            Object.DestroyImmediate(burnMove);
        }

        [Test]
        public void Score_StatusOnConfusionOnly_FullWeight()
        {
            // Per §4.3.3 — secondary (Confusion) does NOT block primary.
            MoveSO burnMove = MakeMove(PokemonType.Fire, 0);
            PokemonInstance enemy = MakeInstance(_fireSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);
            lead.SecondaryStatus = StatusCondition.Confusion;
            // No primary status set.
            Intent intent = new()
            {
                Kind = IntentKind.Status,
                Move = burnMove,
                TargetSlot = 0,
                AppliedStatus = StatusCondition.Burn,
            };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            // BaseWeight 50 (utility), no type-modifier on Status intents in scorer,
            // no HP triggers → score = 50.
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(50f).Within(0.001f));
            Object.DestroyImmediate(burnMove);
        }

        [Test]
        public void Score_StatusOnTypeImmuneTarget_ReturnsZero()
        {
            // AI never picks Status moves against type-immune targets (§4.2.4).
            MoveSO burnMove = MakeMove(PokemonType.Fire, 0);
            PokemonInstance enemy = MakeInstance(_fireSpecies);
            PokemonInstance fireLead = MakeInstance(_fireSpecies); // Fire immune to Burn
            Intent intent = new()
            {
                Kind = IntentKind.Status,
                Move = burnMove,
                TargetSlot = 0,
                AppliedStatus = StatusCondition.Burn,
            };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { fireLead }, Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
            Object.DestroyImmediate(burnMove);
        }

        // ── Bucket 7: Scoring — HPStateModifier (§4.3.3) ─────────────────────

        [Test]
        public void Score_WoundedTarget_AttackGetsLowTargetHPBonus()
        {
            // Target at 20/100 = 20% < 30% threshold → ×2.0.
            MoveSO normalMove = MakeMove(PokemonType.Normal, 50);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies, 20);
            Intent intent = new() { Kind = IntentKind.Attack, Move = normalMove, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            // 50 × 1.0 (type) × 1.0 (status) × 2.0 (hp) × 1.0 = 100.
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(100f).Within(0.001f));
            Object.DestroyImmediate(normalMove);
        }

        [Test]
        public void Score_WoundedAttacker_OffensiveGetsAggressionBonus()
        {
            // Attacker at 30/100 = 30% < 40% threshold → ×1.5 for any offensive intent.
            MoveSO cleave = MakeMove(PokemonType.Normal, 40);
            PokemonInstance enemy = MakeInstance(_normalSpecies, 30);
            PokemonInstance lead = MakeInstance(_normalSpecies);
            Intent intent = new() { Kind = IntentKind.Cleave, Move = cleave, TargetSlot = -1 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            // 40 × 1.0 × 1.0 × 1.5 (aggression) × 1.0 = 60.
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(60f).Within(0.001f));
            Object.DestroyImmediate(cleave);
        }

        [Test]
        public void Score_HealthyAttacker_SetupGetsBonus()
        {
            // Attacker at 100% > 70% threshold → ×1.5 for Buff/Stall.
            MoveSO buffMove = MakeMove(PokemonType.Normal, 0);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            Intent intent = new() { Kind = IntentKind.Buff, Move = buffMove, BuffStat = Stat.Attack };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new List<PokemonInstance>(), Config = _config };
            // 50 (DefaultUtilityWeight) × 1.5 (setup) = 75.
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(75f).Within(0.001f));
            Object.DestroyImmediate(buffMove);
        }

        [Test]
        public void Score_BothHPBonusesStack()
        {
            // Attacker wounded AND target wounded → both bonuses apply.
            MoveSO normalMove = MakeMove(PokemonType.Normal, 50);
            PokemonInstance enemy = MakeInstance(_normalSpecies, 30);
            PokemonInstance lead = MakeInstance(_normalSpecies, 20);
            Intent intent = new() { Kind = IntentKind.Attack, Move = normalMove, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            // 50 × 2.0 (low target) × 1.5 (aggressive) = 150.
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(150f).Within(0.001f));
            Object.DestroyImmediate(normalMove);
        }

        // ── Bucket 8: PickIntent — top selection, no randomness ──────────────

        [Test]
        public void PickIntent_NoRandomness_PicksTopScored()
        {
            // Force randomness off via config.
            _config.RandomnessFloorChance = 0f;

            MoveSO weak = MakeMove(PokemonType.Normal, 30);
            MoveSO strong = MakeMove(PokemonType.Water, 60);
            PokemonInstance enemy = MakeInstance(_waterSpecies);
            PokemonInstance lead = MakeInstance(_fireSpecies); // weak to water

            List<Intent> candidates = new()
            {
                new() { Kind = IntentKind.Attack, Move = weak, TargetSlot = 0 },
                new() { Kind = IntentKind.Attack, Move = strong, TargetSlot = 0 },
            };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            GameRNG rng = new(0xDEADBEEFu);

            Intent picked = IntentScorer.PickIntent(candidates, ctx, rng);
            Assert.That(picked.Move, Is.SameAs(strong));
            Object.DestroyImmediate(weak);
            Object.DestroyImmediate(strong);
        }

        [Test]
        public void PickIntent_BossCounterIntel_AppliesTopPenalty_NoRandomness()
        {
            // Per §4.3.5 + Epic 4.7.7 — top ×0.7, randomness disabled.
            // Setup: two candidates. Top is normally 80, second is 60.
            // After 0.7 penalty: top becomes 56 → second wins at 60.
            MoveSO movePow80 = MakeMove(PokemonType.Normal, 80);
            MoveSO movePow60 = MakeMove(PokemonType.Normal, 60);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);

            List<Intent> candidates = new()
            {
                new() { Kind = IntentKind.Attack, Move = movePow80, TargetSlot = 0 },
                new() { Kind = IntentKind.Attack, Move = movePow60, TargetSlot = 0 },
            };
            IntentScorer.Context ctx = new()
            {
                Attacker = enemy,
                PlayerTeam = new[] { lead },
                Config = _config,
                BossCounterIntelActive = true,
            };
            GameRNG rng = new(0x12345678u);

            Intent picked = IntentScorer.PickIntent(candidates, ctx, rng);
            Assert.That(picked.Move, Is.SameAs(movePow60));
            Object.DestroyImmediate(movePow80);
            Object.DestroyImmediate(movePow60);
        }

        [Test]
        public void PickIntent_RandomnessFloorAtFullChance_NeverPicksTop()
        {
            // RandomnessFloorChance = 1.0 → ALWAYS pick non-top.
            _config.RandomnessFloorChance = 1.0f;

            MoveSO weak = MakeMove(PokemonType.Normal, 30);
            MoveSO strong = MakeMove(PokemonType.Normal, 80);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);

            List<Intent> candidates = new()
            {
                new() { Kind = IntentKind.Attack, Move = strong, TargetSlot = 0 },
                new() { Kind = IntentKind.Attack, Move = weak, TargetSlot = 0 },
            };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            GameRNG rng = new(0xCAFEBABEu);

            Intent picked = IntentScorer.PickIntent(candidates, ctx, rng);
            // With only one non-top candidate, weighted-random must pick it.
            Assert.That(picked.Move, Is.SameAs(weak));
            Object.DestroyImmediate(weak);
            Object.DestroyImmediate(strong);
        }

        [Test]
        public void PickIntent_DeterministicAcrossRuns_SameSeedSameResult()
        {
            // Per §9.7 — deterministic AI given seed.
            _config.RandomnessFloorChance = 0.5f; // exercise the random path

            MoveSO a = MakeMove(PokemonType.Normal, 50);
            MoveSO b = MakeMove(PokemonType.Normal, 50);
            MoveSO c = MakeMove(PokemonType.Normal, 50);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);

            List<Intent> cands = new()
            {
                new() { Kind = IntentKind.Attack, Move = a, TargetSlot = 0 },
                new() { Kind = IntentKind.Attack, Move = b, TargetSlot = 0 },
                new() { Kind = IntentKind.Attack, Move = c, TargetSlot = 0 },
            };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };

            // Run 50 picks with seed K → record sequence. Re-run with same seed
            // → identical sequence.
            const int N = 50;
            const uint SEED = 0xA1B2C3D4u;

            GameRNG rng1 = new(SEED);
            List<MoveSO> seq1 = new();
            for (int i = 0; i < N; i++)
                seq1.Add(IntentScorer.PickIntent(cands, ctx, rng1).Move);

            GameRNG rng2 = new(SEED);
            List<MoveSO> seq2 = new();
            for (int i = 0; i < N; i++)
                seq2.Add(IntentScorer.PickIntent(cands, ctx, rng2).Move);

            Assert.That(seq1, Is.EqualTo(seq2));

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void PickIntent_EmptyCandidates_ReturnsDefault()
        {
            IntentScorer.Context ctx = new() { Config = _config };
            Intent result = IntentScorer.PickIntent(new List<Intent>(), ctx, new GameRNG(1u));
            Assert.That(result.Move, Is.Null);
            Assert.That(result.Kind, Is.EqualTo(IntentKind.Attack)); // enum default
        }

        // ── Bucket 9: scoring guards ────────────────────────────────────────

        [Test]
        public void Score_UnknownKind_ReturnsZero()
        {
            // Unknown is a display state, never executable.
            Intent intent = new() { Kind = IntentKind.Unknown };
            IntentScorer.Context ctx = new() { Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
        }

        [Test]
        public void Score_NullConfig_ReturnsZero()
        {
            Intent intent = new() { Kind = IntentKind.Attack };
            IntentScorer.Context ctx = new() { Config = null };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
        }

        // ── Bucket 10: Cooldown gate (§4.3.3) — Epic 8 Task 8.0 closure ──────

        [Test]
        public void Score_MoveOnCooldown_ReturnsZero()
        {
            // Per §4.3.3 — CooldownGate ×0 short-circuits the score.
            MoveSO sig = MakeMove(PokemonType.Normal, 80);
            sig.CooldownTurns = 3;
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);
            enemy.SetMoveCooldown(sig, 2);

            Intent intent = new() { Kind = IntentKind.Attack, Move = sig, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
            Object.DestroyImmediate(sig);
        }

        [Test]
        public void Score_AfterCooldownTickedToZero_ReturnsNonZero()
        {
            // Per §4.3.3 — TickMoveCooldowns reduces by 1; entries hitting 0
            // are removed so IsMoveOnCooldown reads false and gate returns 1.
            MoveSO sig = MakeMove(PokemonType.Normal, 80);
            sig.CooldownTurns = 1;
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            PokemonInstance lead = MakeInstance(_normalSpecies);
            enemy.SetMoveCooldown(sig, 1);

            Intent intent = new() { Kind = IntentKind.Attack, Move = sig, TargetSlot = 0 };
            IntentScorer.Context ctx = new() { Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config };
            // On cooldown → 0
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(0f));
            // Tick once → cooldown clears
            enemy.TickMoveCooldowns();
            Assert.That(enemy.IsMoveOnCooldown(sig), Is.False);
            Assert.That(IntentScorer.Score(intent, ctx), Is.EqualTo(80f).Within(0.001f));
            Object.DestroyImmediate(sig);
        }

        [Test]
        public void TickMoveCooldowns_DecrementsAllEntries_RemovesAtZero()
        {
            MoveSO a = MakeMove(PokemonType.Normal, 60); a.CooldownTurns = 1;
            MoveSO b = MakeMove(PokemonType.Normal, 60); b.CooldownTurns = 3;
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            enemy.SetMoveCooldown(a, 1);
            enemy.SetMoveCooldown(b, 3);

            enemy.TickMoveCooldowns();
            // a hit 0 → removed; b decremented to 2
            Assert.That(enemy.IsMoveOnCooldown(a), Is.False);
            Assert.That(enemy.MoveCooldowns.ContainsKey(a), Is.False);
            Assert.That(enemy.IsMoveOnCooldown(b), Is.True);
            Assert.That(enemy.MoveCooldowns[b], Is.EqualTo(2));

            // Two more ticks → b removed.
            enemy.TickMoveCooldowns();
            enemy.TickMoveCooldowns();
            Assert.That(enemy.IsMoveOnCooldown(b), Is.False);
            Assert.That(enemy.MoveCooldowns.Count, Is.EqualTo(0));

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void SetMoveCooldown_NullOrZero_NoOp()
        {
            // Guard: null move or non-positive turns must not add an entry.
            MoveSO m = MakeMove(PokemonType.Normal, 60);
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            enemy.SetMoveCooldown(null, 5);
            enemy.SetMoveCooldown(m, 0);
            enemy.SetMoveCooldown(m, -1);
            Assert.That(enemy.MoveCooldowns.Count, Is.EqualTo(0));
            Object.DestroyImmediate(m);
        }

        [Test]
        public void IsMoveOnCooldown_NullMove_ReturnsFalse()
        {
            // Defensive: IntentScorer can call with null Move on utility intents.
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            Assert.That(enemy.IsMoveOnCooldown(null), Is.False);
        }

        [Test]
        public void PokemonInstance_Reset_ClearsCooldowns()
        {
            // Per §9.6.1 — factory pool reuse must not leak cooldown state.
            MoveSO m = MakeMove(PokemonType.Normal, 60); m.CooldownTurns = 2;
            PokemonInstance enemy = MakeInstance(_normalSpecies);
            enemy.SetMoveCooldown(m, 2);
            Assert.That(enemy.MoveCooldowns.Count, Is.EqualTo(1));
            enemy.Reset();
            Assert.That(enemy.MoveCooldowns.Count, Is.EqualTo(0));
            Object.DestroyImmediate(m);
        }
    }
}
