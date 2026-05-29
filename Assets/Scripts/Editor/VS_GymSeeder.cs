// Per Epic 8 Task 8.5 — VS Gym Leader seeder.
// Authors the R1 Rock Gym Leader (§4.4.4, Brock-equivalent) and its rewards:
//   • A Rare-tier relic (none existed — Epic 3 shipped only Common+Uncommon;
//     this is a VS placeholder for the §8.3 Rare catalog — see gap #32).
//   • Corrects the Boulder Badge to match locked §4.4.5.1 (the Epic-3 asset
//     drifted to a "+10% Defense" effect): sets LeadIncomingDamageReduction=1
//     and the canonical description.
//   • Rock Gym Leader: Geodude (L14, 2-phase) + ace Graveler (L16, 3-phase,
//     Sturdy, mid-fight evolution → Golem @50%). Reward = Boulder Badge +
//     Rare relic + 50 XP + 500₽ (§7.12).
//
// Menu: Project Ascendant / Seed VS Gym
// Idempotent. Run AFTER VS_ContentSeeder + VS_ItemSeeder (species + badge).

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    public static class VS_GymSeeder
    {
        const string ROOT = "Assets/ScriptableObjects/VS";

        [MenuItem("Project Ascendant/Seed VS Gym")]
        public static void SeedAll()
        {
            PokemonSpeciesSO geodude = FindByName<PokemonSpeciesSO>("Geodude");
            PokemonSpeciesSO graveler = FindByName<PokemonSpeciesSO>("Graveler");
            PokemonSpeciesSO golem = FindByName<PokemonSpeciesSO>("Golem");
            BadgeSO boulder = FindByName<BadgeSO>("boulder_badge");

            if (geodude == null || graveler == null || golem == null || boulder == null)
            {
                Debug.LogError("[VS_GymSeeder] Missing cross-reference — "
                    + $"Geodude={geodude}, Graveler={graveler}, Golem={golem}, "
                    + $"boulder_badge={boulder}. Run VS_ContentSeeder + VS_ItemSeeder first.");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                MkDir($"{ROOT}/Gyms");

                // ── 1. Rare relic (VS placeholder — see gap #32) ──────────────
                string relicPath = $"{ROOT}/Relics/conquerors_stone.asset";
                AssetDatabase.DeleteAsset(relicPath);
                RelicSO rare = ScriptableObject.CreateInstance<RelicSO>();
                rare.Id = "conquerors_stone";
                rare.DisplayName = "Conqueror's Stone";
                rare.Rarity = RarityTier.Rare;
                rare.MetaTier = 1;
                rare.Categories = new List<SynergyCategory>();
                rare.GDDReference =
                    "§8.3 (Rare tier) | VS placeholder Rare relic — R1 Gym drop. "
                    + "Effect wiring deferred to Epic 12 (hook stub). See gap #32.";
                AssetDatabase.CreateAsset(rare, relicPath);
                EditorUtility.SetDirty(rare);

                // ── 2. Fix Boulder Badge to §4.4.5.1 ──────────────────────────
                boulder.LeadIncomingDamageReduction = 1; // §4.4.5.1
                boulder.EffectDescription =
                    "Your Lead Pokémon reduces all incoming damage by 1 (minimum 0).";
                boulder.GDDReference =
                    "§4.4.5.1 | Boulder Badge. Rock Gym R1. Lead −1 incoming dmg (min 0).";
                EditorUtility.SetDirty(boulder);

                // ── 3. Rock Gym Leader ────────────────────────────────────────
                string gymPath = $"{ROOT}/Gyms/rock_gym_r1.asset";
                AssetDatabase.DeleteAsset(gymPath);
                GymLeaderSO gym = ScriptableObject.CreateInstance<GymLeaderSO>();
                gym.GymLeaderId = "rock_gym_r1";
                gym.DisplayName = "Rock Gym Leader";
                gym.GymType = PokemonType.Rock;
                gym.TacticalIdentity =
                    "Single-type Rock identity. Geodude opens methodically; the ace "
                    + "Graveler evolves into Golem at 50% HP (power spike), and in its "
                    + "last stand (≤20% HP) resets cooldowns to fire its signature and "
                    + "survives one lethal hit at 1 HP (Sturdy).";
                gym.Composition = new List<GymPokemonSlot>
                {
                    new GymPokemonSlot
                    {
                        Species = geodude, Level = 14, PhaseCount = 2,
                        IsAce = false, HasSturdy = false, MidFightEvolution = null,
                    },
                    new GymPokemonSlot
                    {
                        Species = graveler, Level = 16, PhaseCount = 3,
                        IsAce = true, HasSturdy = true, MidFightEvolution = golem,
                    },
                };
                gym.BadgeReward = boulder;
                gym.GuaranteedRareRelic = rare;
                gym.TrainerXPReward = 50;   // §7.12
                gym.PokeDollarReward = 500; // §7.12
                gym.GDDReference =
                    "§4.4.4 | R1 Rock Gym (Brock-equivalent). Ace Graveler→Golem @50%, "
                    + "3-phase, Sturdy. Reward: Boulder Badge + Rare relic + 50 XP + 500₽.";
                AssetDatabase.CreateAsset(gym, gymPath);
                EditorUtility.SetDirty(gym);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log("[VS_GymSeeder] Done — rock_gym_r1 + conquerors_stone authored; "
                + "boulder_badge corrected to §4.4.5.1.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static T FindByName<T>(string assetName) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:{typeof(T).Name} {assetName}", new[] { ROOT });
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == assetName)
                    return AssetDatabase.LoadAssetAtPath<T>(p);
            }
            return null;
        }

        static void MkDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) MkDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
