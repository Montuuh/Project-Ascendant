using System.IO;
using NUnit.Framework;
using UnityEngine;
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
            // Per §9.8 — LoadMeta after SaveMeta must return identical data.
            MetaProgressionSO original = ScriptableObject.CreateInstance<MetaProgressionSO>();
            original.TrainerLevel = 42;
            SaveSystem.SaveMeta(original);

            MetaProgressionSO loaded = SaveSystem.LoadMeta();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.TrainerLevel, Is.EqualTo(42));

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

            RunStateSO loaded = SaveSystem.LoadRun();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.RunSeed, Is.EqualTo(12345));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(loaded);
        }

        [Test]
        public void SaveSystem_CorruptedRun_ReturnsNull()
        {
            // Per §9.8.4 — corrupted run save returns null (run is forfeited; no run backup).
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            SaveSystem.SaveRun(run);
            File.WriteAllText(Path.Combine(_testDir, "run-current.dat"), "CORRUPTED");

            RunStateSO loaded = SaveSystem.LoadRun();
            Assert.That(loaded, Is.Null);
            Object.DestroyImmediate(run);
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
    }
}
