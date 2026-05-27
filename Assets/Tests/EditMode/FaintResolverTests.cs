using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.8 — coverage for faint resolution:
    // IsFainted (§2.4.1), IsSlotLockedForSwap precedence (§3.3.5.1),
    // PurgeCards from deck + discard (§4.8.4), Trauma stack increment
    // (§6.2.2), All-Faint defeat (§3.3.6), eligible Lead replacements
    // (§4.8.1).
    public class FaintResolverTests
    {
        private PokemonSpeciesSO _species;

        [SetUp]
        public void SetUp()
        {
            _species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            _species.Types = new List<PokemonType> { PokemonType.Normal };
            _species.BaseStats = new BaseStats { BaseHP = 60, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            _species.GrowthCurve = null;
            _species.StatusImmunities = new List<StatusCondition>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_species);

        private PokemonInstance MakeAlive(int hp = 60) =>
            new() { Species = _species, Level = 1, CurrentHP = hp };

        private PokemonInstance MakeFainted() =>
            new() { Species = _species, Level = 1, CurrentHP = 0 };

        private static MoveSO MakeMove(string name = "TestMove")
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = name;
            m.Type = PokemonType.Normal;
            m.BasePower = 40;
            m.APCost = 1;
            return m;
        }

        // ── Bucket 1: IsFainted (§2.4.1) ─────────────────────────────────────

        [Test]
        public void IsFainted_HPAboveZero_False()
        {
            Assert.That(FaintResolver.IsFainted(MakeAlive(1)), Is.False);
            Assert.That(FaintResolver.IsFainted(MakeAlive(60)), Is.False);
        }

        [Test]
        public void IsFainted_HPZero_True()
        {
            Assert.That(FaintResolver.IsFainted(MakeFainted()), Is.True);
        }

        [Test]
        public void IsFainted_NullInstance_False()
        {
            Assert.That(FaintResolver.IsFainted(null), Is.False);
        }

        // ── Bucket 2: IsSlotLockedForSwap — faint > Freeze (§3.3.5.1) ───────

        [Test]
        public void IsSlotLockedForSwap_AliveFrozen_True()
        {
            PokemonInstance p = MakeAlive();
            p.PrimaryStatus = StatusCondition.Freeze;
            Assert.That(FaintResolver.IsSlotLockedForSwap(p), Is.True);
        }

        [Test]
        public void IsSlotLockedForSwap_FaintedFrozen_False()
        {
            // The load-bearing rule (§3.3.5.1): faint precedence voids Freeze lock.
            PokemonInstance p = MakeFainted();
            p.PrimaryStatus = StatusCondition.Freeze;
            Assert.That(FaintResolver.IsSlotLockedForSwap(p), Is.False);
        }

        [Test]
        public void IsSlotLockedForSwap_AliveOtherStatus_False()
        {
            PokemonInstance p = MakeAlive();
            p.PrimaryStatus = StatusCondition.Sleep; // unplayable but NOT position-locked
            Assert.That(FaintResolver.IsSlotLockedForSwap(p), Is.False);
        }

        [Test]
        public void IsSlotLockedForSwap_EmptySlot_False()
        {
            Assert.That(FaintResolver.IsSlotLockedForSwap(null), Is.False);
        }

        // ── Bucket 3: Trauma stack on faint (§6.2.2) ─────────────────────────

        [Test]
        public void ApplyTraumaOnFaint_IncrementsByOne()
        {
            PokemonInstance p = MakeFainted();
            Assert.That(p.TraumaStacks, Is.EqualTo(0));
            int after = FaintResolver.ApplyTraumaOnFaint(p);
            Assert.That(after, Is.EqualTo(1));
            Assert.That(p.TraumaStacks, Is.EqualTo(1));
        }

        [Test]
        public void ApplyTraumaOnFaint_NullInstance_ReturnsZero_NoThrow()
        {
            Assert.That(FaintResolver.ApplyTraumaOnFaint(null), Is.EqualTo(0));
        }

        [Test]
        public void ApplyTraumaOnFaint_RepeatedCalls_StackMonotonic()
        {
            // CombatController contract: call exactly once per faint event.
            // The test only verifies arithmetic; spamming is a caller bug,
            // not a resolver bug.
            PokemonInstance p = MakeAlive();
            FaintResolver.ApplyTraumaOnFaint(p);
            FaintResolver.ApplyTraumaOnFaint(p);
            FaintResolver.ApplyTraumaOnFaint(p);
            Assert.That(p.TraumaStacks, Is.EqualTo(3));
        }

        // ── Bucket 4: PurgeCards (§4.8.4) ────────────────────────────────────

        [Test]
        public void PurgeCards_RemovesAllOwnerCardsFromBothLists()
        {
            PokemonInstance fainted = MakeFainted();
            PokemonInstance ally = MakeAlive();

            MoveSO fm1 = MakeMove("F1");
            MoveSO fm2 = MakeMove("F2");
            MoveSO am1 = MakeMove("A1");

            List<CardEntry> deck = new()
            {
                new(fm1, fainted),
                new(am1, ally),
                new(fm2, fainted),
            };
            List<CardEntry> discard = new()
            {
                new(am1, ally),
                new(fm1, fainted),
            };

            int removed = FaintResolver.PurgeCards(fainted, deck, discard);

            Assert.That(removed, Is.EqualTo(3));
            Assert.That(deck.Count, Is.EqualTo(1));
            Assert.That(deck[0].Owner, Is.SameAs(ally));
            Assert.That(discard.Count, Is.EqualTo(1));
            Assert.That(discard[0].Owner, Is.SameAs(ally));

            Object.DestroyImmediate(fm1);
            Object.DestroyImmediate(fm2);
            Object.DestroyImmediate(am1);
        }

        [Test]
        public void PurgeCards_NoMatches_ReturnsZero_ListsUnchanged()
        {
            PokemonInstance fainted = MakeFainted();
            PokemonInstance ally = MakeAlive();
            MoveSO am1 = MakeMove();
            List<CardEntry> deck = new() { new(am1, ally), new(am1, ally) };
            List<CardEntry> discard = new() { new(am1, ally) };

            int removed = FaintResolver.PurgeCards(fainted, deck, discard);

            Assert.That(removed, Is.EqualTo(0));
            Assert.That(deck.Count, Is.EqualTo(2));
            Assert.That(discard.Count, Is.EqualTo(1));
            Object.DestroyImmediate(am1);
        }

        [Test]
        public void PurgeCards_NullDeckOrDiscard_DoesNotThrow()
        {
            PokemonInstance fainted = MakeFainted();
            Assert.DoesNotThrow(() => FaintResolver.PurgeCards(fainted, null, null));
        }

        [Test]
        public void PurgeCards_FaintedOwnsEveryCard_DeckEmptiesCompletely()
        {
            PokemonInstance fainted = MakeFainted();
            MoveSO m = MakeMove();
            List<CardEntry> deck = new()
            {
                new(m, fainted), new(m, fainted), new(m, fainted), new(m, fainted),
            };
            List<CardEntry> discard = new() { new(m, fainted) };

            int removed = FaintResolver.PurgeCards(fainted, deck, discard);
            Assert.That(removed, Is.EqualTo(5));
            Assert.That(deck, Is.Empty);
            Assert.That(discard, Is.Empty);
            Object.DestroyImmediate(m);
        }

        // ── Bucket 5: All-Faint defeat condition (§3.3.6) ────────────────────

        [Test]
        public void IsAllFainted_AllZeroHP_True()
        {
            List<PokemonInstance> team = new() { MakeFainted(), MakeFainted(), MakeFainted() };
            Assert.That(FaintResolver.IsAllFainted(team), Is.True);
        }

        [Test]
        public void IsAllFainted_OneSurvivor_False()
        {
            List<PokemonInstance> team = new() { MakeFainted(), MakeAlive(), MakeFainted() };
            Assert.That(FaintResolver.IsAllFainted(team), Is.False);
        }

        [Test]
        public void IsAllFainted_EmptySlotsCountAsAbsent()
        {
            // Team of two fainted + one null slot = defeated (no survivors).
            List<PokemonInstance> team = new() { MakeFainted(), null, MakeFainted() };
            Assert.That(FaintResolver.IsAllFainted(team), Is.True);
        }

        [Test]
        public void IsAllFainted_NullList_True()
        {
            Assert.That(FaintResolver.IsAllFainted(null), Is.True);
        }

        [Test]
        public void IsAllFainted_OneLivingBenchAfterLeadFaint_False()
        {
            // The defeat check fires AFTER faint resolution. If the lead just
            // fainted but the bench has a survivor, the player still picks a
            // replacement — combat continues.
            PokemonInstance lead = MakeFainted();
            PokemonInstance bench = MakeAlive();
            List<PokemonInstance> team = new() { lead, bench, null };
            Assert.That(FaintResolver.IsAllFainted(team), Is.False);
        }

        // ── Bucket 6: EligibleLeadReplacements (§4.8.1) ─────────────────────

        [Test]
        public void EligibleLeadReplacements_ExcludesLeadAndFainted()
        {
            PokemonInstance lead = MakeFainted();
            PokemonInstance benchAlive = MakeAlive();
            PokemonInstance benchFainted = MakeFainted();
            PokemonInstance benchAlive2 = MakeAlive();
            List<PokemonInstance> team = new() { lead, benchAlive, benchFainted, benchAlive2 };

            List<PokemonInstance> opts = FaintResolver.EligibleLeadReplacements(team, lead);

            Assert.That(opts.Count, Is.EqualTo(2));
            Assert.That(opts, Has.Member(benchAlive));
            Assert.That(opts, Has.Member(benchAlive2));
            Assert.That(opts, Has.No.Member(lead));
            Assert.That(opts, Has.No.Member(benchFainted));
        }

        [Test]
        public void EligibleLeadReplacements_NoCandidates_ReturnsEmptyList()
        {
            // Lead fainted, every other slot also fainted or null.
            // Caller (CombatController) sees Count == 0 → All-Faint defeat path.
            PokemonInstance lead = MakeFainted();
            List<PokemonInstance> team = new() { lead, MakeFainted(), null };

            List<PokemonInstance> opts = FaintResolver.EligibleLeadReplacements(team, lead);
            Assert.That(opts, Is.Empty);
        }

        [Test]
        public void EligibleLeadReplacements_FrozenBenchMemberIsEligible()
        {
            // Frozen ≠ fainted. A frozen bench member is still a valid Lead pick
            // — position-lock only matters once they ARE Lead.
            PokemonInstance lead = MakeFainted();
            PokemonInstance frozenBench = MakeAlive();
            frozenBench.PrimaryStatus = StatusCondition.Freeze;
            List<PokemonInstance> team = new() { lead, frozenBench };

            List<PokemonInstance> opts = FaintResolver.EligibleLeadReplacements(team, lead);
            Assert.That(opts, Has.Member(frozenBench));
        }

        // ── Bucket 7: end-to-end combined scenarios ──────────────────────────

        [Test]
        public void FullFaintFlow_LeadFreezeFaints_LockVoidPurgeAndTrauma()
        {
            // Scenario: Lead is Frozen, takes lethal damage → faints.
            // Expect:
            //   • IsSlotLockedForSwap returns false (faint > freeze)
            //   • EligibleLeadReplacements offers the surviving bench member
            //   • PurgeCards removes the fainted Lead's cards from deck+discard
            //   • Trauma stack increments by 1
            //   • IsAllFainted returns false (survivor still standing)
            PokemonInstance lead = MakeAlive();
            lead.PrimaryStatus = StatusCondition.Freeze;
            PokemonInstance bench = MakeAlive();
            List<PokemonInstance> team = new() { lead, bench };

            // Lethal damage
            lead.CurrentHP = 0;

            Assert.That(FaintResolver.IsSlotLockedForSwap(lead), Is.False, "Faint > Freeze");

            List<PokemonInstance> repl = FaintResolver.EligibleLeadReplacements(team, lead);
            Assert.That(repl, Has.Member(bench));

            MoveSO leadMove = MakeMove("LeadMove");
            MoveSO benchMove = MakeMove("BenchMove");
            List<CardEntry> deck = new() { new(leadMove, lead), new(benchMove, bench) };
            List<CardEntry> discard = new() { new(leadMove, lead) };
            int removed = FaintResolver.PurgeCards(lead, deck, discard);
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(deck.Count, Is.EqualTo(1));
            Assert.That(deck[0].Owner, Is.SameAs(bench));
            Assert.That(discard, Is.Empty);

            int trauma = FaintResolver.ApplyTraumaOnFaint(lead);
            Assert.That(trauma, Is.EqualTo(1));

            Assert.That(FaintResolver.IsAllFainted(team), Is.False);

            Object.DestroyImmediate(leadMove);
            Object.DestroyImmediate(benchMove);
        }
    }
}
