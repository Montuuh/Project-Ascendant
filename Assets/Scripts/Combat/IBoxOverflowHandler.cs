using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §2.3.1 + Epic 8 Task 8.1.5 — hook the WildEncounterController calls
    // when a successful catch would push the Box past capacity (6 default,
    // 8 with relic/meta, 4 under the Box Squeeze difficulty modifier).
    //
    // Contract:
    //   • Return  -1            → Skip. The candidate is discarded; the Box
    //                              is unchanged. The wild encounter ends with
    //                              BoxOverflowPromptShown=true, BoxUpdated=false.
    //   • Return  0..count-1    → Swap. The Box entry at that index is
    //                              released; the candidate takes its place.
    //                              BoxOverflowPromptShown=true, BoxUpdated=true.
    //   • Any out-of-range int  → Treated as Skip (defensive).
    //
    // Production UI implements this; tests stub it inline.
    public interface IBoxOverflowHandler
    {
        int OnBoxOverflow(IReadOnlyList<PokemonInstance> currentBox,
                          PokemonInstance candidate);
    }
}
