using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Per Task 3.4.2 — Custom inspector for MoveSO.
    // Validates: SF/SB only on Melee (§3.3.2), APCost 0-4 (§5.3.6),
    // Ranged moves must use 0.75 multiplier (§9.3.2.2), Offensive with BasePower>0.
    [CustomEditor(typeof(MoveSO))]
    public class MoveSOEditor : UnityEditor.Editor
    {
        private const float RANGED_MULTIPLIER = 0.75f;
        private bool _validationFoldout = true;
        private bool _effectsFoldout    = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            DrawSeparator();

            MoveSO move = (MoveSO)target;

            DrawValidation(move);
            EditorGUILayout.Space(4);
            DrawEffectsLinks(move);
        }

        // ── Validation ─────────────────────────────────────────────────────
        private void DrawValidation(MoveSO move)
        {
            _validationFoldout = EditorGUILayout.Foldout(_validationFoldout,
                "Validation", true, EditorStyles.foldoutHeader);

            if (!_validationFoldout)
                return;

            EditorGUI.indentLevel++;
            bool allClean = true;

            // Identity checks
            if (string.IsNullOrWhiteSpace(move.MoveId))
            {
                EditorGUILayout.HelpBox("⚠ MoveId is empty.", MessageType.Warning);
                allClean = false;
            }
            if (string.IsNullOrWhiteSpace(move.DisplayName))
            {
                EditorGUILayout.HelpBox("⚠ DisplayName is empty.", MessageType.Warning);
                allClean = false;
            }

            // Per §3.3.2 / §3.3.3 — StepForward / StepBackward only valid on Melee.
            if (move.Range == MoveRange.Ranged &&
                move.Modifier != PositionalModifier.None)
            {
                EditorGUILayout.HelpBox(
                    $"ERROR — {move.Modifier} is only valid on Melee moves. " +
                    "Ranged moves cannot use positional modifiers. (Per §3.3.2/§3.3.3)",
                    MessageType.Error);
                allClean = false;
            }

            // Per §9.3.2.2 — Ranged moves must use 0.75 multiplier.
            if (move.Range == MoveRange.Ranged &&
                !Mathf.Approximately(move.RangeModifierMultiplier, RANGED_MULTIPLIER))
            {
                EditorGUILayout.HelpBox(
                    $"WARNING — Ranged move has RangeModifierMultiplier = " +
                    $"{move.RangeModifierMultiplier:F2}. Expected 0.75. " +
                    "(Per §9.3.2.2)",
                    MessageType.Warning);
                if (GUILayout.Button("Fix: Set to 0.75"))
                {
                    Undo.RecordObject(move, "Fix RangeModifierMultiplier");
                    move.RangeModifierMultiplier = RANGED_MULTIPLIER;
                    EditorUtility.SetDirty(move);
                }
                allClean = false;
            }

            // Melee moves should use 1.0 multiplier.
            if (move.Range == MoveRange.Melee &&
                !Mathf.Approximately(move.RangeModifierMultiplier, 1f))
            {
                EditorGUILayout.HelpBox(
                    $"WARNING — Melee move has RangeModifierMultiplier = " +
                    $"{move.RangeModifierMultiplier:F2}. Expected 1.0.",
                    MessageType.Warning);
                if (GUILayout.Button("Fix: Set to 1.0"))
                {
                    Undo.RecordObject(move, "Fix RangeModifierMultiplier");
                    move.RangeModifierMultiplier = 1f;
                    EditorUtility.SetDirty(move);
                }
                allClean = false;
            }

            // Per §5.3.6 — APCost 0-4. [Range(0,4)] attribute already clamps in Inspector
            // but guard against programmatic assignments.
            if (move.APCost < 0 || move.APCost > 4)
            {
                EditorGUILayout.HelpBox(
                    $"ERROR — APCost is {move.APCost}. Must be 0–4. (Per §5.3.6)",
                    MessageType.Error);
                allClean = false;
            }

            // Per §5.3.6 — Offensive moves should have BasePower > 0.
            if (move.Role == MoveRole.Offensive && move.BasePower <= 0)
            {
                EditorGUILayout.HelpBox(
                    "WARNING — Offensive move has BasePower = 0. " +
                    "Defensive/Utility moves with 0 BP are intentional.",
                    MessageType.Warning);
                allClean = false;
            }

            // AlwaysCrit info
            if (move.AlwaysCrit)
            {
                EditorGUILayout.HelpBox(
                    "ℹ AlwaysCrit is active. This move bypasses the normal crit " +
                    "calculation and always delivers a critical hit.",
                    MessageType.Info);
            }

            if (allClean)
            {
                EditorGUILayout.HelpBox("✓ All validation checks passed.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        // ── Effects quick-links ────────────────────────────────────────────
        private void DrawEffectsLinks(MoveSO move)
        {
            if (move.Effects == null || move.Effects.Count == 0)
                return;

            _effectsFoldout = EditorGUILayout.Foldout(_effectsFoldout,
                $"Effects ({move.Effects.Count})", true, EditorStyles.foldoutHeader);

            if (!_effectsFoldout)
                return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < move.Effects.Count; i++)
            {
                MoveEffectSO effect = move.Effects[i];
                if (effect == null)
                {
                    EditorGUILayout.HelpBox($"⚠ Effects[{i}] is null.", MessageType.Warning);
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}] {effect.GetType().Name}",
                    EditorStyles.miniLabel);
                if (GUILayout.Button("Select", GUILayout.Width(56)))
                    Selection.activeObject = effect;
                if (GUILayout.Button("Ping", GUILayout.Width(42)))
                    EditorGUIUtility.PingObject(effect);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            EditorGUILayout.Space(2);
        }
    }
}
