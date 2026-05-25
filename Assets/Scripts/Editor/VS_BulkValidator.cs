using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Per Task 3.4.4 — Bulk validator for all Project Ascendant ScriptableObjects.
    // Run from: Project Ascendant → Run Bulk Validator
    // Outputs errors/warnings to the Console with clickable asset context.
    // Does NOT fix issues — diagnosis only.
    public static class VS_BulkValidator
    {
        [MenuItem("Project Ascendant/Run Bulk Validator")]
        public static void RunAll()
        {
            int errorCount   = 0;
            int warningCount = 0;

            Debug.Log("=== Project Ascendant — Bulk Validator START ===");

            errorCount   += ValidateMoves(ref warningCount);
            errorCount   += ValidateSpecies(ref warningCount);
            errorCount   += ValidateBranches(ref warningCount);
            errorCount   += ValidateRelics(ref warningCount);
            errorCount   += ValidateTMs(ref warningCount);
            errorCount   += ValidateConsumables(ref warningCount);

            string summary = errorCount == 0 && warningCount == 0
                ? "✓ All checks passed — no issues found."
                : $"Finished with {errorCount} error(s), {warningCount} warning(s). " +
                  "Click entries in the Console to navigate to the offending asset.";

            if (errorCount > 0)
                Debug.LogError($"=== Bulk Validator DONE — {summary} ===");
            else if (warningCount > 0)
                Debug.LogWarning($"=== Bulk Validator DONE — {summary} ===");
            else
                Debug.Log($"=== Bulk Validator DONE — {summary} ===");
        }

        // ── MoveSO ─────────────────────────────────────────────────────────
        // Checks: SF/SB on Ranged (ERROR), APCost range (ERROR),
        //         RangeModifierMultiplier (WARNING), missing identity (WARNING),
        //         Offensive with BasePower=0 (WARNING).
        private static int ValidateMoves(ref int warningCount)
        {
            int errorCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:MoveSO");

            foreach (string guid in guids)
            {
                string path  = AssetDatabase.GUIDToAssetPath(guid);
                MoveSO move  = AssetDatabase.LoadAssetAtPath<MoveSO>(path);
                if (move == null) continue;

                string ctx = $"[MoveSO] '{move.DisplayName}' ({move.MoveId})";

                // Identity
                if (string.IsNullOrWhiteSpace(move.MoveId))
                {
                    Debug.LogWarning($"⚠ {ctx}: MoveId is empty.", move);
                    warningCount++;
                }
                if (string.IsNullOrWhiteSpace(move.DisplayName))
                {
                    Debug.LogWarning($"⚠ {ctx}: DisplayName is empty.", move);
                    warningCount++;
                }

                // Per §3.3.2/§3.3.3 — SF/SB only on Melee
                if (move.Range == MoveRange.Ranged &&
                    move.Modifier != PositionalModifier.None)
                {
                    Debug.LogError(
                        $"✘ {ctx}: Positional modifier '{move.Modifier}' on a Ranged move. " +
                        "Only Melee moves may use SF/SB. (Per §3.3.2/§3.3.3)",
                        move);
                    errorCount++;
                }

                // Per §9.3.2.2 — RangeModifierMultiplier
                if (move.Range == MoveRange.Ranged &&
                    !Mathf.Approximately(move.RangeModifierMultiplier, 0.75f))
                {
                    Debug.LogWarning(
                        $"⚠ {ctx}: Ranged move has RangeModifierMultiplier = " +
                        $"{move.RangeModifierMultiplier:F2}. Expected 0.75. (Per §9.3.2.2)",
                        move);
                    warningCount++;
                }
                if (move.Range == MoveRange.Melee &&
                    !Mathf.Approximately(move.RangeModifierMultiplier, 1f))
                {
                    Debug.LogWarning(
                        $"⚠ {ctx}: Melee move has RangeModifierMultiplier = " +
                        $"{move.RangeModifierMultiplier:F2}. Expected 1.0.",
                        move);
                    warningCount++;
                }

                // APCost range
                if (move.APCost < 0 || move.APCost > 4)
                {
                    Debug.LogError(
                        $"✘ {ctx}: APCost is {move.APCost}. Must be 0–4. (Per §5.3.6)",
                        move);
                    errorCount++;
                }

                // Offensive with no power
                if (move.Role == MoveRole.Offensive && move.BasePower <= 0)
                {
                    Debug.LogWarning(
                        $"⚠ {ctx}: Offensive move with BasePower = 0.",
                        move);
                    warningCount++;
                }

                // Null effect entries
                if (move.Effects != null)
                {
                    for (int i = 0; i < move.Effects.Count; i++)
                    {
                        if (move.Effects[i] == null)
                        {
                            Debug.LogWarning(
                                $"⚠ {ctx}: Effects[{i}] is null.", move);
                            warningCount++;
                        }
                    }
                }
            }

            return errorCount;
        }

        // ── PokemonSpeciesSO ───────────────────────────────────────────────
        // Checks: BaseLearnset == 4 (WARNING), MasteryMove null (ERROR),
        //         null entries in BaseLearnset (ERROR), Branch null EvolvedSpecies (ERROR).
        private static int ValidateSpecies(ref int warningCount)
        {
            int errorCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO");

            foreach (string guid in guids)
            {
                string path          = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp  = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(path);
                if (sp == null) continue;

                string ctx = $"[PokemonSpeciesSO] '{sp.DisplayName}' ({sp.SpeciesId})";

                // Mastery Move required on every SO per §4.3.9.2
                if (sp.MasteryMove == null)
                {
                    Debug.LogError(
                        $"✘ {ctx}: MasteryMove is null. Every species SO must define a " +
                        "Mastery move for its stage. (Per §4.3.9.2)",
                        sp);
                    errorCount++;
                }

                // BaseLearnset — per §9.3.2.1 exactly 4 moves
                int baseCount = sp.BaseLearnset?.Count ?? 0;
                if (baseCount != 4)
                {
                    Debug.LogWarning(
                        $"⚠ {ctx}: BaseLearnset has {baseCount} move(s). " +
                        "Expected exactly 4. (Per §9.3.2.1)",
                        sp);
                    warningCount++;
                }

                if (sp.BaseLearnset != null)
                {
                    for (int i = 0; i < sp.BaseLearnset.Count; i++)
                    {
                        if (sp.BaseLearnset[i] == null)
                        {
                            Debug.LogError(
                                $"✘ {ctx}: BaseLearnset[{i}] is null.", sp);
                            errorCount++;
                        }
                    }
                }

                // Branches — null references and null EvolvedSpecies
                if (sp.Branches != null)
                {
                    for (int i = 0; i < sp.Branches.Count; i++)
                    {
                        EvolutionBranchSO branch = sp.Branches[i];
                        if (branch == null)
                        {
                            Debug.LogError(
                                $"✘ {ctx}: Branches[{i}] is null.", sp);
                            errorCount++;
                            continue;
                        }
                        if (branch.EvolvedSpecies == null)
                        {
                            Debug.LogError(
                                $"✘ {ctx}: Branches[{i}] ('{branch.DisplayName}') has no " +
                                "EvolvedSpecies assigned.", sp);
                            errorCount++;
                        }
                    }
                }
            }

            return errorCount;
        }

        // ── EvolutionBranchSO ──────────────────────────────────────────────
        // Checks: null EvolvedSpecies (ERROR), null MoveUpgrade entries (WARNING),
        //         sub-branch null EvolvedSpecies (ERROR).
        private static int ValidateBranches(ref int warningCount)
        {
            int errorCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:EvolutionBranchSO");

            foreach (string guid in guids)
            {
                string path             = AssetDatabase.GUIDToAssetPath(guid);
                EvolutionBranchSO branch = AssetDatabase.LoadAssetAtPath<EvolutionBranchSO>(path);
                if (branch == null) continue;

                string ctx = $"[EvolutionBranchSO] '{branch.DisplayName}' ({branch.BranchId})";

                if (branch.EvolvedSpecies == null)
                {
                    Debug.LogError(
                        $"✘ {ctx}: EvolvedSpecies is null.", branch);
                    errorCount++;
                }

                // MoveUpgrades: both sides must be non-null
                if (branch.MoveUpgrades != null)
                {
                    for (int i = 0; i < branch.MoveUpgrades.Count; i++)
                    {
                        MoveUpgradePair pair = branch.MoveUpgrades[i];
                        if (pair.OldMove == null)
                        {
                            Debug.LogWarning(
                                $"⚠ {ctx}: MoveUpgrades[{i}].OldMove is null.", branch);
                            warningCount++;
                        }
                        if (pair.NewMove == null)
                        {
                            Debug.LogWarning(
                                $"⚠ {ctx}: MoveUpgrades[{i}].NewMove is null.", branch);
                            warningCount++;
                        }
                    }
                }

                // Sub-branches
                if (branch.SubBranches != null)
                {
                    for (int i = 0; i < branch.SubBranches.Count; i++)
                    {
                        EvolutionBranchSO sub = branch.SubBranches[i];
                        if (sub == null)
                        {
                            Debug.LogError(
                                $"✘ {ctx}: SubBranches[{i}] is null.", branch);
                            errorCount++;
                            continue;
                        }
                        if (sub.EvolvedSpecies == null)
                        {
                            Debug.LogError(
                                $"✘ {ctx}: SubBranches[{i}] ('{sub.DisplayName}') has no " +
                                "EvolvedSpecies.", branch);
                            errorCount++;
                        }
                    }
                }
            }

            return errorCount;
        }

        // ── RelicSO ────────────────────────────────────────────────────────
        // Checks: null Channel or null Hook in EventHooks (WARNING),
        //         missing Id/DisplayName (WARNING).
        private static int ValidateRelics(ref int warningCount)
        {
            int errorCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:RelicSO");

            foreach (string guid in guids)
            {
                string path   = AssetDatabase.GUIDToAssetPath(guid);
                RelicSO relic = AssetDatabase.LoadAssetAtPath<RelicSO>(path);
                if (relic == null) continue;

                string ctx = $"[RelicSO] '{relic.DisplayName}' ({relic.Id})";

                if (string.IsNullOrWhiteSpace(relic.Id))
                {
                    Debug.LogWarning($"⚠ {ctx}: Id is empty.", relic);
                    warningCount++;
                }
                if (string.IsNullOrWhiteSpace(relic.DisplayName))
                {
                    Debug.LogWarning($"⚠ {ctx}: DisplayName is empty.", relic);
                    warningCount++;
                }

                if (relic.EventHooks != null)
                {
                    for (int i = 0; i < relic.EventHooks.Count; i++)
                    {
                        HookBinding binding = relic.EventHooks[i];
                        if (binding.Channel == null)
                        {
                            Debug.LogWarning(
                                $"⚠ {ctx}: EventHooks[{i}].Channel is null. " +
                                "HookSubscriber will skip this binding.",
                                relic);
                            warningCount++;
                        }
                        if (binding.Hook == null)
                        {
                            Debug.LogWarning(
                                $"⚠ {ctx}: EventHooks[{i}].Hook is null. " +
                                "HookSubscriber will skip this binding.",
                                relic);
                            warningCount++;
                        }
                    }
                }
            }

            return errorCount;
        }

        // ── TMSO ───────────────────────────────────────────────────────────
        // Checks: null MoveTeach (ERROR), missing Id (WARNING).
        private static int ValidateTMs(ref int warningCount)
        {
            int errorCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:TMSO");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMSO tm     = AssetDatabase.LoadAssetAtPath<TMSO>(path);
                if (tm == null) continue;

                string ctx = $"[TMSO] '{tm.DisplayName}' ({tm.Id})";

                if (string.IsNullOrWhiteSpace(tm.Id))
                {
                    Debug.LogWarning($"⚠ {ctx}: Id is empty.", tm);
                    warningCount++;
                }

                if (tm.MoveTeach == null)
                {
                    Debug.LogError(
                        $"✘ {ctx}: MoveTeach is null. A TM must reference a move. " +
                        "(Per §8.5)",
                        tm);
                    errorCount++;
                }
            }

            return errorCount;
        }

        // ── ConsumableSO ───────────────────────────────────────────────────
        // Checks: null Effect (ERROR), APCost range (ERROR), missing Id (WARNING).
        private static int ValidateConsumables(ref int warningCount)
        {
            int errorCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:ConsumableSO");

            foreach (string guid in guids)
            {
                string path        = AssetDatabase.GUIDToAssetPath(guid);
                ConsumableSO item  = AssetDatabase.LoadAssetAtPath<ConsumableSO>(path);
                if (item == null) continue;

                string ctx = $"[ConsumableSO] '{item.DisplayName}' ({item.Id})";

                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    Debug.LogWarning($"⚠ {ctx}: Id is empty.", item);
                    warningCount++;
                }

                if (item.Effect == null)
                {
                    Debug.LogError(
                        $"✘ {ctx}: Effect is null. Every consumable must have a " +
                        "ConsumableEffectSO assigned.",
                        item);
                    errorCount++;
                }

                if (item.APCost < 0 || item.APCost > 4)
                {
                    Debug.LogError(
                        $"✘ {ctx}: APCost is {item.APCost}. Must be 0–4. (Per §5.3.6)",
                        item);
                    errorCount++;
                }
            }

            return errorCount;
        }
    }
}
