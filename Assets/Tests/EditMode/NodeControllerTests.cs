using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.2 / §9.5 / §9.8.1 + Epic 9 Task 9.2 — NodeController base framework tests.
    public class NodeControllerTests
    {
        private string _tempDir;

        // Minimal concrete controller for exercising the base lifecycle.
        private sealed class FakeNodeController : NodeController
        {
            private readonly NodeOutcome? _completeOnEnter;
            public int OnEnterCalls { get; private set; }

            public FakeNodeController(MapNode node, RunStateSO run, NodeOutcome? completeOnEnter = null)
                : base(node, run) { _completeOnEnter = completeOnEnter; }

            protected override void OnEnter()
            {
                OnEnterCalls++;
                if (_completeOnEnter.HasValue) Complete(_completeOnEnter.Value);
            }

            // Exposes the protected Complete for tests that resolve after entry.
            public void CompleteNow(NodeOutcome outcome) => Complete(outcome);
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_NodeTest_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectoryOverride = _tempDir;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            SaveSystem.SaveDirectoryOverride = null;
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }

        private static MapNode MakeNode(NodeType type, int layer = 2, int lane = 1)
            => new MapNode(layer, lane, 0, type);

        private static RunStateSO MakeRun() => ScriptableObject.CreateInstance<RunStateSO>();

        // ── Construction ────────────────────────────────────────────────────────

        [Test]
        public void Ctor_NullNode_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new FakeNodeController(null, MakeRun()));
        }

        [Test]
        public void Ctor_NullRunState_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new FakeNodeController(MakeNode(NodeType.Wild), null));
        }

        // ── Enter: save-on-entry + event ──────────────────────────────────────────

        [Test]
        public void Enter_SavesRunOnEntry()
        {
            // Per §9.8.1 / Epic 9 DoD — every node entry writes the run save.
            FakeNodeController c = new(MakeNode(NodeType.Trainer), MakeRun());
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True,
                "Entering a node must write run-current.dat.");
        }

        [Test]
        public void Enter_RecordsCurrentLayer()
        {
            // Per §9.8.1 — entry records position so a resumed save lands on this layer.
            RunStateSO run = MakeRun();
            FakeNodeController c = new(MakeNode(NodeType.Shop, layer: 5), run);
            c.Enter();
            Assert.That(run.CurrentLayerIndex, Is.EqualTo(5));
        }

        [Test]
        public void Enter_PublishesNodeEnteredContext()
        {
            NodeEnteredContext? seen = null;
            void Handler(NodeEnteredContext ctx) => seen = ctx;
            EventBus.Subscribe<NodeEnteredContext>(Handler);

            new FakeNodeController(MakeNode(NodeType.Mystery, layer: 3, lane: 0), MakeRun()).Enter();

            Assert.That(seen.HasValue, Is.True);
            Assert.That(seen.Value.NodeType, Is.EqualTo(NodeType.Mystery));
            Assert.That(seen.Value.Layer, Is.EqualTo(3));
            Assert.That(seen.Value.Lane, Is.EqualTo(0));
        }

        [Test]
        public void Enter_IsIdempotent()
        {
            FakeNodeController c = new(MakeNode(NodeType.Wild), MakeRun());
            c.Enter();
            c.Enter();
            Assert.That(c.OnEnterCalls, Is.EqualTo(1), "OnEnter must run once even if Enter is called twice.");
        }

        [Test]
        public void Enter_SetsIsEntered()
        {
            FakeNodeController c = new(MakeNode(NodeType.Center), MakeRun());
            Assert.That(c.IsEntered, Is.False);
            c.Enter();
            Assert.That(c.IsEntered, Is.True);
        }

        // ── Complete: event + outcome ─────────────────────────────────────────────

        [Test]
        public void Complete_PublishesNodeCompletedContext()
        {
            NodeCompletedContext? seen = null;
            EventBus.Subscribe<NodeCompletedContext>(ctx => seen = ctx);

            FakeNodeController c = new(MakeNode(NodeType.Trainer, layer: 4, lane: 1), MakeRun());
            c.Enter();
            c.CompleteNow(NodeOutcome.Cleared);

            Assert.That(seen.HasValue, Is.True);
            Assert.That(seen.Value.NodeType, Is.EqualTo(NodeType.Trainer));
            Assert.That(seen.Value.Layer, Is.EqualTo(4));
            Assert.That(seen.Value.Lane, Is.EqualTo(1));
            Assert.That(seen.Value.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void Complete_SetsOutcomeAndFlag()
        {
            FakeNodeController c = new(MakeNode(NodeType.Gym), MakeRun());
            c.Enter();
            Assert.That(c.IsCompleted, Is.False);
            Assert.That(c.Outcome, Is.Null);
            c.CompleteNow(NodeOutcome.RunEnded);
            Assert.That(c.IsCompleted, Is.True);
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.RunEnded));
        }

        [Test]
        public void Complete_IsIdempotent_FirstOutcomeWins()
        {
            int completedEvents = 0;
            EventBus.Subscribe<NodeCompletedContext>(_ => completedEvents++);

            FakeNodeController c = new(MakeNode(NodeType.Wild), MakeRun());
            c.Enter();
            c.CompleteNow(NodeOutcome.Cleared);
            c.CompleteNow(NodeOutcome.PlayerWiped);

            Assert.That(completedEvents, Is.EqualTo(1));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void CompleteOnEnter_ResolvesSynchronously()
        {
            // A utility node can resolve inside OnEnter (Center/Shop).
            FakeNodeController c = new(MakeNode(NodeType.Center), MakeRun(), completeOnEnter: NodeOutcome.Cleared);
            c.Enter();
            Assert.That(c.IsCompleted, Is.True);
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        // ── Outcome → HSM event mapping ───────────────────────────────────────────

        [Test]
        public void ToGameEventType_MapsOutcomes()
        {
            // Per §9.5 — NodeState reads this to choose the transition.
            RunStateSO run = MakeRun();
            FakeNodeController cleared = new(MakeNode(NodeType.Trainer), run);
            cleared.Enter(); cleared.CompleteNow(NodeOutcome.Cleared);
            Assert.That(cleared.ToGameEventType(), Is.EqualTo(GameEventType.NodeComplete));

            FakeNodeController ended = new(MakeNode(NodeType.Gym), run);
            ended.Enter(); ended.CompleteNow(NodeOutcome.RunEnded);
            Assert.That(ended.ToGameEventType(), Is.EqualTo(GameEventType.RunEnded));

            FakeNodeController wiped = new(MakeNode(NodeType.Elite), run);
            wiped.Enter(); wiped.CompleteNow(NodeOutcome.PlayerWiped);
            Assert.That(wiped.ToGameEventType(), Is.EqualTo(GameEventType.GameOver));
        }

        [Test]
        public void ToGameEventType_BeforeComplete_IsNone()
        {
            FakeNodeController c = new(MakeNode(NodeType.Wild), MakeRun());
            c.Enter();
            Assert.That(c.ToGameEventType(), Is.EqualTo(GameEventType.None));
        }

        // ── Preview (9.2.3) ───────────────────────────────────────────────────────

        [Test]
        public void BuildPreview_DefaultsToTypeKey()
        {
            FakeNodeController c = new(MakeNode(NodeType.Shop, layer: 2, lane: 0), MakeRun());
            NodePreview p = c.BuildPreview();
            Assert.That(p.NodeType, Is.EqualTo(NodeType.Shop));
            Assert.That(p.Layer, Is.EqualTo(2));
            Assert.That(p.Lane, Is.EqualTo(0));
            Assert.That(p.DisplayNameKey, Is.EqualTo("node.shop.name"));
            Assert.That(p.DetailKey, Is.Null);
        }

        [Test]
        public void DefaultNameKey_IsConventional()
        {
            Assert.That(NodeController.DefaultNameKey(NodeType.Mystery), Is.EqualTo("node.mystery.name"));
            Assert.That(NodeController.DefaultNameKey(NodeType.Gym), Is.EqualTo("node.gym.name"));
        }

        // ── NodeControllerFactory ─────────────────────────────────────────────────

        [Test]
        public void Factory_BuildsRegisteredType()
        {
            NodeControllerFactory factory = new();
            factory.Register(NodeType.Wild, (n, r) => new FakeNodeController(n, r));

            Assert.That(factory.CanBuild(NodeType.Wild), Is.True);
            NodeController built = factory.Build(MakeNode(NodeType.Wild), MakeRun());
            Assert.That(built, Is.InstanceOf<FakeNodeController>());
            Assert.That(built.NodeType, Is.EqualTo(NodeType.Wild));
        }

        [Test]
        public void Factory_UnregisteredType_Throws()
        {
            NodeControllerFactory factory = new();
            Assert.That(factory.CanBuild(NodeType.Gym), Is.False);
            Assert.Throws<System.InvalidOperationException>(
                () => factory.Build(MakeNode(NodeType.Gym), MakeRun()));
        }

        // ── NodePresentationConfigSO (9.2.3) ──────────────────────────────────────

        [Test]
        public void PresentationConfig_TryGet_ResolvesEntry()
        {
            NodePresentationConfigSO config = ScriptableObject.CreateInstance<NodePresentationConfigSO>();
            config.Entries = new List<NodePresentationEntry>
            {
                new() { NodeType = NodeType.Wild, DisplayNameKey = "node.wild.name", AccentColorKey = "grass" },
                new() { NodeType = NodeType.Gym,  DisplayNameKey = "node.gym.name",  AccentColorKey = "boss" },
            };

            Assert.That(config.TryGet(NodeType.Gym, out NodePresentationEntry gym), Is.True);
            Assert.That(gym.DisplayNameKey, Is.EqualTo("node.gym.name"));
            Assert.That(config.TryGet(NodeType.Shop, out _), Is.False);
        }
    }
}
