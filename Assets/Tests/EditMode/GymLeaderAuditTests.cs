using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.5 (Gym authoring QA) + §4.4.4.3 + §4.4.5.1 + §7.12.
    // Walks every GymLeaderSO asset and pins the Gym Leader invariants against
    // the authored content. Mirrors the Elite/Trainer audits.
    public class GymLeaderAuditTests
    {
        private const int MIN_LEVEL = 1;
        private const int MAX_R1_LEVEL = 25;       // Gym sits above Elite (≤20)
        private const int LEAD_PHASES = 2;         // §4.4.2 — lead is 2-phase
        private const int ACE_PHASES = 3;          // §4.4.3 — ace is 3-phase
        private const int GYM_XP = 50;             // §7.12
        private const int GYM_DOLLARS = 500;       // §7.12

        private static GymLeaderSO[] LoadAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:GymLeaderSO",
                new[] { "Assets/ScriptableObjects" });
            var list = new List<GymLeaderSO>(guids.Length);
            foreach (string g in guids)
            {
                GymLeaderSO so = AssetDatabase.LoadAssetAtPath<GymLeaderSO>(
                    AssetDatabase.GUIDToAssetPath(g));
                if (so != null) list.Add(so);
            }
            return list.ToArray();
        }

        private static HashSet<PokemonSpeciesSO> LoadVSRoster()
        {
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO",
                new[] { "Assets/ScriptableObjects" });
            var set = new HashSet<PokemonSpeciesSO>();
            foreach (string g in guids)
            {
                PokemonSpeciesSO sp = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(
                    AssetDatabase.GUIDToAssetPath(g));
                if (sp != null) set.Add(sp);
            }
            return set;
        }

        // CL-024: VS audits scope to R1; post-VS R2/R3 content coexists.
        // R1 gyms: Rock, Water, Bug, Normal (§4.4.4.2). R3 gym: Ground (Giovanni).
        private static bool IsR1Gym(GymLeaderSO g)
        {
            // R1 gym types per §4.4.4.2: Rock, Water, Bug, Normal.
            // R3 Giovanni is Ground (or check ID contains "R3" / "GIOVANNI").
            return g != null && (g.GymType == PokemonType.Rock ||
                                 g.GymType == PokemonType.Water ||
                                 g.GymType == PokemonType.Bug ||
                                 g.GymType == PokemonType.Normal);
        }

        [Test]
        public void Library_HasFourR1GymsForVS()
        {
            // Per §7.2 v2 + design/map-redesign-gyms.md — R1 has a 4-gym pool.
            GymLeaderSO[] allGyms = LoadAll();
            GymLeaderSO[] r1Gyms = allGyms.Where(IsR1Gym).ToArray();
            Assert.That(r1Gyms.Length, Is.EqualTo(4),
                "Per §7.2 v2 the VS ships 4 R1 Gym Leaders (Rock/Water/Bug/Normal); found "
                + r1Gyms.Length + " R1 gyms (post-VS R3 Giovanni coexists).");
        }

        [Test]
        public void Gym_HasIdentityAndGDDReference()
        {
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                string path = AssetDatabase.GetAssetPath(g);
                if (string.IsNullOrWhiteSpace(g.GymLeaderId)) bad.Add($"no GymLeaderId @ {path}");
                if (string.IsNullOrWhiteSpace(g.DisplayName)) bad.Add($"no DisplayName @ {path}");
                if (string.IsNullOrWhiteSpace(g.TacticalIdentity)) bad.Add($"no TacticalIdentity @ {path}");
                if (string.IsNullOrWhiteSpace(g.GDDReference)) bad.Add($"no GDDReference @ {path}");
            }
            Assert.That(bad, Is.Empty, string.Join("\n  ", bad));
        }

        [Test]
        public void Gym_FieldsTwoPokemon_LeadTwoPhase_AceThreePhase()
        {
            // Per §4.4.4.3 — 2 Pokémon; the 2nd is the 3-phase ace.
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                int n = g.Composition?.Count ?? 0;
                if (n != 2) { bad.Add($"{g.GymLeaderId} has {n} slots"); continue; }
                GymPokemonSlot lead = g.Composition[0];
                GymPokemonSlot ace = g.Composition[1];
                if (lead.Species == null) bad.Add($"{g.GymLeaderId} lead species null");
                if (ace.Species == null) bad.Add($"{g.GymLeaderId} ace species null");
                if (lead.PhaseCount != LEAD_PHASES) bad.Add($"{g.GymLeaderId} lead PhaseCount={lead.PhaseCount}");
                if (ace.PhaseCount != ACE_PHASES) bad.Add($"{g.GymLeaderId} ace PhaseCount={ace.PhaseCount}");
                if (!ace.IsAce) bad.Add($"{g.GymLeaderId} slot 1 not flagged IsAce");
            }
            Assert.That(bad, Is.Empty, string.Join("\n  ", bad));
        }

        [Test]
        public void Gym_Ace_HasSturdy()
        {
            // Per §4.4.3 Phase 3 — ace has a Sturdy last-stand. Per CL-013 (§4.4.4.3) Gym aces no
            // longer evolve mid-fight (the MidFightEvolution slot field was removed entirely).
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                if (g.Composition == null || g.Composition.Count < 2) continue;
                GymPokemonSlot ace = g.Composition[1];
                if (!ace.HasSturdy) bad.Add($"{g.GymLeaderId} ace missing Sturdy");
            }
            Assert.That(bad, Is.Empty, string.Join("\n  ", bad));
        }

        [Test]
        public void Gym_CompositionSpeciesInRoster_LevelsInBand()
        {
            // CL-024: band-check only R1 gyms (VS scope). R3 Giovanni uses higher bands.
            HashSet<PokemonSpeciesSO> roster = LoadVSRoster();
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                if (g.Composition == null) continue;
                foreach (GymPokemonSlot s in g.Composition)
                {
                    if (s.Species != null && !roster.Contains(s.Species))
                        bad.Add($"{g.GymLeaderId} → {s.Species.SpeciesId} not in roster");

                    // Band-check only R1 gyms (skip R3 Giovanni).
                    if (IsR1Gym(g) && (s.Level < MIN_LEVEL || s.Level > MAX_R1_LEVEL))
                        bad.Add($"{g.GymLeaderId} level {s.Level} out of band [{MIN_LEVEL},{MAX_R1_LEVEL}]");
                }
            }
            Assert.That(bad, Is.Empty, string.Join("\n  ", bad));
        }

        [Test]
        public void Gym_AwardsBadge_EitherHookOrDamageReduction()
        {
            // Per §4.4.5.1 — each Gym awards a Badge. Boulder uses
            // LeadIncomingDamageReduction; others use GrantedHook.
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                if (g.BadgeReward == null) { bad.Add($"{g.GymLeaderId} no BadgeReward"); continue; }
                bool hasDamageReduction = g.BadgeReward.LeadIncomingDamageReduction > 0;
                bool hasHook = g.BadgeReward.GrantedHook != null;
                if (!hasDamageReduction && !hasHook)
                    bad.Add($"{g.GymLeaderId} badge {g.BadgeReward.BadgeId} has neither hook nor dmg reduction");
            }
            Assert.That(bad, Is.Empty,
                "Per §4.4.5.1 each Badge must have either a GrantedHook or LeadIncomingDamageReduction.\n  "
                + string.Join("\n  ", bad));
        }

        [Test]
        public void Gym_GuaranteedRelic_IsRareTier()
        {
            // Per §7.12 — Gym drop is a Rare relic.
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                if (g.GuaranteedRareRelic == null) { bad.Add($"{g.GymLeaderId} no relic"); continue; }
                if (g.GuaranteedRareRelic.Rarity != RarityTier.Rare)
                    bad.Add($"{g.GymLeaderId} relic is {g.GuaranteedRareRelic.Rarity}, not Rare");
            }
            Assert.That(bad, Is.Empty, string.Join("\n  ", bad));
        }

        [Test]
        public void Gym_RewardMatchesSpec()
        {
            var bad = new List<string>();
            foreach (GymLeaderSO g in LoadAll())
            {
                if (g.TrainerXPReward != GYM_XP) bad.Add($"{g.GymLeaderId} XP={g.TrainerXPReward}");
                if (g.PokeDollarReward != GYM_DOLLARS) bad.Add($"{g.GymLeaderId} ₽={g.PokeDollarReward}");
            }
            Assert.That(bad, Is.Empty,
                $"Per §7.12 Gym reward is {GYM_XP} XP + {GYM_DOLLARS}₽.\n  " + string.Join("\n  ", bad));
        }
    }
}
