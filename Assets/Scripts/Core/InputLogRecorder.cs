using System;

namespace ProjectAscendant.Core
{
    // Per §9.7.4 + §9.10.3 — appends committed input events to an InputLog.
    // InputSystem/HSM bridge wiring is handled by MonoBehaviour adapters (Epic 4 / Play Mode).
    // This class exposes Record() so both real callbacks and test stubs can feed it.
    // UI hover/scroll events must NOT be recorded per §9.10.3.
    public sealed class InputLogRecorder
    {
        private readonly InputLog _log;
        private int _logicalTick;

        public InputLog Log         => _log;
        public int      LogicalTick => _logicalTick;

        public InputLogRecorder(InputLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // Record a committed input at the current logical tick.
        public void Record(string actionId, uint payloadHash = 0u)
        {
            _log.Add(actionId, _logicalTick, payloadHash);
        }

        // Advance the logical tick counter. Called once per game-logic tick by the combat loop.
        public void AdvanceTick() => _logicalTick++;

        // Resets the recorder state and clears the underlying log for a new run.
        public void Reset()
        {
            _logicalTick = 0;
            _log.Clear();
        }
    }
}
