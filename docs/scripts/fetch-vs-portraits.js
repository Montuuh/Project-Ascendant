// fetch-vs-portraits.js
// Downloads the VS-roster Pokémon "official-artwork" renders (illustrated lane,
// design/ui/08) into Assets/Sprites/VS/Portraits/<Name>.png from the public
// PokeAPI/sprites repo. This is a fetch tool — it pulls assets you've chosen to
// use into your fan project; it does not generate or modify the artwork.
//
// Usage:
//   node docs/scripts/fetch-vs-portraits.js          (skips files that already exist)
//   node docs/scripts/fetch-vs-portraits.js --force  (re-downloads everything)
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
const URL = (id) =>
  `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/${id}.png`;

// VS roster (PascalCase file name -> National Dex id), from Assets/ScriptableObjects/VS/Species/.
const ROSTER = {
  Bulbasaur: 1, Ivysaur: 2, Venusaur: 3,
  Charmander: 4, Charmeleon: 5, Charizard: 6,
  Squirtle: 7, Wartortle: 8, Blastoise: 9,
  Caterpie: 10, Metapod: 11, Butterfree: 12,
  Pidgey: 16, Pidgeotto: 17, Pidgeot: 18,
  Geodude: 74, Graveler: 75, Golem: 76,
};

(async () => {
  fs.mkdirSync(DEST, { recursive: true });
  let got = 0, skipped = 0, failed = 0;

  for (const [name, id] of Object.entries(ROSTER)) {
    const fileName = `${String(id).padStart(3, '0')}-${name}.png`;   // dex-number prefix
    const out = path.join(DEST, fileName);
    if (fs.existsSync(out) && !FORCE) { console.log(`skip   ${fileName} (exists)`); skipped++; continue; }
    try {
      const res = await fetch(URL(id));
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const buf = Buffer.from(await res.arrayBuffer());
      fs.writeFileSync(out, buf);
      console.log(`saved  ${fileName}  (${(buf.length / 1024).toFixed(0)} KB)`);
      got++;
      await new Promise((r) => setTimeout(r, 150)); // be polite
    } catch (e) {
      console.error(`FAIL   ${name}.png  — ${e.message}`);
      failed++;
    }
  }
  console.log(`\nDone. ${got} downloaded, ${skipped} skipped, ${failed} failed.`);
  console.log('Import in Unity as Sprite (2D); downscale to 96×96 (battle) / 32×32 (map) via import settings.');
})();
