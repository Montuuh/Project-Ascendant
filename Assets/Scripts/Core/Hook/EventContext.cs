namespace ProjectAscendant.Core
{
    // Per §8.7 — mutable context object passed through ScriptableHook.OnFire().
    // Hooks read input fields and write output fields; the calling system applies results.
    // Epic 4 will extend this with full combat state references.
    public class EventContext
    {
        // Source and target Pokémon for this hook invocation.
        public PokemonInstance Source;
        public PokemonInstance Target;

        // Per ModifyDamageHook — multiplied into the damage formula before floor().
        public float DamageMultiplier = 1f;

        // Per ModifyDamageHook — flat bonus added after multiplier.
        public int FlatDamageBonus;

        // Per HealHook — HP to restore to Target.
        public int HealAmount;

        // Per GrantAPHook — AP to add to the player's pool this turn.
        public int APGranted;

        // Per ApplyStatusHook — status condition to apply to Target.
        public StatusCondition StatusToApply;

        // Per BuffStatHook — stat and stage delta to apply to Target.
        public Stat StatTarget;
        public int  StatStageChange;

        // Per DrawCardHook — extra skill cards to draw this turn.
        public int CardsToDrawBonus;
    }
}
