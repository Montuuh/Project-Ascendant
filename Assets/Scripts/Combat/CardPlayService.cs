using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Combat
{
    // Per §3.3 + Epic 5 Task 5.4 — runtime service that owns the
    // "play a skill card from hand" pipeline. Composes:
    //   • CardPlayValidator       — eligibility (Melee/Range/SF, owner alive)
    //   • StatusModifiers         — Sleep/Freeze block, Paralysis +1 AP
    //   • CardPlayValidator       — defensive-swap discount apply + consume
    //   • PositionalModifier      — Step-Forward / Step-Backward side effects
    //   • Damage resolution       — injected callback (the controller owns it)
    //
    // What this class does NOT own:
    //   • Combat phase orchestration (CombatController)
    //   • Card object lifecycle outside the play moment (SkillDeck)
    //   • Damage formula itself (DamageCalculator)
    //   • Faint sweep / outcome check (CombatController.HandleAnyFaints
    //     fires inside the damage callback)
    //
    // SF/SB position changes are §3.3.2 / §3.3.3 — neither increments
    // SwapCounter nor grants the defensive-swap discount.
    public sealed class CardPlayService
    {
        private readonly CombatController.CombatState _state;
        private readonly IPlayerAgent _agent;
        private readonly MoveCardInstanceFactory _factory;
        private readonly Action<PokemonInstance, PokemonInstance, MoveSO> _resolveDamage;

        public CardPlayService(CombatController.CombatState state,
                               IPlayerAgent agent,
                               MoveCardInstanceFactory factory,
                               Action<PokemonInstance, PokemonInstance, MoveSO> resolveDamage)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _agent = agent;
            _factory = factory ?? new MoveCardInstanceFactory();
            _resolveDamage = resolveDamage
                ?? throw new ArgumentNullException(nameof(resolveDamage));
        }

        // Per Epic 4 Task 4.1.5 contract — return true to keep the action loop
        // running, false to break out (EndTurn / unrecoverable invalid input).
        // Invalid plays (out-of-range index, validator-rejected card, AP-short)
        // are non-fatal: return true so the player can try a different card.
        public bool Play(int handIndex, int targetEnemySlot)
        {
            if (handIndex < 0 || handIndex >= _state.SkillHand.Count) return true;
            MoveCardInstance card = _state.SkillHand[handIndex];
            if (card == null) return true;
            MoveSO move = card.Move;
            if (move == null) return true;

            // Per §3.3 + §3.6 + Task 5.6.1 — taxonomy-driven play eligibility.
            CardPlayValidator.PlayResult vr = CardPlayValidator.Validate(
                card, _state.PlayerTeam, _state.LeadIndex);
            if (vr != CardPlayValidator.PlayResult.Playable) return true;

            // Owner is the attacker. For SF cards, owner may be on the bench
            // pre-play; the SF block below promotes them to Lead BEFORE damage
            // resolves, so the attacker is correctly Lead at strike time.
            PokemonInstance attacker = card.Owner;

            // Per §4.2.2.4/5 — Sleep / Freeze block playing. Status applies
            // to the OWNER (Sleep on bench Pokémon still blocks their cards).
            if (!StatusModifiers.AreCardsPlayable(attacker.PrimaryStatus)) return true;

            // Per §3.3.1 + Task 5.6.2 — defensive discount applies on top of
            // status modifiers (so Paralyzed Defensive after swap = base+1−1
            // = base AP cost). Discount consumed iff move is Defensive AND
            // available (split apply / consume so UI preview never lies).
            int apCost = StatusModifiers.GetEffectiveAPCost(move, attacker, _state.Config);
            apCost = CardPlayValidator.ApplyDefensiveDiscount(
                apCost, move, _state.DefensiveSwapDiscountAvailable);
            if (apCost > _state.CurrentAP) return true;

            bool consumeDiscount = CardPlayValidator.ShouldConsumeDefensiveDiscount(
                move, _state.DefensiveSwapDiscountAvailable);

            // ── State mutations from here on — commit point ─────────────────
            _state.CurrentAP -= apCost;
            if (consumeDiscount) _state.DefensiveSwapDiscountAvailable = false;
            _state.SkillHand.RemoveAt(handIndex);
            _state.Deck.Discard(card);

            // Per §3.3.2 — Step-Forward promotes the bench owner to Lead
            // BEFORE the effect resolves. Does not increment SwapCounter and
            // does not grant the defensive-swap discount.
            if (move.Modifier == PositionalModifier.StepForward)
                ApplyStepForward(card.Owner);

            // Per §3.2.4 — "Card effects resolve immediately when played."
            // Damage callback handles HP delta + faint sweep + outcome flip.
            PokemonInstance target = ResolveEnemySlot(targetEnemySlot);
            if (target != null) _resolveDamage(attacker, target, move);

            // Per §3.2.4 — "If a card play kills an enemy or fulfills a
            // Victory/Defeat condition, combat ends at that moment." If the
            // damage callback flipped Outcome, skip SB (the combat is over).
            if (_state.Outcome != CombatController.CombatOutcome.InProgress) return true;

            // Per §3.3.3 — Step-Backward: effect resolves, THEN Lead swaps
            // with a player-chosen bench Pokémon (non-fainted, non-frozen).
            // If no eligible bench exists, the effect still resolves and
            // Lead remains Lead. No SwapCounter increment, no discount.
            if (move.Modifier == PositionalModifier.StepBackward)
                ApplyStepBackward();

            return true;
        }

        // ── Position-change side effects ────────────────────────────────────

        private void ApplyStepForward(PokemonInstance owner)
        {
            if (owner == null) return;
            int idx = IndexOf(_state.PlayerTeam, owner);
            if (idx < 0) return;
            if (idx == _state.LeadIndex) return; // already lead — no-op
            _state.LeadIndex = idx;
            AbilityResolver.ApplyLeadEntryEffects(_state); // §5.5.3.5 Intimidate
        }

        private void ApplyStepBackward()
        {
            List<PokemonInstance> candidates = EligibleStepBackwardTargets(
                _state.PlayerTeam, _state.LeadIndex);
            if (candidates.Count == 0 || _agent == null) return;
            int chosen = _agent.PickLeadReplacement(_state, candidates);
            // Sanity-check the agent's response so a malformed agent can't
            // place a fainted / frozen Pokémon into Lead via SB.
            if (chosen < 0 || chosen >= _state.PlayerTeam.Count) return;
            if (chosen == _state.LeadIndex) return;
            PokemonInstance target = _state.PlayerTeam[chosen];
            if (target == null) return;
            if (target.CurrentHP == 0) return;
            if (target.PrimaryStatus == StatusCondition.Freeze) return;
            _state.LeadIndex = chosen;
            AbilityResolver.ApplyLeadEntryEffects(_state); // §5.5.3.5 Intimidate
        }

        // Per §3.3.3 — Step-Backward eligibility differs from Lead-faint
        // replacement (§3.3.5): Frozen bench Pokémon are excluded for SB
        // (manual swap is blocked by Freeze position-lock — SB is effectively
        // a manual swap rider on top of a card play). Fainted excluded too.
        public static List<PokemonInstance> EligibleStepBackwardTargets(
            IReadOnlyList<PokemonInstance> activeTeam, int currentLeadIndex)
        {
            List<PokemonInstance> result = new();
            if (activeTeam == null) return result;
            for (int i = 0; i < activeTeam.Count; i++)
            {
                if (i == currentLeadIndex) continue;
                PokemonInstance p = activeTeam[i];
                if (p == null) continue;
                if (p.CurrentHP == 0) continue;
                if (p.PrimaryStatus == StatusCondition.Freeze) continue;
                result.Add(p);
            }
            return result;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private PokemonInstance ResolveEnemySlot(int slot)
        {
            if (_state.EnemyTeam == null) return null;
            if (slot < 0 || slot >= _state.EnemyTeam.Count) return null;
            return _state.EnemyTeam[slot];
        }

        private static int IndexOf(IReadOnlyList<PokemonInstance> team, PokemonInstance p)
        {
            if (team == null) return -1;
            for (int i = 0; i < team.Count; i++)
                if (ReferenceEquals(team[i], p)) return i;
            return -1;
        }
    }
}
