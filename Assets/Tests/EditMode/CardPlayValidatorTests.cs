using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.3 / §3.6 + Epic 5 Task 5.6.1 — CardPlayValidator covers the
    // Melee-vs-Ranged play-position rule, Step-Forward override, and the
    // owner-absent / owner-fainted guards.
    public class CardPlayValidatorTests
    {
        private PokemonSpeciesSO _species;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            _species.Types = new List<PokemonType> { PokemonType.Normal };
            _species.BaseStats = new BaseStats { BaseHP = 60, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            _species.GrowthCurve = null;
            _species.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(_species);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        private MoveSO Mk(MoveRole role, MoveRange range, PositionalModifier mod)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = $"{role}_{range}_{mod}";
            m.Type = PokemonType.Normal;
            m.BasePower = 40;
            m.APCost = 1;
            m.Role = role;
            m.Range = range;
            m.Modifier = mod;
            _disposables.Add(m);
            return m;
        }

        private PokemonInstance MakeAlive() =>
            new() { Species = _species, Level = 1, CurrentHP = 60 };

        // ── Position eligibility (pure) ──────────────────────────────────────

        [Test]
        public void Position_MeleeFromLead_Eligible()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Melee, PositionalModifier.None);
            Assert.That(CardPlayValidator.IsPositionEligible(m, ownerSlot: 0, leadIndex: 0),
                Is.True);
        }

        [Test]
        public void Position_MeleeFromBench_NoSF_Ineligible()
        {
            // Per §3.2.4 — "Melee cards can only be played from the Lead position,
            //   unless they carry the Step-Forward modifier."
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Melee, PositionalModifier.None);
            Assert.That(CardPlayValidator.IsPositionEligible(m, ownerSlot: 1, leadIndex: 0),
                Is.False);
        }

        [Test]
        public void Position_MeleeFromBench_WithSF_Eligible()
        {
            // Per §3.3.2 — Step-Forward promotes the bench owner to Lead before
            // the effect resolves. Validator approves the play from bench.
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Melee, PositionalModifier.StepForward);
            Assert.That(CardPlayValidator.IsPositionEligible(m, ownerSlot: 1, leadIndex: 0),
                Is.True);
        }

        [Test]
        public void Position_RangedFromAnywhere_Eligible()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None);
            Assert.That(CardPlayValidator.IsPositionEligible(m, 0, 0), Is.True);
            Assert.That(CardPlayValidator.IsPositionEligible(m, 1, 0), Is.True);
            Assert.That(CardPlayValidator.IsPositionEligible(m, 2, 0), Is.True);
        }

        [Test]
        public void Position_MeleeFromBench_WithSB_Ineligible()
        {
            // Per §3.3.3 — Step-Backward resolves THEN swaps; the owner must
            // already be Lead at play time. Validator rejects SB-from-bench.
            MoveSO m = Mk(MoveRole.Defensive, MoveRange.Melee, PositionalModifier.StepBackward);
            Assert.That(CardPlayValidator.IsPositionEligible(m, ownerSlot: 1, leadIndex: 0),
                Is.False);
        }

        // ── Full Validate (card + team + lead) ───────────────────────────────

        [Test]
        public void Validate_NullCard_NullCard()
        {
            PokemonInstance p = MakeAlive();
            Assert.That(CardPlayValidator.Validate(null, new List<PokemonInstance> { p }, 0),
                Is.EqualTo(CardPlayValidator.PlayResult.NullCard));
        }

        [Test]
        public void Validate_NullMove_NullCard()
        {
            MoveCardInstance card = new() { Move = null, Owner = MakeAlive() };
            Assert.That(CardPlayValidator.Validate(card, new List<PokemonInstance> { card.Owner }, 0),
                Is.EqualTo(CardPlayValidator.PlayResult.NullCard));
        }

        [Test]
        public void Validate_OwnerNotInTeam_OwnerAbsent()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None);
            PokemonInstance stranger = MakeAlive();
            MoveCardInstance card = new() { Move = m, Owner = stranger };
            List<PokemonInstance> team = new() { MakeAlive(), MakeAlive() };
            Assert.That(CardPlayValidator.Validate(card, team, 0),
                Is.EqualTo(CardPlayValidator.PlayResult.OwnerAbsent));
        }

        [Test]
        public void Validate_OwnerFainted_OwnerFainted()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None);
            PokemonInstance owner = MakeAlive();
            owner.CurrentHP = 0;
            MoveCardInstance card = new() { Move = m, Owner = owner };
            Assert.That(CardPlayValidator.Validate(card, new List<PokemonInstance> { owner }, 0),
                Is.EqualTo(CardPlayValidator.PlayResult.OwnerFainted));
        }

        [Test]
        public void Validate_MeleeOwnerOnBench_MeleeFromBenchNoSF()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Melee, PositionalModifier.None);
            PokemonInstance lead = MakeAlive();
            PokemonInstance bench = MakeAlive();
            MoveCardInstance card = new() { Move = m, Owner = bench };
            Assert.That(CardPlayValidator.Validate(card, new List<PokemonInstance> { lead, bench }, leadIndex: 0),
                Is.EqualTo(CardPlayValidator.PlayResult.MeleeFromBenchNoSF));
        }

        [Test]
        public void Validate_MeleeWithSFOnBench_Playable()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Melee, PositionalModifier.StepForward);
            PokemonInstance lead = MakeAlive();
            PokemonInstance bench = MakeAlive();
            MoveCardInstance card = new() { Move = m, Owner = bench };
            Assert.That(CardPlayValidator.Validate(card, new List<PokemonInstance> { lead, bench }, leadIndex: 0),
                Is.EqualTo(CardPlayValidator.PlayResult.Playable));
        }

        [Test]
        public void Validate_RangedFromBench_Playable()
        {
            MoveSO m = Mk(MoveRole.Offensive, MoveRange.Ranged, PositionalModifier.None);
            PokemonInstance lead = MakeAlive();
            PokemonInstance bench = MakeAlive();
            MoveCardInstance card = new() { Move = m, Owner = bench };
            Assert.That(CardPlayValidator.Validate(card, new List<PokemonInstance> { lead, bench }, leadIndex: 0),
                Is.EqualTo(CardPlayValidator.PlayResult.Playable));
        }
    }
}
