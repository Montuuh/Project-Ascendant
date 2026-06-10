using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.2.1 (CL-017) + Epic 11 Task 11.1 — locks the Trauma System's load-bearing numbers: the
    // §6.2.1 two-zone EffectiveMaxHP table (−5%/stack for 1–5, −10%/stack for 6–10), the 10-stack
    // soft cap (−75%), and the per-instance zero-start that makes run-end reset + fresh recruits
    // structural (§6.2.3). Faint-application, evolution-carry, and heal/DoT integration are covered in
    // FaintResolver/Progression/StatusEffect/PokemonCenter test suites; this file owns the formula +
    // persistence invariants.
    public class TraumaSystemTests
    {
        // §6.2.1 (CL-017) — the live two-zone economy: zone-1 −5%/stack to 5, zone-2 −10%/stack to 10.
        private static EconomyConfigSO Economy()
        {
            EconomyConfigSO e = ScriptableObject.CreateInstance<EconomyConfigSO>();
            e.TraumaStackPenaltyPercent = 5;  // zone-1: −5%/stack
            e.TraumaZone1StackCount = 5;      // zone-1 boundary
            e.TraumaZone2PenaltyPercent = 10; // zone-2: −10%/stack
            e.TraumaStackCap = 10;            // soft cap at 10 → −75%
            return e;
        }

        private static PokemonInstance Mon(int baseHp, int stacks)
        {
            PokemonSpeciesSO sp = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            sp.BaseStats = new BaseStats { BaseHP = baseHp, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            sp.GrowthCurve = null; // MaxHP == BaseHP
            return new PokemonInstance { Species = sp, Level = 1, CurrentHP = baseHp, TraumaStacks = stacks };
        }

        // Per §6.2.1 (CL-017) — the locked two-zone EffectiveMaxHP table (BaseMaxHP = 100).
        [TestCase(0, 100)]
        [TestCase(1, 95)]
        [TestCase(2, 90)]
        [TestCase(3, 85)]
        [TestCase(4, 80)]
        [TestCase(5, 75)]   // end of zone 1 (−25%)
        [TestCase(6, 65)]   // zone 2 begins (−10%/stack)
        [TestCase(7, 55)]
        [TestCase(8, 45)]
        [TestCase(9, 35)]
        [TestCase(10, 25)]  // soft cap (−75%)
        [TestCase(11, 25)]  // past cap — no further penalty
        [TestCase(99, 25)]  // far past cap — still 25
        public void EffectiveMaxHP_FollowsLockedTable(int stacks, int expected)
        {
            EconomyConfigSO e = Economy();
            PokemonInstance p = Mon(100, stacks);
            Assert.That(PokemonVitals.EffectiveMaxHP(p, e), Is.EqualTo(expected));
            Object.DestroyImmediate(p.Species);
            Object.DestroyImmediate(e);
        }

        // Per §6.2.1 — floor() applies (odd base HP).
        [Test]
        public void EffectiveMaxHP_Floors()
        {
            EconomyConfigSO e = Economy();
            PokemonInstance p = Mon(63, 1); // floor(63 × 0.95) = floor(59.85) = 59
            Assert.That(PokemonVitals.EffectiveMaxHP(p, e), Is.EqualTo(59));
            Object.DestroyImmediate(p.Species);
            Object.DestroyImmediate(e);
        }

        // Per §6.2.1 (CL-017) — when the cap equals the zone-1 boundary, the curve is linear (zone 2
        // never engages). Locks the backward-compat invariant relied on by legacy cap=5 callers/tests.
        [Test]
        public void EffectiveMaxHP_CapAtZone1Boundary_IsLinear()
        {
            EconomyConfigSO e = ScriptableObject.CreateInstance<EconomyConfigSO>();
            e.TraumaStackPenaltyPercent = 5; e.TraumaZone1StackCount = 5;
            e.TraumaZone2PenaltyPercent = 10; e.TraumaStackCap = 5; // legacy cap → no zone 2
            PokemonInstance p = Mon(100, 8); // 8 stacks, capped at 5 → −25%, never −80%
            Assert.That(PokemonVitals.EffectiveMaxHP(p, e), Is.EqualTo(75));
            Object.DestroyImmediate(p.Species);
            Object.DestroyImmediate(e);
        }

        // Null economy → raw MaxHP (Trauma-agnostic callers, e.g. enemies / tests).
        [Test]
        public void EffectiveMaxHP_NullEconomy_ReturnsRawMax()
        {
            PokemonInstance p = Mon(100, 4);
            Assert.That(PokemonVitals.EffectiveMaxHP(p, null), Is.EqualTo(100));
            Object.DestroyImmediate(p.Species);
        }

        // Per §6.2.3 — Trauma is per-instance and starts at 0. A fresh instance (run start, recruit,
        // factory) carries no Trauma, so run-end reset is structural: a new run = new instances.
        [Test]
        public void NewInstance_StartsAtZeroTrauma()
        {
            Assert.That(new PokemonInstance().TraumaStacks, Is.EqualTo(0));

            PokemonInstance reused = Mon(100, 5);
            reused.Reset();
            Assert.That(reused.TraumaStacks, Is.EqualTo(0), "Reset (pool reuse) clears Trauma (§6.2.3).");
            Object.DestroyImmediate(reused.Species);
        }
    }
}
