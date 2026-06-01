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
            // Per §6.2 + Epic 11 Task 11.1.8 — Trauma params source for EffectiveMaxHP (DoT, HP-bar max).
            // Optional: when null, combat falls back to raw MaxHP (e.g. unit tests that don't model Trauma).
            public EconomyConfigSO Economy;
            public GameRNG Rng;
            // Per §7.4 + Epic 8 Task 8.2 — optional. When the enemy team
            // wipes, the controller consults this provider; a non-empty
            // return replaces the EnemyTeam contents and Outcome stays
            // InProgress (sequential trainer / boss-phase spawns).
            public IEnemyReinforcementProvider Reinforcements;

            // Per §4.4.5 + Epic 8 Task 8.5.7 — run-wide Badges active this
            // combat (sourced from RunStateSO.EarnedBadges by the run layer).
            // Optional; null/empty = no badges. Currently consumed for the
            // Boulder Badge's Lead incoming-damage reduction (§4.4.5.1).
            public List<BadgeSO> ActiveBadges;
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
            public SkillDeck Deck;
            // Per Epic 5 Task 5.2.1 — Hand class bundles the two compartments.
            // SkillHand / ConsumableHand below are backwards-compat forwarders
            // returning the underlying mutable lists (List<T>) so existing
            // callers' Add / RemoveAt / Clear / indexer usage keeps working
            // without per-call-site churn.
            public Hand Hand = new();
            public List<MoveCardInstance> SkillHand => Hand.Skill;
            public List<ConsumableSO> ConsumableHand => Hand.Consumables;

            // Snapshot read accessors so tests / UI can inspect deck + discard
            // without knowing the SkillDeck class. Forwarded to Deck.
            public IReadOnlyList<MoveCardInstance> SkillDeckView =>
                Deck != null ? Deck.DeckView : System.Array.Empty<MoveCardInstance>();
            public IReadOnlyList<MoveCardInstance> DiscardView =>
                Deck != null ? Deck.DiscardView : System.Array.Empty<MoveCardInstance>();

            // Per Epic 5 Task 5.1.2 — ConsumablePile owns inventory ref + UsedThisCombat.
            public List<ConsumableSO> ConsumableInventory = new();  // persistent
            public ConsumablePile Consumables;

            // Per Epic 5 Task 5.2.3 — additive hand-size modifiers from relics
            // / Badges / Region Modifiers. Wired through the EventContext
            // DrawCardHook surface in Epic 12; this field is the accumulator
            // the controller reads at DrawPhase time. Resets at combat end
            // (per-combat span); a per-turn dispatch would refresh it pre-draw.
            public int SkillHandSizeBonus;
            public int ConsumableHandSizeBonus;

            public int CurrentAP;
            public int SwapCounter;                       // per-turn (§3.3.1)
            // Per §3.3.1 + Epic 5 Task 5.6.2 — a manual Lead swap grants a
            // 1-AP discount to the FIRST Defensive-tagged card played after
            // the swap, that turn. SF/SB swaps do NOT set this. Reset to false
            // at DrawPhase. Consumed by the first Defensive card play that
            // matches CardPlayValidator.ShouldConsumeDefensiveDiscount.
            public bool DefensiveSwapDiscountAvailable;
            public int TurnNumber;
            public FieldState Field;
            public List<Intent> EnemyIntents = new();
            public CombatOutcome Outcome = CombatOutcome.InProgress;
            public Phase CurrentPhase = Phase.PreStart;
            public BattleConfigSO Config;
            // Per §6.2 / Task 11.1.8 — Trauma-aware EffectiveMaxHP source (nullable; null → raw MaxHP).
            public EconomyConfigSO Economy;
            public GameRNG Rng;

            // Per §7.3.4 + Epic 8 Task 8.1 — non-null iff a wild Pokémon was
            // caught this combat via a CatchConsumableEffectSO. Read by
            // WildEncounterController.ResolveOutcome to disambiguate
            // "Victory by catch" from "Victory by KO". Cleared at CombatEnd.
            public PokemonInstance CaughtTarget;

            // Per §4.4.5 + Task 8.5.7 — run-wide Badges active this combat.
            // Never null after construction.
            public List<BadgeSO> ActiveBadges = new();
        }

        // ── State ────────────────────────────────────────────────────────────

        public CombatState State { get; }
        private readonly IPlayerAgent _agent;
        private readonly MoveCardInstanceFactory _cardFactory;
        private readonly CardPlayService _playService;
        private readonly IEnemyReinforcementProvider _reinforcements;

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
            _reinforcements = setup.Reinforcements;
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
                Economy = setup.Economy,
                Rng = setup.Rng,
                Deck = new SkillDeck(_cardFactory),
                Consumables = new ConsumablePile(),
                ActiveBadges = setup.ActiveBadges != null
                    ? new List<BadgeSO>(setup.ActiveBadges)
                    : new List<BadgeSO>(),
            };
            State.Consumables.Build(State.ConsumableInventory);
            // Per Epic 5 Task 5.4.1 — extracted play pipeline. Wired with a
            // ResolveDamage callback so the service can resolve a strike
            // without the controller owning the play logic.
            _playService = new CardPlayService(State, _agent, _cardFactory, ResolveDamage);
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
            AbilityResolver.ApplyLeadEntryEffects(State); // §5.5.3.5 Intimidate — initial Lead enters
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
            State.DefensiveSwapDiscountAvailable = false; // §3.3.1 — per-turn
            State.SkillHand.Clear();
            State.ConsumableHand.Clear();

            // Per §3.7 + Task 5.2.3 — effective hand sizes are base + bonus.
            int skillTarget = HandSizeCalculator.EffectiveSkillCount(
                State.Config, State.SkillHandSizeBonus);
            int consumableTarget = HandSizeCalculator.EffectiveConsumableCount(
                State.Config, State.ConsumableHandSizeBonus);
            // Per §5.5.3 — Swift Swim: +1 skill draw on turn 1 of a Rain combat.
            skillTarget += AbilityResolver.SwiftSwimDrawBonus(
                State.PlayerTeam, State.Field, State.TurnNumber, State.Config);
            DrawSkillCards(skillTarget);
            DrawConsumableCards(consumableTarget);

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
            // Per §4.4.3 / §4.4.4.3 + Task 8.5 — fire boss phase transitions
            // (mid-fight evolution @ Phase 2, last-stand cooldown reset @
            // Phase 3) BEFORE the boss declares intents, so the escalated form
            // acts this turn. No-op for non-boss enemies (PhaseCount 1).
            ProcessBossPhaseTransitions();
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

            // Per §5.5.3.1 — Keen Eye: a team holding it reveals any Hidden intents (latent while VS
            // intents are all Witnessed; active once Hidden/counter-intel content exists).
            if (AbilityResolver.TeamRevealsIntents(State.PlayerTeam))
                for (int i = 0; i < State.EnemyIntents.Count; i++)
                {
                    Intent it = State.EnemyIntents[i];
                    if (it.Reveal == IntentReveal.Hidden)
                    {
                        it.Reveal = IntentReveal.Witnessed;
                        State.EnemyIntents[i] = it;
                    }
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
                    TargetsLead = true, // re-resolves to the current Lead at resolution (§4.3.2 / Pillar 2)
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
                    TargetsLead = true,
                    Reveal = IntentReveal.Witnessed,
                });
            }

            if (candidates.Count == 0)
                return new Intent { Kind = IntentKind.Stall, Reveal = IntentReveal.Witnessed };

            // Per §4.4.3 + Epic 8 Task 8.4 — a multi-phase enemy (PhaseCount > 1)
            // becomes aggressive once its HP crosses BossPhase2HPThreshold.
            // BossPhaseTracker returns Phase 1 for ordinary Pokémon, so this is
            // a no-op for wild/standard-trainer encounters.
            bool phaseAggressive = BossPhaseTracker.IsAggressivePhase(enemy, State.Config);

            IntentScorer.Context ctx = new()
            {
                Attacker = enemy,
                PlayerTeam = State.PlayerTeam,
                Config = State.Config,
                BossCounterIntelActive = false, // counter-intel wired with Gym boss (Task 8.5)
                PhaseAggressive = phaseAggressive,
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

        // Per Epic 5 Task 5.4.1 — delegated to CardPlayService. The service
        // owns validation, AP spend, discount apply/consume, SF/SB position
        // changes, and the post-play combat-end short-circuit (§3.2.4).
        private bool TryPlaySkillCard(int handIndex, int enemySlot) =>
            _playService.Play(handIndex, enemySlot);

        private bool TryPlayConsumable(int handIndex)
        {
            if (handIndex < 0 || handIndex >= State.ConsumableHand.Count) return true;
            ConsumableSO c = State.ConsumableHand[handIndex];
            if (c == null) return true;

            // Per §3.5 — consumable AP cost is typically 0; tracked on the SO.
            // We don't read it here yet (ConsumableSO authoring varies); when
            // wired this becomes: if (c.APCost > State.CurrentAP) return true;
            // TODO Epic 12: full ConsumableEffectSO dispatch chain — only the
            // Catch effect is dispatched below; Heal/Cure/AP/StatBoost still
            // fall through to the generic MarkUsed path.
            DispatchConsumableEffect(c);

            State.ConsumableHand.RemoveAt(handIndex);
            State.Consumables.MarkUsed(c);
            return true;
        }

        // Per Epic 8 Task 8.1.4 — only CatchConsumableEffectSO is wired here.
        // The wild Pokémon occupies enemy slot 0 in §7.3.4 encounters; on
        // successful catch we set CaughtTarget and clear EnemyTeam so the
        // existing CheckOutcome path flips Outcome → Victory naturally.
        // Failed catches (HP too high / fainted) consume the ball with no
        // state change beyond MarkUsed (handled by caller).
        private void DispatchConsumableEffect(ConsumableSO consumable)
        {
            if (consumable == null) return;
            if (consumable.Effect is not CatchConsumableEffectSO catchEffect) return;

            // Wild encounters always occupy slot 0; in trainer/boss fights
            // a Pokéball throw would target slot 0 too and either fail (no
            // catchable wild) or fizzle harmlessly (no-op below).
            PokemonInstance wild = ResolveEnemySlot(0);
            if (wild == null) return;

            if (!WildCatchResolver.IsCatchable(wild, catchEffect)) return;

            State.CaughtTarget = wild;
            // Per §7.3.4.1 step 6 — combat ends; clear the enemy team so the
            // existing IsAllFainted-driven Victory path fires on the next
            // CheckOutcome / HandleAnyFaints invocation.
            State.EnemyTeam.Clear();
        }

        // Per §3.3.1 + Epic 6 Task 6.1 — delegated to SwapManager. The action
        // loop convention is to return true even on rejection so the player
        // can try a different action; SwapManager's bool result is purely an
        // observability signal here.
        private bool TryManualSwap(int benchSlot)
        {
            SwapManager.TryManualSwap(State, benchSlot);
            return true;
        }

        // ── Phase 4: Resolution (Task 4.1.6) ─────────────────────────────────

        public void ResolutionPhase()
        {
            State.CurrentPhase = Phase.ResolutionPhase;

            // Per §4.3.6 + Epic 8 Task 8.6 — multi-enemy resolution order:
            // SUPPORTS first (slots 1..N, in slot order), then the LEAD enemy
            // (slot 0) LAST. Convention: enemy slot 0 is the Lead enemy; higher
            // slots are supports (Healer/Buffer/Debuffer/Attacker — full
            // support AI is a Region-3 accent, so the VS only proves the
            // ordering + per-enemy targeting architecture). Single-enemy
            // encounters (wild/trainer/Elite/Gym) have only slot 0, so this is
            // identical to the old list-order resolution for them.
            int n = State.EnemyIntents.Count < State.EnemyTeam.Count
                ? State.EnemyIntents.Count : State.EnemyTeam.Count;
            for (int i = 1; i < n; i++)
                if (!ResolveEnemyIntentAt(i)) return; // a support changed outcome
            if (n > 0 && !ResolveEnemyIntentAt(0)) return; // Lead enemy last

            // Status DoT + duration ticks for every Pokémon on both sides.
            TickStatusForAll(State.PlayerTeam);
            TickStatusForAll(State.EnemyTeam);
            // Per §4.3.3 — cooldown decrement end-of-turn, both sides.
            // Player-side is symmetric for future use (no VS card sets
            // CooldownTurns yet); enemy-side is the active gate for AI.
            TickCooldownsForAll(State.PlayerTeam);
            TickCooldownsForAll(State.EnemyTeam);
            // DoT can cause faints — check.
            HandleAnyFaints();
        }

        // Per §4.3.6 + Task 8.6 — resolve one enemy's intent by slot index.
        // Returns true to continue the resolution sequence, false if combat
        // ended (caller stops). A fainted/empty slot is skipped (returns true).
        private bool ResolveEnemyIntentAt(int i)
        {
            PokemonInstance enemy = State.EnemyTeam[i];
            if (enemy == null || enemy.CurrentHP <= 0) return true;
            ExecuteEnemyIntent(enemy, State.EnemyIntents[i]);
            return State.Outcome == CombatOutcome.InProgress;
        }

        private void ExecuteEnemyIntent(PokemonInstance enemy, Intent intent)
        {
            switch (intent.Kind)
            {
                case IntentKind.Attack:
                {
                    // Per §4.3.2 / Pillar 2 — a Lead-targeted attack hits whoever is the Lead NOW
                    // (after any manual swap / SF / SB this turn), not the Pokémon that was Lead when
                    // the intent was declared. Fixed-slot intents use TargetSlot unchanged.
                    PokemonInstance target = IntentTargeting.ResolveSlotOccupant(
                        intent.EffectiveTargetSlot(State.LeadIndex), State.PlayerTeam);
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
                        intent.EffectiveTargetSlot(State.LeadIndex), State.PlayerTeam);
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

            // Per §4.3.3 — after a Move-bearing intent resolves, lock the
            // attacker's cooldown if the move authors one. Set unconditionally
            // (success/fizzle is irrelevant — the AI committed the choice).
            if (intent.Move != null && intent.Move.CooldownTurns > 0)
                enemy.SetMoveCooldown(intent.Move, intent.Move.CooldownTurns);
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
            // Per §5.5.4 + Epic 6 Task 6.6 — Lead Aura broadcasts a +5%
            // (config-driven) damage bonus to BENCH attackers on the player
            // team whose move type matches an active aura source on the
            // player's Lead. Sources stack additively (Ability + Held Item
            // of the same type → +10%). The resolver returns 1.0 for any
            // attacker not in PlayerTeam, so enemy intents are unaffected.
            float playerAuraMul = LeadAuraResolver.GetDamageMultiplier(
                attacker, move, ResolveLead(), State.PlayerTeam, State.Config);

            // Per §5.5.3.4 — Overgrow/Blaze/Torrent: the attacker's matching-type moves deal +X%
            // while its HP is below the threshold (1.0 for everyone else).
            float abilityOutMul = AbilityResolver.OutgoingDamageMultiplier(attacker, move, State.Config);

            int final = Mathf.FloorToInt(dmg.Final * fieldMul * freezeFireMul * playerAuraMul * abilityOutMul);
            if (final <= 0) final = (dmg.TypeEffectiveness == 0.0) ? 0 : 1; // immune stays 0

            // Per §5.5.3.3 — Levitate: Ground-type moves do nothing to a Levitate target.
            if (AbilityResolver.IsImmuneTo(target, move)) final = 0;

            // Per §4.4.5.1 — Boulder Badge: flat reduction on damage INCOMING
            // to the player's Lead (minimum 0). Applied AFTER the non-immune
            // floor so a 1-damage hit can be reduced to 0 ("reduces all
            // incoming damage by 1, minimum 0"). Non-Lead targets unaffected.
            if (final > 0 && IsPlayerLead(target))
            {
                // Per §5.8 — Shell Armor stacks additively with the Boulder Badge reduction.
                int reduction = SumLeadIncomingDamageReduction()
                              + AbilityResolver.IncomingFlatReduction(target, State.Config);
                if (reduction > 0) final = Mathf.Max(0, final - reduction);
            }

            // Per §4.4.3 Phase 3 — Sturdy: an ace with HasSturdy survives the
            // first otherwise-lethal hit at 1 HP, once per combat (last-stand).
            // Robust to one-shots: does not require having "entered" Phase 3 on
            // a prior turn. A boss already at 1 HP is not protected.
            if (final >= target.CurrentHP && target.CurrentHP > 1
                && AbilityResolver.HasSturdy(target) && !target.SturdyConsumed)
            {
                target.SturdyConsumed = true;
                target.CurrentHP = 1;
            }
            else
            {
                target.CurrentHP = Mathf.Max(0, target.CurrentHP - final);
            }
            // Per §5.5.3 — Static: a surviving target hit by an Electric move may be Paralysed.
            if (target.CurrentHP > 0
                && AbilityResolver.RollStaticParalysis(attacker, move, State.Config, State.Rng))
                StatusEffectManager.TryApply(target, StatusCondition.Paralysis, State.Config);

            // Faint resolution happens after each strike chain — see HandleAnyFaints.
            HandleAnyFaints();
        }

        // Per §4.4.5.1 — true iff this instance is the player's current Lead.
        private bool IsPlayerLead(PokemonInstance p) => p != null && p == ResolveLead();

        // Per §4.4.5.1 — sum the Lead incoming-damage reduction across every
        // active Badge (data-driven; Boulder contributes 1). 0 if none.
        private int SumLeadIncomingDamageReduction()
        {
            if (State.ActiveBadges == null) return 0;
            int sum = 0;
            for (int i = 0; i < State.ActiveBadges.Count; i++)
            {
                BadgeSO b = State.ActiveBadges[i];
                if (b != null && b.LeadIncomingDamageReduction > 0)
                    sum += b.LeadIncomingDamageReduction;
            }
            return sum;
        }

        // Per §4.4.3 / §4.4.4.3 + Task 8.5 — boss phase-transition director.
        // Runs at IntentPhase start: detects upward phase crossings (vs the
        // instance's LastObservedPhase) and fires the one-shot transition
        // effects. No-op for ordinary enemies (PhaseCount 1).
        private void ProcessBossPhaseTransitions()
        {
            if (State.EnemyTeam == null) return;
            for (int i = 0; i < State.EnemyTeam.Count; i++)
            {
                PokemonInstance e = State.EnemyTeam[i];
                if (e == null || e.CurrentHP <= 0 || e.PhaseCount <= 1) continue;

                int phase = BossPhaseTracker.CurrentPhase(e, State.Config);
                if (phase <= e.LastObservedPhase) continue; // escalate forward only

                // Entering Phase 2 (HP ≤ 50%) → mid-fight evolution (ace, once).
                if (phase >= 2 && e.MidFightEvolutionTarget != null && !e.HasEvolvedMidFight)
                    EvolveMidFight(e);

                // Entering Phase 3 (HP ≤ 20%) → last-stand: reset cooldowns so
                // the signature move fires without cooldown (§4.4.3).
                if (phase >= 3)
                    e.MoveCooldowns.Clear();

                e.LastObservedPhase = phase;
            }
        }

        // Per §4.4.4.3 — mid-fight evolution. Swaps the ace to its evolved
        // species and preserves the HP FRACTION across the stat jump, so the
        // evolution is a power spike (bigger effective max HP + Atk/Def) rather
        // than an instant phase shift. Stats recompute downstream from the new
        // Species.BaseStats; moves are unchanged in the VS.
        private void EvolveMidFight(PokemonInstance e)
        {
            PokemonSpeciesSO target = e.MidFightEvolutionTarget;
            if (target == null) return;
            float frac = (float)e.CurrentHP / Mathf.Max(1, EffectiveMaxHP(e));
            e.Species = target;
            e.HasEvolvedMidFight = true;
            int newMax = EffectiveMaxHP(e);
            e.CurrentHP = Mathf.Clamp(Mathf.RoundToInt(newMax * frac), 1, newMax);
        }

        // MaxHP = Species.BaseStats.BaseHP + GrowthCurve.GetHPAt(Level).
        // Mirrors IntentScorer/BossPhaseTracker; see the shared-helper TODO.
        private static int EffectiveMaxHP(PokemonInstance p)
        {
            if (p == null || p.Species == null) return 1;
            int max = p.Species.BaseStats.BaseHP;
            if (p.Species.GrowthCurve != null)
                max += p.Species.GrowthCurve.GetHPAt(p.Level);
            return max <= 0 ? 1 : max;
        }

        private void TickStatusForAll(IList<PokemonInstance> team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Count; i++)
            {
                PokemonInstance p = team[i];
                if (p == null || p.CurrentHP <= 0) continue;
                int dot = StatusEffectManager.ComputeDoTDamage(p, State.Config, State.Economy);
                if (dot > 0) p.CurrentHP = Mathf.Max(0, p.CurrentHP - dot);
                StatusEffectManager.TickAtEndOfTurn(p);
            }
        }

        // Per §4.3.3 — decrement every active move cooldown by 1; entries that
        // hit 0 are removed by TickMoveCooldowns. Fainted slots skip (cooldown
        // map is per-instance; faint clears via Reset on box return).
        private void TickCooldownsForAll(IList<PokemonInstance> team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Count; i++)
            {
                PokemonInstance p = team[i];
                if (p == null || p.CurrentHP <= 0) continue;
                p.TickMoveCooldowns();
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
                State.Deck.PurgeOwner(p);
                // Per Epic 5 Task 5.5.2 — fainted-owner cards STAY in the
                // hand until TurnEnd ("greyed out, not hidden"). The UI
                // shows them as unplayable; CardPlayValidator rejects play
                // attempts with PlayResult.OwnerFainted; TurnEnd drops them
                // (rather than sending to discard) so they don't get
                // reshuffled into the deck next turn.
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
                        AbilityResolver.ApplyLeadEntryEffects(State); // §5.5.3.5 Intimidate
                    }
                }
            }
            // Per §3.3.6 — All-Faint defeat.
            if (FaintResolver.IsAllFainted(State.PlayerTeam))
            {
                State.Outcome = CombatOutcome.Defeat;
            }
            else if (FaintResolver.IsAllFainted(State.EnemyTeam))
            {
                // Per §7.4 + Epic 8 Task 8.2.3 — sequential trainer/boss spawn.
                // If a provider supplies reinforcements, swap them into the
                // EnemyTeam in-place and keep the combat live. New entrants
                // do not act this turn — IntentPhase rebuilds intents fresh
                // on the next loop iteration.
                if (TryInjectReinforcements()) return;
                State.Outcome = CombatOutcome.Victory;
            }
        }

        // Per Epic 8 Task 8.2 — returns true if reinforcements landed (so the
        // caller knows to skip the Outcome.Victory branch). Replaces team
        // contents in-place rather than swapping the list reference so any
        // downstream code holding the IList<PokemonInstance> stays valid.
        private bool TryInjectReinforcements()
        {
            if (_reinforcements == null) return false;
            List<PokemonInstance> next = _reinforcements.RequestReinforcements(State);
            if (next == null || next.Count == 0) return false;
            State.EnemyTeam.Clear();
            for (int i = 0; i < next.Count; i++) State.EnemyTeam.Add(next[i]);
            return true;
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
            // Per Task 5.5.2 — fainted-owner cards must NOT re-enter the
            // deck (their owner can't play anything anyway). Release them
            // here instead of sending to discard.
            for (int i = 0; i < State.SkillHand.Count; i++)
            {
                MoveCardInstance hc = State.SkillHand[i];
                if (hc == null) continue;
                PokemonInstance owner = hc.Owner;
                if (owner != null && owner.CurrentHP == 0)
                    _cardFactory.Release(hc);
                else
                    State.Deck.Discard(hc);
            }
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
            {
                State.Outcome = CombatOutcome.Defeat;
            }
            else if (FaintResolver.IsAllFainted(State.EnemyTeam))
            {
                // Mirror HandleAnyFaints — consult reinforcements once more
                // in case a status DoT (Burn/Poison) fainted the last enemy
                // on a turn where the provider hasn't been asked yet.
                if (TryInjectReinforcements()) return;
                State.Outcome = CombatOutcome.Victory;
            }
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
