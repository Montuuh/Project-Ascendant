using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Per Tasks 3.4.1 + 3.5.3 — Custom inspector for PokemonSpeciesSO.
    // Shows: evolution graph (branches → sub-branches → evolved species),
    // mastery move summary, learnset count validation, GDD reference footer.
    [CustomEditor(typeof(PokemonSpeciesSO))]
    public class PokemonSpeciesSOEditor : UnityEditor.Editor
    {
        private bool _evolutionFoldout = true;
        private bool _masteryFoldout  = true;
        private bool _learnsetFoldout = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            DrawSeparator();

            PokemonSpeciesSO species = (PokemonSpeciesSO)target;

            DrawEvolutionGraph(species);
            EditorGUILayout.Space(4);
            DrawMasterySection(species);
            EditorGUILayout.Space(4);
            DrawLearnsetSummary(species);
            SOEditorUtils.DrawGDDFooter(target);
        }

        // ── Evolution Graph ────────────────────────────────────────────────
        private void DrawEvolutionGraph(PokemonSpeciesSO species)
        {
            _evolutionFoldout = EditorGUILayout.Foldout(_evolutionFoldout,
                "Evolution Graph", true, EditorStyles.foldoutHeader);

            if (!_evolutionFoldout)
                return;

            EditorGUI.indentLevel++;

            if (species.Branches == null || species.Branches.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No branches defined. This is a final-form species or a 1-stage line.",
                    MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (EvolutionBranchSO branch in species.Branches)
            {
                if (branch == null)
                {
                    EditorGUILayout.HelpBox("⚠ Null branch reference.", MessageType.Warning);
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"Branch: {branch.DisplayName} [{branch.Archetype}]",
                    EditorStyles.boldLabel);

                if (GUILayout.Button("Select", GUILayout.Width(56)))
                    Selection.activeObject = branch;
                if (GUILayout.Button("Ping", GUILayout.Width(42)))
                    EditorGUIUtility.PingObject(branch);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;

                // Evolved species
                if (branch.EvolvedSpecies != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"→ {branch.EvolvedSpecies.DisplayName}");
                    if (GUILayout.Button("Select", GUILayout.Width(56)))
                        Selection.activeObject = branch.EvolvedSpecies;
                    if (GUILayout.Button("Ping", GUILayout.Width(42)))
                        EditorGUIUtility.PingObject(branch.EvolvedSpecies);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"⚠ Branch '{branch.DisplayName}' has no EvolvedSpecies assigned.",
                        MessageType.Error);
                }

                // Pool upgrades
                if (branch.MoveUpgrades != null && branch.MoveUpgrades.Count > 0)
                {
                    EditorGUILayout.LabelField($"Pool upgrades: {branch.MoveUpgrades.Count}",
                        EditorStyles.miniLabel);
                }

                // New moves
                if (branch.NewMoves != null && branch.NewMoves.Count > 0)
                {
                    EditorGUILayout.LabelField($"New pool additions: {branch.NewMoves.Count}",
                        EditorStyles.miniLabel);
                }

                // Sub-branches (Stage 2 splits)
                if (branch.SubBranches != null && branch.SubBranches.Count > 0)
                {
                    EditorGUILayout.LabelField("Sub-branches (Stage 2):", EditorStyles.miniLabel);
                    EditorGUI.indentLevel++;
                    foreach (EvolutionBranchSO sub in branch.SubBranches)
                    {
                        if (sub == null)
                        {
                            EditorGUILayout.HelpBox("⚠ Null sub-branch reference.",
                                MessageType.Warning);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        string evolvedName = sub.EvolvedSpecies != null
                            ? sub.EvolvedSpecies.DisplayName
                            : "⚠ NO SPECIES";
                        EditorGUILayout.LabelField($"{sub.DisplayName} → {evolvedName}");
                        if (GUILayout.Button("Select", GUILayout.Width(56)))
                            Selection.activeObject = sub;
                        if (GUILayout.Button("Ping", GUILayout.Width(42)))
                            EditorGUIUtility.PingObject(sub);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }

            EditorGUI.indentLevel--;
        }

        // ── Mastery Move ───────────────────────────────────────────────────
        private void DrawMasterySection(PokemonSpeciesSO species)
        {
            _masteryFoldout = EditorGUILayout.Foldout(_masteryFoldout,
                "Mastery Move", true, EditorStyles.foldoutHeader);

            if (!_masteryFoldout)
                return;

            EditorGUI.indentLevel++;

            // Per §4.3.9.2 — each stage SO defines its own Mastery tier move.
            if (species.MasteryMove == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠ MasteryMove is null. Every species SO must define a Mastery move " +
                    "for its stage. (Per §4.3.9.2)",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{species.MasteryMove.DisplayName}  " +
                    $"[{species.MasteryMove.Type} / {species.MasteryMove.Role}]  " +
                    $"AP:{species.MasteryMove.APCost}  " +
                    $"BP:{species.MasteryMove.BasePower}");

                if (GUILayout.Button("Select", GUILayout.Width(56)))
                    Selection.activeObject = species.MasteryMove;
                if (GUILayout.Button("Ping", GUILayout.Width(42)))
                    EditorGUIUtility.PingObject(species.MasteryMove);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    "Mastery tier advances on evolution if the player has unlocked the " +
                    "required achievement. TMs and Tutors cannot replace this move. (§4.3.9.2)",
                    MessageType.None);
            }

            EditorGUI.indentLevel--;
        }

        // ── Learnset Summary ───────────────────────────────────────────────
        private void DrawLearnsetSummary(PokemonSpeciesSO species)
        {
            _learnsetFoldout = EditorGUILayout.Foldout(_learnsetFoldout,
                "Learnset Summary", true, EditorStyles.foldoutHeader);

            if (!_learnsetFoldout)
                return;

            EditorGUI.indentLevel++;

            // Per §9.3.2.1 — base learnset should be exactly 4 moves.
            int baseCount   = species.BaseLearnset?.Count ?? 0;
            int tutorCount  = species.TutorLearnset?.Count ?? 0;
            int tmCount     = species.TMCompatibility?.Count ?? 0;

            // Base learnset validation
            if (baseCount != 4)
            {
                EditorGUILayout.HelpBox(
                    $"⚠ BaseLearnset has {baseCount} move(s). Expected exactly 4. (Per §9.3.2.1)",
                    baseCount == 0 ? MessageType.Error : MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"✓ BaseLearnset: {baseCount} moves  |  Tutor pool: {tutorCount}  |  " +
                    $"TM compatible: {tmCount}",
                    MessageType.Info);
            }

            // List any null entries in BaseLearnset
            if (species.BaseLearnset != null)
            {
                for (int i = 0; i < species.BaseLearnset.Count; i++)
                {
                    if (species.BaseLearnset[i] == null)
                    {
                        EditorGUILayout.HelpBox(
                            $"⚠ BaseLearnset[{i}] is null.", MessageType.Error);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static void DrawSeparator() => SOEditorUtils.DrawSeparator();
    }
}
