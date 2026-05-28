using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.4 + §5.5.1 + §5.8 (Compound Eyes) + §5.11.4 Mastery bands.
    // Caterpie line golden tests — Specialist archetype (status-rider focused).
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
            Assert.That(caterpie.Branches.Count, Is.EqualTo(1));
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
        public void CaterpieEvolveBranch_HasIronShell_SpecialistArchetype_NoCritBonus()
        {
            // Per §5.3.4 — Specialist archetype reward is a species-specific passive
            // (no crit bonus — that's Vanguard's reward).
            //
            // NOTE: Implementation places Iron Shell (themed Metapod chrysalis defense)
            // as the species PRIMARY, with Compound Eyes promoted to Butterfree's
            // branch SECONDARY. §5.8 catalog lists Compound Eyes as the Butterfree-line
            // primary — see ⚠ OPEN gap #28 in BACKLOG. Content is asserted as-authored.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/caterpie/caterpie_evolve.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Archetype, Is.EqualTo(BranchArchetype.Specialist));
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("iron_shell"));
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0f).Within(0.001f),
                "Specialist archetype does not grant a crit bonus.");
        }

        [Test]
        public void Metapod_PrimaryAbilityIsIronShell_SingleEvolveBranch()
        {
            PokemonSpeciesSO metapod = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Caterpie/Metapod.asset");
            Assert.That(metapod.PrimaryAbility, Is.Not.Null);
            Assert.That(metapod.PrimaryAbility.AbilityId, Is.EqualTo("iron_shell"));
            Assert.That(metapod.Branches.Count, Is.EqualTo(1));
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
        public void MetapodEvolveBranch_HasCompoundEyes()
        {
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/caterpie/metapod_evolve.asset");
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("compoundeyes"),
                "Butterfree secondary: Compound Eyes (§5.8 ability; promoted to secondary slot here).");
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
