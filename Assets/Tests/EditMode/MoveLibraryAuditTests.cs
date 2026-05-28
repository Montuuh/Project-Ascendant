using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.7 + §5.3.6 kit-template constraints.
    // Walks every MoveSO asset under Assets/ScriptableObjects and asserts
    // library-wide invariants. Mirrors VS_BulkValidator's ValidateMoves but
    // runs as a regression test instead of a manual menu action.
    public class MoveLibraryAuditTests
    {
        private static MoveSO[] LoadAllMoves()
        {
            string[] guids = AssetDatabase.FindAssets("t:MoveSO",
                new[] { "Assets/ScriptableObjects" });
            var moves = new List<MoveSO>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MoveSO move = AssetDatabase.LoadAssetAtPath<MoveSO>(path);
                if (move != null) moves.Add(move);
            }
            return moves.ToArray();
        }

        // ── §5.3.6 — APCost band ────────────────────────────────────────────

        [Test]
        public void AllMoves_APCostInBand_ZeroToFour()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
                if (m.APCost < 0 || m.APCost > 4)
                    bad.Add($"{m.MoveId} APCost={m.APCost}");
            Assert.That(bad, Is.Empty,
                "Per §5.3.6 every move's APCost must be 0–4 inclusive.\nOffenders:\n  "
                + string.Join("\n  ", bad));
        }

        // ── §3.3.2 / §3.3.3 — SF/SB only valid on Melee ─────────────────────

        [Test]
        public void AllMoves_PositionalModifier_OnlyOnMelee()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
                if (m.Range == MoveRange.Ranged && m.Modifier != PositionalModifier.None)
                    bad.Add($"{m.MoveId} ({m.Modifier} on Ranged)");
            Assert.That(bad, Is.Empty,
                "Per §3.3.2/§3.3.3 — Step-Forward and Step-Backward modifiers are valid "
                + "only on Melee moves.\nOffenders:\n  " + string.Join("\n  ", bad));
        }

        // ── §9.3.2.2 — Range modifier multiplier conventions ────────────────

        [Test]
        public void AllMoves_RangedHave075Multiplier()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
                if (m.Range == MoveRange.Ranged
                    && !UnityEngine.Mathf.Approximately(m.RangeModifierMultiplier, 0.75f))
                    bad.Add($"{m.MoveId} mult={m.RangeModifierMultiplier:F3}");
            Assert.That(bad, Is.Empty,
                "Per §9.3.2.2 — Ranged moves use a 0.75 RangeModifierMultiplier.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllMoves_MeleeHave100Multiplier()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
                if (m.Range == MoveRange.Melee
                    && !UnityEngine.Mathf.Approximately(m.RangeModifierMultiplier, 1f))
                    bad.Add($"{m.MoveId} mult={m.RangeModifierMultiplier:F3}");
            Assert.That(bad, Is.Empty,
                "Per §9.3.2.2 — Melee moves use a 1.0 RangeModifierMultiplier.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        // ── Identity discipline (catch silent paste/seeder errors) ──────────

        [Test]
        public void AllMoves_HaveNonEmptyMoveIdAndDisplayName()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
            {
                string path = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrWhiteSpace(m.MoveId))
                    bad.Add($"missing MoveId @ {path}");
                if (string.IsNullOrWhiteSpace(m.DisplayName))
                    bad.Add($"missing DisplayName @ {path}");
            }
            Assert.That(bad, Is.Empty,
                "Every MoveSO must have a non-empty MoveId and DisplayName.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        [Test]
        public void AllMoves_HaveNonEmptyGDDReference()
        {
            // Per data-assets.md — every SO with numeric balance values carries a
            // §N.N.N GDDReference so grep keeps code and spec aligned.
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
                if (string.IsNullOrWhiteSpace(m.GDDReference))
                    bad.Add(AssetDatabase.GetAssetPath(m));
            Assert.That(bad, Is.Empty,
                "Every MoveSO must have a non-empty GDDReference.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        // ── Offensive moves must do damage ──────────────────────────────────

        [Test]
        public void AllOffensiveMoves_HaveNonZeroBasePower()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
                if (m.Role == MoveRole.Offensive && m.BasePower <= 0)
                    bad.Add($"{m.MoveId} BP={m.BasePower}");
            Assert.That(bad, Is.Empty,
                "Offensive moves must carry BasePower > 0 (Defensive/Utility may not).\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }

        // ── No null entries in Effects lists (catches stale references) ─────

        [Test]
        public void AllMoves_EffectsListHasNoNullEntries()
        {
            var bad = new List<string>();
            foreach (MoveSO m in LoadAllMoves())
            {
                if (m.Effects == null) continue;
                for (int i = 0; i < m.Effects.Count; i++)
                    if (m.Effects[i] == null)
                        bad.Add($"{m.MoveId} Effects[{i}] is null");
            }
            Assert.That(bad, Is.Empty,
                "MoveSO.Effects must not carry null entries.\nOffenders:\n  "
                + string.Join("\n  ", bad));
        }

        // ── Library size sanity (catches accidental wipes) ──────────────────

        [Test]
        public void Library_HasAtLeastSeventyMoves()
        {
            // 6 lines × 4 base + ≥1 Mastery per stage + branch upgrades + shared
            // commons + TM exclusives ≈ 80+. Floor of 70 catches a wipe regression
            // without locking the exact catalogue.
            MoveSO[] all = LoadAllMoves();
            Assert.That(all.Length, Is.GreaterThanOrEqualTo(70),
                $"Library has {all.Length} moves; expected ≥70.");
        }
    }
}
