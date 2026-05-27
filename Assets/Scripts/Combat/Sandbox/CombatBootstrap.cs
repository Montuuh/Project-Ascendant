using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat.Sandbox
{
    // Per Epic 4 Task 4.1.9 — Play Mode smoke-test driver.
    //
    // This is a THROWAWAY sandbox MonoBehaviour. Epic 13 builds the real
    // combat scene with UI Toolkit, prefabs, and a proper Bootstrap that
    // loads encounter data from Addressables. This file exists so the
    // user can press Play and see Epic 4's combat backend run end-to-end.
    //
    // Responsibilities:
    //   • Build a 1v1 player-vs-enemy setup with code-authored mock data
    //     (no asset references — fewer scene-modify risks).
    //   • Construct CombatController + HUDAgent.
    //   • Auto-step Start → Draw → Intent before handing off to HUD.
    //   • Expose AdvanceTurn() so the HUD can drive Resolution → TurnEnd →
    //     next Draw cycle from the End Turn button.
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [Header("Encounter Seed")]
        [Tooltip("RNG seed for deterministic playthroughs.")]
        public uint Seed = 0xC0FFEE01u;

        [Header("Config")]
        [Tooltip("Optional BattleConfigSO asset. If null, a transient config is " +
                 "instantiated with default values.")]
        public BattleConfigSO ConfigAsset;

        // Set up in Awake — lifetime tied to this Bootstrap (and the scene).
        public CombatController Controller { get; private set; }
        public HUDAgent Agent { get; private set; }

        // Transient SOs we own and must clean up on destroy.
        private readonly List<Object> _disposables = new();
        private PokemonInstance _player;
        private PokemonInstance _enemy;

        private void Awake()
        {
            BattleConfigSO config = ConfigAsset != null ? ConfigAsset : CreateDefaultConfig();

            // Player: Charmander-shaped mock — 50/50/40 with a 50-power Fire move.
            PokemonSpeciesSO playerSp = MakeSpecies("Charmander", 50, 50, 40, PokemonType.Fire);
            MoveSO ember = MakeMove("Ember", PokemonType.Fire, power: 40, ap: 1);
            MoveSO scratch = MakeMove("Scratch", PokemonType.Normal, power: 35, ap: 1);
            MoveSO claw = MakeMove("Metal Claw", PokemonType.Steel, power: 45, ap: 2);
            _player = MakeMon(playerSp, new[] { ember, scratch, claw });

            // Enemy: Squirtle — Water 50/40/45 with a 40-power Water move.
            PokemonSpeciesSO enemySp = MakeSpecies("Squirtle", 50, 40, 45, PokemonType.Water);
            MoveSO bubble = MakeMove("Bubble", PokemonType.Water, power: 35, ap: 1);
            MoveSO tackle = MakeMove("Tackle", PokemonType.Normal, power: 30, ap: 1);
            _enemy = MakeMon(enemySp, new[] { bubble, tackle });

            Agent = new HUDAgent();

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { _player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { _enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = config,
                Rng = new GameRNG(Seed),
            };

            Controller = new CombatController(setup, Agent);
            Controller.Start();
            BeginPlayerTurn();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        // Step the controller into the player's turn: Draw + Intent + leave it
        // in ActionPhase awaiting input.
        public void BeginPlayerTurn()
        {
            if (Controller.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            Controller.DrawPhase();
            Controller.IntentPhase();
            Controller.State.CurrentPhase = CombatController.Phase.ActionPhase;
        }

        // Called by the HUD on End Turn. Runs Resolution → TurnEnd → outcome
        // check → next Draw + Intent (if combat continues).
        public void AdvanceTurn()
        {
            if (Controller.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            Controller.ResolutionPhase();
            Controller.TurnEnd();
            if (Controller.State.Outcome != CombatController.CombatOutcome.InProgress)
            {
                Controller.CombatEnd();
                return;
            }
            BeginPlayerTurn();
        }

        // Called by the HUD when the player clicks a card in hand.
        public void PlayCard(int handIndex)
        {
            if (Controller.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            Controller.ExecuteAction(PlayerAction.PlaySkill(handIndex, enemySlot: 0));
        }

        public void RestartCombat()
        {
            // Simple reset: re-run Awake's setup. Caller destroys + recreates
            // the GameObject in production; here we just reseed.
            OnDestroy();
            Awake();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private BattleConfigSO CreateDefaultConfig()
        {
            BattleConfigSO c = ScriptableObject.CreateInstance<BattleConfigSO>();
            // BattleConfigSO has reasonable defaults set in its field initialisers,
            // but pin the AI scoring fields explicitly (they may be 0 on a fresh
            // instance depending on Unity's serialization quirks).
            c.Divisor = 50;
            c.StabMultiplier = 1.5f;
            c.CritMultiplier = 1.5f;
            c.MeleeModifier = 1.0f;
            c.RangedModifier = 0.75f;
            c.BaseAPPerTurn = 3;
            c.MaxAPPerTurn = 6;
            c.BaseSkillCardsPerTurn = 5;
            c.BaseConsumableCardsPerTurn = 2;
            c.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            c.BurnDoTDivisor = 16;
            c.BurnAttackMultiplier = 0.75f;
            c.PoisonDoTDivisor = 16;
            c.PoisonDefenseMultiplier = 0.85f;
            c.ParalysisAPCostBonus = 1;
            c.ParalysisDuration = 3;
            c.SleepDuration = 1;
            c.FreezeDuration = 1;
            c.FreezeFireDamageMultiplier = 1.5f;
            c.ConfusionDuration = 3;
            c.DefaultUtilityWeight = 50;
            c.LowTargetHPMultiplier = 2.0f;
            c.LowTargetHPThreshold = 0.30f;
            c.AggressiveSelfMultiplier = 1.5f;
            c.LowSelfHPThreshold = 0.40f;
            c.SetupSelfMultiplier = 1.5f;
            c.HighSelfHPThreshold = 0.70f;
            c.RandomnessFloorChance = 0.125f;
            c.BossCounterIntelTopPenalty = 0.7f;
            c.SunnyDayFireMultiplier = 1.5f;
            c.SunnyDayWaterMultiplier = 0.5f;
            c.RainDanceWaterMultiplier = 1.5f;
            c.RainDanceFireMultiplier = 0.5f;
            c.ElectricTerrainElectricMultiplier = 1.3f;
            _disposables.Add(c);
            return c;
        }

        private PokemonSpeciesSO MakeSpecies(string nickname, int hp, int atk, int def, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.name = nickname;
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(s);
            return s;
        }

        private MoveSO MakeMove(string moveName, PokemonType type, int power, int ap)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = moveName;
            m.DisplayName = moveName;
            m.MoveId = moveName.ToLowerInvariant();
            m.Type = type;
            m.BasePower = power;
            m.APCost = ap;
            m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private static PokemonInstance MakeMon(PokemonSpeciesSO sp, MoveSO[] moves)
        {
            PokemonInstance p = new()
            {
                Species = sp,
                Level = 1,
                CurrentHP = sp.BaseStats.BaseHP,
            };
            for (int i = 0; i < moves.Length; i++) p.CurrentMoves.Add(moves[i]);
            return p;
        }
    }

    // HUDAgent: IPlayerAgent that lets external code submit actions. The HUD
    // calls Controller.ExecuteAction directly, so DecideAction is only invoked
    // if some code path enters ActionPhase via the looping API — which the
    // sandbox doesn't. Implementations of both methods are safe defaults.
    public sealed class HUDAgent : IPlayerAgent
    {
        public PlayerAction DecideAction(CombatController.CombatState state)
        {
            // The sandbox never reaches this code path. Default: end turn.
            return PlayerAction.End();
        }

        public int PickLeadReplacement(CombatController.CombatState state,
                                       System.Collections.Generic.IReadOnlyList<PokemonInstance> candidates)
        {
            // 1v1 sandbox: no bench to swap to. Production HUD will surface
            // a modal letting the player pick.
            return state.LeadIndex;
        }
    }
}
