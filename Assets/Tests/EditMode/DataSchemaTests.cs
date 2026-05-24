using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 3.1 + §9.11.2 — unit tests for data-layer schemas.
    // Tests cover: StatGrowthCurveSO computed accessors; PokemonInstance faint & reset invariants.
    public class DataSchemaTests
    {
        // ── StatGrowthCurveSO ─────────────────────────────────────────────────────

        [Test]
        public void StatGrowthCurve_GetHPAt_Level1_ReturnsZero()
        {
            // Per §5.2.3 — no growth has occurred at level 1; accumulated delta is 0.
            StatGrowthCurveSO curve = ScriptableObject.CreateInstance<StatGrowthCurveSO>();
            curve.HPGrowthPerLevel = new int[] { 3, 3, 3 };
            Assert.That(curve.GetHPAt(1), Is.EqualTo(0));
            Object.DestroyImmediate(curve);
        }

        [Test]
        public void StatGrowthCurve_GetHPAt_Level3_AccumulatesCorrectly()
        {
            // Per §5.2.3 — level 3 growth = sum of indices 0 and 1 (level 1→2 and 2→3).
            StatGrowthCurveSO curve = ScriptableObject.CreateInstance<StatGrowthCurveSO>();
            curve.HPGrowthPerLevel = new int[] { 4, 5, 6 }; // levels 1→2, 2→3, 3→4
            Assert.That(curve.GetHPAt(3), Is.EqualTo(9)); // 4 + 5
            Object.DestroyImmediate(curve);
        }

        [Test]
        public void StatGrowthCurve_GetHPAt_NullArray_ReturnsZero()
        {
            // Per §5.2.3 — null growth array is safe; returns 0 rather than throwing.
            StatGrowthCurveSO curve = ScriptableObject.CreateInstance<StatGrowthCurveSO>();
            curve.HPGrowthPerLevel = null;
            Assert.That(curve.GetHPAt(10), Is.EqualTo(0));
            Object.DestroyImmediate(curve);
        }

        // ── PokemonInstance — faint state & reset ─────────────────────────────────

        [Test]
        public void PokemonInstance_Reset_CurrentHPIsZero()
        {
            // Per §2.4.1 — CurrentHP == 0 means fainted. Reset() must yield this state.
            PokemonInstance instance = new();
            instance.CurrentHP = 50;
            instance.Reset();
            Assert.That(instance.CurrentHP, Is.EqualTo(0));
        }

        [Test]
        public void PokemonInstance_Reset_PrimaryStatusIsNone()
        {
            // Per §3.3.5.1 — after reset, no status conditions should be active.
            PokemonInstance instance = new();
            instance.PrimaryStatus = StatusCondition.Burn;
            instance.SecondaryStatus = StatusCondition.Confusion;
            instance.Reset();
            Assert.That(instance.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Assert.That(instance.SecondaryStatus, Is.EqualTo(StatusCondition.None));
        }

        [Test]
        public void PokemonInstance_Reset_StatStagesAreCleared()
        {
            // Per §4.X — all stat stages zero after reset; no stale combat modifiers.
            PokemonInstance instance = new();
            instance.StatStages[Stat.Attack] = 3;
            instance.StatStages[Stat.Defense] = -2;
            instance.Reset();
            Assert.That(instance.StatStages.Count, Is.EqualTo(0));
        }

        [Test]
        public void PokemonInstance_Reset_SelectedBranchIsNull()
        {
            // Per §5.3.3 — SelectedBranch null after reset; branch choice is per-run state.
            PokemonSpeciesSO species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            EvolutionBranchSO branch = ScriptableObject.CreateInstance<EvolutionBranchSO>();

            PokemonInstance instance = new();
            instance.Species = species;
            instance.SelectedBranch = branch;
            instance.Reset();

            Assert.That(instance.SelectedBranch, Is.Null);

            Object.DestroyImmediate(species);
            Object.DestroyImmediate(branch);
        }
    }
}
