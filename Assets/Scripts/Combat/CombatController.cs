using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Combat
{
    // Per §3.2 + Epic 4 Task 4.1 — orchestrator for the five combat phases:
    //   Start → (Draw → Intent → Action → Resolution → TurnEnd)* → CombatEnd
    //
    // The HSM (Epic 2 Task 2.3) drives state transitions in the real game.
    // This class is engine-agnostic — pure C# — so it can be unit-tested
    // headlessly without a Unity scene.
    //
    // What this class owns:
    //   • CombatState (mutable runtime state)
    //   • Phase methods (callable in sequence; idempotent re-entry guards)
    //   • Composition over every Epic 4 subsystem:
    //       DamageCalculator, CritResolver, StatusEffectManager,
    //       StatusModifiers, StatStageManager, CombatStatResolver,
    //       TypeChart, IntentScorer, IntentTargeting, FaintResolver,
    //       FieldEffectResolver.
    //
    // What this class does NOT own (deferred):
    //   • UI / animation timing — production wiring fires events into UI.
    //   • Swap-counter AP scaling (§3.3.1) — Epic 6 task.
    //   • Consumable inventory restoration semantics — Epic 12 task; this
    //     class tracks Used-this-combat and clears it on combat-end so the
    //     §3.5 invariant ("not expendable") is preserved.
    //   • Boss phase transitions / Champion buff cap — Epic 8 task.
    //   • XP award on CombatEnd — Epic 10 task; stub emits OnCombatEnded.
    public class CombatController
    {
        // ── Public sub-types ─────────────────────────────────────────────────

        public enum CombatOutcome { InProgress, Victory, Defeat }

        // Per §3.2 — the five phase markers. Exposed so tests + UI can query
        // / assert the controller's current position in the turn structure.
        public enum Phase
        {
            PreStart,
            Start,
            DrawPhase,
            IntentPhase,
            ActionPhase,
            ResolutionPhase,
            TurnEnd,
            CombatEnd
        }

        // Initial inputs — once Start() runs, this is captured into State.
        public struct CombatSetup
        {
            public List<PokemonInstance> PlayerTeam;
            public int InitialLeadIndex;
            public List<PokemonInstance> EnemyTeam;
            public List<ConsumableSO> ConsumableInventory;
            public FieldState InitialField;
            public BattleConfigSO Config;
            public GameRNG Rng;
        }

        // Runtime state — fully describes the encounter. Public mutable for
        // ease of inspection from tests / agents; production should treat as
        // read-only outside this class.
        public class CombatState
        {
            public List<PokemonInstance> PlayerTeam;
            public int LeadIndex;
            public List<PokemonInstance> EnemyTeam;

            // Per Epic 5 Task 5.1.1 — proper SkillDeck class owns deck+discard.
            // Hand stays a flat List<MoveCardInstance> (controller-owned;
            // promoted to a Hand class in Task 5.2).
            public SkillDeck Deck;
            public List<MoveCardInstance> SkillHand = new();
            // Snapshot read accessors so tests / UI can inspect deck + discard
            // without knowing the SkillDeck class. Forwarded to Deck.
            public IReadOnlyList<MoveCardInstance> SkillDeckView =>
                Deck != null ? Deck.DeckView : System.Array.Empty<MoveCardInstance>();
            public IReadOnlyList<MoveCardInstance> DiscardView =>
                Deck != null ? Deck.DiscardView : System.Array.Empty<MoveCardInstance>();

            // Per Epic 5 Task 5.1.2 — ConsumablePile owns inventory ref + UsedThisCombat.
            public List<ConsumableSO> ConsumableInventory = new();  // persistent
            public List<ConsumableSO> ConsumableHand = new();       // drawn this turn
            public ConsumablePile Consumables;

            public int CurrentAP;
            public int SwapCounter;                       // per-turn (§3.3.1)
            public int TurnNumber;
            public FieldState Field;
            public List<Intent> EnemyIntents = new();
            public CombatOutcome Outcome = CombatOutcome.InProgress;
            public Phase CurrentPhase = Phase.PreStart;
            public BattleConfigSO Config;
            public GameRNG Rng;
        }

        // ── State ────────────────────────────────────────────────────────────

        public CombatState State { get; }
        private readonly IPlayerAgent _agent;
        private readonly MoveCardInstanceFactory _cardFactory;

        // ── Constructor + lifecycle ──────────────────────────────────────────

        public CombatController(in CombatSetup setup, IPlayerAgent agent)
            : this(setup, agent, cardFactory: null) { }

        // Per Epic 5 Task 5.1 — optional factory injection so tests / future
        // pool tuning can swap the allocation strategy. Defaults to a fresh
        // factory per controller (matches existing single-arg behaviour).
        public CombatController(in CombatSetup setup, IPlayerAgent agent,
                                MoveCardInstanceFactory cardFactory)
        {
            _agent = agent;
            _cardFactory = cardFactory ?? new MoveCardInstanceFactory();
            State = new CombatState
            {
                PlayerTeam = setup.PlayerTeam ?? new List<PokemonInstance>(),
                LeadIndex = setup.InitialLeadIndex,
                EnemyTeam = setup.EnemyTeam ?? new List<PokemonInstance>(),
                ConsumableInventory = setup.ConsumableInventory != null
                    ? new List<ConsumableSO>(setup.ConsumableInventory)
                    : new List<ConsumableSO>(),
                Field = setup.InitialField,
                Config = setup.Config,
                Rng = setup.Rng,
                Deck = new SkillDeck(_cardFactory),
                Consumables = new ConsumablePile(),
            };
            State.Consumables.Build(State.ConsumableInventory);
        }

        // Per Epic 4 Task 4.1.2 — initial encounter setup.
        public void Start()
        {
            State.CurrentPhase = Phase.Start;
            // Per Epic 5 Task 5.1.1 — SkillDeck.Build clears + repopulates
            // and ConsumablePile.Build resets the per-combat used-list.
            State.Deck.Build(State.PlayerTeam);
            State.Consumables.Build(State.ConsumableInventory);
            State.TurnNumber = 0;
        }

        // Per §3.2 + Task 4.1.7 — composition of all phases until outcome.
        // Caps at maxTurns (default 50) to prevent runaway tests on agent bugs.
        public CombatOutcome RunFullCombat(int maxTurns = 50)
        {
            if (State.CurrentPhase == Phase.PreStart) Start();

            for (int t = 0; t < maxTurns && State.Outcome == CombatOutcome.InProgress; t++)
            {
                DrawPhase();
                IntentPhase();
                ActionPhase();
                ResolutionPhase();
                TurnEnd();
                CheckOutcome();
            }
            if (State.Outcome != CombatOutcome.InProgress) CombatEnd();
            return State.Outcome;
        }

        // ── Phase 1: Draw (Task 4.1.3) ───────────────────────────────────────

        public void DrawPhase()
        {
            State.CurrentPhase = Phase.DrawPhase;
            State.TurnNumber++;
            State.CurrentAP = State.Config.BaseAPPerTurn;
            State.SwapCounter = 0;
            State.SkillHand.Clear();
            State.ConsumableHand.Clear();

            DrawSkillCards(State.Config.BaseSkillCardsPerTurn);
            DrawConsumableCards(State.Config.BaseConsumableCardsPerTurn);

            // Per §4.2.3.1 — Confusion: each Confused active Pokémon discards
            // one random skill card from the hand. Consumables immune.
            // StatusEffectManager.ResolveConfusionDiscard operates on
            // IList<MoveSO>; adapt the hand via a temp list and mirror the
            // removal back. Discarded cards go to the discard pile so deck
            // reshuffle still sees them.
            List<MoveSO> tempMoves = new(State.SkillHand.Count);
            for (int i = 0; i < State.SkillHand.Count; i++) tempMoves.Add(State.SkillHand[i].Move);
            for (int i = 0; i < State.PlayerTeam.Count; i++)
            {
                PokemonInstance p = State.PlayerTeam[i];
                if (p == null || p.CurrentHP <= 0) continue;
                if (p.SecondaryStatus != StatusCondition.Confusion) continue;
                int removedIdx = StatusEffectManager.ResolveConfusionDiscard(p, tempMoves, State.Rng);
                if (removedIdx >= 0 && removedIdx < State.SkillHand.Count)
                {
                    State.Deck.Discard(State.SkillHand[removedIdx]);
                    State.SkillHand.RemoveAt(removedIdx);
                }
            }
        }

        private void DrawSkillCards(int count)
        {
            // SkillDeck.Draw handles reshuffle inline + returns null when both
            // deck and discard are empty.
            for (int i = 0; i < count; i++)
            {
                MoveCardInstance card = State.Deck.Draw(State.Rng);
                if (card == null) break;
                State.SkillHand.Add(card);
            }
        }

        private void DrawConsumableCards(int count)
        {
            // Per §3.5 — consumables are NOT expendable. ConsumablePile.DrawHand
            // returns a fresh roll skipping anything in UsedThisCombat.
            List<ConsumableSO> drawn = State.Consumables.DrawHand(count, State.Rng);
            for (int i = 0; i < drawn.Count; i++) State.ConsumableHand.Add(drawn[i]);
        }

        // ── Phase 2: Intent (Task 4.1.4) ─────────────────────────────────────

        public void IntentPhase()
        {
            State.CurrentPhase = Phase.IntentPhase;
            State.EnemyIntents.Clear();
            for (int i = 0; i < State.EnemyTeam.Count; i++)
            {
                PokemonInstance enemy = State.EnemyTeam[i];
                if (enemy == null || enemy.CurrentHP <= 0)
                {
                    State.EnemyIntents.Add(new Intent { Kind = IntentKind.Unknown });
                    continue;
                }
                State.EnemyIntents.Add(BuildIntentForEnemy(enemy));
            }
        }

        // Builds the candidate intents this enemy could declare this turn,
        // then picks one via §4.3.3 scoring. VS scope: each move becomes a
        // single-target Attack on the player's Lead. (Cleave/Backstrike/Buff
        // declaration requires move-level metadata not yet authored on
        // MoveSO; the Intent struct supports those kinds when content lands.)
        private Intent BuildIntentForEnemy(PokemonInstance enemy)
        {
            List<Intent> candidates = new();
            for (int i = 0; i < enemy.CurrentMoves.Count; i++)
            {
                MoveSO m = enemy.CurrentMoves[i];
                if (m == null) continue;
                candidates.Add(new Intent
                {
                    Kind = IntentKind.Attack,
                    Move = m,
                    TargetSlot = State.LeadIndex,
                    Reveal = IntentReveal.Witnessed,
                });
            }
            if (enemy.MasteryMove != null)
            {
                candidates.Add(new Intent
                {
                    Kind = IntentKind.Attack,
                    Move = enemy.MasteryMove,
                    TargetSlot = State.LeadIndex,
                    Reveal = IntentReveal.Witnessed,
                });
            }

            if (candidates.Count == 0)
                return new Intent { Kind = IntentKind.Stall, Reveal = IntentReveal.Witnessed };

            IntentScorer.Context ctx = new()
            {
                Attacker = enemy,
                PlayerTeam = State.PlayerTeam,
                Config = State.Config,
                BossCounterIntelActive = false, // Epic 8 will wire boss state
            };
            return IntentScorer.PickIntent(candidates, ctx, State.Rng);
        }

        // ── Phase 3: Action (Task 4.1.5) ─────────────────────────────────────

        public void ActionPhase()
        {
            State.CurrentPhase = Phase.ActionPhase;
            // Hard cap on actions per turn to prevent infinite loops on agent
            // bugs. Practical max is ~10 (AP=3 × min-cost 1 + EndTurn).
            const int MAX_ACTIONS = 32;
            for (int i = 0; i < MAX_ACTIONS; i++)
            {
                if (State.CurrentAP <= 0) return;
                PlayerAction action = _agent.DecideAction(State);
                if (!ExecutePlayerAction(action)) return; // EndTurn or invalid
            }
        }

        // Per Epic 4 Task 4.1.9 — single-action API for Play Mode / UI drivers.
        // The IMGUI sandbox + the production UI step one action per user input
        // event instead of looping inside ActionPhase. Returns true if the
        // controller is still in the player's turn after the action; false on
        // EndTurn / invalid / AP-exhausted (caller should advance to Resolution).
        public bool ExecuteAction(PlayerAction action)
        {
            // Make sure we are in (or entering) ActionPhase. Calling this in
            // any other phase is a programming error.
            if (State.CurrentPhase != Phase.ActionPhase)
                State.CurrentPhase = Phase.ActionPhase;
            bool kept = ExecutePlayerAction(action);
            if (!kept) return false;
            return State.CurrentAP > 0;
        }

        // Returns true if the action was processed and the loop should continue;
        // false to break out (EndTurn or unrecoverable invalid input).
        private bool ExecutePlayerAction(PlayerAction action)
        {
            switch (action.Kind)
            {
                case PlayerActionKind.EndTurn:
                    return false;
                case PlayerActionKind.PlaySkill:
                    return TryPlaySkillCard(action.CardIndex, action.TargetEnemySlot);
                case PlayerActionKind.PlayConsumable:
                    return TryPlayConsumable(action.CardIndex);
                case PlayerActionKind.ManualSwap:
                    return TryManualSwap(action.SwapToBenchSlot);
                default:
                    return false;
            }
        }

        private bool TryPlaySkillCard(int handIndex, int enemySlot)
        {
            if (handIndex < 0 || handIndex >= State.SkillHand.Count) return true;
            MoveCardInstance card = State.SkillHand[handIndex];
            if (card == null) return true;
            MoveSO move = card.Move;
            if (move == null) return true;

            PokemonInstance attacker = ResolveLead();
            if (attacker == null) return true;

            // Per §4.2.2.4/5 — Sleep/Freeze block playing.
            if (!StatusModifiers.AreCardsPlayable(attacker.PrimaryStatus)) return true;

            int apCost = StatusModifiers.GetEffectiveAPCost(move, attacker, State.Config);
            if (apCost > State.CurrentAP) return true;
            State.CurrentAP -= apCost;
            // Card consumed: hand → discard. Faint purge can still find it
            // (the card retains its Owner reference inside the instance).
            State.SkillHand.RemoveAt(handIndex);
            State.Deck.Discard(card);

            // Target — for the VS skeleton: skill cards target a single enemy
            // slot (or the move's chosen target). Cleave/Backstrike on the
            // PLAYER side ship with move-level metadata in Epic 7 follow-up.
            PokemonInstance target = ResolveEnemySlot(enemySlot);
            if (target != null) ResolveDamage(attacker, target, move);

            return true;
        }

        private bool TryPlayConsumable(int handIndex)
        {
            if (handIndex < 0 || handIndex >= State.ConsumableHand.Count) return true;
            ConsumableSO c = State.ConsumableHand[handIndex];
            if (c == null) return true;

            // Per §3.5 — consumable AP cost is typically 0; tracked on the SO.
            // We don't read it here yet (ConsumableSO authoring varies); when
            // wired this becomes: if (c.APCost > State.CurrentAP) return true;
            // TODO Epic 12: full ConsumableEffectSO dispatch chain.
            State.ConsumableHand.RemoveAt(handIndex);
            State.Consumables.MarkUsed(c);
            return true;
        }

        private bool TryManualSwap(int benchSlot)
        {
            // Per §3.3.1 — 1st=1AP, 2nd=2AP, 3rd=3AP. SF/SB do NOT increment.
            // Full implementation is Epic 6 Task 6.x; here we provide the
            // counter-mutation contract so tests can validate it independently.
            int cost = State.SwapCounter + 1;
            if (cost > State.CurrentAP) return true;
            // Per §3.3.5.1 — Frozen Lead cannot swap unless fainted.
            PokemonInstance lead = ResolveLead();
            if (FaintResolver.IsSlotLockedForSwap(lead)) return true;
            if (benchSlot < 0 || benchSlot >= State.PlayerTeam.Count) return true;
            if (benchSlot == State.LeadIndex) return true;
            PokemonInstance target = State.PlayerTeam[benchSlot];
            if (target == null || target.CurrentHP <= 0) return true;

            State.CurrentAP -= cost;
            State.SwapCounter += 1;
            State.LeadIndex = benchSlot;
            return true;
        }

        // ── Phase 4: Resolution (Task 4.1.6) ─────────────────────────────────

        public void ResolutionPhase()
        {
            State.CurrentPhase = Phase.ResolutionPhase;

            // Per §4.3.6 — execute enemy intents in declared order.
            // Supports-first ordering is implicit: tests / encounter setup
            // determine the EnemyTeam order, and intents resolve in that
            // order. Multi-enemy regional tuning lands in Epic 8.
            for (int i = 0; i < State.EnemyIntents.Count && i < State.EnemyTeam.Count; i++)
            {
                PokemonInstance enemy = State.EnemyTeam[i];
                if (enemy == null || enemy.CurrentHP <= 0) continue;
                ExecuteEnemyIntent(enemy, State.EnemyIntents[i]);
                if (State.Outcome != CombatOutcome.InProgress) return;
            }

            // Status DoT + duration ticks for every Pokémon on both sides.
            TickStatusForAll(State.PlayerTeam);
            TickStatusForAll(State.EnemyTeam);
            // DoT can cause faints — check.
            HandleAnyFaints();
        }

        private void ExecuteEnemyIntent(PokemonInstance enemy, Intent intent)
        {
            switch (intent.Kind)
            {
                case IntentKind.Attack:
                {
                    PokemonInstance target = IntentTargeting.ResolveSlotOccupant(
                        intent.TargetSlot, State.PlayerTeam);
                    if (target != null && intent.Move != null)
                        ResolveDamage(enemy, target, intent.Move);
                    break;
                }
                case IntentKind.Cleave:
                {
                    // Per §4.3.4.1 — never fizzles; hits every alive occupant.
                    List<int> targets = IntentTargeting.ResolveCleaveTargets(State.PlayerTeam);
                    for (int t = 0; t < targets.Count; t++)
                    {
                        PokemonInstance occ = State.PlayerTeam[targets[t]];
                        if (occ != null && intent.Move != null)
                            ResolveDamage(enemy, occ, intent.Move);
                    }
                    break;
                }
                case IntentKind.Backstrike:
                {
                    int slot = IntentTargeting.ResolveBackstrikeTarget(
                        intent.TargetSlot, State.PlayerTeam);
                    if (slot >= 0 && intent.Move != null)
                    {
                        PokemonInstance occ = State.PlayerTeam[slot];
                        ResolveDamage(enemy, occ, intent.Move);
                    }
                    // Per §4.3.4.1 — empty slot Backstrike fizzles, no redirect.
                    break;
                }
                case IntentKind.Status:
                {
                    PokemonInstance occ = IntentTargeting.ResolveSlotOccupant(
                        intent.TargetSlot, State.PlayerTeam);
                    if (occ != null && FieldEffectResolver.CanApplyParalysis(State.Field, occ)
                                    || intent.AppliedStatus != StatusCondition.Paralysis)
                    {
                        if (occ != null)
                            StatusEffectManager.TryApply(occ, intent.AppliedStatus, State.Config);
                    }
                    break;
                }
                case IntentKind.Buff:
                    StatStageManager.Modify(enemy, intent.BuffStat, +1);
                    break;
                case IntentKind.Stall:
                    // Stall effects are content-driven; the controller has no
                    // generic action here. TODO Epic 7 — author Stall riders.
                    break;
                case IntentKind.Unknown:
                    // Should never reach resolution still Unknown — intents
                    // are revealed by Witnessed at minimum during Resolution.
                    break;
            }
        }

        // Per §4.1.1 — central damage application. Composes:
        //   DamageCalculator (Power, Atk/Def, Range, Crit, STAB, TypeEff)
        //   × FieldEffectResolver.GetDamageMultiplier (Weather/Terrain)
        //   × StatusModifiers.GetIncomingDamageMultiplier (Frozen+Fire)
        private void ResolveDamage(PokemonInstance attacker, PokemonInstance target, MoveSO move)
        {
            if (attacker == null || target == null || move == null) return;

            // Crit roll via §4.1.3 — three sources composed via CritResolver.
            float passive = CritResolver.GatherPassiveBonus(attacker);
            // ConsumableTempBonus is gathered at draw/play time in production;
            // for the controller skeleton we pass 0 (no consumable-derived
            // crit bonus active by default).
            CritInputs critIn = new(move, combatTempBonus: 0f, permanentPassiveBonus: passive);
            CritResult crit = CritResolver.Resolve(critIn, State.Rng);

            MoveContext ctx = new(attacker, target, move, State.Config, crit.IsCrit);
            DamageBreakdown dmg = DamageCalculator.Compute(ctx);

            float fieldMul = FieldEffectResolver.GetDamageMultiplier(
                State.Field, move.Type, target, State.Config);
            float freezeFireMul = StatusModifiers.GetIncomingDamageMultiplier(
                target, move, State.Config);

            int final = Mathf.FloorToInt(dmg.Final * fieldMul * freezeFireMul);
            if (final <= 0) final = (dmg.TypeEffectiveness == 0.0) ? 0 : 1; // immune stays 0
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - final);
            // Faint resolution happens after each strike chain — see HandleAnyFaints.
            HandleAnyFaints();
        }

        private void TickStatusForAll(IList<PokemonInstance> team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Count; i++)
            {
                PokemonInstance p = team[i];
                if (p == null || p.CurrentHP <= 0) continue;
                int dot = StatusEffectManager.ComputeDoTDamage(p, State.Config);
                if (dot > 0) p.CurrentHP = Mathf.Max(0, p.CurrentHP - dot);
                StatusEffectManager.TickAtEndOfTurn(p);
            }
        }

        // Per §3.3.5 + Task 4.8 — handle faints for both teams.
        // Lead faint on player side → ask agent for replacement.
        // Defeat/Victory check happens here.
        private void HandleAnyFaints()
        {
            // Player team
            for (int i = 0; i < State.PlayerTeam.Count; i++)
            {
                PokemonInstance p = State.PlayerTeam[i];
                if (p == null) continue;
                if (p.CurrentHP > 0) continue;
                // Per §4.8.4 — purge fainted Pokémon's cards from deck +
                // discard via SkillDeck.PurgeOwner (handles factory release).
                // Also sweep the active hand: a fainted Pokémon's cards
                // become unplayable, so they leave play entirely.
                State.Deck.PurgeOwner(p);
                for (int h = State.SkillHand.Count - 1; h >= 0; h--)
                {
                    MoveCardInstance hc = State.SkillHand[h];
                    if (hc != null && ReferenceEquals(hc.Owner, p))
                    {
                        _cardFactory.Release(hc);
                        State.SkillHand.RemoveAt(h);
                    }
                }
                // Per §4.8.5 — +1 Trauma stack at moment of faint.
                FaintResolver.ApplyTraumaOnFaint(p);
            }

            // If Lead is fainted, ask for replacement.
            PokemonInstance lead = ResolveLead();
            if (lead == null || lead.CurrentHP <= 0)
            {
                List<PokemonInstance> candidates = FaintResolver.EligibleLeadReplacements(
                    State.PlayerTeam, lead);
                if (candidates.Count > 0 && _agent != null)
                {
                    int newIdx = _agent.PickLeadReplacement(State, candidates);
                    if (newIdx >= 0 && newIdx < State.PlayerTeam.Count
                        && State.PlayerTeam[newIdx] != null
                        && State.PlayerTeam[newIdx].CurrentHP > 0)
                    {
                        State.LeadIndex = newIdx;
                    }
                }
            }
            // Per §3.3.6 — All-Faint defeat.
            if (FaintResolver.IsAllFainted(State.PlayerTeam))
                State.Outcome = CombatOutcome.Defeat;
            else if (FaintResolver.IsAllFainted(State.EnemyTeam))
                State.Outcome = CombatOutcome.Victory;
        }

        // ── Phase 5: TurnEnd (Task 4.1.7) ────────────────────────────────────

        public void TurnEnd()
        {
            State.CurrentPhase = Phase.TurnEnd;
            // Per §4.4.3.1 — stat stages persist across turn boundaries
            // (and across boss phase transitions). No reset here.
            // SwapCounter resets at next DrawPhase.

            // Unplayed cards in the hand → discard pile so they can be
            // reshuffled into the deck on the next turn when needed.
            for (int i = 0; i < State.SkillHand.Count; i++)
                State.Deck.Discard(State.SkillHand[i]);
            State.SkillHand.Clear();
            // Consumable cards in hand return implicitly — they were never
            // removed from the inventory (drawing was a read; only the
            // played consumable goes into UsedThisCombat in TryPlayConsumable).
            State.ConsumableHand.Clear();
        }

        private void CheckOutcome()
        {
            if (State.Outcome != CombatOutcome.InProgress) return;
            if (FaintResolver.IsAllFainted(State.PlayerTeam))
                State.Outcome = CombatOutcome.Defeat;
            else if (FaintResolver.IsAllFainted(State.EnemyTeam))
                State.Outcome = CombatOutcome.Victory;
        }

        // ── Combat End (Task 4.1.8) ──────────────────────────────────────────

        public void CombatEnd()
        {
            State.CurrentPhase = Phase.CombatEnd;
            // Per §4.2.1 — clear all status conditions on every Pokémon.
            ClearAllStatus(State.PlayerTeam);
            ClearAllStatus(State.EnemyTeam);
            // Per §4.2.6 — reset stat stages on every Pokémon.
            ResetAllStatStages(State.PlayerTeam);
            ResetAllStatStages(State.EnemyTeam);
            // Per §3.5 — consumables are NOT expendable; ConsumablePile.RestoreAll
            // clears the UsedThisCombat list so the full inventory is available
            // again outside combat. (Bestiary / XP / OnCombatEnded event are
            // downstream — see Epic 10 / 11.)
            State.Consumables.RestoreAll();
            // Release any leftover hand cards back through the factory before
            // dropping references. The Deck.Clear() call below handles the
            // rest of the deck + discard lifetimes.
            for (int i = 0; i < State.SkillHand.Count; i++)
                _cardFactory.Release(State.SkillHand[i]);
            State.SkillHand.Clear();
            State.ConsumableHand.Clear();
        }

        private static void ClearAllStatus(IList<PokemonInstance> team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Count; i++)
                StatusEffectManager.ClearOnCombatEnd(team[i]);
        }

        private static void ResetAllStatStages(IList<PokemonInstance> team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Count; i++)
                StatStageManager.ResetAll(team[i]);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public PokemonInstance ResolveLead()
        {
            if (State.PlayerTeam == null) return null;
            if (State.LeadIndex < 0 || State.LeadIndex >= State.PlayerTeam.Count) return null;
            return State.PlayerTeam[State.LeadIndex];
        }

        public PokemonInstance ResolveEnemySlot(int slot)
        {
            if (State.EnemyTeam == null) return null;
            if (slot < 0 || slot >= State.EnemyTeam.Count) return null;
            return State.EnemyTeam[slot];
        }

        // Per Epic 5 Task 5.1.1 — deck construction now lives on SkillDeck.Build.
        // CombatController.Start() invokes it via State.Deck.Build(State.PlayerTeam).
    }
}
