using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §2.3.1 + Epic 9 Task 9.3 — safe non-interactive default for Box overflow.
    //
    // The real Swap-or-Skip prompt (§2.3.1) is an interactive UI decision authored in Epic 13.
    // Until that lands, the run layer injects this handler: it always returns -1 (Skip), so a
    // catch while the Box is full declines the recruit rather than silently releasing a Pokémon
    // the player did not choose. Recruits are never lost-by-bug — the player simply can't pick
    // which to release until the UI handler replaces this.
    public sealed class AutoSkipBoxOverflowHandler : IBoxOverflowHandler
    {
        public int OnBoxOverflow(IReadOnlyList<PokemonInstance> currentBox, PokemonInstance candidate)
            => -1; // Skip
    }
}
