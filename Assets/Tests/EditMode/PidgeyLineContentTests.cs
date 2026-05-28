using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.5 + §5.5.1 + §5.8 (Keen Eye, Healer) + §5.11.4 Mastery bands.
    // Pidgey line golden tests — Support archetype (no crit bonus per §5.3.4).
    public class PidgeyLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        private static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        [Test]
        public void Pidgey_BaseLearnsetHasFourMoves_NoPrimaryAbility()
        {
            PokemonSpeciesSO pidgey = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Pidgey/Pidgey.asset");
            Assert.That(pidgey, Is.Not.Null);
            Assert.That(pidgey.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(pidgey.PrimaryAbility, Is.Null);
            Assert.That(pidgey.Branches.Count, Is.EqualTo(1));
        }

        [Test]
        public void Pidgey_MasteryLv1_BandCompliant()
        {
            PokemonSpeciesSO pidgey = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Pidgey/Pidgey.asset");
            MoveSO mastery = pidgey.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(60, 80));
            Assert.That(mastery.APCost, Is.EqualTo(1));
            Assert.That(mastery.Modifier, Is.EqualTo(PositionalModifier.None));
        }

        [Test]
        public void PidgeyEvolveBranch_HasKeenEye_SupportArchetype_NoCritBonus()
        {
            // Per §5.3.4 — Support archetype reward is a team-sustain passive (Healer),
            // NOT crit chance. So CritChanceBonus stays 0 (Vanguard-only attribute).
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/pidgey/pidgey_evolve.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Archetype, Is.EqualTo(BranchArchetype.Support));
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("keen_eye"),
                "Pidgey line primary is Keen Eye (Vision passive — §5.5.3.1).");
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0f).Within(0.001f),
                "Support archetype does not grant a crit bonus.");
        }

        [Test]
        public void Pidgeotto_PrimaryAbilityIsKeenEye_SingleEvolveBranch()
        {
            PokemonSpeciesSO pidgeotto = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Pidgey/Pidgeotto.asset");
            Assert.That(pidgeotto.PrimaryAbility, Is.Not.Null);
            Assert.That(pidgeotto.PrimaryAbility.AbilityId, Is.EqualTo("keen_eye"));
            Assert.That(pidgeotto.Branches.Count, Is.EqualTo(1));
        }

        [Test]
        public void Pidgeotto_MasteryLv2_BandCompliant()
        {
            PokemonSpeciesSO pidgeotto = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Pidgey/Pidgeotto.asset");
            MoveSO mastery = pidgeotto.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(85, 110));
            Assert.That(mastery.APCost, Is.InRange(1, 2));
            bool hasSfSb = mastery.Modifier == PositionalModifier.StepForward
                        || mastery.Modifier == PositionalModifier.StepBackward;
            bool hasRider = mastery.Effects != null && mastery.Effects.Count >= 1;
            Assert.That(hasSfSb || hasRider, Is.True);
        }

        [Test]
        public void PidgeottoEvolveBranch_HasHealer()
        {
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/pidgey/pidgeotto_evolve.asset");
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("healer"),
                "Pidgeot secondary: Healer (§5.3.4 Support archetype team-sustain reward).");
        }

        [Test]
        public void Pidgeot_MasteryLv3_BandCompliant()
        {
            PokemonSpeciesSO pidgeot = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Pidgey/Pidgeot.asset");
            MoveSO mastery = pidgeot.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(110, 140));
            Assert.That(mastery.APCost, Is.InRange(2, 3));
            // Pidgey Lv3 (Air Slash) already carries a Confusion StatusRider —
            // partial satisfaction of the §5.11.4 'composite, species-unique effect'
            // requirement (tracked as gap #27 in BACKLOG for the full effect spec).
            Assert.That(mastery.Effects.Count, Is.GreaterThanOrEqualTo(1),
                "Pidgeot Lv3 Mastery already carries a status rider — pre-existing identity content.");
        }
    }
}
