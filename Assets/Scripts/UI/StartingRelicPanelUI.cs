using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §6.6.3 + Epic 12 Task 12.11 + CL-024 — the pre-run "choose 1 of 3 Starting Relics" modal.
    // Thin wrapper around RelicChoicePanelUI (refactored for Elite Trainer Rare-relic choice).
    // View-layer only; the offer (Common/Uncommon, never Rare) is built by StartingRelicService.
    public sealed class StartingRelicPanelUI : MonoBehaviour
    {
        private RelicChoicePanelUI _panel;

        public bool IsOpen => _panel != null && _panel.IsOpen;

        public void Open(IReadOnlyList<RelicSO> offer, Action<RelicSO> onPicked)
        {
            if (_panel == null)
            {
                _panel = gameObject.AddComponent<RelicChoicePanelUI>();
            }
            _panel.Open(offer, "CHOOSE A STARTING RELIC", onPicked);
        }
    }
}
