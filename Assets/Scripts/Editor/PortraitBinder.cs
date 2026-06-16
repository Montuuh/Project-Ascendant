#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Binds each PokemonSpeciesSO.Portrait to its canonical dex-format sprite
    // (Assets/Sprites/VS/Portraits/<NNN>-<Name>.png), matched by the species
    // asset name. Idempotent — a species already pointing at its dex sprite is
    // left untouched. Run after docs/scripts/fetch-pokemon-portraits.js pulls
    // real artwork, to replace the plain `<Name>.png` placeholder bindings the
    // PlaceholderSpriteSeeder created. Companion to that seeder (Epic 7 / CL-024).
    public static class PortraitBinder
    {
        private const string DIR = "Assets/Sprites/VS/Portraits";

        [MenuItem("Project Ascendant/Bind Pokémon Portraits")]
        public static void Bind()
        {
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO");
            int bound = 0, missing = 0;

            foreach (string guid in guids)
            {
                string spPath = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(spPath);
                if (sp == null) continue;

                string dexPath = FindDexPortrait(sp.name);
                if (dexPath == null) { missing++; continue; }

                EnsureSprite(dexPath);
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(dexPath);
                if (spr != null && sp.Portrait != spr)
                {
                    sp.Portrait = spr;
                    EditorUtility.SetDirty(sp);
                    bound++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PortraitBinder] Bound {bound} species to dex portraits " +
                      $"({missing} had no <NNN>-<Name>.png yet — run fetch-pokemon-portraits.js).");
        }

        // Finds Assets/Sprites/VS/Portraits/<digits>-<name>.png for a species asset name.
        private static string FindDexPortrait(string speciesName)
        {
            if (!Directory.Exists(DIR)) return null;
            Regex rx = new Regex($@"^\d+-{Regex.Escape(speciesName)}\.png$");
            foreach (string file in Directory.GetFiles(DIR, "*.png"))
            {
                string name = Path.GetFileName(file);
                if (rx.IsMatch(name)) return DIR + "/" + name;
            }
            return null;
        }

        private static void EnsureSprite(string assetPath)
        {
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;

            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }
}
#endif
