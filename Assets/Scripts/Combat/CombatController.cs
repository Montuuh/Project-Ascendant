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

        // ── R4-4: Combat log data layer (§playtest 2026-06-02) ──────────────
        public enum CombatLogCategory { PlayerAction, TurnEvent, EnemyAction }
        public struct CombatLogEntry
        {
            public CombatLogCategory Category;
            public string Message;
            public CombatLogEntry(CombatLogCategory cat, string msg)
            {
                Category = cat;
                Message = msg;
            }
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
            // Per §6.9 / Task 11.8 — optional Bestiary; enemy faints record a kill into it.
            public BestiaryProgressSO Bestiary;
            // Per §4.3.9.1 — optional Meta; reaching the Bestiary Master tier for a defeated
            // species unlocks that species' Mastery Move into Meta.UnlockedMasteryMoveIds.
            public MetaProgressionSO Meta;
            // Per §8.3 / Task 12.3 — the run's held Trainer Relics (RelicResolver dispatch). Optional.
            public List<RelicSO> ActiveRelics;
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

            // Per §4.3.9.2 — Mastery MoveIds unlocked in meta (MetaProgressionSO.UnlockedMasteryMoveIds).
            // A Pokémon's Mastery card is built into the Skill Deck only if its MoveId is here.
            // Null/empty ⇒ no Mastery cards (locked).
            public ICollection<string> UnlockedMasteryIds;
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
            public int ManualSwapsThisCombat;             // §8.3.4 — for Tactician's Coin / Defense Curl Charm
            public int RangedMovesPlayedThisTurn;         // §8.3.4 — Choice Specs
            public int MeleeMovesPlayedThisTurn;          // §8.3.4 — Choice Band
            public bool SmokeBallTriggeredThisCombat;     // §8.3.3 — Smoke Ball (first enemy attack)
            // §8.3.4 Move Echo — distinct moves played per attacker this turn → +2 AP next turn.
            public Dictionary<PokemonInstance, HashSet<MoveSO>> MovesPlayedThisTurn = new();
            public bool MoveEchoGrantedThisTurn;
            public int PendingBonusAPNextTurn;
            public bool QuickClawUsedThisCombat;          // §8.3.3 — Quick Claw Charm (once per combat)
            // Per §3.3.1 + Epic 5 Task 5.6.2 — a manual Lead swap grants a
            // 1-AP discount to the FIRST Defensive-tagged card played after
            // the swap, that turn. SF/SB swaps do NOT set this. Reset to false
            // at DrawPhase. Consumed by the first Defensive card play that
            // matches CardPlayValidator.ShouldConsumeDefensiveDiscount.
            public bool DefensiveSwapDiscountAvailable;

            // Per §4.4.5.1 — Cascade Badge: manual swap → +1 skill card draw
            // this turn. Set by SwapManager on manual swap; consumed + reset at DrawPhase.
            public bool CascadeBadgeDrawPending;

            // Per §4.4.5.1 — Hive Badge: cards cycling discard→deck have 20%
            // to spawn a free copy next turn. This queue accumulates pending
            // copies during TurnEnd (reshuffle); DrawPhase adds them to hand.
            public List<MoveCardInstance> HiveBadgePendingCopies = new();

            public int TurnNumber;
            public FieldState Field;
            public List<Intent> EnemyIntents = new();
            // Per §playtest R4-4 — readable combat log for UI display (PlayerAction/TurnEvent/EnemyAction).
            // Cleared at Start(); appended throughout combat phases. UI renders this in a scrollable panel.
            public List<CombatLogEntry> CombatLog = new();
            public CombatOutcome Outcome = CombatOutcome.InProgress;
            public Phase CurrentPhase = Phase.PreStart;
            public BattleConfigSO Config;
            // Per §6.2 / Task 11.1.8 — Trauma-aware EffectiveMaxHP source (nullable; null → raw MaxHP).
            public EconomyConfigSO Economy;
            // Per §6.9 / Task 11.8 — Bestiary that enemy faints record into (nullable in tests).
            public BestiaryProgressSO Bestiary;
            // Per §4.3.9.1 — Meta receiving Bestiary-driven Mastery unlocks (nullable in tests).
            public MetaProgressionSO Meta;
            // Per §8.3 / Task 12.3 — held Trainer Relics active this combat (RelicResolver). Never null.
            public List<RelicSO> ActiveRelics = new();
            public GameRNG Rng;

            // Per §7.3.4 + Epic 8 Task 8.1 — non-null iff a wild Pokémon was
            // caught this combat via a CatchConsumableEffectSO. Read by
            // WildEncounterController.ResolveOutcome to disambiguate
            // "Victory by catch" from "Victory by KO". Cleared at CombatEnd.
            public PokemonInstance CaughtTarget;

            // Per §4.4.5 + Task 8.5.7 — run-wide Badges active this combat.
            // Never null after construction.
            public List<BadgeSO> ActiveBadges = new();

            // Per §4.3.9.2 — unlocked Mastery MoveIds for this combat's deck build. Never null.
            public HashSet<string> UnlockedMasteryIds = new();

            // Per §3.3.5 + Bug #10 — when the Lead faints, the controller pauses
            // mid-HandleAnyFaints and exposes the replacement candidates here.
            // UI shows a picker modal; player clicks; UI calls ApplyLeadReplacement(index).
            // Null when no replacement is pending.
            public IReadOnlyList<PokemonInstance> PendingLeadReplacementCandidates;

            // Per §3.2.6 (OPEN) + R2-5 Task 5 — when reinforcements arrive mid-combat,
            // the player gets a "Breather" beat: +BreatherBonusAP and permission for
            // ONE action (card play OR manual swap) before the next turn starts.
            // UI-driven: UI checks this flag and renders a modal / gated ActionPhase
            // continuation. Headless path (tests/AI) auto-clears when no valid action.
            public bool BreatherPending;
            // Counts actions during the breather so only ONE is allowed (card play
            // OR manual swap). Reset to 0 when BreatherPending is cleared.
            public int BreatherActionsAllowed;
        }

        // ── State ────────────────────────────────────────────────────────────

        public CombatState State { get; }
        private readonly IPlayerAgent _agent;
        private readonly MoveCardInstanceFactory _cardFactory;
        private readonly CardPlayService _playService;
        private readonly IEnemyReinforcementProvider _reinforcements;
        // Per §6.9 / Task 11.8.2 — enemies whose kill has already been recorded this combat (once each,
        // even across the multiple HandleAnyFaints passes before reinforcements clear them).
        private readonly HashSet<PokemonInstance> _recordedEnemyKills = new();

        // Per §6.2.2 / Bug #9 — each fainted player Pokémon gets +1 Trauma
        // EXACTLY once per combat, even if hit multiple times or DoT-ticked
        // after fainting (mirrors _recordedEnemyKills pattern).
        private readonly HashSet<PokemonInstance> _traumaApplied = new();

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
                Bestiary = setup.Bestiary,
                Meta = setup.Meta,
                ActiveRelics = setup.ActiveRelics != null
                    ? new List<RelicSO>(setup.ActiveRelics)
                    : new List<RelicSO>(),
                Rng = setup.Rng,
                Deck = new SkillDeck(_cardFactory),
                Consumables = new ConsumablePile(),
                ActiveBadges = setup.ActiveBadges != null
                    ? new List<BadgeSO>(setup.ActiveBadges)
                    : new List<BadgeSO>(),
                UnlockedMasteryIds = setup.UnlockedMasteryIds != null
                    ? new HashSet<string>(setup.UnlockedMasteryIds)
                    : new HashSet<string>(),
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
            State.Deck.Build(State.PlayerTeam, State.UnlockedMasteryIds);
            State.Consumables.Build(State.ConsumableInventory);
            State.TurnNumber = 0;
            State.CombatLog.Clear(); // Per R4-4 — fresh log each combat
            State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.TurnEvent, "Combat started"));
            AbilityResolver.ApplyLeadEntryEffects(State); // §5.5.3.5 Intimidate — initial Lead enters
            RecordEnemiesSeen(); // §6.9 — Pokédex discovery: the enemy species are now "seen"
        }

        // Per §6.9 — mark every current enemy species as seen in the Pokédex (combat start + each
        // reinforcement wave). No-op when no Bestiary is wired (unit tests).
        private void RecordEnemiesSeen()
        {
            if (State.Bestiary == null || State.EnemyTeam == null) return;
            for (int i = 0; i < State.EnemyTeam.Count; i++)
            {
                PokemonInstance e = State.EnemyTeam[i];
                if (e != null && e.Species != null) State.Bestiary.RecordSeen(e.Species.SpeciesId);
            }
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
            State.CombatLog.Add(new CombatLogEntry(
                CombatLogCategory.TurnEvent, $"=== Turn {State.TurnNumber} ==="));
            // §8.3.4 Move Echo — carry the +2 AP bonus earned last turn, then clear it.
            State.CurrentAP = State.Config.BaseAPPerTurn + State.PendingBonusAPNextTurn;
            if (State.PendingBonusAPNextTurn > 0)
                State.CombatLog.Add(new CombatLogEntry(
                    CombatLogCategory.TurnEvent, $"Move Echo: +{State.PendingBonusAPNextTurn} AP"));
            State.PendingBonusAPNextTurn = 0;
            State.SwapCounter = 0;
            State.RangedMovesPlayedThisTurn = 0;          // §8.3.4 — Choice Specs/Band reset per turn
            State.MeleeMovesPlayedThisTurn = 0;
            State.MovesPlayedThisTurn.Clear();            // §8.3.4 — Move Echo per-turn tracking
            State.MoveEchoGrantedThisTurn = false;
            State.DefensiveSwapDiscountAvailable = false; // §3.3.1 — per-turn
            State.BreatherPending = false;                // §3.2.6 (OPEN) — breather is a per-transition beat, cleared at turn start
            State.BreatherActionsAllowed = 0;
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
            // §8.3.3 Quick Draw relic — +1 skill draw on turn 1.
            skillTarget += RelicResolver.QuickDrawBonus(State.ActiveRelics, State.TurnNumber);

            // Per §4.4.5.1 — Cascade Badge: manual swap last turn → +1 skill draw.
            if (State.CascadeBadgeDrawPending)
            {
                skillTarget += 1;
                State.CascadeBadgeDrawPending = false;
                State.CombatLog.Add(new CombatLogEntry(
                    CombatLogCategory.TurnEvent, "Cascade Badge: +1 card"));
            }

            // Per §4.4.5.1 — Hive Badge: add pending free copies from last turn's
            // 20% cycle procs. These bypass normal draw (directly to hand).
            if (State.HiveBadgePendingCopies.Count > 0)
            {
                for (int i = 0; i < State.HiveBadgePendingCopies.Count; i++)
                {
                    MoveCardInstance copy = State.HiveBadgePendingCopies[i];
                    State.SkillHand.Add(copy);
                    State.CombatLog.Add(new CombatLogEntry(
                        CombatLogCategory.TurnEvent,
                        $"Hive Badge: free {copy.Move.DisplayName} copy"));
                }
                State.HiveBadgePendingCopies.Clear();
            }

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
            // Per §4.4.5.1 — Hive Badge: detect reshuffles and proc 20% on each
            // cycled card to create a free copy next turn.
            for (int i = 0; i < count; i++)
            {
                int preDeckCount = State.Deck.DeckCount;
                int preDiscardCount = State.Deck.DiscardCount;
                MoveCardInstance card = State.Deck.Draw(State.Rng);
                if (card == null) break;
                State.SkillHand.Add(card);

                // Detect reshuffle: deck was 0, discard > 0, now deck > 0, discard == 0.
                bool reshuffleOccurred = (preDeckCount == 0 && preDiscardCount > 0
                    && State.Deck.DeckCount > 0 && State.Deck.DiscardCount == 0);
                if (reshuffleOccurred)
                    ProcessHiveBadgeOnReshuffle(preDiscardCount);
            }
        }

        // Per §4.4.5.1 — Hive Badge: when discard pile reshuffles into deck,
        // each card has a 20% chance to spawn a free copy next turn.
        // cycledCount = number of cards that just moved discard→deck.
        private void ProcessHiveBadgeOnReshuffle(int cycledCount)
        {
            if (!HoldsBadge(State.ActiveBadges, "hive_badge")) return;
            if (cycledCount <= 0) return;

            // The cycled cards are now shuffled into the deck (inaccessible).
            // We can't inspect them directly, but we CAN snapshot the DeckView
            // and roll 20% for each. To avoid duplicating cards that might be
            // drawn this same turn, we defer the copies to a pending queue that
            // DrawPhase will add to hand at the START of the next turn.
            // For the VS, we'll roll once per cycled card and pick a random card
            // from the current deck to copy (approximation — the real logic would
            // need per-card tracking, which is deferred to Epic 12 hook wiring).
            System.Collections.Generic.IReadOnlyList<MoveCardInstance> deckSnapshot = State.Deck.DeckView;
            for (int c = 0; c < cycledCount; c++)
            {
                // Per §4.4.5.1 — 20% proc. Use State.Rng (seeded).
                if (State.Rng.Range(0, 100) < 20)
                {
                    // Pick a random card from the current deck to copy.
                    if (deckSnapshot.Count == 0) continue;
                    MoveCardInstance template = deckSnapshot[State.Rng.Range(0, deckSnapshot.Count)];
                    MoveCardInstance copy = _cardFactory.Create(template.Move, template.Owner, template.IsMasteryMove);
                    State.HiveBadgePendingCopies.Add(copy);
                }
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
            RebuildEnemyIntents();

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

        // Per §4.3.2 / Bug #2 + #5 — classify a single enemy move into the intent
        // kind that matches its effect, so a buff/debuff/status move telegraphs and
        // resolves correctly (a self-buff points at the enemy itself, not the Lead).
        //   • BasePower > 0                         → Attack (Lead-targeted)
        //   • BuffSelfEffectSO (StageChange > 0)    → Buff   (self-targeted)
        //   • DebuffTargetEffectSO                  → Debuff (Lead-targeted)
        //   • StatusRiderEffectSO (!ApplyToSelf)    → Status (Lead-targeted)
        //   • HealEffectSO (HealSelf)               → Stall  (self-targeted)
        //   • otherwise                             → Attack fallback
        private Intent ClassifyEnemyMove(MoveSO m)
        {
            Intent leadAttack = new()
            {
                Kind = IntentKind.Attack,
                Move = m,
                TargetSlot = State.LeadIndex,
                TargetsLead = true, // re-resolves to the current Lead at resolution (§4.3.2 / Pillar 2)
                Reveal = IntentReveal.Witnessed,
            };

            // Damaging moves are always Attacks; their Effects[] riders fire on hit.
            if (m.BasePower > 0) return leadAttack;

            if (m.Effects != null)
            {
                for (int e = 0; e < m.Effects.Count; e++)
                {
                    if (m.Effects[e] is BuffSelfEffectSO buff && buff.StageChange > 0)
                        return new Intent
                        {
                            Kind = IntentKind.Buff,
                            Move = m,
                            TargetSlot = -1,
                            TargetsLead = false, // self-target — UI shows "→ Self"
                            BuffStat = buff.TargetStat,
                            Reveal = IntentReveal.Witnessed,
                        };
                }
                for (int e = 0; e < m.Effects.Count; e++)
                {
                    if (m.Effects[e] is DebuffTargetEffectSO debuff)
                        return new Intent
                        {
                            Kind = IntentKind.Debuff,
                            Move = m,
                            TargetSlot = State.LeadIndex,
                            TargetsLead = true,
                            BuffStat = debuff.TargetStat,
                            Reveal = IntentReveal.Witnessed,
                        };
                }
                for (int e = 0; e < m.Effects.Count; e++)
                {
                    if (m.Effects[e] is StatusRiderEffectSO rider && !rider.ApplyToSelf)
                        return new Intent
                        {
                            Kind = IntentKind.Status,
                            Move = m,
                            TargetSlot = State.LeadIndex,
                            TargetsLead = true,
                            AppliedStatus = rider.StatusToApply,
                            Reveal = IntentReveal.Witnessed,
                        };
                }
                for (int e = 0; e < m.Effects.Count; e++)
                {
                    if (m.Effects[e] is HealEffectSO heal && heal.HealSelf)
                        return new Intent
                        {
                            Kind = IntentKind.Stall,
                            Move = m,
                            TargetSlot = -1,
                            TargetsLead = false,
                            Reveal = IntentReveal.Witnessed,
                        };
                }
            }

            // 0-power move with no recognised support effect — treat as a (weak) Attack.
            return leadAttack;
        }

        // Builds the candidate intents this enemy could declare this turn,
        // then picks one via §4.3.3 scoring. Each move is classified by its
        // BasePower + Effects[] into the matching IntentKind so the AI can
        // telegraph and execute self-buffs (Harden), player debuffs (Growl),
        // and status moves — not just damage. (Cleave/Backstrike still require
        // move-level metadata not yet authored on MoveSO.)
        private Intent BuildIntentForEnemy(PokemonInstance enemy)
        {
            List<Intent> candidates = new();
            for (int i = 0; i < enemy.CurrentMoves.Count; i++)
            {
                MoveSO m = enemy.CurrentMoves[i];
                if (m == null) continue;
                candidates.Add(ClassifyEnemyMove(m));
            }
            if (enemy.MasteryMove != null)
                candidates.Add(ClassifyEnemyMove(enemy.MasteryMove));

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

        // Per Bug #1 — rebuild intents for the current EnemyTeam. Factored out
        // from IntentPhase so TryInjectReinforcements can refresh intents mid-turn
        // when reinforcements spawn (UI display only; these intents do NOT fire
        // the same turn per §7.4 constraint).
        private void RebuildEnemyIntents()
        {
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
                    return TryPlayConsumable(action.CardIndex, action.TargetPlayerSlot);
                case PlayerActionKind.ManualSwap:
                    return TryManualSwap(action.SwapToBenchSlot);
                default:
                    return false;
            }
        }

        // Per Epic 5 Task 5.4.1 — delegated to CardPlayService. The service
        // owns validation, AP spend, discount apply/consume, SF/SB position
        // changes, and the post-play combat-end short-circuit (§3.2.4).
        //
        // Per §3.2.6 (OPEN) — if a Breather is active, decrement the allowed
        // action count on successful card play so only ONE action is permitted.
        private bool TryPlaySkillCard(int handIndex, int enemySlot)
        {
            // Per §3.2.6 (OPEN) — capture the breather state BEFORE the play.
            // The play itself may TRIGGER reinforcements (KO the last enemy →
            // TryInjectReinforcements sets BreatherPending), so only a play made
            // while the breather was ALREADY pending counts against the allowance —
            // otherwise the triggering kill would instantly consume its own breather.
            bool breatherActiveBefore = State.BreatherPending;
            bool success = _playService.Play(handIndex, enemySlot);
            if (success && breatherActiveBefore && State.BreatherPending && State.BreatherActionsAllowed > 0)
            {
                State.BreatherActionsAllowed--;
                if (State.BreatherActionsAllowed <= 0) EndBreather();
            }
            return success;
        }

        private bool TryPlayConsumable(int handIndex, int targetPlayerSlot)
        {
            if (handIndex < 0 || handIndex >= State.ConsumableHand.Count) return true;
            ConsumableSO c = State.ConsumableHand[handIndex];
            if (c == null) return true;

            // Per §3.5 — reject if unaffordable; no state change (player can pick another action).
            if (c.APCost > State.CurrentAP) return true;

            // Per §3.2.6 (OPEN) — capture breather state before the play (see TryPlaySkillCard).
            bool breatherActiveBefore = State.BreatherPending;

            DispatchConsumableEffect(c, targetPlayerSlot);
            State.CurrentAP -= c.APCost; // §3.5 — AP consumed on play

            State.ConsumableHand.RemoveAt(handIndex);
            State.Consumables.MarkUsed(c); // §8.2.1 — restored to inventory at combat end

            // Per §3.2.6 (OPEN) — decrement breather action count on successful consumable play.
            if (breatherActiveBefore && State.BreatherPending && State.BreatherActionsAllowed > 0)
            {
                State.BreatherActionsAllowed--;
                if (State.BreatherActionsAllowed <= 0) EndBreather();
            }

            return true;
        }

        // Per §8.2 + Epic 12 Task 12.1 — full VS consumable dispatch. Targeted effects (Heal/Cure/
        // StatBoost) resolve a player Pokémon (TargetPlayerSlot, else the Lead); Ether grants AP this
        // turn; Pokéball targets the wild in slot 0. Revive/CritBoost/IntentReveal are post-VS (§8.8).
        private void DispatchConsumableEffect(ConsumableSO consumable, int targetPlayerSlot)
        {
            if (consumable?.Effect == null) return;
            switch (consumable.Effect)
            {
                case CatchConsumableEffectSO catchEffect:
                    DispatchCatch(catchEffect);
                    break;

                case HealConsumableEffectSO heal:
                {
                    PokemonInstance t = ResolveConsumableTarget(targetPlayerSlot);
                    if (t == null || t.CurrentHP <= 0) return; // §2.4.3 — Potions never revive
                    int effMax = EffectiveMaxHpFor(t);
                    int healAmount = heal.RestoreToFull ? effMax : heal.FlatHealAmount;
                    // §8.3.3 Berry Pouch — healing consumables restore +20% (not applied to full-restore).
                    if (!heal.RestoreToFull)
                        healAmount = RelicResolver.ApplyHealBonus(healAmount, State.ActiveRelics, State.Config);
                    t.CurrentHP = Mathf.Min(effMax, t.CurrentHP + healAmount);
                    break;
                }

                case StatusCureConsumableEffectSO cure:
                {
                    PokemonInstance t = ResolveConsumableTarget(targetPlayerSlot);
                    if (t == null) return;
                    if (cure.CureAll) StatusEffectManager.CureAll(t);
                    else StatusEffectManager.Cure(t, cure.CuresStatus);
                    break;
                }

                case StatBoostConsumableEffectSO boost:
                {
                    PokemonInstance t = ResolveConsumableTarget(targetPlayerSlot);
                    if (t == null) return;
                    StatStageManager.Modify(t, boost.TargetStat, boost.StageChange);
                    break;
                }

                case APGrantConsumableEffectSO ap:
                    State.CurrentAP += ap.APGranted; // §8.2.4 Ether — this turn
                    break;
            }
        }

        // §8.2 — resolve the player Pokémon a targeted consumable affects (explicit slot, else Lead).
        private PokemonInstance ResolveConsumableTarget(int slot)
        {
            if (slot >= 0 && slot < State.PlayerTeam.Count && State.PlayerTeam[slot] != null)
                return State.PlayerTeam[slot];
            return ResolveLead();
        }

        // §6.2 — Trauma-aware heal ceiling (raw MaxHP when no economy, e.g. tests).
        private int EffectiveMaxHpFor(PokemonInstance p) =>
            State.Economy != null ? PokemonVitals.EffectiveMaxHP(p, State.Economy) : PokemonVitals.MaxHP(p);

        // Per Epic 8 Task 8.1.4 — Pokéball: the wild occupies enemy slot 0; on a successful catch set
        // CaughtTarget + clear EnemyTeam so the IsAllFainted Victory path fires. Failed/non-wild = no-op.
        private void DispatchCatch(CatchConsumableEffectSO catchEffect)
        {
            PokemonInstance wild = ResolveEnemySlot(0);
            if (wild == null || !WildCatchResolver.IsCatchable(wild, catchEffect)) return;
            State.CaughtTarget = wild;
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
            // Per §7.4 (OPEN — VS override per playtest 2026-06-02 / R4-1) —
            // reinforcements NOW ACT the turn they spawn (the +1 AP "breather"
            // and wave-telegraph grants the player reaction time; the old skip
            // was overly punishing). The §7.4 "new entrants don't act this turn"
            // rule is REVERSED for the VS; breather suffices. §3.2.6 (OPEN).
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
                case IntentKind.Stall:
                    // Per §3.2.4 / Bug #5 — self-targeted setup. BuffSelf/Heal
                    // riders route to the enemy itself (no player target).
                    ApplyEnemyMoveEffects(enemy, intent.Move, null);
                    break;
                case IntentKind.Debuff:
                {
                    // Per §3.2.4 / Bug #2 — lowers a stat of WHOEVER is the Lead
                    // now (§4.3.2 / Pillar 2). DebuffTarget riders route to them.
                    PokemonInstance dt = IntentTargeting.ResolveSlotOccupant(
                        intent.EffectiveTargetSlot(State.LeadIndex), State.PlayerTeam);
                    ApplyEnemyMoveEffects(enemy, intent.Move, dt);
                    break;
                }
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

        // Per §3.2.4 / Bug #2 + #5 — enemy-side mirror of CardPlayService.ResolveEffects:
        // applies a (non-damage) move's secondary effects from the ENEMY's perspective.
        //   • BuffSelf   → enemy (raises its own stat)
        //   • DebuffTarget / StatusRider(!ApplyToSelf) → playerTarget (the Lead occupant)
        //   • Heal       → enemy if HealSelf, else playerTarget
        // playerTarget may be null for pure self-buff/heal moves.
        private void ApplyEnemyMoveEffects(PokemonInstance enemy, MoveSO move, PokemonInstance playerTarget)
        {
            if (move == null || move.Effects == null) return;

            for (int i = 0; i < move.Effects.Count; i++)
            {
                MoveEffectSO effect = move.Effects[i];
                if (effect == null) continue;

                if (effect is BuffSelfEffectSO buff)
                {
                    if (enemy != null && enemy.CurrentHP > 0)
                    {
                        StatStageManager.Modify(enemy, buff.TargetStat, buff.StageChange);
                        string sign = buff.StageChange >= 0 ? "+" : "";
                        State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.EnemyAction,
                            $"{enemy.Species?.DisplayName} {buff.TargetStat} {sign}{buff.StageChange}"));
                    }
                }
                else if (effect is DebuffTargetEffectSO debuff)
                {
                    if (playerTarget != null && playerTarget.CurrentHP > 0)
                    {
                        StatStageManager.Modify(playerTarget, debuff.TargetStat, debuff.StageChange);
                        string sign = debuff.StageChange >= 0 ? "+" : "";
                        State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.EnemyAction,
                            $"{playerTarget.Species?.DisplayName} {debuff.TargetStat} {sign}{debuff.StageChange}"));
                    }
                }
                else if (effect is StatusRiderEffectSO rider)
                {
                    PokemonInstance statusTarget = rider.ApplyToSelf ? enemy : playerTarget;
                    if (statusTarget != null && statusTarget.CurrentHP > 0
                        && State.Rng != null && State.Rng.Range01() < rider.ApplicationChance)
                    {
                        StatusEffectManager.TryApply(statusTarget, rider.StatusToApply, State.Config);
                        State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.EnemyAction,
                            $"{statusTarget.Species?.DisplayName} {rider.StatusToApply} applied"));
                    }
                }
                else if (effect is HealEffectSO heal)
                {
                    PokemonInstance healTarget = heal.HealSelf ? enemy : playerTarget;
                    if (healTarget != null && healTarget.CurrentHP > 0)
                    {
                        int hpBefore = healTarget.CurrentHP;
                        int effectiveMax = State.Economy != null
                            ? PokemonVitals.EffectiveMaxHP(healTarget, State.Economy)
                            : PokemonVitals.MaxHP(healTarget);
                        int healAmount = heal.FlatHealAmount;
                        if (heal.PercentageOfMaxHP > 0)
                            healAmount += Mathf.FloorToInt(effectiveMax * heal.PercentageOfMaxHP);
                        healTarget.CurrentHP = Mathf.Min(effectiveMax, healTarget.CurrentHP + healAmount);
                        int actualHeal = healTarget.CurrentHP - hpBefore;
                        if (actualHeal > 0)
                            State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.EnemyAction,
                                $"{healTarget.Species?.DisplayName} healed {actualHeal} HP"));
                    }
                }
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
            // Per §4.4.5.1 — Normal Badge boosts only the PLAYER's Pokémon. Pass the player's badges to
            // the attacker side only when the attacker is player-owned, and to the target side only when
            // the target is player-owned — so an enemy's Atk/Def is never buffed by a player badge.
            var atkBadges = State.PlayerTeam != null && State.PlayerTeam.Contains(attacker) ? State.ActiveBadges : null;
            var defBadges = State.PlayerTeam != null && State.PlayerTeam.Contains(target) ? State.ActiveBadges : null;
            DamageBreakdown dmg = DamageCalculator.Compute(ctx, atkBadges, defBadges);

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

            // Per §8.3.3 + Task 12.3 — held-relic outgoing multiplier (player attackers only; relics are
            // player run-state). Brave Charm / Soothe Bell.
            float relicOutMul = State.PlayerTeam.Contains(attacker)
                ? RelicResolver.OutgoingDamageMultiplier(attacker, State.ActiveRelics, State.Config) : 1f;

            // Per §8.4.2 + Task 12.6 — the wearer's Held Item type-boost (Charcoal/Mystic Water/etc.).
            float heldItemMul = HeldItemResolver.OutgoingDamageMultiplier(attacker, move);

            int final = Mathf.FloorToInt(dmg.Final * fieldMul * freezeFireMul * playerAuraMul * abilityOutMul * relicOutMul * heldItemMul);
            // Per §4.1.1 + R3-4 — 0-damage floor ONLY for real attacks (non-immune).
            // Pure status/buff/debuff moves (BasePower 0) deal 0, not 1.
            if (final <= 0)
            {
                if (dmg.TypeEffectiveness == 0.0) final = 0; // immune → 0
                else if (move.BasePower <= 0) final = 0;    // 0-power → 0 (R3-4)
                else final = 1;                              // non-immune attack → floor to 1
            }

            // Per §5.5.3.3 — Levitate: Ground-type moves do nothing to a Levitate target.
            if (AbilityResolver.IsImmuneTo(target, move)) final = 0;

            // §8.3.3 Smoke Ball — the FIRST enemy attack each combat deals −20% (player run-state relic).
            // VS simplification: per-combat (GDD §8.3.3 says first combat per Region) — flagged.
            if (final > 0 && !State.SmokeBallTriggeredThisCombat
                && State.PlayerTeam.Contains(target) && !State.PlayerTeam.Contains(attacker)
                && RelicResolver.Holds(State.ActiveRelics, "smoke_ball"))
            {
                final = Mathf.FloorToInt(final * State.Config.SmokeBallDamageMultiplier);
                State.SmokeBallTriggeredThisCombat = true;
            }

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

            // Per R4-4 — log damage events with actual numbers.
            if (final > 0)
            {
                bool isPlayerAttack = State.PlayerTeam.Contains(attacker);
                CombatLogCategory cat = isPlayerAttack
                    ? CombatLogCategory.PlayerAction : CombatLogCategory.EnemyAction;
                State.CombatLog.Add(new CombatLogEntry(cat,
                    $"{attacker.Species?.DisplayName} used {move.DisplayName} → {target.Species?.DisplayName} took {final} dmg"));
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

        // Per §4.4.5.1 — true iff the player holds the named badge (case-sensitive id).
        private bool HoldsBadge(System.Collections.Generic.IReadOnlyList<BadgeSO> badges, string badgeId)
        {
            if (badges == null) return false;
            for (int i = 0; i < badges.Count; i++)
                if (badges[i] != null && badges[i].BadgeId == badgeId)
                    return true;
            return false;
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
                if (dot > 0)
                {
                    p.CurrentHP = Mathf.Max(0, p.CurrentHP - dot);
                    State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.TurnEvent,
                        $"{p.Species?.DisplayName} {p.PrimaryStatus} → {dot} dmg"));
                }
                // §8.4.4 Leftovers — end-of-Resolution regen (after DoT; never revives a fainted wearer).
                int regen = HeldItemResolver.LeftoversRegen(p, State.Economy);
                if (regen > 0)
                {
                    p.CurrentHP = Mathf.Min(EffectiveMaxHpFor(p), p.CurrentHP + regen);
                    State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.TurnEvent,
                        $"{p.Species?.DisplayName} Leftovers → +{regen} HP"));
                }
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
                // Per §4.8.5 / §6.2.2 — +1 Trauma stack at moment of faint,
                // but ONLY once per faint event (Bug #9 fix: guard with HashSet).
                if (_traumaApplied.Add(p))
                {
                    FaintResolver.ApplyTraumaOnFaint(p);
                    State.CombatLog.Add(new CombatLogEntry(
                        CombatLogCategory.TurnEvent, $"{p.Species?.DisplayName} fainted"));
                }
            }

            // Per §6.9 / Task 11.8.2 — record each defeated enemy in the Bestiary exactly once.
            if (State.Bestiary != null)
            {
                for (int i = 0; i < State.EnemyTeam.Count; i++)
                {
                    PokemonInstance e = State.EnemyTeam[i];
                    if (e == null || e.CurrentHP > 0 || e.Species == null) continue;
                    if (_recordedEnemyKills.Add(e))
                    {
                        State.Bestiary.RecordKill(e.Species.SpeciesId, e.Species.WildRarity);

                        // Per §4.3.9.1 — Pokédex tier-reward ladder. Veteran → Shiny variant unlock;
                        // Master → Mastery Move unlock. Both permanent (the Pokédex grind payoff).
                        if (BestiaryShinyUnlock.TryUnlockShiny(State.Bestiary, State.Meta, e.Species))
                            State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.TurnEvent,
                                $"✨ {e.Species.DisplayName} can now be Shiny!"));
                        if (BestiaryMasteryUnlock.TryUnlockMastery(State.Bestiary, State.Meta, e.Species))
                            State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.TurnEvent,
                                $"Mastered {e.Species.DisplayName}! Mastery Move unlocked."));
                    }
                }
            }

            // Per §3.3.5 + Bug #10 — if Lead is fainted, pause and expose candidates for UI modal picker.
            // The UI will call ApplyLeadReplacement(index) to resume, or the headless agent path below
            // resolves it immediately (backward-compat for unit tests that don't drive the modal).
            PokemonInstance lead = ResolveLead();
            if (lead == null || lead.CurrentHP <= 0)
            {
                List<PokemonInstance> candidates = FaintResolver.EligibleLeadReplacements(
                    State.PlayerTeam, lead);
                if (candidates.Count > 0)
                {
                    State.PendingLeadReplacementCandidates = candidates;
                    // Headless agent path (unit tests / AI). If the agent is the UIPlayerAgent stub
                    // that returns state.LeadIndex, this would soft-lock; but production UI calls
                    // ApplyLeadReplacement(idx) directly from the modal and skips this branch.
                    if (_agent != null)
                    {
                        int newIdx = _agent.PickLeadReplacement(State, candidates);
                        ApplyLeadReplacement(newIdx);
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

        // Per §3.3.5 + Bug #10 — UI-driven Lead replacement hook. Called from the modal picker after the
        // player chooses a replacement. The index must be valid and the candidate must be non-fainted;
        // any invalid input is logged and ignored (controller state stays paused). Clears the pending
        // candidates and applies Lead Aura entry effects per §5.5.3.5 (Intimidate, etc.).
        public void ApplyLeadReplacement(int newLeadIndex)
        {
            if (State.PendingLeadReplacementCandidates == null || State.PendingLeadReplacementCandidates.Count == 0)
            {
                Debug.LogWarning($"[CombatController] ApplyLeadReplacement({newLeadIndex}) called but no pending replacement.");
                return;
            }
            if (newLeadIndex < 0 || newLeadIndex >= State.PlayerTeam.Count)
            {
                Debug.LogWarning($"[CombatController] ApplyLeadReplacement({newLeadIndex}) out of bounds.");
                return;
            }
            PokemonInstance replacement = State.PlayerTeam[newLeadIndex];
            if (replacement == null || replacement.CurrentHP <= 0)
            {
                Debug.LogWarning($"[CombatController] ApplyLeadReplacement({newLeadIndex}) candidate is fainted/null.");
                return;
            }
            State.LeadIndex = newLeadIndex;
            State.PendingLeadReplacementCandidates = null; // resume
            AbilityResolver.ApplyLeadEntryEffects(State); // §5.5.3.5 Intimidate
        }

        // Per Epic 8 Task 8.2 — returns true if reinforcements landed (so the
        // caller knows to skip the Outcome.Victory branch). Replaces team
        // contents in-place rather than swapping the list reference so any
        // downstream code holding the IList<PokemonInstance> stays valid.
        //
        // Per §7.4 + Bug #1 — refreshes intents for the new team so the UI
        // displays the reinforcements' intents immediately (not stale dead-enemy
        // intents). Per §8.2: new entrants do NOT act this turn (they only
        // telegraph for next turn).
        //
        // Per §3.2.6 (OPEN) + R2-5 Task 5 — when reinforcements arrive mid-combat
        // AND combat continues, grant a one-time Breather: +BreatherBonusAP and
        // permission for ONE action (card play OR manual swap). UI drives the
        // breather flow via BreatherPending flag. If the player has no valid
        // action (empty hand + no eligible swap target), auto-clear the breather.
        private bool TryInjectReinforcements()
        {
            if (_reinforcements == null) return false;
            List<PokemonInstance> next = _reinforcements.RequestReinforcements(State);
            if (next == null || next.Count == 0) return false;
            State.EnemyTeam.Clear();
            for (int i = 0; i < next.Count; i++) State.EnemyTeam.Add(next[i]);
            RecordEnemiesSeen(); // §6.9 — Pokédex discovery for the new wave

            // Per §7.4 (OPEN — VS override per playtest R4-1) — rebuild intents
            // for the new team. The reinforcements ACT the turn they spawn (the
            // +1 AP breather + wave telegraph grants reaction time per §3.2.6).
            RebuildEnemyIntents();

            // Per §3.2.6 (OPEN) — grant Breather: +BreatherBonusAP and set flag.
            if (State.Config != null && State.Config.BreatherBonusAP > 0)
            {
                State.CurrentAP += State.Config.BreatherBonusAP;
                // Clamp to MaxAPPerTurn to prevent AP overflow exploits.
                int maxAP = State.Config.MaxAPPerTurn;
                if (State.CurrentAP > maxAP) State.CurrentAP = maxAP;
                State.CombatLog.Add(new CombatLogEntry(CombatLogCategory.TurnEvent,
                    $"Reinforcements! Breather: +{State.Config.BreatherBonusAP} AP"));
            }

            State.BreatherPending = true;
            State.BreatherActionsAllowed = 1;

            // Auto-skip breather if the player has no valid action: empty hand
            // AND no eligible manual swap target (non-fainted non-Frozen bench).
            if (ShouldAutoSkipBreather())
            {
                State.BreatherPending = false;
                State.BreatherActionsAllowed = 0;
            }

            return true;
        }

        // Per §3.2.6 (OPEN) — auto-skip the breather when the player has no valid
        // action: empty hand AND no eligible manual swap target. A full-party
        // Freeze (Lead + all bench Frozen) qualifies as "no swap target".
        private bool ShouldAutoSkipBreather()
        {
            // Check for playable cards in hand.
            if (State.SkillHand != null && State.SkillHand.Count > 0)
            {
                for (int i = 0; i < State.SkillHand.Count; i++)
                {
                    MoveCardInstance c = State.SkillHand[i];
                    if (c == null || c.Owner == null) continue;
                    // If the owner is in the team and not fainted, there's a
                    // potentially-playable card (AP cost check is expensive; defer
                    // to CardPlayValidator when the player tries to play it).
                    if (State.PlayerTeam.Contains(c.Owner) && c.Owner.CurrentHP > 0)
                        return false;
                }
            }
            if (State.ConsumableHand != null && State.ConsumableHand.Count > 0)
            {
                // Consumables are typically free or low-cost; if any exist, there's
                // a valid action. Full cost check is overkill; presence suffices.
                for (int i = 0; i < State.ConsumableHand.Count; i++)
                    if (State.ConsumableHand[i] != null) return false;
            }

            // Check for eligible manual swap target: non-fainted, non-Frozen bench.
            PokemonInstance lead = ResolveLead();
            if (lead == null) return true; // no valid Lead → no swap possible
            for (int i = 0; i < State.PlayerTeam.Count; i++)
            {
                if (i == State.LeadIndex) continue; // Lead can't swap with itself
                PokemonInstance p = State.PlayerTeam[i];
                if (p == null || p.CurrentHP <= 0) continue;
                if (StatusModifiers.IsPositionLocked(p.PrimaryStatus)) continue; // Frozen
                return false; // found an eligible swap target
            }

            return true; // no valid action
        }

        // Per §3.2.6 (OPEN) + R2-5 Task 5 — UI-driven hook to clear the breather
        // flag and resume normal flow. Called by the UI when the player confirms
        // "End Breather" (typically after the ONE allowed action is taken, or
        // immediately if the player chooses to pass). Headless path (tests/AI)
        // may call this explicitly, or auto-skip will clear it in TryInjectReinforcements.
        public void EndBreather()
        {
            if (!State.BreatherPending) return; // idempotent no-op
            State.BreatherPending = false;
            State.BreatherActionsAllowed = 0;
        }

        // Per §7.4.4 (OPEN) — UI passthrough: preview the next reinforcement wave
        // for the wave-queue telegraph panel. Empty when there's no provider or no
        // wave remaining. Non-consuming (delegates to the provider's PeekNextWave).
        public IReadOnlyList<ReinforcementPreview> PeekNextWave()
            => _reinforcements?.PeekNextWave() ?? System.Array.Empty<ReinforcementPreview>();

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
