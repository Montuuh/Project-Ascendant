using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.1 + §5.5.1 + §5.8 (Overgrow) + §5.11.4 Mastery bands.
    // Bulbasaur line golden tests — Vanguard branch only (VS scope).
    public class BulbasaurLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        private static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        // ── §5.5.1 — Bulbasaur base form has no PrimaryAbility ──────────────

        [Test]
        public void Bulbasaur_BaseLearnsetHasFourMoves_NoPrimaryAbility()
        {
            PokemonSpeciesSO bulbasaur = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Bulbasaur/Bulbasaur.asset");
            Assert.That(bulbasaur, Is.Not.Null);
            Assert.That(bulbasaur.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(bulbasaur.PrimaryAbility, Is.Null,
                "Pre-evo Bulbasaur must not have a PrimaryAbility (§5.5.1).");
            Assert.That(bulbasaur.Branches.Count, Is.EqualTo(1),
                "VS scope: Bulbasaur has only the Vanguard branch.");
        }

        [Test]
        public void Bulbasaur_MasteryLv1_BandCompliant()
        {
            PokemonSpeciesSO bulbasaur = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Bulbasaur/Bulbasaur.asset");
            MoveSO mastery = bulbasaur.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(60, 80),
                $"Lv1 Mastery Power={mastery.BasePower} outside §5.11.4 band.");
            Assert.That(mastery.APCost, Is.EqualTo(1));
            Assert.That(mastery.Modifier, Is.EqualTo(PositionalModifier.None));
        }

        // ── §5.3.4 + §5.6 — bulbasaur_vanguard branch ───────────────────────

        [Test]
        public void BulbasaurVanguardBranch_HasOvergrowAndTenPercentCritBonus()
        {
            // Per §5.8 — Bulbasaur line primary ability is Overgrow.
            // Per §5.3.4 — Vanguard stage 1 grants a flat crit bonus (matches §5.6's
            // 10% on the Squirtle worked example; applied consistently across starters).
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/bulbasaur/bulbasaur_vanguard.asset");
            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.GrantedAbility, Is.Not.Null);
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("overgrow"));
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(branch.NewMoves.Count, Is.EqualTo(0),
                "Vanguard stage-1 evolution is upgrade-only; pool size stays 4.");
        }

        // ── Ivysaur stage-1 form ────────────────────────────────────────────

        [Test]
        public void IvysaurVanguard_PrimaryAbilityIsOvergrow_TwoSubBranches()
        {
            PokemonSpeciesSO ivysaur = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Bulbasaur/Ivysaur_Vanguard.asset");
            Assert.That(ivysaur.PrimaryAbility, Is.Not.Null);
            Assert.That(ivysaur.PrimaryAbility.AbilityId, Is.EqualTo("overgrow"));
            Assert.That(ivysaur.Branches.Count, Is.EqualTo(2),
                "Ivysaur Vanguard offers 2 sub-branches (VA1 Bloom Brawler / VA2 Toxic Briar).");
        }

        [Test]
        public void Ivysaur_MasteryLv2_BandCompliant()
        {
            PokemonSpeciesSO ivysaur = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Bulbasaur/Ivysaur_Vanguard.asset");
            MoveSO mastery = ivysaur.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(85, 110),
                $"Lv2 Mastery Power={mastery.BasePower} outside §5.11.4 band.");
            Assert.That(mastery.APCost, Is.InRange(1, 2));
            // §5.11.4 — must carry SF/SB OR a status rider.
            // Ranged Razor Leaf cannot carry SF/SB; satisfies the band via the
            // Bulbasaur Lv2 achievement themed Poison rider (§5.11).
            bool hasSfSb = mastery.Modifier == PositionalModifier.StepForward
                        || mastery.Modifier == PositionalModifier.StepBackward;
            bool hasRider = mastery.Effects != null && mastery.Effects.Count >= 1;
            Assert.That(hasSfSb || hasRider, Is.True,
                "Lv2 Mastery must carry SF/SB or a status rider per §5.11.4.");
        }

        // ── Venusaur sub-branches ───────────────────────────────────────────

        [Test]
        public void IvysaurVA1_ToughClaws_BloomBrawler()
        {
            EvolutionBranchSO va1 = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/bulbasaur/ivysaur_va1.asset");
            Assert.That(va1.GrantedAbility, Is.Not.Null);
            Assert.That(va1.GrantedAbility.AbilityId, Is.EqualTo("tough_claws"),
                "VA1 secondary: Tough Claws (melee +15% — Vanguard-themed identity).");
            Assert.That(va1.MoveUpgrades.Count, Is.EqualTo(3));
        }

        [Test]
        public void IvysaurVA2_Snipe_ToxicBriar()
        {
            EvolutionBranchSO va2 = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/bulbasaur/ivysaur_va2.asset");
            Assert.That(va2.GrantedAbility, Is.Not.Null);
            Assert.That(va2.GrantedAbility.AbilityId, Is.EqualTo("snipe"),
                "VA2 secondary: Snipe (ranged +15% — Toxic Briar identity).");
            Assert.That(va2.MoveUpgrades.Count, Is.EqualTo(3));
        }

        // ── Venusaur Lv3 Mastery ────────────────────────────────────────────

        [Test]
        public void Venusaur_MasteryLv3_BandCompliant()
        {
            PokemonSpeciesSO va1 = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Bulbasaur/Venusaur_VanguardA1.asset");
            PokemonSpeciesSO va2 = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Bulbasaur/Venusaur_VanguardA2.asset");
            Assert.That(va1.MasteryMove, Is.EqualTo(va2.MasteryMove),
                "Both Vanguard sub-branches share the Lv3 Mastery move.");
            MoveSO mastery = va1.MasteryMove;
            Assert.That(mastery.BasePower, Is.InRange(110, 140));
            Assert.That(mastery.APCost, Is.InRange(2, 3));
        }
    }
}
