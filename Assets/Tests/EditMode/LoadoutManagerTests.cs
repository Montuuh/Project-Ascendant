using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §2.3 / §2.4.1 + Epic 9 Task 9.10 — LoadoutManager (Active Team commit + lock).
    public class LoadoutManagerTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private PokemonSpeciesSO MakeSpecies()
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.BaseStats = new BaseStats { BaseHP = 40 };
            _disposables.Add(s);
            return s;
        }

        // Box of `count` Pokémon; index `faintedIdx` (if >=0) is fainted (CurrentHP 0).
        private Box MakeBox(int count, int faintedIdx = -1)
        {
            Box box = new(6);
            for (int i = 0; i < count; i++)
                box.Members.Add(new PokemonInstance { Species = MakeSpecies(), Level = 5, CurrentHP = i == faintedIdx ? 0 : 30 });
            return box;
        }

        private RunStateSO MakeRun()
        {
            RunStateSO r = ScriptableObject.CreateInstance<RunStateSO>();
            _disposables.Add(r);
            return r;
        }

        // ── Confirm (9.10.1) ──────────────────────────────────────────────────

        [Test]
        public void Confirm_ValidSelection_CommitsToRun()
        {
            RunStateSO run = MakeRun();
            LoadoutManager m = new(run, MakeBox(4));
            bool ok = m.Confirm(new List<int> { 0, 2, 3 }, leadSlot: 1);
            Assert.That(ok, Is.True);
            Assert.That(run.ActiveTeamIndices, Is.EqualTo(new List<int> { 0, 2, 3 }));
            Assert.That(run.LeadIndex, Is.EqualTo(1));
        }

        [Test]
        public void Confirm_SingleMember_Allowed()
        {
            RunStateSO run = MakeRun();
            LoadoutManager m = new(run, MakeBox(1));
            Assert.That(m.Confirm(new List<int> { 0 }, 0), Is.True);
        }

        [Test]
        public void Confirm_MoreThanThree_Rejected()
        {
            LoadoutManager m = new(MakeRun(), MakeBox(5));
            Assert.That(m.Confirm(new List<int> { 0, 1, 2, 3 }, 0), Is.False);
        }

        [Test]
        public void Confirm_FaintedMember_Rejected()
        {
            // Per §2.4.1 — a fainted Pokémon cannot be in the Active Team.
            LoadoutManager m = new(MakeRun(), MakeBox(4, faintedIdx: 2));
            Assert.That(m.Confirm(new List<int> { 0, 2 }, 0), Is.False);
        }

        [Test]
        public void Confirm_DuplicateIndex_Rejected()
        {
            LoadoutManager m = new(MakeRun(), MakeBox(4));
            Assert.That(m.Confirm(new List<int> { 1, 1 }, 0), Is.False);
        }

        [Test]
        public void Confirm_OutOfRangeIndex_Rejected()
        {
            LoadoutManager m = new(MakeRun(), MakeBox(2));
            Assert.That(m.Confirm(new List<int> { 0, 5 }, 0), Is.False);
        }

        [Test]
        public void Confirm_BadLeadSlot_Rejected()
        {
            LoadoutManager m = new(MakeRun(), MakeBox(3));
            Assert.That(m.Confirm(new List<int> { 0, 1 }, leadSlot: 2), Is.False); // lead must index the team
        }

        [Test]
        public void Confirm_Empty_Rejected()
        {
            LoadoutManager m = new(MakeRun(), MakeBox(3));
            Assert.That(m.Confirm(new List<int>(), 0), Is.False);
        }

        // ── Lock (9.10.2 / 9.10.3) ────────────────────────────────────────────

        [Test]
        public void Confirm_WhileLocked_Refused_NoChange()
        {
            // Per §2.3 — Active Team is locked on node entry; no changes during the node/combat.
            RunStateSO run = MakeRun();
            LoadoutManager m = new(run, MakeBox(4));
            m.Confirm(new List<int> { 0, 1 }, 0); // initial loadout

            m.Lock();
            bool ok = m.Confirm(new List<int> { 2, 3 }, 0);
            Assert.That(ok, Is.False);
            Assert.That(run.ActiveTeamIndices, Is.EqualTo(new List<int> { 0, 1 }), "Loadout must not change while locked.");
        }

        [Test]
        public void Unlock_RestoresConfirm()
        {
            RunStateSO run = MakeRun();
            LoadoutManager m = new(run, MakeBox(4));
            m.Lock();
            Assert.That(m.Confirm(new List<int> { 0 }, 0), Is.False);
            m.Unlock();
            Assert.That(m.Confirm(new List<int> { 0 }, 0), Is.True);
        }

        [Test]
        public void IsLocked_DefaultsFalse()
        {
            LoadoutManager m = new(MakeRun(), MakeBox(3));
            Assert.That(m.IsLocked, Is.False);
        }
    }
}
