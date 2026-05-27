using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.3.5 + §4.8.4 + Epic 5 Task 5.5 — end-to-end faint purge through
    // CombatController:
    //   • 5.5.1 OnFaint handler purges 4+1 cards from Deck + Discard.
    //   • 5.5.2 Hand-state handling — fainted-owner cards stay visible in
    //           hand ("greyed out, not hidden") and CardPlayValidator rejects
    //           their play. TurnEnd drops them (does NOT route to discard,
    //           so they cannot resurface via reshuffle).
    //
    // SkillDeck-level purge primitives are unit-tested in SkillDeckTests
    // (5.1). This file exercises the controller-level orchestration.
    public class FaintCardPurgeTests
    {
        private readonly List<Object> _disposables = new();
        private PokemonSpeciesSO _species;
        private BattleConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            _species.Types = new List<PokemonType> { PokemonType.Normal };
            _species.BaseStats = new BaseStats { BaseHP = 60, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            _species.GrowthCurve = null;
            _species.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(_species);

            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.Divisor = 10;
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 8;  // draw enough to see fainted's cards in hand
            _config.BaseConsumableCardsPerTurn = 0;
            _config.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        private MoveSO Mk(string id, int power = 40)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = id;
            m.Type = PokemonType.Normal;
            m.BasePower = power;
            m.APCost = 1;
            m.Role = MoveRole.Offensive;
            m.Range = MoveRange.Ranged;
            m.Modifier = PositionalModifier.None;
            _disposables.Add(m);
            return m;
        }

        private PokemonInstance MakeMon(int hp, MoveSO mastery, params MoveSO[] moves)
        {
            PokemonInstance p = new() { Species = _species, Level = 1, CurrentHP = hp };
            for (int i = 0; i < moves.Length; i++) p.CurrentMoves.Add(moves[i]);
            p.MasteryMove = mastery;
            return p;
        }

        private sealed class EndAgent : IPlayerAgent
        {
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates)
                => candidates.Count > 0 ? IndexOf(s.PlayerTeam, candidates[0]) : s.LeadIndex;
            private static int IndexOf(IList<PokemonInstance> team, PokemonInstance p)
            {
                for (int i = 0; i < team.Count; i++)
                    if (ReferenceEquals(team[i], p)) return i;
                return -1;
            }
        }

        private CombatController Build(List<PokemonInstance> team, PokemonInstance enemy)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = team,
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0xFA1),
            };
            CombatController c = new(setup, new EndAgent());
            c.Start();
            return c;
        }

        private static int CountOwnerCards(IReadOnlyList<MoveCardInstance> cards, PokemonInstance owner)
        {
            int n = 0;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null && ReferenceEquals(cards[i].Owner, owner)) n++;
            return n;
        }

        // ── 5.5.1: Deck + Discard purge — 4+1 (mastered) cards removed ──────

        [Test]
        public void Faint_RemovesFourPlusOneMastery_FromDeck()
        {
            // Build deck: bench has 4 base moves + 1 Mastery (5 cards).
            MoveSO m = Mk("m");
            MoveSO mastery = Mk("mast");
            PokemonInstance lead = MakeMon(60, null, m, m, m, m);
            PokemonInstance bench = MakeMon(60, mastery, m, m, m, m);
            PokemonInstance enemy = MakeMon(60, null, m);
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy);

            Assert.That(c.State.Deck.DeckCount, Is.EqualTo(4 + 4 + 1),
                "Setup sanity: 4 lead + 4 bench + 1 mastery = 9.");
            int benchCardsBefore = CountOwnerCards(c.State.Deck.DeckView, bench);
            Assert.That(benchCardsBefore, Is.EqualTo(5), "4 + 1 mastery for bench.");

            // Faint the bench Pokémon. Trigger the controller's faint pathway by
            // running a turn (DoT or enemy intent isn't required — just lower
            // HP to 0 and run a phase that calls HandleAnyFaints).
            bench.CurrentHP = 0;
            c.DrawPhase();
            c.IntentPhase();
            c.ResolutionPhase();  // triggers faint sweep + purge

            int benchCardsAfter = CountOwnerCards(c.State.Deck.DeckView, bench)
                                + CountOwnerCards(c.State.Deck.DiscardView, bench);
            Assert.That(benchCardsAfter, Is.EqualTo(0),
                "All 5 of the fainted Pokémon's cards must be purged from deck + discard.");
        }

        // ── 5.5.2: Hand-state — visible but unplayable through the turn ──────

        [Test]
        public void Faint_HandCards_StayVisible_NotPlayable_RemovedAtTurnEnd()
        {
            // Lead + bench both have moves; draw fills hand with cards from
            // both owners. Faint the bench during resolution → assert their
            // cards remain in hand until TurnEnd.
            MoveSO m = Mk("m");
            PokemonInstance lead = MakeMon(60, null, m, m, m, m);
            PokemonInstance bench = MakeMon(60, null, m, m, m, m);
            PokemonInstance enemy = MakeMon(60, null, m);
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy);
            c.DrawPhase();
            c.IntentPhase();
            int benchInHandBefore = CountOwnerCards(c.State.SkillHand, bench);
            Assume.That(benchInHandBefore, Is.GreaterThan(0),
                "Test invariant: hand must contain at least one of bench's cards.");

            // Faint bench mid-resolution: lower HP first, then run resolution.
            bench.CurrentHP = 0;
            c.ResolutionPhase();

            int benchInHandAfter = CountOwnerCards(c.State.SkillHand, bench);
            Assert.That(benchInHandAfter, Is.EqualTo(benchInHandBefore),
                "Fainted-owner cards must stay in hand until TurnEnd (5.5.2 — greyed, not hidden).");

            // Validator rejects play of any fainted-owner card.
            int benchIdx = -1;
            for (int i = 0; i < c.State.SkillHand.Count; i++)
            {
                if (ReferenceEquals(c.State.SkillHand[i].Owner, bench))
                { benchIdx = i; break; }
            }
            Assume.That(benchIdx, Is.GreaterThanOrEqualTo(0));
            CardPlayValidator.PlayResult vr = CardPlayValidator.Validate(
                c.State.SkillHand[benchIdx], c.State.PlayerTeam, c.State.LeadIndex);
            Assert.That(vr, Is.EqualTo(CardPlayValidator.PlayResult.OwnerFainted),
                "Validator must mark fainted-owner cards as unplayable.");

            // TurnEnd: fainted-owner cards must NOT enter the discard pile
            // (otherwise reshuffle would resurface them).
            int discardBefore = c.State.Deck.DiscardCount;
            c.TurnEnd();
            int discardAfter = c.State.Deck.DiscardCount;
            // The hand is empty after TurnEnd, but the discard delta excludes
            // bench's cards. (Lead's cards do go to discard.)
            int leadInHandBefore = CountOwnerCards(c.State.SkillHand, lead);
            Assert.That(leadInHandBefore, Is.EqualTo(0), "Hand cleared.");
            Assert.That(CountOwnerCards(c.State.Deck.DiscardView, bench), Is.EqualTo(0),
                "Fainted-owner cards must not enter the discard pile (5.5.2).");
        }

        [Test]
        public void Faint_LeadFaint_ReplacementChosen_HandCardsForFaintedLeadStay()
        {
            // Lead faints from a lethal enemy intent → bench promoted via
            // PickLeadReplacement. Lead's cards stay in hand until TurnEnd.
            MoveSO m = Mk("m");
            PokemonInstance lead = MakeMon(1, null, m, m, m, m);
            PokemonInstance bench = MakeMon(60, null, m, m, m, m);
            // Enemy with a high-power move that one-shots the lead.
            PokemonInstance enemy = MakeMon(60, null, Mk("big", power: 200));
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy);
            c.DrawPhase();
            c.IntentPhase();

            int leadHandBefore = CountOwnerCards(c.State.SkillHand, lead);
            Assume.That(leadHandBefore, Is.GreaterThan(0));

            c.ResolutionPhase();

            Assert.That(lead.CurrentHP, Is.EqualTo(0), "Lead must have fainted.");
            Assert.That(c.State.LeadIndex, Is.EqualTo(1),
                "PickLeadReplacement must promote bench to Lead.");
            int leadHandAfter = CountOwnerCards(c.State.SkillHand, lead);
            Assert.That(leadHandAfter, Is.EqualTo(leadHandBefore),
                "Fainted Lead's hand cards stay visible (5.5.2).");
            Assert.That(CountOwnerCards(c.State.Deck.DeckView, lead)
                      + CountOwnerCards(c.State.Deck.DiscardView, lead), Is.EqualTo(0),
                "Fainted Lead's cards purged from deck + discard.");
        }

        [Test]
        public void TurnEnd_AliveCardsGoToDiscard_FaintedCardsDropped()
        {
            MoveSO m = Mk("m");
            PokemonInstance lead = MakeMon(60, null, m, m);
            PokemonInstance bench = MakeMon(60, null, m, m);
            PokemonInstance enemy = MakeMon(60, null, m);
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy);
            c.DrawPhase();
            c.IntentPhase();
            int leadInHand = CountOwnerCards(c.State.SkillHand, lead);
            int benchInHand = CountOwnerCards(c.State.SkillHand, bench);
            bench.CurrentHP = 0;
            c.ResolutionPhase();
            // Discard the alive lead's hand cards; drop bench's.
            c.TurnEnd();
            Assert.That(CountOwnerCards(c.State.Deck.DiscardView, lead),
                Is.EqualTo(leadInHand), "Alive owner's hand → discard.");
            Assert.That(CountOwnerCards(c.State.Deck.DiscardView, bench), Is.EqualTo(0),
                "Fainted owner's hand → dropped, NOT discard.");
        }

        [Test]
        public void Reshuffle_AfterFaint_NeverResurfacesFaintedCards()
        {
            // Drain deck completely over a couple of turns; ensure fainted
            // Pokémon's cards never appear in subsequent draws.
            MoveSO m = Mk("m");
            PokemonInstance lead = MakeMon(60, null, m, m, m, m);
            PokemonInstance bench = MakeMon(60, null, m, m, m, m);
            PokemonInstance enemy = MakeMon(60, null, m);
            CombatController c = Build(new List<PokemonInstance> { lead, bench }, enemy);
            bench.CurrentHP = 0;  // bench starts already fainted
            c.DrawPhase();
            c.IntentPhase();
            c.ResolutionPhase();
            // bench cards purged. Run several DrawPhase / TurnEnd cycles.
            for (int t = 0; t < 5; t++)
            {
                c.TurnEnd();
                c.DrawPhase();
                Assert.That(CountOwnerCards(c.State.SkillHand, bench), Is.EqualTo(0),
                    $"Turn {t}: fainted bench cards must never reappear in hand.");
                c.IntentPhase();
                c.ResolutionPhase();
            }
        }
    }
}
