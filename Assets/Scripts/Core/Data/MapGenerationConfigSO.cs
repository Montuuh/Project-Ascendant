using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.20 — map topology and per-layer node-type distribution weights.
    // Consumed by MapSeeder (Epic 9). All values here — not in code.
    [CreateAssetMenu(fileName = "MapGenerationConfig", menuName = "Project Ascendant/Config/Map Generation Config")]
    public class MapGenerationConfigSO : ScriptableObject
    {
        [Header("Topology")]
        [Tooltip("Number of map layers between Region start and Gym.")]
        public int LayerCount = 7;

        [Tooltip("Maximum route branches shown at each junction. Overridden by DifficultyModifierSO.")]
        public int DefaultMaxBranches = 3;

        [Header("Per-Layer Node Weights")]
        // One entry per layer (0 = first layer after start, LayerCount-1 = pre-Gym layer).
        // Weights are relative — not probabilities. MapSeeder normalises per §9.7.2.
        public List<NodeLayerWeights> LayerWeights;
    }

    [Serializable]
    public struct NodeLayerWeights
    {
        [Tooltip("Layer index this weight set applies to (0-based).")]
        public int Layer;

        public float WildWeight;
        public float TrainerWeight;
        public float CenterWeight;
        public float ShopWeight;
        public float MysteryWeight;

        [Tooltip("Gym weight — typically 0 except the final layer.")]
        public float GymWeight;
    }
}
