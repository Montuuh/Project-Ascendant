using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §3.3.2 + §3.3.3 + §3.3.4 + Epic 6 Task 6.4 — structural validation
    // of authored MoveSO assets and synthetic edge cases.
    //
    // What's covered here:
    //   • Enum-level mutual exclusivity of SF/SB (structural, not runtime).
    //   • SF/SB on Ranged move flagged as Error.
    //   • Catalog sweep: every authored MoveSO in Assets/ScriptableObjects/
    //     must pass IsStructurallyValid.
    public class MoveValidatorTests
    {
        private readonly List<UnityEngine.Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) UnityEngine.Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        // Per §3.3.4 — the PositionalModifier enum is the single source of
        // truth for SF/SB exclusivity. A MoveSO cannot carry both flags
        // because there is no syntactic representation for it. We lock the
        // enum's value set here so the structural guarantee is asserted
        // (a future PR that splits SF/SB into two booleans would fail this).
        [Test]
        public void PositionalModifier_HasExactlyThreeValues_StructuralExclusivity()
        {
            Array values = Enum.GetValues(typeof(PositionalModifier));
            Assert.That(values.Length, Is.EqualTo(3),
                "PositionalModifier must enumerate exactly {None, StepForward, " +
                "StepBackward}. SF/SB exclusivity is enforced by this enum (§3.3.4).");
            HashSet<string> names = new(Enum.GetNames(typeof(PositionalModifier)));
            Assert.That(names, Contains.Item("None"));
            Assert.That(names, Contains.Item("StepForward"));
            Assert.That(names, Contains.Item("StepBackward"));
        }

        [Test]
        public void Validate_StepForwardOnRanged_FlagsError()
        {
            MoveSO m = MakeMove(MoveRange.Ranged, PositionalModifier.StepForward);
            List<MoveValidator.Issue> issues = MoveValidator.Validate(m);
            Assert.That(MoveValidator.IsStructurallyValid(m), Is.False);
            AssertHasError(issues, "StepForward");
        }

        [Test]
        public void Validate_StepBackwardOnRanged_FlagsError()
        {
            MoveSO m = MakeMove(MoveRange.Ranged, PositionalModifier.StepBackward);
            List<MoveValidator.Issue> issues = MoveValidator.Validate(m);
            Assert.That(MoveValidator.IsStructurallyValid(m), Is.False);
            AssertHasError(issues, "StepBackward");
        }

        [Test]
        public void Validate_StepForwardOnMelee_NoError()
        {
            MoveSO m = MakeMove(MoveRange.Melee, PositionalModifier.StepForward);
            Assert.That(MoveValidator.IsStructurallyValid(m), Is.True);
        }

        [Test]
        public void Validate_StepBackwardOnMelee_NoError()
        {
            MoveSO m = MakeMove(MoveRange.Melee, PositionalModifier.StepBackward);
            Assert.That(MoveValidator.IsStructurallyValid(m), Is.True);
        }

        [Test]
        public void Validate_NoneModifierOnRanged_NoError()
        {
            MoveSO m = MakeMove(MoveRange.Ranged, PositionalModifier.None);
            Assert.That(MoveValidator.IsStructurallyValid(m), Is.True);
        }

        [Test]
        public void Validate_APCostOutOfRange_FlagsError()
        {
            MoveSO m = MakeMove(MoveRange.Melee, PositionalModifier.None);
            m.APCost = 9;
            List<MoveValidator.Issue> issues = MoveValidator.Validate(m);
            Assert.That(MoveValidator.IsStructurallyValid(m), Is.False);
            AssertHasError(issues, "APCost");
        }

        [Test]
        public void Validate_NullMove_FlagsError()
        {
            List<MoveValidator.Issue> issues = MoveValidator.Validate(null);
            Assert.That(issues.Count, Is.GreaterThan(0));
            Assert.That(issues[0].Level, Is.EqualTo(MoveValidator.Severity.Error));
        }

        // Catalog sweep — load every authored MoveSO and assert no errors.
        // If a designer ever lands an invalid move via the inspector, this
        // test fires immediately rather than at runtime.
        [Test]
        public void AllAuthoredMoves_AreStructurallyValid()
        {
            string[] guids = AssetDatabase.FindAssets("t:MoveSO");
            Assume.That(guids.Length, Is.GreaterThan(0),
                "Expected at least one authored MoveSO in the project.");

            List<string> failures = new();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                MoveSO move = AssetDatabase.LoadAssetAtPath<MoveSO>(path);
                if (move == null) continue;
                List<MoveValidator.Issue> issues = MoveValidator.Validate(move);
                for (int j = 0; j < issues.Count; j++)
                {
                    if (issues[j].Level != MoveValidator.Severity.Error) continue;
                    failures.Add($"{path}: {issues[j].Message}");
                }
            }

            Assert.That(failures, Is.Empty,
                "Authored MoveSO assets must be structurally valid (§3.3.2/§3.3.3/§5.3.6).\n" +
                string.Join("\n", failures));
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private MoveSO MakeMove(MoveRange range, PositionalModifier mod)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "test";
            m.Type = PokemonType.Normal;
            m.BasePower = 40;
            m.APCost = 2;
            m.Role = MoveRole.Offensive;
            m.Range = range;
            m.Modifier = mod;
            m.RangeModifierMultiplier = range == MoveRange.Ranged ? 0.75f : 1f;
            _disposables.Add(m);
            return m;
        }

        private static void AssertHasError(List<MoveValidator.Issue> issues, string substring)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Level != MoveValidator.Severity.Error) continue;
                if (issues[i].Message.Contains(substring)) return;
            }
            Assert.Fail($"Expected an Error-severity issue containing '{substring}'.");
        }
    }
}
