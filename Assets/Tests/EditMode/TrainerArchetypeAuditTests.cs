using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.3 (Trainer Archetype QA) + §3.1.15 (Trainer archetype
    // definition) + §7.4 (Trainer nodes / R1 eligibility).
    //
    // Walks every TrainerArchetypeSO asset under Assets/ScriptableObjects and
    // asserts library-wide invariants. Mirrors MoveLibraryAuditTests: a
    // regression net rather than a manual menu action.
    //
    // R1-eligibility note: TrainerArchetypeSO carries no explicit
    // RegionEligibility field — R1 restriction is enforced by curation (these
    // four are the only authored archetypes, and every composition species is
    // drawn from the VS / R1 roster at an R1-band level). These tests pin that
    // curation invariant so a non-R1 species or out-of-band level cannot drift
    // in unnoticed. See the ⚠ OPEN flag for the proposed explicit field.
    public class TrainerArchetypeAuditTests
    {
        // §3.1.15 — a trainer fields 1-3 Pokémon.
        private const int MIN_SLOTS = 1;
        private const int MAX_SLOTS = 3;

        // §7.4 — R1 trainers field early-route Pokémon. 20 is a conservative
        // ceiling that catches typos (e.g. an accidental L100) without
        // over-constraining the authored 5-12 band.
        private const int MIN_LEVEL = 1;
        private const int MAX_R1_LEVEL = 20;

        // The four VS / R1 archetypes per §3.3.18.
        private static readonly string[] ExpectedArchetypeIds =
            { "bug_catcher", "lass", "hiker", "sailor" };

        private static TrainerArchetypeSO[] LoadAllArchetypes()
        {
            string[] guids = AssetDatabase.FindAssets("t:TrainerArchetypeSO",
                new[] { "Assets/ScriptableObjects" });
            var list = new List<TrainerArchetypeSO>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TrainerArchetypeSO so =
                    AssetDatabase.LoadAssetAtPath<TrainerArchetypeSO>(path);
                if (so != null) list.Add(so);
            }
            return list.ToArray();
        }

        // The VS roster = every PokemonSpeciesSO shipped in the project. The VS
        // carve-out ships ONLY the 6 R1 lines (18 species), so membership in
        // this set is equivalent to "is an R1-eligible species".
        private static HashSet<PokemonSpeciesSO> LoadVSRoster()
        {
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO",
                new[] { "Assets/ScriptableObjects" });
            var set = new HashSet<PokemonSpeciesSO>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp =
                    AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(path);
                if (sp != null) set.Add(sp);
            }
            return set;
        }

        // ── §3.3.18 — exactly the four VS archetypes exist ──────────────────

        [Test]
        public void Library_HasExactlyTheFourVSArchetypes()
        {
            var ids = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
                ids.Add(a.ArchetypeId);

            foreach (string expected in ExpectedArchetypeIds)
                Assert.That(ids, Has.Member(expected),
                    $"Per §3.3.18 the VS ships archetype '{expected}'. "
                    + "Present ids: " + string.Join(", ", ids));

            Assert.That(ids.Count, Is.EqualTo(ExpectedArchetypeIds.Length),
                "Per the VS carve-out exactly 4 trainer archetypes ship; "
                + "found " + ids.Count + ": " + string.Join(", ", ids));
        }

        // ── Identity discipline ─────────────────────────────────────────────

        [Test]
        public void AllArchetypes_HaveNonEmptyIdAndDisplayName()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                string path = AssetDatabase.GetAssetPath(a);
                if (string.IsNullOrWhiteSpace(a.ArchetypeId))
                    bad.Add($"missing ArchetypeId @ {path}");
                if (string.IsNullOrWhiteSpace(a.DisplayName))
                    bad.Add($"missing DisplayName @ {path}");
            }
            Assert.That(bad, Is.Empty,
                "Every TrainerArchetypeSO needs a non-empty ArchetypeId and "
                + "DisplayName.\nOffenders:\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllArchetypes_HaveTacticalIdentity()
        {
            // Per §3.1.15 — TacticalIdentity drives Epic 8 AI tendencies; it
            // must be authored for every archetype.
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
                if (string.IsNullOrWhiteSpace(a.TacticalIdentity))
                    bad.Add(a.ArchetypeId);
            Assert.That(bad, Is.Empty,
                "Every archetype must carry a TacticalIdentity.\nOffenders:\n  "
                + string.Join("\n  ", bad));
        }

        [Test]
        public void AllArchetypes_HaveNonEmptyGDDReference()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
                if (string.IsNullOrWhiteSpace(a.GDDReference))
                    bad.Add(AssetDatabase.GetAssetPath(a));
            Assert.That(bad, Is.Empty,
                "Every TrainerArchetypeSO must carry a §-reference.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        // ── §3.1.15 — composition shape ─────────────────────────────────────

        [Test]
        public void AllArchetypes_CompositionWithinOneToThreeSlots()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                int n = a.Composition?.Count ?? 0;
                if (n < MIN_SLOTS || n > MAX_SLOTS)
                    bad.Add($"{a.ArchetypeId} has {n} slots");
            }
            Assert.That(bad, Is.Empty,
                $"Per §3.1.15 a trainer fields {MIN_SLOTS}-{MAX_SLOTS} Pokémon.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllArchetypes_CompositionHasNoNullSpecies()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                if (a.Composition == null) continue;
                for (int i = 0; i < a.Composition.Count; i++)
                    if (a.Composition[i].Species == null)
                        bad.Add($"{a.ArchetypeId} Composition[{i}].Species is null");
            }
            Assert.That(bad, Is.Empty,
                "A trainer composition slot must reference a real species.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        // ── §7.4 — R1 eligibility (species + level band) ─────────────────────

        [Test]
        public void AllArchetypes_CompositionSpeciesAreInVSRoster()
        {
            HashSet<PokemonSpeciesSO> roster = LoadVSRoster();
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                if (a.Composition == null) continue;
                foreach (TrainerPokemonSlot slot in a.Composition)
                {
                    if (slot.Species == null) continue; // covered elsewhere
                    if (!roster.Contains(slot.Species))
                        bad.Add($"{a.ArchetypeId} → {slot.Species.SpeciesId} "
                            + "is not in the VS/R1 roster");
                }
            }
            Assert.That(bad, Is.Empty,
                "Per §7.4 R1 trainers may only field VS-roster species.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllArchetypes_CompositionLevelsInR1Band()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                if (a.Composition == null) continue;
                for (int i = 0; i < a.Composition.Count; i++)
                {
                    int lvl = a.Composition[i].Level;
                    if (lvl < MIN_LEVEL || lvl > MAX_R1_LEVEL)
                        bad.Add($"{a.ArchetypeId} slot {i} Level={lvl}");
                }
            }
            Assert.That(bad, Is.Empty,
                $"Per §7.4 R1 trainer Pokémon levels must sit in "
                + $"[{MIN_LEVEL},{MAX_R1_LEVEL}].\nOffenders:\n  "
                + string.Join("\n  ", bad));
        }

        // ── Rewards / loot ───────────────────────────────────────────────────

        [Test]
        public void AllArchetypes_HavePositivePokeDollarReward()
        {
            // Per §7.4.2 — defeating a trainer pays PokéDollars; the base must
            // be a positive value so the reward path is meaningful.
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
                if (a.BasePokeDollarReward <= 0)
                    bad.Add($"{a.ArchetypeId} reward={a.BasePokeDollarReward}");
            Assert.That(bad, Is.Empty,
                "Every archetype must pay a positive BasePokeDollarReward.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllArchetypes_HaveAtLeastOneLootEntry()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                int relics = a.RelicLootTable?.Count ?? 0;
                int cons = a.ConsumableLootTable?.Count ?? 0;
                if (relics + cons == 0)
                    bad.Add(a.ArchetypeId);
            }
            Assert.That(bad, Is.Empty,
                "Every archetype must offer at least one loot entry "
                + "(relic or consumable).\nOffenders:\n  "
                + string.Join("\n  ", bad));
        }

        [Test]
        public void AllArchetypes_LootTablesHaveNoNullEntries()
        {
            var bad = new List<string>();
            foreach (TrainerArchetypeSO a in LoadAllArchetypes())
            {
                if (a.RelicLootTable != null)
                    for (int i = 0; i < a.RelicLootTable.Count; i++)
                        if (a.RelicLootTable[i] == null)
                            bad.Add($"{a.ArchetypeId} RelicLootTable[{i}] null");
                if (a.ConsumableLootTable != null)
                    for (int i = 0; i < a.ConsumableLootTable.Count; i++)
                        if (a.ConsumableLootTable[i] == null)
                            bad.Add($"{a.ArchetypeId} ConsumableLootTable[{i}] null");
            }
            Assert.That(bad, Is.Empty,
                "Loot tables must not carry null entries (stale references).\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }
    }
}
