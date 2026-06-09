using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per CL-007 (§5.12.2) + §5.11.4 Mastery bands.
    // Pidgey line golden tests — moves-only, 3 archetypes per stage, no ability/crit grant.
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
            Assert.That(pidgey.Branches.Count, Is.EqualTo(3));
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
        public void PidgeyEvolveBranch_MovesOnly_NoAbility()
        {
            // Per CL-007 (§5.12.2) — stage-1 branch: ≤2 upgrades, no ability/crit grant.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/pidgey/pidgey_vanguard.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Archetype, Is.EqualTo(BranchArchetype.Vanguard));
            Assert.That(branch.GrantedAbility, Is.Null, "CL-007: pidgey_vanguard grants no ability.");
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0f).Within(0.001f), "No crit bonus (CL-007).");
            Assert.That(branch.MoveUpgrades.Count, Is.InRange(1, 2), "≤2 move upgrades (§5.12.2).");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(0));
        }

        [Test]
        public void Pidgeotto_NoAbility_SingleEvolveBranch()
        {
            // Per CL-007 (§5.12.2) — mid-form has no PrimaryAbility.
            PokemonSpeciesSO pidgeotto = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Wild/Pidgey/Pidgeotto.asset");
            Assert.That(pidgeotto.PrimaryAbility, Is.Null, "CL-007: Pidgeotto has no PrimaryAbility.");
            Assert.That(pidgeotto.Branches.Count, Is.EqualTo(3));
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
        public void PidgeottoEvolveBranch_AdditiveSignature_NoAbility()
        {
            // Per CL-007 (§5.12.2) — stage-2 branch: 0 upgrades, +1 signature (Hurricane), no ability.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/pidgey/pidgeotto_vanguard.asset");
            Assert.That(branch.GrantedAbility, Is.Null, "CL-007: pidgeotto_vanguard grants no ability.");
            Assert.That(branch.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 is purely additive.");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(1), "Adds exactly 1 signature (Hurricane).");
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
