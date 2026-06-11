namespace ProjectAscendant.Core
{
    // Per §9.8.6 (gap #45) — a JsonUtility-safe snapshot of all 5 stream cursors, persisted in the
    // run save so a resume continues each stream exactly where it left off. Map is captured for
    // completeness but is NOT restored on resume (the map regenerates by deterministic replay —
    // §9.8.6); only the 4 content cursors are restored.
    [System.Serializable]
    public struct RNGCursors
    {
        public uint Map;
        public uint Combat;
        public uint Loot;
        public uint Mystery;
        public uint Encounter;
    }

    // Per §9.7.2 — five isolated RNG streams, each seeded deterministically from RunSeed.
    // Seed derivation: streamSeed = RunSeed XOR FNV1a(streamName).
    // Registered with Services in Bootstrap. Epic 3 overwrites with RunStateSO.RunSeed.
    public sealed class RNGStreams
    {
        // Per §9.7.2 — stream ownership table.
        public readonly GameRNG MapRNG;       // MapSeeder — region map topology
        public readonly GameRNG CombatRNG;    // CombatController — crit checks, AI randomness floor
        public readonly GameRNG LootRNG;      // RewardController — trainer drops, shop seeds
        public readonly GameRNG MysteryRNG;   // MysteryEventController — event outcome resolutions
        public readonly GameRNG EncounterRNG; // EncounterController — wild Pokémon species rolls

        public RNGStreams(uint runSeed)
        {
            MapRNG       = new GameRNG(runSeed ^ FNV1a("MapRNG"));
            CombatRNG    = new GameRNG(runSeed ^ FNV1a("CombatRNG"));
            LootRNG      = new GameRNG(runSeed ^ FNV1a("LootRNG"));
            MysteryRNG   = new GameRNG(runSeed ^ FNV1a("MysteryRNG"));
            EncounterRNG = new GameRNG(runSeed ^ FNV1a("EncounterRNG"));
        }

        // Per §9.8.6 (gap #45) — snapshot all 5 cursors for the run save (written before each
        // node-entry autosave).
        public RNGCursors CaptureCursors() => new()
        {
            Map       = MapRNG.State,
            Combat    = CombatRNG.State,
            Loot      = LootRNG.State,
            Mystery   = MysteryRNG.State,
            Encounter = EncounterRNG.State,
        };

        // Per §9.8.6 (gap #45) — restore the 4 CONTENT cursors on resume so encounters/loot/mystery/
        // combat rolls don't re-roll. MapRNG is deliberately left untouched: the map is rebuilt by
        // deterministic replay (RegionMapGenerator over MapRNG), so it must stay at its region-entry
        // state — restoring the save-time (post-build) cursor would regenerate a different map.
        public void RestoreContentCursors(RNGCursors c)
        {
            CombatRNG.State    = c.Combat;
            LootRNG.State      = c.Loot;
            MysteryRNG.State   = c.Mystery;
            EncounterRNG.State = c.Encounter;
        }

        // FNV-1a 32-bit hash — used for deterministic stream seed derivation.
        public static uint FNV1a(string s)
        {
            uint hash = 2166136261u; // FNV offset basis
            foreach (char c in s)
            {
                hash ^= (uint)c;
                hash *= 16777619u;  // FNV prime
            }
            return hash;
        }
    }
}
