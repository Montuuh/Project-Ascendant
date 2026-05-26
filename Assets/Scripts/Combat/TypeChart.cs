using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.1.2 + Epic 4 Task 4.3 — Gen I type effectiveness chart.
    //
    // OPEN G5 — PokemonType enum holds 18 values (Gen I 15 + Dark/Steel/Fairy
    // from Gen II+). Spec is "Gen I 15 types". Implementation: full 18×18 array;
    // any cell involving the 3 extras returns 1.0× (neutral). Three TODOs below
    // mark the affected rows/columns until the OPEN flag is ratified.
    //
    // OPEN G6 — Spec worked example "Ground vs Water/Rock = 1.0× × 0.5× = 0.5×"
    // contains an arithmetic error. In Gen I: Ground→Water = 1.0× (no relation),
    // Ground→Rock = 2.0× (super-effective). Product = 2.0×. Implemented per
    // canonical Gen I; tests assert 2.0× for this matchup.
    //
    // Gen I quirks preserved verbatim per spec ("Gen I matrix exactly"):
    //   - Ghost → Psychic = 0.0× (famous Gen I bug; intended 2× in later gens)
    //   - Bug   → Poison  = 2.0× (Gen I; later gens 0.5×)
    //   - Poison → Bug    = 2.0× (Gen I; later gens 1.0×)
    public static class TypeChart
    {
        private const int Count = 18; // matches PokemonType enum length

        private static readonly double[,] s_chart = BuildGenIChart();

        // Returns the cumulative type multiplier for an attack of type `attackType`
        // against a defender with one or two types (product of both lookups).
        // Per §4.1.2: dual-type effectiveness is the product. Immunity (0×) wins
        // by virtue of multiplication.
        public static double GetMultiplier(PokemonType attackType, IReadOnlyList<PokemonType> defenderTypes)
        {
            if (defenderTypes == null || defenderTypes.Count == 0)
                return 1.0;

            double mul = 1.0;
            for (int i = 0; i < defenderTypes.Count; i++)
                mul *= s_chart[(int)attackType, (int)defenderTypes[i]];
            return mul;
        }

        // Single-type lookup, primarily for tests and matrix verification.
        public static double GetSingle(PokemonType attackType, PokemonType defenderType)
            => s_chart[(int)attackType, (int)defenderType];

        private static double[,] BuildGenIChart()
        {
            double[,] c = new double[Count, Count];
            for (int i = 0; i < Count; i++)
                for (int j = 0; j < Count; j++)
                    c[i, j] = 1.0;

            // ─── Normal ────────────────────────────────────────────────────────
            Set(c, PokemonType.Normal, PokemonType.Rock,  0.5);
            Set(c, PokemonType.Normal, PokemonType.Ghost, 0.0);

            // ─── Fire ──────────────────────────────────────────────────────────
            Set(c, PokemonType.Fire, PokemonType.Fire,    0.5);
            Set(c, PokemonType.Fire, PokemonType.Water,   0.5);
            Set(c, PokemonType.Fire, PokemonType.Grass,   2.0);
            Set(c, PokemonType.Fire, PokemonType.Ice,     2.0);
            Set(c, PokemonType.Fire, PokemonType.Bug,     2.0);
            Set(c, PokemonType.Fire, PokemonType.Rock,    0.5);
            Set(c, PokemonType.Fire, PokemonType.Dragon,  0.5);

            // ─── Water ─────────────────────────────────────────────────────────
            Set(c, PokemonType.Water, PokemonType.Fire,   2.0);
            Set(c, PokemonType.Water, PokemonType.Water,  0.5);
            Set(c, PokemonType.Water, PokemonType.Grass,  0.5);
            Set(c, PokemonType.Water, PokemonType.Ground, 2.0);
            Set(c, PokemonType.Water, PokemonType.Rock,   2.0);
            Set(c, PokemonType.Water, PokemonType.Dragon, 0.5);

            // ─── Electric ──────────────────────────────────────────────────────
            Set(c, PokemonType.Electric, PokemonType.Water,    2.0);
            Set(c, PokemonType.Electric, PokemonType.Electric, 0.5);
            Set(c, PokemonType.Electric, PokemonType.Grass,    0.5);
            Set(c, PokemonType.Electric, PokemonType.Ground,   0.0);
            Set(c, PokemonType.Electric, PokemonType.Flying,   2.0);
            Set(c, PokemonType.Electric, PokemonType.Dragon,   0.5);

            // ─── Grass ─────────────────────────────────────────────────────────
            Set(c, PokemonType.Grass, PokemonType.Fire,   0.5);
            Set(c, PokemonType.Grass, PokemonType.Water,  2.0);
            Set(c, PokemonType.Grass, PokemonType.Grass,  0.5);
            Set(c, PokemonType.Grass, PokemonType.Poison, 0.5);
            Set(c, PokemonType.Grass, PokemonType.Ground, 2.0);
            Set(c, PokemonType.Grass, PokemonType.Flying, 0.5);
            Set(c, PokemonType.Grass, PokemonType.Bug,    0.5);
            Set(c, PokemonType.Grass, PokemonType.Rock,   2.0);
            Set(c, PokemonType.Grass, PokemonType.Dragon, 0.5);

            // ─── Ice ───────────────────────────────────────────────────────────
            Set(c, PokemonType.Ice, PokemonType.Water,  0.5);
            Set(c, PokemonType.Ice, PokemonType.Grass,  2.0);
            Set(c, PokemonType.Ice, PokemonType.Ice,    0.5);
            Set(c, PokemonType.Ice, PokemonType.Ground, 2.0);
            Set(c, PokemonType.Ice, PokemonType.Flying, 2.0);
            Set(c, PokemonType.Ice, PokemonType.Dragon, 2.0);

            // ─── Fighting ──────────────────────────────────────────────────────
            Set(c, PokemonType.Fighting, PokemonType.Normal,  2.0);
            Set(c, PokemonType.Fighting, PokemonType.Ice,     2.0);
            Set(c, PokemonType.Fighting, PokemonType.Poison,  0.5);
            Set(c, PokemonType.Fighting, PokemonType.Flying,  0.5);
            Set(c, PokemonType.Fighting, PokemonType.Psychic, 0.5);
            Set(c, PokemonType.Fighting, PokemonType.Bug,     0.5);
            Set(c, PokemonType.Fighting, PokemonType.Rock,    2.0);
            Set(c, PokemonType.Fighting, PokemonType.Ghost,   0.0);

            // ─── Poison ────────────────────────────────────────────────────────
            Set(c, PokemonType.Poison, PokemonType.Grass,  2.0);
            Set(c, PokemonType.Poison, PokemonType.Poison, 0.5);
            Set(c, PokemonType.Poison, PokemonType.Ground, 0.5);
            Set(c, PokemonType.Poison, PokemonType.Bug,    2.0); // Gen I quirk
            Set(c, PokemonType.Poison, PokemonType.Rock,   0.5);
            Set(c, PokemonType.Poison, PokemonType.Ghost,  0.5);

            // ─── Ground ────────────────────────────────────────────────────────
            Set(c, PokemonType.Ground, PokemonType.Fire,     2.0);
            Set(c, PokemonType.Ground, PokemonType.Electric, 2.0);
            Set(c, PokemonType.Ground, PokemonType.Grass,    0.5);
            Set(c, PokemonType.Ground, PokemonType.Poison,   2.0);
            Set(c, PokemonType.Ground, PokemonType.Flying,   0.0);
            Set(c, PokemonType.Ground, PokemonType.Bug,      0.5);
            Set(c, PokemonType.Ground, PokemonType.Rock,     2.0);

            // ─── Flying ────────────────────────────────────────────────────────
            Set(c, PokemonType.Flying, PokemonType.Electric, 0.5);
            Set(c, PokemonType.Flying, PokemonType.Grass,    2.0);
            Set(c, PokemonType.Flying, PokemonType.Fighting, 2.0);
            Set(c, PokemonType.Flying, PokemonType.Bug,      2.0);
            Set(c, PokemonType.Flying, PokemonType.Rock,     0.5);

            // ─── Psychic ───────────────────────────────────────────────────────
            Set(c, PokemonType.Psychic, PokemonType.Fighting, 2.0);
            Set(c, PokemonType.Psychic, PokemonType.Poison,   2.0);
            Set(c, PokemonType.Psychic, PokemonType.Psychic,  0.5);

            // ─── Bug ───────────────────────────────────────────────────────────
            Set(c, PokemonType.Bug, PokemonType.Fire,     0.5);
            Set(c, PokemonType.Bug, PokemonType.Grass,    2.0);
            Set(c, PokemonType.Bug, PokemonType.Fighting, 0.5);
            Set(c, PokemonType.Bug, PokemonType.Poison,   2.0); // Gen I quirk
            Set(c, PokemonType.Bug, PokemonType.Flying,   0.5);
            Set(c, PokemonType.Bug, PokemonType.Psychic,  2.0);
            Set(c, PokemonType.Bug, PokemonType.Ghost,    0.5);

            // ─── Rock ──────────────────────────────────────────────────────────
            Set(c, PokemonType.Rock, PokemonType.Fire,     2.0);
            Set(c, PokemonType.Rock, PokemonType.Ice,      2.0);
            Set(c, PokemonType.Rock, PokemonType.Fighting, 0.5);
            Set(c, PokemonType.Rock, PokemonType.Ground,   0.5);
            Set(c, PokemonType.Rock, PokemonType.Flying,   2.0);
            Set(c, PokemonType.Rock, PokemonType.Bug,      2.0);

            // ─── Ghost ─────────────────────────────────────────────────────────
            Set(c, PokemonType.Ghost, PokemonType.Normal,  0.0);
            Set(c, PokemonType.Ghost, PokemonType.Psychic, 0.0); // Gen I bug
            Set(c, PokemonType.Ghost, PokemonType.Ghost,   2.0);

            // ─── Dragon ────────────────────────────────────────────────────────
            Set(c, PokemonType.Dragon, PokemonType.Dragon, 2.0);

            // TODO: Pending OPEN G5 ratification — Dark/Steel/Fairy rows + columns
            // are left at 1.0× (neutral) until the enum is either trimmed to Gen I
            // or the chart is extended to Gen VI.

            return c;
        }

        private static void Set(double[,] chart, PokemonType atk, PokemonType def, double value)
            => chart[(int)atk, (int)def] = value;
    }
}
