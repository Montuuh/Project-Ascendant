using NUnit.Framework;
using UnityEditor;
using System.Linq;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per CL-007 #D (§5.12.2) — golden content for wild lines (Caterpie, Geodude, Pidgey).
    // Wild lines now have 3 archetypes per stage, matching starter parity.
    // Invariants:
    //   • Base / mid form: 4 moves, no PrimaryAbility, 3 branches (Vanguard+Specialist+Support).
    //   • Stage-1 branches: ≤2 move upgrades, 0 new moves, no ability/crit, no sub-branches.
    //   • Stage-2 branches: 0 upgrades, +1 signature move each, no ability (additive / mix-safe).
    //   • Final form: no PrimaryAbility, no branches.
    public class WildLinesContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";
        private static T Load<T>(string p) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(p);
        private static PokemonSpeciesSO Wild(string sub, string f) => Load<PokemonSpeciesSO>($"{ROOT}/Species/Wild/{sub}/{f}.asset");
        private static EvolutionBranchSO Br(string sub, string f) => Load<EvolutionBranchSO>($"{ROOT}/Branches/{sub}/{f}.asset");

        // ── Caterpie / Metapod / Butterfree ──────────────────────────────────

        [Test]
        // §5.12.2 — Caterpie: 4 base moves, no ability, 3 archetype branches → Metapod.
        public void Caterpie_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Caterpie", "Caterpie");
            Assert.That(s, Is.Not.Null, "Caterpie.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Caterpie has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Caterpie: 3 archetype branches (CL-007 #D).");
            var archetypes = s.Branches.Select(b => b.Archetype).ToHashSet();
            Assert.That(archetypes, Does.Contain(BranchArchetype.Vanguard));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Specialist));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Support));
            PokemonSpeciesSO metapod = Wild("Caterpie", "Metapod");
            foreach (var b in s.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(metapod), $"{b.BranchId} should evolve to Metapod.");
        }

        [Test]
        // §5.12.2 — caterpie_vanguard: ≤2 upgrades, 0 new, no ability/crit.
        public void CaterpieVanguardBranch_LighterPayload_NoAbility()
        {
            EvolutionBranchSO b = Br("caterpie", "caterpie_vanguard");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.Archetype, Is.EqualTo(BranchArchetype.Vanguard));
            Assert.That(b.GrantedAbility, Is.Null, "caterpie_vanguard grants no ability (CL-007).");
            Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), "caterpie_vanguard: ≤2 upgrades (§5.12.2).");
            Assert.That(b.NewMoves.Count, Is.EqualTo(0));
            Assert.That(b.SubBranches, Is.Empty);
        }

        [Test]
        // §5.12.2 — Metapod: no ability, 3 archetype branches → Butterfree.
        public void Metapod_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Caterpie", "Metapod");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Metapod has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Metapod: 3 archetype branches (CL-007 #D).");
            var archetypes = s.Branches.Select(b => b.Archetype).ToHashSet();
            Assert.That(archetypes, Does.Contain(BranchArchetype.Vanguard));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Specialist));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Support));
            PokemonSpeciesSO butterfree = Wild("Caterpie", "Butterfree");
            foreach (var b in s.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(butterfree), $"{b.BranchId} should evolve to Butterfree.");
        }

        [Test]
        // §5.12.2 — metapod_vanguard: 0 upgrades, +1 signature, no ability.
        public void MetapodVanguardBranch_AdditiveSignature_NoAbility()
        {
            EvolutionBranchSO b = Br("caterpie", "metapod_vanguard");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "metapod_vanguard grants no ability (CL-007).");
            Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 branch is purely additive.");
            Assert.That(b.NewMoves.Count, Is.EqualTo(1), "metapod_vanguard adds exactly 1 signature (SilverWind).");
        }

        [Test]
        // §5.12.2 — All Metapod stage-2 branches are purely additive (+1 sig, 0 upgrades, no ability).
        public void MetapodAllBranches_AdditiveSignature_NoAbility()
        {
            foreach (string name in new[] { "metapod_vanguard", "metapod_specialist", "metapod_support" })
            {
                EvolutionBranchSO b = Br("caterpie", name);
                Assert.That(b, Is.Not.Null, $"{name}.asset missing");
                Assert.That(b.GrantedAbility, Is.Null, $"{name} grants no ability (CL-007).");
                Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), $"{name}: Stage-2 is purely additive.");
                Assert.That(b.NewMoves.Count, Is.EqualTo(1), $"{name}: adds exactly 1 signature.");
            }
        }

        [Test]
        // §5.12.2 — Butterfree: final form, no ability, no branches.
        public void Butterfree_Final_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Caterpie", "Butterfree");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Butterfree has no PrimaryAbility under CL-007.");
            Assert.That(s.Branches.Count, Is.EqualTo(0), "Final form has no branches.");
        }

        // ── Geodude / Graveler / Golem ────────────────────────────────────────

        [Test]
        // §5.12.2 — Geodude: 4 base moves, no ability, 3 archetype branches → Graveler.
        public void Geodude_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Geodude", "Geodude");
            Assert.That(s, Is.Not.Null, "Geodude.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Geodude has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Geodude: 3 archetype branches (CL-007 #D).");
            var archetypes = s.Branches.Select(b => b.Archetype).ToHashSet();
            Assert.That(archetypes, Does.Contain(BranchArchetype.Vanguard));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Specialist));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Support));
            PokemonSpeciesSO graveler = Wild("Geodude", "Graveler");
            foreach (var b in s.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(graveler), $"{b.BranchId} should evolve to Graveler.");
        }

        [Test]
        // §5.12.2 — geodude_vanguard: ≤2 upgrades, 0 new, no ability/crit.
        public void GeodudeVanguardBranch_LighterPayload_NoAbility()
        {
            EvolutionBranchSO b = Br("geodude", "geodude_vanguard");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.Archetype, Is.EqualTo(BranchArchetype.Vanguard));
            Assert.That(b.GrantedAbility, Is.Null, "geodude_vanguard grants no ability (CL-007).");
            Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), "geodude_vanguard: ≤2 upgrades (§5.12.2).");
            Assert.That(b.NewMoves.Count, Is.EqualTo(0));
            Assert.That(b.SubBranches, Is.Empty);
        }

        [Test]
        // §5.12.2 — Graveler: no ability, 3 archetype branches → Golem.
        public void Graveler_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Geodude", "Graveler");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Graveler has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Graveler: 3 archetype branches (CL-007 #D).");
            var archetypes = s.Branches.Select(b => b.Archetype).ToHashSet();
            Assert.That(archetypes, Does.Contain(BranchArchetype.Vanguard));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Specialist));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Support));
            PokemonSpeciesSO golem = Wild("Geodude", "Golem");
            foreach (var b in s.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(golem), $"{b.BranchId} should evolve to Golem.");
        }

        [Test]
        // §5.12.2 — graveler_vanguard: 0 upgrades, +1 signature (Body Press), no ability.
        public void GravelerVanguardBranch_AdditiveSignature_NoAbility()
        {
            EvolutionBranchSO b = Br("geodude", "graveler_vanguard");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "graveler_vanguard grants no ability (CL-007).");
            Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 branch is purely additive.");
            Assert.That(b.NewMoves.Count, Is.EqualTo(1), "graveler_vanguard adds exactly 1 signature (Body Press).");
        }

        [Test]
        // §5.12.2 — All Graveler stage-2 branches are purely additive (+1 sig, 0 upgrades, no ability).
        public void GravelerAllBranches_AdditiveSignature_NoAbility()
        {
            foreach (string name in new[] { "graveler_vanguard", "graveler_specialist", "graveler_support" })
            {
                EvolutionBranchSO b = Br("geodude", name);
                Assert.That(b, Is.Not.Null, $"{name}.asset missing");
                Assert.That(b.GrantedAbility, Is.Null, $"{name} grants no ability (CL-007).");
                Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), $"{name}: Stage-2 is purely additive.");
                Assert.That(b.NewMoves.Count, Is.EqualTo(1), $"{name}: adds exactly 1 signature.");
            }
        }

        [Test]
        // §5.12.2 — Golem: final form, no ability, no branches.
        public void Golem_Final_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Geodude", "Golem");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Golem has no PrimaryAbility under CL-007.");
            Assert.That(s.Branches.Count, Is.EqualTo(0), "Final form has no branches.");
        }

        // ── Pidgey / Pidgeotto / Pidgeot ─────────────────────────────────────

        [Test]
        // §5.12.2 — Pidgey: 4 base moves, no ability, 3 archetype branches → Pidgeotto.
        public void Pidgey_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Pidgey", "Pidgey");
            Assert.That(s, Is.Not.Null, "Pidgey.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Pidgey has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Pidgey: 3 archetype branches (CL-007 #D).");
            var archetypes = s.Branches.Select(b => b.Archetype).ToHashSet();
            Assert.That(archetypes, Does.Contain(BranchArchetype.Vanguard));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Specialist));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Support));
            PokemonSpeciesSO pidgeotto = Wild("Pidgey", "Pidgeotto");
            foreach (var b in s.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(pidgeotto), $"{b.BranchId} should evolve to Pidgeotto.");
        }

        [Test]
        // §5.12.2 — pidgey_vanguard: ≤2 upgrades, 0 new, no ability/crit.
        public void PidgeyVanguardBranch_LighterPayload_NoAbility()
        {
            EvolutionBranchSO b = Br("pidgey", "pidgey_vanguard");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.Archetype, Is.EqualTo(BranchArchetype.Vanguard));
            Assert.That(b.GrantedAbility, Is.Null, "pidgey_vanguard grants no ability (CL-007).");
            Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), "pidgey_vanguard: ≤2 upgrades (§5.12.2).");
            Assert.That(b.NewMoves.Count, Is.EqualTo(0));
            Assert.That(b.SubBranches, Is.Empty);
        }

        [Test]
        // §5.12.2 — Pidgeotto: no ability, 3 archetype branches → Pidgeot.
        public void Pidgeotto_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Pidgey", "Pidgeotto");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Pidgeotto has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Pidgeotto: 3 archetype branches (CL-007 #D).");
            var archetypes = s.Branches.Select(b => b.Archetype).ToHashSet();
            Assert.That(archetypes, Does.Contain(BranchArchetype.Vanguard));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Specialist));
            Assert.That(archetypes, Does.Contain(BranchArchetype.Support));
            PokemonSpeciesSO pidgeot = Wild("Pidgey", "Pidgeot");
            foreach (var b in s.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(pidgeot), $"{b.BranchId} should evolve to Pidgeot.");
        }

        [Test]
        // §5.12.2 — pidgeotto_vanguard: 0 upgrades, +1 signature (Hurricane), no ability.
        public void PidgeottoVanguardBranch_AdditiveSignature_NoAbility()
        {
            EvolutionBranchSO b = Br("pidgey", "pidgeotto_vanguard");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "pidgeotto_vanguard grants no ability (CL-007).");
            Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 branch is purely additive.");
            Assert.That(b.NewMoves.Count, Is.EqualTo(1), "pidgeotto_vanguard adds exactly 1 signature (Hurricane).");
        }

        [Test]
        // §5.12.2 — All Pidgeotto stage-2 branches are purely additive (+1 sig, 0 upgrades, no ability).
        public void PidgeottoAllBranches_AdditiveSignature_NoAbility()
        {
            foreach (string name in new[] { "pidgeotto_vanguard", "pidgeotto_specialist", "pidgeotto_support" })
            {
                EvolutionBranchSO b = Br("pidgey", name);
                Assert.That(b, Is.Not.Null, $"{name}.asset missing");
                Assert.That(b.GrantedAbility, Is.Null, $"{name} grants no ability (CL-007).");
                Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), $"{name}: Stage-2 is purely additive.");
                Assert.That(b.NewMoves.Count, Is.EqualTo(1), $"{name}: adds exactly 1 signature.");
            }
        }

        [Test]
        // §5.12.2 — Pidgeot: final form, no ability, no branches.
        public void Pidgeot_Final_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Pidgey", "Pidgeot");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Pidgeot has no PrimaryAbility under CL-007.");
            Assert.That(s.Branches.Count, Is.EqualTo(0), "Final form has no branches.");
        }
    }
}
