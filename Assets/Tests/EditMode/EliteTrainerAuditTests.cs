using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.4 (Elite authoring QA) + §7.5.1 + §7.12 + §4.4.3.
    //
    // Walks every EliteTrainerSO asset under Assets/ScriptableObjects and pins
    // the §7.5.1 Elite invariants against the authored content. Mirrors
    // TrainerArchetypeAuditTests (Task 8.3): a regression net, not a manual
    // menu check. The VS ships exactly one R1 Elite (bespoke Ace Trainer); see
    // the ⚠ OPEN flag on §7.5.1 + BACKLOG gap #31 for the Ace-Trainer-pool gap.
    public class EliteTrainerAuditTests
    {
        // §7.5.1 — Elite fields exactly 2 Pokémon, sequential, each 2-phase.
        private const int ELITE_SLOTS = 2;
        private const int ELITE_PHASE_COUNT = 2;

        // §7.4 band reused for R1 eligibility (mirrors the 8.3 audit ceiling).
        private const int MIN_LEVEL = 1;
        private const int MAX_R1_LEVEL = 20;

        // §7.12 — Elite reward row.
        private const int ELITE_XP = 25;
        private const int ELITE_DOLLARS = 300;

        private static EliteTrainerSO[] LoadAllElites()
        {
            string[] guids = AssetDatabase.FindAssets("t:EliteTrainerSO",
                new[] { "Assets/ScriptableObjects" });
            var list = new List<EliteTrainerSO>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EliteTrainerSO so = AssetDatabase.LoadAssetAtPath<EliteTrainerSO>(path);
                if (so != null) list.Add(so);
            }
            return list.ToArray();
        }

        // VS roster = every PokemonSpeciesSO shipped — equivalent to R1-eligible
        // since the VS carve-out ships only the 6 R1 lines.
        private static HashSet<PokemonSpeciesSO> LoadVSRoster()
        {
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO",
                new[] { "Assets/ScriptableObjects" });
            var set = new HashSet<PokemonSpeciesSO>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(path);
                if (sp != null) set.Add(sp);
            }
            return set;
        }

        // ── VS ships exactly one R1 Elite (§7.13) ────────────────────────────

        [Test]
        public void Library_HasExactlyOneEliteForVS()
        {
            EliteTrainerSO[] elites = LoadAllElites();
            Assert.That(elites.Length, Is.EqualTo(1),
                "Per §7.13 the VS ships exactly one Elite Trainer; found "
                + elites.Length);
        }

        // ── Identity discipline ──────────────────────────────────────────────

        [Test]
        public void AllElites_HaveNonEmptyIdentityFields()
        {
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                string path = AssetDatabase.GetAssetPath(e);
                if (string.IsNullOrWhiteSpace(e.EliteId)) bad.Add($"missing EliteId @ {path}");
                if (string.IsNullOrWhiteSpace(e.DisplayName)) bad.Add($"missing DisplayName @ {path}");
                if (string.IsNullOrWhiteSpace(e.TacticalIdentity)) bad.Add($"missing TacticalIdentity @ {path}");
                if (string.IsNullOrWhiteSpace(e.GDDReference)) bad.Add($"missing GDDReference @ {path}");
            }
            Assert.That(bad, Is.Empty,
                "Every EliteTrainerSO needs Id/DisplayName/TacticalIdentity/GDDReference.\n  "
                + string.Join("\n  ", bad));
        }

        // ── §7.5.1 composition shape: 2 Pokémon, each 2-phase ────────────────

        [Test]
        public void AllElites_FieldExactlyTwoPokemon()
        {
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                int n = e.Composition?.Count ?? 0;
                if (n != ELITE_SLOTS) bad.Add($"{e.EliteId} has {n} slots");
            }
            Assert.That(bad, Is.Empty,
                $"Per §7.5.1 an Elite fields exactly {ELITE_SLOTS} Pokémon.\n  "
                + string.Join("\n  ", bad));
        }

        [Test]
        public void AllElites_CompositionHasNoNullSpecies()
        {
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                if (e.Composition == null) continue;
                for (int i = 0; i < e.Composition.Count; i++)
                    if (e.Composition[i].Species == null)
                        bad.Add($"{e.EliteId} Composition[{i}].Species is null");
            }
            Assert.That(bad, Is.Empty,
                "An Elite slot must reference a real species.\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllElites_EveryPokemonIsTwoPhase()
        {
            // Per §7.5.1 / §4.4.3 — both Elite Pokémon use the 2-phase template
            // (the 3-phase ace template is a Gym-only thing, Task 8.5).
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                if (e.Composition == null) continue;
                for (int i = 0; i < e.Composition.Count; i++)
                    if (e.Composition[i].PhaseCount != ELITE_PHASE_COUNT)
                        bad.Add($"{e.EliteId} slot {i} PhaseCount={e.Composition[i].PhaseCount}");
            }
            Assert.That(bad, Is.Empty,
                $"Per §7.5.1 every Elite Pokémon is {ELITE_PHASE_COUNT}-phase.\n  "
                + string.Join("\n  ", bad));
        }

        // ── §7.4 R1 eligibility: roster + level band ─────────────────────────

        [Test]
        public void AllElites_CompositionSpeciesAreInVSRoster()
        {
            HashSet<PokemonSpeciesSO> roster = LoadVSRoster();
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                if (e.Composition == null) continue;
                foreach (ElitePokemonSlot slot in e.Composition)
                {
                    if (slot.Species == null) continue;
                    if (!roster.Contains(slot.Species))
                        bad.Add($"{e.EliteId} → {slot.Species.SpeciesId} not in VS/R1 roster");
                }
            }
            Assert.That(bad, Is.Empty,
                "Per §7.4 an R1 Elite may only field VS-roster species.\n  "
                + string.Join("\n  ", bad));
        }

        [Test]
        public void AllElites_CompositionLevelsInR1Band()
        {
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                if (e.Composition == null) continue;
                for (int i = 0; i < e.Composition.Count; i++)
                {
                    int lvl = e.Composition[i].Level;
                    if (lvl < MIN_LEVEL || lvl > MAX_R1_LEVEL)
                        bad.Add($"{e.EliteId} slot {i} Level={lvl}");
                }
            }
            Assert.That(bad, Is.Empty,
                $"Per §7.4 R1 Elite levels must sit in [{MIN_LEVEL},{MAX_R1_LEVEL}].\n  "
                + string.Join("\n  ", bad));
        }

        // ── §7.5.1 / §7.12 reward: guaranteed Uncommon relic + 25 XP + 300₽ ──

        [Test]
        public void AllElites_HaveGuaranteedUncommonRelic()
        {
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                if (e.GuaranteedRelic == null)
                {
                    bad.Add($"{e.EliteId} has no GuaranteedRelic");
                    continue;
                }
                if (e.GuaranteedRelic.Rarity != RarityTier.Uncommon)
                    bad.Add($"{e.EliteId} relic {e.GuaranteedRelic.DisplayName} is "
                        + $"{e.GuaranteedRelic.Rarity}, not Uncommon");
            }
            Assert.That(bad, Is.Empty,
                "Per §7.5.1 the Elite drops one GUARANTEED Uncommon relic.\n  "
                + string.Join("\n  ", bad));
        }

        [Test]
        public void AllElites_RewardMatchesSpec()
        {
            // Per §7.12 — Elite row: 25 Trainer XP, 300₽.
            var bad = new List<string>();
            foreach (EliteTrainerSO e in LoadAllElites())
            {
                if (e.TrainerXPReward != ELITE_XP)
                    bad.Add($"{e.EliteId} XP={e.TrainerXPReward} (expected {ELITE_XP})");
                if (e.PokeDollarReward != ELITE_DOLLARS)
                    bad.Add($"{e.EliteId} ₽={e.PokeDollarReward} (expected {ELITE_DOLLARS})");
            }
            Assert.That(bad, Is.Empty,
                $"Per §7.12 Elite reward is {ELITE_XP} XP + {ELITE_DOLLARS}₽.\n  "
                + string.Join("\n  ", bad));
        }
    }
}
