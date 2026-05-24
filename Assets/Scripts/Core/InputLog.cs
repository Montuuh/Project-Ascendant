using System;
using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.7.4 + §9.10.3 — single recorded input event in the determinism log.
    [Serializable]
    public struct InputLogEntry
    {
        public int    LogicalTick;
        public string ActionId;
        public uint   PayloadHash;
    }

    // Per §9.3.2.4 + §9.7.4 — ordered input stream stored on RunStateSO.RecordedInputs.
    // Serializable for binary save (§9.8). Mutated only by InputLogRecorder.
    [Serializable]
    public sealed class InputLog
    {
        private readonly List<InputLogEntry> _entries = new();

        public IReadOnlyList<InputLogEntry> Entries => _entries;
        public int Count => _entries.Count;

        public void Add(string actionId, int logicalTick, uint payloadHash = 0u)
        {
            _entries.Add(new InputLogEntry
            {
                LogicalTick = logicalTick,
                ActionId    = actionId,
                PayloadHash = payloadHash,
            });
        }

        public void Clear() => _entries.Clear();
    }
}
