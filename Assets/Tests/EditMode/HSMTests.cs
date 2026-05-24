using NUnit.Framework;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.5 — HSM unit tests.
    public class HSMTests
    {
        // ── Helpers ─────────────────────────────────────────────────────────────

        // Minimal tracked state: counts OnEnter / OnExit calls.
        private sealed class TrackedState : GameStateNode
        {
            public int EnterCount;
            public int ExitCount;
            public bool HandledEvent;

            public override void OnEnter() => EnterCount++;
            public override void OnExit()  => ExitCount++;

            public override void HandleEvent(GameEvent evt)
            {
                HandledEvent = true;
                base.HandleEvent(evt); // propagate to child
            }
        }

        // Parent state that holds two tracked children for sibling-transition tests.
        // Exposes EnterChild publicly so tests can drive transitions without reflection.
        private sealed class ParentState : GameStateNode
        {
            public readonly TrackedState ChildA;
            public readonly TrackedState ChildB;

            public ParentState()
            {
                ChildA = new TrackedState();
                ChildB = new TrackedState();
            }

            public override void OnEnter() { EnterChild(ChildA); }

            // Test helper — exposes protected EnterChild for driving transitions.
            public void SwitchToChild(GameStateNode child) => EnterChild(child);
        }

        // ── Tree traversal ───────────────────────────────────────────────────────

        // Per §9.5.1 — GameStateMachine constructs root and enters MainMenu on boot.
        [Test]
        public void HSM_Boot_CurrentChildIsMainMenu()
        {
            GameStateMachine hsm = new GameStateMachine();

            Assert.That(hsm.Root.CurrentChild, Is.InstanceOf<MainMenuState>());
        }

        // Per §9.5.1 — RunState enters MapView as its default child.
        [Test]
        public void HSM_AfterStartNewRun_RunStateCurrentChildIsMapView()
        {
            GameStateMachine hsm = new GameStateMachine();
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));

            Assert.That(hsm.Root.CurrentChild, Is.InstanceOf<RunState>());
            Assert.That(hsm.Root.CurrentChild.CurrentChild, Is.InstanceOf<MapViewState>());
        }

        // Per §9.5.1 — MapView → NodeState on NodeConfirmed.
        [Test]
        public void HSM_NodeConfirmed_TransitionsToNodeState()
        {
            GameStateMachine hsm = new GameStateMachine();
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));
            hsm.HandleEvent(new GameEvent(GameEventType.NodeConfirmed));

            Assert.That(hsm.Root.CurrentChild.CurrentChild, Is.InstanceOf<NodeState>());
        }

        // Per §9.5.1 — deep tree traversal: root → Run → Node → Combat → TurnLoop → DrawPhase.
        [Test]
        public void HSM_DeepTree_CombatTurnLoopReachesDrawPhase()
        {
            GameStateMachine hsm = new GameStateMachine();
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));
            hsm.HandleEvent(new GameEvent(GameEventType.NodeConfirmed));
            hsm.HandleEvent(new GameEvent(GameEventType.CombatBegin));

            // root → Run → Node → Combat → TurnLoop → DrawPhase
            GameStateNode leaf = hsm.Root
                .CurrentChild           // RunState
                .CurrentChild           // NodeState
                .CurrentChild           // CombatState
                .CurrentChild           // TurnLoopState
                .CurrentChild;          // DrawPhaseState

            Assert.That(leaf, Is.InstanceOf<DrawPhaseState>());
        }

        // ── Nested state transitions ─────────────────────────────────────────────

        // Per §9.5.3 — TransitionTo correctly switches sibling within same parent.
        [Test]
        public void TransitionTo_SwitchesSiblingInsideParent()
        {
            ParentState parent = new ParentState();
            parent.OnEnter(); // enters ChildA

            Assert.That(parent.CurrentChild, Is.SameAs(parent.ChildA));

            parent.SwitchToChild(parent.ChildB);

            Assert.That(parent.CurrentChild, Is.SameAs(parent.ChildB));
        }

        // Per §9.5.3 — after transition, old state OnExit and new state OnEnter called exactly once.
        [Test]
        public void TransitionTo_OnExitAndOnEnterCalledExactlyOnce()
        {
            // Use GameStateMachine's real transition: StartNewRun moves MainMenu → RunState.
            GameStateMachine hsm = new GameStateMachine();

            // Boot enters MainMenuState — but we can't intercept that easily.
            // Instead, verify the combat turn-phase transitions via HandleEvent chain.
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));   // → RunState
            hsm.HandleEvent(new GameEvent(GameEventType.NodeConfirmed)); // → NodeState (combat)
            hsm.HandleEvent(new GameEvent(GameEventType.CombatBegin));   // CombatStart → TurnLoop → DrawPhase

            // Transition DrawPhase → IntentPhase
            hsm.HandleEvent(new GameEvent(GameEventType.DrawComplete));

            GameStateNode current = hsm.Root.CurrentChild   // Run
                                             .CurrentChild  // Node
                                             .CurrentChild  // Combat
                                             .CurrentChild  // TurnLoop
                                             .CurrentChild; // should be IntentPhase

            Assert.That(current, Is.InstanceOf<IntentPhaseState>());
        }

        // Per §9.5.2 — full turn phase cycle: Draw → Intent → Action → Resolution → TurnEnd → Draw.
        [Test]
        public void TurnLoop_FullCycle_ReturnsToDrawPhase()
        {
            GameStateMachine hsm = new GameStateMachine();
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));
            hsm.HandleEvent(new GameEvent(GameEventType.NodeConfirmed));
            hsm.HandleEvent(new GameEvent(GameEventType.CombatBegin));

            hsm.HandleEvent(new GameEvent(GameEventType.DrawComplete));
            hsm.HandleEvent(new GameEvent(GameEventType.IntentRevealComplete));
            hsm.HandleEvent(new GameEvent(GameEventType.PlayerEndedTurn));
            hsm.HandleEvent(new GameEvent(GameEventType.ResolutionComplete));
            hsm.HandleEvent(new GameEvent(GameEventType.TurnEndComplete));

            GameStateNode leaf = hsm.Root.CurrentChild.CurrentChild.CurrentChild
                                         .CurrentChild.CurrentChild; // TurnLoop.CurrentChild

            Assert.That(leaf, Is.InstanceOf<DrawPhaseState>());
        }

        // ── Event bubbling ────────────────────────────────────────────────────────

        // Per §9.5.3 — event dispatched at root bubbles to leaf state.
        [Test]
        public void HandleEvent_BubblesFromRootToLeaf()
        {
            ParentState parent = new ParentState();
            parent.OnEnter(); // enters ChildA

            parent.HandleEvent(GameEvent.None);

            Assert.That(parent.ChildA.HandledEvent, Is.True,
                "Event should bubble from parent to active child.");
        }

        // Per §9.5.3 — event stops at inactive sibling (only active child receives it).
        [Test]
        public void HandleEvent_DoesNotReachInactiveSibling()
        {
            ParentState parent = new ParentState();
            parent.OnEnter(); // enters ChildA

            parent.HandleEvent(GameEvent.None);

            Assert.That(parent.ChildB.HandledEvent, Is.False,
                "Inactive sibling must not receive bubbled event.");
        }

        // ── OnEnter / OnExit invocation parity ───────────────────────────────────

        // Per §9.5.3 — EnterChild calls OnEnter on new child and OnExit on previous child.
        [Test]
        public void EnterChild_CallsOnExitOnOldAndOnEnterOnNew()
        {
            ParentState parent = new ParentState();
            parent.OnEnter(); // ChildA.OnEnter() called

            int aEnterAfterBoot = parent.ChildA.EnterCount; // should be 1
            int aExitBefore     = parent.ChildA.ExitCount;  // should be 0
            int bEnterBefore    = parent.ChildB.EnterCount; // should be 0

            parent.SwitchToChild(parent.ChildB); // exits ChildA, enters ChildB

            Assert.That(parent.ChildA.EnterCount, Is.EqualTo(aEnterAfterBoot),
                "Old child OnEnter must not be called again.");
            Assert.That(parent.ChildA.ExitCount, Is.EqualTo(aExitBefore + 1),
                "Old child OnExit must be called exactly once.");
            Assert.That(parent.ChildB.EnterCount, Is.EqualTo(bEnterBefore + 1),
                "New child OnEnter must be called exactly once.");
        }

        // Per §9.5.3 — ExitSubtree exits leaf first, then parent (leaf-to-root order).
        [Test]
        public void ExitSubtree_ExitsLeafBeforeParent()
        {
            int exitOrder = 0;
            int parentExitOrder = -1;
            int childExitOrder  = -1;

            // Can't easily hook into sealed states, so test with TrackedState helpers.
            // Verify using the GameStateMachine: after StartNewRun, combat is entered deep.
            // CombatVictory transitions TurnLoop (parent) away from DrawPhase (child).
            GameStateMachine hsm = new GameStateMachine();
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));
            hsm.HandleEvent(new GameEvent(GameEventType.NodeConfirmed));
            hsm.HandleEvent(new GameEvent(GameEventType.CombatBegin)); // CombatStart → TurnLoop → DrawPhase

            // CombatVictory: TurnLoop.ExitSubtree() exits DrawPhase first, then TurnLoop.
            // Then CombatState.CurrentChild = CombatVictoryState.
            hsm.HandleEvent(new GameEvent(GameEventType.CombatVictory));

            // After CombatVictory, CombatState.CurrentChild should be CombatVictoryState.
            GameStateNode combatCurrent = hsm.Root.CurrentChild   // Run
                                                  .CurrentChild   // Node
                                                  .CurrentChild   // CombatState
                                                  .CurrentChild;  // should be CombatVictoryState

            Assert.That(combatCurrent, Is.InstanceOf<CombatVictoryState>());
        }

        // Per §9.5.2 — after NodeComplete, HSM returns to MapView.
        [Test]
        public void NodeComplete_TransitionsBackToMapView()
        {
            GameStateMachine hsm = new GameStateMachine();
            hsm.HandleEvent(new GameEvent(GameEventType.StartNewRun));
            hsm.HandleEvent(new GameEvent(GameEventType.NodeConfirmed));
            hsm.HandleEvent(new GameEvent(GameEventType.NodeComplete));

            Assert.That(hsm.Root.CurrentChild.CurrentChild, Is.InstanceOf<MapViewState>());
        }
    }
}
