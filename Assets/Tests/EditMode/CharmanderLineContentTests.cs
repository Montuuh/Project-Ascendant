using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per CL-007 (§5.12.2) — golden content for the Charmander line under the moves-only /
    // one-species-per-stage model: 3 archetype branches per evolving stage, NO ability/crit grant,
    // lighter payload (≤2 upgrades stage-1, +1 signature stage-2), and gap #46 closed (one Charizard).
    public class CharmanderLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";
        private static T Load<T>(string p) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(p);
        private static PokemonSpeciesSO Sp(string f) => Load<PokemonSpeciesSO>($"{ROOT}/Species/Starters/Charmander/{f}.asset");
        private static EvolutionBranchSO Br(string f) => Load<EvolutionBranchSO>($"{ROOT}/Branches/charmander/{f}.asset");

        [Test]
        // §5.12.2 — Charmander: 4 base moves, no pre-evo ability, 3 archetype branches all → Charmeleon.
        public void Charmander_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Sp("Charmander");
            Assert.That(s, Is.Not.Null, "Charmander.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Pre-evolution Charmander has no PrimaryAbility (§5.5.1).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Charmander offers Vanguard/Specialist/Support (§5.12.2).");

            PokemonSpeciesSO charmeleon = Sp("Charmeleon");
            HashSet<BranchArchetype> archs = new();
            foreach (EvolutionBranchSO b in s.Branches)
            {
                Assert.That(b.EvolvedSpecies, Is.SameAs(charmeleon), "All stage-1 archetypes evolve to the same Charmeleon (moves-only).");
                archs.Add(b.Archetype);
            }
            Assert.That(archs, Is.EquivalentTo(new[]
                { BranchArchetype.Vanguard, BranchArchetype.Specialist, BranchArchetype.Support }));
        }

        [Test]
        // §5.12.2 — stage-1 branches: moves-only, 1–2 upgrades, 0 new, no ability/crit, no sub-branches.
        public void CharmanderBranches_MovesOnly_LighterPayload()
        {
            foreach (string id in new[] { "charmander_vanguard", "charmander_specialist", "charmander_support" })
            {
                EvolutionBranchSO b = Br(id);
                Assert.That(b, Is.Not.Null, id);
                Assert.That(b.GrantedAbility, Is.Null, $"{id} grants no ability (CL-007).");
                Assert.That(b.CritChanceBonus, Is.EqualTo(0f).Within(0.0001f), $"{id} grants no crit (CL-007).");
                Assert.That(b.MoveUpgrades.Count, Is.InRange(1, 2), $"{id} has ≤2 move upgrades (§5.12.2 lighter payload).");
                Assert.That(b.NewMoves.Count, Is.EqualTo(0), $"{id} stage-1 adds no new pool moves.");
                Assert.That(b.SubBranches, Is.Empty, $"{id} has no A1/A2 sub-branches (removed).");
            }
        }

        [Test]
        // §5.12.2 — Charmeleon: one consolidated SO; 3 branches → Charizard; Lv2 Mastery band (§5.11.4).
        public void Charmeleon_ThreeBranches_MasteryLv2Band()
        {
            PokemonSpeciesSO w = Sp("Charmeleon");
            Assert.That(w, Is.Not.Null, "Charmeleon.asset (consolidated) missing");
            Assert.That(w.PrimaryAbility, Is.Null, "Charmeleon has no PrimaryAbility under CL-007 (§5.12.2).");
            Assert.That(w.Branches.Count, Is.EqualTo(3));
            PokemonSpeciesSO charizard = Sp("Charizard");
            foreach (EvolutionBranchSO b in w.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(charizard));

            MoveSO m = w.MasteryMove;
            Assert.That(m, Is.Not.Null, "Charmeleon MasteryMove (§4.3.9.2).");
            Assert.That(m.BasePower, Is.InRange(85, 110), "Lv2 Mastery band 85–110 (§5.11.4).");
            Assert.That(m.APCost, Is.InRange(1, 2));
        }

        [Test]
        // §5.12.2 — stage-2 branches: additive (+1 signature), 0 upgrades, no ability (mix-safe).
        public void CharmeleonBranches_AdditiveSignature()
        {
            foreach (string id in new[] { "charmeleon_vanguard", "charmeleon_specialist", "charmeleon_support" })
            {
                EvolutionBranchSO b = Br(id);
                Assert.That(b, Is.Not.Null, id);
                Assert.That(b.GrantedAbility, Is.Null, $"{id} grants no ability (CL-007).");
                Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), $"{id} stage-2 is purely additive (mix-safe).");
                Assert.That(b.NewMoves.Count, Is.EqualTo(1), $"{id} adds exactly 1 signature move.");
            }
        }

        [Test]
        // §5.12.2 / gap #46 — Charizard is ONE final SO with a unique id, no branches, Lv3 Mastery band.
        public void Charizard_UniqueId_Final_MasteryLv3()
        {
            PokemonSpeciesSO c = Sp("Charizard");
            Assert.That(c, Is.Not.Null, "Charizard.asset (consolidated A1/A2 → one) missing");
            Assert.That(c.SpeciesId, Is.EqualTo("charizard"), "Final-form SpeciesId is unique (gap #46 closed).");
            Assert.That(c.PrimaryAbility, Is.Null, "Charizard has no PrimaryAbility under CL-007 (§5.12.2).");
            Assert.That(c.Branches.Count, Is.EqualTo(0), "Final form has no branches.");

            MoveSO m = c.MasteryMove;
            Assert.That(m, Is.Not.Null);
            Assert.That(m.BasePower, Is.InRange(110, 140), "Lv3 Mastery band 110–140 (§5.11.4).");
            Assert.That(m.APCost, Is.InRange(2, 3));
        }

        [Test]
        // §5.12.2 — cross-archetype mix: Vanguard (stage 1) then Specialist (stage 2).
        // Pool reflects both archetypes; species advances to Charizard; no ability is granted.
        public void Evolution_CrossArchetypeMix_PoolReflectsBoth_NoAbility()
        {
            PokemonSpeciesSO charmander = Sp("Charmander");
            PokemonInstanceFactory factory = new();
            PokemonInstance p = factory.Create(charmander, 12);

            EvolutionExecutor.Evolve(p, Br("charmander_vanguard"));    // stage 1: Vanguard
            EvolutionExecutor.Evolve(p, Br("charmeleon_specialist"));  // stage 2: Specialist (the mix)

            Assert.That(p.Species.SpeciesId, Is.EqualTo("charizard"));
            Assert.That(p.CurrentStage, Is.EqualTo(EvolutionStage.Stage2));
            List<string> ids = p.LearnedMoves.ConvertAll(m => m.MoveId);
            Assert.That(ids, Has.Member("dragon_claw"), "Vanguard stage-1 upgrade survived into the pool.");
            Assert.That(ids, Has.Member("flame_wheel"), "Vanguard stage-1 upgrade survived into the pool.");
            Assert.That(ids, Has.Member("flamethrower"), "Specialist stage-2 signature added (the mix).");
            Assert.That(p.Ability, Is.Null, "CL-007 — evolution grants no ability.");
            factory.Release(p);
        }
    }
}
