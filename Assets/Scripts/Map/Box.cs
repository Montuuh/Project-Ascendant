using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §2.3 / §2.3.1 + Epic 9 Task 9.3 — the run's persistent Pokémon roster (the only storage
    // container in a run; there is no Deposit pool). Pure C# run-state; the Active Team is selected
    // from this by the LoadoutManager (Task 9.10).
    //
    // Capacity per §2.3 = 6 by default, raised to 8 via relic/meta. The run layer computes the
    // effective capacity from EconomyConfigSO.BoxCapacity (+ relic/meta) and passes it in here.
    // Overflow on recruitment (Box full) is handled by WildEncounterController.ResolveOutcome via
    // IBoxOverflowHandler (§2.3.1 Swap-or-Skip), which mutates Members directly.
    public sealed class Box
    {
        public readonly List<PokemonInstance> Members = new();
        public int Capacity { get; set; }

        public Box(int capacity) { Capacity = capacity; }

        public int Count => Members.Count;

        // Capacity <= 0 is treated as unbounded (matches WildEncounterController.ResolveOutcome).
        public bool IsFull => Capacity > 0 && Members.Count >= Capacity;

        // Adds a Pokémon if there is room. Returns false (no change) when full — overflow handling
        // (§2.3.1) is the caller's responsibility via the IBoxOverflowHandler path.
        public bool TryAdd(PokemonInstance pokemon)
        {
            if (pokemon == null || IsFull) return false;
            Members.Add(pokemon);
            return true;
        }
    }
}
