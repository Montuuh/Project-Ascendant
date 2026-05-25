using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectAscendant.Editor
{
    // Per Task 3.5.2 — Naming convention enforcement per data-assets.md:
    //   SO .asset files  → PascalCase  (e.g. Squirtle_Vanguard.asset)
    //   JSON data files  → kebab-case  (e.g. squirtle-base.json)
    //
    // Run from: Project Ascendant → Run Naming Linter
    // Reports violations to Console with clickable asset context.
    // Does NOT rename assets — diagnosis only.
    public static class VS_NamingLinter
    {
        // Matches PascalCase: starts with an uppercase letter; only letters, digits, underscores.
        // Underscores allowed as separators between words (e.g. Squirtle_Vanguard_A1).
        private static readonly Regex PascalCasePattern =
            new Regex(@"^[A-Z][A-Za-z0-9_]*$", RegexOptions.Compiled);

        // Matches kebab-case: all lowercase letters and digits separated by hyphens.
        private static readonly Regex KebabCasePattern =
            new Regex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        [MenuItem("Project Ascendant/Run Naming Linter")]
        public static void RunAll()
        {
            int errorCount = 0;

            Debug.Log("=== Project Ascendant — Naming Linter START ===");

            errorCount += LintSOAssets();
            errorCount += LintJSONFiles();

            string summary = errorCount == 0
                ? "✓ All naming checks passed."
                : $"Found {errorCount} naming violation(s). Rename the assets listed above.";

            if (errorCount > 0)
                Debug.LogWarning($"=== Naming Linter DONE — {summary} ===");
            else
                Debug.Log($"=== Naming Linter DONE — {summary} ===");
        }

        // ── ScriptableObject .asset files → PascalCase ────────────────────
        // Per data-assets.md: "ScriptableObject .asset files: PascalCase"
        private static int LintSOAssets()
        {
            int violations = 0;
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject",
                new[] { "Assets/ScriptableObjects" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(path);

                if (!PascalCasePattern.IsMatch(filename))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    Debug.LogWarning(
                        $"⚠ [NamingLint] SO asset '{filename}.asset' is not PascalCase. " +
                        $"Path: {path} — Rename to e.g. '{ToPascalCase(filename)}.asset'",
                        asset);
                    violations++;
                }
            }

            return violations;
        }

        // ── JSON data files → kebab-case ──────────────────────────────────
        // Per data-assets.md: "JSON data files: kebab-case filenames"
        private static int LintJSONFiles()
        {
            int violations = 0;
            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Data" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

                string filename = Path.GetFileNameWithoutExtension(path);

                if (!KebabCasePattern.IsMatch(filename))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    Debug.LogWarning(
                        $"⚠ [NamingLint] JSON file '{filename}.json' is not kebab-case. " +
                        $"Path: {path} — Rename to e.g. '{ToKebabCase(filename)}.json'",
                        asset);
                    violations++;
                }
            }

            return violations;
        }

        // ── Suggestion helpers (read-only — do not apply automatically) ───

        // Best-effort: insert underscore before capital runs and lowercase.
        // This is a hint to the developer, not an automatic rename.
        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // Replace hyphens/spaces/underscores with nothing and capitalise each word.
            string[] parts = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string part in parts)
            {
                if (part.Length > 0)
                    sb.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1).ToLowerInvariant());
            }
            return sb.ToString();
        }

        private static string ToKebabCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // Insert hyphen before uppercase letters, then lowercase everything.
            string result = Regex.Replace(input, @"([A-Z])", "-$1").TrimStart('-');
            return Regex.Replace(result.ToLowerInvariant(), @"[\s_]+", "-");
        }
    }
}
