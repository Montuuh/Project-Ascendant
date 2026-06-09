using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per CL-007 (§5.12.2) + §5.11.4 Mastery bands.
    // Caterpie line golden tests — moves-only, 3 archetypes per stage, no ability/crit grant.
    public class CaterpieLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        private static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        [Test]
        public void Caterpie_BaseLearnsetHasFourMoves_NoPrimaryAbility()
        {
            PokemonSpeciesSO caterpie = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Caterpie/Caterpie.asset");
            Assert.That(caterpie, Is.Not.Null);
            Assert.That(caterpie.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(caterpie.PrimaryAbility, Is.Null);
            Assert.That(caterpie.Branches.Count, Is.EqualTo(3));
        }

        [Test]
        public void Caterpie_MasteryLv1_BandCompliant()
        {
            PokemonSpeciesSO caterpie = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Caterpie/Caterpie.asset");
            MoveSO mastery = caterpie.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(60, 80));
            Assert.That(mastery.APCost, Is.EqualTo(1));
            Assert.That(mastery.Modifier, Is.EqualTo(PositionalModifier.None));
        }

        [Test]
        public void CaterpieEvolveBranch_MovesOnly_NoAbility()
        {
            // Per CL-007 (§5.12.2) — stage-1 branch: ≤2 upgrades, no ability/crit grant.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/caterpie/caterpie_specialist.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.GrantedAbility, Is.Null, "CL-007: caterpie_specialist grants no ability.");
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0f).Within(0.001f), "No crit bonus (CL-007).");
            Assert.That(branch.MoveUpgrades.Count, Is.InRange(1, 2), "≤2 move upgrades (§5.12.2).");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(0));
        }

        [Test]
        public void Metapod_NoAbility_SingleEvolveBranch()
        {
            // Per CL-007 (§5.12.2) — mid-form has no PrimaryAbility.
            PokemonSpeciesSO metapod = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Caterpie/Metapod.asset");
            Assert.That(metapod.PrimaryAbility, Is.Null, "CL-007: Metapod has no PrimaryAbility.");
            Assert.That(metapod.Branches.Count, Is.EqualTo(3));
        }

        [Test]
        public void Metapod_MasteryLv2_BandCompliant()
        {
            PokemonSpeciesSO metapod = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Caterpie/Metapod.asset");
            MoveSO mastery = metapod.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(85, 110));
            Assert.That(mastery.APCost, Is.InRange(1, 2));
            bool hasSfSb = mastery.Modifier == PositionalModifier.StepForward
                        || mastery.Modifier == PositionalModifier.StepBackward;
            bool hasRider = mastery.Effects != null && mastery.Effects.Count >= 1;
            Assert.That(hasSfSb || hasRider, Is.True,
                "Butterfly Coil carries a +1 Defense BuffSelfEffectSO — satisfies modifier req.");
        }

        [Test]
        public void MetapodEvolveBranch_AdditiveSignature_NoAbility()
        {
            // Per CL-007 (§5.12.2) — stage-2 branch: 0 upgrades, +1 signature (Psybeam), no ability.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/caterpie/metapod_specialist.asset");
            Assert.That(branch.GrantedAbility, Is.Null, "CL-007: metapod_specialist grants no ability.");
            Assert.That(branch.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 is purely additive.");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(1), "Adds exactly 1 signature (Psybeam).");
        }

        [Test]
        public void Butterfree_MasteryLv3_BandCompliant()
        {
            PokemonSpeciesSO butterfree = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Caterpie/Butterfree.asset");
            MoveSO mastery = butterfree.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(110, 140));
            Assert.That(mastery.APCost, Is.InRange(2, 3));
        }
    }
}
