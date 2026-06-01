using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.4 + Epic 8 Task 8.2 — orchestrates a Trainer Battle node.
    //
    // Responsibilities:
    //   • Materialise the archetype's first Pokémon into the EnemyTeam.
    //   • Hand the remaining slots to CombatController as reinforcements,
    //     dequeued one-at-a-time per sequential spawn rule (§7.4).
    //   • On Victory, compute the TrainerRewardBundle from the archetype's
    //     loot tables using the LootRNG stream (§9.7.2).
    //
    // Lifecycle:
    //   var ctrl = new TrainerBattleController(archetype, pokemonFactory,
    //                                          combatRng, lootRng);
    //   var combat = ctrl.BuildCombatSetup(playerTeam, leadIdx, consumables,
    //                                      field, config);
    //   var cc = new CombatController(combat, agent);
    //   var outcome = cc.RunFullCombat();
    //   var reward = ctrl.ResolveReward(outcome);
    //
    // Spawn semantics:
    //   • First Pokémon is built up-front so EnemyTeam is non-empty at Start.
    //   • Subsequent slots are dequeued ONLY when the controller calls
    //     RequestReinforcements (i.e. all current enemies have fainted).
    //   • Returning an empty queue → CombatController sets Outcome.Victory.
    //
    // Reward semantics (VS):
    //   • Trainer XP defaults to 5 per §7.4.2 (Epic 10 consumes; not credited
    //     to MetaProgression here).
    //   • PokeDollars = archetype.BasePokeDollarReward verbatim.
    //   • At most 1 RelicSO from the archetype's RelicLootTable (uniform pick).
    //   • At most 1 ConsumableSO from the archetype's ConsumableLootTable.
    //   • All rolls go through LootRNG so two runs with the same seed produce
    //     the same drop sequence (Pillar: determinism).
    public sealed class TrainerBattleController : IEnemyReinforcementProvider
    {
        private const int DEFAULT_TRAINER_XP = 5; // §7.4.2

        private readonly TrainerArchetypeSO _archetype;
        private readonly PokemonInstanceFactory _pokemonFactory;
        private readonly GameRNG _lootRng;
        private readonly Queue<TrainerPokemonSlot> _remaining = new();

        public TrainerArchetypeSO Archetype => _archetype;

        // Spawn diagnostics. The first Pokémon is materialised inside the
        // constructor so the count starts at 1 even before combat begins.
        public int SpawnsExecuted { get; private set; }
        // Tracks queued-but-not-yet-spawned slots so tests / UI can show
        // "Trainer has N Pokémon left."
        public int RemainingInQueue => _remaining.Count;

        public TrainerBattleController(TrainerArchetypeSO archetype,
                                       PokemonInstanceFactory pokemonFactory,
                                       GameRNG lootRng)
        {
            _archetype = archetype;
            _pokemonFactory = pokemonFactory;
            _lootRng = lootRng;
            if (_archetype == null) return;
            if (_archetype.Composition == null) return;
            for (int i = 0; i < _archetype.Composition.Count; i++)
                _remaining.Enqueue(_archetype.Composition[i]);
        }

        // Builds the CombatSetup pre-populated with the trainer's first
        // Pokémon. Caller passes their own player team / consumables / field.
        // Throws nothing — returns a default-initialised setup if the
        // archetype is empty (caller should null-check Archetype upstream).
        public CombatController.CombatSetup BuildCombatSetup(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> consumableInventory,
            FieldState initialField,
            BattleConfigSO config,
            GameRNG combatRng)
        {
            List<PokemonInstance> enemyTeam = new();
            PokemonInstance first = DequeueAndMaterialiseOne();
            if (first != null) enemyTeam.Add(first);

            return new CombatController.CombatSetup
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = initialLeadIndex,
                EnemyTeam = enemyTeam,
                ConsumableInventory = consumableInventory,
                InitialField = initialField,
                Config = config,
                Rng = combatRng,
                Reinforcements = this,
            };
        }

        // IEnemyReinforcementProvider — called by CombatController when the
        // current enemy team has been wiped. Returns the next Pokémon as a
        // single-element list per sequential-spawn rule. Empty list signals
        // "trainer is out of Pokémon" → CombatController completes Victory.
        public List<PokemonInstance> RequestReinforcements(CombatController.CombatState state)
        {
            List<PokemonInstance> next = new();
            PokemonInstance p = DequeueAndMaterialiseOne();
            if (p != null) next.Add(p);
            return next;
        }

        // Per §7.4.2 — compute the reward bundle. Returns Empty for any
        // outcome other than Victory. Loot rolls are deterministic given
        // the seeded LootRNG. Empty loot tables produce empty drop lists
        // (no entry added; never null).
        public TrainerRewardBundle ResolveReward(CombatController.CombatOutcome outcome)
        {
            TrainerRewardBundle bundle = TrainerRewardBundle.Empty;
            if (outcome != CombatController.CombatOutcome.Victory) return bundle;
            if (_archetype == null) return bundle;

            bundle.TrainerXP = DEFAULT_TRAINER_XP;
            bundle.PokeDollars = _archetype.BasePokeDollarReward;

            // Per §7.4.2 + Task 12.10.1 — ONE weighted drop: 50% Common consumable / 30% Common relic /
            // 20% Uncommon relic (weights authored on the archetype).
            int wC = _archetype.CommonConsumableWeight;
            int wR = _archetype.CommonRelicWeight;
            int wU = _archetype.UncommonRelicWeight;
            int total = wC + wR + wU;
            if (total > 0 && _lootRng != null)
            {
                int roll = _lootRng.Range(0, total);
                if (roll < wC)
                {
                    ConsumableSO c = PickOneUniform(_archetype.ConsumableLootTable, _lootRng);
                    if (c != null) bundle.ConsumableDrops.Add(c);
                }
                else if (roll < wC + wR)
                {
                    RelicSO r = PickOneUniform(FilterByRarity(_archetype.RelicLootTable, RarityTier.Common), _lootRng);
                    if (r != null) bundle.RelicDrops.Add(r);
                }
                else
                {
                    RelicSO r = PickOneUniform(FilterByRarity(_archetype.RelicLootTable, RarityTier.Uncommon), _lootRng);
                    if (r != null) bundle.RelicDrops.Add(r);
                }
            }

            return bundle;
        }

        // ── Internals ────────────────────────────────────────────────────────

        private PokemonInstance DequeueAndMaterialiseOne()
        {
            if (_remaining.Count == 0) return null;
            if (_pokemonFactory == null) return null;
            TrainerPokemonSlot slot = _remaining.Dequeue();
            if (slot.Species == null) return null;
            PokemonInstance inst = _pokemonFactory.Create(slot.Species, slot.Level);
            // Mirror PokemonInstanceFactory contract — populate moves from
            // the species learnset so the AI has candidate intents to score.
            // Factory does not auto-fill CurrentMoves; copy species.BaseLearnset
            // (capped at the §3.7 active-4 slot count) so trainer encounters
            // work without an out-of-band setup call. Mastery move is already
            // wired by the factory from species.MasteryMove.
            if (slot.Species.BaseLearnset != null)
            {
                int max = slot.Species.BaseLearnset.Count < 4 ? slot.Species.BaseLearnset.Count : 4;
                for (int i = 0; i < max; i++)
                {
                    MoveSO m = slot.Species.BaseLearnset[i];
                    if (m != null) inst.CurrentMoves.Add(m);
                }
            }
            SpawnsExecuted++;
            return inst;
        }

        // §7.4.2 — relics of a given rarity from the loot table.
        private static List<RelicSO> FilterByRarity(List<RelicSO> table, RarityTier rarity)
        {
            List<RelicSO> result = new();
            if (table != null)
                for (int i = 0; i < table.Count; i++)
                    if (table[i] != null && table[i].Rarity == rarity) result.Add(table[i]);
            return result;
        }

        private static T PickOneUniform<T>(List<T> table, GameRNG rng) where T : class
        {
            if (table == null || table.Count == 0 || rng == null) return null;
            // Uniform pick — convert to (value, weight=1) tuple for the
            // existing GameRNG.PickWeighted helper. Allocates a transient
            // list of length N; trainer loot tables are tiny (~3 entries).
            List<(T value, float weight)> opts = new(table.Count);
            for (int i = 0; i < table.Count; i++)
            {
                T v = table[i];
                if (v == null) continue;
                opts.Add((v, 1f));
            }
            if (opts.Count == 0) return null;
            return rng.PickWeighted(opts);
        }
    }
}
