using System;

namespace ProjectAscendant.Core
{
    // Per §9.7.4 — payload published by InputLogReplayer via EventBus during replay.
    public readonly struct InputReplayedContext
    {
        public readonly int    LogicalTick;
        public readonly string ActionId;
        public readonly uint   PayloadHash;

        public InputReplayedContext(int logicalTick, string actionId, uint payloadHash)
        {
            LogicalTick = logicalTick;
            ActionId    = actionId;
            PayloadHash = payloadHash;
        }
    }

    // Per §9.7.4 — consumes an InputLog and republishes each entry via EventBus.
    // Consumers subscribe to InputReplayedContext to observe the replayed stream.
    // RunSeed + InputLog → deterministic full replay (§9.7.4).
    public sealed class InputLogReplayer
    {
        private readonly InputLog _log;

        public InputLogReplayer(InputLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // Replay all entries in recording order, publishing InputReplayedContext for each.
        public void Replay()
        {
            foreach (InputLogEntry entry in _log.Entries)
                EventBus.Publish(new InputReplayedContext(entry.LogicalTick, entry.ActionId, entry.PayloadHash));
        }
    }
}
