using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.4 + Epic 5 Task 5.1.4 — SkillDeck composition + draw + reshuffle
    // + faint purge coverage. The construction test (3 starters; mastered)
    // is the spec acceptance test for Task 5.1.4.
    public class SkillDeckTests
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

        private MoveSO MakeMove(string name)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = name;
            m.Type = PokemonType.Normal;
            m.BasePower = 40;
            m.APCost = 1;
            _disposables.Add(m);
            return m;
        }

        private PokemonInstance MakePokemon(int moveCount, MoveSO mastery = null)
        {
            PokemonInstance p = new() { Species = _species, Level = 1, CurrentHP = 60 };
            for (int i = 0; i < moveCount; i++) p.CurrentMoves.Add(MakeMove($"m{i}"));
            p.MasteryMove = mastery;
            return p;
        }

        // ── Build (§3.4) ─────────────────────────────────────────────────────

        [Test]
        public void Build_ThreeStarters_TwelveCards()
        {
            // Per §3.4 — "Built from the 4 moves of each Active Pokémon (12 cards baseline)".
            SkillDeck deck = new();
            List<PokemonInstance> team = new()
            {
                MakePokemon(4), MakePokemon(4), MakePokemon(4),
            };
            deck.Build(team);
            Assert.That(deck.DeckCount, Is.EqualTo(12));
            Assert.That(deck.DiscardCount, Is.EqualTo(0));
        }

        [Test]
        public void Build_OneMastered_AddsOneFlaggedCard()
        {
            // Per §3.4 + §4.3.9 — "Each mastered Pokémon adds one Mastery Move
            //   (+1 per mastered Pokémon, max 15 cards at full mastery)."
            SkillDeck deck = new();
            MoveSO mastery = MakeMove("M-mastery");
            List<PokemonInstance> team = new()
            {
                MakePokemon(4, mastery), MakePokemon(4), MakePokemon(4),
            };
            deck.Build(team);
            Assert.That(deck.DeckCount, Is.EqualTo(13));
            // The Mastery card is in the deck and IsMasteryMove == true.
            int masteryCount = 0;
            for (int i = 0; i < deck.DeckView.Count; i++)
                if (deck.DeckView[i].IsMasteryMove) masteryCount++;
            Assert.That(masteryCount, Is.EqualTo(1));
        }

        [Test]
        public void Build_AllThreeMastered_FifteenCards()
        {
            // Per §3.4 — max 15 cards at full mastery.
            SkillDeck deck = new();
            List<PokemonInstance> team = new()
            {
                MakePokemon(4, MakeMove("M1")),
                MakePokemon(4, MakeMove("M2")),
                MakePokemon(4, MakeMove("M3")),
            };
            deck.Build(team);
            Assert.That(deck.DeckCount, Is.EqualTo(15));
        }

        [Test]
        public void Build_NullPokemonSlot_Skipped()
        {
            SkillDeck deck = new();
            List<PokemonInstance> team = new() { MakePokemon(4), null, MakePokemon(4) };
            deck.Build(team);
            Assert.That(deck.DeckCount, Is.EqualTo(8));
        }

        [Test]
        public void Build_PartialMoveSlots_OnlyAddsNonNull()
        {
            // CurrentMoves may legitimately have fewer than 4 entries pre-evolution.
            SkillDeck deck = new();
            List<PokemonInstance> team = new() { MakePokemon(2) };
            deck.Build(team);
            Assert.That(deck.DeckCount, Is.EqualTo(2));
        }

        [Test]
        public void Build_OwnerReferencePreserved()
        {
            // Per Task 5.1.3 — every card tracks its owner so faint purge works.
            SkillDeck deck = new();
            PokemonInstance p = MakePokemon(4);
            deck.Build(new List<PokemonInstance> { p });
            for (int i = 0; i < deck.DeckView.Count; i++)
                Assert.That(deck.DeckView[i].Owner, Is.SameAs(p));
        }

        [Test]
        public void Build_TwiceWithDifferentTeam_FullRebuild()
        {
            SkillDeck deck = new();
            deck.Build(new List<PokemonInstance> { MakePokemon(4), MakePokemon(4) });
            Assert.That(deck.DeckCount, Is.EqualTo(8));
            deck.Build(new List<PokemonInstance> { MakePokemon(4) });
            Assert.That(deck.DeckCount, Is.EqualTo(4));
        }

        // ── Draw + Reshuffle (§3.4 / §3.2.2) ─────────────────────────────────

        [Test]
        public void Draw_RemovesCardFromDeck_NotIntoDiscard()
        {
            // Drawn cards live in the caller's hand — NOT in the discard pile
            // until played. This prevents the self-feeding reshuffle loop.
            SkillDeck deck = new();
            deck.Build(new List<PokemonInstance> { MakePokemon(4) });
            GameRNG rng = new(seed: 12345);
            MoveCardInstance card = deck.Draw(rng);
            Assert.That(card, Is.Not.Null);
            Assert.That(deck.DeckCount, Is.EqualTo(3));
            Assert.That(deck.DiscardCount, Is.EqualTo(0));
        }

        [Test]
        public void Draw_EmptyDeckWithDiscard_TriggersReshuffle()
        {
            // Per §3.4 — "When the Skill Deck empties, the discard pile reshuffles into it."
            SkillDeck deck = new();
            deck.Build(new List<PokemonInstance> { MakePokemon(4) });
            GameRNG rng = new(seed: 7);
            // Drain deck into hand, then move all to discard via Discard().
            List<MoveCardInstance> drained = new();
            while (deck.DeckCount > 0) drained.Add(deck.Draw(rng));
            for (int i = 0; i < drained.Count; i++) deck.Discard(drained[i]);
            Assert.That(deck.DeckCount, Is.EqualTo(0));
            Assert.That(deck.DiscardCount, Is.EqualTo(4));

            // Next draw should reshuffle (discard goes back to deck), then draw 1.
            MoveCardInstance reshuffled = deck.Draw(rng);
            Assert.That(reshuffled, Is.Not.Null);
            Assert.That(deck.DeckCount, Is.EqualTo(3));
            Assert.That(deck.DiscardCount, Is.EqualTo(0));
        }

        [Test]
        public void Draw_DeckAndDiscardBothEmpty_ReturnsNull()
        {
            SkillDeck deck = new();
            deck.Build(new List<PokemonInstance>());
            GameRNG rng = new(seed: 1);
            Assert.That(deck.Draw(rng), Is.Null);
        }

        [Test]
        public void Draw_SameSeed_SameSequence()
        {
            // Per Task 5.2.4 (foreshadowed) — deterministic draw via seeded CombatRNG.
            // Doing the assertion in 5.1 too because SkillDeck owns the Range call.
            List<PokemonInstance> team1 = new() { MakePokemon(4), MakePokemon(4), MakePokemon(4) };
            List<PokemonInstance> team2 = new() { MakePokemon(4), MakePokemon(4), MakePokemon(4) };
            // Hand cards are reference-equal to the deck's cards — to compare
            // sequences across two decks built from different Pokémon, compare
            // by Move.name + Owner index. Map owner to its index in the team.
            SkillDeck d1 = new();
            SkillDeck d2 = new();
            d1.Build(team1);
            d2.Build(team2);
            GameRNG r1 = new(seed: 0xDEADBEEF);
            GameRNG r2 = new(seed: 0xDEADBEEF);
            for (int i = 0; i < 5; i++)
            {
                MoveCardInstance c1 = d1.Draw(r1);
                MoveCardInstance c2 = d2.Draw(r2);
                Assert.That(c1.Move.name, Is.EqualTo(c2.Move.name),
                    $"Draw #{i} diverged: {c1.Move.name} vs {c2.Move.name}");
                int idx1 = team1.IndexOf(c1.Owner);
                int idx2 = team2.IndexOf(c2.Owner);
                Assert.That(idx1, Is.EqualTo(idx2),
                    $"Owner index diverged at draw #{i}");
            }
        }

        // ── PurgeOwner (§3.3.5 / §4.8.4) ─────────────────────────────────────

        [Test]
        public void PurgeOwner_RemovesFromBothDeckAndDiscard()
        {
            SkillDeck deck = new();
            PokemonInstance fainted = MakePokemon(4);
            PokemonInstance ally = MakePokemon(4);
            deck.Build(new List<PokemonInstance> { fainted, ally });
            // Move two of the fainted Pokémon's cards into the discard pile
            // by draw-then-discard.
            GameRNG rng = new(seed: 999);
            // Drain everything, then route fainted's cards to discard, ally's cards stay in hand.
            List<MoveCardInstance> hand = new();
            while (deck.DeckCount > 0) hand.Add(deck.Draw(rng));
            // Send half back to discard:
            for (int i = 0; i < hand.Count; i += 2) deck.Discard(hand[i]);

            int totalBefore = deck.DeckCount + deck.DiscardCount;
            int faintedBefore = 0;
            for (int i = 0; i < deck.DeckView.Count; i++)
                if (ReferenceEquals(deck.DeckView[i].Owner, fainted)) faintedBefore++;
            for (int i = 0; i < deck.DiscardView.Count; i++)
                if (ReferenceEquals(deck.DiscardView[i].Owner, fainted)) faintedBefore++;

            int removed = deck.PurgeOwner(fainted);
            Assert.That(removed, Is.EqualTo(faintedBefore));
            Assert.That(deck.DeckCount + deck.DiscardCount,
                        Is.EqualTo(totalBefore - faintedBefore));
            // Every remaining card belongs to ally.
            for (int i = 0; i < deck.DeckView.Count; i++)
                Assert.That(deck.DeckView[i].Owner, Is.SameAs(ally));
            for (int i = 0; i < deck.DiscardView.Count; i++)
                Assert.That(deck.DiscardView[i].Owner, Is.SameAs(ally));
        }

        [Test]
        public void PurgeOwner_NullFainted_ReturnsZero()
        {
            SkillDeck deck = new();
            deck.Build(new List<PokemonInstance> { MakePokemon(4) });
            Assert.That(deck.PurgeOwner(null), Is.EqualTo(0));
            Assert.That(deck.DeckCount, Is.EqualTo(4));
        }

        // ── Clear lifecycle ──────────────────────────────────────────────────

        [Test]
        public void Clear_EmptiesBothLists()
        {
            SkillDeck deck = new();
            deck.Build(new List<PokemonInstance> { MakePokemon(4) });
            GameRNG rng = new(seed: 42);
            MoveCardInstance c = deck.Draw(rng);
            deck.Discard(c);
            Assert.That(deck.DeckCount + deck.DiscardCount, Is.GreaterThan(0));
            deck.Clear();
            Assert.That(deck.DeckCount, Is.EqualTo(0));
            Assert.That(deck.DiscardCount, Is.EqualTo(0));
        }
    }
}
