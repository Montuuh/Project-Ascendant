using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §3.3.2 + §3.3.3 + §3.3.4 + Epic 6 Task 6.4 — single source of truth
    // for MoveSO structural validation. Both the custom inspector
    // (MoveSOEditor) and EditMode tests consume this, so a rule change
    // only has to land in one place.
    //
    // Why a separate validator rather than only in the editor inspector:
    //   • Editor-only checks don't run when MoveSO assets are loaded from
    //     Addressables — the test suite asserts the whole authored catalog.
    //   • Designers occasionally script MoveSO creation; a pure validator
    //     can be invoked from a tooling pass without GUI.
    //
    // Mutual exclusivity of Step-Forward and Step-Backward is enforced
    // structurally by the single PositionalModifier enum (§3.3.4) — there
    // is no path through which a MoveSO can carry both. The validator
    // documents this by asserting the enum's value set in a test, rather
    // than checking a redundant flag at runtime.
    public static class MoveValidator
    {
        public enum Severity { Info, Warning, Error }

        public readonly struct Issue
        {
            public readonly Severity Level;
            public readonly string Message;
            public Issue(Severity level, string message)
            {
                Level = level;
                Message = message;
            }
        }

        // Validate a single move. Returns an empty list if clean. The same
        // checks the inspector surfaces, minus the cosmetic "all clean" /
        // "AlwaysCrit" info messages.
        public static List<Issue> Validate(MoveSO move)
        {
            List<Issue> issues = new();
            if (move == null)
            {
                issues.Add(new Issue(Severity.Error, "MoveSO is null."));
                return issues;
            }

            // Per §3.3.2 / §3.3.3 / §3.3.4 — SF/SB only valid on Melee moves.
            // The single-enum design (PositionalModifier) means SF+SB cannot
            // both be set on a single move; only the SF/SB-on-Ranged guard
            // is meaningful here.
            if (move.Range == MoveRange.Ranged &&
                move.Modifier != PositionalModifier.None)
            {
                issues.Add(new Issue(Severity.Error,
                    $"{move.Modifier} is only valid on Melee moves " +
                    "(§3.3.2/§3.3.3). Ranged moves cannot use positional modifiers."));
            }

            // Per §5.3.6 — APCost 0–4.
            if (move.APCost < 0 || move.APCost > 4)
            {
                issues.Add(new Issue(Severity.Error,
                    $"APCost is {move.APCost}. Must be 0–4 (§5.3.6)."));
            }

            // Per §5.3.6 — Offensive moves must have BasePower > 0.
            if (move.Role == MoveRole.Offensive && move.BasePower <= 0)
            {
                issues.Add(new Issue(Severity.Warning,
                    "Offensive move has BasePower = 0. Defensive/Utility " +
                    "moves with 0 BP are intentional."));
            }

            // Per §9.3.2.2 — Range multiplier conventions.
            const float RANGED_MULTIPLIER = 0.75f;
            const float MELEE_MULTIPLIER  = 1.0f;
            if (move.Range == MoveRange.Ranged &&
                !Approximately(move.RangeModifierMultiplier, RANGED_MULTIPLIER))
            {
                issues.Add(new Issue(Severity.Warning,
                    $"Ranged move has RangeModifierMultiplier = " +
                    $"{move.RangeModifierMultiplier:F2}. Expected 0.75 (§9.3.2.2)."));
            }
            if (move.Range == MoveRange.Melee &&
                !Approximately(move.RangeModifierMultiplier, MELEE_MULTIPLIER))
            {
                issues.Add(new Issue(Severity.Warning,
                    $"Melee move has RangeModifierMultiplier = " +
                    $"{move.RangeModifierMultiplier:F2}. Expected 1.0."));
            }

            return issues;
        }

        // True iff the move has zero Error-severity issues. Warnings allowed.
        public static bool IsStructurallyValid(MoveSO move)
        {
            List<Issue> issues = Validate(move);
            for (int i = 0; i < issues.Count; i++)
                if (issues[i].Level == Severity.Error) return false;
            return true;
        }

        private static bool Approximately(float a, float b) =>
            System.Math.Abs(a - b) < 1e-4f;
    }
}
