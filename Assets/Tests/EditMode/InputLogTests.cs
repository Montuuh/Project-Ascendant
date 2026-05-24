using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.7.4 + §9.10.3 + Task 2.6.4 — InputLog recorder/replayer unit tests.
    public class InputLogTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        // ── InputLog ─────────────────────────────────────────────────────────────

        [Test]
        public void InputLog_Add_StoresEntriesInOrder()
        {
            // Per §9.10.3 — entries must preserve insertion order for determinism.
            InputLog log = new();
            log.Add("PlayCard_0", 0);
            log.Add("EndTurn", 1);

            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log.Entries[0].ActionId, Is.EqualTo("PlayCard_0"));
            Assert.That(log.Entries[0].LogicalTick, Is.EqualTo(0));
            Assert.That(log.Entries[1].ActionId, Is.EqualTo("EndTurn"));
            Assert.That(log.Entries[1].LogicalTick, Is.EqualTo(1));
        }

        [Test]
        public void InputLog_Clear_RemovesAllEntries()
        {
            InputLog log = new();
            log.Add("PlayCard_0", 0);
            log.Clear();
            Assert.That(log.Count, Is.EqualTo(0));
        }

        // ── InputLogRecorder ─────────────────────────────────────────────────────

        [Test]
        public void InputLogRecorder_Record_AppendsToLog()
        {
            // Per §9.10.3 — committed inputs must be appended at current logical tick.
            InputLog log = new();
            InputLogRecorder recorder = new(log);
            recorder.Record("SwapLead_1");
            recorder.Record("EndTurn");

            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log.Entries[0].ActionId, Is.EqualTo("SwapLead_1"));
            Assert.That(log.Entries[1].ActionId, Is.EqualTo("EndTurn"));
        }

        [Test]
        public void InputLogRecorder_AdvanceTick_StampsCorrectTickOnSubsequentRecords()
        {
            // Per §9.10.3 — logical tick must be the frame tick at time of recording.
            InputLog log = new();
            InputLogRecorder recorder = new(log);
            recorder.Record("PlayCard_0");
            recorder.AdvanceTick();
            recorder.Record("EndTurn");

            Assert.That(log.Entries[0].LogicalTick, Is.EqualTo(0));
            Assert.That(log.Entries[1].LogicalTick, Is.EqualTo(1));
        }

        [Test]
        public void InputLogRecorder_Reset_ClearsLogAndResetsTick()
        {
            InputLog log = new();
            InputLogRecorder recorder = new(log);
            recorder.Record("EndTurn");
            recorder.AdvanceTick();
            recorder.Reset();

            Assert.That(log.Count, Is.EqualTo(0));
            Assert.That(recorder.LogicalTick, Is.EqualTo(0));
        }

        [Test]
        public void InputLogRecorder_PayloadHash_StoredCorrectly()
        {
            // Per §9.7.4 — payload hash enables content verification during replay.
            InputLog log = new();
            InputLogRecorder recorder = new(log);
            recorder.Record("PlayCard_2", 0xDEADBEEFu);

            Assert.That(log.Entries[0].PayloadHash, Is.EqualTo(0xDEADBEEFu));
        }

        // ── InputLogReplayer — round-trip ────────────────────────────────────────

        [Test]
        public void InputLogReplayer_Replay_ProducesIdenticalEventStream()
        {
            // Per §9.7.4 — record + replay must produce the identical ActionId sequence
            // at the identical logical ticks, with identical payload hashes.
            InputLog log = new();
            InputLogRecorder recorder = new(log);

            recorder.Record("PlayCard_2", 0xDEADu);
            recorder.AdvanceTick();
            recorder.Record("SwapLead_1");
            recorder.AdvanceTick();
            recorder.Record("EndTurn", 0xBEEFu);

            List<InputReplayedContext> replayed = new();
            Action<InputReplayedContext> handler = ctx => replayed.Add(ctx);
            EventBus.Subscribe<InputReplayedContext>(handler);

            try
            {
                new InputLogReplayer(log).Replay();
            }
            finally
            {
                EventBus.Unsubscribe<InputReplayedContext>(handler);
            }

            Assert.That(replayed.Count, Is.EqualTo(3));

            Assert.That(replayed[0].ActionId,    Is.EqualTo("PlayCard_2"));
            Assert.That(replayed[0].LogicalTick, Is.EqualTo(0));
            Assert.That(replayed[0].PayloadHash, Is.EqualTo(0xDEADu));

            Assert.That(replayed[1].ActionId,    Is.EqualTo("SwapLead_1"));
            Assert.That(replayed[1].LogicalTick, Is.EqualTo(1));

            Assert.That(replayed[2].ActionId,    Is.EqualTo("EndTurn"));
            Assert.That(replayed[2].LogicalTick, Is.EqualTo(2));
            Assert.That(replayed[2].PayloadHash, Is.EqualTo(0xBEEFu));
        }
    }
}
