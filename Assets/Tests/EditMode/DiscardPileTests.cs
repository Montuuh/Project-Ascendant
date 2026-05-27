using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.4 + §3.5 + Epic 5 Task 5.3 — discard pile + reshuffle + consumable
    // combat-end restoration coverage. The DiscardPile is encapsulated by
    // SkillDeck (no separate class — see SkillDeck.DiscardView / Discard() /
    // ReshuffleDiscardIntoDeck() / PurgeOwner()) and by ConsumablePile for
    // consumables (UsedThisCombat / RestoreAll()).
    //
    // 5.1 already covers the basic happy paths in SkillDeckTests +
    // ConsumablePileTests. This file adds:
    //   • 5.3.2 Reshuffle edge cases (mid-draw, repeated reshuffles, post-purge).
    //   • 5.3.3 Combat-end restoration round-trip through CombatController.
    //   • 5.3.4 Discard cycle determinism (same seed → same reshuffle order).
    public class DiscardPileTests
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
            _config.BaseSkillCardsPerTurn = 2;
            _config.BaseConsumableCardsPerTurn = 1;
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

        private MoveSO Mk(string id)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = id;
            m.Type = PokemonType.Normal;
            m.BasePower = 40;
            m.APCost = 1;
            m.Role = MoveRole.Offensive;
            m.Range = MoveRange.Ranged;
            _disposables.Add(m);
            return m;
        }

        private ConsumableSO Mc(string id)
        {
            ConsumableSO c = ScriptableObject.CreateInstance<ConsumableSO>();
            c.name = id;
            _disposables.Add(c);
            return c;
        }

        private PokemonInstance MakeMon(params MoveSO[] moves)
        {
            PokemonInstance p = new() { Species = _species, Level = 1, CurrentHP = 60 };
            for (int i = 0; i < moves.Length; i++) p.CurrentMoves.Add(moves[i]);
            return p;
        }

        // ── 5.3.2 — Reshuffle edge cases ─────────────────────────────────────

        [Test]
        public void Reshuffle_RepeatedCycles_PreservesTotalCardCount()
        {
            // Per §3.4 — discard refills the deck when empty. Across many
            // draw-play-reshuffle cycles, the total card count in flight
            // (deck + discard + hand) is conserved until faint purge.
            SkillDeck deck = new();
            PokemonInstance p = MakeMon(Mk("a"), Mk("b"), Mk("c"), Mk("d"));
            deck.Build(new List<PokemonInstance> { p });
            int total = deck.DeckCount;  // 4
            GameRNG rng = new(seed: 0xCC);

            List<MoveCardInstance> hand = new();
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Draw entire deck into hand, discard everything, reshuffle.
                while (deck.DeckCount > 0) hand.Add(deck.Draw(rng));
                for (int i = 0; i < hand.Count; i++) deck.Discard(hand[i]);
                hand.Clear();
                Assert.That(deck.DeckCount + deck.DiscardCount, Is.EqualTo(total),
                    $"Card count not conserved after cycle {cycle}.");
            }
        }

        [Test]
        public void Reshuffle_PostPurge_ExcludesOwnerCards()
        {
            // Fainted Pokémon's cards are removed from BOTH deck and discard
            // before reshuffle (§3.3.5 / §4.8.4). Subsequent draws never see
            // them — even after multiple reshuffle cycles.
            SkillDeck deck = new();
            PokemonInstance p1 = MakeMon(Mk("p1a"), Mk("p1b"));
            PokemonInstance p2 = MakeMon(Mk("p2a"), Mk("p2b"));
            deck.Build(new List<PokemonInstance> { p1, p2 });
            GameRNG rng = new(seed: 0xDD);

            // Drain to discard then purge p1.
            List<MoveCardInstance> hand = new();
            while (deck.DeckCount > 0) hand.Add(deck.Draw(rng));
            for (int i = 0; i < hand.Count; i++) deck.Discard(hand[i]);
            deck.PurgeOwner(p1);
            // Now deck is empty; only p2's 2 cards remain in discard.
            for (int draws = 0; draws < 5; draws++)
            {
                MoveCardInstance c = deck.Draw(rng);
                if (c == null) break;
                Assert.That(c.Owner, Is.SameAs(p2),
                    "Purged owner's cards must never resurface (§4.8.4).");
                deck.Discard(c);
            }
        }

        [Test]
        public void Reshuffle_DeterministicAcrossSeededRuns()
        {
            // Per §9.7 + Task 5.3.4 — identical seeds produce identical
            // reshuffle-and-redraw sequences.
            SkillDeck d1 = new();
            SkillDeck d2 = new();
            PokemonInstance p1 = MakeMon(Mk("a"), Mk("b"), Mk("c"), Mk("d"));
            PokemonInstance p2 = MakeMon(Mk("a"), Mk("b"), Mk("c"), Mk("d"));
            d1.Build(new List<PokemonInstance> { p1 });
            d2.Build(new List<PokemonInstance> { p2 });
            GameRNG r1 = new(seed: 0xFEED);
            GameRNG r2 = new(seed: 0xFEED);
            // Drain + discard + drain (triggers reshuffle) — compare each draw.
            List<MoveCardInstance> buf1 = new();
            List<MoveCardInstance> buf2 = new();
            for (int i = 0; i < 4; i++) buf1.Add(d1.Draw(r1));
            for (int i = 0; i < 4; i++) buf2.Add(d2.Draw(r2));
            for (int i = 0; i < buf1.Count; i++) d1.Discard(buf1[i]);
            for (int i = 0; i < buf2.Count; i++) d2.Discard(buf2[i]);
            for (int i = 0; i < 4; i++)
            {
                MoveCardInstance c1 = d1.Draw(r1);
                MoveCardInstance c2 = d2.Draw(r2);
                Assert.That(c1.Move.name, Is.EqualTo(c2.Move.name),
                    $"Reshuffle draw #{i} diverged across identical seeds.");
            }
        }

        [Test]
        public void Reshuffle_EmptyDiscard_ReturnsNull_NoThrow()
        {
            SkillDeck deck = new();
            PokemonInstance p = MakeMon(Mk("a"));
            deck.Build(new List<PokemonInstance> { p });
            GameRNG rng = new(seed: 1);
            // Drain the one card, never discard it.
            MoveCardInstance c = deck.Draw(rng);
            Assert.That(c, Is.Not.Null);
            // Deck is empty; discard is empty (card sits in caller's hand).
            // Next draw must return null without throwing.
            Assert.That(deck.Draw(rng), Is.Null);
            // Discard the card; next draw reshuffles + returns it.
            deck.Discard(c);
            Assert.That(deck.Draw(rng), Is.SameAs(c));
        }

        // ── 5.3.3 — Consumable combat-end restoration round-trip ─────────────

        private sealed class PlayConsumableAgent : IPlayerAgent
        {
            public bool Played;
            public PlayerAction DecideAction(CombatController.CombatState s)
            {
                if (!Played && s.ConsumableHand.Count > 0)
                {
                    Played = true;
                    return new PlayerAction
                    {
                        Kind = PlayerActionKind.PlayConsumable,
                        CardIndex = 0,
                    };
                }
                return PlayerAction.End();
            }
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => s.LeadIndex;
        }

        [Test]
        public void Consumable_PlayedDuringCombat_RestoredAtCombatEnd()
        {
            // Per §3.5 — "At combat end, all consumables are automatically
            //   restored to the player's persistent inventory."
            ConsumableSO potion = Mc("potion");
            MoveSO m = Mk("strike");
            PokemonInstance lead = MakeMon(m);
            PokemonInstance enemy = MakeMon(m);
            enemy.CurrentHP = 1;  // dies on first strike → quick Victory

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO> { potion },
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0x111),
            };
            PlayConsumableAgent agent = new();
            CombatController c = new(setup, agent);
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            // Play the consumable, then end turn / resolve.
            c.ActionPhase();
            Assert.That(c.State.Consumables.IsUsed(potion), Is.True,
                "Consumable must be marked used after play.");
            c.ResolutionPhase();
            c.TurnEnd();
            // After CombatEnd (triggered manually here), RestoreAll clears
            // the used list so the consumable is available again outside combat.
            c.CombatEnd();
            Assert.That(c.State.Consumables.IsUsed(potion), Is.False,
                "RestoreAll at combat end must un-mark used consumables (§3.5).");
            Assert.That(c.State.ConsumableInventory, Has.Member(potion),
                "Consumable must still be present in the inventory.");
        }

        [Test]
        public void Consumable_DrawnNotPlayed_StaysInInventory()
        {
            // Drawing a consumable into the hand without playing it does NOT
            // mark it as used. After CombatEnd it's still available.
            ConsumableSO potion = Mc("potion");
            MoveSO m = Mk("strike");
            PokemonInstance lead = MakeMon(m);
            PokemonInstance enemy = MakeMon(m);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO> { potion },
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(seed: 0x222),
            };
            CombatController c = new(setup, new EndAgent());
            c.Start();
            c.DrawPhase();
            Assert.That(c.State.ConsumableHand, Has.Member(potion),
                "Drawn consumable must appear in hand.");
            Assert.That(c.State.Consumables.IsUsed(potion), Is.False,
                "Drawing is non-consuming — IsUsed stays false.");
            c.IntentPhase(); c.ResolutionPhase(); c.TurnEnd();
            // Combat continues but next DrawPhase still sees the consumable.
            c.DrawPhase();
            Assert.That(c.State.ConsumableHand, Has.Member(potion));
        }

        private sealed class EndAgent : IPlayerAgent
        {
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> candidates) => s.LeadIndex;
        }
    }
}
