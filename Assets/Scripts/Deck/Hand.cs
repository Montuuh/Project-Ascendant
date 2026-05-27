using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Deck
{
    // Per §3.4 + §3.5 + §3.7 + Epic 5 Task 5.2.1 — the per-turn hand:
    //   • 5 skill cards baseline (drawn from the SkillDeck — §3.4)
    //   • 2 consumable cards baseline (drawn from the ConsumablePile — §3.5)
    // Both numbers come from BattleConfigSO; the per-turn effective count is
    // base + sum of registered hook bonuses (Task 5.2.3 — relics, Badges,
    // Region Modifiers stack additively).
    //
    // Hand is a thin bundle wrapping two mutable lists. The lists are exposed
    // directly so existing callers (CombatController, CombatHUD) can keep
    // using familiar List APIs (Add, RemoveAt, Clear, indexer). Encapsulation
    // benefit comes from the SIZE-modifier surface and the conceptual grouping,
    // not from defensive read-only views.
    public sealed class Hand
    {
        public List<MoveCardInstance> Skill { get; } = new();
        public List<ConsumableSO> Consumables { get; } = new();

        public int SkillCount => Skill.Count;
        public int ConsumableCount => Consumables.Count;

        // Clears both compartments. Called at TurnEnd's tail (after unplayed
        // skill cards are routed to the discard pile) and at CombatEnd.
        public void Clear()
        {
            Skill.Clear();
            Consumables.Clear();
        }
    }

    // Per §3.7 + Task 5.2.3 — effective hand-size calculator. Pure static
    // function so relics / Badges / Region Modifiers can be modeled as simple
    // additive ints without coupling to the ScriptableHook event chain. The
    // hook framework (DrawCardHook + EventContext.CardsToDrawBonus, §8.7)
    // already populates this bonus on the fly; production wiring sums it via
    // an EventBus dispatch before each draw and passes the total here.
    //
    // Floored at 0 — relic-induced negatives can't take the hand below empty.
    public static class HandSizeCalculator
    {
        public static int EffectiveSkillCount(BattleConfigSO config, int bonus = 0)
        {
            if (config == null) return 0;
            int n = config.BaseSkillCardsPerTurn + bonus;
            return n < 0 ? 0 : n;
        }

        public static int EffectiveConsumableCount(BattleConfigSO config, int bonus = 0)
        {
            if (config == null) return 0;
            int n = config.BaseConsumableCardsPerTurn + bonus;
            return n < 0 ? 0 : n;
        }
    }
}
