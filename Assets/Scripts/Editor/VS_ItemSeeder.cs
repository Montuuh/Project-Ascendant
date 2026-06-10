// Per Epic 3.3.C–G — VS Item & World Content Seeder.
// Authors:
//   3.3.C  10 Consumables  (Potion, Super Potion, Antidote, Burn Heal, Awakening,
//                          Paralyze Heal, Full Heal, Ether, X Attack, Pokéball)
//   3.3.D  15 Relics       (10 Common + 5 Uncommon; hook stubs — wired in Epic 4)
//   3.3.E   5 Held Items   (Charcoal, Mystic Water, Magnet, Miracle Seed, Leftovers)
//   3.3.F   3 TMs          (TM05 Surf, TM11 Body Slam, TM15 Foresight)
//   3.3.G  World content   (3 Biomes, 4 Mystery Events, 4 Trainer Archetypes,
//                           3 Difficulty Modifiers, 4 Badges,
//                           BattleConfigSO, EconomyConfigSO, MapGenerationConfigSO)
// Menu: Project Ascendant / Seed VS Items
// Idempotent — safe to re-run; existing assets are deleted and recreated.
// Run AFTER VS_ContentSeeder (species + moves must exist for cross-references).

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    public static class VS_ItemSeeder
    {
        const string ROOT = "Assets/ScriptableObjects/VS";

        // ── Entry point ───────────────────────────────────────────────────────

        [MenuItem("Project Ascendant/Seed VS Items")]
        public static void SeedAll()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                EnsureFolders();

                var cons = SeedConsumables();          // 3.3.C
                var rels = SeedRelics();               // 3.3.D
                SeedHeldItems();                       // 3.3.E
                SeedTMs();                             // 3.3.F
                SeedWorldContent(cons, rels);          // 3.3.G
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log("[VS_ItemSeeder] Done — 3.3.C–G authored under " + ROOT);
        }

        // ── Folder setup ──────────────────────────────────────────────────────

        static void EnsureFolders()
        {
            MkDir($"{ROOT}/Consumables");
            MkDir($"{ROOT}/Relics");
            MkDir($"{ROOT}/HeldItems");
            MkDir($"{ROOT}/TMs");
            MkDir($"{ROOT}/Moves/TM");
            MkDir($"{ROOT}/Biomes");
            MkDir($"{ROOT}/MysteryEvents");
            MkDir($"{ROOT}/TrainerArchetypes");
            MkDir($"{ROOT}/DifficultyModifiers");
            MkDir($"{ROOT}/Badges");
            MkDir($"{ROOT}/Configs");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3.3.C — CONSUMABLES
        // Per §8.2 + §3.5 — consumables restore at combat end (not expendable).
        // Potion → Super Potion upgrade chain (§8.2.1).
        // ══════════════════════════════════════════════════════════════════════

        static Dictionary<string, ConsumableSO> SeedConsumables()
        {
            var d = new Dictionary<string, ConsumableSO>();
            string p = $"{ROOT}/Consumables";

            // Heal items
            var potion   = Cs(p,"potion",        "Potion",         apCost:1, tier:1, "§8.2.1");
            ConsHeal(potion, flatHeal:20);

            var superPot = Cs(p,"super_potion",  "Super Potion",   apCost:1, tier:2, "§8.2.1");
            ConsHeal(superPot, flatHeal:50);

            // Wire upgrade chain
            potion.UpgradeTo = superPot;
            EditorUtility.SetDirty(potion);

            // Status cures — Per §8.2.3
            var antidote = Cs(p,"antidote",      "Antidote",       apCost:0, tier:1, "§8.2.3");
            ConsStatus(antidote, StatusCondition.Poison);

            var burnHeal = Cs(p,"burn_heal",     "Burn Heal",      apCost:0, tier:1, "§8.2.3");
            ConsStatus(burnHeal, StatusCondition.Burn);

            var awakening= Cs(p,"awakening",     "Awakening",      apCost:0, tier:1, "§8.2.3");
            ConsStatus(awakening, StatusCondition.Sleep);

            var parHeal  = Cs(p,"paralyze_heal", "Paralyze Heal",  apCost:0, tier:1, "§8.2.3");
            ConsStatus(parHeal, StatusCondition.Paralysis);

            var fullHeal = Cs(p,"full_heal",     "Full Heal",      apCost:0, tier:1, "§8.2.3");
            ConsStatus(fullHeal, StatusCondition.None, cureAll:true);

            // AP restore — Per §8.2.4
            var ether    = Cs(p,"ether",         "Ether",          apCost:0, tier:1, "§8.2.4");
            ConsAP(ether, apGranted:2);

            // Stat boost — Per §8.2.4
            var xAttack  = Cs(p,"x_attack",      "X Attack",       apCost:1, tier:1, "§8.2.4");
            ConsStat(xAttack, Stat.Attack, stages:1);

            // Catch mechanic — Per §8.2.5 + §7.3.4
            // Catch succeeds when target HP < 50% OR target has any status condition.
            var pokeball = Cs(p,"pokeball",      "Pokéball",       apCost:1, tier:1, "§8.2.5");
            ConsCatch(pokeball, threshold:0.30f, statusBonus:0.20f); // §7.3.4 (CL-014) basic ball

            // Field clear — Per §4.3.8.6 (CL-012). Clears the active field (any class incl. Home Field).
            var defog    = Cs(p,"defog",         "Defog",          apCost:1, tier:1, "§4.3.8.6");
            ConsClearField(defog);

            d["potion"]=potion;           d["super_potion"]=superPot;
            d["antidote"]=antidote;       d["burn_heal"]=burnHeal;
            d["awakening"]=awakening;     d["paralyze_heal"]=parHeal;
            d["full_heal"]=fullHeal;      d["ether"]=ether;
            d["x_attack"]=xAttack;        d["pokeball"]=pokeball;
            d["defog"]=defog;

            // Epic 13 — player-facing tooltip text (consumable §refs carry no effect prose).
            potion.EffectDescription   = "Restore HP to one Pokémon.";
            superPot.EffectDescription = "Restore a large amount of HP to one Pokémon.";
            antidote.EffectDescription = "Cure Poison from one Pokémon.";
            burnHeal.EffectDescription = "Cure Burn from one Pokémon.";
            awakening.EffectDescription= "Wake one sleeping Pokémon.";
            parHeal.EffectDescription  = "Cure Paralysis from one Pokémon.";
            fullHeal.EffectDescription = "Cure all status conditions from one Pokémon.";
            ether.EffectDescription    = "Restore +2 AP this turn.";
            xAttack.EffectDescription  = "Raise one Pokémon's Attack by 1 stage.";
            pokeball.EffectDescription = "Catch a wild Pokémon at 30% HP or below (50% if it has a status).";
            defog.EffectDescription    = "Clear the active field effect (weather, terrain, hazard, or Home Field).";
            return d;
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3.3.D — RELICS
        // Per §8.3 — persistent run-state modifiers.
        // Per §8.3.2 — each has 1-2 SynergyCategories for City Shop curation.
        // EventHooks empty — wired by HookSubscriber in Epic 4.
        // GDDReference carries effect description as designer context.
        // ══════════════════════════════════════════════════════════════════════

        static Dictionary<string, RelicSO> SeedRelics()
        {
            var d = new Dictionary<string, RelicSO>();
            string p = $"{ROOT}/Relics";

            // ── Common Relics (x10) ──────────────────────────────────────────

            // Barrier Charm: once per combat, the first enemy attack deals −20% damage.
            d["barrier_charm"]   = Rl(p,"barrier_charm",  "Barrier Charm",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.Combat},
                "§8.3.3 | Effect: First combat per Region (VS: per combat): take −20% damage from the first enemy attack.");

            // Quick Claw Charm: draw +1 skill card on turn 1 of each combat.
            d["quick_claw_charm"]= Rl(p,"quick_claw_charm","Quick Claw Charm",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.CardEconomy},
                "§8.3 | Effect: Draw +1 skill card on turn 1 of combat.");

            // Berry Pouch: healing consumables restore +5 additional HP.
            d["berry_pouch"]     = Rl(p,"berry_pouch",    "Berry Pouch",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.Combat, SynergyCategory.CardEconomy},
                "§8.3 | Effect: Healing consumables restore +5 additional HP.");

            // Soothe Bell: status utility moves (role=Utility) cost 0 AP this turn.
            d["soothe_bell"]     = Rl(p,"soothe_bell",    "Soothe Bell",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.Status, SynergyCategory.LeadEconomy},
                "§8.3 | Effect: Once per turn, the first Utility move played costs 0 AP.");

            // Coin Pouch: +50 PD per trainer/gym victory.
            d["coin_pouch"]      = Rl(p,"coin_pouch",     "Coin Pouch",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.MetaAcquisition},
                "§8.3 | Effect: Gain +50 Poké Dollars per Trainer or Gym victory.");

            // Lucky Egg Token: XP gain +20% from all sources this run.
            d["lucky_egg_token"] = Rl(p,"lucky_egg_token","Lucky Egg Token",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.MetaAcquisition},
                "§8.3 | Effect: XP gain +20% from all sources this run.");

            // Exp Share: bench Pokémon gain 50% of Lead's combat XP.
            d["exp_share"]       = Rl(p,"exp_share",      "Exp Share",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.MetaAcquisition},
                "§8.3 | Effect: All non-Lead Pokémon gain 50% of the Lead's earned XP per combat.");

            // Defense Curl Charm: playing a Defense-buffing move restores 2 HP to the caster.
            d["defense_curl_charm"]=Rl(p,"defense_curl_charm","Defense Curl Charm",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.Combat},
                "§8.3 | Effect: Whenever a move grants +Defense stage, restore 2 HP to the user.");

            // Quick Draw: drawing a Utility card draws +1 additional card (once per turn).
            d["quick_draw"]      = Rl(p,"quick_draw",     "Quick Draw",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.CardEconomy},
                "§8.3 | Effect: Once per turn, drawing a Utility card triggers draw +1 more card.");

            // Brave Charm: Lead deals +5% damage below 50% HP.
            d["brave_charm"]     = Rl(p,"brave_charm",    "Brave Charm",
                RarityTier.Common, metaTier:1,
                new[]{SynergyCategory.Combat, SynergyCategory.LeadEconomy},
                "§8.3 | Effect: Lead deals +5% damage while below 50% HP.");

            // ── Uncommon Relics (x5) ─────────────────────────────────────────

            // Choice Specs: Ranged moves deal +15% damage; Melee moves cost +1 AP.
            // Mapped to Ranged/Melee split (no SpAtk/SpDef in this game — §4.1.1).
            d["choice_specs"]    = Rl(p,"choice_specs",   "Choice Specs",
                RarityTier.Uncommon, metaTier:1,
                new[]{SynergyCategory.Combat},
                "§8.3 | Effect: Ranged moves deal +15% damage. Melee moves cost +1 AP (min 0).");

            // Choice Band: Melee moves deal +15% damage; Ranged moves cost +1 AP.
            d["choice_band"]     = Rl(p,"choice_band",    "Choice Band",
                RarityTier.Uncommon, metaTier:1,
                new[]{SynergyCategory.Combat},
                "§8.3 | Effect: Melee moves deal +15% damage. Ranged moves cost +1 AP (min 0).");

            // Move Echo: after playing a move, 20% chance to draw it again.
            d["move_echo"]       = Rl(p,"move_echo",      "Move Echo",
                RarityTier.Uncommon, metaTier:1,
                new[]{SynergyCategory.CardEconomy},
                "§8.3 | Effect: After playing a skill card, 20% chance to draw one copy back to hand.");

            // Tactician's Coin: each manual swap reduces next move's AP cost by 1 (min 0).
            d["tacticians_coin"] = Rl(p,"tacticians_coin","Tactician's Coin",
                RarityTier.Uncommon, metaTier:1,
                new[]{SynergyCategory.LeadEconomy},
                "§8.3 | Effect: Each manual swap this turn reduces next move's AP cost by 1 (min 0).");

            // Trauma Salve (§6.2.4 LOCKED): single-charge — removes ALL Trauma stacks from one CHOSEN
            // Pokémon, consumed on use. (Prior seeding said "−1 to most-traumatized at combat end" — that
            // violated the locked spec; corrected here.) The active-use flow (pick target → clear → consume
            // from HeldRelics) is Epic 12 (Relics runtime); see gap log.
            d["trauma_salve"]    = Rl(p,"trauma_salve",   "Trauma Salve",
                RarityTier.Uncommon, metaTier:1,
                new[]{SynergyCategory.Status},
                "§6.2.4 | Effect: Single-charge. Removes ALL Trauma stacks from one chosen Pokémon. Consumed on use.");

            return d;
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3.3.E — HELD ITEMS
        // Per §8.4 — one slot per Pokémon. Persists across combats.
        // Type-boosting items use GrantsLeadAura + LeadAuraType (§5.5.4).
        // Leftovers uses EventHook (wired Epic 4) for per-turn regen.
        // ══════════════════════════════════════════════════════════════════════

        static void SeedHeldItems()
        {
            string p = $"{ROOT}/HeldItems";

            // Type-boosting plates — Lead Aura gives bench allies of that type +5%.
            // Per §5.5.4, the wearer's type-move power is also boosted while Lead.
            Hi(p,"charcoal",     "Charcoal",     PokemonType.Fire,     1.20f, 0, "§8.4.2 | Wearer's Fire-type moves +20% damage.");
            Hi(p,"mystic_water", "Mystic Water", PokemonType.Water,    1.20f, 0, "§8.4.2 | Wearer's Water-type moves +20% damage.");
            Hi(p,"magnet",       "Magnet",       PokemonType.Electric, 1.20f, 0, "§8.4.2 | Wearer's Electric-type moves +20% damage.");
            Hi(p,"miracle_seed", "Miracle Seed", PokemonType.Grass,    1.20f, 0, "§8.4.2 | Wearer's Grass-type moves +20% damage.");

            // Leftovers — hook-driven per-turn regen: floor(EffectiveMaxHP / 16).
            // EventHook wired to TurnEnd channel in Epic 4.
            Hi(p,"leftovers",    "Leftovers",    PokemonType.Normal,   1f, 16, "§8.4.4 | At end of Resolution, restore floor(EffectiveMaxHP/16) HP to the wearer.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3.3.F — TMs
        // Per §5.4.1 + §8.5 — applied from Map View. Mastery slot exempt (§4.3.9.2).
        // TM05 Surf reuses the existing surf.asset from VS_ContentSeeder.
        // TM11 Body Slam and TM15 Foresight are new moves authored here.
        // ══════════════════════════════════════════════════════════════════════

        static void SeedTMs()
        {
            string ptm  = $"{ROOT}/TMs";
            string pmtm = $"{ROOT}/Moves/TM";
            string pSq  = $"{ROOT}/Species/Starters";
            string pWl  = $"{ROOT}/Species/Wild";

            // ── New TM-exclusive moves ────────────────────────────────────────

            // Body Slam — Normal/Melee, AP 2, Power 85, 20% Paralysis.
            // Compatible: Squirtle line, Geodude line, Charmander line.
            var bodySlam = CreateSO<MoveSO>($"{pmtm}/body_slam.asset");
            bodySlam.MoveId      = "body_slam";
            bodySlam.DisplayName = "Body Slam";
            bodySlam.Type        = PokemonType.Normal;
            bodySlam.Role        = MoveRole.Offensive;
            bodySlam.Range       = MoveRange.Melee;
            bodySlam.Modifier    = PositionalModifier.None;
            bodySlam.APCost      = 2;
            bodySlam.BasePower   = 85;
            bodySlam.RangeModifierMultiplier = 1f;
            bodySlam.GDDReference = "§5.4.1 | TM11 Body Slam";
            bodySlam.Effects     = new List<MoveEffectSO>();
            var bsFx = ScriptableObject.CreateInstance<StatusRiderEffectSO>();
            bsFx.StatusToApply     = StatusCondition.Paralysis;
            bsFx.ApplicationChance = 0.2f;
            bsFx.name = "body_slam_ParalysisRider";
            AssetDatabase.AddObjectToAsset(bsFx, bodySlam);
            bodySlam.Effects.Add(bsFx);
            EditorUtility.SetDirty(bodySlam);

            // Foresight — Normal/Utility, AP 0, Power 0, strips 1 enemy Defense stage.
            // In deckbuilder context: removes opponent's Def boost, enabling cleaner damage reads.
            var foresight = CreateSO<MoveSO>($"{pmtm}/foresight.asset");
            foresight.MoveId      = "foresight";
            foresight.DisplayName = "Foresight";
            foresight.Type        = PokemonType.Normal;
            foresight.Role        = MoveRole.Utility;
            foresight.Range       = MoveRange.Ranged;
            foresight.Modifier    = PositionalModifier.None;
            foresight.APCost      = 0;
            foresight.BasePower   = 0;
            foresight.RangeModifierMultiplier = 0.75f;
            foresight.GDDReference = "§5.4.1 | TM15 Foresight — strips 1 enemy Defense stage.";
            foresight.Effects     = new List<MoveEffectSO>();
            var fsFx = ScriptableObject.CreateInstance<DebuffTargetEffectSO>();
            fsFx.TargetStat  = Stat.Defense;
            fsFx.StageChange = -1;
            fsFx.name = "foresight_DebuffDef";
            AssetDatabase.AddObjectToAsset(fsFx, foresight);
            foresight.Effects.Add(fsFx);
            EditorUtility.SetDirty(foresight);

            // ── Load existing surf move from VS_ContentSeeder ─────────────────
            var surfMove = AssetDatabase.LoadAssetAtPath<MoveSO>($"{ROOT}/Moves/Squirtle/surf.asset");

            // ── Load species for CompatibleSpecies lists ─────────────────────
            PokemonSpeciesSO SpSt(string n) =>
                AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{pSq}/{n}.asset");
            PokemonSpeciesSO SpWl(string n) =>
                AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{pWl}/{n}.asset");

            // TM05 Surf — Water/Ranged; teachable to non-Water lines for type diversity.
            var tm05 = CreateSO<TMSO>($"{ptm}/TM05_Surf.asset");
            tm05.Id          = "TM05";
            tm05.DisplayName = "TM05 — Surf";
            tm05.MoveTeach   = surfMove;
            tm05.ShopPrice   = 300;
            tm05.GDDReference = "§5.4.1 | TM05 Surf — Water/Ranged 70P. Teachable to Grass + Rock lines.";
            tm05.CompatibleSpecies = new List<PokemonSpeciesSO>
            {
                SpSt("Bulbasaur"), SpSt("Ivysaur_Vanguard"),
                SpSt("Venusaur_VanguardA1"), SpSt("Venusaur_VanguardA2"),
                SpWl("Geodude"), SpWl("Graveler"), SpWl("Golem"),
            };
            tm05.CompatibleSpecies.RemoveAll(x => x == null);
            EditorUtility.SetDirty(tm05);

            // TM11 Body Slam — Normal/Melee; teachable to physical attackers.
            var tm11 = CreateSO<TMSO>($"{ptm}/TM11_BodySlam.asset");
            tm11.Id          = "TM11";
            tm11.DisplayName = "TM11 — Body Slam";
            tm11.MoveTeach   = bodySlam;
            tm11.ShopPrice   = 250;
            tm11.GDDReference = "§5.4.1 | TM11 Body Slam — Normal/Melee 85P, 20% Paralysis.";
            tm11.CompatibleSpecies = new List<PokemonSpeciesSO>
            {
                SpSt("Squirtle"), SpSt("Wartortle_Vanguard"),
                SpSt("Blastoise_VanguardA1"), SpSt("Blastoise_VanguardA2"),
                SpSt("Charmander"), SpSt("Charmeleon_Vanguard"),
                SpWl("Geodude"), SpWl("Graveler"), SpWl("Golem"),
                SpWl("Pidgey"), SpWl("Pidgeotto"), SpWl("Pidgeot"),
            };
            tm11.CompatibleSpecies.RemoveAll(x => x == null);
            EditorUtility.SetDirty(tm11);

            // TM15 Foresight — Normal/Utility; teachable to support and utility-heavy lines.
            var tm15 = CreateSO<TMSO>($"{ptm}/TM15_Foresight.asset");
            tm15.Id          = "TM15";
            tm15.DisplayName = "TM15 — Foresight";
            tm15.MoveTeach   = foresight;
            tm15.ShopPrice   = 150;
            tm15.GDDReference = "§5.4.1 | TM15 Foresight — strips 1 enemy Def stage. Support/utility.";
            tm15.CompatibleSpecies = new List<PokemonSpeciesSO>
            {
                SpSt("Charmander"), SpSt("Charmeleon_Vanguard"),
                SpSt("Charizard_VanguardA1"), SpSt("Charizard_VanguardA2"),
                SpWl("Pidgey"), SpWl("Pidgeotto"), SpWl("Pidgeot"),
                SpWl("Caterpie"), SpWl("Metapod"), SpWl("Butterfree"),
            };
            tm15.CompatibleSpecies.RemoveAll(x => x == null);
            EditorUtility.SetDirty(tm15);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3.3.G — WORLD CONTENT
        // Biomes, Mystery Events, Trainer Archetypes, Difficulty Modifiers,
        // Badges, BattleConfigSO, EconomyConfigSO, MapGenerationConfigSO.
        // ══════════════════════════════════════════════════════════════════════

        static void SeedWorldContent(
            Dictionary<string, ConsumableSO> cons,
            Dictionary<string, RelicSO> rels)
        {
            string pSt = $"{ROOT}/Species/Starters";
            string pWl = $"{ROOT}/Species/Wild";
            PokemonSpeciesSO SpSt(string n) =>
                AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{pSt}/{n}.asset");
            PokemonSpeciesSO SpWl(string n) =>
                AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>($"{pWl}/{n}.asset");

            // ── Biomes ────────────────────────────────────────────────────────
            // Per §3.1.13 + §3.3.16 — VS biomes: Meadow, Cave, River.
            string pb = $"{ROOT}/Biomes";

            Bm(pb,"meadow","Meadow",Biome.Meadow,
                new[]{ SpSt("Bulbasaur"), SpWl("Caterpie"), SpWl("Pidgey") },
                "§3.1.13 | Open grass area. Starter region for Run start.");

            Bm(pb,"cave","Cave",Biome.Cave,
                new[]{ SpSt("Charmander"), SpWl("Geodude") },
                "§3.1.13 | Dark rocky cavern. Fire and Rock types dominate.");

            Bm(pb,"river","River",Biome.River,
                new[]{ SpSt("Squirtle") },
                "§3.1.13 | Flowing water route. Water types common.");

            // ── Mystery Events ────────────────────────────────────────────────
            // Per §3.1.14 + §3.3.17 — 4 VS events.
            string pm = $"{ROOT}/MysteryEvents";

            Me(pm,"mysterious_stone","Mysterious Stone",
                "A strangely glowing stone sits at the side of the path. It pulses with faint energy.",
                new MysteryChoice[]
                {
                    new MysteryChoice
                    {
                        ChoiceText    = "Inspect it carefully",
                        OutcomeText   = "The stone crumbles in your hands — inside is a shimmering item!",
                        OutcomeMechanicsNote = "Award a random Common or Uncommon Relic from the run pool."
                    },
                    new MysteryChoice
                    {
                        ChoiceText    = "Leave it be",
                        OutcomeText   = "You step around it. Better not take chances.",
                        OutcomeMechanicsNote = "No effect."
                    }
                },
                "§3.1.14 | Relic reward or nothing.");

            Me(pm,"berry_bush","Berry Bush",
                "A wild berry bush bursts with ripe berries — but thorny vines block the path.",
                new MysteryChoice[]
                {
                    new MysteryChoice
                    {
                        ChoiceText    = "Harvest the berries",
                        OutcomeText   = "You gather a handful of sweet berries. Your Pokémon look refreshed!",
                        OutcomeMechanicsNote = "Restore 15 HP to all active party members."
                    },
                    new MysteryChoice
                    {
                        ChoiceText    = "Push through quickly",
                        OutcomeText   = "The thorns scratch your Lead Pokémon on the way through.",
                        OutcomeMechanicsNote = "Lead Pokémon loses 5 HP (non-faint minimum: 1 HP)."
                    }
                },
                "§3.1.14 | Heal all or take minor damage.");

            Me(pm,"wandering_tutor","Wandering Tutor",
                "An elderly trainer sits on a stump, mumbling ancient move techniques.",
                new MysteryChoice[]
                {
                    new MysteryChoice
                    {
                        ChoiceText    = "Ask to be taught",
                        OutcomeText   = "The tutor demonstrates a technique. One of your Pokémon learns a new move!",
                        OutcomeMechanicsNote = "Open Tutor Learnset UI for one Pokémon. Player picks one move to add (if any tutor moves available)."
                    },
                    new MysteryChoice
                    {
                        ChoiceText    = "Decline politely",
                        OutcomeText   = "\"Come back when you're ready,\" the tutor nods.",
                        OutcomeMechanicsNote = "No effect."
                    }
                },
                "§3.1.14 | Tutor move teach or skip.");

            Me(pm,"slot_booth","Slot Booth",
                "An ancient mechanical booth blocks the mountain path. Its lever looks well-worn.",
                new MysteryChoice[]
                {
                    new MysteryChoice
                    {
                        ChoiceText    = "Pull the lever (costs 50 PD)",
                        OutcomeText   = "The machine whirs and clanks... something falls out!",
                        OutcomeMechanicsNote = "Spend 50 PD. RNG outcome (seeded): 40% Consumable, 40% PD gain (+100), 20% Relic."
                    },
                    new MysteryChoice
                    {
                        ChoiceText    = "Walk past",
                        OutcomeText   = "You squeeze around the machine. Some mysteries are best left alone.",
                        OutcomeMechanicsNote = "No effect."
                    }
                },
                "§3.1.14 | Slot machine — gamble PD for item/PD/relic.");

            // ── Trainer Archetypes ─────────────────────────────────────────────
            // Per §3.1.15 + §3.3.18 — 4 VS archetypes.
            string pt = $"{ROOT}/TrainerArchetypes";

            // Bug Catcher — Caterpie/Metapod team. Status-focused.
            Ta(pt,"bug_catcher","Bug Catcher",
                composition: new[]
                {
                    new TrainerPokemonSlot { Species=SpWl("Caterpie"), Level=5 },
                    new TrainerPokemonSlot { Species=SpWl("Metapod"),  Level=7 }
                },
                tactical: "Stalls with Harden/Silk Bind, then chips with Bug Bite. " +
                           "Prefers status moves early (String Shot) to limit AP. " +
                           "Always targets Lead slot. Uses tackle as filler.",
                pokeDollarReward: 100,
                consumableLoot: new[]{ cons["antidote"] },
                "§3.1.15 | Early-route Bug Catcher. Low threat, good XP source.");

            // Lass — Bulbasaur/Pidgey team. Support/control.
            Ta(pt,"lass","Lass",
                composition: new[]
                {
                    new TrainerPokemonSlot { Species=SpSt("Bulbasaur"), Level=8  },
                    new TrainerPokemonSlot { Species=SpWl("Pidgey"),    Level=7  }
                },
                tactical: "Disrupts with Growl + Feather Dance to shred Attack stages. " +
                           "Then Vine Whip + Gust for chip. Roost to stall. " +
                           "Targets highest-ATK slot to neutralise threats.",
                pokeDollarReward: 130,
                consumableLoot: new[]{ cons["potion"], cons["antidote"] },
                "§3.1.15 | Control Lass. Debuff heavy, Roost stall.");

            // Hiker — Geodude/Graveler team. Vanguard/aggressive.
            Ta(pt,"hiker","Hiker",
                composition: new[]
                {
                    new TrainerPokemonSlot { Species=SpWl("Geodude"),  Level=10 },
                    new TrainerPokemonSlot { Species=SpWl("Graveler"), Level=12 }
                },
                tactical: "Opens with Rock Throw to poke, then escalates to Earthquake. " +
                           "Defense Curl on high-HP turns to tank. " +
                           "Rollout StepForward when Lead HP > 50%. Stealth Rock when HP < 50%.",
                pokeDollarReward: 175,
                consumableLoot: new[]{ cons["potion"], cons["burn_heal"] },
                "§3.1.15 | Mid-route Hiker. High Def tank, Rock/Ground damage.");

            // Sailor — Squirtle/Wartortle team. Defensive/attrition.
            Ta(pt,"sailor","Sailor",
                composition: new[]
                {
                    new TrainerPokemonSlot { Species=SpSt("Squirtle"),        Level=9  },
                    new TrainerPokemonSlot { Species=SpSt("Wartortle_Vanguard"),Level=11}
                },
                tactical: "Opens Withdraw for +Def, then Water Gun. " +
                           "Wartortle escalates with Skull Bash (StepBackward) to tank. " +
                           "Aqua Jet as finisher when enemy HP < 30%. Tail Whip to -Def first.",
                pokeDollarReward: 160,
                consumableLoot: new[]{ cons["super_potion"] },
                "§3.1.15 | Sailor. Defensive attrition, strong midgame.");

            // ── Difficulty Modifiers ──────────────────────────────────────────
            // Per §6.8 + §3.1.17 — VS modifiers: Iron Will, Dense Fog, One Path.
            string pdiff = $"{ROOT}/DifficultyModifiers";

            Dm(pdiff,"iron_will","Iron Will",
                "Enemies are tougher, and trauma hits harder. Only the strongest survive.",
                enemyMult: 1.25f, traumaMult: 2.0f, hideIntents: false, maxBranches: 3,
                "§6.8 | +25% enemy stats, ×2 Trauma per faint.");

            Dm(pdiff,"dense_fog","Dense Fog",
                "Enemy intentions are shrouded. No scouting, no certainty.",
                enemyMult: 1.0f, traumaMult: 1.0f, hideIntents: true, maxBranches: 2,
                "§6.8 | All intents Unknown. Keen Eye cannot reveal. 2 route branches max.");

            Dm(pdiff,"one_path","One Path",
                "The road narrows. You cannot choose your own destiny.",
                enemyMult: 1.0f, traumaMult: 1.0f, hideIntents: false, maxBranches: 1,
                "§6.8 | Exactly 1 route branch at every junction. Forces a fixed path.");

            // ── Badges ────────────────────────────────────────────────────────
            // Per §4.4.5 — persistent run-wide passive effects. Hooks wired Epic 4.
            // VS scope: Boulder, Cascade, Hive, Normal.
            string pbd = $"{ROOT}/Badges";

            Bg(pbd,"boulder_badge","Boulder Badge","R1-Gym1",
                "All Pokémon's Defense increased by 10% (run-wide).",
                "§4.4.5 | Boulder Badge. Rock Gym R1. Def +10% run-wide.");

            Bg(pbd,"cascade_badge","Cascade Badge","R1-Gym2",
                "Water-type moves cost 1 less AP (minimum 1).",
                "§4.4.5 | Cascade Badge. Water Gym R1. Water AP cost −1.");

            Bg(pbd,"hive_badge","Hive Badge","R1-Gym3",
                "Bug- and Grass-type status moves apply their effect 100% of the time.",
                "§4.4.5 | Hive Badge. Bug Gym R1. Bug/Grass status riders always apply.");

            Bg(pbd,"normal_badge","Normal Badge","R1-Gym4",
                "Normal-type moves deal STAB damage regardless of the user's type.",
                "§4.4.5 | Normal Badge. Normal Gym R1. Universal Normal STAB.");

            // ── Config SOs ────────────────────────────────────────────────────
            // Per §4.1.1, §5.2, §9.7.2 — tuned VS numbers. All defaults from schemas
            // are validated here; non-default values explicitly noted.
            string pc = $"{ROOT}/Configs";

            SeedBattleConfig(pc);
            SeedEconomyConfig(pc);
            SeedMapGenerationConfig(pc);
        }

        // ── Config seed helpers ───────────────────────────────────────────────

        static void SeedBattleConfig(string folder)
        {
            // Per §4.1.1 — damage formula: floor(Power × (Atk/Def) × TypeMult × STAB × RangeMod × Crit / Divisor)
            var c = CreateSO<BattleConfigSO>($"{folder}/BattleConfig.asset");
            c.Divisor     = 50;      // Placeholder; tune in playtest (§3.3.22)
            c.StabMultiplier  = 1.5f;
            c.CritMultiplier  = 1.5f;
            c.RangedModifier  = 0.75f; // Per §9.3.2.2
            c.MeleeModifier   = 1.0f;
            c.BaseAPPerTurn   = 3;
            c.MaxAPPerTurn    = 6;
            c.BaseSkillCardsPerTurn      = 4;
            c.BaseConsumableCardsPerTurn = 2;
            // Stat stage table — index 0=stage−6 … index 12=stage+6
            c.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            EditorUtility.SetDirty(c);
        }

        static void SeedEconomyConfig(string folder)
        {
            // Per §5.2 + §6.2 — XP curve and Trauma economy.
            var c = CreateSO<EconomyConfigSO>($"{folder}/EconomyConfig.asset");
            // Cumulative XP to reach each level (index 0 = XP for Level 2).
            // Simple ascending curve for VS 20-level cap. Tune in playtest.
            c.LevelUpThresholds = new int[]
            {
                  100,   200,   325,   475,   650,
                  850,  1075,  1325,  1600,  1900,
                 2225,  2575,  2950,  3350,  3775,
                 4225,  4700,  5200,  5725,  6275
            };
            c.WildXPMultiplier      = 1.0f;
            c.TrainerXPMultiplier   = 1.5f;
            c.EliteXPMultiplier     = 2.0f;
            c.GymLeaderXPMultiplier = 3.0f;
            c.TokenPerXP            = 1.0f;
            c.TraumaStackPenaltyPercent = 5;   // §6.2.1 (CL-017) zone-1: −5%/stack (1–5)
            c.TraumaZone1StackCount     = 5;   // §6.2.1 (CL-017) zone-1 boundary
            c.TraumaZone2PenaltyPercent = 10;  // §6.2.1 (CL-017) zone-2: −10%/stack (6–10)
            c.TraumaStackCap            = 10;  // §6.2.1 (CL-017) soft cap → −75%
            c.TraumaStacksPerFaint      = 1;
            c.BoxCapacity               = 18;
            EditorUtility.SetDirty(c);
        }

        static void SeedMapGenerationConfig(string folder)
        {
            // Per §9.7.2 — 7-layer VS region map. Weights are relative (normalised at runtime).
            // Layer 0 = first layer after Run start. Layer 6 = pre-Gym gate.
            var c = CreateSO<MapGenerationConfigSO>($"{folder}/MapGenerationConfig.asset");
            c.LayerCount         = 7;
            c.DefaultMaxBranches = 3;
            c.LayerWeights = new List<NodeLayerWeights>
            {
                new NodeLayerWeights { Layer=0, WildWeight=5, TrainerWeight=1, CenterWeight=0, ShopWeight=0, MysteryWeight=0, GymWeight=0 },
                new NodeLayerWeights { Layer=1, WildWeight=3, TrainerWeight=2, CenterWeight=1, ShopWeight=0, MysteryWeight=0, GymWeight=0 },
                new NodeLayerWeights { Layer=2, WildWeight=2, TrainerWeight=2, CenterWeight=0, ShopWeight=1, MysteryWeight=1, GymWeight=0 },
                new NodeLayerWeights { Layer=3, WildWeight=1, TrainerWeight=3, CenterWeight=1, ShopWeight=1, MysteryWeight=1, GymWeight=0 },
                new NodeLayerWeights { Layer=4, WildWeight=1, TrainerWeight=2, CenterWeight=0, ShopWeight=1, MysteryWeight=2, GymWeight=0 },
                new NodeLayerWeights { Layer=5, WildWeight=1, TrainerWeight=3, CenterWeight=1, ShopWeight=1, MysteryWeight=1, GymWeight=0 },
                new NodeLayerWeights { Layer=6, WildWeight=0, TrainerWeight=0, CenterWeight=0, ShopWeight=0, MysteryWeight=0, GymWeight=1 },
            };
            EditorUtility.SetDirty(c);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        static T CreateSO<T>(string path) where T : ScriptableObject
        {
            AssetDatabase.DeleteAsset(path);
            var a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void MkDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string folder = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, folder);
        }

        // ── Consumable factories ──────────────────────────────────────────────

        static ConsumableSO Cs(string folder, string id, string displayName,
            int apCost, int tier, string gdd)
        {
            var c = CreateSO<ConsumableSO>($"{folder}/{id}.asset");
            c.Id          = id;
            c.DisplayName = displayName;
            c.APCost      = apCost;
            c.Tier        = tier;
            c.GDDReference= gdd;
            EditorUtility.SetDirty(c);
            return c;
        }

        static void ConsHeal(ConsumableSO c, int flatHeal, bool restoreToFull = false)
        {
            var fx = ScriptableObject.CreateInstance<HealConsumableEffectSO>();
            fx.FlatHealAmount = flatHeal;
            fx.RestoreToFull  = restoreToFull;
            fx.name = $"{c.Id}_Heal";
            AssetDatabase.AddObjectToAsset(fx, c);
            c.Effect = fx;
            EditorUtility.SetDirty(c);
        }

        static void ConsStatus(ConsumableSO c, StatusCondition status, bool cureAll = false)
        {
            var fx = ScriptableObject.CreateInstance<StatusCureConsumableEffectSO>();
            fx.CuresStatus = status;
            fx.CureAll     = cureAll;
            fx.name = $"{c.Id}_Cure";
            AssetDatabase.AddObjectToAsset(fx, c);
            c.Effect = fx;
            EditorUtility.SetDirty(c);
        }

        static void ConsStat(ConsumableSO c, Stat stat, int stages)
        {
            var fx = ScriptableObject.CreateInstance<StatBoostConsumableEffectSO>();
            fx.TargetStat  = stat;
            fx.StageChange = stages;
            fx.name = $"{c.Id}_StatBoost";
            AssetDatabase.AddObjectToAsset(fx, c);
            c.Effect = fx;
            EditorUtility.SetDirty(c);
        }

        static void ConsAP(ConsumableSO c, int apGranted)
        {
            var fx = ScriptableObject.CreateInstance<APGrantConsumableEffectSO>();
            fx.APGranted = apGranted;
            fx.name = $"{c.Id}_AP";
            AssetDatabase.AddObjectToAsset(fx, c);
            c.Effect = fx;
            EditorUtility.SetDirty(c);
        }

        static void ConsCatch(ConsumableSO c, float threshold, float statusBonus)
        {
            var fx = ScriptableObject.CreateInstance<CatchConsumableEffectSO>();
            fx.CatchThresholdPercent  = threshold;
            fx.StatusCatchBonusPercent = statusBonus;
            fx.name = $"{c.Id}_Catch";
            AssetDatabase.AddObjectToAsset(fx, c);
            c.Effect = fx;
            EditorUtility.SetDirty(c);
        }

        static void ConsClearField(ConsumableSO c)
        {
            var fx = ScriptableObject.CreateInstance<ClearFieldConsumableEffectSO>();
            fx.name = $"{c.Id}_ClearField";
            AssetDatabase.AddObjectToAsset(fx, c);
            c.Effect = fx;
            EditorUtility.SetDirty(c);
        }

        // ── Relic factory ─────────────────────────────────────────────────────

        static RelicSO Rl(string folder, string id, string displayName,
            RarityTier rarity, int metaTier, SynergyCategory[] categories, string gdd)
        {
            var r = CreateSO<RelicSO>($"{folder}/{id}.asset");
            r.Id          = id;
            r.DisplayName = displayName;
            r.Rarity      = rarity;
            r.MetaTier    = metaTier;
            r.Categories  = new List<SynergyCategory>(categories);
            r.EventHooks  = new List<HookBinding>(); // wired in Epic 4
            r.GDDReference= gdd;
            r.EffectDescription = EffectFrom(gdd); // Epic 13 — tooltip text from the seeded effect note
            EditorUtility.SetDirty(r);
            return r;
        }

        // Extracts a clean player-facing effect line from a seeded "§ref | [Effect:] text" string.
        static string EffectFrom(string gdd)
        {
            if (string.IsNullOrEmpty(gdd)) return "";
            string s = gdd;
            int bar = s.LastIndexOf('|');
            if (bar >= 0) s = s.Substring(bar + 1);
            s = s.Trim();
            if (s.StartsWith("Effect:", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Effect:".Length).Trim();
            return s;
        }

        // ── Held Item factory ─────────────────────────────────────────────────

        // §8.4.2/§8.4.4 — VS Held Items are WEARER type-boost (+20%) or Leftovers regen, NOT Lead Aura
        // (that's §8.4.3 Type Plates, post-VS). GrantsLeadAura stays false for the VS 5.
        static HeldItemSO Hi(string folder, string id, string displayName,
            PokemonType boostsType, float wearerMult, int leftoversDivisor, string gdd)
        {
            var h = CreateSO<HeldItemSO>($"{folder}/{id}.asset");
            h.Id                    = id;
            h.DisplayName           = displayName;
            h.BoostsType            = boostsType;
            h.WearerDamageMultiplier= wearerMult;
            h.LeftoversRegenDivisor = leftoversDivisor;
            h.GrantsLeadAura        = false;
            h.EventHooks            = new List<HookBinding>();
            h.GDDReference          = gdd;
            h.EffectDescription     = EffectFrom(gdd); // Epic 13 — tooltip text
            EditorUtility.SetDirty(h);
            return h;
        }

        // ── Biome factory ─────────────────────────────────────────────────────

        static BiomeSO Bm(string folder, string id, string displayName,
            Biome biomeType, PokemonSpeciesSO[] speciesPool, string gdd)
        {
            var b = CreateSO<BiomeSO>($"{folder}/{id}.asset");
            b.BiomeId     = id;
            b.DisplayName = displayName;
            b.BiomeType   = biomeType;
            var pool = new List<PokemonSpeciesSO>();
            foreach (var s in speciesPool) if (s != null) pool.Add(s);
            b.SpeciesPool = pool;
            b.GDDReference= gdd;
            EditorUtility.SetDirty(b);
            return b;
        }

        // ── Mystery Event factory ─────────────────────────────────────────────

        static MysteryEventSO Me(string folder, string id, string displayName,
            string narrative, MysteryChoice[] choices, string gdd)
        {
            var m = CreateSO<MysteryEventSO>($"{folder}/{id}.asset");
            m.EventId      = id;
            m.DisplayName  = displayName;
            m.NarrativeText= narrative;
            m.Choices      = new List<MysteryChoice>(choices);
            m.GDDReference = gdd;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ── Trainer Archetype factory ─────────────────────────────────────────

        static TrainerArchetypeSO Ta(string folder, string id, string displayName,
            TrainerPokemonSlot[] composition, string tactical,
            int pokeDollarReward, ConsumableSO[] consumableLoot, string gdd)
        {
            var t = CreateSO<TrainerArchetypeSO>($"{folder}/{id}.asset");
            t.ArchetypeId           = id;
            t.DisplayName           = displayName;
            t.Composition           = new List<TrainerPokemonSlot>(composition);
            t.TacticalIdentity      = tactical;
            t.RelicLootTable        = new List<RelicSO>();
            t.ConsumableLootTable   = new List<ConsumableSO>(consumableLoot);
            t.BasePokeDollarReward  = pokeDollarReward;
            t.GDDReference          = gdd;
            EditorUtility.SetDirty(t);
            return t;
        }

        // ── Difficulty Modifier factory ───────────────────────────────────────

        static DifficultyModifierSO Dm(string folder, string id, string displayName,
            string desc, float enemyMult, float traumaMult,
            bool hideIntents, int maxBranches, string gdd)
        {
            var d = CreateSO<DifficultyModifierSO>($"{folder}/{id}.asset");
            d.ModifierId             = id;
            d.DisplayName            = displayName;
            d.Description            = desc;
            d.EnemyStatMultiplier    = enemyMult;
            d.TraumaStackMultiplier  = traumaMult;
            d.HideAllEnemyIntents    = hideIntents;
            d.MaxRouteBranchChoices  = maxBranches;
            d.GDDReference           = gdd;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ── Badge factory ─────────────────────────────────────────────────────

        static BadgeSO Bg(string folder, string id, string displayName,
            string gymSource, string effectDesc, string gdd)
        {
            var b = CreateSO<BadgeSO>($"{folder}/{id}.asset");
            b.BadgeId          = id;
            b.DisplayName      = displayName;
            b.GymSource        = gymSource;
            b.EffectDescription= effectDesc;
            b.GDDReference     = gdd;
            EditorUtility.SetDirty(b);
            return b;
        }
    }
}
#endif
