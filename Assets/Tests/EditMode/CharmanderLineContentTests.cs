using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.2 + §5.5.1 + §5.8 (Blaze) + §5.11.4 Mastery bands.
    // Charmander line golden tests — Vanguard branch only (VS scope).
    public class CharmanderLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        private static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        [Test]
        public void Charmander_BaseLearnsetHasFourMoves_NoPrimaryAbility()
        {
            PokemonSpeciesSO charmander = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Charmander/Charmander.asset");
            Assert.That(charmander, Is.Not.Null);
            Assert.That(charmander.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(charmander.PrimaryAbility, Is.Null);
            Assert.That(charmander.Branches.Count, Is.EqualTo(1));
        }

        [Test]
        public void Charmander_MasteryLv1_BandCompliant()
        {
            PokemonSpeciesSO charmander = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Charmander/Charmander.asset");
            MoveSO mastery = charmander.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(60, 80));
            Assert.That(mastery.APCost, Is.EqualTo(1));
            Assert.That(mastery.Modifier, Is.EqualTo(PositionalModifier.None),
                "Lv1 Mastery must not carry SF/SB (positional modifier).");
        }

        [Test]
        public void CharmanderVanguardBranch_HasBlazeAndTenPercentCritBonus()
        {
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/charmander/charmander_vanguard.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("blaze"));
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(branch.NewMoves.Count, Is.EqualTo(0));
        }

        [Test]
        public void CharmeleonVanguard_PrimaryAbilityIsBlaze_TwoSubBranches()
        {
            PokemonSpeciesSO charmeleon = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Charmander/Charmeleon_Vanguard.asset");
            Assert.That(charmeleon.PrimaryAbility, Is.Not.Null);
            Assert.That(charmeleon.PrimaryAbility.AbilityId, Is.EqualTo("blaze"));
            Assert.That(charmeleon.Branches.Count, Is.EqualTo(2));
        }

        [Test]
        public void Charmeleon_MasteryLv2_BandCompliant()
        {
            PokemonSpeciesSO charmeleon = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Charmander/Charmeleon_Vanguard.asset");
            MoveSO mastery = charmeleon.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(85, 110));
            Assert.That(mastery.APCost, Is.InRange(1, 2));
            bool hasSfSb = mastery.Modifier == PositionalModifier.StepForward
                        || mastery.Modifier == PositionalModifier.StepBackward;
            bool hasRider = mastery.Effects != null && mastery.Effects.Count >= 1;
            Assert.That(hasSfSb || hasRider, Is.True);
        }

        [Test]
        public void CharmeleonVA1_ToughClaws_SkyStriker()
        {
            EvolutionBranchSO va1 = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/charmander/charmeleon_va1.asset");
            Assert.That(va1.GrantedAbility, Is.Not.Null);
            Assert.That(va1.GrantedAbility.AbilityId, Is.EqualTo("tough_claws"),
                "§5.8 lists Tough Claws explicitly on Charizard Vanguard.");
        }

        [Test]
        public void CharmeleonVA2_Snipe_InfernoDragon()
        {
            EvolutionBranchSO va2 = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/charmander/charmeleon_va2.asset");
            Assert.That(va2.GrantedAbility, Is.Not.Null);
            Assert.That(va2.GrantedAbility.AbilityId, Is.EqualTo("snipe"));
        }

        [Test]
        public void Charizard_MasteryLv3_BandCompliant()
        {
            PokemonSpeciesSO va1 = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Charmander/Charizard_VanguardA1.asset");
            PokemonSpeciesSO va2 = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Charmander/Charizard_VanguardA2.asset");
            Assert.That(va1.MasteryMove, Is.EqualTo(va2.MasteryMove));
            MoveSO mastery = va1.MasteryMove;
            Assert.That(mastery.BasePower, Is.InRange(110, 140));
            Assert.That(mastery.APCost, Is.InRange(2, 3));
        }
    }
}
