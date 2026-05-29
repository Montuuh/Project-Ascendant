using System;
using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §2.3 + Epic 9 Task 9.10 — owns the Map-View Loadout: the player's Active Team of up to 3
    // selected from the Box, committed via a Confirm gesture and LOCKED the moment a node is entered
    // (no loadout changes during combat / inside a node). Pure C#; the UI (Epic 13) drives it.
    //
    // Active Team is stored on RunStateSO as box indices (ActiveTeamIndices) + a Lead slot
    // (LeadIndex, an index INTO the active team, §3.3.1). A fainted Pokémon (CurrentHP == 0) may not
    // be in the Active Team (§2.4.1).
    public sealed class LoadoutManager
    {
        public const int MAX_ACTIVE_TEAM = 3; // §2.3 — 3 Pokémon brought into a combat.

        private readonly RunStateSO _run;
        private readonly Box _box;

        // §2.3 — locked on node entry; unlocked when the Map View regains control between nodes.
        public bool IsLocked { get; private set; }

        public LoadoutManager(RunStateSO run, Box box)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _box = box ?? throw new ArgumentNullException(nameof(box));
        }

        // 9.10.2 — called on node entry to freeze the Active Team for the duration of the node.
        public void Lock() => IsLocked = true;

        // Called by the Map View between nodes so the player can re-loadout.
        public void Unlock() => IsLocked = false;

        // 9.10.1 — the Confirm gesture: validate the proposed Active Team + Lead, then commit to the
        // run. Refused (no change) while locked (9.10.3) or if the selection is invalid.
        public bool Confirm(List<int> boxIndices, int leadSlot)
        {
            if (IsLocked) return false;
            if (!IsValidSelection(boxIndices, leadSlot)) return false;

            _run.ActiveTeamIndices = new List<int>(boxIndices);
            _run.LeadIndex = leadSlot;
            return true;
        }

        // Per §2.3 / §2.4.1 — a selection is valid iff: 1..MAX distinct in-range box indices, none
        // fainted, and the Lead slot indexes into the selection.
        public bool IsValidSelection(List<int> boxIndices, int leadSlot)
        {
            if (boxIndices == null) return false;
            if (boxIndices.Count < 1 || boxIndices.Count > MAX_ACTIVE_TEAM) return false;
            if (leadSlot < 0 || leadSlot >= boxIndices.Count) return false;

            HashSet<int> seen = new();
            for (int i = 0; i < boxIndices.Count; i++)
            {
                int idx = boxIndices[i];
                if (idx < 0 || idx >= _box.Members.Count) return false;
                if (!seen.Add(idx)) return false; // distinct — a Pokémon can't occupy two slots
                PokemonInstance p = _box.Members[idx];
                if (p == null || p.CurrentHP == 0) return false; // §2.4.1 — no fainted in Active Team
            }
            return true;
        }
    }
}
