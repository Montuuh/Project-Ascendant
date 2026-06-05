using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.8 — save system facade. Three save layers: Meta, Run, Settings (§9.8.1).
    // Serialization: JSON via JsonUtility for the skeleton; binary optimization post-VS.
    // Atomicity: write-to-temp → verify-checksum → rename (§9.8.4).
    // SaveSystem is a static class — no Services registration needed.
    public static class SaveSystem
    {
        public const int SCHEMA_VERSION = 1;

        // Per §9.8.2 — canonical save directory. Override for testing.
        public static string SaveDirectoryOverride { get; set; }

        private static string Dir => SaveDirectoryOverride
            ?? Path.Combine(Application.persistentDataPath, "ProjectAscendant");

        private static string MetaPath      => Path.Combine(Dir, "meta.dat");
        private static string MetaBakPath   => Path.Combine(Dir, "meta.dat.bak");
        private static string PokedexPath  => Path.Combine(Dir, "bestiary.dat");
        private static string PokedexBak   => Path.Combine(Dir, "bestiary.dat.bak");
        private static string RunPath       => Path.Combine(Dir, "run-current.dat");
        private static string SettingsPath  => Path.Combine(Dir, "settings.json");

        // Per §9.8.1 — save MetaProgressionSO. Triggered after run end + Pokémart purchase.
        public static void SaveMeta(MetaProgressionSO meta)
        {
            AtomicWrite(MetaPath, MetaBakPath, JsonUtility.ToJson(meta));
        }

        // Per §9.8.1 — load MetaProgressionSO. Falls back to backup on corruption.
        public static MetaProgressionSO LoadMeta()
        {
            string dataJson = AtomicRead(MetaPath, MetaBakPath);
            if (dataJson == null) return null;

            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            JsonUtility.FromJsonOverwrite(dataJson, meta);
            return meta;
        }

        // Per §6.9 / §9.8.1 — persist PokedexProgressSO (per-species kill counts + tiers). Saved at
        // run-end alongside meta; last-known-good backup retained like meta.
        public static void SavePokedex(PokedexProgressSO bestiary)
        {
            AtomicWrite(PokedexPath, PokedexBak, JsonUtility.ToJson(bestiary));
        }

        // Per §6.9 — load PokedexProgressSO; falls back to backup on corruption, null if absent.
        public static PokedexProgressSO LoadPokedex()
        {
            string dataJson = AtomicRead(PokedexPath, PokedexBak);
            if (dataJson == null) return null;

            PokedexProgressSO bestiary = ScriptableObject.CreateInstance<PokedexProgressSO>();
            JsonUtility.FromJsonOverwrite(dataJson, bestiary);
            return bestiary;
        }

        // Per §9.8.1 + gap #43 — save run-state only (no team). Back-compat convenience; the live run
        // uses the Box-aware overload so the team persists too.
        public static void SaveRun(RunStateSO run)
        {
            SaveRun(run, box: null, boxCapacity: 0);
        }

        // Per §9.8.1 + gap #43 — save RunStateSO + the full Box (team) after every Node entry.
        // Serialized through RunSaveDTO so every nested SO reference persists as a stable ID, not an
        // unstable instanceID. Box is the run's live roster (RunContext.Box.Members).
        public static void SaveRun(RunStateSO run, IReadOnlyList<PokemonInstance> box, int boxCapacity)
        {
            RunSaveDTO dto = new()
            {
                Run         = RunStateDTO.Capture(run),
                Box         = PokemonInstanceDTO.CaptureBox(box),
                BoxCapacity = boxCapacity,
            };
            AtomicWrite(RunPath, bakPath: null, JsonUtility.ToJson(dto));
        }

        // Per §9.8.1 + gap #43 — load the full run save. SO references (run-state + team) are resolved
        // back from their stored IDs via the registry (built from the run's RunContentCatalogSO);
        // Box instances are rebuilt via the factory pool. Returns null if missing or corrupt (run is
        // forfeited).
        public static RunSaveData LoadRun(RunContentRegistry registry, PokemonInstanceFactory factory)
        {
            string dataJson = AtomicRead(RunPath, bakPath: null);
            if (dataJson == null) return null;

            RunSaveDTO dto = JsonUtility.FromJson<RunSaveDTO>(dataJson);
            if (dto == null) return null;

            return new RunSaveData
            {
                Run         = dto.Run?.Rebuild(registry),
                Box         = PokemonInstanceDTO.RebuildBox(dto.Box, registry, factory),
                BoxCapacity = dto.BoxCapacity,
            };
        }

        // Per §9.8.1 — delete the in-progress run save. Called at run end (victory/defeat) so a saved
        // file always denotes a resumable in-progress run.
        public static void DeleteRun()
        {
            if (File.Exists(RunPath)) File.Delete(RunPath);
        }

        // Per §9.8.1 + gap #43 — does a valid (checksum-passing) in-progress run save exist? Used by the
        // Main Menu to enable/disable Continue. Cheap: a corrupt/absent file reads as no save.
        public static bool HasRun() => AtomicRead(RunPath, bakPath: null) != null;

        // Per §9.8.1 + gap #43 — load the run save INTO an existing RunStateSO (preserving its identity,
        // so live references like LoadoutManager stay valid). Returns false if no/corrupt save. The Box
        // (team) is returned via out params for the caller to install into its RunContext.Box.
        public static bool LoadRunInto(
            RunStateSO target, RunContentRegistry registry, PokemonInstanceFactory factory,
            out List<PokemonInstance> box, out int boxCapacity)
        {
            box = null;
            boxCapacity = 0;
            if (target == null) return false;

            string dataJson = AtomicRead(RunPath, bakPath: null);
            if (dataJson == null) return false;

            RunSaveDTO dto = JsonUtility.FromJson<RunSaveDTO>(dataJson);
            if (dto?.Run == null) return false;

            dto.Run.ApplyTo(target, registry);
            box = PokemonInstanceDTO.RebuildBox(dto.Box, registry, factory);
            boxCapacity = dto.BoxCapacity;
            return true;
        }

        // Per §9.8.1 — save Settings as JSON on change.
        public static void SaveSettings(SettingsSO settings)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings, prettyPrint: true));
        }

        // Per §9.8.4 — write-to-temp → verify-checksum → atomic-rename.
        // If bakPath is non-null, the prior file is preserved as a last-known-good backup.
        private static void AtomicWrite(string path, string bakPath, string dataJson)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            uint checksum = ComputeChecksum(dataJson);
            SaveHeader header = new()
            {
                SchemaVersion = SCHEMA_VERSION,
                GameVersion   = Application.version,
                Timestamp     = DateTime.UtcNow.ToString("o"),
                Checksum      = checksum,
            };

            string wrapped  = JsonUtility.ToJson(header) + "\n" + dataJson;
            string tmpPath  = path + ".tmp";
            File.WriteAllText(tmpPath, wrapped);

            // Verify: read back and confirm checksum before committing.
            string readBack = File.ReadAllText(tmpPath);
            var (readHeader, readData) = UnwrapHeader(readBack);
            if (readHeader.Checksum != ComputeChecksum(readData))
            {
                File.Delete(tmpPath);
                throw new IOException($"[SaveSystem] Checksum mismatch after temp write: {tmpPath}");
            }

            // Retain last-known-good backup before overwriting the primary.
            if (bakPath != null && File.Exists(path))
                File.Copy(path, bakPath, overwrite: true);

            // Atomic rename: delete-then-move (same-volume rename on Windows is OS-atomic).
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmpPath, path);
        }

        // Reads primary file; falls back to bakPath if primary is missing or corrupt.
        // Returns null if both are unavailable or corrupt.
        private static string AtomicRead(string path, string bakPath)
        {
            static string TryRead(string filePath)
            {
                if (!File.Exists(filePath)) return null;
                try
                {
                    string content = File.ReadAllText(filePath);
                    var (header, dataJson) = UnwrapHeader(content);
                    return header.Checksum == ComputeChecksum(dataJson) ? dataJson : null;
                }
                catch { return null; }
            }

            return TryRead(path) ?? (bakPath != null ? TryRead(bakPath) : null);
        }

        private static (SaveHeader header, string dataJson) UnwrapHeader(string content)
        {
            int nl = content.IndexOf('\n');
            if (nl < 0) throw new FormatException("[SaveSystem] Save file missing header separator.");
            SaveHeader header = JsonUtility.FromJson<SaveHeader>(content[..nl]);
            return (header, content[(nl + 1)..]);
        }

        // FNV-1a 32-bit checksum — matches RNGStreams.FNV1a for consistency.
        public static uint ComputeChecksum(string data)
        {
            uint hash = 2166136261u;
            foreach (char c in data)
            {
                hash ^= (uint)c;
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
