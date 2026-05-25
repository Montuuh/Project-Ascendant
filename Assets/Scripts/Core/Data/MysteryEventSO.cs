using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §3.1.14 — Definition SO for a Mystery Event node.
    // VS events: Mysterious Stone, Berry Bush, Wandering Tutor, Slot Booth (§3.3.17).
    // Full outcome resolution logic implemented in Epic 9 (Map & Nodes).
    [CreateAssetMenu(fileName = "New Mystery Event", menuName = "Project Ascendant/World/Mystery Event")]
    public class MysteryEventSO : ScriptableObject
    {
        [Header("Identity")]
        public string EventId;
        public string DisplayName;
        public Sprite Icon;

        [Header("Narrative")]
        [TextArea(3, 6)]
        public string NarrativeText;

        [Header("Choices")]
        // Player is presented with these choices. 1-4 choices per event.
        public List<MysteryChoice> Choices;

        [Tooltip("GDD section for this event. Per §9.15.")]
        public string GDDReference;
    }

    [Serializable]
    public struct MysteryChoice
    {
        public string ChoiceText;

        [TextArea(1, 3)]
        [Tooltip("Text shown after the choice is made.")]
        public string OutcomeText;

        // Outcome effect wired in Epic 9. Described here for designer reference.
        [TextArea(1, 2)]
        [Tooltip("Designer note on what happens mechanically. Wired in Epic 9.")]
        public string OutcomeMechanicsNote;
    }
}
