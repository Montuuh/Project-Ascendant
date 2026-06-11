using System;
using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.7.1 — seeded xorshift32 RNG wrapper. All production randomness must use this class.
    // See BannedApiValidator: the engine and BCL random types are forbidden in production (§9.7.2).
    // All callers must go through a GameRNG instance obtained from RNGStreams via Services.
    public sealed class GameRNG
    {
        private uint _state;

        // Per §9.7.1 — xorshift32 requires non-zero state; clamp seed 0 to 1.
        public GameRNG(uint seed)
        {
            _state = seed == 0 ? 1u : seed;
        }

        // Per §9.8.6 (gap #45) — the live cursor. Get to snapshot a stream's position into the run
        // save; set to restore it on resume so already-consumed rolls (encounters/loot/mystery) do
        // not re-roll. Clamp 0 → 1 (xorshift32 cannot leave the zero state).
        public uint State
        {
            get => _state;
            set => _state = value == 0 ? 1u : value;
        }

        // Per §9.7.1 — xorshift32: deterministic, fast, zero GC.
        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        // Returns an int in [min, maxExclusive). Modulo bias is acceptable for game use.
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            uint range = (uint)(maxExclusive - min);
            return min + (int)(NextUInt() % range);
        }

        // Returns a float in [0, 1].
        public float Range01() => NextUInt() / (float)uint.MaxValue;

        // Per §9.7.1 — selects one item proportionally to its weight.
        // options must be non-null and non-empty; weights must be > 0.
        public T PickWeighted<T>(IList<(T value, float weight)> options)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("options must be non-empty", nameof(options));

            float total = 0f;
            for (int i = 0; i < options.Count; i++)
                total += options[i].weight;

            float roll = Range01() * total;
            float cumulative = 0f;
            for (int i = 0; i < options.Count; i++)
            {
                cumulative += options[i].weight;
                if (roll <= cumulative)
                    return options[i].value;
            }
            return options[options.Count - 1].value;
        }
    }
}
