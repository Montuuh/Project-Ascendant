using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 + §8.3 — Definition SO for a Trainer Relic. Immutable at runtime.
    // Relics are persistent run-state modifiers wired via ScriptableHook to EventBus channels.
    // Per §8.3.2 — each relic has a primary + optional secondary SynergyCategory for shop curation.
    [CreateAssetMenu(fileName = "New Relic", menuName = "Project Ascendant/Items/Relic")]
    public class RelicSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        public Sprite Icon;

        [Header("Rarity & Tier")]
        public RarityTier Rarity;

        // Per §6.6.1 — 1/2/3 meta-unlock tier. Tier 1 = available from run start.
        [Range(1, 3)]
        public int MetaTier = 1;

        // Per §8.3.2 — used by City Shop curation algorithm.
        public List<SynergyCategory> Categories;

        [Header("Hooks")]
        // Per §8.7 — fired once when this relic is acquired mid-run.
        public ScriptableHook OnAcquireHook;

        // Per §8.7 — list of event+hook pairs. HookSubscriber wires these in Epic 4.
        public List<HookBinding> EventHooks;

        [Tooltip("GDD section for this relic. Per §9.15.")]
        public string GDDReference;
    }

    // Per §8.7 — pairs an SO event channel with a ScriptableHook.
    // HookSubscriber (Epic 4) reads this list to wire up the relic at acquisition time.
    [Serializable]
    public struct HookBinding
    {
        [Tooltip("The GameEventSO<T> channel asset this hook subscribes to.")]
        public ScriptableObject Channel;

        public ScriptableHook Hook;
    }
}
