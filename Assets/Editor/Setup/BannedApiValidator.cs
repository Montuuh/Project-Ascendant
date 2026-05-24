using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Per Epic 1 Task 1.5.2 — Warns on banned API usage in production scripts.
// Runs on every AssetDatabase.Refresh() import cycle.
[InitializeOnLoad]
public static class BannedApiValidator
{
    private static readonly (string pattern, string message)[] BANNED = {
        (@"UnityEngine\.Random\b",  "Use GameRNG wrapper instead of UnityEngine.Random (§9.7)"),
        (@"\bnew\s+System\.Random\b", "Use GameRNG wrapper instead of System.Random (§9.7)"),
        (@"Resources\.Load\b",      "Use Addressables instead of Resources.Load (§9.2)"),
        (@"GameObject\.Find\b",     "Use DI / ServiceLocator instead of GameObject.Find (§9.5)"),
        (@"FindObjectOfType\b",     "Use DI / ServiceLocator instead of FindObjectOfType (§9.5)"),
        (@"async\s+void\b",         "async void is forbidden in event handlers — use synchronous handlers (§9.4.2)"),
        (@"GetComponent\s*<",       null),  // not banned, skip
    };

    static BannedApiValidator()
    {
        EditorApplication.delayCall += RunValidation;
    }

    [MenuItem("ProjectAscendant/Validate Banned APIs")]
    public static void RunValidation()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        if (!Directory.Exists(scriptsRoot))
            return;

        string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);
        int warnings = 0;

        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            string relativePath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');

            foreach (var (pattern, message) in BANNED)
            {
                if (message == null) continue;

                MatchCollection matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    // Count line number
                    int line = CountLines(content, match.Index);
                    Debug.LogWarning($"[BannedAPI] {relativePath}:{line} — {message}", null);
                    warnings++;
                }
            }
        }

        if (warnings == 0)
            Debug.Log("[BannedAPI] Validation passed — no banned API usage found.");
    }

    private static int CountLines(string text, int charIndex)
    {
        int lines = 1;
        for (int i = 0; i < charIndex && i < text.Length; i++)
            if (text[i] == '\n') lines++;
        return lines;
    }
}
