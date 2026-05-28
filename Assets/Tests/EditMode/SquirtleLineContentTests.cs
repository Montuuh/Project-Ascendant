using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.3 + §5.6 worked example + §5.11.4 Mastery bands.
    // Golden-content tests for the Squirtle line — load real project assets and
    // assert the canonical, locked spec. Any drift on disk fails the suite.
    public class SquirtleLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        private static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        // ── §5.6 — Squirtle base form ───────────────────────────────────────

        [Test]
        public void Squirtle_BaseLearnsetHasFourMoves_NoPrimaryAbility()
        {
            // Per §5.6 — base pool = {Tackle, Water Gun, Withdraw, Tail Whip}.
            // Per §5.5.1 — pre-evolution: no PrimaryAbility.
            PokemonSpeciesSO squirtle = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Squirtle/Squirtle.asset");
            Assert.That(squirtle, Is.Not.Null, "Squirtle.asset missing");
            Assert.That(squirtle.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(squirtle.PrimaryAbility, Is.Null,
                "Pre-evolution Squirtle must not have a PrimaryAbility (§5.5.1).");
            Assert.That(squirtle.Branches.Count, Is.EqualTo(1),
                "Squirtle has one stage-1 branch (Vanguard) in VS scope.");
        }

        [Test]
        public void Squirtle_MasteryLv1_BandCompliant()
        {
            // Per §5.11.4 — Lv1 Mastery band: Power 60–80, AP 1, no SF/SB modifier.
            PokemonSpeciesSO squirtle = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Squirtle/Squirtle.asset");
            MoveSO mastery = squirtle.MasteryMove;
            Assert.That(mastery, Is.Not.Null, "Squirtle has no MasteryMove (§4.3.9.2).");
            Assert.That(mastery.BasePower, Is.InRange(60, 80),
                $"Lv1 Mastery '{mastery.DisplayName}' Power={mastery.BasePower} outside §5.11.4 band 60–80.");
            Assert.That(mastery.APCost, Is.EqualTo(1),
                "Lv1 Mastery AP must be 1 per §5.11.4.");
            Assert.That(mastery.Modifier, Is.EqualTo(PositionalModifier.None),
                "Lv1 Mastery may not carry SF/SB per §5.11.4.");
        }

        // ── §5.6 — Squirtle → Wartortle Vanguard branch ─────────────────────

        [Test]
        public void SquirtleVanguardBranch_HasTorrentAndTenPercentCritBonus()
        {
            // Per §5.6 — taking Squirtle Vanguard grants Torrent + CritChance +10%.
            EvolutionBranchSO branch = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/squirtle/squirtle_vanguard.asset");
            Assert.That(branch, Is.Not.Null, "squirtle_vanguard branch missing");
            Assert.That(branch.GrantedAbility, Is.Not.Null,
                "Vanguard stage-1 branch must record its granted ability (§5.6).");
            Assert.That(branch.GrantedAbility.AbilityId, Is.EqualTo("torrent"));
            Assert.That(branch.CritChanceBonus, Is.EqualTo(0.1f).Within(0.001f),
                "§5.6 — Squirtle Vanguard stage 1 grants CritChance +10%.");
            Assert.That(branch.MoveUpgrades.Count, Is.EqualTo(2),
                "§5.6 — Tackle→Skull Bash and Tail Whip→Aqua Jet are the only stage-1 upgrades.");
            Assert.That(branch.NewMoves.Count, Is.EqualTo(0),
                "§5.6 — Squirtle Vanguard adds no new pool entries; upgrades only.");
        }

        // ── §5.6 — Wartortle Vanguard species ────────────────────────────────

        [Test]
        public void WartortleVanguard_PrimaryAbilityIsTorrent_TwoSubBranches()
        {
            // Per §5.6 — Wartortle Vanguard has Torrent (Water +20% when HP < 30%)
            // and 2 sub-branches (Heavy Brawler VA1 / Aqua-Jet Duelist VA2).
            PokemonSpeciesSO wartortle = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Squirtle/Wartortle_Vanguard.asset");
            Assert.That(wartortle.PrimaryAbility, Is.Not.Null);
            Assert.That(wartortle.PrimaryAbility.AbilityId, Is.EqualTo("torrent"));
            Assert.That(wartortle.Branches.Count, Is.EqualTo(2),
                "Wartortle Vanguard offers 2 sub-branches (VA1, VA2) per §5.6.");
        }

        [Test]
        public void Wartortle_MasteryLv2_BandCompliant()
        {
            // Per §5.11.4 — Lv2 Mastery band: Power 85–110, AP 1–2, SF/SB or 1 rider.
            PokemonSpeciesSO wartortle = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Squirtle/Wartortle_Vanguard.asset");
            MoveSO mastery = wartortle.MasteryMove;
            Assert.That(mastery, Is.Not.Null);
            Assert.That(mastery.BasePower, Is.InRange(85, 110),
                $"Lv2 Mastery Power={mastery.BasePower} outside §5.11.4 band 85–110.");
            Assert.That(mastery.APCost, Is.InRange(1, 2));
            bool hasSfSb = mastery.Modifier == PositionalModifier.StepForward
                        || mastery.Modifier == PositionalModifier.StepBackward;
            bool hasRider = mastery.Effects != null && mastery.Effects.Count >= 1;
            Assert.That(hasSfSb || hasRider, Is.True,
                "Lv2 Mastery must carry SF/SB or a status rider per §5.11.4.");
        }

        // ── §5.6 — Wartortle → Blastoise sub-branches ────────────────────────

        [Test]
        public void WartortleVA1_ShellArmor_HeavyBrawler()
        {
            EvolutionBranchSO va1 = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/squirtle/wartortle_va1.asset");
            Assert.That(va1, Is.Not.Null);
            Assert.That(va1.GrantedAbility, Is.Not.Null);
            Assert.That(va1.GrantedAbility.AbilityId, Is.EqualTo("shell_armor"),
                "§5.6 — Blastoise VA1 secondary passive is Shell Armor.");
            Assert.That(va1.MoveUpgrades.Count, Is.EqualTo(3),
                "§5.6 — VA1 upgrades Skull Bash→Hydro Crash, Water Gun→Surf, Withdraw→Aqua Ring.");
        }

        [Test]
        public void WartortleVA2_SwiftSwim_AquaJetDuelist()
        {
            EvolutionBranchSO va2 = Load<EvolutionBranchSO>(
                $"{ROOT}/Branches/squirtle/wartortle_va2.asset");
            Assert.That(va2, Is.Not.Null);
            Assert.That(va2.GrantedAbility, Is.Not.Null);
            Assert.That(va2.GrantedAbility.AbilityId, Is.EqualTo("swift_swim"),
                "§5.6 — Blastoise VA2 secondary passive is Swift Swim.");
            Assert.That(va2.MoveUpgrades.Count, Is.EqualTo(3),
                "§5.6 — VA2 upgrades Skull Bash→Skull Bash+, Water Gun→Hydro Pump, Aqua Jet→Aqua Jet+.");
        }

        // ── §5.11.4 — Blastoise Lv3 Mastery ──────────────────────────────────

        [Test]
        public void Blastoise_MasteryLv3_BandCompliant()
        {
            // Per §5.11.4 — Lv3 Mastery band: Power 110–140, AP 2–3.
            // Composite/unique effect is required per spec but tracked separately
            // (Effects=[] is the current stub — see ⚠ OPEN §5.11.4 in BACKLOG).
            PokemonSpeciesSO blastoiseA1 = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Squirtle/Blastoise_VanguardA1.asset");
            PokemonSpeciesSO blastoiseA2 = Load<PokemonSpeciesSO>(
                $"{ROOT}/Species/Starters/Squirtle/Blastoise_VanguardA2.asset");
            MoveSO masteryA1 = blastoiseA1.MasteryMove;
            MoveSO masteryA2 = blastoiseA2.MasteryMove;
            Assert.That(masteryA1, Is.EqualTo(masteryA2),
                "§5.6 — both Vanguard sub-branches share the same Lv3 Mastery move.");
            Assert.That(masteryA1.BasePower, Is.InRange(110, 140),
                $"Lv3 Mastery Power={masteryA1.BasePower} outside §5.11.4 band 110–140.");
            Assert.That(masteryA1.APCost, Is.InRange(2, 3),
                $"Lv3 Mastery AP={masteryA1.APCost} outside §5.11.4 band 2–3.");
        }
    }
}
