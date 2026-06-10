// Per design/map-redesign-gyms.md + §4.4.4 / §4.4.5.1 — R1 Gym Pool seeder.
// Creates the 3 missing gyms (water, bug, normal) + fixes/creates the 4 R1 badges
// (boulder, cascade, hive, normal) with their ScriptableHook channels. Idempotent.
//
// Menu: Project Ascendant / Seed R1 Gym Pool
// Run AFTER VS_ContentSeeder (species exist) + VS_GymSeeder (boulder_badge + rock_gym_r1).

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    public static class VS_R1GymPoolSeeder
    {
        const string ROOT = "Assets/ScriptableObjects/VS";

        [MenuItem("Project Ascendant/Seed R1 Gym Pool")]
        public static void SeedAll()
        {
            // ── Load species ────────────────────────────────────────────────────
            // Per CL-013 — Gym aces no longer evolve mid-fight, so the evolved ace forms
            // (Blastoise/Butterfree/Pidgeot) are no longer referenced here.
            PokemonSpeciesSO squirtle = FindByName<PokemonSpeciesSO>("Squirtle");
            PokemonSpeciesSO wartortleVanguard = FindByName<PokemonSpeciesSO>("Wartortle_Vanguard");
            PokemonSpeciesSO caterpie = FindByName<PokemonSpeciesSO>("Caterpie");
            PokemonSpeciesSO metapod = FindByName<PokemonSpeciesSO>("Metapod");
            PokemonSpeciesSO pidgey = FindByName<PokemonSpeciesSO>("Pidgey");
            PokemonSpeciesSO pidgeotto = FindByName<PokemonSpeciesSO>("Pidgeotto");

            if (squirtle == null || wartortleVanguard == null
                || caterpie == null || metapod == null
                || pidgey == null || pidgeotto == null)
            {
                Debug.LogError("[VS_R1GymPoolSeeder] Missing species — run VS_ContentSeeder first. "
                    + $"Squirtle={squirtle}, Wartortle_Vanguard={wartortleVanguard}, "
                    + $"Caterpie={caterpie}, Metapod={metapod}, "
                    + $"Pidgey={pidgey}, Pidgeotto={pidgeotto}");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                MkDir($"{ROOT}/Badges");
                MkDir($"{ROOT}/Hooks");
                MkDir($"{ROOT}/Gyms");

                // ── 1. ScriptableHook channels (per §4.4.5.1) ──────────────────
                // OnLeadChanged may already exist (Boulder); others are new.
                DrawCardHook onLeadChanged = GetOrCreateDrawCardHook(
                    $"{ROOT}/Hooks/OnLeadChanged.asset",
                    "OnLeadChanged",
                    drawCount: 1);

                // OnSkillCardCycled — fires when a card moves discard→deck. Hive badge
                // checks 20% proc to add a free copy to hand next turn.
                // For the VS stub, we use a DrawCardHook placeholder (0 cards — the real
                // effect wiring happens in combat code, not via the hook's data field).
                DrawCardHook onSkillCardCycled = GetOrCreateDrawCardHook(
                    $"{ROOT}/Hooks/OnSkillCardCycled.asset",
                    "OnSkillCardCycled",
                    drawCount: 0); // Hive logic is code-driven; hook is a marker.

                // OnDamageCalculation — fires during damage calc. Normal badge applies
                // +10% to base stats. We use a ModifyDamageHook placeholder (1.0× — the
                // real +10% stat boost is in DamageCalculator, not a flat multiplier).
                ModifyDamageHook onDamageCalculation = GetOrCreateModifyDamageHook(
                    $"{ROOT}/Hooks/OnDamageCalculation.asset",
                    "OnDamageCalculation",
                    multiplier: 1.0f); // Normal logic is code-driven; hook is a marker.

                // ── 2. Badges (per §4.4.5.1) ───────────────────────────────────
                BadgeSO boulder = FindByName<BadgeSO>("boulder_badge");
                if (boulder == null)
                {
                    Debug.LogError("[VS_R1GymPoolSeeder] boulder_badge not found — run VS_GymSeeder first.");
                    return;
                }
                // Boulder's hook is null (its effect is LeadIncomingDamageReduction field).

                BadgeSO cascade = CreateOrUpdateBadge(
                    $"{ROOT}/Badges/cascade_badge.asset",
                    badgeId: "cascade_badge",
                    displayName: "Cascade Badge",
                    gymSource: "R1-Gym2",
                    effectDescription: "After a MANUAL Lead swap, draw 1 extra skill card this turn.",
                    grantedHook: onLeadChanged,
                    leadIncomingDamageReduction: 0,
                    gddRef: "§4.4.5.1 | Cascade Badge. Water Gym R1. Manual swap → +1 card.");

                BadgeSO hive = CreateOrUpdateBadge(
                    $"{ROOT}/Badges/hive_badge.asset",
                    badgeId: "hive_badge",
                    displayName: "Hive Badge",
                    gymSource: "R1-Gym3",
                    effectDescription: "When a card cycles discard→deck, 20% to make a free copy next turn.",
                    grantedHook: onSkillCardCycled,
                    leadIncomingDamageReduction: 0,
                    gddRef: "§4.4.5.1 | Hive Badge. Bug Gym R1. Cycle proc → 20% free copy.");

                BadgeSO normal = CreateOrUpdateBadge(
                    $"{ROOT}/Badges/normal_badge.asset",
                    badgeId: "normal_badge",
                    displayName: "Normal Badge",
                    gymSource: "R1-Gym4",
                    effectDescription: "Base stats treated +10% for damage dealt AND received.",
                    grantedHook: onDamageCalculation,
                    leadIncomingDamageReduction: 0,
                    gddRef: "§4.4.5.1 | Normal Badge. Normal Gym R1. +10% base stats (dmg dealt+rcvd).");

                // ── 3. Rare relics (VS placeholders) ───────────────────────────
                RelicSO waterRare = GetOrCreateRelic(
                    $"{ROOT}/Relics/neptunes_trident.asset",
                    id: "neptunes_trident",
                    displayName: "Neptune's Trident",
                    gddRef: "§8.3 (Rare tier) | VS placeholder — Water Gym R1 drop. Effect wiring deferred to Epic 12.");

                RelicSO bugRare = GetOrCreateRelic(
                    $"{ROOT}/Relics/hive_mind.asset",
                    id: "hive_mind",
                    displayName: "Hive Mind",
                    gddRef: "§8.3 (Rare tier) | VS placeholder — Bug Gym R1 drop. Effect wiring deferred to Epic 12.");

                RelicSO normalRare = GetOrCreateRelic(
                    $"{ROOT}/Relics/versatile_gem.asset",
                    id: "versatile_gem",
                    displayName: "Versatile Gem",
                    gddRef: "§8.3 (Rare tier) | VS placeholder — Normal Gym R1 drop. Effect wiring deferred to Epic 12.");

                // ── 4. Gyms (per design/map-redesign-gyms.md) ─────────────────
                CreateOrUpdateGym(
                    $"{ROOT}/Gyms/water_gym_r1.asset",
                    gymLeaderId: "water_gym_r1",
                    displayName: "Water Gym Leader",
                    gymType: PokemonType.Water,
                    tacticalIdentity: "Single-type Water identity. Squirtle opens with Aqua Jet aggression; "
                        + "the Wartortle ace is a higher-tier threat than the route (CL-013 power premium), "
                        + "gains a Sturdy last-stand (≤20% HP), and survives one lethal hit at 1 HP. "
                        + "No mid-fight evolution (CL-013).",
                    slot1Species: squirtle,
                    slot1Level: 13,
                    slot1PhaseCount: 2,
                    aceSpecies: wartortleVanguard,
                    aceLevel: 15,
                    badgeReward: cascade,
                    rareRelic: waterRare,
                    gddRef: "§4.4.4 / design/map-redesign-gyms.md | R1 Water Gym. Squirtle L13 + "
                        + "Wartortle L15 ace (no mid-evo, CL-013). Reward: Cascade Badge + Rare relic + 50 XP + 500₽.");

                CreateOrUpdateGym(
                    $"{ROOT}/Gyms/bug_gym_r1.asset",
                    gymLeaderId: "bug_gym_r1",
                    displayName: "Bug Gym Leader",
                    gymType: PokemonType.Bug,
                    tacticalIdentity: "Single-type Bug identity. Caterpie opens with String Shot control; "
                        + "the Metapod ace is a higher-tier threat than the route (CL-013 power premium), "
                        + "gains a Sturdy last-stand, and survives one lethal hit at 1 HP. "
                        + "No mid-fight evolution (CL-013).",
                    slot1Species: caterpie,
                    slot1Level: 12,
                    slot1PhaseCount: 2,
                    aceSpecies: metapod,
                    aceLevel: 15,
                    badgeReward: hive,
                    rareRelic: bugRare,
                    gddRef: "§4.4.4 / design/map-redesign-gyms.md | R1 Bug Gym. Caterpie L12 + "
                        + "Metapod L15 ace (no mid-evo, CL-013). Reward: Hive Badge + Rare relic + 50 XP + 500₽.");

                CreateOrUpdateGym(
                    $"{ROOT}/Gyms/normal_gym_r1.asset",
                    gymLeaderId: "normal_gym_r1",
                    displayName: "Normal Gym Leader",
                    gymType: PokemonType.Normal,
                    tacticalIdentity: "Single-type Normal identity. Pidgey opens with Gust poke; "
                        + "the Pidgeotto ace is a higher-tier threat than the route (CL-013 power premium), "
                        + "gains a Sturdy last-stand, and survives one lethal hit at 1 HP. "
                        + "No mid-fight evolution (CL-013).",
                    slot1Species: pidgey,
                    slot1Level: 13,
                    slot1PhaseCount: 2,
                    aceSpecies: pidgeotto,
                    aceLevel: 16,
                    badgeReward: normal,
                    rareRelic: normalRare,
                    gddRef: "§4.4.4 / design/map-redesign-gyms.md | R1 Normal Gym. Pidgey L13 + "
                        + "Pidgeotto L16 ace (no mid-evo, CL-013). Reward: Normal Badge + Rare relic + 50 XP + 500₽.");

                // ── 5. Wire into RunContentCatalog ─────────────────────────────
                RunContentCatalogSO catalog = FindByName<RunContentCatalogSO>("RunContentCatalog");
                if (catalog != null)
                {
                    GymLeaderSO rock = FindByName<GymLeaderSO>("rock_gym_r1");
                    GymLeaderSO water = FindByName<GymLeaderSO>("water_gym_r1");
                    GymLeaderSO bug = FindByName<GymLeaderSO>("bug_gym_r1");
                    GymLeaderSO normalGym = FindByName<GymLeaderSO>("normal_gym_r1");

                    catalog.GymPool = new List<GymLeaderSO> { rock, water, bug, normalGym };
                    catalog.Gym = rock; // Fallback for backward compat.
                    EditorUtility.SetDirty(catalog);
                }
                else
                {
                    Debug.LogWarning("[VS_R1GymPoolSeeder] RunContentCatalog not found — manual GymPool wiring needed.");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[VS_R1GymPoolSeeder] Done — 4 gyms (rock/water/bug/normal), 4 badges, 3 hooks, 3 rare relics.");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        static T FindByName<T>(string assetName) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {assetName}", new[] { ROOT });
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

        static DrawCardHook GetOrCreateDrawCardHook(string path, string hookName, int drawCount)
        {
            DrawCardHook hook = AssetDatabase.LoadAssetAtPath<DrawCardHook>(path);
            if (hook == null)
            {
                hook = ScriptableObject.CreateInstance<DrawCardHook>();
                hook.name = hookName;
                hook.DrawCount = drawCount;
                AssetDatabase.CreateAsset(hook, path);
            }
            else
            {
                hook.DrawCount = drawCount;
            }
            EditorUtility.SetDirty(hook);
            return hook;
        }

        static ModifyDamageHook GetOrCreateModifyDamageHook(string path, string hookName, float multiplier)
        {
            ModifyDamageHook hook = AssetDatabase.LoadAssetAtPath<ModifyDamageHook>(path);
            if (hook == null)
            {
                hook = ScriptableObject.CreateInstance<ModifyDamageHook>();
                hook.name = hookName;
                hook.Multiplier = multiplier;
                AssetDatabase.CreateAsset(hook, path);
            }
            else
            {
                hook.Multiplier = multiplier;
            }
            EditorUtility.SetDirty(hook);
            return hook;
        }

        static BadgeSO CreateOrUpdateBadge(string path, string badgeId, string displayName,
            string gymSource, string effectDescription, ScriptableHook grantedHook,
            int leadIncomingDamageReduction, string gddRef)
        {
            BadgeSO badge = AssetDatabase.LoadAssetAtPath<BadgeSO>(path);
            if (badge == null)
            {
                badge = ScriptableObject.CreateInstance<BadgeSO>();
                AssetDatabase.CreateAsset(badge, path);
            }
            badge.BadgeId = badgeId;
            badge.DisplayName = displayName;
            badge.GymSource = gymSource;
            badge.EffectDescription = effectDescription;
            badge.GrantedHook = grantedHook;
            badge.LeadIncomingDamageReduction = leadIncomingDamageReduction;
            badge.GDDReference = gddRef;
            EditorUtility.SetDirty(badge);
            return badge;
        }

        static RelicSO GetOrCreateRelic(string path, string id, string displayName, string gddRef)
        {
            RelicSO relic = AssetDatabase.LoadAssetAtPath<RelicSO>(path);
            if (relic == null)
            {
                relic = ScriptableObject.CreateInstance<RelicSO>();
                relic.Id = id;
                relic.DisplayName = displayName;
                relic.Rarity = RarityTier.Rare;
                relic.MetaTier = 1;
                relic.Categories = new List<SynergyCategory>();
                relic.GDDReference = gddRef;
                AssetDatabase.CreateAsset(relic, path);
            }
            EditorUtility.SetDirty(relic);
            return relic;
        }

        static void CreateOrUpdateGym(string path, string gymLeaderId, string displayName,
            PokemonType gymType, string tacticalIdentity, PokemonSpeciesSO slot1Species, int slot1Level,
            int slot1PhaseCount, PokemonSpeciesSO aceSpecies, int aceLevel,
            BadgeSO badgeReward, RelicSO rareRelic, string gddRef)
        {
            GymLeaderSO gym = AssetDatabase.LoadAssetAtPath<GymLeaderSO>(path);
            if (gym == null)
            {
                gym = ScriptableObject.CreateInstance<GymLeaderSO>();
                AssetDatabase.CreateAsset(gym, path);
            }
            gym.GymLeaderId = gymLeaderId;
            gym.DisplayName = displayName;
            gym.GymType = gymType;
            gym.TacticalIdentity = tacticalIdentity;
            gym.Composition = new List<GymPokemonSlot>
            {
                new GymPokemonSlot
                {
                    Species = slot1Species, Level = slot1Level, PhaseCount = slot1PhaseCount,
                    IsAce = false, HasSturdy = false,
                },
                new GymPokemonSlot
                {
                    Species = aceSpecies, Level = aceLevel, PhaseCount = 3,
                    IsAce = true, HasSturdy = true, // CL-013: no mid-fight evolution
                },
            };
            gym.BadgeReward = badgeReward;
            gym.GuaranteedRareRelic = rareRelic;
            gym.TrainerXPReward = 50;
            gym.PokeDollarReward = 500;
            gym.GDDReference = gddRef;
            EditorUtility.SetDirty(gym);
        }
    }
}
#endif
