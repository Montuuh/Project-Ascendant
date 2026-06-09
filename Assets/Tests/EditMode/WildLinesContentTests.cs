using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per CL-007 (§5.12.2) — golden content for wild lines (Caterpie, Geodude, Pidgey).
    // Wild lines use 1 archetype per stage (simpler than starters). Invariants:
    //   • Base form: 4 moves, no PrimaryAbility, 1 branch.
    //   • Stage-1 branch: ≤2 move upgrades, 0 new moves, no ability/crit, no sub-branches.
    //   • Mid form: no PrimaryAbility, 1 branch (stage-2).
    //   • Stage-2 branch: 0 upgrades, +1 signature move, no ability (additive / mix-safe).
    //   • Final form: no PrimaryAbility, no branches.
    public class WildLinesContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";
        private static T Load<T>(string p) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(p);
        private static PokemonSpeciesSO Wild(string sub, string f) => Load<PokemonSpeciesSO>($"{ROOT}/Species/Wild/{sub}/{f}.asset");
        private static EvolutionBranchSO Br(string sub, string f) => Load<EvolutionBranchSO>($"{ROOT}/Branches/{sub}/{f}.asset");

        // ── Caterpie / Metapod / Butterfree ──────────────────────────────────

        [Test]
        // §5.12.2 — Caterpie: 4 base moves, no ability, single branch → Metapod.
        public void Caterpie_SingleBranch_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Caterpie", "Caterpie");
            Assert.That(s, Is.Not.Null, "Caterpie.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Caterpie has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(1));
            EvolutionBranchSO b = s.Branches[0];
            PokemonSpeciesSO metapod = Wild("Caterpie", "Metapod");
            Assert.That(b.EvolvedSpecies, Is.SameAs(metapod));
        }

        [Test]
        // §5.12.2 — caterpie_evolve: ≤2 upgrades, 0 new, no ability/crit.
        public void CaterpieEvolveBranch_LighterPayload_NoAbility()
        {
            EvolutionBranchSO b = Br("caterpie", "caterpie_evolve");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "caterpie_evolve grants no ability (CL-007).");
            Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), "caterpie_evolve: ≤2 upgrades (§5.12.2).");
            Assert.That(b.NewMoves.Count, Is.EqualTo(0));
            Assert.That(b.SubBranches, Is.Empty);
        }

        [Test]
        // §5.12.2 — Metapod: no ability, single stage-2 branch → Butterfree.
        public void Metapod_SingleBranch_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Caterpie", "Metapod");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Metapod has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(1));
            EvolutionBranchSO b = s.Branches[0];
            PokemonSpeciesSO butterfree = Wild("Caterpie", "Butterfree");
            Assert.That(b.EvolvedSpecies, Is.SameAs(butterfree));
        }

        [Test]
        // §5.12.2 — metapod_evolve: 0 upgrades, +1 signature (Psybeam), no ability.
        public void MetapodEvolveBranch_AdditiveSignature_NoAbility()
        {
            EvolutionBranchSO b = Br("caterpie", "metapod_evolve");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "metapod_evolve grants no ability (CL-007).");
            Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 branch is purely additive.");
            Assert.That(b.NewMoves.Count, Is.EqualTo(1), "metapod_evolve adds exactly 1 signature (Psybeam).");
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
        // §5.12.2 — Geodude: 4 base moves, no ability, single branch → Graveler.
        public void Geodude_SingleBranch_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Geodude", "Geodude");
            Assert.That(s, Is.Not.Null, "Geodude.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Geodude has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(1));
            EvolutionBranchSO b = s.Branches[0];
            PokemonSpeciesSO graveler = Wild("Geodude", "Graveler");
            Assert.That(b.EvolvedSpecies, Is.SameAs(graveler));
        }

        [Test]
        // §5.12.2 — geodude_evolve: ≤2 upgrades, 0 new, no ability/crit.
        public void GeodudeEvolveBranch_LighterPayload_NoAbility()
        {
            EvolutionBranchSO b = Br("geodude", "geodude_evolve");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "geodude_evolve grants no ability (CL-007).");
            Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), "geodude_evolve: ≤2 upgrades (§5.12.2).");
            Assert.That(b.NewMoves.Count, Is.EqualTo(0));
            Assert.That(b.SubBranches, Is.Empty);
        }

        [Test]
        // §5.12.2 — Graveler: no ability, single stage-2 branch → Golem.
        public void Graveler_SingleBranch_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Geodude", "Graveler");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Graveler has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(1));
            EvolutionBranchSO b = s.Branches[0];
            PokemonSpeciesSO golem = Wild("Geodude", "Golem");
            Assert.That(b.EvolvedSpecies, Is.SameAs(golem));
        }

        [Test]
        // §5.12.2 — graveler_evolve: 0 upgrades, +1 signature (Body Press), no ability.
        public void GravelerEvolveBranch_AdditiveSignature_NoAbility()
        {
            EvolutionBranchSO b = Br("geodude", "graveler_evolve");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "graveler_evolve grants no ability (CL-007).");
            Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 branch is purely additive.");
            Assert.That(b.NewMoves.Count, Is.EqualTo(1), "graveler_evolve adds exactly 1 signature (Body Press).");
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
        // §5.12.2 — Pidgey: 4 base moves, no ability, single branch → Pidgeotto.
        public void Pidgey_SingleBranch_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Pidgey", "Pidgey");
            Assert.That(s, Is.Not.Null, "Pidgey.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Pidgey has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(1));
            EvolutionBranchSO b = s.Branches[0];
            PokemonSpeciesSO pidgeotto = Wild("Pidgey", "Pidgeotto");
            Assert.That(b.EvolvedSpecies, Is.SameAs(pidgeotto));
        }

        [Test]
        // §5.12.2 — pidgey_evolve: ≤2 upgrades, 0 new, no ability/crit.
        public void PidgeyEvolveBranch_LighterPayload_NoAbility()
        {
            EvolutionBranchSO b = Br("pidgey", "pidgey_evolve");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "pidgey_evolve grants no ability (CL-007).");
            Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), "pidgey_evolve: ≤2 upgrades (§5.12.2).");
            Assert.That(b.NewMoves.Count, Is.EqualTo(0));
            Assert.That(b.SubBranches, Is.Empty);
        }

        [Test]
        // §5.12.2 — Pidgeotto: no ability, single stage-2 branch → Pidgeot.
        public void Pidgeotto_SingleBranch_NoAbility()
        {
            PokemonSpeciesSO s = Wild("Pidgey", "Pidgeotto");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.PrimaryAbility, Is.Null, "Pidgeotto has no PrimaryAbility (§5.12.2).");
            Assert.That(s.Branches.Count, Is.EqualTo(1));
            EvolutionBranchSO b = s.Branches[0];
            PokemonSpeciesSO pidgeot = Wild("Pidgey", "Pidgeot");
            Assert.That(b.EvolvedSpecies, Is.SameAs(pidgeot));
        }

        [Test]
        // §5.12.2 — pidgeotto_evolve: 0 upgrades, +1 signature (Hurricane), no ability.
        public void PidgeottoEvolveBranch_AdditiveSignature_NoAbility()
        {
            EvolutionBranchSO b = Br("pidgey", "pidgeotto_evolve");
            Assert.That(b, Is.Not.Null);
            Assert.That(b.GrantedAbility, Is.Null, "pidgeotto_evolve grants no ability (CL-007).");
            Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), "Stage-2 branch is purely additive.");
            Assert.That(b.NewMoves.Count, Is.EqualTo(1), "pidgeotto_evolve adds exactly 1 signature (Hurricane).");
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
