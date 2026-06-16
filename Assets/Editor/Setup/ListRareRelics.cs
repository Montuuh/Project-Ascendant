using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;
using System.Linq;

public static class ListRareRelics
{
    public static void Execute()
    {
        var relics = AssetDatabase.FindAssets("t:RelicSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<RelicSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(r => r != null && r.Rarity == RarityTier.Rare)
            .ToList();

        Debug.Log($"Found {relics.Count} Rare relics:");
        foreach (var r in relics)
        {
            Debug.Log($"  - {r.name} ({r.DisplayName})");
        }
    }
}
