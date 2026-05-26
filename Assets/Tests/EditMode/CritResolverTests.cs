using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.4.5 — all three source combinations + clamp 0-100% +
    // AlwaysCrit override + UI redundancy flag + determinism + plumbing helpers.
    public class CritResolverTests
    {
        private const uint SEED = 0xC0FFEE;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static MoveSO MakeMove(bool alwaysCrit = false)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.AlwaysCrit = alwaysCrit;
            m.BasePower = 50;
            m.Type = PokemonType.Normal;
            return m;
        }

        // ── Bucket 1: base behaviour (3) ──────────────────────────────────────

        [Test]
        public void Resolve_BaseZeroChance_NeverCrits()
        {
            // Per §4.1.3 — base crit chance is 0%. With no sources, 1000 rolls
            // must all return false.
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritInputs inputs = new CritInputs(move, 0f, 0f);
            for (int i = 0; i < 1000; i++)
                Assert.That(CritResolver.Resolve(inputs, rng).IsCrit, Is.False,
                    $"Roll {i} crit at 0% chance.");
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_BaseZeroChance_ResolvedChanceIsZero()
        {
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0f, 0f), rng);
            Assert.That(r.ResolvedChance, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(r.HasChanceBonus, Is.False);
            Assert.That(r.IsAlwaysCrit, Is.False);
            Assert.That(r.IsRedundant, Is.False);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_FullChance_AlwaysCrits()
        {
            // 100% chance via stacking → must always crit (no AlwaysCrit needed).
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritInputs inputs = new CritInputs(move, 1f, 0f);
            for (int i = 0; i < 100; i++)
                Assert.That(CritResolver.Resolve(inputs, rng).IsCrit, Is.True);
            Object.DestroyImmediate(move);
        }

        // ── Bucket 2: AlwaysCrit (4) ──────────────────────────────────────────

        [Test]
        public void Resolve_AlwaysCritMove_OverridesZeroChance()
        {
            // Per §4.1.3 — AlwaysCrit is independent. Even at 0% sum, crits.
            MoveSO move = MakeMove(alwaysCrit: true);
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0f, 0f), rng);
            Assert.That(r.IsCrit, Is.True);
            Assert.That(r.IsAlwaysCrit, Is.True);
            Assert.That(r.ResolvedChance, Is.EqualTo(1f).Within(0.0001f));
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_AlwaysCritWithChanceBonus_FlagsRedundant()
        {
            // Per §4.1.3 Task 4.4.4 — UI redundancy flag when AlwaysCrit ∧ bonus.
            MoveSO move = MakeMove(alwaysCrit: true);
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0.25f, 0.10f), rng);
            Assert.That(r.IsRedundant, Is.True);
            Assert.That(r.HasChanceBonus, Is.True);
            Assert.That(r.IsCrit, Is.True);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_AlwaysCritWithoutChanceBonus_NotRedundant()
        {
            MoveSO move = MakeMove(alwaysCrit: true);
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0f, 0f), rng);
            Assert.That(r.IsRedundant, Is.False);
            Assert.That(r.HasChanceBonus, Is.False);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_AlwaysCrit_DoesNotConsumeRNGRoll()
        {
            // Determinism: AlwaysCrit short-circuits before Range01(). The RNG
            // stream must be unchanged across the call. Verify by feeding two
            // identically-seeded RNGs to (resolve+roll) vs (roll only) and
            // comparing the post-call Range01 value.
            MoveSO move = MakeMove(alwaysCrit: true);
            GameRNG a = new GameRNG(SEED);
            GameRNG b = new GameRNG(SEED);
            CritResolver.Resolve(new CritInputs(move, 0.5f, 0.5f), a);
            Assert.That(a.Range01(), Is.EqualTo(b.Range01()).Within(0.0001f));
            Object.DestroyImmediate(move);
        }

        // ── Bucket 3: source stacking (4) ─────────────────────────────────────

        [Test]
        public void Resolve_ConsumableBonusOnly_SetsResolvedChance()
        {
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0.25f, 0f), rng);
            Assert.That(r.ResolvedChance, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(r.HasChanceBonus, Is.True);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_PassiveBonusOnly_SetsResolvedChance()
        {
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0f, 0.10f), rng);
            Assert.That(r.ResolvedChance, Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(r.HasChanceBonus, Is.True);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_StackingAdditive_PerSpec()
        {
            // Per §4.1.3 — Consumable + Passive are additive.
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0.25f, 0.10f), rng);
            Assert.That(r.ResolvedChance, Is.EqualTo(0.35f).Within(0.0001f));
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_StackingOverflow_ClampedTo1()
        {
            // Per Task 4.4.5 — clamp 0-100%.
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, 0.70f, 0.70f), rng);
            Assert.That(r.ResolvedChance, Is.EqualTo(1f).Within(0.0001f));
            Object.DestroyImmediate(move);
        }

        // ── Bucket 4: input hygiene (2) ───────────────────────────────────────

        [Test]
        public void Resolve_NegativeInputs_ClampedToZero()
        {
            // Defensive: callers may pass negative values; resolver clamps.
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(move, -0.5f, 0f), rng);
            Assert.That(r.ResolvedChance, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(r.IsCrit, Is.False);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Resolve_NullMove_TreatedAsNotAlwaysCrit()
        {
            // Defensive null-safety. Null move → no AlwaysCrit, normal resolve.
            GameRNG rng = new GameRNG(SEED);
            CritResult r = CritResolver.Resolve(new CritInputs(null, 0.5f, 0f), rng);
            Assert.That(r.IsAlwaysCrit, Is.False);
            Assert.That(r.ResolvedChance, Is.EqualTo(0.5f).Within(0.0001f));
        }

        // ── Bucket 5: determinism + preview (3) ───────────────────────────────

        [Test]
        public void Resolve_Deterministic_SameSeedSameSequence()
        {
            // Per Engineering Pillar 3 — same seed → identical bool sequence.
            MoveSO move = MakeMove();
            GameRNG a = new GameRNG(SEED);
            GameRNG b = new GameRNG(SEED);
            CritInputs inputs = new CritInputs(move, 0.5f, 0f);
            for (int i = 0; i < 50; i++)
            {
                bool aCrit = CritResolver.Resolve(inputs, a).IsCrit;
                bool bCrit = CritResolver.Resolve(inputs, b).IsCrit;
                Assert.That(aCrit, Is.EqualTo(bCrit), $"Diverged at roll {i}.");
            }
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Preview_MatchesResolveResolvedChance_NoRoll()
        {
            // Preview must not consume RNG and must agree on the chance value.
            MoveSO move = MakeMove();
            GameRNG postPreview = new GameRNG(SEED);
            GameRNG control    = new GameRNG(SEED);

            CritResult preview = CritResolver.Preview(new CritInputs(move, 0.25f, 0.10f));
            Assert.That(preview.ResolvedChance, Is.EqualTo(0.35f).Within(0.0001f));

            // No RNG was consumed by Preview — both streams roll the same value.
            Assert.That(postPreview.Range01(), Is.EqualTo(control.Range01()).Within(0.0001f));
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Preview_AlwaysCrit_ReportsCertainty()
        {
            MoveSO move = MakeMove(alwaysCrit: true);
            CritResult p = CritResolver.Preview(new CritInputs(move, 0f, 0f));
            Assert.That(p.IsAlwaysCrit, Is.True);
            Assert.That(p.ResolvedChance, Is.EqualTo(1f).Within(0.0001f));
            Object.DestroyImmediate(move);
        }

        // ── Bucket 6: distribution (1) ────────────────────────────────────────

        [Test]
        public void Resolve_Distribution_30Percent_RollsNearExpected()
        {
            // Soft-cap region per §4.1.3 (~30-35%) — 5000 rolls at chance=0.30
            // should land within ±5% of expected. xorshift32 is uniform enough.
            MoveSO move = MakeMove();
            GameRNG rng = new GameRNG(SEED);
            CritInputs inputs = new CritInputs(move, 0.30f, 0f);

            const int Trials = 5000;
            int crits = 0;
            for (int i = 0; i < Trials; i++)
                if (CritResolver.Resolve(inputs, rng).IsCrit) crits++;

            float observed = crits / (float)Trials;
            Assert.That(observed, Is.EqualTo(0.30f).Within(0.025f),
                $"Observed crit rate {observed:F3} outside ±2.5% of 0.30.");
            Object.DestroyImmediate(move);
        }

        // ── Bucket 7: plumbing helpers (4) ────────────────────────────────────

        [Test]
        public void GatherPassiveBonus_NullAttacker_ReturnsZero()
        {
            Assert.That(CritResolver.GatherPassiveBonus(null), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GatherPassiveBonus_NoBranch_ReturnsZero()
        {
            PokemonInstance pi = new PokemonInstance { SelectedBranch = null };
            Assert.That(CritResolver.GatherPassiveBonus(pi), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GatherPassiveBonus_ReadsBranchField()
        {
            // Per §4.1.3 — permanent passive comes from the attacker's selected branch.
            EvolutionBranchSO branch = ScriptableObject.CreateInstance<EvolutionBranchSO>();
            branch.CritChanceBonus = 0.15f;
            PokemonInstance pi = new PokemonInstance { SelectedBranch = branch };
            Assert.That(CritResolver.GatherPassiveBonus(pi), Is.EqualTo(0.15f).Within(0.0001f));
            Object.DestroyImmediate(branch);
        }

        [Test]
        public void GatherConsumableBonus_SumsCritBoostEffects()
        {
            // Per §4.1.3 — combat temp boost is the sum of active CritBoost effects.
            CritBoostConsumableEffectSO boost1 = ScriptableObject.CreateInstance<CritBoostConsumableEffectSO>();
            boost1.CritChanceBoost = 0.20f;
            CritBoostConsumableEffectSO boost2 = ScriptableObject.CreateInstance<CritBoostConsumableEffectSO>();
            boost2.CritChanceBoost = 0.10f;

            ConsumableSO sharpLens = ScriptableObject.CreateInstance<ConsumableSO>();
            sharpLens.Effect = boost1;
            ConsumableSO focusLens = ScriptableObject.CreateInstance<ConsumableSO>();
            focusLens.Effect = boost2;
            ConsumableSO unrelated = ScriptableObject.CreateInstance<ConsumableSO>();
            unrelated.Effect = null; // ignored

            List<ConsumableSO> active = new List<ConsumableSO> { sharpLens, focusLens, unrelated };
            Assert.That(CritResolver.GatherConsumableBonus(active),
                Is.EqualTo(0.30f).Within(0.0001f));

            Object.DestroyImmediate(boost1);
            Object.DestroyImmediate(boost2);
            Object.DestroyImmediate(sharpLens);
            Object.DestroyImmediate(focusLens);
            Object.DestroyImmediate(unrelated);
        }

        [Test]
        public void GatherConsumableBonus_NullOrEmpty_ReturnsZero()
        {
            Assert.That(CritResolver.GatherConsumableBonus(null), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(CritResolver.GatherConsumableBonus(new List<ConsumableSO>()),
                Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
