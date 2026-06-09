using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Map
{
    // Per §7.14 (CL-009) — standalone Dojo map node. Teaches off-learnset moves and/or abilities
    // to a chosen Pokémon for Poké Dollars. Appears ~1 per region.
    //
    //   • TeachMove:    adds an entry from TutorLearnset to the Pokémon's Learned Move Pool (§5.10.1).
    //                   Cost: EconomyConfigSO.DojoMoveCost. Repeatable within one visit.
    //   • TeachAbility: sets (or swaps) the Pokémon's single passive ability (§5.12.3 / CL-008).
    //                   Cost: EconomyConfigSO.DojoAbilityCost. Any entry in species.AvailableAbilities.
    //
    // Both services are available per-visit; the player may teach multiple things if affordable.
    // The Dojo is the primary ₽ sink and the key deliberate-sculpt stop (Pillar 3 / §7.14).
    // Full UI: CL-023. Minimal stub: NodePanelUI.RenderDojo.
    public sealed class DojoNodeController : NodeController
    {
        private readonly Box _box;
        private readonly EconomyConfigSO _economy;

        public DojoNodeController(MapNode node, RunStateSO runState, Box box, EconomyConfigSO economy)
            : base(node, runState)
        {
            _box     = box ?? throw new ArgumentNullException(nameof(box));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        // Utility node — no combat. Services are invoked interactively after Enter().
        protected override void OnEnter() { }

        // Per §7.14 — Poké Dollar cost to teach a move (flat per move, placeholder per §7.14 note).
        public int MoveCost() => _economy.DojoMoveCost;

        // Per §7.14 — Poké Dollar cost to teach (or swap) an ability.
        public int AbilityCost() => _economy.DojoAbilityCost;

        // Per §7.14 / §5.4.2 — all moves in the species' TutorLearnset that the Pokémon has not yet
        // learned. Moves already in the pool are excluded (§5.10.1 dedup). No cap — the Dojo shows
        // the full available pool (unlike the old Center which capped at 3).
        public List<MoveSO> OfferMoves(PokemonInstance pokemon)
        {
            List<MoveSO> offer = new();
            if (pokemon?.Species?.TutorLearnset == null) return offer;
            List<MoveSO> tutor = pokemon.Species.TutorLearnset;
            for (int i = 0; i < tutor.Count; i++)
            {
                MoveSO m = tutor[i];
                if (m == null) continue;
                if (pokemon.LearnedMoves.Contains(m)) continue; // already in pool (§5.10.1)
                offer.Add(m);
            }
            return offer;
        }

        // Per §5.12.3 (CL-008) — abilities available for this Pokémon at the Dojo: all entries in
        // species.AvailableAbilities. An ability already equipped is still listed (player can swap).
        public List<AbilitySO> OfferAbilities(PokemonInstance pokemon)
        {
            List<AbilitySO> offer = new();
            if (pokemon?.Species?.AvailableAbilities == null) return offer;
            List<AbilitySO> pool = pokemon.Species.AvailableAbilities;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null) offer.Add(pool[i]);
            return offer;
        }

        // Per §7.14 / §5.10.1 — teach an off-learnset move: add to the Learned Move Pool and deduct
        // the cost. Returns false (no change) if the move is not in the tutor pool, already learned,
        // or the player cannot afford it.
        public bool TeachMove(PokemonInstance pokemon, MoveSO move)
        {
            if (pokemon == null || move == null) return false;
            if (!IsTutorMove(pokemon, move)) return false;   // not in tutor pool
            if (pokemon.LearnedMoves.Contains(move)) return false; // already learned (§5.10.1 dedup)
            int cost = MoveCost();
            if (RunState.PokeDollars < cost) return false;   // insufficient funds

            RunState.PokeDollars -= cost;
            MoveLoadoutService.AddToPool(pokemon, move);
            return true;
        }

        // Per §5.12.3 (CL-008) — teach an ability: set (or swap) the Pokémon's single passive slot
        // and deduct the cost. Returns false if the ability is not in the species' pool, the Pokémon
        // already has it equipped, or the player cannot afford it.
        public bool TeachAbility(PokemonInstance pokemon, AbilitySO ability)
        {
            if (pokemon == null || ability == null) return false;
            if (!IsAvailableAbility(pokemon, ability)) return false; // not in species pool
            if (pokemon.Ability == ability) return false;            // already equipped
            int cost = AbilityCost();
            if (RunState.PokeDollars < cost) return false;           // insufficient funds

            RunState.PokeDollars -= cost;
            pokemon.Ability = ability;
            return true;
        }

        // Player leaves the Dojo → node resolved.
        public void Leave() => Complete(NodeOutcome.Cleared);

        // ── Private helpers ──────────────────────────────────────────────────

        private static bool IsTutorMove(PokemonInstance pokemon, MoveSO move)
        {
            List<MoveSO> tutor = pokemon.Species?.TutorLearnset;
            if (tutor == null) return false;
            for (int i = 0; i < tutor.Count; i++)
                if (tutor[i] == move) return true;
            return false;
        }

        private static bool IsAvailableAbility(PokemonInstance pokemon, AbilitySO ability)
        {
            List<AbilitySO> pool = pokemon.Species?.AvailableAbilities;
            if (pool == null) return false;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] == ability) return true;
            return false;
        }
    }
}
