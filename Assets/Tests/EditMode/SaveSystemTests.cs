using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.8 + Task 2.7.5 — SaveSystem unit tests.
    public class SaveSystemTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "PATest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            SaveSystem.SaveDirectoryOverride = _testDir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.SaveDirectoryOverride = null;
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        // ── Meta save / load ─────────────────────────────────────────────────────

        [Test]
        public void SaveSystem_SaveMeta_WritesFileOnDisk()
        {
            // Per §9.8.1 — SaveMeta must produce a file at the canonical meta path.
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            SaveSystem.SaveMeta(meta);
            Assert.That(File.Exists(Path.Combine(_testDir, "meta.dat")), Is.True);
            Object.DestroyImmediate(meta);
        }

        [Test]
        public void SaveSystem_MetaRoundTrip_PreservesData()
        {
            // Per §9.8 + §6.3 (Task 11.7.5) — LoadMeta after SaveMeta must round-trip the Trainer
            // progression fields (Level/XP/Tokens) committed by the run-end flow.
            MetaProgressionSO original = ScriptableObject.CreateInstance<MetaProgressionSO>();
            original.TrainerLevel = 42;
            original.TrainerXP = 117151;
            original.TrainerTokens = 33;
            SaveSystem.SaveMeta(original);

            MetaProgressionSO loaded = SaveSystem.LoadMeta();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.TrainerLevel, Is.EqualTo(42));
            Assert.That(loaded.TrainerXP, Is.EqualTo(117151));
            Assert.That(loaded.TrainerTokens, Is.EqualTo(33));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
        }

        // Per §4.3.9.2 — unlocked Mastery moves persist across runs in meta.dat.
        [Test]
        public void SaveSystem_Meta_UnlockedMasteries_RoundTrip()
        {
            MetaProgressionSO original = ScriptableObject.CreateInstance<MetaProgressionSO>();
            Assert.That(original.IsMasteryUnlocked("wartortle_mastery"), Is.False);
            Assert.That(original.UnlockMastery("wartortle_mastery"), Is.True);
            Assert.That(original.UnlockMastery("wartortle_mastery"), Is.False, "Idempotent.");
            Assert.That(original.IsMasteryUnlocked("wartortle_mastery"), Is.True);
            SaveSystem.SaveMeta(original);

            MetaProgressionSO loaded = SaveSystem.LoadMeta();
            Assert.That(loaded.IsMasteryUnlocked("wartortle_mastery"), Is.True);

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
        }

        [Test]
        public void SaveSystem_SaveMeta_Twice_CreatesBackup()
        {
            // Per §9.8.4 — each overwrite must retain last-known-good backup.
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            SaveSystem.SaveMeta(meta); // first write — no backup yet
            SaveSystem.SaveMeta(meta); // second write — backup created
            Assert.That(File.Exists(Path.Combine(_testDir, "meta.dat.bak")), Is.True);
            Object.DestroyImmediate(meta);
        }

        [Test]
        public void SaveSystem_CorruptedMeta_FallsBackToBackup()
        {
            // Per §9.8.4 — corrupted primary must yield data from last-known-good backup.
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            meta.TrainerLevel = 7;
            SaveSystem.SaveMeta(meta); // write → becomes backup on second write
            SaveSystem.SaveMeta(meta); // second write creates meta.dat.bak with TrainerLevel=7

            File.WriteAllText(Path.Combine(_testDir, "meta.dat"), "CORRUPTED");

            MetaProgressionSO loaded = SaveSystem.LoadMeta();
            Assert.That(loaded, Is.Not.Null, "Must recover data from backup");
            Assert.That(loaded.TrainerLevel, Is.EqualTo(7));

            Object.DestroyImmediate(meta);
            Object.DestroyImmediate(loaded);
        }

        [Test]
        public void SaveSystem_MissingMeta_ReturnsNull()
        {
            // Edge case: no save file → LoadMeta returns null, no exception.
            MetaProgressionSO result = SaveSystem.LoadMeta();
            Assert.That(result, Is.Null);
        }

        // Per §6.9 + Task 11.8.1 — Pokedex persists round-trip with kill counts.
        [Test]
        public void SaveSystem_PokedexRoundTrip_PreservesKillCounts()
        {
            PokedexProgressSO b = ScriptableObject.CreateInstance<PokedexProgressSO>();
            b.RecordKill("pidgey", RarityTier.Common);
            b.RecordKill("pidgey", RarityTier.Common);
            SaveSystem.SavePokedex(b);

            PokedexProgressSO loaded = SaveSystem.LoadPokedex();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.GetOrCreate("pidgey").TimesDefeated, Is.EqualTo(2));

            Object.DestroyImmediate(b);
            Object.DestroyImmediate(loaded);
        }

        [Test]
        public void SaveSystem_MissingPokedex_ReturnsNull()
        {
            Assert.That(SaveSystem.LoadPokedex(), Is.Null);
        }

        // ── Run save / load ──────────────────────────────────────────────────────

        [Test]
        public void SaveSystem_SaveRun_WritesRunFile()
        {
            // Per §9.8.1 — SaveRun must produce a file at the canonical run path.
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            SaveSystem.SaveRun(run);
            Assert.That(File.Exists(Path.Combine(_testDir, "run-current.dat")), Is.True);
            Object.DestroyImmediate(run);
        }

        [Test]
        public void SaveSystem_RunRoundTrip_PreservesData()
        {
            // Per §9.8 — LoadRun after SaveRun must return identical data.
            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.RunSeed = 12345;
            SaveSystem.SaveRun(original);

            RunSaveData saved = SaveSystem.LoadRun(new RunContentRegistry(), new PokemonInstanceFactory());
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.Run, Is.Not.Null);
            Assert.That(saved.Run.RunSeed, Is.EqualTo(12345));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(saved.Run);
        }

        // Per §9.8 + gap #43 — nested SO references must survive a save round-trip. JsonUtility
        // serializes them as unstable instanceIDs; SaveRun/LoadRun route through RunStateDTO so they
        // persist as stable IDs and re-resolve via the registry to the SAME authored assets.
        [Test]
        public void SaveSystem_RunRoundTrip_ResolvesSOReferencesById()
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            relic.Id = "coin_pouch";
            ConsumableSO potion = ScriptableObject.CreateInstance<ConsumableSO>();
            potion.Id = "potion";
            BadgeSO badge = ScriptableObject.CreateInstance<BadgeSO>();
            badge.BadgeId = "boulder";
            DifficultyModifierSO diff = ScriptableObject.CreateInstance<DifficultyModifierSO>();
            diff.ModifierId = "ironman";

            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.RunSeed = 999;
            original.PokeDollars = 250;
            original.HeldRelics = new System.Collections.Generic.List<RelicSO> { relic };
            original.Inventory = new System.Collections.Generic.List<ConsumableSO> { potion };
            original.EarnedBadges = new System.Collections.Generic.List<BadgeSO> { badge };
            original.ActiveDifficultyModifiers = new System.Collections.Generic.List<DifficultyModifierSO> { diff };
            SaveSystem.SaveRun(original);

            RunContentRegistry registry = new();
            registry.RegisterRelic(relic);
            registry.RegisterConsumable(potion);
            registry.RegisterBadge(badge);
            registry.RegisterDifficultyModifier(diff);

            RunStateSO loaded = SaveSystem.LoadRun(registry, new PokemonInstanceFactory()).Run;

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.RunSeed, Is.EqualTo(999));
            Assert.That(loaded.PokeDollars, Is.EqualTo(250));
            // Resolved back to the SAME asset instances, not broken instanceID clones.
            Assert.That(loaded.HeldRelics, Has.Count.EqualTo(1));
            Assert.That(loaded.HeldRelics[0], Is.SameAs(relic));
            Assert.That(loaded.Inventory[0], Is.SameAs(potion));
            Assert.That(loaded.EarnedBadges[0], Is.SameAs(badge));
            Assert.That(loaded.ActiveDifficultyModifiers[0], Is.SameAs(diff));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
            Object.DestroyImmediate(relic);
            Object.DestroyImmediate(potion);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(diff);
        }

        // Per gap #43 — a saved ID absent from the registry must drop gracefully (logged), not crash
        // the load. A missing item is recoverable; a forfeited run is not.
        [Test]
        public void SaveSystem_RunRoundTrip_UnknownIdDropsGracefully()
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            relic.Id = "ghost_relic";
            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.HeldRelics = new System.Collections.Generic.List<RelicSO> { relic };
            SaveSystem.SaveRun(original);

            // Empty registry — the ID cannot resolve.
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ghost_relic"));
            RunStateSO loaded = SaveSystem.LoadRun(new RunContentRegistry(), new PokemonInstanceFactory()).Run;

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.HeldRelics, Has.Count.EqualTo(0));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
            Object.DestroyImmediate(relic);
        }

        [Test]
        public void SaveSystem_CorruptedRun_ReturnsNull()
        {
            // Per §9.8.4 — corrupted run save returns null (run is forfeited; no run backup).
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            SaveSystem.SaveRun(run);
            File.WriteAllText(Path.Combine(_testDir, "run-current.dat"), "CORRUPTED");

            RunSaveData loaded = SaveSystem.LoadRun(new RunContentRegistry(), new PokemonInstanceFactory());
            Assert.That(loaded, Is.Null);
            Object.DestroyImmediate(run);
        }

        // ── Team / Box round-trip (gap #43 team persistence) ────────────────────────

        // Per §9.8 + §2.3 + gap #43 — the live team (Box of PokemonInstances) must survive a save
        // round-trip with every SO ref (species/moves/ability/held/branch) re-resolved to the SAME
        // authored asset, and all runtime state (HP/XP/level/trauma/stages/status/stage) preserved.
        [Test]
        public void SaveSystem_RunRoundTrip_RestoresTeamFromBox()
        {
            MoveSO tackle = ScriptableObject.CreateInstance<MoveSO>();   tackle.MoveId = "tackle";
            MoveSO vine   = ScriptableObject.CreateInstance<MoveSO>();   vine.MoveId   = "vine_whip";
            MoveSO mastery= ScriptableObject.CreateInstance<MoveSO>();   mastery.MoveId= "solar_mastery";
            MoveSO razor  = ScriptableObject.CreateInstance<MoveSO>();   razor.MoveId  = "razor_leaf"; // branch new move
            AbilitySO overgrow = ScriptableObject.CreateInstance<AbilitySO>(); overgrow.AbilityId = "overgrow";
            HeldItemSO leftovers = ScriptableObject.CreateInstance<HeldItemSO>(); leftovers.Id = "leftovers";

            EvolutionBranchSO branch = ScriptableObject.CreateInstance<EvolutionBranchSO>();
            branch.BranchId = "ivysaur_vanguard";
            branch.NewMoves = new System.Collections.Generic.List<MoveSO> { razor };

            PokemonSpeciesSO bulbasaur = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            bulbasaur.SpeciesId = "bulbasaur";
            bulbasaur.BaseLearnset = new System.Collections.Generic.List<MoveSO> { tackle, vine };
            bulbasaur.PrimaryAbility = overgrow;
            bulbasaur.MasteryMove = mastery;
            bulbasaur.Branches = new System.Collections.Generic.List<EvolutionBranchSO> { branch };

            PokemonInstanceFactory factory = new();
            PokemonInstance mon = factory.RentEmpty();
            mon.Species = bulbasaur; mon.Level = 7; mon.CurrentHP = 18; mon.CurrentXP = 42; mon.TraumaStacks = 2;
            mon.CurrentMoves.Add(tackle); mon.CurrentMoves.Add(vine);
            mon.LearnedMoves.Add(tackle); mon.LearnedMoves.Add(vine); mon.LearnedMoves.Add(razor);
            mon.MasteryMove = mastery; mon.Ability = overgrow; mon.HeldItem = leftovers;
            mon.StatStages[Stat.Attack] = 1;
            mon.PrimaryStatus = StatusCondition.Burn; mon.PrimaryStatusTurnsRemaining = int.MaxValue;
            mon.CurrentStage = EvolutionStage.Basic; mon.SelectedBranch = branch;

            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            run.RunSeed = 7; run.LeadIndex = 0;
            run.ActiveTeamIndices = new System.Collections.Generic.List<int> { 0 };

            var box = new System.Collections.Generic.List<PokemonInstance> { mon };
            SaveSystem.SaveRun(run, box, boxCapacity: 6);

            RunContentRegistry registry = new();
            registry.RegisterSpeciesGraph(bulbasaur); // pulls moves/ability/mastery/branch+newmoves
            registry.RegisterHeldItem(leftovers);

            RunSaveData saved = SaveSystem.LoadRun(registry, factory);

            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.BoxCapacity, Is.EqualTo(6));
            Assert.That(saved.Run.ActiveTeamIndices[0], Is.EqualTo(0));
            Assert.That(saved.Box, Has.Count.EqualTo(1));

            PokemonInstance r = saved.Box[0];
            Assert.That(r.Species, Is.SameAs(bulbasaur));
            Assert.That(r.Level, Is.EqualTo(7));
            Assert.That(r.CurrentHP, Is.EqualTo(18));
            Assert.That(r.CurrentXP, Is.EqualTo(42));
            Assert.That(r.TraumaStacks, Is.EqualTo(2));
            Assert.That(r.CurrentMoves, Is.EquivalentTo(new[] { tackle, vine }));
            Assert.That(r.LearnedMoves, Is.EquivalentTo(new[] { tackle, vine, razor }));
            Assert.That(r.MasteryMove, Is.SameAs(mastery));
            Assert.That(r.Ability, Is.SameAs(overgrow));
            Assert.That(r.HeldItem, Is.SameAs(leftovers));
            Assert.That(r.SelectedBranch, Is.SameAs(branch));
            Assert.That(r.StatStages[Stat.Attack], Is.EqualTo(1));
            Assert.That(r.PrimaryStatus, Is.EqualTo(StatusCondition.Burn));
            Assert.That(r.PrimaryStatusTurnsRemaining, Is.EqualTo(int.MaxValue));
            Assert.That(r.CurrentStage, Is.EqualTo(EvolutionStage.Basic));

            Object.DestroyImmediate(run); Object.DestroyImmediate(saved.Run);
            Object.DestroyImmediate(tackle); Object.DestroyImmediate(vine);
            Object.DestroyImmediate(mastery); Object.DestroyImmediate(razor);
            Object.DestroyImmediate(overgrow); Object.DestroyImmediate(leftovers);
            Object.DestroyImmediate(branch); Object.DestroyImmediate(bulbasaur);
        }

        // Per §5.3.5 + gap #43 — final-form sub-branch builds (e.g. Blastoise A1/A2) intentionally
        // share a SpeciesId; the unique SelectedBranch disambiguates them. Resolution must restore the
        // EXACT evolved-form asset via the branch, not whichever shares the colliding SpeciesId.
        [Test]
        public void SaveSystem_RunRoundTrip_EvolvedForm_DisambiguatesByBranch()
        {
            // Two distinct final-form species assets that SHARE a SpeciesId (mirrors authored data).
            PokemonSpeciesSO blastoiseA1 = ScriptableObject.CreateInstance<PokemonSpeciesSO>(); blastoiseA1.SpeciesId = "blastoise";
            PokemonSpeciesSO blastoiseA2 = ScriptableObject.CreateInstance<PokemonSpeciesSO>(); blastoiseA2.SpeciesId = "blastoise";
            EvolutionBranchSO va1 = ScriptableObject.CreateInstance<EvolutionBranchSO>(); va1.BranchId = "wartortle_va1"; va1.EvolvedSpecies = blastoiseA1;
            EvolutionBranchSO va2 = ScriptableObject.CreateInstance<EvolutionBranchSO>(); va2.BranchId = "wartortle_va2"; va2.EvolvedSpecies = blastoiseA2;

            PokemonInstanceFactory factory = new();
            PokemonInstance mon = factory.RentEmpty();
            mon.Species = blastoiseA1; mon.Level = 36; mon.CurrentHP = 1; mon.SelectedBranch = va1; // chose A1
            mon.CurrentStage = EvolutionStage.Stage2;

            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            SaveSystem.SaveRun(run, new System.Collections.Generic.List<PokemonInstance> { mon }, 6);

            RunContentRegistry registry = new();
            registry.RegisterSpecies(blastoiseA2); // A2 registered LAST under "blastoise" (would win an id lookup)
            registry.RegisterSpecies(blastoiseA1);
            registry.RegisterBranch(va1);
            registry.RegisterBranch(va2);

            RunSaveData saved = SaveSystem.LoadRun(registry, factory);

            // Branch-first resolution restores the A1 asset, not the id-colliding A2.
            Assert.That(saved.Box[0].Species, Is.SameAs(blastoiseA1));
            Assert.That(saved.Box[0].SelectedBranch, Is.SameAs(va1));

            Object.DestroyImmediate(run); Object.DestroyImmediate(saved.Run);
            Object.DestroyImmediate(blastoiseA1); Object.DestroyImmediate(blastoiseA2);
            Object.DestroyImmediate(va1); Object.DestroyImmediate(va2);
        }

        // Per gap #43 — HasRun reflects whether a valid in-progress save exists (drives Continue).
        [Test]
        public void SaveSystem_HasRun_TrueAfterSave_FalseAfterDelete()
        {
            Assert.That(SaveSystem.HasRun(), Is.False, "No save yet.");
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            SaveSystem.SaveRun(run, null, 6);
            Assert.That(SaveSystem.HasRun(), Is.True, "Save written.");
            SaveSystem.DeleteRun();
            Assert.That(SaveSystem.HasRun(), Is.False, "Save deleted.");
            Object.DestroyImmediate(run);
        }

        // Per gap #43 — LoadRunInto applies the save onto an EXISTING RunStateSO (identity preserved).
        [Test]
        public void SaveSystem_LoadRunInto_AppliesOntoExistingInstance()
        {
            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.RunSeed = 4242;
            original.PokeDollars = 90;
            SaveSystem.SaveRun(original, null, 6);

            RunStateSO target = ScriptableObject.CreateInstance<RunStateSO>();
            bool ok = SaveSystem.LoadRunInto(target, new RunContentRegistry(), new PokemonInstanceFactory(),
                out System.Collections.Generic.List<PokemonInstance> box, out int cap);

            Assert.That(ok, Is.True);
            Assert.That(target.RunSeed, Is.EqualTo(4242)); // same instance, mutated in place
            Assert.That(target.PokeDollars, Is.EqualTo(90));
            Assert.That(cap, Is.EqualTo(6));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(target);
        }

        // Per gap #43 — ResetToNewRun clears run state in place but keeps the new seed + instance.
        [Test]
        public void RunStateSO_ResetToNewRun_ClearsStateKeepsSeed()
        {
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            run.RunSeed = 1; run.PokeDollars = 500; run.CurrentLayerIndex = 5; run.LeadIndex = 2;
            run.HeldRelics = new System.Collections.Generic.List<RelicSO> { ScriptableObject.CreateInstance<RelicSO>() };
            run.ActiveTeamIndices = new System.Collections.Generic.List<int> { 0, 1 };

            run.ResetToNewRun(7777);

            Assert.That(run.RunSeed, Is.EqualTo(7777));
            Assert.That(run.PokeDollars, Is.EqualTo(0));
            Assert.That(run.CurrentLayerIndex, Is.EqualTo(0));
            Assert.That(run.LeadIndex, Is.EqualTo(0));
            Assert.That(run.HeldRelics, Is.Null);
            Assert.That(run.ActiveTeamIndices, Is.Null);

            Object.DestroyImmediate(run);
        }

        // ── RNG cursor persistence (§9.8.6 / gap #45, CL-022) ───────────────────────

        // Per §9.8.6 — the 5 RNG stream cursors must survive a run save round-trip so a resume does
        // not re-roll already-consumed encounters/loot/mystery/combat. (uint via JsonUtility.)
        [Test]
        public void SaveSystem_RunRoundTrip_PersistsRngCursors()
        {
            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.RunSeed = 555;
            original.RngCursors = new RNGCursors { Map = 111u, Combat = 222u, Loot = 333u, Mystery = 444u, Encounter = 555u };
            SaveSystem.SaveRun(original);

            RunStateSO loaded = SaveSystem.LoadRun(new RunContentRegistry(), new PokemonInstanceFactory()).Run;
            Assert.That(loaded.RngCursors.Map, Is.EqualTo(111u));
            Assert.That(loaded.RngCursors.Combat, Is.EqualTo(222u));
            Assert.That(loaded.RngCursors.Loot, Is.EqualTo(333u));
            Assert.That(loaded.RngCursors.Mystery, Is.EqualTo(444u));
            Assert.That(loaded.RngCursors.Encounter, Is.EqualTo(555u));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
        }

        // Per §9.8.6 — RestoreContentCursors restores the 4 content streams so they CONTINUE where the
        // save left off, but deliberately leaves MapRNG at its (re-derived) start so the replayed map
        // does not shift on resume.
        [Test]
        public void RNGStreams_RestoreContentCursors_ContinuesContentStreams_NotMap()
        {
            // A "live" run: advance every stream a few rolls, then snapshot the cursors.
            RNGStreams live = new(0xABCDEF);
            for (int i = 0; i < 5; i++) { live.MapRNG.NextUInt(); live.CombatRNG.NextUInt(); live.LootRNG.NextUInt(); live.MysteryRNG.NextUInt(); live.EncounterRNG.NextUInt(); }
            uint nextCombat = PeekNext(live.CombatRNG);   // what the live run would roll next
            uint nextLoot = PeekNext(live.LootRNG);
            RNGCursors saved = live.CaptureCursors();

            // Resume: fresh streams (Map re-derived to start), then restore the content cursors.
            RNGStreams resumed = new(0xABCDEF);
            uint mapStart = resumed.MapRNG.State;
            resumed.RestoreContentCursors(saved);

            // Content streams continue identically to the live run...
            Assert.That(resumed.CombatRNG.NextUInt(), Is.EqualTo(nextCombat));
            Assert.That(resumed.LootRNG.NextUInt(), Is.EqualTo(nextLoot));
            // ...but MapRNG is untouched (still at its fresh start — the map re-derives by replay).
            Assert.That(resumed.MapRNG.State, Is.EqualTo(mapStart));
            Assert.That(resumed.MapRNG.State, Is.Not.EqualTo(saved.Map));
        }

        private static uint PeekNext(GameRNG rng)
        {
            uint snapshot = rng.State;
            uint next = rng.NextUInt();
            rng.State = snapshot; // rewind — non-destructive peek
            return next;
        }

        // ── Legendary relic save round-trip (§8.3.7 / CL-021, gap B) ────────────────

        // Per §8.3.7 — a held Legendary relic (code-built catalog, not in catalog.Relics) must survive
        // a save round-trip once LegendaryRelicCatalog is registered on the resume path.
        [Test]
        public void SaveSystem_RunRoundTrip_HeldLegendary_SurvivesWhenCatalogRegistered()
        {
            System.Collections.Generic.List<RelicSO> legendaries = LegendaryRelicCatalog.BuildAll();
            RelicSO battleHardened = legendaries.Find(r => r.Id == "battle_hardened");
            Assert.That(battleHardened, Is.Not.Null);
            Assert.That(battleHardened.Rarity, Is.EqualTo(RarityTier.Legendary));

            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.HeldRelics = new System.Collections.Generic.List<RelicSO> { battleHardened };
            SaveSystem.SaveRun(original);

            // Resume path registers the code-built Legendary catalog (RunLauncher does this).
            RunContentRegistry registry = new();
            registry.RegisterRelics(LegendaryRelicCatalog.BuildAll());

            RunStateSO loaded = SaveSystem.LoadRun(registry, new PokemonInstanceFactory()).Run;
            Assert.That(loaded.HeldRelics, Has.Count.EqualTo(1));
            Assert.That(loaded.HeldRelics[0].Id, Is.EqualTo("battle_hardened"));
            Assert.That(loaded.HeldRelics[0].Rarity, Is.EqualTo(RarityTier.Legendary));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
            foreach (RelicSO r in legendaries) Object.DestroyImmediate(r);
        }

        // ── Naturalist's Lens biome round-trip (§7.3.1 / CL-018, gap C) ─────────────

        [Test]
        public void SaveSystem_RunRoundTrip_NaturalistLensBiome_ResolvesById()
        {
            BiomeSO cave = ScriptableObject.CreateInstance<BiomeSO>();
            cave.BiomeId = "cave";

            RunStateSO original = ScriptableObject.CreateInstance<RunStateSO>();
            original.NaturalistLensBiome = cave;
            SaveSystem.SaveRun(original);

            RunContentRegistry registry = new();
            registry.RegisterBiome(cave);

            RunStateSO loaded = SaveSystem.LoadRun(registry, new PokemonInstanceFactory()).Run;
            Assert.That(loaded.NaturalistLensBiome, Is.SameAs(cave));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
            Object.DestroyImmediate(cave);
        }

        // ── ShieldHP is combat-transient, never carried on a restore (§8.3.7 / gap D) ──

        [Test]
        public void PokemonInstance_Reset_ZeroesShieldHP()
        {
            PokemonInstanceFactory factory = new();
            PokemonInstance mon = factory.RentEmpty();
            mon.ShieldHP = 25;
            factory.Release(mon); // Release → Reset → returns to pool
            PokemonInstance reused = factory.RentEmpty(); // a pooled instance must not carry a stale shield
            Assert.That(reused.ShieldHP, Is.EqualTo(0));
        }

        // ── Meta: CL-019 Token / milestone state round-trips (gap E, verify-only) ───

        // Per §6.3.4/§6.3.5 (CL-019) — whole-object JsonUtility means new Meta fields auto-serialize;
        // prove ClaimedLevelMilestones + TrainerTokens actually round-trip.
        [Test]
        public void SaveSystem_Meta_ClaimedLevelMilestonesAndTokens_RoundTrip()
        {
            MetaProgressionSO original = ScriptableObject.CreateInstance<MetaProgressionSO>();
            original.TrainerTokens = 44;
            original.ClaimedLevelMilestones = new System.Collections.Generic.List<int> { 5, 10, 15 };
            SaveSystem.SaveMeta(original);

            MetaProgressionSO loaded = SaveSystem.LoadMeta();
            Assert.That(loaded.TrainerTokens, Is.EqualTo(44));
            Assert.That(loaded.ClaimedLevelMilestones, Is.EquivalentTo(new[] { 5, 10, 15 }));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
        }

        // ── Checksum ──────────────────────────────────────────────────────────────

        [Test]
        public void SaveSystem_ComputeChecksum_SameInput_SameOutput()
        {
            // Deterministic checksum required for save integrity verification.
            uint a = SaveSystem.ComputeChecksum("test-payload");
            uint b = SaveSystem.ComputeChecksum("test-payload");
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void SaveSystem_ComputeChecksum_DifferentInput_DifferentOutput()
        {
            uint a = SaveSystem.ComputeChecksum("payload-a");
            uint b = SaveSystem.ComputeChecksum("payload-b");
            Assert.That(a, Is.Not.EqualTo(b));
        }

        // ── Settings save / load (BACKLOG #47 — settings were write-only) ────────────

        [Test]
        public void SaveSystem_SaveSettings_WritesFileOnDisk()
        {
            // Per §9.8.1 — SaveSettings produces settings.json at the canonical path.
            SettingsSO s = ScriptableObject.CreateInstance<SettingsSO>();
            SaveSystem.SaveSettings(s);
            Assert.That(File.Exists(Path.Combine(_testDir, "settings.json")), Is.True);
            Object.DestroyImmediate(s);
        }

        [Test]
        public void SaveSystem_SettingsRoundTrip_PreservesFields()
        {
            // Per §9.8.7.6 (#47) — LoadSettings after SaveSettings restores every field.
            SettingsSO original = ScriptableObject.CreateInstance<SettingsSO>();
            original.MasterVolume = 0.42f;
            original.MusicVolume = 0.13f;
            original.ColorblindMode = true;
            original.ReducedMotion = true;
            original.SubtitlesEnabled = false;
            original.FullScreen = false;
            original.TargetFrameRate = 144;
            original.KeyBindingsJson = "{\"jump\":\"space\"}";
            SaveSystem.SaveSettings(original);

            SettingsSO loaded = ScriptableObject.CreateInstance<SettingsSO>();
            bool ok = SaveSystem.LoadSettings(loaded);

            Assert.That(ok, Is.True);
            Assert.That(loaded.MasterVolume, Is.EqualTo(0.42f).Within(0.0001f));
            Assert.That(loaded.MusicVolume, Is.EqualTo(0.13f).Within(0.0001f));
            Assert.That(loaded.ColorblindMode, Is.True);
            Assert.That(loaded.ReducedMotion, Is.True);
            Assert.That(loaded.SubtitlesEnabled, Is.False);
            Assert.That(loaded.FullScreen, Is.False);
            Assert.That(loaded.TargetFrameRate, Is.EqualTo(144));
            Assert.That(loaded.KeyBindingsJson, Is.EqualTo("{\"jump\":\"space\"}"));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
        }

        [Test]
        public void SaveSystem_LoadSettings_NoFile_KeepsDefaults_ReturnsFalse()
        {
            // Per §9.8.7.6 (#47) — first boot (no settings.json) keeps SO defaults, no throw.
            SettingsSO fresh = ScriptableObject.CreateInstance<SettingsSO>();
            float defaultMaster = fresh.MasterVolume;
            bool ok = SaveSystem.LoadSettings(fresh);

            Assert.That(ok, Is.False);
            Assert.That(fresh.MasterVolume, Is.EqualTo(defaultMaster));
            Object.DestroyImmediate(fresh);
        }
    }
}
