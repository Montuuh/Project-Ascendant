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
            rival.RivalEvoBranch = null; // R1 = 2-phase, no mid-fight evo

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
            specialist.RivalEvoBranch = null;
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

        // ===================================
        // Region 2 Content
        // ===================================

        [MenuItem("Project Ascendant/CL-024/Seed Elite Content (R2)")]
        public static void SeedRegion2()
        {
            Debug.Log("[CL-024] Seeding Region 2 Elite content...");

            // Per design/cl-024-content-spec.md — R2 species + Rival + roster.
            SeedGyaradosSpecies();
            SeedExeggutorSpecies();
            SeedHitmonchanSpecies();
            SeedPrimapeSpecies();
            SeedRivalBlue_R2();
            SeedKarateKingSpecialist_R2();
            SeedEliteRoster_R2();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CL-024] Region 2 complete. Update RunContentCatalog to wire R2 roster.");
        }

        // §7.5.1 — Gyarados #130 (Water/Flying, R2 Rival).
        // Intimidate passive. Tunable placeholder stats ~Lv23-25.
        private static void SeedGyaradosSpecies()
        {
            string folder = $"{SPECIES_BASE}/Gyarados";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO gyarados = GetOrCreate<PokemonSpeciesSO>($"{folder}/Gyarados.asset", "Gyarados");
            gyarados.SpeciesId = "130";
            gyarados.DisplayName = "Gyarados";
            gyarados.Types = new List<PokemonType> { PokemonType.Water, PokemonType.Flying };
            gyarados.BaseStats = new BaseStats
            {
                BaseHP = 95,
                BaseAtk = 125,
                BaseDef = 79,
                BaseSpd = 81
            };
            gyarados.GrowthCurve = ByName<StatGrowthCurveSO>("Slow");
            gyarados.Branches = new List<EvolutionBranchSO>();
            gyarados.EvolveLevel = 0;

            // Per §5.12.1 — lean learnset.
            gyarados.BaseLearnset = new List<MoveSO>();
            gyarados.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("aqua_tail", "Aqua Tail", PokemonType.Water, MoveRole.Offensive, MoveRange.Melee, 90, 2, 1f, "Melee 90 Water.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("dragon_rage", "Dragon Rage", PokemonType.Dragon, MoveRole.Offensive, MoveRange.Ranged, 40, 1, 0.75f, "Ranged 40 Dragon. Fixed damage variant in Gen I.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("bite", "Bite", PokemonType.Dark, MoveRole.Offensive, MoveRange.Melee, 60, 1, 1f, "Melee 60 Dark. 30% Flinch (post-VS).", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("leer", "Leer", PokemonType.Normal, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Target Defense -1.", "§5.12.1") }
            };
            gyarados.TutorLearnset = new List<MoveSO>();
            gyarados.TMCompatibility = new List<TMSO>();

            gyarados.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("intimidate", "Intimidate", "Lowers foes' Attack on entry.", AbilityCategory.Combat, "§7.5.1")
            };

            gyarados.StatusImmunities = new List<StatusCondition>();
            gyarados.WildRarity = RarityTier.Rare;
            gyarados.SpawnBiomes = new List<Biome>();
            gyarados.Portrait = null;
            gyarados.GDDReference = "§7.5.1 — Gyarados (R2 Rival)";

            EditorUtility.SetDirty(gyarados);
            Debug.Log($"[CL-024] Seeded {gyarados.name}");
        }

        // §7.5.1 — Exeggutor #103 (Grass/Psychic, R2 Rival).
        private static void SeedExeggutorSpecies()
        {
            string folder = $"{SPECIES_BASE}/Exeggutor";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO exeggutor = GetOrCreate<PokemonSpeciesSO>($"{folder}/Exeggutor.asset", "Exeggutor");
            exeggutor.SpeciesId = "103";
            exeggutor.DisplayName = "Exeggutor";
            exeggutor.Types = new List<PokemonType> { PokemonType.Grass, PokemonType.Psychic };
            exeggutor.BaseStats = new BaseStats
            {
                BaseHP = 95,
                BaseAtk = 95,
                BaseDef = 85,
                BaseSpd = 55
            };
            exeggutor.GrowthCurve = ByName<StatGrowthCurveSO>("Slow");
            exeggutor.Branches = new List<EvolutionBranchSO>();
            exeggutor.EvolveLevel = 0;

            exeggutor.BaseLearnset = new List<MoveSO>();
            exeggutor.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("psychic", "Psychic", PokemonType.Psychic, MoveRole.Offensive, MoveRange.Ranged, 90, 2, 0.75f, "Ranged 90 Psychic. 10% SpD↓.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("solar_beam", "Solar Beam", PokemonType.Grass, MoveRole.Offensive, MoveRange.Ranged, 120, 3, 0.75f, "Ranged 120 Grass. 2-turn charge in standard weather.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("stomp", "Stomp", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 65, 1, 1f, "Melee 65. 30% Flinch (post-VS).", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("hypnosis", "Hypnosis", PokemonType.Psychic, MoveRole.Utility, MoveRange.Ranged, 0, 1, 0.6f, "60% to inflict Sleep 2t.", "§7.5.1") }
            };
            exeggutor.TutorLearnset = new List<MoveSO>();
            exeggutor.TMCompatibility = new List<TMSO>();

            exeggutor.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("chlorophyll", "Chlorophyll", "Speed doubled in harsh sunlight.", AbilityCategory.Combat, "§7.5.1")
            };

            exeggutor.StatusImmunities = new List<StatusCondition>();
            exeggutor.WildRarity = RarityTier.Rare;
            exeggutor.SpawnBiomes = new List<Biome>();
            exeggutor.Portrait = null;
            exeggutor.GDDReference = "§7.5.1 — Exeggutor (R2 Rival)";

            EditorUtility.SetDirty(exeggutor);
            Debug.Log($"[CL-024] Seeded {exeggutor.name}");
        }

        // §7.5.1 — Hitmonchan #107 (Fighting, R2 Specialist Karate King).
        private static void SeedHitmonchanSpecies()
        {
            string folder = $"{SPECIES_BASE}/Hitmonchan";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO hitmonchan = GetOrCreate<PokemonSpeciesSO>($"{folder}/Hitmonchan.asset", "Hitmonchan");
            hitmonchan.SpeciesId = "107";
            hitmonchan.DisplayName = "Hitmonchan";
            hitmonchan.Types = new List<PokemonType> { PokemonType.Fighting };
            hitmonchan.BaseStats = new BaseStats
            {
                BaseHP = 50,
                BaseAtk = 105,
                BaseDef = 79,
                BaseSpd = 76
            };
            hitmonchan.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            hitmonchan.Branches = new List<EvolutionBranchSO>();
            hitmonchan.EvolveLevel = 0;

            hitmonchan.BaseLearnset = new List<MoveSO>();
            hitmonchan.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("sky_uppercut", "Sky Uppercut", PokemonType.Fighting, MoveRole.Offensive, MoveRange.Melee, 85, 2, 1f, "Melee 85 Fighting. Hits airborne targets.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("mach_punch", "Mach Punch", PokemonType.Fighting, MoveRole.Offensive, MoveRange.Melee, 40, 1, 1f, "Melee 40 Fighting. Priority +1.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("ice_punch", "Ice Punch", PokemonType.Ice, MoveRole.Offensive, MoveRange.Melee, 75, 2, 1f, "Melee 75 Ice. 10% Freeze.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("agility", "Agility", PokemonType.Psychic, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "+2 Speed.", "§5.12.1") }
            };
            hitmonchan.TutorLearnset = new List<MoveSO>();
            hitmonchan.TMCompatibility = new List<TMSO>();

            hitmonchan.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("iron_fist", "Iron Fist", "Punching moves deal +20% damage.", AbilityCategory.Combat, "§7.5.1")
            };

            hitmonchan.StatusImmunities = new List<StatusCondition>();
            hitmonchan.WildRarity = RarityTier.Rare;
            hitmonchan.SpawnBiomes = new List<Biome>();
            hitmonchan.Portrait = null;
            hitmonchan.GDDReference = "§7.5.1 — Hitmonchan (R2 Specialist Karate King)";

            EditorUtility.SetDirty(hitmonchan);
            Debug.Log($"[CL-024] Seeded {hitmonchan.name}");
        }

        // §7.5.1 — Primeape #57 (Fighting, R2 Specialist Karate King).
        private static void SeedPrimapeSpecies()
        {
            string folder = $"{SPECIES_BASE}/Primeape";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO primeape = GetOrCreate<PokemonSpeciesSO>($"{folder}/Primeape.asset", "Primeape");
            primeape.SpeciesId = "57";
            primeape.DisplayName = "Primeape";
            primeape.Types = new List<PokemonType> { PokemonType.Fighting };
            primeape.BaseStats = new BaseStats
            {
                BaseHP = 65,
                BaseAtk = 105,
                BaseDef = 60,
                BaseSpd = 95
            };
            primeape.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            primeape.Branches = new List<EvolutionBranchSO>();
            primeape.EvolveLevel = 0;

            primeape.BaseLearnset = new List<MoveSO>();
            primeape.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("cross_chop", "Cross Chop", PokemonType.Fighting, MoveRole.Offensive, MoveRange.Melee, 100, 2, 1f, "Melee 100 Fighting. High crit rate.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("thrash_fixed", "Thrash", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 120, 3, 1f, "Melee 120. Locked for exactly 2 turns (deterministic per spec), then self-Confusion.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("screech", "Screech", PokemonType.Normal, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Target Defense -2.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("rage", "Rage", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 20, 1, 1f, "Melee 20. +1 Attack each time hit.", "§7.5.1") }
            };
            primeape.TutorLearnset = new List<MoveSO>();
            primeape.TMCompatibility = new List<TMSO>();

            primeape.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("vital_spirit", "Vital Spirit", "Cannot be put to Sleep.", AbilityCategory.Survival, "§7.5.1")
            };

            primeape.StatusImmunities = new List<StatusCondition>();
            primeape.WildRarity = RarityTier.Rare;
            primeape.SpawnBiomes = new List<Biome>();
            primeape.Portrait = null;
            primeape.GDDReference = "§7.5.1 — Primeape (R2 Specialist Karate King)";

            EditorUtility.SetDirty(primeape);
            Debug.Log($"[CL-024] Seeded {primeape.name}");
        }

        // §7.5.1 — Rival Blue R2 Elite Trainer (3 mons: Pidgeot + Gyarados + Exeggutor, 2-phase).
        // Reuses existing Pidgeot species. RegionScaling for R2.
        private static void SeedRivalBlue_R2()
        {
            // Fetch existing Rival asset and update R2 scaling.
            EliteTrainerSO rival = ByName<EliteTrainerSO>("EliteTrainer_Rival_Blue");
            if (rival == null)
            {
                Debug.LogError("[CL-024] EliteTrainer_Rival_Blue not found. Run R1 seeder first.");
                return;
            }

            // Add R2 scaling entry.
            if (rival.RegionScaling == null) rival.RegionScaling = new List<EliteRegionScaling>();
            rival.RegionScaling.Add(new EliteRegionScaling { RegionIndex = 1, PokemonCount = 3, AcePhaseCount = 2 });

            EditorUtility.SetDirty(rival);
            Debug.Log("[CL-024] Updated Rival Blue R2 scaling (3 Pokémon, 2-phase ace).");
        }

        // §7.5.1 — Karate King Specialist R2 (Hitmonchan + Primeape, 2-phase).
        private static void SeedKarateKingSpecialist_R2()
        {
            string folder = ELITETRAINER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerSO karateKing = GetOrCreate<EliteTrainerSO>($"{folder}/EliteTrainer_KarateKing_R2.asset", "EliteTrainer_KarateKing_R2");
            karateKing.EliteId = "KARATE_KING_R2";
            karateKing.DisplayName = "Karate King";
            karateKing.TrainerSprite = null;
            karateKing.TacticalIdentity = "Fighting specialist. High-speed offense.";

            karateKing.Composition = new List<ElitePokemonSlot>
            {
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Hitmonchan"), Level = 23, PhaseCount = 2 },
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Primeape"), Level = 24, PhaseCount = 2 }
            };

            karateKing.RareRelicChoices = new List<RelicSO>
            {
                ByName<RelicSO>("conquerors_stone"),
                ByName<RelicSO>("hive_mind"),
                ByName<RelicSO>("versatile_gem")
            }.Where(r => r != null).ToList();

            karateKing.TrainerXPReward = 25;
            karateKing.PokeDollarReward = 300;
            karateKing.IsRival = false;
            karateKing.RivalEvoBranch = null;
            karateKing.RegionScaling = new List<EliteRegionScaling>();
            karateKing.GDDReference = "§7.5.1 — Specialist R2 (Karate King)";

            EditorUtility.SetDirty(karateKing);
            Debug.Log($"[CL-024] Seeded {karateKing.name}");
        }

        // §7.5.1 — R2 Elite Trainer Roster (Rival 60 / Specialist 40).
        private static void SeedEliteRoster_R2()
        {
            string folder = ELITEROSTER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerRosterSO roster = GetOrCreate<EliteTrainerRosterSO>($"{folder}/EliteTrainerRoster_R2.asset", "EliteTrainerRoster_R2");
            roster.RegionIndex = 1;

            EliteTrainerSO rival = ByName<EliteTrainerSO>("EliteTrainer_Rival_Blue");
            EliteTrainerSO specialist = ByName<EliteTrainerSO>("EliteTrainer_KarateKing_R2");

            roster.OccupantPool = new List<EliteOccupantEntry>
            {
                new EliteOccupantEntry { Occupant = rival, Weight = 60f },
                new EliteOccupantEntry { Occupant = specialist, Weight = 40f }
            };
            roster.GDDReference = "§7.5.1";

            EditorUtility.SetDirty(roster);
            Debug.Log($"[CL-024] Seeded {roster.name} (Rival 60 / Specialist 40)");
        }

        // ===================================
        // Region 3 Content
        // ===================================

        [MenuItem("Project Ascendant/CL-024/Seed Elite Content (R3)")]
        public static void SeedRegion3()
        {
            Debug.Log("[CL-024] Seeding Region 3 Elite content...");

            // Per design/cl-024-content-spec.md — R3 species + Rival ace evo + Giovanni dual-lane + roster.
            SeedAlakazamSpecies();
            SeedDugtrioSpecies();
            SeedPersianSpecies();
            SeedNidoqueenSpecies();
            SeedRhydonSpecies();
            SeedDewgongSpecies();
            SeedCloysterSpecies();
            SeedRivalBlue_R3();
            SeedGiovanniElite_R3();
            SeedGiovanniGym_R3();
            SeedCooltrainerSpecialist_R3();
            SeedEliteRoster_R3();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CL-024] Region 3 complete. Update RunContentCatalog to wire R3 roster + Gym pool.");
        }

        // §7.5.1 — Alakazam #65 (Psychic, R3 Rival).
        private static void SeedAlakazamSpecies()
        {
            string folder = $"{SPECIES_BASE}/Alakazam";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO alakazam = GetOrCreate<PokemonSpeciesSO>($"{folder}/Alakazam.asset", "Alakazam");
            alakazam.SpeciesId = "65";
            alakazam.DisplayName = "Alakazam";
            alakazam.Types = new List<PokemonType> { PokemonType.Psychic };
            alakazam.BaseStats = new BaseStats
            {
                BaseHP = 55,
                BaseAtk = 135,
                BaseDef = 45,
                BaseSpd = 120
            };
            alakazam.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Slow");
            alakazam.Branches = new List<EvolutionBranchSO>();
            alakazam.EvolveLevel = 0;

            alakazam.BaseLearnset = new List<MoveSO>();
            alakazam.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("psychic", "Psychic", PokemonType.Psychic, MoveRole.Offensive, MoveRange.Ranged, 90, 2, 0.75f, "Ranged 90 Psychic. 10% SpD↓.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("psybeam", "Psybeam", PokemonType.Psychic, MoveRole.Offensive, MoveRange.Ranged, 65, 1, 0.75f, "Ranged 65 Psychic. 10% Confusion.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("recover", "Recover", PokemonType.Normal, MoveRole.Defensive, MoveRange.Ranged, 0, 1, 1f, "Heal 50% MaxHP.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("reflect", "Reflect", PokemonType.Psychic, MoveRole.Defensive, MoveRange.Ranged, 0, 1, 1f, "Set screen: halve physical damage for 5 turns.", "§7.5.1") }
            };
            alakazam.TutorLearnset = new List<MoveSO>();
            alakazam.TMCompatibility = new List<TMSO>();

            alakazam.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("synchronize", "Synchronize", "Passes status conditions to the attacker.", AbilityCategory.Combat, "§7.5.1")
            };

            alakazam.StatusImmunities = new List<StatusCondition>();
            alakazam.WildRarity = RarityTier.Rare;
            alakazam.SpawnBiomes = new List<Biome>();
            alakazam.Portrait = null;
            alakazam.GDDReference = "§7.5.1 — Alakazam (R3 Rival)";

            EditorUtility.SetDirty(alakazam);
            Debug.Log($"[CL-024] Seeded {alakazam.name}");
        }

        // §7.5.1 — Dugtrio #51 (Ground, R3 Giovanni Elite). Passive: Sand Veil (NOT Arena Trap per spec).
        private static void SeedDugtrioSpecies()
        {
            string folder = $"{SPECIES_BASE}/Dugtrio";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO dugtrio = GetOrCreate<PokemonSpeciesSO>($"{folder}/Dugtrio.asset", "Dugtrio");
            dugtrio.SpeciesId = "51";
            dugtrio.DisplayName = "Dugtrio";
            dugtrio.Types = new List<PokemonType> { PokemonType.Ground };
            dugtrio.BaseStats = new BaseStats
            {
                BaseHP = 35,
                BaseAtk = 100,
                BaseDef = 50,
                BaseSpd = 120
            };
            dugtrio.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            dugtrio.Branches = new List<EvolutionBranchSO>();
            dugtrio.EvolveLevel = 0;

            dugtrio.BaseLearnset = new List<MoveSO>();
            dugtrio.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("earthquake", "Earthquake", PokemonType.Ground, MoveRole.Offensive, MoveRange.Ranged, 100, 2, 0.75f, "Ranged 100 Ground. Hits all foes.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("slash", "Slash", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 70, 1, 1f, "Melee 70. High crit rate.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("sand_attack", "Sand Attack", PokemonType.Ground, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Target accuracy -1 (or Evasion +1).", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("dig", "Dig", PokemonType.Ground, MoveRole.Offensive, MoveRange.Melee, 80, 2, 1f, "Melee 80 Ground. 2-turn (underground P1, strike P2).", "§7.5.1") }
            };
            dugtrio.TutorLearnset = new List<MoveSO>();
            dugtrio.TMCompatibility = new List<TMSO>();

            // Per spec: Sand Veil, NOT Arena Trap.
            dugtrio.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("sand_veil", "Sand Veil", "Evasion +20% in sandstorm.", AbilityCategory.Combat, "§7.5.1 + Pillar-1 fix")
            };

            dugtrio.StatusImmunities = new List<StatusCondition>();
            dugtrio.WildRarity = RarityTier.Rare;
            dugtrio.SpawnBiomes = new List<Biome>();
            dugtrio.Portrait = null;
            dugtrio.GDDReference = "§7.5.1 — Dugtrio (R3 Giovanni Elite, Sand Veil per spec)";

            EditorUtility.SetDirty(dugtrio);
            Debug.Log($"[CL-024] Seeded {dugtrio.name}");
        }

        // §7.5.1 — Persian #53 (Normal, R3 Giovanni Elite signature).
        private static void SeedPersianSpecies()
        {
            string folder = $"{SPECIES_BASE}/Persian";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO persian = GetOrCreate<PokemonSpeciesSO>($"{folder}/Persian.asset", "Persian");
            persian.SpeciesId = "53";
            persian.DisplayName = "Persian";
            persian.Types = new List<PokemonType> { PokemonType.Normal };
            persian.BaseStats = new BaseStats
            {
                BaseHP = 65,
                BaseAtk = 70,
                BaseDef = 60,
                BaseSpd = 115
            };
            persian.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            persian.Branches = new List<EvolutionBranchSO>();
            persian.EvolveLevel = 0;

            persian.BaseLearnset = new List<MoveSO>();
            persian.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("slash", "Slash", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 70, 1, 1f, "Melee 70. High crit rate.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("fury_swipes_fixed", "Fury Swipes", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 18, 1, 1f, "Melee 18. Hits exactly 3 times (deterministic per spec).", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("screech", "Screech", PokemonType.Normal, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Target Defense -2.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("bite", "Bite", PokemonType.Dark, MoveRole.Offensive, MoveRange.Melee, 60, 1, 1f, "Melee 60 Dark. 30% Flinch (post-VS).", "§5.12.1") }
            };
            persian.TutorLearnset = new List<MoveSO>();
            persian.TMCompatibility = new List<TMSO>();

            persian.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("tough_claws", "Tough Claws", "Contact moves deal +30% damage.", AbilityCategory.Combat, "§7.5.1")
            };

            persian.StatusImmunities = new List<StatusCondition>();
            persian.WildRarity = RarityTier.Rare;
            persian.SpawnBiomes = new List<Biome>();
            persian.Portrait = null;
            persian.GDDReference = "§7.5.1 — Persian (R3 Giovanni Elite signature)";

            EditorUtility.SetDirty(persian);
            Debug.Log($"[CL-024] Seeded {persian.name}");
        }

        // §7.5.1 / §4.4.4 — Nidoqueen #31 (Poison/Ground, Giovanni Gym lead).
        private static void SeedNidoqueenSpecies()
        {
            string folder = $"{SPECIES_BASE}/Nidoqueen";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO nidoqueen = GetOrCreate<PokemonSpeciesSO>($"{folder}/Nidoqueen.asset", "Nidoqueen");
            nidoqueen.SpeciesId = "31";
            nidoqueen.DisplayName = "Nidoqueen";
            nidoqueen.Types = new List<PokemonType> { PokemonType.Poison, PokemonType.Ground };
            nidoqueen.BaseStats = new BaseStats
            {
                BaseHP = 90,
                BaseAtk = 92,
                BaseDef = 87,
                BaseSpd = 76
            };
            nidoqueen.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Slow");
            nidoqueen.Branches = new List<EvolutionBranchSO>();
            nidoqueen.EvolveLevel = 0;

            nidoqueen.BaseLearnset = new List<MoveSO>();
            nidoqueen.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("earthquake", "Earthquake", PokemonType.Ground, MoveRole.Offensive, MoveRange.Ranged, 100, 2, 0.75f, "Ranged 100 Ground. Hits all foes.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("sludge_bomb", "Sludge Bomb", PokemonType.Poison, MoveRole.Offensive, MoveRange.Ranged, 90, 2, 0.75f, "Ranged 90 Poison. 30% Poison rider.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("body_slam", "Body Slam", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 85, 2, 1f, "Melee 85. 30% Paralysis rider.", "§7.5.2") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("toxic", "Toxic", PokemonType.Poison, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Inflict badly poisoned (escalating DoT).", "§7.5.1") }
            };
            nidoqueen.TutorLearnset = new List<MoveSO>();
            nidoqueen.TMCompatibility = new List<TMSO>();

            nidoqueen.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("poison_point", "Poison Point", "30% to Poison attacker on contact.", AbilityCategory.Combat, "§7.5.1")
            };

            nidoqueen.StatusImmunities = new List<StatusCondition>();
            nidoqueen.WildRarity = RarityTier.Rare;
            nidoqueen.SpawnBiomes = new List<Biome>();
            nidoqueen.Portrait = null;
            nidoqueen.GDDReference = "§7.5.1 / §4.4.4 — Nidoqueen (Giovanni Gym lead)";

            EditorUtility.SetDirty(nidoqueen);
            Debug.Log($"[CL-024] Seeded {nidoqueen.name}");
        }

        // §7.5.1 / §4.4.4 — Rhydon #112 (Ground/Rock, Giovanni Gym ace 3-phase, Solid Rock).
        private static void SeedRhydonSpecies()
        {
            string folder = $"{SPECIES_BASE}/Rhydon";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO rhydon = GetOrCreate<PokemonSpeciesSO>($"{folder}/Rhydon.asset", "Rhydon");
            rhydon.SpeciesId = "112";
            rhydon.DisplayName = "Rhydon";
            rhydon.Types = new List<PokemonType> { PokemonType.Ground, PokemonType.Rock };
            rhydon.BaseStats = new BaseStats
            {
                BaseHP = 105,
                BaseAtk = 130,
                BaseDef = 120,
                BaseSpd = 40
            };
            rhydon.GrowthCurve = ByName<StatGrowthCurveSO>("Slow");
            rhydon.Branches = new List<EvolutionBranchSO>();
            rhydon.EvolveLevel = 0;

            rhydon.BaseLearnset = new List<MoveSO>();
            rhydon.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("earthquake", "Earthquake", PokemonType.Ground, MoveRole.Offensive, MoveRange.Ranged, 100, 2, 0.75f, "Ranged 100 Ground. Hits all foes.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("rock_slide", "Rock Slide", PokemonType.Rock, MoveRole.Offensive, MoveRange.Ranged, 75, 2, 0.75f, "Ranged 75 Rock. 30% Flinch (post-VS).", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("horn_drill", "Horn Drill", PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee, 1, 3, 1f, "OHKO move. 30% accuracy.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("scary_face", "Scary Face", PokemonType.Normal, MoveRole.Utility, MoveRange.Ranged, 0, 1, 1f, "Target Speed -2.", "§7.5.1") }
            };
            rhydon.TutorLearnset = new List<MoveSO>();
            rhydon.TMCompatibility = new List<TMSO>();

            rhydon.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("solid_rock", "Solid Rock", "Reduces super-effective damage by 25%.", AbilityCategory.Combat, "§7.5.1")
            };

            rhydon.StatusImmunities = new List<StatusCondition>();
            rhydon.WildRarity = RarityTier.Rare;
            rhydon.SpawnBiomes = new List<Biome>();
            rhydon.Portrait = null;
            rhydon.GDDReference = "§7.5.1 / §4.4.4 — Rhydon (Giovanni Gym ace, 3-phase, Solid Rock)";

            EditorUtility.SetDirty(rhydon);
            Debug.Log($"[CL-024] Seeded {rhydon.name}");
        }

        // §7.5.1 — Dewgong #87 (Water/Ice, R3 Specialist Cooltrainer).
        private static void SeedDewgongSpecies()
        {
            string folder = $"{SPECIES_BASE}/Dewgong";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO dewgong = GetOrCreate<PokemonSpeciesSO>($"{folder}/Dewgong.asset", "Dewgong");
            dewgong.SpeciesId = "87";
            dewgong.DisplayName = "Dewgong";
            dewgong.Types = new List<PokemonType> { PokemonType.Water, PokemonType.Ice };
            dewgong.BaseStats = new BaseStats
            {
                BaseHP = 90,
                BaseAtk = 70,
                BaseDef = 80,
                BaseSpd = 70
            };
            dewgong.GrowthCurve = ByName<StatGrowthCurveSO>("Medium_Fast");
            dewgong.Branches = new List<EvolutionBranchSO>();
            dewgong.EvolveLevel = 0;

            dewgong.BaseLearnset = new List<MoveSO>();
            dewgong.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("ice_beam", "Ice Beam", PokemonType.Ice, MoveRole.Offensive, MoveRange.Ranged, 90, 2, 0.75f, "Ranged 90 Ice. 10% Freeze.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("surf", "Surf", PokemonType.Water, MoveRole.Offensive, MoveRange.Ranged, 90, 2, 0.75f, "Ranged 90 Water. Hits all adjacent foes.", "§5.12.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("aurora_beam", "Aurora Beam", PokemonType.Ice, MoveRole.Offensive, MoveRange.Ranged, 65, 1, 0.75f, "Ranged 65 Ice. 10% Attack↓.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("rest", "Rest", PokemonType.Psychic, MoveRole.Defensive, MoveRange.Ranged, 0, 1, 1f, "Heal 100% MaxHP, self-Sleep 2t.", "§5.12.1") }
            };
            dewgong.TutorLearnset = new List<MoveSO>();
            dewgong.TMCompatibility = new List<TMSO>();

            dewgong.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("thick_fat", "Thick Fat", "Half damage from Fire and Ice moves.", AbilityCategory.Combat, "§7.5.2")
            };

            dewgong.StatusImmunities = new List<StatusCondition>();
            dewgong.WildRarity = RarityTier.Rare;
            dewgong.SpawnBiomes = new List<Biome>();
            dewgong.Portrait = null;
            dewgong.GDDReference = "§7.5.1 — Dewgong (R3 Specialist Cooltrainer)";

            EditorUtility.SetDirty(dewgong);
            Debug.Log($"[CL-024] Seeded {dewgong.name}");
        }

        // §7.5.1 — Cloyster #91 (Water/Ice, R3 Specialist Cooltrainer).
        private static void SeedCloysterSpecies()
        {
            string folder = $"{SPECIES_BASE}/Cloyster";
            Directory.CreateDirectory(folder);

            PokemonSpeciesSO cloyster = GetOrCreate<PokemonSpeciesSO>($"{folder}/Cloyster.asset", "Cloyster");
            cloyster.SpeciesId = "91";
            cloyster.DisplayName = "Cloyster";
            cloyster.Types = new List<PokemonType> { PokemonType.Water, PokemonType.Ice };
            cloyster.BaseStats = new BaseStats
            {
                BaseHP = 50,
                BaseAtk = 95,
                BaseDef = 180,
                BaseSpd = 70
            };
            cloyster.GrowthCurve = ByName<StatGrowthCurveSO>("Slow");
            cloyster.Branches = new List<EvolutionBranchSO>();
            cloyster.EvolveLevel = 0;

            cloyster.BaseLearnset = new List<MoveSO>();
            cloyster.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = EnsureMove("icicle_spear_fixed", "Icicle Spear", PokemonType.Ice, MoveRole.Offensive, MoveRange.Ranged, 25, 2, 0.75f, "Ranged 25 Ice. Hits exactly 5 times (deterministic per spec).", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("spike_cannon_fixed", "Spike Cannon", PokemonType.Normal, MoveRole.Offensive, MoveRange.Ranged, 20, 2, 0.75f, "Ranged 20. Hits exactly 5 times (deterministic per spec).", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("clamp", "Clamp", PokemonType.Water, MoveRole.Offensive, MoveRange.Melee, 35, 1, 1f, "Melee 35 Water. Traps target for 2-5 turns.", "§7.5.1") },
                new LevelUpEntry { Level = 1, Move = EnsureMove("withdraw", "Withdraw", PokemonType.Water, MoveRole.Defensive, MoveRange.Ranged, 0, 1, 1f, "+1 Defense.", "§5.12.1") }
            };
            cloyster.TutorLearnset = new List<MoveSO>();
            cloyster.TMCompatibility = new List<TMSO>();

            cloyster.AvailableAbilities = new List<AbilitySO>
            {
                EnsureAbility("shell_armor", "Shell Armor", "Cannot be critically hit.", AbilityCategory.Combat, "§7.5.1")
            };

            cloyster.StatusImmunities = new List<StatusCondition>();
            cloyster.WildRarity = RarityTier.Rare;
            cloyster.SpawnBiomes = new List<Biome>();
            cloyster.Portrait = null;
            cloyster.GDDReference = "§7.5.1 — Cloyster (R3 Specialist Cooltrainer)";

            EditorUtility.SetDirty(cloyster);
            Debug.Log($"[CL-024] Seeded {cloyster.name}");
        }

        // §7.5.1 — Rival Blue R3 Elite Trainer (3 mons: Pidgeot + Alakazam + Wartortle→Blastoise ace).
        // RivalEvoBranch = existing Wartortle Vanguard→Blastoise branch (§4.3.7).
        private static void SeedRivalBlue_R3()
        {
            EliteTrainerSO rival = ByName<EliteTrainerSO>("EliteTrainer_Rival_Blue");
            if (rival == null)
            {
                Debug.LogError("[CL-024] EliteTrainer_Rival_Blue not found. Run R1 seeder first.");
                return;
            }

            // Add R3 scaling entry: 3 Pokémon, ace 3-phase (mid-fight evo at 50% HP).
            if (rival.RegionScaling == null) rival.RegionScaling = new List<EliteRegionScaling>();
            rival.RegionScaling.Add(new EliteRegionScaling { RegionIndex = 2, PokemonCount = 3, AcePhaseCount = 3 });

            // Per spec: Rival R3 ace evolves Wartortle→Blastoise at 50% HP. Use existing Vanguard branch.
            EvolutionBranchSO wartortleBranch = ByName<EvolutionBranchSO>("wartortle_vanguard");
            if (wartortleBranch == null)
            {
                Debug.LogWarning("[CL-024] wartortle_vanguard branch not found. RivalEvoBranch will be null (needs authoring).");
            }
            rival.RivalEvoBranch = wartortleBranch;

            EditorUtility.SetDirty(rival);
            Debug.Log("[CL-024] Updated Rival Blue R3 scaling (3 Pokémon, ace 3-phase + Wartortle→Blastoise evo).");
        }

        // §7.5.1 — Giovanni Elite Trainer R3 (Dugtrio + Persian, 2-phase).
        private static void SeedGiovanniElite_R3()
        {
            string folder = ELITETRAINER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerSO giovanni = GetOrCreate<EliteTrainerSO>($"{folder}/EliteTrainer_Giovanni_R3.asset", "EliteTrainer_Giovanni_R3");
            giovanni.EliteId = "GIOVANNI_R3";
            giovanni.DisplayName = "Giovanni";
            giovanni.TrainerSprite = null;
            giovanni.TacticalIdentity = "Ground specialist. Signature Persian. Dual-lane (Elite + Gym).";

            giovanni.Composition = new List<ElitePokemonSlot>
            {
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Dugtrio"), Level = 33, PhaseCount = 2 },
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Persian"), Level = 35, PhaseCount = 2 }
            };

            giovanni.RareRelicChoices = new List<RelicSO>
            {
                ByName<RelicSO>("conquerors_stone"),
                ByName<RelicSO>("hive_mind"),
                ByName<RelicSO>("neptunes_trident")
            }.Where(r => r != null).ToList();

            giovanni.TrainerXPReward = 25;
            giovanni.PokeDollarReward = 300;
            giovanni.IsRival = false;
            giovanni.RivalEvoBranch = null;
            giovanni.RegionScaling = new List<EliteRegionScaling>();
            giovanni.GDDReference = "§7.5.1 — Giovanni Elite R3 (dual-lane with Gym)";

            EditorUtility.SetDirty(giovanni);
            Debug.Log($"[CL-024] Seeded {giovanni.name} (Elite R3)");
        }

        // §4.4.4 — Giovanni Gym Leader R3 (Viridian Ground: Nidoqueen lead + Rhydon ace 3-phase, no evo).
        // GymType = Ground → Home Field ×1.5 (§4.4.4.3). Phase2Archetype = Entrenchment (derived from Ground).
        private static void SeedGiovanniGym_R3()
        {
            // Create Earth Badge for Giovanni's Gym (per §4.4.5 — every Gym awards a Badge).
            string badgeFolder = "Assets/ScriptableObjects/VS/Badges";
            Directory.CreateDirectory(badgeFolder);
            BadgeSO earthBadge = GetOrCreate<BadgeSO>($"{badgeFolder}/earth_badge.asset", "earth_badge");
            earthBadge.BadgeId = "earth_badge";
            earthBadge.DisplayName = "Earth Badge";
            earthBadge.GymSource = "R3-Gym-Ground";
            earthBadge.Icon = null;
            earthBadge.GrantedHook = null; // Hook wiring deferred (Epic 12)
            earthBadge.LeadIncomingDamageReduction = 0;
            earthBadge.EffectDescription = "Badge from Giovanni's Viridian Gym. (Effect hook deferred.)";
            earthBadge.GDDReference = "§4.4.5 — Earth Badge (Giovanni R3 Ground Gym)";
            EditorUtility.SetDirty(earthBadge);

            string folder = "Assets/ScriptableObjects/VS/Gyms";
            Directory.CreateDirectory(folder);

            GymLeaderSO gym = GetOrCreate<GymLeaderSO>($"{folder}/GymLeader_Giovanni_R3.asset", "GymLeader_Giovanni_R3");
            gym.GymLeaderId = "GIOVANNI_R3_GYM";
            gym.DisplayName = "Giovanni";
            gym.GymType = PokemonType.Ground;
            gym.TrainerSprite = null;
            gym.TacticalIdentity = "Ground master. Viridian Gym Leader. Entrenchment Phase-2 archetype (§4.4.4.4).";

            gym.Composition = new List<GymPokemonSlot>
            {
                new GymPokemonSlot
                {
                    Species = ByName<PokemonSpeciesSO>("Nidoqueen"),
                    Level = 35,
                    PhaseCount = 2,
                    IsAce = false,
                    HasSturdy = false
                },
                new GymPokemonSlot
                {
                    Species = ByName<PokemonSpeciesSO>("Rhydon"),
                    Level = 37,
                    PhaseCount = 3,
                    IsAce = true,
                    HasSturdy = true
                }
            };

            // Rare relic per §7.12 — Gym reward. Placeholder.
            gym.GuaranteedRareRelic = ByName<RelicSO>("conquerors_stone");

            // Badge — Earth Badge for Viridian (created above).
            gym.BadgeReward = earthBadge;

            gym.TrainerXPReward = 50;
            gym.PokeDollarReward = 500;

            // Phase2Archetype derived from GymType=Ground → Entrenchment per §4.4.4.4.
            gym.AcePhase2Archetype = Phase2Archetype.None; // Let ResolvedAcePhase2Archetype derive it

            gym.GDDReference = "§4.4.4 — Giovanni Viridian Gym (Ground, R3)";

            EditorUtility.SetDirty(gym);
            Debug.Log($"[CL-024] Seeded {gym.name} (Viridian Ground Gym)");
        }

        // §7.5.1 — Cooltrainer Specialist R3 (Dewgong + Cloyster, 2-phase).
        private static void SeedCooltrainerSpecialist_R3()
        {
            string folder = ELITETRAINER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerSO cooltrainer = GetOrCreate<EliteTrainerSO>($"{folder}/EliteTrainer_Cooltrainer_R3.asset", "EliteTrainer_Cooltrainer_R3");
            cooltrainer.EliteId = "COOLTRAINER_R3";
            cooltrainer.DisplayName = "Cooltrainer";
            cooltrainer.TrainerSprite = null;
            cooltrainer.TacticalIdentity = "Water/Ice dual-type. Defensive control.";

            cooltrainer.Composition = new List<ElitePokemonSlot>
            {
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Dewgong"), Level = 33, PhaseCount = 2 },
                new ElitePokemonSlot { Species = ByName<PokemonSpeciesSO>("Cloyster"), Level = 34, PhaseCount = 2 }
            };

            cooltrainer.RareRelicChoices = new List<RelicSO>
            {
                ByName<RelicSO>("versatile_gem"),
                ByName<RelicSO>("conquerors_stone"),
                ByName<RelicSO>("hive_mind")
            }.Where(r => r != null).ToList();

            cooltrainer.TrainerXPReward = 25;
            cooltrainer.PokeDollarReward = 300;
            cooltrainer.IsRival = false;
            cooltrainer.RivalEvoBranch = null;
            cooltrainer.RegionScaling = new List<EliteRegionScaling>();
            cooltrainer.GDDReference = "§7.5.1 — Specialist R3 (Cooltrainer)";

            EditorUtility.SetDirty(cooltrainer);
            Debug.Log($"[CL-024] Seeded {cooltrainer.name}");
        }

        // §7.5.1 — R3 Elite Trainer Roster (Rival 40 / Giovanni 30 / Cooltrainer 30).
        private static void SeedEliteRoster_R3()
        {
            string folder = ELITEROSTER_BASE;
            Directory.CreateDirectory(folder);

            EliteTrainerRosterSO roster = GetOrCreate<EliteTrainerRosterSO>($"{folder}/EliteTrainerRoster_R3.asset", "EliteTrainerRoster_R3");
            roster.RegionIndex = 2;

            EliteTrainerSO rival = ByName<EliteTrainerSO>("EliteTrainer_Rival_Blue");
            EliteTrainerSO giovanni = ByName<EliteTrainerSO>("EliteTrainer_Giovanni_R3");
            EliteTrainerSO cooltrainer = ByName<EliteTrainerSO>("EliteTrainer_Cooltrainer_R3");

            roster.OccupantPool = new List<EliteOccupantEntry>
            {
                new EliteOccupantEntry { Occupant = rival, Weight = 40f },
                new EliteOccupantEntry { Occupant = giovanni, Weight = 30f },
                new EliteOccupantEntry { Occupant = cooltrainer, Weight = 30f }
            };
            roster.GDDReference = "§7.5.1";

            EditorUtility.SetDirty(roster);
            Debug.Log($"[CL-024] Seeded {roster.name} (Rival 40 / Giovanni 30 / Cooltrainer 30)");
        }
    }
}
