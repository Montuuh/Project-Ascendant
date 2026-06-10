#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    public static class VS_Verifier
    {
        [MenuItem("Project Ascendant/Verify VS Content")]
        public static void Verify()
        {
            int pass = 0, fail = 0;

            void Check(bool cond, string msg)
            {
                if (cond) { pass++; }
                else      { fail++; Debug.LogError($"[VS_Verifier] FAIL: {msg}"); }
            }

            string root = "Assets/ScriptableObjects/VS";

            // ── Species wiring ───────────────────────────────────────────────
            var squirtle   = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{root}/Species/Starters/Squirtle/Squirtle.asset");
            var wartortle  = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{root}/Species/Starters/Squirtle/Wartortle_Vanguard.asset");
            var branch     = AssetDatabase.LoadAssetAtPath<EvolutionBranchSO>($"{root}/Branches/squirtle/squirtle_vanguard.asset");

            Check(squirtle   != null, "Squirtle.asset exists");
            Check(wartortle  != null, "Wartortle_Vanguard.asset exists");
            Check(branch     != null, "squirtle_vanguard branch exists");
            Check(squirtle   != null && squirtle.Branches.Count == 1,  "Squirtle has 1 branch");
            Check(branch     != null && branch.EvolvedSpecies == wartortle, "Branch evolves → Wartortle_Vanguard");
            Check(branch     != null && branch.MoveUpgrades.Count == 2, "Squirtle→Wartortle has 2 upgrades (SkullBash + AquaJet)");

            // ── Wartortle → Blastoise sub-branch wiring ──────────────────────
            Check(wartortle  != null && wartortle.Branches.Count == 2, "Wartortle_Vanguard has 2 sub-branches (VA1, VA2)");

            // ── Bulbasaur learnset ───────────────────────────────────────────
            var bulbasaur = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{root}/Species/Starters/Bulbasaur/Bulbasaur.asset");
            Check(bulbasaur  != null && bulbasaur.BaseLearnset.Count == 4, "Bulbasaur has 4 base moves");
            Check(bulbasaur  != null && bulbasaur.MasteryMove != null,  "Bulbasaur has a Mastery Move");
            // Per §5.12.3 (CL-008) — PrimaryAbility is legacy; VS species use AvailableAbilities.
            Check(bulbasaur  != null && bulbasaur.PrimaryAbility == null,   "Bulbasaur PrimaryAbility == null (CL-008 legacy field)");

            var ivysaur = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{root}/Species/Starters/Bulbasaur/Ivysaur_Vanguard.asset");
            // CL-008: no auto-granted ability; abilities are taught via Dojo (§7.14 / §5.12.3).
            Check(ivysaur    != null && ivysaur.PrimaryAbility == null,     "Ivysaur PrimaryAbility == null (CL-008 Dojo model)");

            // ── Move effects as sub-assets ───────────────────────────────────
            var leechSeed = AssetDatabase.LoadAssetAtPath<MoveSO>($"{root}/Moves/Bulbasaur/leech_seed.asset");
            Check(leechSeed  != null && leechSeed.Effects.Count == 1, "Leech Seed has 1 effect");
            Check(leechSeed  != null && leechSeed.Effects.Count > 0 &&
                  leechSeed.Effects[0] is StatusRiderEffectSO, "Leech Seed effect is StatusRider");

            var aquaRing = AssetDatabase.LoadAssetAtPath<MoveSO>($"{root}/Moves/Squirtle/aqua_ring.asset");
            Check(aquaRing   != null && aquaRing.Effects.Count == 1,  "Aqua Ring has 1 heal effect");
            var healFx = aquaRing?.Effects[0] as HealEffectSO;
            Check(healFx     != null && healFx.DurationTurns == 3,    "Aqua Ring regen DurationTurns == 3");

            // ── Growth curves ────────────────────────────────────────────────
            var geodudeCrv = AssetDatabase.LoadAssetAtPath<StatGrowthCurveSO>($"{root}/GrowthCurves/Geodude_Line.asset");
            Check(geodudeCrv != null && geodudeCrv.AttackGrowthPerLevel.Length == 50, "Geodude curve has 50 atk entries");
            Check(geodudeCrv != null && geodudeCrv.AttackGrowthPerLevel[0] == 3,      "Geodude Atk growth == 3/level");

            // ── Abilities ────────────────────────────────────────────────────
            var sturdy = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{root}/Abilities/Geodude/sturdy.asset");
            Check(sturdy     != null && sturdy.Category == AbilityCategory.Survival, "Sturdy category == Survival");

            // ── Wild species biomes ──────────────────────────────────────────
            var caterpie = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{root}/Species/Wild/Caterpie/Caterpie.asset");
            Check(caterpie   != null && caterpie.SpawnBiomes.Count > 0, "Caterpie has spawn biomes");

            // ── 3.3.C — Consumables ──────────────────────────────────────────
            var potion    = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/potion.asset");
            var superPot  = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/super_potion.asset");
            var antidote  = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/antidote.asset");
            var pokeball  = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/pokeball.asset");
            var xAttack   = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/x_attack.asset");
            var ether     = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/ether.asset");
            var fullHeal  = AssetDatabase.LoadAssetAtPath<ConsumableSO>($"{root}/Consumables/full_heal.asset");

            Check(potion   != null && potion.APCost == 1,                   "Potion exists, APCost==1");
            Check(potion   != null && potion.Tier   == 1,                   "Potion Tier==1");
            Check(potion   != null && potion.UpgradeTo == superPot,         "Potion.UpgradeTo == Super Potion");
            Check(superPot != null && superPot.Tier == 2,                   "Super Potion Tier==2");
            Check(potion   != null && potion.Effect is HealConsumableEffectSO,    "Potion effect is HealConsumableEffect");
            var potHeal = potion?.Effect as HealConsumableEffectSO;
            Check(potHeal  != null && potHeal.FlatHealAmount == 20,         "Potion heals 20 HP flat");
            var spHeal = superPot?.Effect as HealConsumableEffectSO;
            Check(spHeal   != null && spHeal.FlatHealAmount == 50,          "Super Potion heals 50 HP flat");
            Check(antidote != null && antidote.Effect is StatusCureConsumableEffectSO, "Antidote effect is StatusCure");
            var antFx = antidote?.Effect as StatusCureConsumableEffectSO;
            Check(antFx    != null && antFx.CuresStatus == StatusCondition.Poison, "Antidote cures Poison");
            Check(fullHeal != null && (fullHeal.Effect as StatusCureConsumableEffectSO)?.CureAll == true, "Full Heal CureAll==true");
            var catchFx = pokeball?.Effect as CatchConsumableEffectSO;
            Check(catchFx  != null && catchFx.CatchThresholdPercent == 0.30f,    "Pokéball threshold==0.30");
            Check(catchFx  != null && catchFx.StatusCatchBonusPercent == 0.20f,  "Pokéball status bonus==0.20");
            var statFx = xAttack?.Effect as StatBoostConsumableEffectSO;
            Check(statFx   != null && statFx.TargetStat == Stat.Attack,     "X Attack boosts Attack");
            var apFx = ether?.Effect as APGrantConsumableEffectSO;
            Check(apFx     != null && apFx.APGranted == 2,                  "Ether grants 2 AP");

            // ── 3.3.D — Relics ───────────────────────────────────────────────
            var barrierCharm= AssetDatabase.LoadAssetAtPath<RelicSO>($"{root}/Relics/barrier_charm.asset");
            var traumaSalve= AssetDatabase.LoadAssetAtPath<RelicSO>($"{root}/Relics/trauma_salve.asset");
            var choiceBand = AssetDatabase.LoadAssetAtPath<RelicSO>($"{root}/Relics/choice_band.asset");

            Check(barrierCharm != null && barrierCharm.Rarity == RarityTier.Common,  "Barrier Charm is Common");
            Check(barrierCharm != null && barrierCharm.Categories.Count > 0,         "Barrier Charm has SynergyCategories");
            Check(traumaSalve != null && traumaSalve.Rarity == RarityTier.Uncommon, "Trauma Salve is Uncommon");
            Check(choiceBand  != null && choiceBand.Rarity  == RarityTier.Uncommon, "Choice Band is Uncommon");

            // ── 3.3.E — Held Items ───────────────────────────────────────────
            var charcoal   = AssetDatabase.LoadAssetAtPath<HeldItemSO>($"{root}/HeldItems/charcoal.asset");
            var leftovers  = AssetDatabase.LoadAssetAtPath<HeldItemSO>($"{root}/HeldItems/leftovers.asset");

            Check(charcoal  != null && charcoal.GrantsLeadAura,             "Charcoal GrantsLeadAura==true");
            Check(charcoal  != null && charcoal.LeadAuraType == PokemonType.Fire, "Charcoal LeadAuraType==Fire");
            Check(leftovers != null && !leftovers.GrantsLeadAura,           "Leftovers GrantsLeadAura==false");

            // ── 3.3.F — TMs ──────────────────────────────────────────────────
            var tm05 = AssetDatabase.LoadAssetAtPath<TMSO>($"{root}/TMs/TM05_Surf.asset");
            var tm11 = AssetDatabase.LoadAssetAtPath<TMSO>($"{root}/TMs/TM11_BodySlam.asset");
            var tm15 = AssetDatabase.LoadAssetAtPath<TMSO>($"{root}/TMs/TM15_Foresight.asset");

            Check(tm05 != null && tm05.MoveTeach != null,                   "TM05 Surf has MoveTeach");
            Check(tm05 != null && tm05.CompatibleSpecies.Count > 0,         "TM05 has compatible species");
            Check(tm11 != null && tm11.MoveTeach != null,                   "TM11 Body Slam has MoveTeach");
            var bsMove = tm11?.MoveTeach;
            Check(bsMove != null && bsMove.Effects.Count == 1,              "Body Slam has 1 effect (Paralysis rider)");
            Check(bsMove != null && bsMove.Effects.Count > 0 &&
                  bsMove.Effects[0] is StatusRiderEffectSO,                 "Body Slam effect is StatusRider");
            Check(tm15 != null && tm15.MoveTeach != null,                   "TM15 Foresight has MoveTeach");

            // ── 3.3.G — World Content ─────────────────────────────────────────
            var meadow      = AssetDatabase.LoadAssetAtPath<BiomeSO>($"{root}/Biomes/meadow.asset");
            var cave        = AssetDatabase.LoadAssetAtPath<BiomeSO>($"{root}/Biomes/cave.asset");
            var river       = AssetDatabase.LoadAssetAtPath<BiomeSO>($"{root}/Biomes/river.asset");
            var mystStone   = AssetDatabase.LoadAssetAtPath<MysteryEventSO>($"{root}/MysteryEvents/mysterious_stone.asset");
            var slotBooth   = AssetDatabase.LoadAssetAtPath<MysteryEventSO>($"{root}/MysteryEvents/slot_booth.asset");
            var bugCatcher  = AssetDatabase.LoadAssetAtPath<TrainerArchetypeSO>($"{root}/TrainerArchetypes/bug_catcher.asset");
            var hiker       = AssetDatabase.LoadAssetAtPath<TrainerArchetypeSO>($"{root}/TrainerArchetypes/hiker.asset");
            var ironWill    = AssetDatabase.LoadAssetAtPath<DifficultyModifierSO>($"{root}/DifficultyModifiers/iron_will.asset");
            var denseFog    = AssetDatabase.LoadAssetAtPath<DifficultyModifierSO>($"{root}/DifficultyModifiers/dense_fog.asset");
            var onePath     = AssetDatabase.LoadAssetAtPath<DifficultyModifierSO>($"{root}/DifficultyModifiers/one_path.asset");
            var boulder     = AssetDatabase.LoadAssetAtPath<BadgeSO>($"{root}/Badges/boulder_badge.asset");
            var battleCfg   = AssetDatabase.LoadAssetAtPath<BattleConfigSO>($"{root}/Configs/BattleConfig.asset");
            var econCfg     = AssetDatabase.LoadAssetAtPath<EconomyConfigSO>($"{root}/Configs/EconomyConfig.asset");
            var mapCfg      = AssetDatabase.LoadAssetAtPath<MapGenerationConfigSO>($"{root}/Configs/MapGenerationConfig.asset");

            Check(meadow     != null && meadow.SpeciesPool.Count >= 3,       "Meadow biome has ≥3 species");
            Check(cave       != null && cave.BiomeType  == Biome.Cave,       "Cave biome type == Cave");
            Check(river      != null && river.BiomeType == Biome.River,      "River biome type == River");
            Check(mystStone  != null && mystStone.Choices.Count == 2,        "Mysterious Stone has 2 choices");
            Check(slotBooth  != null && slotBooth.Choices.Count == 2,        "Slot Booth has 2 choices");
            Check(bugCatcher != null && bugCatcher.Composition.Count == 2,   "Bug Catcher has 2 Pokémon");
            Check(hiker      != null && hiker.BasePokeDollarReward == 175,   "Hiker reward == 175 PD");
            Check(ironWill   != null && ironWill.EnemyStatMultiplier == 1.25f,"Iron Will EnemyStatMult==1.25");
            Check(denseFog   != null && denseFog.HideAllEnemyIntents,        "Dense Fog HideAllEnemyIntents==true");
            Check(onePath    != null && onePath.MaxRouteBranchChoices == 1,  "One Path MaxBranches==1");
            Check(boulder    != null && boulder.GymSource == "R1-Gym1",      "Boulder Badge GymSource==R1-Gym1");
            Check(battleCfg  != null && battleCfg.BaseAPPerTurn == 3,        "BattleConfig BaseAP==3");
            Check(econCfg    != null && econCfg.LevelUpThresholds.Length == 20, "EconomyConfig has 20 XP thresholds");
            Check(mapCfg     != null && mapCfg.LayerCount == 7,              "MapGenerationConfig LayerCount==7");
            Check(mapCfg     != null && mapCfg.LayerWeights.Count == 7,      "MapGenerationConfig has 7 layer entries");
            var lastLayer = mapCfg?.LayerWeights[6];
            Check(lastLayer.HasValue && lastLayer.Value.GymWeight == 1f,     "MapConfig layer 6 GymWeight==1");

            Debug.Log($"[VS_Verifier] {pass} passed, {fail} failed.");
        }
    }
}
#endif
