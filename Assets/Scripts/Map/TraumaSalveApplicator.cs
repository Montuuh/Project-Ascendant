using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §6.2.4 + Epic 12 Task 12.4.B (also closes 11.1.10) — the Trauma Salve relic: single-charge,
    // removes ALL Trauma stacks from one CHOSEN Pokémon, consumed on use. Pure C#.
    public static class TraumaSalveApplicator
    {
        // Returns true if applied (mon had ≥1 stack). On 0 stacks it is NOT consumed (§6.2.6 — can't waste).
        public static bool Apply(RunStateSO run, RelicSO salve, PokemonInstance mon)
        {
            if (run == null || salve == null || mon == null) return false;
            if (mon.TraumaStacks <= 0) return false; // §6.2.6 — option greyed; salve not wasted
            mon.TraumaStacks = 0;                      // §6.2.4 — remove ALL
            run.HeldRelics?.Remove(salve);             // single-charge, consumed on use
            return true;
        }
    }
}
