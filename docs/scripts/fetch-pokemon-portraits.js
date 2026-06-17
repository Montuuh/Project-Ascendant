// fetch-pokemon-portraits.js
// General Pokémon-portrait fetcher for the VS roster. Downloads the public
// PokeAPI "official-artwork" render for any species that currently has only a
// *placeholder* portrait, and saves it in the canonical dex-number format
// (`<NNN>-<Name>.png`) into Assets/Sprites/VS/Portraits/.
//
// This is a *fetch* tool — it pulls artwork you've chosen to use into your
// fan project; it does not generate or modify artwork. After running it, bind
// the sprites in Unity (Project Ascendant → Bind Pokémon Portraits) and delete
// the now-orphaned placeholder files (the menu does the binding; --clean here
// removes placeholders whose dex portrait now exists).
//
// HOW IT KNOWS WHAT TO FETCH (no hardcoded roster):
//   Portraits are saved dex-first (`143-Snorlax.png`). The placeholder seeder
//   instead writes plain `<Name>.png` (no dex prefix). So every file in the
//   Portraits folder WITHOUT a leading `<digits>-` is a placeholder that still
//   needs its real art. For each, we resolve the National Dex id from PokeAPI's
//   name endpoint (variant suffixes like `_Spirit` are stripped for the lookup
//   but kept in the output filename), then download its official-artwork.
//
// Usage:
//   node docs/scripts/fetch-pokemon-portraits.js           (fetch missing dex art)
//   node docs/scripts/fetch-pokemon-portraits.js --force   (re-download even if dex file exists)
//   node docs/scripts/fetch-pokemon-portraits.js --clean    (also delete placeholder <Name>.png once its dex file exists)
//
// Requires Node 18+ (global fetch).

const fs = require('fs');
const path = require('path');

if (typeof fetch !== 'function') {
  console.error('Node 18+ required (global fetch missing).');
  process.exit(1);
}

const DEST = path.join(__dirname, '..', '..', 'Assets', 'Sprites', 'VS', 'Portraits');
const FORCE = process.argv.includes('--force');
const CLEAN = process.argv.includes('--clean');
const API = (name) => `https://pokeapi.co/api/v2/pokemon/${name}`;

// A placeholder is any *.png whose name does NOT start with "<digits>-".
const isDexFormat = (file) => /^\d+-/.test(file);
const isPng = (file) => file.toLowerCase().endsWith('.png');

// PokeAPI lookup name: drop the extension, lowercase, take the part before the
// first underscore (so a fan variant like `Marowak_Spirit` resolves to `marowak`),
// and normalise spaces/dots to hyphens (e.g. `Mr. Mime` -> `mr-mime`).
function apiName(fileBase) {
  return fileBase
    .split('_')[0]
    .toLowerCase()
    .replace(/[.\s]+/g, '-')
    .replace(/[^a-z0-9-]/g, '');
}

(async () => {
  if (!fs.existsSync(DEST)) {
    console.error(`Portraits folder not found: ${DEST}`);
    process.exit(1);
  }

  // Two modes: explicit `--species=a,b,c` fetches those species by name (e.g. new
  // evolution lines not yet in the roster); the default scans for plain `<Name>.png`
  // placeholders and resolves them to dex art.
  const speciesArg = (process.argv.find((a) => a.startsWith('--species=')) || '').slice('--species='.length);
  let work;
  if (speciesArg) {
    work = speciesArg.split(',').map((s) => s.trim()).filter(Boolean).map((name) => {
      const lookup = apiName(name);
      const base = lookup.split('-').map((w) => w.charAt(0).toUpperCase() + w.slice(1)).join('-'); // PascalCase filename
      return { base, lookup, placeholder: null };
    });
    console.log(`Fetching ${work.length} species by name:\n  ${work.map((w) => w.lookup).join(', ')}\n`);
  } else {
    const files = fs.readdirSync(DEST).filter(isPng);
    const placeholders = files.filter((f) => !isDexFormat(f));
    if (placeholders.length === 0) {
      console.log('No placeholder portraits found — every portrait is already dex-format. Nothing to fetch.');
      return;
    }
    console.log(`Found ${placeholders.length} placeholder portrait(s) to resolve:\n  ${placeholders.join(', ')}\n`);
    work = placeholders.map((ph) => ({ base: ph.replace(/\.png$/i, ''), lookup: apiName(ph.replace(/\.png$/i, '')), placeholder: ph }));
  }

  let got = 0, skipped = 0, failed = 0, cleaned = 0;

  for (const item of work) {
    const base = item.base;          // e.g. "Marowak_Spirit" or "Pikachu"
    const lookup = item.lookup;      // e.g. "marowak" / "pikachu"
    const placeholder = item.placeholder;
    try {
      const res = await fetch(API(lookup));
      if (!res.ok) throw new Error(`PokeAPI HTTP ${res.status} for "${lookup}"`);
      const data = await res.json();
      const id = data.id;
      const artUrl = data?.sprites?.other?.['official-artwork']?.front_default;
      if (!id || !artUrl) throw new Error(`no official-artwork for "${lookup}"`);

      const dexFile = `${String(id).padStart(3, '0')}-${base}.png`;  // keep the original (variant) name
      const out = path.join(DEST, dexFile);

      if (fs.existsSync(out) && !FORCE) {
        console.log(`skip   ${dexFile} (exists)`);
        skipped++;
      } else {
        const img = await fetch(artUrl);
        if (!img.ok) throw new Error(`artwork HTTP ${img.status}`);
        const buf = Buffer.from(await img.arrayBuffer());
        fs.writeFileSync(out, buf);
        console.log(`saved  ${dexFile}  (${(buf.length / 1024).toFixed(0)} KB)${placeholder ? `  ← ${placeholder}` : ''}`);
        got++;
      }

      // Optional: remove the placeholder (and its .meta) now that the dex file exists.
      if (CLEAN && placeholder && fs.existsSync(out)) {
        const ph = path.join(DEST, placeholder);
        const phMeta = ph + '.meta';
        if (fs.existsSync(ph)) { fs.unlinkSync(ph); cleaned++; }
        if (fs.existsSync(phMeta)) fs.unlinkSync(phMeta);
        console.log(`clean  removed placeholder ${placeholder}`);
      }

      await new Promise((r) => setTimeout(r, 150)); // be polite to PokeAPI
    } catch (e) {
      console.error(`FAIL   ${placeholder || lookup}  — ${e.message}`);
      failed++;
    }
  }

  console.log(`\nDone. ${got} downloaded, ${skipped} skipped, ${failed} failed${CLEAN ? `, ${cleaned} placeholders cleaned` : ''}.`);
  console.log('Next: in Unity run  Project Ascendant → Bind Pokémon Portraits  to point species.Portrait at the new dex sprites.');
  if (!CLEAN) console.log('Then re-run with --clean (or delete the plain <Name>.png placeholders) once binding is confirmed.');
})();
