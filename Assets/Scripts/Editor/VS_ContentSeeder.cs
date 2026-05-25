// Per Epic 3.3.A + 3.3.B — VS Content Seeder.
// Generates all ScriptableObject assets for the 6 VS Pokémon lines:
//   Starters (Vanguard only):  Bulbasaur, Charmander, Squirtle
//   Wild (single line):        Caterpie, Pidgey, Geodude
// Menu: Project Ascendant / Seed VS Content
// Idempotent — recreates assets at the same paths on every run.
//
// TWO-PASS species/branch creation avoids the circular reference:
//   Pass 1 — species SOs with moves + abilities (Branches list empty)
//   Pass 2 — branch SOs (EvolvedSpecies now exists); wire back to source species

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    public static class VS_ContentSeeder
    {
        const string ROOT = "Assets/ScriptableObjects/VS";

        // ── Entry point ───────────────────────────────────────────────────────

        [MenuItem("Project Ascendant/Seed VS Content")]
        public static void SeedAll()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                EnsureFolders();
                var ab  = SeedAbilities();
                var crv = SeedGrowthCurves();
                var mv  = SeedMoves();
                // Pass 1 — species with moves, abilities, growth; Branches empty
                var sp  = SeedSpecies_Pass1(mv, ab, crv);
                // Pass 2 — branches; now species SOs exist so EvolvedSpecies can be set
                var br  = SeedBranches(mv, ab, sp);
                // Pass 3 — wire branches back to their source species
                SeedSpecies_Pass2(sp, br);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log("[VS_ContentSeeder] Done — 6 Pokémon lines authored under " + ROOT);
        }

        // ── Folder setup ──────────────────────────────────────────────────────

        static void EnsureFolders()
        {
            MkDir("Assets/ScriptableObjects");
            MkDir(ROOT);
            MkDir($"{ROOT}/Abilities");
            MkDir($"{ROOT}/GrowthCurves");
            MkDir($"{ROOT}/Moves");
            MkDir($"{ROOT}/Moves/_Common");
            MkDir($"{ROOT}/Moves/Bulbasaur");
            MkDir($"{ROOT}/Moves/Charmander");
            MkDir($"{ROOT}/Moves/Squirtle");
            MkDir($"{ROOT}/Moves/Caterpie");
            MkDir($"{ROOT}/Moves/Pidgey");
            MkDir($"{ROOT}/Moves/Geodude");
            MkDir($"{ROOT}/Branches");
            MkDir($"{ROOT}/Species");
            MkDir($"{ROOT}/Species/Starters");
            MkDir($"{ROOT}/Species/Wild");
        }

        // ── Abilities ─────────────────────────────────────────────────────────
        // Per §5.8 — VS subset of the launch ability catalog.

        static Dictionary<string, AbilitySO> SeedAbilities()
        {
            var d = new Dictionary<string, AbilitySO>();
            string p = $"{ROOT}/Abilities";

            d["overgrow"]     = Ab(p,"overgrow",    "Overgrow",      AbilityCategory.Combat,
                "Grass moves deal +20% damage when HP < 30%. (Bulbasaur line — §5.5.3.4)","§5.5.3.4");
            d["blaze"]        = Ab(p,"blaze",       "Blaze",         AbilityCategory.Combat,
                "Fire moves deal +20% damage when HP < 30%. (Charmander line — §5.5.3.4)","§5.5.3.4");
            d["torrent"]      = Ab(p,"torrent",     "Torrent",       AbilityCategory.Combat,
                "Water moves deal +20% damage when HP < 30%. (Squirtle line — §5.5.3.4)","§5.5.3.4");
            d["tough_claws"]  = Ab(p,"tough_claws", "Tough Claws",   AbilityCategory.Combat,
                "Melee moves deal +15% damage.","§5.8");
            d["snipe"]        = Ab(p,"snipe",       "Snipe",         AbilityCategory.Combat,
                "Ranged moves deal +15% damage.","§5.8");
            d["shell_armor"]  = Ab(p,"shell_armor", "Shell Armor",   AbilityCategory.Combat,
                "While Lead, incoming hits deal −2 damage. (Blastoise VA1 — §5.6)","§5.6");
            d["swift_swim"]   = Ab(p,"swift_swim",  "Swift Swim",    AbilityCategory.Combat,
                "Draw +1 skill card on turn 1 of Rain Dance–active combats.","§5.8");
            d["compoundeyes"] = Ab(p,"compoundeyes","Compound Eyes", AbilityCategory.Combat,
                "Status-rider moves always apply their secondary effect. (Butterfree line — §5.8)","§5.8");
            d["keen_eye"]     = Ab(p,"keen_eye",    "Keen Eye",      AbilityCategory.Vision,
                "All Unknown intents are permanently revealed at combat start. (§5.5.3.1)","§5.5.3.1");
            d["sturdy"]       = Ab(p,"sturdy",      "Sturdy",        AbilityCategory.Survival,
                "Survive one lethal hit per combat at 1 HP. (Geodude line — §5.8)","§5.8");
            d["healer"]       = Ab(p,"healer",      "Healer",        AbilityCategory.Support,
                "At turn end, restore 3 HP to a random bench ally. (Pidgeot line — §5.8)","§5.8");
            d["iron_shell"]   = Ab(p,"iron_shell",  "Iron Shell",    AbilityCategory.Survival,
                "At combat start, this Pokémon's Defense is raised by 1 stage. (Metapod)","§5.5.1");

            return d;
        }

        // ── Growth Curves ─────────────────────────────────────────────────────
        // Per §5.2.3 — flat-per-level curves, custom-tuned for roguelike context.
        // 50 entries covers levels 1–51; index 0 = level 1→2 growth.

        static Dictionary<string, StatGrowthCurveSO> SeedGrowthCurves()
        {
            var d = new Dictionary<string, StatGrowthCurveSO>();
            string p = $"{ROOT}/GrowthCurves";
            // Crv(path, id, hp, atk, def, spd) — no SpAtk/SpDef per §4.1.1
            d["bulbasaur"]  = Crv(p,"Bulbasaur_Line",   3,2,2,2);
            d["charmander"] = Crv(p,"Charmander_Line",  2,3,2,3);
            d["squirtle"]   = Crv(p,"Squirtle_Line",    3,2,3,2);
            d["caterpie"]   = Crv(p,"Caterpie_Line",    2,1,2,2);
            d["pidgey"]     = Crv(p,"Pidgey_Line",      2,2,2,3);
            d["geodude"]    = Crv(p,"Geodude_Line",     3,3,3,2);
            return d;
        }

        // ── Moves ─────────────────────────────────────────────────────────────
        // Per §5.3.6 — kit construction rules per stage and archetype.
        // Effects stored as sub-assets of the move SO (AddObjectToAsset).
        // RangeModifierMultiplier set to 0.75 for Ranged, 1.0 for Melee per §9.3.2.2.

        static Dictionary<string, MoveSO> SeedMoves()
        {
            var d = new Dictionary<string, MoveSO>();

            // ── Common (shared across multiple species) ──────────────────────
            string pc = $"{ROOT}/Moves/_Common";

            var tackle   = Mv(pc,"tackle",      "Tackle",       PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         1, 40);
            var scratch  = Mv(pc,"scratch",     "Scratch",      PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         1, 40);
            var growl    = Mv(pc,"growl",       "Growl",        PokemonType.Normal, MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         0,  0);
                             Debuff(growl,   Stat.Attack,  -1);
            var gust     = Mv(pc,"gust",        "Gust",         PokemonType.Flying, MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         1, 50);
            var wingAtk  = Mv(pc,"wing_attack", "Wing Attack",  PokemonType.Flying, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 70);
            var sandAtk  = Mv(pc,"sand_attack", "Sand Attack",  PokemonType.Normal, MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0);
                             Debuff(sandAtk, Stat.Defense, -1);
            var roost    = Mv(pc,"roost",       "Roost",        PokemonType.Normal, MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         1,  0);
                             Heal(roost,   0.25f, true, 0);
            var withdraw = Mv(pc,"withdraw",    "Withdraw",     PokemonType.Normal, MoveRole.Defensive, MoveRange.Melee,  PositionalModifier.None,         1,  0);
                             Buff(withdraw, Stat.Defense, 1);

            d["tackle"]      = tackle;   d["scratch"]     = scratch;
            d["growl"]       = growl;    d["gust"]        = gust;
            d["wing_attack"] = wingAtk;  d["sand_attack"] = sandAtk;
            d["roost"]       = roost;    d["withdraw"]    = withdraw;

            // ── Bulbasaur line (Grass/Poison — Vanguard branch) ──────────────
            // Per §5.3.6: Pre-evo AP 0-2 no SF/SB; Mid Vanguard introduces SF/SB;
            //             Final Vanguard has 2 Off + 1 Def/Util + 1 signature, AP 1-4.
            string pb = $"{ROOT}/Moves/Bulbasaur";

            var vineWhip  = Mv(pb,"vine_whip",   "Vine Whip",    PokemonType.Grass,  MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         1, 45);
            var leechSeed = Mv(pb,"leech_seed",  "Leech Seed",   PokemonType.Grass,  MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         1,  0,
                flavor:"Poisons the target. (Modelled as Poison rider — §4.2.3)");
                             StatusRider(leechSeed, StatusCondition.Poison, 1f);
            // Mastery — §4.3.9.2: immutable 5th slot
            var seedBomb  = Mv(pb,"mastery_seedbomb","Seed Bomb", PokemonType.Grass, MoveRole.Offensive,  MoveRange.Ranged, PositionalModifier.None,         2, 60, gdd:"§4.3.9.2");
            // Ivysaur Vanguard — slot 0 replaces Tackle (introduces StepForward)
            var headbutt  = Mv(pb,"headbutt",    "Headbutt",     PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 65);
            var vineLash  = Mv(pb,"vine_lash",   "Vine Lash",    PokemonType.Grass,  MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 65);
            var megaDrain = Mv(pb,"mega_drain",  "Mega Drain",   PokemonType.Grass,  MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 50);
                             Heal(megaDrain, 0.25f, true, 0);
            var razorLeaf = Mv(pb,"mastery_razorleaf","Razor Leaf",PokemonType.Grass,MoveRole.Offensive,  MoveRange.Ranged, PositionalModifier.None,         2, 55, alwaysCrit:true, gdd:"§4.3.9.2");
            // Venusaur Vanguard A1 (Bloom Brawler)
            var petalBliz = Mv(pb,"petal_blizzard","Petal Blizzard",PokemonType.Grass,MoveRole.Offensive,MoveRange.Melee,  PositionalModifier.StepForward,  3, 90);
            var powerWhip = Mv(pb,"power_whip",  "Power Whip",   PokemonType.Grass,  MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 85);
            var sweetScnt = Mv(pb,"sweet_scent", "Sweet Scent",  PokemonType.Normal, MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0);
                             Debuff(sweetScnt, Stat.Defense, -2);
            var solarBeam = Mv(pb,"mastery_solarbeam","Solar Beam",PokemonType.Grass,MoveRole.Offensive,  MoveRange.Ranged, PositionalModifier.None,         4,120, gdd:"§4.3.9.2",
                flavor:"Ultimate. Venusaur signature mastery move. Shared by VA1 and VA2.");
            // Venusaur Vanguard A2 (Toxic Briar)
            var seedFlare = Mv(pb,"seed_flare",  "Seed Flare",   PokemonType.Grass,  MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 70);
                             Debuff(seedFlare, Stat.Defense, -1);
            var toxic     = Mv(pb,"toxic",       "Toxic",        PokemonType.Poison, MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         1,  0);
                             StatusRider(toxic, StatusCondition.Poison, 1f);
            var gigaDrain = Mv(pb,"giga_drain",  "Giga Drain",   PokemonType.Grass,  MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         3, 75);
                             Heal(gigaDrain, 0.50f, true, 0);

            d["vine_whip"]=vineWhip;
            d["leech_seed"]=leechSeed; d["mastery_seedbomb"]=seedBomb;
            d["headbutt"]=headbutt;    d["vine_lash"]=vineLash;
            d["mega_drain"]=megaDrain; d["mastery_razorleaf"]=razorLeaf;
            d["petal_blizzard"]=petalBliz; d["power_whip"]=powerWhip;
            d["sweet_scent"]=sweetScnt;    d["mastery_solarbeam"]=solarBeam;
            d["seed_flare"]=seedFlare;     d["toxic"]=toxic;
            d["giga_drain"]=gigaDrain;

            // ── Charmander line (Fire — Vanguard branch) ─────────────────────
            string pch = $"{ROOT}/Moves/Charmander";

            var ember       = Mv(pch,"ember",          "Ember",         PokemonType.Fire,   MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         1, 40);
                               StatusRider(ember, StatusCondition.Burn, 0.2f);
            var smokescr    = Mv(pch,"smokescreen",    "Smokescreen",   PokemonType.Normal, MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0);
                               Debuff(smokescr, Stat.Defense, -1);
            var fireFangM   = Mv(pch,"mastery_firefang","Fire Fang",    PokemonType.Fire,   MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         1, 55, gdd:"§4.3.9.2");
                               StatusRider(fireFangM, StatusCondition.Burn, 0.3f);
            // Charmeleon Vanguard
            var dClaw       = Mv(pch,"dragon_claw",    "Dragon Claw",   PokemonType.Dragon, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 65);
            var flameWheel  = Mv(pch,"flame_wheel",    "Flame Wheel",   PokemonType.Fire,   MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepBackward, 2, 60);
                               StatusRider(flameWheel, StatusCondition.Burn, 0.3f);
            var slash       = Mv(pch,"slash",          "Slash",         PokemonType.Normal, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         2, 70, alwaysCrit:true);
            var flameChrg   = Mv(pch,"mastery_flamecharge","Flame Charge",PokemonType.Fire, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 65, gdd:"§4.3.9.2");
            // Charizard VA1 (Sky Striker)
            var flamethr    = Mv(pch,"flamethrower",   "Flamethrower",  PokemonType.Fire,   MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 90);
                               StatusRider(flamethr, StatusCondition.Burn, 0.2f);
            var dClawPlus   = Mv(pch,"dragon_claw_plus","Dragon Claw+", PokemonType.Dragon, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepBackward, 3,100,
                flavor:"Signature move. Charizard VA1 slot 3.");
            var overheat    = Mv(pch,"mastery_overheat","Overheat",     PokemonType.Fire,   MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         4,130, gdd:"§4.3.9.2",
                flavor:"Ultimate. Charizard signature mastery move.");
            // Charizard VA2 (Inferno Dragon)
            var fireFangP   = Mv(pch,"fire_fang_plus", "Fire Fang+",   PokemonType.Fire,   MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 75);
                               StatusRider(fireFangP, StatusCondition.Burn, 0.5f);
            var inferno     = Mv(pch,"inferno",        "Inferno",       PokemonType.Fire,   MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         3, 95, alwaysCrit:true,
                flavor:"Signature move. Charizard VA2 slot 3.");

            d["ember"]=ember;                d["smokescreen"]=smokescr;
            d["mastery_firefang"]=fireFangM; d["dragon_claw"]=dClaw;
            d["flame_wheel"]=flameWheel;     d["slash"]=slash;
            d["mastery_flamecharge"]=flameChrg; d["flamethrower"]=flamethr;
            d["dragon_claw_plus"]=dClawPlus; d["mastery_overheat"]=overheat;
            d["fire_fang_plus"]=fireFangP;   d["inferno"]=inferno;

            // ── Squirtle line (Water — Vanguard branch, §5.6 worked example) ─
            string ps = $"{ROOT}/Moves/Squirtle";

            var waterGun  = Mv(ps,"water_gun",      "Water Gun",    PokemonType.Water, MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         1, 45);
            var tailWhip  = Mv(ps,"tail_whip",      "Tail Whip",    PokemonType.Normal,MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         0,  0);
                             Debuff(tailWhip, Stat.Defense, -1);
            var waterPulse= Mv(ps,"mastery_waterpulse","Water Pulse",PokemonType.Water,MoveRole.Offensive,  MoveRange.Ranged, PositionalModifier.None,         1, 50, gdd:"§4.3.9.2");
            // Wartortle Vanguard (§5.6)
            var skullBash = Mv(ps,"skull_bash",     "Skull Bash",   PokemonType.Normal,MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepBackward, 2, 60,
                flavor:"+50% Power vs Tackle. §5.6 Wartortle Vanguard.");
            var aquaJet   = Mv(ps,"aqua_jet",       "Aqua Jet",     PokemonType.Water, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  1, 45,
                flavor:"Priority-feeling StepForward. §5.6 Wartortle Vanguard.");
            var waterfall = Mv(ps,"mastery_waterfall","Waterfall",  PokemonType.Water, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 80, gdd:"§4.3.9.2");
            // Blastoise VA1 (Heavy Brawler, §5.6)
            var hydroCrsh = Mv(ps,"hydro_crash",    "Hydro Crash",  PokemonType.Water, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  3, 95,
                flavor:"Ultimate-eligible. §5.6 Heavy Brawler Blastoise.");
            var surf      = Mv(ps,"surf",           "Surf",         PokemonType.Water, MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 70,
                flavor:"Cleave — hits all enemies for 70% damage. Epic 4 combat flag. §5.6.");
            var aquaRing  = Mv(ps,"aqua_ring",      "Aqua Ring",    PokemonType.Water, MoveRole.Defensive, MoveRange.Melee,  PositionalModifier.None,         1,  0,
                flavor:"Regen: floor(MaxHP/8) HP per turn for 3 turns. §5.6.");
                             Heal(aquaRing, 0.125f, true, 3);
            var hydroCann = Mv(ps,"mastery_hydrocannon","Hydro Cannon",PokemonType.Water,MoveRole.Offensive,MoveRange.Ranged,PositionalModifier.None,          4,130, gdd:"§4.3.9.2",
                flavor:"Ultimate. Blastoise signature mastery. Shared by VA1 and VA2.");
            // Blastoise VA2 (Aqua-Jet Duelist, §5.6)
            var skullBshP = Mv(ps,"skull_bash_plus","Skull Bash+",  PokemonType.Normal,MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepBackward, 2, 80,
                flavor:"Higher power than Skull Bash; debuffs enemy Defense −1. §5.6.");
                             Debuff(skullBshP, Stat.Defense, -1);
            var hydroPump = Mv(ps,"hydro_pump",     "Hydro Pump",   PokemonType.Water, MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         3,110,
                flavor:"Highest single-target damage in kit. §5.6.");
            var aquaJetP  = Mv(ps,"aqua_jet_plus",  "Aqua Jet+",   PokemonType.Water, MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  1, 60,
                flavor:"Ignores 1 point of enemy Defense on resolution. §5.6 Aqua-Jet Duelist.");

            d["water_gun"]=waterGun;         d["tail_whip"]=tailWhip;
            d["mastery_waterpulse"]=waterPulse; d["skull_bash"]=skullBash;
            d["aqua_jet"]=aquaJet;           d["mastery_waterfall"]=waterfall;
            d["hydro_crash"]=hydroCrsh;      d["surf"]=surf;
            d["aqua_ring"]=aquaRing;         d["mastery_hydrocannon"]=hydroCann;
            d["skull_bash_plus"]=skullBshP;  d["hydro_pump"]=hydroPump;
            d["aqua_jet_plus"]=aquaJetP;

            // ── Caterpie line (Bug — single Specialist path) ─────────────────
            string pcat = $"{ROOT}/Moves/Caterpie";

            var strShot   = Mv(pcat,"string_shot",  "String Shot",  PokemonType.Bug,    MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0);
                             Debuff(strShot, Stat.Defense, -1);
            var bugBite   = Mv(pcat,"bug_bite",     "Bug Bite",     PokemonType.Bug,    MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         1, 40);
            var harden    = Mv(pcat,"harden",       "Harden",       PokemonType.Normal, MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         0,  0);
                             Buff(harden, Stat.Defense, 1);
            var silkShot  = Mv(pcat,"mastery_silkshot","Silk Shot", PokemonType.Bug,    MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 55, gdd:"§4.3.9.2");
                             Debuff(silkShot, Stat.Defense, -1);
            // Metapod
            var silkBind  = Mv(pcat,"silk_bind",    "Silk Bind",    PokemonType.Bug,    MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         1,  0);
                             Debuff(silkBind, Stat.Attack, -1);
            var pinShot   = Mv(pcat,"pin_shot",     "Pin Shot",     PokemonType.Bug,    MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         1, 50);
            var hardenP   = Mv(pcat,"harden_plus",  "Harden+",      PokemonType.Normal, MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         0,  0);
                             Buff(hardenP, Stat.Defense, 2);
            var bflyCoil  = Mv(pcat,"mastery_bflycoil","Butterfly Coil",PokemonType.Bug,MoveRole.Offensive,MoveRange.Melee,  PositionalModifier.None,         2, 65, gdd:"§4.3.9.2");
                             Buff(bflyCoil, Stat.Defense, 1);
            // Butterfree (gust reused from Common)
            var powdSprd  = Mv(pcat,"powder_spread","Powder Spread", PokemonType.Bug,    MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         1,  0);
                             StatusRider(powdSprd, StatusCondition.Sleep, 0.6f);
            var silvWind  = Mv(pcat,"silver_wind",  "Silver Wind",  PokemonType.Bug,    MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 65);
                             Buff(silvWind, Stat.Attack, 1);
            var psybeam   = Mv(pcat,"psybeam",      "Psybeam",      PokemonType.Psychic,MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 70);
                             StatusRider(psybeam, StatusCondition.Confusion, 0.2f);
            var bugBuzz   = Mv(pcat,"mastery_bugbuzz","Bug Buzz",   PokemonType.Bug,    MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         3, 90, gdd:"§4.3.9.2");

            d["string_shot"]=strShot;        d["bug_bite"]=bugBite;
            d["harden"]=harden;              d["mastery_silkshot"]=silkShot;
            d["silk_bind"]=silkBind;         d["pin_shot"]=pinShot;
            d["harden_plus"]=hardenP;        d["mastery_bflycoil"]=bflyCoil;
            d["powder_spread"]=powdSprd;     d["silver_wind"]=silvWind;
            d["psybeam"]=psybeam;            d["mastery_bugbuzz"]=bugBuzz;

            // ── Pidgey line (Flying/Normal — single Support path) ─────────────
            string pp = $"{ROOT}/Moves/Pidgey";
            // gust, wing_attack, sand_attack, roost already in d[]

            var peck      = Mv(pp,"mastery_peck",   "Peck",          PokemonType.Flying,MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         1, 45, gdd:"§4.3.9.2");
            var quickAtk  = Mv(pp,"quick_attack",   "Quick Attack",  PokemonType.Normal,MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  1, 40);
            var tailwind  = Mv(pp,"tailwind",       "Tailwind",      PokemonType.Flying,MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0,
                flavor:"Draws +1 skill card this turn. Support archetype — §5.3.4.");
                             DrawCards(tailwind, 1);
            var fthrDnce  = Mv(pp,"feather_dance",  "Feather Dance", PokemonType.Flying,MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0);
                             Debuff(fthrDnce, Stat.Attack, -2);
            var aerialAce = Mv(pp,"aerial_ace",     "Aerial Ace",    PokemonType.Flying,MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 70);
            var masWingAtk= Mv(pp,"mastery_wingattack_mid","Wing Attack",PokemonType.Flying,MoveRole.Offensive,MoveRange.Melee,PositionalModifier.StepForward, 2, 70, gdd:"§4.3.9.2",
                flavor:"Pidgeotto mastery. Shared asset with common wing_attack stats.");
            var hurricane = Mv(pp,"hurricane",      "Hurricane",     PokemonType.Flying,MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         3, 95);
            var tailwindV2= Mv(pp,"tailwind_v2",    "Tailwind+",     PokemonType.Flying,MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0,
                flavor:"Draws +2 skill cards this turn. Upgraded Tailwind at Pidgeot.");
                             DrawCards(tailwindV2, 2);
            var roostFnl  = Mv(pp,"roost_final",    "Roost+",        PokemonType.Normal,MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         1,  0);
                             Heal(roostFnl, 0.35f, true, 0);
            var airSlash  = Mv(pp,"mastery_airslash","Air Slash",    PokemonType.Flying,MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 75, gdd:"§4.3.9.2");
                             StatusRider(airSlash, StatusCondition.Confusion, 0.2f);

            d["mastery_peck"]=peck;           d["quick_attack"]=quickAtk;
            d["tailwind"]=tailwind;           d["feather_dance"]=fthrDnce;
            d["aerial_ace"]=aerialAce;        d["mastery_wingattack_mid"]=masWingAtk;
            d["hurricane"]=hurricane;         d["tailwind_v2"]=tailwindV2;
            d["roost_final"]=roostFnl;        d["mastery_airslash"]=airSlash;

            // ── Geodude line (Rock/Ground — single Vanguard path) ─────────────
            string pgeo = $"{ROOT}/Moves/Geodude";

            var rockThrow = Mv(pgeo,"rock_throw",   "Rock Throw",   PokemonType.Rock,    MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         1, 45);
            var defCurl   = Mv(pgeo,"defense_curl", "Defense Curl", PokemonType.Normal,  MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         0,  0);
                             Buff(defCurl, Stat.Defense, 1);
            var magnitude = Mv(pgeo,"magnitude",    "Magnitude",    PokemonType.Ground,  MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         1, 50);
            var smackDown = Mv(pgeo,"mastery_smackdown","Smack Down",PokemonType.Rock,   MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None,         2, 65, gdd:"§4.3.9.2");
            // Graveler Vanguard
            var rockBlast = Mv(pgeo,"rock_blast",   "Rock Blast",   PokemonType.Rock,    MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepBackward, 2, 65);
            var rollout   = Mv(pgeo,"rollout",      "Rollout",      PokemonType.Rock,    MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  1, 50,
                flavor:"Introduces StepForward at mid-evo — Vanguard archetype. §5.3.6.2.");
            var sRock     = Mv(pgeo,"stealth_rock", "Stealth Rock", PokemonType.Rock,    MoveRole.Utility,   MoveRange.Ranged, PositionalModifier.None,         0,  0);
                             Debuff(sRock, Stat.Defense, -1);
            var earthquake= Mv(pgeo,"earthquake",   "Earthquake",   PokemonType.Ground,  MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         3, 90);
            var rockSmash = Mv(pgeo,"mastery_rocksmash","Rock Smash",PokemonType.Fighting,MoveRole.Offensive,MoveRange.Melee,  PositionalModifier.None,         2, 50, gdd:"§4.3.9.2");
                             Debuff(rockSmash, Stat.Defense, -1);
            // Golem Vanguard
            var stoneEdge = Mv(pgeo,"stone_edge",   "Stone Edge",   PokemonType.Rock,    MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepBackward, 3, 95, alwaysCrit:true,
                flavor:"Signature. Always crits. Golem Vanguard slot 0.");
            var rockPol   = Mv(pgeo,"rock_polish",  "Rock Polish",  PokemonType.Rock,    MoveRole.Utility,   MoveRange.Melee,  PositionalModifier.None,         0,  0);
                             Buff(rockPol, Stat.Attack, 2);
            var bodyPress = Mv(pgeo,"body_press",   "Body Press",   PokemonType.Fighting,MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.StepForward,  2, 80);
            var fissure   = Mv(pgeo,"mastery_fissure","Fissure",    PokemonType.Ground,  MoveRole.Offensive, MoveRange.Melee,  PositionalModifier.None,         4,140, gdd:"§4.3.9.2",
                flavor:"Ultimate. Golem signature mastery.");

            d["rock_throw"]=rockThrow;      d["defense_curl"]=defCurl;
            d["magnitude"]=magnitude;       d["mastery_smackdown"]=smackDown;
            d["rock_blast"]=rockBlast;      d["rollout"]=rollout;
            d["stealth_rock"]=sRock;        d["earthquake"]=earthquake;
            d["mastery_rocksmash"]=rockSmash; d["stone_edge"]=stoneEdge;
            d["rock_polish"]=rockPol;       d["body_press"]=bodyPress;
            d["mastery_fissure"]=fissure;

            return d;
        }

        // ── Pass 1 — Species SOs (Branches list empty) ───────────────────────
        // Per §9.3.2.1. One SO per stage per branch for evolved forms.
        // Starters: base(1) + mid_V(1) + final_VA1(1) + final_VA2(1) each.
        // Wild:     base(1) + mid(1) + final(1) each (single path).

        static Dictionary<string, PokemonSpeciesSO> SeedSpecies_Pass1(
            Dictionary<string, MoveSO> mv,
            Dictionary<string, AbilitySO> ab,
            Dictionary<string, StatGrowthCurveSO> crv)
        {
            var d = new Dictionary<string, PokemonSpeciesSO>();
            string pst = $"{ROOT}/Species/Starters";
            string pwl = $"{ROOT}/Species/Wild";

            // ── Bulbasaur line ───────────────────────────────────────────────
            d["bulbasaur"] = Sp(pst,"Bulbasaur","Bulbasaur","bulbasaur",
                new[]{PokemonType.Grass},
                BS(45,49,49,45), crv["bulbasaur"],
                new[]{mv["tackle"],mv["vine_whip"],mv["growl"],mv["leech_seed"]},
                masteryMove: mv["mastery_seedbomb"],
                primaryAbility: null,      // no ability at base form — §5.5.1
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Meadow});

            d["ivysaur_v"] = Sp(pst,"Ivysaur_Vanguard","Ivysaur","ivysaur",
                new[]{PokemonType.Grass,PokemonType.Poison},
                BS(60,62,63,60), crv["bulbasaur"],
                new[]{mv["headbutt"],mv["vine_lash"],mv["growl"],mv["mega_drain"]},
                masteryMove: mv["mastery_razorleaf"],
                primaryAbility: ab["overgrow"],   // gained at first evolution — §5.5.1
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Meadow});

            d["venusaur_va1"] = Sp(pst,"Venusaur_VanguardA1","Venusaur","venusaur",
                new[]{PokemonType.Grass,PokemonType.Poison},
                BS(80,82,83,80), crv["bulbasaur"],
                new[]{mv["petal_blizzard"],mv["power_whip"],mv["sweet_scent"],mv["mega_drain"]},
                masteryMove: mv["mastery_solarbeam"],
                primaryAbility: ab["overgrow"],
                secondaryAbility: ab["tough_claws"],  // Vanguard branch secondary — §5.5.1
                wildRarity: RarityTier.Rare,
                biomes: new[]{Biome.Meadow});

            d["venusaur_va2"] = Sp(pst,"Venusaur_VanguardA2","Venusaur","venusaur",
                new[]{PokemonType.Grass,PokemonType.Poison},
                BS(80,82,83,80), crv["bulbasaur"],
                new[]{mv["seed_flare"],mv["vine_lash"],mv["toxic"],mv["giga_drain"]},
                masteryMove: mv["mastery_solarbeam"],
                primaryAbility: ab["overgrow"],
                secondaryAbility: ab["snipe"],
                wildRarity: RarityTier.Rare,
                biomes: new[]{Biome.Meadow});

            // ── Charmander line ──────────────────────────────────────────────
            d["charmander"] = Sp(pst,"Charmander","Charmander","charmander",
                new[]{PokemonType.Fire},
                BS(39,52,43,65), crv["charmander"],
                new[]{mv["scratch"],mv["ember"],mv["growl"],mv["smokescreen"]},
                masteryMove: mv["mastery_firefang"],
                primaryAbility: null,
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Cave});

            d["charmeleon_v"] = Sp(pst,"Charmeleon_Vanguard","Charmeleon","charmeleon",
                new[]{PokemonType.Fire},
                BS(58,64,58,80), crv["charmander"],
                new[]{mv["dragon_claw"],mv["flame_wheel"],mv["growl"],mv["slash"]},
                masteryMove: mv["mastery_flamecharge"],
                primaryAbility: ab["blaze"],
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Cave});

            d["charizard_va1"] = Sp(pst,"Charizard_VanguardA1","Charizard","charizard",
                new[]{PokemonType.Fire,PokemonType.Flying},
                BS(78,84,78,100), crv["charmander"],
                new[]{mv["wing_attack"],mv["flamethrower"],mv["roost"],mv["dragon_claw_plus"]},
                masteryMove: mv["mastery_overheat"],
                primaryAbility: ab["blaze"],
                secondaryAbility: ab["tough_claws"],
                wildRarity: RarityTier.Rare,
                biomes: new[]{Biome.Cave,Biome.Mountain});

            d["charizard_va2"] = Sp(pst,"Charizard_VanguardA2","Charizard","charizard",
                new[]{PokemonType.Fire,PokemonType.Flying},
                BS(78,84,78,100), crv["charmander"],
                new[]{mv["fire_fang_plus"],mv["flame_wheel"],mv["growl"],mv["inferno"]},
                masteryMove: mv["mastery_overheat"],
                primaryAbility: ab["blaze"],
                secondaryAbility: ab["snipe"],
                wildRarity: RarityTier.Rare,
                biomes: new[]{Biome.Cave,Biome.Mountain});

            // ── Squirtle line (§5.6) ─────────────────────────────────────────
            d["squirtle"] = Sp(pst,"Squirtle","Squirtle","squirtle",
                new[]{PokemonType.Water},
                BS(44,48,65,43), crv["squirtle"],
                new[]{mv["tackle"],mv["water_gun"],mv["withdraw"],mv["tail_whip"]},
                masteryMove: mv["mastery_waterpulse"],
                primaryAbility: null,
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.River,Biome.Shoreline});

            d["wartortle_v"] = Sp(pst,"Wartortle_Vanguard","Wartortle","wartortle",
                new[]{PokemonType.Water},
                BS(59,63,80,58), crv["squirtle"],
                new[]{mv["skull_bash"],mv["water_gun"],mv["withdraw"],mv["aqua_jet"]},
                masteryMove: mv["mastery_waterfall"],
                primaryAbility: ab["torrent"],
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.River,Biome.Shoreline});

            d["blastoise_va1"] = Sp(pst,"Blastoise_VanguardA1","Blastoise","blastoise",
                new[]{PokemonType.Water},
                BS(79,83,100,78), crv["squirtle"],
                new[]{mv["hydro_crash"],mv["surf"],mv["aqua_ring"],mv["aqua_jet"]},
                masteryMove: mv["mastery_hydrocannon"],
                primaryAbility: ab["torrent"],
                secondaryAbility: ab["shell_armor"],
                wildRarity: RarityTier.Rare,
                biomes: new[]{Biome.River,Biome.Shoreline});

            d["blastoise_va2"] = Sp(pst,"Blastoise_VanguardA2","Blastoise","blastoise",
                new[]{PokemonType.Water},
                BS(79,83,100,78), crv["squirtle"],
                new[]{mv["skull_bash_plus"],mv["hydro_pump"],mv["withdraw"],mv["aqua_jet_plus"]},
                masteryMove: mv["mastery_hydrocannon"],
                primaryAbility: ab["torrent"],
                secondaryAbility: ab["swift_swim"],
                wildRarity: RarityTier.Rare,
                biomes: new[]{Biome.River,Biome.Shoreline});

            // ── Caterpie line (Bug — single Specialist path) ─────────────────
            d["caterpie"] = Sp(pwl,"Caterpie","Caterpie","caterpie",
                new[]{PokemonType.Bug},
                BS(45,30,35,45), crv["caterpie"],
                new[]{mv["tackle"],mv["string_shot"],mv["bug_bite"],mv["harden"]},
                masteryMove: mv["mastery_silkshot"],
                primaryAbility: null,
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Meadow,Biome.Forest});

            d["metapod"] = Sp(pwl,"Metapod","Metapod","metapod",
                new[]{PokemonType.Bug},
                BS(50,20,55,30), crv["caterpie"],
                new[]{mv["tackle"],mv["silk_bind"],mv["pin_shot"],mv["harden_plus"]},
                masteryMove: mv["mastery_bflycoil"],
                primaryAbility: ab["iron_shell"],
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Meadow,Biome.Forest});

            d["butterfree"] = Sp(pwl,"Butterfree","Butterfree","butterfree",
                new[]{PokemonType.Bug,PokemonType.Flying},
                BS(60,45,50,70), crv["caterpie"],
                new[]{mv["gust"],mv["powder_spread"],mv["silver_wind"],mv["psybeam"]},
                masteryMove: mv["mastery_bugbuzz"],
                primaryAbility: ab["iron_shell"],
                secondaryAbility: ab["compoundeyes"],
                wildRarity: RarityTier.Uncommon,
                biomes: new[]{Biome.Meadow,Biome.Forest});

            // ── Pidgey line (Flying/Normal — single Support path) ─────────────
            d["pidgey"] = Sp(pwl,"Pidgey","Pidgey","pidgey",
                new[]{PokemonType.Normal,PokemonType.Flying},
                BS(40,45,40,56), crv["pidgey"],
                new[]{mv["gust"],mv["tackle"],mv["sand_attack"],mv["roost"]},
                masteryMove: mv["mastery_peck"],
                primaryAbility: null,
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Meadow});

            d["pidgeotto"] = Sp(pwl,"Pidgeotto","Pidgeotto","pidgeotto",
                new[]{PokemonType.Normal,PokemonType.Flying},
                BS(63,60,55,71), crv["pidgey"],
                new[]{mv["aerial_ace"],mv["tailwind"],mv["sand_attack"],mv["roost"]},
                masteryMove: mv["mastery_wingattack_mid"],
                primaryAbility: ab["keen_eye"],
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Meadow});

            d["pidgeot"] = Sp(pwl,"Pidgeot","Pidgeot","pidgeot",
                new[]{PokemonType.Normal,PokemonType.Flying},
                BS(83,80,75,101), crv["pidgey"],
                new[]{mv["hurricane"],mv["tailwind_v2"],mv["sand_attack"],mv["roost_final"]},
                masteryMove: mv["mastery_airslash"],
                primaryAbility: ab["keen_eye"],
                secondaryAbility: ab["healer"],
                wildRarity: RarityTier.Uncommon,
                biomes: new[]{Biome.Meadow});

            // ── Geodude line (Rock/Ground — single Vanguard path) ─────────────
            d["geodude"] = Sp(pwl,"Geodude","Geodude","geodude",
                new[]{PokemonType.Rock,PokemonType.Ground},
                BS(40,80,100,20), crv["geodude"],
                new[]{mv["tackle"],mv["rock_throw"],mv["defense_curl"],mv["magnitude"]},
                masteryMove: mv["mastery_smackdown"],
                primaryAbility: null,
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Cave,Biome.Mountain});

            d["graveler"] = Sp(pwl,"Graveler","Graveler","graveler",
                new[]{PokemonType.Rock,PokemonType.Ground},
                BS(55,95,115,35), crv["geodude"],
                new[]{mv["rock_blast"],mv["rollout"],mv["stealth_rock"],mv["earthquake"]},
                masteryMove: mv["mastery_rocksmash"],
                primaryAbility: ab["sturdy"],
                wildRarity: RarityTier.Common,
                biomes: new[]{Biome.Cave,Biome.Mountain});

            d["golem"] = Sp(pwl,"Golem","Golem","golem",
                new[]{PokemonType.Rock,PokemonType.Ground},
                BS(80,120,130,45), crv["geodude"],
                new[]{mv["stone_edge"],mv["rock_polish"],mv["body_press"],mv["earthquake"]},
                masteryMove: mv["mastery_fissure"],
                primaryAbility: ab["sturdy"],
                secondaryAbility: ab["tough_claws"],
                wildRarity: RarityTier.Uncommon,
                biomes: new[]{Biome.Cave,Biome.Mountain});

            return d;
        }

        // ── Branches ──────────────────────────────────────────────────────────
        // Per §5.3 + §5.3.5 + §5.10. MoveUpgrades = pool moves that change; NewMoves = pool additions.

        static Dictionary<string, EvolutionBranchSO> SeedBranches(
            Dictionary<string, MoveSO> mv,
            Dictionary<string, AbilitySO> ab,
            Dictionary<string, PokemonSpeciesSO> sp)
        {
            var d = new Dictionary<string, EvolutionBranchSO>();
            string p = $"{ROOT}/Branches";

            // ── Bulbasaur → Ivysaur (Vanguard) ──────────────────────────────
            // Bulbasaur pool: [tackle, vine_whip, growl, leech_seed]
            // Upgrades: tackle→headbutt, vine_whip→vine_lash, leech_seed→mega_drain (growl kept)
            d["bulbasaur_vanguard"] = Br(p,"bulbasaur_vanguard","Vanguard",
                BranchArchetype.Vanguard, sp["ivysaur_v"],
                new[]{(mv["tackle"],mv["headbutt"]),(mv["vine_whip"],mv["vine_lash"]),(mv["leech_seed"],mv["mega_drain"])},
                null, "§5.3.4");

            // Ivysaur V → Venusaur A1 (Bloom Brawler)
            // Ivysaur pool: [headbutt, vine_lash, growl, mega_drain]
            // Upgrades: headbutt→petal_blizzard, vine_lash→power_whip, growl→sweet_scent (mega_drain kept)
            d["ivysaur_va1"] = Br(p,"ivysaur_va1","Bloom Brawler",
                BranchArchetype.Vanguard, sp["venusaur_va1"],
                new[]{(mv["headbutt"],mv["petal_blizzard"]),(mv["vine_lash"],mv["power_whip"]),(mv["growl"],mv["sweet_scent"])},
                ab["tough_claws"], "§5.3.5");

            // Ivysaur V → Venusaur A2 (Toxic Briar)
            // Ivysaur pool: [headbutt, vine_lash, growl, mega_drain]
            // Upgrades: headbutt→seed_flare, growl→toxic, mega_drain→giga_drain (vine_lash kept)
            d["ivysaur_va2"] = Br(p,"ivysaur_va2","Toxic Briar",
                BranchArchetype.Vanguard, sp["venusaur_va2"],
                new[]{(mv["headbutt"],mv["seed_flare"]),(mv["growl"],mv["toxic"]),(mv["mega_drain"],mv["giga_drain"])},
                ab["snipe"], "§5.3.5");

            // ── Charmander → Charmeleon (Vanguard) ──────────────────────────
            // Charmander pool: [scratch, ember, growl, smokescreen]
            // Upgrades: scratch→dragon_claw, ember→flame_wheel, smokescreen→slash (growl kept)
            d["charmander_vanguard"] = Br(p,"charmander_vanguard","Vanguard",
                BranchArchetype.Vanguard, sp["charmeleon_v"],
                new[]{(mv["scratch"],mv["dragon_claw"]),(mv["ember"],mv["flame_wheel"]),(mv["smokescreen"],mv["slash"])},
                null, "§5.3.4");

            // Charmeleon V → Charizard A1 (Sky Striker)
            // Charmeleon pool: [dragon_claw, flame_wheel, growl, slash]
            // Upgrades: all 4 — dragon_claw→wing_attack, flame_wheel→flamethrower, growl→roost, slash→dragon_claw_plus
            d["charmeleon_va1"] = Br(p,"charmeleon_va1","Sky Striker",
                BranchArchetype.Vanguard, sp["charizard_va1"],
                new[]{(mv["dragon_claw"],mv["wing_attack"]),(mv["flame_wheel"],mv["flamethrower"]),(mv["growl"],mv["roost"]),(mv["slash"],mv["dragon_claw_plus"])},
                ab["tough_claws"], "§5.3.5");

            // Charmeleon V → Charizard A2 (Inferno Dragon)
            // Charmeleon pool: [dragon_claw, flame_wheel, growl, slash]
            // Upgrades: dragon_claw→fire_fang_plus, slash→inferno (flame_wheel, growl kept)
            d["charmeleon_va2"] = Br(p,"charmeleon_va2","Inferno Dragon",
                BranchArchetype.Vanguard, sp["charizard_va2"],
                new[]{(mv["dragon_claw"],mv["fire_fang_plus"]),(mv["slash"],mv["inferno"])},
                ab["snipe"], "§5.3.5");

            // ── Squirtle → Wartortle (Vanguard, §5.6) ───────────────────────
            // Squirtle pool: [tackle, water_gun, withdraw, tail_whip]
            // Upgrades: tackle→skull_bash, tail_whip→aqua_jet (water_gun, withdraw kept)
            d["squirtle_vanguard"] = Br(p,"squirtle_vanguard","Vanguard",
                BranchArchetype.Vanguard, sp["wartortle_v"],
                new[]{(mv["tackle"],mv["skull_bash"]),(mv["tail_whip"],mv["aqua_jet"])},
                null, "§5.6");

            // Wartortle V → Blastoise A1 (Heavy Brawler, §5.6)
            // Wartortle pool: [skull_bash, water_gun, withdraw, aqua_jet]
            // Upgrades: skull_bash→hydro_crash, water_gun→surf, withdraw→aqua_ring (aqua_jet kept)
            d["wartortle_va1"] = Br(p,"wartortle_va1","Heavy Brawler",
                BranchArchetype.Vanguard, sp["blastoise_va1"],
                new[]{(mv["skull_bash"],mv["hydro_crash"]),(mv["water_gun"],mv["surf"]),(mv["withdraw"],mv["aqua_ring"])},
                ab["shell_armor"], "§5.6");

            // Wartortle V → Blastoise A2 (Aqua-Jet Duelist, §5.6)
            // Wartortle pool: [skull_bash, water_gun, withdraw, aqua_jet]
            // Upgrades: skull_bash→skull_bash_plus, water_gun→hydro_pump, aqua_jet→aqua_jet_plus (withdraw kept)
            d["wartortle_va2"] = Br(p,"wartortle_va2","Aqua-Jet Duelist",
                BranchArchetype.Vanguard, sp["blastoise_va2"],
                new[]{(mv["skull_bash"],mv["skull_bash_plus"]),(mv["water_gun"],mv["hydro_pump"]),(mv["aqua_jet"],mv["aqua_jet_plus"])},
                ab["swift_swim"], "§5.6");

            // ── Caterpie → Metapod (single Specialist path) ──────────────────
            // Caterpie pool: [tackle, string_shot, bug_bite, harden]
            // Upgrades: string_shot→silk_bind, bug_bite→pin_shot, harden→harden_plus (tackle kept)
            d["caterpie_evolve"] = Br(p,"caterpie_evolve","Specialist",
                BranchArchetype.Specialist, sp["metapod"],
                new[]{(mv["string_shot"],mv["silk_bind"]),(mv["bug_bite"],mv["pin_shot"]),(mv["harden"],mv["harden_plus"])},
                ab["iron_shell"], "§5.3.4");

            // Metapod → Butterfree
            // Metapod pool: [tackle, silk_bind, pin_shot, harden_plus]
            // Upgrades: all 4 — tackle→gust, silk_bind→powder_spread, pin_shot→silver_wind, harden_plus→psybeam
            d["metapod_evolve"] = Br(p,"metapod_evolve","Specialist",
                BranchArchetype.Specialist, sp["butterfree"],
                new[]{(mv["tackle"],mv["gust"]),(mv["silk_bind"],mv["powder_spread"]),(mv["pin_shot"],mv["silver_wind"]),(mv["harden_plus"],mv["psybeam"])},
                ab["compoundeyes"], "§5.3.4");

            // ── Pidgey → Pidgeotto (single Support path) ─────────────────────
            // Pidgey pool: [gust, tackle, sand_attack, roost]
            // Upgrades: gust→aerial_ace, tackle→tailwind (sand_attack, roost kept)
            d["pidgey_evolve"] = Br(p,"pidgey_evolve","Support",
                BranchArchetype.Support, sp["pidgeotto"],
                new[]{(mv["gust"],mv["aerial_ace"]),(mv["tackle"],mv["tailwind"])},
                ab["keen_eye"], "§5.3.4");

            // Pidgeotto → Pidgeot
            // Pidgeotto pool: [aerial_ace, tailwind, sand_attack, roost]
            // Upgrades: aerial_ace→hurricane, tailwind→tailwind_v2, roost→roost_final (sand_attack kept)
            d["pidgeotto_evolve"] = Br(p,"pidgeotto_evolve","Support",
                BranchArchetype.Support, sp["pidgeot"],
                new[]{(mv["aerial_ace"],mv["hurricane"]),(mv["tailwind"],mv["tailwind_v2"]),(mv["roost"],mv["roost_final"])},
                ab["healer"], "§5.3.4");

            // ── Geodude → Graveler (single Vanguard path) ─────────────────────
            // Geodude pool: [tackle, rock_throw, defense_curl, magnitude]
            // Upgrades: all 4 — tackle→rock_blast, rock_throw→rollout, defense_curl→stealth_rock, magnitude→earthquake
            d["geodude_evolve"] = Br(p,"geodude_evolve","Vanguard",
                BranchArchetype.Vanguard, sp["graveler"],
                new[]{(mv["tackle"],mv["rock_blast"]),(mv["rock_throw"],mv["rollout"]),(mv["defense_curl"],mv["stealth_rock"]),(mv["magnitude"],mv["earthquake"])},
                ab["sturdy"], "§5.3.4");

            // Graveler → Golem
            // Graveler pool: [rock_blast, rollout, stealth_rock, earthquake]
            // Upgrades: rock_blast→stone_edge, rollout→rock_polish, stealth_rock→body_press (earthquake kept)
            d["graveler_evolve"] = Br(p,"graveler_evolve","Vanguard",
                BranchArchetype.Vanguard, sp["golem"],
                new[]{(mv["rock_blast"],mv["stone_edge"]),(mv["rollout"],mv["rock_polish"]),(mv["stealth_rock"],mv["body_press"])},
                ab["tough_claws"], "§5.3.4");

            return d;
        }

        // ── Pass 2 — Wire branches back to source species ─────────────────────

        static void SeedSpecies_Pass2(
            Dictionary<string, PokemonSpeciesSO> sp,
            Dictionary<string, EvolutionBranchSO> br)
        {
            // Starters — Vanguard branch only; sub-branches (A1/A2) via SubBranches on mid-form
            WireBranches(sp["bulbasaur"],   br["bulbasaur_vanguard"]);
            WireBranches(sp["ivysaur_v"],   br["ivysaur_va1"], br["ivysaur_va2"]);
            WireBranches(sp["charmander"],  br["charmander_vanguard"]);
            WireBranches(sp["charmeleon_v"],br["charmeleon_va1"], br["charmeleon_va2"]);
            WireBranches(sp["squirtle"],    br["squirtle_vanguard"]);
            WireBranches(sp["wartortle_v"], br["wartortle_va1"], br["wartortle_va2"]);
            // Wild lines — single branch each
            WireBranches(sp["caterpie"],  br["caterpie_evolve"]);
            WireBranches(sp["metapod"],   br["metapod_evolve"]);
            // Butterfree, Pidgeot, Golem — final forms; no outbound branches
            WireBranches(sp["pidgey"],    br["pidgey_evolve"]);
            WireBranches(sp["pidgeotto"], br["pidgeotto_evolve"]);
            WireBranches(sp["geodude"],   br["geodude_evolve"]);
            WireBranches(sp["graveler"],  br["graveler_evolve"]);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        // ── Asset creation ────────────────────────────────────────────────────

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

        // ── Ability factory ───────────────────────────────────────────────────

        static AbilitySO Ab(string folder, string id, string displayName,
            AbilityCategory cat, string desc, string gddRef)
        {
            var a = CreateSO<AbilitySO>($"{folder}/{id}.asset");
            a.AbilityId   = id;
            a.DisplayName = displayName;
            a.Category    = cat;
            a.Description = desc;
            a.GDDReference= gddRef;
            EditorUtility.SetDirty(a);
            return a;
        }

        // ── Growth curve factory ──────────────────────────────────────────────

        // Per §4.1.1 — 4 stats only: HP, Atk, Def, Speed (no SpAtk/SpDef).
        static StatGrowthCurveSO Crv(string folder, string id,
            int hp, int atk, int def, int spe)
        {
            var c = CreateSO<StatGrowthCurveSO>($"{folder}/{id}.asset");
            c.HPGrowthPerLevel      = Flat(hp);
            c.AttackGrowthPerLevel  = Flat(atk);
            c.DefenseGrowthPerLevel = Flat(def);
            c.SpeedGrowthPerLevel   = Flat(spe);
            EditorUtility.SetDirty(c);
            return c;
        }

        static int[] Flat(int v) { var a = new int[50]; for(int i=0;i<50;i++) a[i]=v; return a; }

        // ── Move factory ──────────────────────────────────────────────────────

        static MoveSO Mv(string folder, string id, string displayName,
            PokemonType type, MoveRole role, MoveRange range, PositionalModifier mod,
            int ap, int power, bool alwaysCrit = false,
            string flavor = null, string gdd = "§5.3.6")
        {
            var m = CreateSO<MoveSO>($"{folder}/{id}.asset");
            m.MoveId      = id;
            m.DisplayName = displayName;
            m.Type        = type;
            m.Role        = role;
            m.Range       = range;
            m.Modifier    = mod;
            m.APCost      = ap;
            m.BasePower   = power;
            m.AlwaysCrit  = alwaysCrit;
            m.RangeModifierMultiplier = (range == MoveRange.Ranged) ? 0.75f : 1f;
            m.FlavorText  = flavor ?? string.Empty;
            m.GDDReference= gdd;
            m.Effects     = new System.Collections.Generic.List<MoveEffectSO>();
            EditorUtility.SetDirty(m);
            return m;
        }

        // ── Effect sub-asset helpers ───────────────────────────────────────────

        static void StatusRider(MoveSO m, StatusCondition s, float chance)
        {
            var fx = ScriptableObject.CreateInstance<StatusRiderEffectSO>();
            fx.StatusToApply     = s;
            fx.ApplicationChance = chance;
            fx.name = $"{m.MoveId}_{s}Rider";
            AssetDatabase.AddObjectToAsset(fx, m);
            m.Effects.Add(fx);
            EditorUtility.SetDirty(m);
        }

        static void Debuff(MoveSO m, Stat stat, int stages)
        {
            var fx = ScriptableObject.CreateInstance<DebuffTargetEffectSO>();
            fx.TargetStat  = stat;
            fx.StageChange = stages;
            fx.name = $"{m.MoveId}_Debuff{stat}";
            AssetDatabase.AddObjectToAsset(fx, m);
            m.Effects.Add(fx);
            EditorUtility.SetDirty(m);
        }

        static void Buff(MoveSO m, Stat stat, int stages)
        {
            var fx = ScriptableObject.CreateInstance<BuffSelfEffectSO>();
            fx.TargetStat  = stat;
            fx.StageChange = stages;
            fx.name = $"{m.MoveId}_Buff{stat}";
            AssetDatabase.AddObjectToAsset(fx, m);
            m.Effects.Add(fx);
            EditorUtility.SetDirty(m);
        }

        static void Heal(MoveSO m, float pct, bool self, int dur)
        {
            var fx = ScriptableObject.CreateInstance<HealEffectSO>();
            fx.PercentageOfMaxHP = pct;
            fx.HealSelf          = self;
            fx.DurationTurns     = dur;
            fx.name = $"{m.MoveId}_Heal";
            AssetDatabase.AddObjectToAsset(fx, m);
            m.Effects.Add(fx);
            EditorUtility.SetDirty(m);
        }

        static void DrawCards(MoveSO m, int count)
        {
            var fx = ScriptableObject.CreateInstance<DrawCardsEffectSO>();
            fx.CardsToDrawBonus = count;
            fx.name = $"{m.MoveId}_DrawCards{count}";
            AssetDatabase.AddObjectToAsset(fx, m);
            m.Effects.Add(fx);
            EditorUtility.SetDirty(m);
        }

        // ── Species factory ───────────────────────────────────────────────────

        static PokemonSpeciesSO Sp(string folder, string assetName, string displayName,
            string speciesId, PokemonType[] types, BaseStats baseStats,
            StatGrowthCurveSO curve, MoveSO[] learnset, MoveSO masteryMove,
            AbilitySO primaryAbility, AbilitySO secondaryAbility = null,
            RarityTier wildRarity = RarityTier.Common, Biome[] biomes = null)
        {
            var s = CreateSO<PokemonSpeciesSO>($"{folder}/{assetName}.asset");
            s.SpeciesId      = speciesId;
            s.DisplayName    = displayName;
            s.Types          = new System.Collections.Generic.List<PokemonType>(types);
            s.BaseStats      = baseStats;
            s.GrowthCurve    = curve;
            s.Branches       = new System.Collections.Generic.List<EvolutionBranchSO>();
            s.BaseLearnset   = new System.Collections.Generic.List<MoveSO>(learnset);
            s.TutorLearnset  = new System.Collections.Generic.List<MoveSO>();
            s.TMCompatibility= new System.Collections.Generic.List<TMSO>();
            s.MasteryMove    = masteryMove;
            s.PrimaryAbility = primaryAbility;
            // Secondary ability stored as the Branch's GrantedAbility — stored here for
            // reference by inspector; runtime wiring done by BranchSO in Epic 4.
            // We annotate it in GDDReference so it's visible in the inspector.
            s.WildRarity     = wildRarity;
            s.SpawnBiomes    = biomes != null
                ? new System.Collections.Generic.List<Biome>(biomes)
                : new System.Collections.Generic.List<Biome>();
            s.StatusImmunities = new System.Collections.Generic.List<StatusCondition>();
            s.GDDReference   = secondaryAbility != null
                ? $"§5.3 | Secondary ability (branch): {secondaryAbility.DisplayName}"
                : "§5.3";
            EditorUtility.SetDirty(s);
            return s;
        }

        // ── Branch factory ────────────────────────────────────────────────────

        // upgrades: (oldMove, newMove) pairs — in-place pool upgrades per §5.3.5 + §5.10.
        // newMoves: additional moves added to pool on evolution (null = none).
        static EvolutionBranchSO Br(string folder, string id, string displayName,
            BranchArchetype archetype, PokemonSpeciesSO evolvedSpecies,
            (MoveSO oldMove, MoveSO newMove)[] upgrades, AbilitySO grantedAbility, string gddRef,
            MoveSO[] newMoves = null)
        {
            var b = CreateSO<EvolutionBranchSO>($"{folder}/{id}.asset");
            b.BranchId      = id;
            b.DisplayName   = displayName;
            b.Archetype     = archetype;
            b.EvolvedSpecies= evolvedSpecies;
            b.MoveUpgrades  = new System.Collections.Generic.List<MoveUpgradePair>();
            foreach (var (oldMove, newMove) in upgrades)
                b.MoveUpgrades.Add(new MoveUpgradePair { OldMove = oldMove, NewMove = newMove });
            b.NewMoves      = newMoves != null
                ? new System.Collections.Generic.List<MoveSO>(newMoves)
                : new System.Collections.Generic.List<MoveSO>();
            b.GrantedAbility= grantedAbility;
            b.SubBranches   = new System.Collections.Generic.List<EvolutionBranchSO>();
            b.GDDReference  = gddRef;
            EditorUtility.SetDirty(b);
            return b;
        }

        static void WireBranches(PokemonSpeciesSO source, params EvolutionBranchSO[] branches)
        {
            source.Branches = new System.Collections.Generic.List<EvolutionBranchSO>(branches);
            EditorUtility.SetDirty(source);
        }

        // ── Base stats shorthand ──────────────────────────────────────────────

        // Per §4.1.1 — 4 stats only: HP, Atk, Def, Speed.
        static BaseStats BS(int hp, int atk, int def, int spe) =>
            new BaseStats { BaseHP=hp, BaseAtk=atk, BaseDef=def, BaseSpd=spe };
    }
}
#endif
