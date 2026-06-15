// fetch-vs-boxicons.js — tiny box/menu sprites for the map (32×32) + Box list.
// Source: msikma/pokesprite (name-keyed box sprites). Unity-ready PNGs.
//   node docs/scripts/fetch-vs-boxicons.js [--force]
const fs = require('fs'); const path = require('path');
if (typeof fetch !== 'function') { console.error('Node 18+ required.'); process.exit(1); }
const DEST = path.join(__dirname, '..', '..', 'Assets', 'Sprites', 'VS', 'BoxIcons');
const FORCE = process.argv.includes('--force');
const URL = (n) => `https://raw.githubusercontent.com/msikma/pokesprite/master/pokemon-gen8/regular/${n}.png`;
const ROSTER = { Bulbasaur:1,Ivysaur:2,Venusaur:3,Charmander:4,Charmeleon:5,Charizard:6,Squirtle:7,Wartortle:8,Blastoise:9,Caterpie:10,Metapod:11,Butterfree:12,Pidgey:16,Pidgeotto:17,Pidgeot:18,Geodude:74,Graveler:75,Golem:76 };
(async () => {
  fs.mkdirSync(DEST, { recursive: true });
  let got=0,skip=0,fail=0;
  for (const [name,id] of Object.entries(ROSTER)) {
    const file = `${String(id).padStart(3,'0')}-${name}.png`;
    const out = path.join(DEST, file);
    if (fs.existsSync(out) && !FORCE) { console.log(`skip   ${file}`); skip++; continue; }
    try {
      const res = await fetch(URL(name.toLowerCase()));
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      fs.writeFileSync(out, Buffer.from(await res.arrayBuffer()));
      console.log(`saved  ${file}`); got++; await new Promise(r=>setTimeout(r,120));
    } catch (e) { console.error(`FAIL   ${file} — ${e.message}`); fail++; }
  }
  console.log(`\nBox icons: ${got} saved, ${skip} skipped, ${fail} failed.`);
})();
