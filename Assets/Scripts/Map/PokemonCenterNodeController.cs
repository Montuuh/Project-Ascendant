using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Map
{
    // Per §7.6 + Epic 9 Task 9.5 — the Pokémon Center node (Region-internal, Layer 6). Utility node:
    // fires NO combat. Offers three services over the Box, then the player leaves to the Gym.
    //
    //   • Heal (9.5.2): all Box Pokémon → EffectiveMaxHP (revives fainted, §2.4.2). Free.
    //   • Move Tutor (9.5.3): 3-move offer from a Pokémon's TutorLearnset; learn 1 (replace a
    //     CurrentMoves slot — Mastery is immutable §4.3.9.2). Once per visit.
    //   • Therapy (9.5.4): −1 Trauma stack for TherapyBaseCost × (1 + stacks) (§6.2.4). Repeatable
    //     while affordable.
    //
    // Services are invoked interactively (UI = Epic 13); Leave() completes the node (→ MapView).
    //
    // Move-Tutor note: §5.10 specifies an additive Learned Move Pool, but PokemonInstance has only
    // the 4 CurrentMoves slots today — so the Tutor REPLACES a chosen slot (user-confirmed VS model).
    // See BACKLOG gap (§5.10 pool not implemented).
    public sealed class PokemonCenterNodeController : NodeController
    {
        // Per §7.6.1 — Move Tutor presents a curated 3-move offer.
        private const int MOVE_TUTOR_OFFER = 3;

        private readonly Box _box;
        private readonly EconomyConfigSO _economy;

        private bool _tutorUsed;
        public bool TutorUsed => _tutorUsed;

        public PokemonCenterNodeController(MapNode node, RunStateSO runState, Box box, EconomyConfigSO economy)
            : base(node, runState)
        {
            _box     = box ?? throw new ArgumentNullException(nameof(box));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        // Utility node — no combat. Services are called interactively after Enter().
        protected override void OnEnter() { }

        // 9.5.2 / §7.6.1 — full heal: every Box Pokémon to its Trauma-adjusted Max HP (revives fainted)
        // AND cure all status conditions. "Full restore" per §7.6.1 matches the franchise definition
        // (HP + status cure).
        public void Heal()
        {
            for (int i = 0; i < _box.Members.Count; i++)
            {
                PokemonInstance p = _box.Members[i];
                if (p != null)
                {
                    p.CurrentHP = PokemonVitals.EffectiveMaxHP(p, _economy);
                    StatusEffectManager.CureAll(p); // Per §7.6.1 "Full restore" — cure status too
                }
            }
        }

        // 9.5.3 / §5.4.2 — up to MOVE_TUTOR_OFFER moves from the Pokémon's TutorLearnset, excluding
        // moves already in the Learned Move Pool (§5.10 approved 2026-06-02, pending Notion lock).
        // Curated order (deterministic — no RNG).
        public List<MoveSO> OfferTutorMoves(PokemonInstance pokemon)
        {
            List<MoveSO> offer = new();
            if (pokemon == null || pokemon.Species == null || pokemon.Species.TutorLearnset == null)
                return offer;

            List<MoveSO> tutor = pokemon.Species.TutorLearnset;
            for (int i = 0; i < tutor.Count && offer.Count < MOVE_TUTOR_OFFER; i++)
            {
                MoveSO m = tutor[i];
                if (m == null) continue;
                if (pokemon.LearnedMoves.Contains(m)) continue; // already in pool (§5.10)
                offer.Add(m);
            }
            return offer;
        }

        // 9.5.3 / §5.10.1 — learn (add to Learned Move Pool; deduplicates). Once per visit. Mastery
        // (separate slot) untouched per §4.3.9.2. Returns false if the tutor was already used this
        // visit or the args are invalid.
        public bool LearnMove(PokemonInstance pokemon, MoveSO move)
        {
            if (_tutorUsed) return false;
            if (pokemon == null || move == null) return false;
            if (pokemon.LearnedMoves.Contains(move)) return false; // already in pool

            MoveLoadoutService.AddToPool(pokemon, move);
            _tutorUsed = true;
            return true;
        }

        // 9.5.4 / §6.2.4 — cost to remove one Trauma stack from this Pokémon right now.
        public int TherapyCost(PokemonInstance pokemon)
            => _economy.TherapyBaseCost * (1 + (pokemon?.TraumaStacks ?? 0));

        // 9.5.4 — remove 1 Trauma stack if the Pokémon has any and the player can afford it.
        // Repeatable while affordable. Returns false (no change) otherwise.
        public bool Therapy(PokemonInstance pokemon)
        {
            if (pokemon == null || pokemon.TraumaStacks <= 0) return false;
            int cost = TherapyCost(pokemon);
            if (RunState.PokeDollars < cost) return false;

            RunState.PokeDollars -= cost;
            pokemon.TraumaStacks--;
            return true;
        }

        // Player leaves the Center → node resolved.
        public void Leave() => Complete(NodeOutcome.Cleared);
    }
}
