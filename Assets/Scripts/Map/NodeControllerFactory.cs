using System;
using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §9.5 / §9.6 + Epic 9 Task 9.2 — builds the concrete NodeController for a MapNode's type.
    // Node-type controllers (Tasks 9.3–9.8) register a builder delegate per NodeType, so the HSM
    // NodeState resolves a node without a hardcoded switch. Pure C#; registration is done at run
    // setup (Bootstrap / RunState init), keeping the Map assembly free of Combat references here.
    public sealed class NodeControllerFactory
    {
        private readonly Dictionary<NodeType, Func<MapNode, RunStateSO, NodeController>> _builders = new();

        // Registers (or replaces) the builder for a node type.
        public void Register(NodeType type, Func<MapNode, RunStateSO, NodeController> builder)
        {
            _builders[type] = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public bool CanBuild(NodeType type) => _builders.ContainsKey(type);

        // Builds the controller for the node's type. Throws if no builder is registered —
        // an unregistered node type is a wiring bug, not a recoverable runtime state.
        public NodeController Build(MapNode node, RunStateSO runState)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (!_builders.TryGetValue(node.NodeType, out Func<MapNode, RunStateSO, NodeController> builder))
                throw new InvalidOperationException(
                    $"No NodeController builder registered for NodeType.{node.NodeType}.");
            return builder(node, runState);
        }
    }
}
