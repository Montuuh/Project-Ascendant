using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.Tests
{
    // Per §6.2.4 + Epic 12 Task 12.4.B / 11.1.10 — Trauma Salve clears all Trauma + is consumed.
    public class TraumaSalveApplicatorTests
    {
        private readonly List<Object> _disp = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disp) if (o != null) Object.DestroyImmediate(o);
            _disp.Clear();
        }

        private RelicSO Salve() { RelicSO r = ScriptableObject.CreateInstance<RelicSO>(); r.Id = "trauma_salve"; _disp.Add(r); return r; }

        [Test]
        public void Apply_ClearsAllTrauma_AndConsumesSalve()
        {
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>(); _disp.Add(run);
            RelicSO salve = Salve();
            run.HeldRelics = new List<RelicSO> { salve };
            PokemonInstance mon = new() { TraumaStacks = 4 };

            bool ok = TraumaSalveApplicator.Apply(run, salve, mon);

            Assert.That(ok, Is.True);
            Assert.That(mon.TraumaStacks, Is.EqualTo(0));
            Assert.That(run.HeldRelics, Has.No.Member(salve), "single-charge — consumed.");
        }

        [Test]
        public void Apply_ZeroTrauma_NotWasted()
        {
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>(); _disp.Add(run);
            RelicSO salve = Salve();
            run.HeldRelics = new List<RelicSO> { salve };
            PokemonInstance mon = new() { TraumaStacks = 0 };

            bool ok = TraumaSalveApplicator.Apply(run, salve, mon);

            Assert.That(ok, Is.False, "§6.2.6 — cannot waste on a 0-stack Pokémon.");
            Assert.That(run.HeldRelics, Has.Member(salve), "salve retained.");
        }
    }
}
