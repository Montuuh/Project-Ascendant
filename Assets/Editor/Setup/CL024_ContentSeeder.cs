using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.EditorSetup
{
    // Per CL-024 (Q24) — authors real Elite node content to replace VS placeholders.
    // Region 1 first: Snorlax / Marowak's Spirit elite wilds, Rival Blue R1, EliteTrainerRoster_R1.
    // Spec: design/cl-024-content-spec.md + GDD §7.5.1/§7.5.2/§7.12, §5.3.6, §5.12.1.
    // Locked tuning from spec: deterministic multi-hit counts, Sand Veil on Dugtrio, Rest heal cap 50%, Curse 25% self-HP.
    public static class CL024_ContentSeeder
    {
        private const string SPECIES_BASE = "Assets/ScriptableObjects/VS/Species/Elite";
        private const string MOVES_BASE = "Assets/ScriptableObjects/VS/Moves/Elite";
        private const string ABILITIES_BASE = "Assets/ScriptableObjects/VS/Abilities/Shared";
        private const string ELITEWILD_BASE = "Assets/ScriptableObjects/VS/EliteWilds";
        private const string ELITETRAINER_BASE = "Assets/ScriptableObjects/VS/EliteTrainers";
        private const string ELITEROSTER_BASE = "Assets/ScriptableObjects/VS/EliteRosters";
        private const string HELDITEMS_BASE = "Assets/ScriptableObjects/VS/HeldItems";

        [MenuItem("Project Ascendant/CL-024/Seed Elite Content (R1)")]
        public static void SeedRegion1()
        {
            Debug.Log("[CL-024] Seeding Region 1 Elite content...");

            // Per design/cl-024-content-spec.md — R1 boss-wilds + Rival + roster.
            SeedSnorlaxSpecies();
            SeedMarowakSpecies();
            SeedMarowakSpiritSpecies();
            SeedThickClubHeldItem();
            SeedEliteWilds_R1();
            SeedRivalBlue_R1();
            SeedEliteRoster_R1();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CL-024] Region 1 complete. Run 'Seed Run Content Catalog' to wire into RunContentCatalog.");
        }

        // §7.5.2 — Snorlax #143 (Normal boss-wild, ~Lv14-16).
        // Kit: Rest (50% MaxHP heal cap, P1 only), Snore, Body Slam, Amnesia.
        // Passives: Thick Fat + Immunity.
        private static void SeedSnorlaxSpecies()
        {
            string folder = $"{SPECIES_BASE}/Snorlax";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO snorlax = GetOrCreate<PokemonSpeciesSO>($"{folder}/Snorlax.asset", "Snorlax");
            snorlax.SpeciesId = "143";
            snorlax.DisplayName = "Snorlax";
            snorlax.Types = new List<PokemonType> { PokemonType.Normal };
            snorlax.BaseStats = new BaseStats
            {
                // Per spec: boss HP ≈2× Elite mon, ~Lv14-16 band. Tunable placeholder.
                BaseHP = 160,
                BaseAtk = 60,
                BaseDef = 60,
                BaseSpd = 30
            };
            snorlax.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Slow");
            snorlax.Branches = new List<EvolutionBranchSO>(); // final form
            snorlax.EvolveLevel = 0;

            // Per §5.12.1 — lean learnset. Elite wild uses a fixed 4-move kit at instantiation.
            snorlax.BaseLearnset = new List<MoveSO>();
            snorlax.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("rest_capped", "Rest", PokemonType.Normal, MoveRole.Defensive, MoveRange.Ranged, 0, 0, 1f, "Heal 50% MaxHP (capped per spec), self-Sleep 2t. P1 only.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("snore", "Snore", PokemonType.Normal, MoveRole.Offensive, MoveRange.Ranged, 60, 1, 0.75f, "Ranged 60. Only usable while asleep.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("body_slam", "Body Slam", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 85, 2, 1f, "Melee 85. 30% Paralysis rider.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("amnesia", "Amnesia", PokemonType.Psychic, MoveRole.Defensive, MoveRange.Ranged, 0, 1, 1f, "+2 SpD (Defense in simplified stat model).", "§7.5.2") }
            };
            snorlax.TutorLearnset = new List<MoveSO>();
            snorlax.TMCompatibility = new List<TMSO>();

            // Per spec: Thick Fat + Immunity.
            snorlax.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("thick_fat", "Thick Fat", "Half damage from Fire and Ice moves.", AbilityCategory.Combat, "§7.5.2"),
                EnsureAbility("immunity", "Immunity", "Cannot be Poisoned.", AbilityCategory.Survival, "§7.5.2")
            };

            snorlax.StatusImmunities = new List<StatusCondition>();
            snorlax.WildRarity = RarityTier.Rare;
            snorlax.SpawnBiomes = new List<Biome>(); // boss-wild, not standard Wild Area
            snorlax.Portrait = null; // placeholder sprite
            snorlax.GDDReference = "§7.5.2 — Elite Wild boss (R1, Snorlax)";

            EditorUtility.SetDirty(snorlax);
            Debug.Log($"[CL-024] Seeded {snorlax.name}");
        }

        // §7.5.2 — Marowak #105 (Ground, recruited form from Marowak's Spirit catch).
        // Kit: Bone Club, Headbutt, Bonemerang (2 hits), Growl.
        // Passive: Rock Head. Recruits with Thick Club auto-equipped.
        private static void SeedMarowakSpecies()
        {
            string folder = $"{SPECIES_BASE}/Marowak";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO marowak = GetOrCreate<PokemonSpeciesSO>($"{folder}/Marowak.asset", "Marowak");
            marowak.SpeciesId = "105";
            marowak.DisplayName = "Marowak";
            marowak.Types = new List<PokemonType> { PokemonType.Ground };
            marowak.BaseStats = new BaseStats
            {
                // Boss band ~Lv14-16, recruited form. Tunable placeholder.
                BaseHP = 60,
                BaseAtk = 80,
                BaseDef = 70,
                BaseSpd = 45
            };
            marowak.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            marowak.Branches = new List<EvolutionBranchSO>(); // final form (living)
            marowak.EvolveLevel = 0;

            marowak.BaseLearnset = new List<MoveSO>();
            marowak.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("bone_club", "Bone Club", PokemonType.Ground, MoveRole.Offensive, MoveRange.Melee, 65, 1, 1f, "Melee 65. 10% Flinch (post-VS).", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("headbutt", "Headbutt", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 70, 1, 1f, "Melee 70. 30% Flinch (post-VS).", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("bonemerang_fixed", "Bonemerang", PokemonType.Ground, MoveRole.Offensive, MoveRange.Ranged, 50, 2, 0.75f, "Ranged 50. Hits exactly 2 times (deterministic per spec).", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("growl", "Growl", PokemonType.Normal, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Target Attack -1.", "§5.12.1") }
            };
            marowak.TutorLearnset = new List<MoveSO>();
            marowak.TMCompatibility = new List<TMSO>();

            marowak.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("rock_head", "Rock Head", "No recoil damage from recoil moves.", AbilityCategory.Combat, "§7.5.2")
            };

            marowak.StatusImmunities = new List<StatusCondition>();
            marowak.WildRarity = RarityTier.Rare;
            marowak.SpawnBiomes = new List<Biome>();
            marowak.Portrait = null;
            marowak.GDDReference = "§7.5.2 — Marowak (Ground, recruited from Marowak's Spirit catch)";

            EditorUtility.SetDirty(marowak);
            Debug.Log($"[CL-024] Seeded {marowak.name}");
        }

        // §7.5.2 — Marowak's Spirit (Ghost variant of #105, boss-wild ~Lv14-16).
        // Kit: Curse (25% self-HP → 3t DoT), Confuse Ray, Shadow Bone, Lick.
        // Passives: Levitate + Cursed Body.
        // Catch → recruit living Ground Marowak with Thick Club equipped.
        private static void SeedMarowakSpiritSpecies()
        {
            string folder = $"{SPECIES_BASE}/Marowak";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO spirit = GetOrCreate<PokemonSpeciesSO>($"{folder}/Marowak_Spirit.asset", "Marowak_Spirit");
            spirit.SpeciesId = "105_SPIRIT";
            spirit.DisplayName = "Marowak's Spirit";
            spirit.Types = new List<PokemonType> { PokemonType.Ghost };
            spirit.BaseStats = new BaseStats
            {
                // Boss band, Ghost variant. Tunable placeholder.
                BaseHP = 60,
                BaseAtk = 80,
                BaseDef = 70,
                BaseSpd = 45
            };
            spirit.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            spirit.Branches = new List<EvolutionBranchSO>();
            spirit.EvolveLevel = 0;

            spirit.BaseLearnset = new List<MoveSO>();
            spirit.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("curse_capped", "Curse", PokemonType.Ghost, MoveRole.Utility, MoveRange.Ranged, 0, 2, 1f, "Pay 25% self-HP (capped per spec) → inflict 3t DoT on target.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("confuse_ray", "Confuse Ray", PokemonType.Ghost, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Inflict 3t Confusion.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("shadow_bone", "Shadow Bone", PokemonType.Ghost, MoveRole.Offensive, MoveRange.Melee, 85, 2, 1f, "Melee 85 Ghost. 20% Def↓ rider.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("lick", "Lick", PokemonType.Ghost, MoveRole.Offensive, MoveRange.Melee, 40, 1, 1f, "Melee 40 Ghost. 30% Paralysis rider.", "§7.5.2") }
            };
            spirit.TutorLearnset = new List<MoveSO>();
            spirit.TMCompatibility = new List<TMSO>();

            spirit.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("levitate", "Levitate", "Immune to Ground moves.", AbilityCategory.Type, "§7.5.2"),
                EnsureAbility("cursed_body", "Cursed Body", "30% to disable attacker's last move on hit.", AbilityCategory.Combat, "§7.5.2")
            };

            spirit.StatusImmunities = new List<StatusCondition>();
            spirit.WildRarity = RarityTier.Rare;
            spirit.SpawnBiomes = new List<Biome>();
            spirit.Portrait = null;
            spirit.GDDReference = "§7.5.2 — Elite Wild boss (R1, Marowak's Spirit — Ghost variant)";

            EditorUtility.SetDirty(spirit);
            Debug.Log($"[CL-024] Seeded {spirit.name}");
        }

        // §7.5.2 — Thick Club held item (Marowak-only, +50% Melee damage).
        // Auto-equipped when catching Marowak's Spirit (recruits living Ground Marowak).
        private static void SeedThickClubHeldItem()
        {
            string folder = HELDITEMS_BASE;
            Directory.CreateDirectory(folder);

            HeldItemSO thickClub = GetOrCreate<HeldItemSO>($"{folder}/ThickClub.asset", "ThickClub");
            thickClub.Id = "thick_club";
            thickClub.DisplayName = "Thick Club";
            thickClub.Icon = null; // placeholder sprite
            thickClub.EffectDescription = "Marowak-only. +50% Melee damage.";
            thickClub.BoostsType = PokemonType.Normal; // not type-specific, so leave default
            thickClub.WearerDamageMultiplier = 1.5f; // +50% melee per spec
            thickClub.LeftoversRegenDivisor = 0;
            thickClub.GrantsLeadAura = false;
            thickClub.OnEquipHook = null; // species lock enforced in equip logic, not via hook
            thickClub.EventHooks = new List<HookBinding>();
            thickClub.GDDReference = "§7.5.2 — Thick Club (Marowak-only, +50% Melee)";

            EditorUtility.SetDirty(thickClub);
            Debug.Log($"[CL-024] Seeded {thickClub.name}");
        }

        // §7.5.2 — R1 Elite Wild assets: Snorlax and Marowak's Spirit.
        private static void SeedEliteWilds_R1()
        {
            string folder = ELITEWILD_BASE;
            Directory.CreateDirectory(folder);

            // Snorlax Elite Wild
            EliteWildSO snorlaxWild = GetOrCreate<EliteWildSO>($"{folder}/EliteWild_Snorlax_R1.asset", "EliteWild_Snorlax_R1");
            snorlaxWild.EliteWildId = "SNORLAX_R1";
            snorlaxWild.DisplayName = "Snorlax";
            snorlaxWild.Species = ByName<PokemonSpeciesSO>("Snorlax");
            snorlaxWild.Level = 15;
            snorlaxWild.PhaseCount = 2;
            snorlaxWild.CatchRewardXP = 25;
            snorlaxWild.DefeatRelic = ByName<RelicSO>("neptunes_trident"); // Rare relic
            snorlaxWild.PokeDollarReward = 100;
            snorlaxWild.GDDReference = "§7.5.2";
            EditorUtility.SetDirty(snorlaxWild);

            // Marowak's Spirit Elite Wild
            EliteWildSO spiritWild = GetOrCreate<EliteWildSO>($"{folder}/EliteWild_MarowakSpirit_R1.asset", "EliteWild_MarowakSpirit_R1");
            spiritWild.EliteWildId = "MAROWAK_SPIRIT_R1";
            spiritWild.DisplayName = "Marowak's Spirit";
            spiritWild.Species = ByName<PokemonSpeciesSO>("Marowak_Spirit");
            spiritWild.Level = 15;
            spiritWild.PhaseCount = 2;
            spiritWild.CatchRewardXP = 25;
            spiritWild.DefeatRelic = ByName<RelicSO>("versatile_gem"); // Rare relic
            spiritWild.PokeDollarReward = 100;
            spiritWild.GDDReference = "§7.5.2";
            EditorUtility.SetDirty(spiritWild);

            Debug.Log("[CL-024] Seeded R1 Elite Wilds (Snorlax, Marowak's Spirit)");
        }

        // §7.5.1 — Rival Blue R1 Elite Trainer (2 mons: Pidgeotto + Ivysaur, 2-phase each).
        // Uses existing VS-roster species. IsRival=true, 3 Rare relic choices, RegionScaling for R1.
        private static void SeedRivalBlue_R1()
        {
            string folder = ELITETRAINER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerSO rival = GetOrCreate<EliteTrainerSO>($"{folder}/EliteTrainer_Rival_Blue.asset", "EliteTrainer_Rival_Blue");
            rival.EliteId = "RIVAL_BLUE";
            rival.DisplayName = "Blue";
            rival.TrainerSprite = null; // placeholder sprite
            rival.TacticalIdentity = "Balanced multi-type. Recurring antagonist.";

            // R1 composition: Pidgeotto (~Lv12-14) + Ivysaur (~Lv13-15), each 2-phase.
            rival.Composition = new List<ElitePokemonSlot>
            {
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Pidgeotto"), Level = 13, PhaseCount = 2 },
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Ivysaur"), Level = 14, PhaseCount = 2 }
            };

            // Per §7.5.1 / §7.12 — Rare-relic choice (1 of 3).
            rival.RareRelicChoices = new List<RelicSO>
            {
                ByName<RelicSO>("conquerors_stone"),
                ByName<RelicSO>("hive_mind"),
                ByName<RelicSO>("neptunes_trident")
            }.Where(r => r != null).ToList();

            rival.TrainerXPReward = 25;
            rival.PokeDollarReward = 300;

            rival.IsRival = true;
            rival.RivalEvoSpecies = null; // R1 = 2-phase, no mid-fight evo

            // Per §7.5.1 — Rival scales by Region. R1 = 2 Pokémon, 2-phase.
            rival.RegionScaling = new List<EliteRegionScaling>
            {
                new EliteRegionScaling { RegionIndex = 0, PokemonCount = 2, AcePhaseCount = 2 }
            };

            rival.GDDReference = "§7.5.1 — Rival Blue (R1 Elite Trainer)";

            EditorUtility.SetDirty(rival);
            Debug.Log($"[CL-024] Seeded {rival.name}");
        }

        // §7.5.1 — R1 Elite Trainer Roster (Rival 80% / Specialist 20%).
        private static void SeedEliteRoster_R1()
        {
            string folder = ELITEROSTER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerRosterSO roster = GetOrCreate<EliteTrainerRosterSO>($"{folder}/EliteTrainerRoster_R1.asset", "EliteTrainerRoster_R1");
            roster.RegionIndex = 0;

            // Per spec: R1 = Rival 80 / Specialist 20.
            // Specialist stub = existing ace_trainer_r1 (Pidgeotto+Ivysaur reuse).
            // NOTE: The spec lists "Ace Trainer — Pidgeotto + Ivysaur (reuse existing `ace_trainer_r1`)",
            // but EliteTrainerSO != TrainerArchetypeSO. For R1 we'll create a dedicated Specialist
            // EliteTrainerSO mirroring the Rival's composition.
            EliteTrainerSO rival = ByName<EliteTrainerSO>("EliteTrainer_Rival_Blue");
            EliteTrainerSO specialist = GetOrCreate<EliteTrainerSO>($"{ELITETRAINER_BASE}/EliteTrainer_Specialist_R1.asset", "EliteTrainer_Specialist_R1");
            specialist.EliteId = "SPECIALIST_R1";
            specialist.DisplayName = "Ace Trainer";
            specialist.TrainerSprite = null;
            specialist.TacticalIdentity = "Elevated archetype. Multi-type balanced.";
            specialist.Composition = new List<ElitePokemonSlot>
            {
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Pidgeotto"), Level = 12, PhaseCount = 2 },
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Ivysaur"), Level = 13, PhaseCount = 2 }
            };
            specialist.RareRelicChoices = new List<RelicSO>
            {
                ByName<RelicSO>("versatile_gem"),
                ByName<RelicSO>("conquerors_stone"),
                ByName<RelicSO>("hive_mind")
            }.Where(r => r != null).ToList();
            specialist.TrainerXPReward = 25;
            specialist.PokeDollarReward = 300;
            specialist.IsRival = false;
            specialist.RivalEvoSpecies = null;
            specialist.RegionScaling = new List<EliteRegionScaling>();
            specialist.GDDReference = "§7.5.1 — Specialist R1 (Ace Trainer)";
            EditorUtility.SetDirty(specialist);

            roster.OccupantPool = new List<EliteOccupantEntry>
            {
                new EliteOccupantEntry { Occupant = rival, Weight = 80f },
                new EliteOccupantEntry { Occupant = specialist, Weight = 20f }
            };
            roster.GDDReference = "§7.5.1";

            EditorUtility.SetDirty(roster);
            Debug.Log($"[CL-024] Seeded {roster.name} (Rival 80 / Specialist 20)");
        }

        // === Helper methods ===

        private static T GetOrCreate<T>(string path, string defaultName) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = defaultName;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T ByName<T>(string nameHint) where T : Object
        {
            foreach (string g in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p).ToLowerInvariant() == nameHint.ToLowerInvariant())
                    return AssetDatabase.LoadAssetAtPath<T>(p);
            }
            // fallback: contains
            foreach (string g in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains(nameHint.ToLowerInvariant()))
                    return AssetDatabase.LoadAssetAtPath<T>(p);
            }
            return null;
        }

        // Ensure a MoveSO exists; if not, create placeholder.
        private static MoveSO EnsureMove(string id, string displayName, PokemonType type, MoveRole role, MoveRange range, int power, int apCost, float rangeMultiplier, string flavorText, string gddRef)
        {
            MoveSO existing = ByName<MoveSO>(id);
            if (existing != null) return existing;

            string folder = $"{MOVES_BASE}/Shared";
            Directory.CreateDirectory(folder);
            MoveSO move = GetOrCreate<MoveSO>($"{folder}/{id}.asset", id);
            move.MoveId = id;
            move.DisplayName = displayName;
            move.Type = type;
            move.Role = role;
            move.Range = range;
            move.Modifier = PositionalModifier.None;
            move.BasePower = power;
            move.APCost = apCost;
            move.CooldownTurns = 0;
            move.RangeModifierMultiplier = rangeMultiplier;
            move.AlwaysCrit = false;
            move.Effects = new List<MoveEffectSO>();
            move.CardArt = null;
            move.FlavorText = flavorText;
            move.GDDReference = gddRef;
            EditorUtility.SetDirty(move);
            return move;
        }

        // Ensure an AbilitySO exists; if not, create placeholder.
        private static AbilitySO EnsureAbility(string id, string displayName, string description, AbilityCategory category, string gddRef)
        {
            AbilitySO existing = ByName<AbilitySO>(id);
            if (existing != null) return existing;

            string folder = ABILITIES_BASE;
            Directory.CreateDirectory(folder);
            AbilitySO ability = GetOrCreate<AbilitySO>($"{folder}/{id}.asset", id);
            ability.AbilityId = id;
            ability.DisplayName = displayName;
            ability.Description = description;
            ability.Category = category;
            ability.GrantsLeadAura = false;
            ability.EffectHook = null;
            ability.GDDReference = gddRef;
            EditorUtility.SetDirty(ability);
            return ability;
        }
    }
}
