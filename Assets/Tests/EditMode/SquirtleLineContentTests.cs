using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per CL-007 (§5.12.2) — golden content for the Squirtle line under the moves-only /
    // one-species-per-stage model: 3 archetype branches per evolving stage, NO ability/crit grant,
    // lighter payload (≤2 upgrades stage-1, +1 signature stage-2), and gap #46 closed (one Blastoise).
    public class SquirtleLineContentTests
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";
        private static T Load<T>(string p) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(p);
        private static PokemonSpeciesSO Sp(string f) => Load<PokemonSpeciesSO>($"{ROOT}/Species/Starters/Squirtle/{f}.asset");
        private static EvolutionBranchSO Br(string f) => Load<EvolutionBranchSO>($"{ROOT}/Branches/squirtle/{f}.asset");
        private static MoveSO Mv(string sub) => Load<MoveSO>($"{ROOT}/Moves/{sub}.asset");

        [Test]
        // §5.12.2 — Squirtle: 4 base moves, no pre-evo ability, 3 archetype branches all → Wartortle.
        public void Squirtle_ThreeArchetypeBranches_NoAbility()
        {
            PokemonSpeciesSO s = Sp("Squirtle");
            Assert.That(s, Is.Not.Null, "Squirtle.asset missing");
            Assert.That(s.BaseLearnset.Count, Is.EqualTo(4));
            Assert.That(s.PrimaryAbility, Is.Null, "Pre-evolution Squirtle has no PrimaryAbility (§5.5.1).");
            Assert.That(s.Branches.Count, Is.EqualTo(3), "Squirtle offers Vanguard/Specialist/Support (§5.12.2).");

            PokemonSpeciesSO wartortle = Sp("Wartortle");
            HashSet<BranchArchetype> archs = new();
            foreach (EvolutionBranchSO b in s.Branches)
            {
                Assert.That(b.EvolvedSpecies, Is.SameAs(wartortle), "All stage-1 archetypes evolve to the same Wartortle (moves-only).");
                archs.Add(b.Archetype);
            }
            Assert.That(archs, Is.EquivalentTo(new[]
                { BranchArchetype.Vanguard, BranchArchetype.Specialist, BranchArchetype.Support }));
        }

        [Test]
        // §5.12.2 — stage-1 branches: moves-only, 1–2 upgrades, 0 new, no ability/crit, no sub-branches.
        public void SquirtleBranches_MovesOnly_LighterPayload()
        {
            foreach (string id in new[] { "squirtle_vanguard", "squirtle_specialist", "squirtle_support" })
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
        // §5.12.2 — Wartortle: one consolidated SO; 3 branches → Blastoise; Lv2 Mastery band (§5.11.4).
        public void Wartortle_ThreeBranches_MasteryLv2Band()
        {
            PokemonSpeciesSO w = Sp("Wartortle");
            Assert.That(w, Is.Not.Null, "Wartortle.asset (consolidated) missing");
            Assert.That(w.Branches.Count, Is.EqualTo(3));
            PokemonSpeciesSO blastoise = Sp("Blastoise");
            foreach (EvolutionBranchSO b in w.Branches)
                Assert.That(b.EvolvedSpecies, Is.SameAs(blastoise));

            MoveSO m = w.MasteryMove;
            Assert.That(m, Is.Not.Null, "Wartortle MasteryMove (§4.3.9.2).");
            Assert.That(m.BasePower, Is.InRange(85, 110), "Lv2 Mastery band 85–110 (§5.11.4).");
            Assert.That(m.APCost, Is.InRange(1, 2));
        }

        [Test]
        // §5.12.2 — stage-2 branches: additive (+1 signature), 0 upgrades, no ability (mix-safe).
        public void WartortleBranches_AdditiveSignature()
        {
            foreach (string id in new[] { "wartortle_vanguard", "wartortle_specialist", "wartortle_support" })
            {
                EvolutionBranchSO b = Br(id);
                Assert.That(b, Is.Not.Null, id);
                Assert.That(b.GrantedAbility, Is.Null, $"{id} grants no ability (CL-007).");
                Assert.That(b.MoveUpgrades.Count, Is.EqualTo(0), $"{id} stage-2 is purely additive (mix-safe).");
                Assert.That(b.NewMoves.Count, Is.EqualTo(1), $"{id} adds exactly 1 signature move.");
            }
        }

        [Test]
        // §5.12.2 / gap #46 — Blastoise is ONE final SO with a unique id, no branches, Lv3 Mastery band.
        public void Blastoise_UniqueId_Final_MasteryLv3()
        {
            PokemonSpeciesSO b = Sp("Blastoise");
            Assert.That(b, Is.Not.Null, "Blastoise.asset (consolidated A1/A2 → one) missing");
            Assert.That(b.SpeciesId, Is.EqualTo("blastoise"), "Final-form SpeciesId is unique (gap #46 closed).");
            Assert.That(b.Branches.Count, Is.EqualTo(0), "Final form has no branches.");

            MoveSO m = b.MasteryMove;
            Assert.That(m, Is.Not.Null);
            Assert.That(m.BasePower, Is.InRange(110, 140), "Lv3 Mastery band 110–140 (§5.11.4).");
            Assert.That(m.APCost, Is.InRange(2, 3));
        }

        [Test]
        // §5.12.2 — the new Specialist/Support move assets exist with the intended shape.
        public void NewArchetypeMoves_ExistWithCorrectShape()
        {
            MoveSO waterPulse = Mv("Squirtle/water_pulse");
            Assert.That(waterPulse, Is.Not.Null, "water_pulse missing");
            Assert.That(waterPulse.Range, Is.EqualTo(MoveRange.Ranged));
            Assert.That(waterPulse.Effects, Is.Not.Empty, "Water Pulse carries a Confusion rider.");

            MoveSO charm = Mv("Squirtle/charm");
            Assert.That(charm, Is.Not.Null, "charm missing");
            Assert.That(charm.Role, Is.EqualTo(MoveRole.Utility));

            MoveSO ironDefense = Mv("Squirtle/iron_defense");
            Assert.That(ironDefense, Is.Not.Null, "iron_defense missing");
            Assert.That(ironDefense.Modifier, Is.EqualTo(PositionalModifier.StepBackward));

            MoveSO aquaFortress = Mv("Squirtle/aqua_fortress");
            Assert.That(aquaFortress, Is.Not.Null, "aqua_fortress missing");
            Assert.That(aquaFortress.Effects.Count, Is.EqualTo(2), "Aqua Fortress = regen + Defense buff.");
        }

        [Test]
        // §5.12.2 — cross-archetype mix on REAL content: Vanguard (stage 1) then Specialist (stage 2).
        // The pool reflects BOTH archetypes; species advances to Blastoise; no ability is granted.
        public void Evolution_CrossArchetypeMix_PoolReflectsBoth_NoAbility()
        {
            PokemonSpeciesSO squirtle = Sp("Squirtle");
            PokemonInstanceFactory factory = new();
            PokemonInstance p = factory.Create(squirtle, 12); // factory seeds the pool from the learnset

            EvolutionExecutor.Evolve(p, Br("squirtle_vanguard"));       // stage 1: Vanguard
            EvolutionExecutor.Evolve(p, Br("wartortle_specialist"));    // stage 2: Specialist (the mix)

            Assert.That(p.Species.SpeciesId, Is.EqualTo("blastoise"));
            Assert.That(p.CurrentStage, Is.EqualTo(EvolutionStage.Stage2));
            List<string> ids = p.LearnedMoves.ConvertAll(m => m.MoveId);
            Assert.That(ids, Has.Member("skull_bash"), "Vanguard stage-1 upgrade survived into the pool.");
            Assert.That(ids, Has.Member("aqua_jet"), "Vanguard stage-1 upgrade survived into the pool.");
            Assert.That(ids, Has.Member("hydro_pump"), "Specialist stage-2 signature added (the mix).");
            Assert.That(p.Ability, Is.Null, "CL-007 — evolution grants no ability.");
            factory.Release(p);
        }
    }
}
