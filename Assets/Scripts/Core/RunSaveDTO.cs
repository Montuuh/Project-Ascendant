using System;
using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.8 + VS gap #43 — the complete on-disk run save: the RunStateSO snapshot PLUS the live
    // Box (team) snapshot and its capacity. The Box is owned by the run layer (RunContext.Box), not
    // RunStateSO, so it is persisted alongside rather than inside the run-state DTO. ActiveTeamIndices
    // in the run-state index into this Box.
    [Serializable]
    public sealed class RunSaveDTO
    {
        public RunStateDTO Run;
        public List<PokemonInstanceDTO> Box;
        public int BoxCapacity;
    }

    // Runtime result of SaveSystem.LoadRun — the rebuilt run-state + Box, ready for the resume path
    // to install into a RunContext. Plain bundle (not a ScriptableObject); null when no save exists.
    public sealed class RunSaveData
    {
        public RunStateSO Run;
        public List<PokemonInstance> Box;
        public int BoxCapacity;
    }
}
