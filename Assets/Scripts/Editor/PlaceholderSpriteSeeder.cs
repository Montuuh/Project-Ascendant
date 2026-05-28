#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Per Epic 7 Task 7.8 — Placeholder portrait generator (96x96 PNGs,
    // type-colored solid squares with a 4px border + 1-letter species initial).
    // Final art swap deferred per Task 7.8.3.
    //
    // Run from: Project Ascendant → Generate Placeholder Portraits.
    // Idempotent — overwrites prior placeholders; re-assigns species.Portrait
    // refs in case any species SO lost its binding.
    public static class PlaceholderSpriteSeeder
    {
        private const string OUT_DIR = "Assets/Sprites/VS/Portraits";
        private const int SIZE_PORTRAIT = 96;
        private const int BORDER_PX = 4;

        [MenuItem("Project Ascendant/Generate Placeholder Portraits")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(OUT_DIR))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                    AssetDatabase.CreateFolder("Assets", "Sprites");
                if (!AssetDatabase.IsValidFolder("Assets/Sprites/VS"))
                    AssetDatabase.CreateFolder("Assets/Sprites", "VS");
                AssetDatabase.CreateFolder("Assets/Sprites/VS", "Portraits");
            }

            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO");
            int written = 0, assigned = 0;

            foreach (string guid in guids)
            {
                string speciesPath = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(speciesPath);
                if (sp == null) continue;

                PokemonType primaryType = (sp.Types != null && sp.Types.Count > 0)
                    ? sp.Types[0]
                    : PokemonType.Normal;

                string fileName = sp.name + ".png";
                string pngPath = $"{OUT_DIR}/{fileName}";
                string initial = string.IsNullOrEmpty(sp.DisplayName)
                    ? "?"
                    : sp.DisplayName.Substring(0, 1).ToUpperInvariant();

                WritePlaceholderPng(pngPath, ColorFor(primaryType), initial);
                written++;
                ConfigureAsSprite(pngPath, SIZE_PORTRAIT);

                Sprite imported = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (imported != null && sp.Portrait != imported)
                {
                    sp.Portrait = imported;
                    EditorUtility.SetDirty(sp);
                    assigned++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[PlaceholderSpriteSeeder] Wrote {written} portraits, " +
                $"assigned {assigned} species.Portrait references.");
        }

        // ── Pixel writer ────────────────────────────────────────────────────

        private static void WritePlaceholderPng(string assetPath, Color32 bg, string initial)
        {
            var tex = new Texture2D(SIZE_PORTRAIT, SIZE_PORTRAIT,
                                    TextureFormat.RGBA32, mipChain: false);
            Color32 border = new Color32(20, 20, 20, 255);
            Color32 letterPx = new Color32(255, 255, 255, 255);

            // Fill background + border.
            Color32[] buf = new Color32[SIZE_PORTRAIT * SIZE_PORTRAIT];
            for (int y = 0; y < SIZE_PORTRAIT; y++)
            for (int x = 0; x < SIZE_PORTRAIT; x++)
            {
                bool onBorder = x < BORDER_PX || y < BORDER_PX
                             || x >= SIZE_PORTRAIT - BORDER_PX
                             || y >= SIZE_PORTRAIT - BORDER_PX;
                buf[y * SIZE_PORTRAIT + x] = onBorder ? border : bg;
            }

            // Stamp species initial as a chunky 5x7 bitmap centered on the texture.
            StampLetter(buf, initial[0], SIZE_PORTRAIT, letterPx);

            tex.SetPixels32(buf);
            tex.Apply(updateMipmaps: false);

            byte[] png = tex.EncodeToPNG();
            string systemPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(systemPath));
            File.WriteAllBytes(systemPath, png);

            Object.DestroyImmediate(tex);
        }

        // 5x7 monospace bitmap font for A–Z (uppercase). Each row is 5 bits.
        // Letters not in the table fall back to a filled square.
        private static readonly Dictionary<char, byte[]> FONT = new()
        {
            ['A'] = new byte[] { 0b01110,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001 },
            ['B'] = new byte[] { 0b11110,0b10001,0b10001,0b11110,0b10001,0b10001,0b11110 },
            ['C'] = new byte[] { 0b01110,0b10001,0b10000,0b10000,0b10000,0b10001,0b01110 },
            ['G'] = new byte[] { 0b01110,0b10001,0b10000,0b10111,0b10001,0b10001,0b01110 },
            ['I'] = new byte[] { 0b11111,0b00100,0b00100,0b00100,0b00100,0b00100,0b11111 },
            ['M'] = new byte[] { 0b10001,0b11011,0b10101,0b10001,0b10001,0b10001,0b10001 },
            ['P'] = new byte[] { 0b11110,0b10001,0b10001,0b11110,0b10000,0b10000,0b10000 },
            ['S'] = new byte[] { 0b01111,0b10000,0b10000,0b01110,0b00001,0b00001,0b11110 },
            ['V'] = new byte[] { 0b10001,0b10001,0b10001,0b10001,0b10001,0b01010,0b00100 },
            ['W'] = new byte[] { 0b10001,0b10001,0b10001,0b10001,0b10101,0b11011,0b10001 },
            ['F'] = new byte[] { 0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b10000 },
            ['?'] = new byte[] { 0b01110,0b10001,0b00001,0b00010,0b00100,0b00000,0b00100 },
        };

        private const int GLYPH_W = 5;
        private const int GLYPH_H = 7;
        private const int GLYPH_SCALE = 8; // 5*8=40 wide; 7*8=56 tall — centered.

        private static void StampLetter(Color32[] buf, char ch, int texSize, Color32 px)
        {
            if (!FONT.TryGetValue(ch, out byte[] rows))
                rows = FONT['?'];

            int stampW = GLYPH_W * GLYPH_SCALE;
            int stampH = GLYPH_H * GLYPH_SCALE;
            int x0 = (texSize - stampW) / 2;
            int y0 = (texSize - stampH) / 2;

            for (int gy = 0; gy < GLYPH_H; gy++)
            {
                byte row = rows[gy];
                for (int gx = 0; gx < GLYPH_W; gx++)
                {
                    bool lit = ((row >> (GLYPH_W - 1 - gx)) & 1) != 0;
                    if (!lit) continue;
                    for (int sy = 0; sy < GLYPH_SCALE; sy++)
                    for (int sx = 0; sx < GLYPH_SCALE; sx++)
                    {
                        int xx = x0 + gx * GLYPH_SCALE + sx;
                        // Glyph rows are top-down; Unity texture coords are bottom-up.
                        int yy = texSize - 1 - (y0 + gy * GLYPH_SCALE + sy);
                        if (xx >= 0 && xx < texSize && yy >= 0 && yy < texSize)
                            buf[yy * texSize + xx] = px;
                    }
                }
            }
        }

        // ── Importer config ─────────────────────────────────────────────────

        private static void ConfigureAsSprite(string assetPath, int ppu)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;

            imp.textureType        = TextureImporterType.Sprite;
            imp.spriteImportMode   = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.filterMode         = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled      = false;
            imp.SaveAndReimport();
        }

        // ── Type → placeholder color (rough §10.1.3.2 affinity) ─────────────

        private static Color32 ColorFor(PokemonType t) => t switch
        {
            PokemonType.Fire     => new Color32(255,  80,  60, 255),
            PokemonType.Water    => new Color32( 70, 130, 230, 255),
            PokemonType.Grass    => new Color32( 80, 190, 100, 255),
            PokemonType.Electric => new Color32(255, 220,  70, 255),
            PokemonType.Ice      => new Color32(160, 230, 240, 255),
            PokemonType.Fighting => new Color32(190,  90,  60, 255),
            PokemonType.Poison   => new Color32(160,  70, 180, 255),
            PokemonType.Ground   => new Color32(210, 180, 100, 255),
            PokemonType.Flying   => new Color32(170, 180, 240, 255),
            PokemonType.Psychic  => new Color32(255, 110, 170, 255),
            PokemonType.Bug      => new Color32(160, 200,  60, 255),
            PokemonType.Rock     => new Color32(180, 150,  90, 255),
            PokemonType.Ghost    => new Color32(110,  90, 160, 255),
            PokemonType.Dragon   => new Color32(110,  90, 250, 255),
            PokemonType.Dark     => new Color32( 80,  70,  70, 255),
            PokemonType.Steel    => new Color32(180, 180, 200, 255),
            PokemonType.Fairy    => new Color32(240, 180, 220, 255),
            _                    => new Color32(170, 170, 170, 255),
        };
    }
}
#endif
