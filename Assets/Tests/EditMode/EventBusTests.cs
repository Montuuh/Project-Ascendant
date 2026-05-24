using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.4 — EventBus Hybrid Model unit tests.
    public class EventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // ── Static EventBus ─────────────────────────────────────────────────────

        // Per §9.4.1.2 — Subscribe + Publish delivers payload to handler.
        [Test]
        public void EventBus_SubscribeAndPublish_HandlerReceivesPayload()
        {
            DamageContext received = default;
            EventBus.Subscribe<DamageContext>(p => received = p);

            DamageContext sent = new DamageContext(slotIndex: 1, baseDamage: 30, finalDamage: 45,
                isCrit: true, isStab: true, typeMultiplier: 2f);
            EventBus.Publish(sent);

            Assert.That(received.SlotIndex, Is.EqualTo(1));
            Assert.That(received.FinalDamage, Is.EqualTo(45));
            Assert.That(received.IsCrit, Is.True);
        }

        // Per §9.4.1.2 — Unsubscribe removes handler; no further calls.
        [Test]
        public void EventBus_Unsubscribe_HandlerNotCalled()
        {
            int callCount = 0;
            void Handler(TurnContext _) => callCount++;

            EventBus.Subscribe<TurnContext>(Handler);
            EventBus.Unsubscribe<TurnContext>(Handler);
            EventBus.Publish(new TurnContext(turnNumber: 1, remainingAP: 3));

            Assert.That(callCount, Is.EqualTo(0));
        }

        // Per §9.4.1.2 — Publish with no subscribers must not throw.
        [Test]
        public void EventBus_PublishWithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                EventBus.Publish(new FaintContext(slotIndex: 0, wasLead: true)));
        }

        // Per §9.4.1.2 — Multiple subscribers all receive the event.
        [Test]
        public void EventBus_MultipleSubscribers_AllReceivePayload()
        {
            int callCount = 0;
            EventBus.Subscribe<TurnContext>(_ => callCount++);
            EventBus.Subscribe<TurnContext>(_ => callCount++);
            EventBus.Subscribe<TurnContext>(_ => callCount++);

            EventBus.Publish(new TurnContext(turnNumber: 2, remainingAP: 2));

            Assert.That(callCount, Is.EqualTo(3));
        }

        // Per §9.4.1.2 — Clear removes all subscribers across all types.
        [Test]
        public void EventBus_Clear_RemovesAllSubscribers()
        {
            int damageCount = 0;
            int turnCount = 0;
            EventBus.Subscribe<DamageContext>(_ => damageCount++);
            EventBus.Subscribe<TurnContext>(_ => turnCount++);

            EventBus.Clear();

            EventBus.Publish(new DamageContext(0, 10, 10, false, false, 1f));
            EventBus.Publish(new TurnContext(1, 3));

            Assert.That(damageCount, Is.EqualTo(0));
            Assert.That(turnCount, Is.EqualTo(0));
        }

        // Per §9.4.1.2 — Subscriptions for different payload types are independent.
        [Test]
        public void EventBus_DifferentPayloadTypes_DoNotCrossfire()
        {
            bool damageReceived = false;
            bool faintReceived = false;
            EventBus.Subscribe<DamageContext>(_ => damageReceived = true);
            EventBus.Subscribe<FaintContext>(_ => faintReceived = true);

            EventBus.Publish(new DamageContext(0, 10, 10, false, false, 1f));

            Assert.That(damageReceived, Is.True);
            Assert.That(faintReceived, Is.False);
        }

        // Per §9.4.1.2 — Unsubscribe with no registered handler is a no-op.
        [Test]
        public void EventBus_UnsubscribeNotRegistered_DoesNotThrow()
        {
            void Handler(RunEndedContext _) { }
            Assert.DoesNotThrow(() => EventBus.Unsubscribe<RunEndedContext>(Handler));
        }

        // ── GameEventSO (SO channel) ─────────────────────────────────────────────

        // Per §9.4.1.1 — Register + Raise delivers payload to listener.
        [Test]
        public void GameEventSO_RegisterAndRaise_ListenerReceivesPayload()
        {
            DamageEventChannelSO channel = ScriptableObject.CreateInstance<DamageEventChannelSO>();
            DamageContext received = default;
            channel.Register(p => received = p);

            DamageContext sent = new DamageContext(slotIndex: 2, baseDamage: 20, finalDamage: 30,
                isCrit: false, isStab: false, typeMultiplier: 1f);
            channel.Raise(sent);

            Assert.That(received.SlotIndex, Is.EqualTo(2));
            Assert.That(received.FinalDamage, Is.EqualTo(30));

            Object.DestroyImmediate(channel);
        }

        // Per §9.4.1.1 — Unregister removes listener; no further calls.
        [Test]
        public void GameEventSO_Unregister_ListenerNotCalled()
        {
            FaintEventChannelSO channel = ScriptableObject.CreateInstance<FaintEventChannelSO>();
            int callCount = 0;
            void Listener(FaintContext _) => callCount++;

            channel.Register(Listener);
            channel.Unregister(Listener);
            channel.Raise(new FaintContext(slotIndex: 0, wasLead: true));

            Assert.That(callCount, Is.EqualTo(0));

            Object.DestroyImmediate(channel);
        }

        // Per §9.4.1.1 — Raise with no listeners must not throw.
        [Test]
        public void GameEventSO_RaiseWithNoListeners_DoesNotThrow()
        {
            TurnEventChannelSO channel = ScriptableObject.CreateInstance<TurnEventChannelSO>();

            Assert.DoesNotThrow(() => channel.Raise(new TurnContext(turnNumber: 1, remainingAP: 3)));

            Object.DestroyImmediate(channel);
        }

        // Per §9.4.1.1 — Raise is snapshot-safe: listener registered during Raise
        // does not receive the in-flight event, and does not cause InvalidOperationException.
        [Test]
        public void GameEventSO_RegisterDuringRaise_DoesNotThrowAndDoesNotReceiveCurrentEvent()
        {
            LeadChangeEventChannelSO channel = ScriptableObject.CreateInstance<LeadChangeEventChannelSO>();
            int lateCallCount = 0;

            channel.Register(_ =>
            {
                // Register a second listener during the Raise — snapshot must prevent this
                // from being called in the same dispatch.
                channel.Register(__ => lateCallCount++);
            });

            channel.Raise(new LeadChangeContext(previousSlotIndex: 0, newSlotIndex: 1, apCost: 1));

            Assert.That(lateCallCount, Is.EqualTo(0));

            Object.DestroyImmediate(channel);
        }

        // ── Payload struct correctness ───────────────────────────────────────────

        // Per §9.4.1.2 — DamageContext fields initialise correctly.
        [Test]
        public void DamageContext_FieldsInitialiseCorrectly()
        {
            DamageContext ctx = new DamageContext(
                slotIndex: 2, baseDamage: 40, finalDamage: 60,
                isCrit: true, isStab: false, typeMultiplier: 2f);

            Assert.That(ctx.SlotIndex, Is.EqualTo(2));
            Assert.That(ctx.BaseDamage, Is.EqualTo(40));
            Assert.That(ctx.FinalDamage, Is.EqualTo(60));
            Assert.That(ctx.IsCrit, Is.True);
            Assert.That(ctx.IsStab, Is.False);
            Assert.That(ctx.TypeMultiplier, Is.EqualTo(2f));
        }

        // Per §9.4.1.2 — RunEndedContext fields initialise correctly.
        [Test]
        public void RunEndedContext_FieldsInitialiseCorrectly()
        {
            RunEndedContext ctx = new RunEndedContext(victory: true, turnCount: 42, nodesCleared: 7);

            Assert.That(ctx.Victory, Is.True);
            Assert.That(ctx.TurnCount, Is.EqualTo(42));
            Assert.That(ctx.NodesCleared, Is.EqualTo(7));
        }
    }
}
