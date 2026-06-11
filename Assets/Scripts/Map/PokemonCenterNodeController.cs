using System;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.6 + Epic 9 Task 9.5 — the Pokémon Center node. Utility node; fires NO combat.
    // Per §7.14 / §5.12.4 (CL-009) — Move Tutor service removed; the Dojo node handles it.
    // Remaining services:
    //   • Heal (9.5.2): all Box Pokémon → EffectiveMaxHP (revives fainted, §2.4.2). Free.
    //   • Therapy (9.5.4): −1 Trauma stack for TherapyBaseCost × (1 + stacks) (§6.2.4). Repeatable.
    // Services are invoked interactively (UI = Epic 13); Leave() completes the node (→ MapView).
    public sealed class PokemonCenterNodeController : NodeController
    {
        private readonly Box _box;
        private readonly EconomyConfigSO _economy;

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
                    // §7.8.3.1 (CL-016) Trauma Resistance raises the heal ceiling this Region.
                    p.CurrentHP = PokemonVitals.EffectiveMaxHP(p, _economy,
                        RegionModifierResolver.TraumaPenaltyReduction(RunState?.ActiveRegionModifiers));
                    StatusEffectManager.CureAll(p); // Per §7.6.1 "Full restore" — cure status too
                }
            }
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
