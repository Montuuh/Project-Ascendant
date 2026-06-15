// fetch-ui-icons.js — toolbar / nav / action icons from Tabler (MIT, open-licensed).
//   node docs/scripts/fetch-ui-icons.js [--force]
const fs = require('fs'); const path = require('path');
if (typeof fetch !== 'function') { console.error('Node 18+ required.'); process.exit(1); }
const ROOT = path.join(__dirname, '..', '..', 'Assets', 'Sprites', 'UI', 'Icons');
const FORCE = process.argv.includes('--force');
// try outline path first, then the flat legacy path
const URLS = (n) => [
  `https://raw.githubusercontent.com/tabler/tabler-icons/main/icons/outline/${n}.svg`,
  `https://raw.githubusercontent.com/tabler/tabler-icons/main/icons/${n}.svg`,
];
const SETS = {
  Toolbar: { bag:'backpack', team:'users', pokedex:'book', settings:'settings', pause:'player-pause', map:'map' },
  Nav: { back:'arrow-left', confirm:'check', close:'x', drag:'grip-vertical', search:'search', filter:'filter', lock:'lock', info:'info-circle' },
  Action: { use:'hand-finger', equip:'plug', unequip:'unlink', swap:'arrows-exchange', evolve:'arrow-up-circle', teach:'school', catch:'target-arrow', release:'door-exit', heal:'heart-plus', buy:'shopping-bag' },
};
(async () => {
  let got=0,skip=0,fail=0;
  for (const [family, map] of Object.entries(SETS)) {
    const dir = path.join(ROOT, family); fs.mkdirSync(dir, { recursive: true });
    for (const [label, tabler] of Object.entries(map)) {
      const file = `icon-${family.toLowerCase()}-${label}.svg`;
      const out = path.join(dir, file);
      if (fs.existsSync(out) && !FORCE) { console.log(`skip   ${family}/${file}`); skip++; continue; }
      let ok=false;
      for (const u of URLS(tabler)) {
        try { const r = await fetch(u); if (r.ok) { fs.writeFileSync(out, await r.text()); ok=true; break; } } catch {}
      }
      if (ok) { console.log(`saved  ${family}/${file}  (tabler:${tabler})`); got++; }
      else { console.error(`FAIL   ${family}/${file}  (tabler:${tabler} not found)`); fail++; }
      await new Promise(r=>setTimeout(r,80));
    }
  }
  console.log(`\nUI icons: ${got} saved, ${skip} skipped, ${fail} failed.`);
})();
