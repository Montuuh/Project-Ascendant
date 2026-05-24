namespace ProjectAscendant.Core
{
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
