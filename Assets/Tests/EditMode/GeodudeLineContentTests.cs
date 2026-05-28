using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.6 + §5.5.1 + §5.8 (Sturdy) + §5.11.4 Mastery bands.
    // Geodude line golden tests — Vanguard tank archetype, 3-stage wild line,
    // no sub-branch split (single Graveler→Golem path).
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
            Assert.That(geodude.Branches.Count, Is.EqualTo(1));
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
        public void GeodudeEvolveBranch_HasSturdyAndTenPercentCritBonus()
        {
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/geodude/geodude_evolve.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Archetype, Is.EqualTo(BranchArchetype.Vanguard));
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("sturdy"));
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0.1f).Within(0.001f),
                "Vanguard archetype contributes +10% crit per §5.3.4.");
        }

        [Test]
        public void Graveler_PrimaryAbilityIsSturdy_SingleEvolveBranch()
        {
            PokemonSpeciesSO graveler = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Geodude/Graveler.asset");
            Assert.That(graveler.PrimaryAbility, Is.Not.Null);
            Assert.That(graveler.PrimaryAbility.AbilityId, Is.EqualTo("sturdy"));
            Assert.That(graveler.Branches.Count, Is.EqualTo(1),
                "Wild 3-stage lines use a single Stage 2 evolution path (no A1/A2 split).");
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
        public void GravelerEvolveBranch_HasToughClaws()
        {
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/geodude/graveler_evolve.asset");
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("tough_claws"),
                "Golem secondary: Tough Claws (Vanguard tank's melee identity).");
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
