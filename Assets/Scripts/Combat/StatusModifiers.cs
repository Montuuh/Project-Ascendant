using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.2 + Epic 4 Task 4.5 — pure query helpers translating a Pokémon's
    // current status into the modifiers used by combat sub-systems.
    //
    // All numeric values come from BattleConfigSO (per data-discipline rules).
    // Boolean queries (cards-playable, position-locked) don't need config.
    //
    // Interaction order (OPEN G8): callers apply stat-stage multipliers FIRST,
    // then multiply by the status modifier returned here. See CombatStatResolver.
    public static class StatusModifiers
    {
        // Per §4.2.2.1 — Burn reduces Attack by 25%. Other primary statuses: 1.0×.
        public static float GetAttackMultiplier(StatusCondition primary, BattleConfigSO config)
        {
            if (config != null && primary == StatusCondition.Burn)
                return config.BurnAttackMultiplier;
            return 1f;
        }

        // Per §4.2.2.2 — Poison reduces Defense by 15%. Other primary statuses: 1.0×.
        public static float GetDefenseMultiplier(StatusCondition primary, BattleConfigSO config)
        {
            if (config != null && primary == StatusCondition.Poison)
                return config.PoisonDefenseMultiplier;
            return 1f;
        }

        // Per §4.2.2.3 — Paralysis adds AP cost to every move (Lead or bench).
        // 0 for all other primary statuses.
        public static int GetMoveAPCostBonus(StatusCondition primary, BattleConfigSO config)
        {
            if (config != null && primary == StatusCondition.Paralysis)
                return config.ParalysisAPCostBonus;
            return 0;
        }

        // Per §4.2.2.4/5 — Sleep and Freeze make all cards unplayable.
        // Other statuses don't block playability.
        public static bool AreCardsPlayable(StatusCondition primary)
        {
            return primary != StatusCondition.Sleep && primary != StatusCondition.Freeze;
        }

        // Per §4.2.2.5 — only Freeze locks the Pokémon's position (cannot be
        // manually swapped, cannot be SB'd). Per §3.3.5.1 Faint precedence
        // voids this lock when CurrentHP == 0; callers must check faint first.
        public static bool IsPositionLocked(StatusCondition primary)
        {
            return primary == StatusCondition.Freeze;
        }

        // Per §4.2.2.5 — Frozen targets take ×1.5 damage from Fire-type moves.
        // Per OPEN G9 — the multiplier is active for the full Freeze duration.
        // Returns 1.0 otherwise.
        public static float GetIncomingDamageMultiplier(PokemonInstance target, MoveSO move, BattleConfigSO config)
        {
            if (target == null || move == null || config == null) return 1f;
            if (target.PrimaryStatus == StatusCondition.Freeze && move.Type == PokemonType.Fire)
                return config.FreezeFireDamageMultiplier;
            return 1f;
        }

        // Convenience for callers (Action Phase / Deck system): the effective AP
        // cost of a move accounting for its owner's status. Useful for card UI.
        public static int GetEffectiveAPCost(MoveSO move, PokemonInstance owner, BattleConfigSO config)
        {
            if (move == null) return 0;
            int baseCost = move.APCost;
            if (owner == null) return baseCost;
            return baseCost + GetMoveAPCostBonus(owner.PrimaryStatus, config);
        }
    }
}
