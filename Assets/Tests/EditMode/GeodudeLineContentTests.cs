using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per CL-007 (§5.12.2) + §5.11.4 Mastery bands.
    // Geodude line golden tests — moves-only, 3 archetypes per stage, no ability/crit grant.
    public class GeodudeLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        private static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        [Test]
        public void Geodude_BaseLearnsetHasFourMoves_NoPrimaryAbility()
        {
            PokemonSpeciesSO geodude = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Geodude/Geodude.asset");
            Assert.That(geodude, Is.Not.Null);
            Assert.That(geodude.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(geodude.PrimaryAbility, Is.Null);
            Assert.That(geodude.Branches.Count, Is.EqualTo(3));
        }

        [Test]
        public void Geodude_MasteryLv1_BandCompliant()
        {
            PokemonSpeciesSO geodude = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Geodude/Geodude.asset");
            MoveSO mastery = geodude.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(60, 80));
            Assert.That(mastery.APCost, Is.EqualTo(1));
            Assert.That(mastery.Modifier, Is.EqualTo(PositionalModifier.None));
        }

        [Test]
        public void GeodudeEvolveBranch_MovesOnly_NoAbility()
        {
            // Per CL-007 (§5.12.2) — stage-1 branch: ≤2 upgrades, no ability/crit grant.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/geodude/geodude_vanguard.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Archetype, Is.EqualTo(BranchArchetype.Vanguard));
            Assert.That(branch.GrantedAbility, Is.Null, "CL-007: geodude_vanguard grants no ability.");
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0f).Within(0.001f), "No crit bonus (CL-007).");
            Assert.That(branch.MoveUpgrades.Count, Is.InRange(1, 2), "≤2 move upgrades (§5.12.2).");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(0));
        }

        [Test]
        public void Graveler_NoAbility_SingleEvolveBranch()
        {
            // Per CL-007 (§5.12.2) — mid-form has no PrimaryAbility.
            PokemonSpeciesSO graveler = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Geodude/Graveler.asset");
            Assert.That(graveler.PrimaryAbility, Is.Null, "CL-007: Graveler has no PrimaryAbility.");
            Assert.That(graveler.Branches.Count, Is.EqualTo(3),
                "Wild 3-stage lines have 3 archetype branches per stage (CL-007 #D).");
        }

        [Test]
        public void Graveler_MasteryLv2_BandCompliant()
        {
            PokemonSpeciesSO graveler = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Geodude/Graveler.asset");
            MoveSO mastery = graveler.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(85, 110));
            Assert.That(mastery.APCost, Is.InRange(1, 2));
            bool hasSfSb = mastery.Modifier == PositionalModifier.StepForward
                        || mastery.Modifier == PositionalModifier.StepBackward;
            bool hasRider = mastery.Effects != null && mastery.Effects.Count >= 1;
            Assert.That(hasSfSb || hasRider, Is.True);
        }

        [Test]
        public void GravelerEvolveBranch_AdditiveSignature_NoAbility()
        {
            // Per CL-007 (§5.12.2) — stage-2 branch: 0 upgrades, +1 signature (Body Press), no ability.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/geodude/graveler_vanguard.asset");
            Assert.That(branch.GrantedAbility, Is.Null, "CL-007: graveler_vanguard grants no ability.");
            Assert.That(branch.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 is purely additive.");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(1), "Adds exactly 1 signature (Body Press).");
        }

        [Test]
        public void Golem_MasteryLv3_BandCompliant()
        {
            PokemonSpeciesSO golem = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Geodude/Golem.asset");
            MoveSO mastery = golem.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(110, 140));
            Assert.That(mastery.APCost, Is.InRange(2, 3));
        }
    }
}
