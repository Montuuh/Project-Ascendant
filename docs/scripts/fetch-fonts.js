// fetch-fonts.js — Baloo 2 (display) + Nunito (body), Google Fonts (OFL, open).
//   node docs/scripts/fetch-fonts.js [--force]
const fs = require('fs'); const path = require('path');
if (typeof fetch !== 'function') { console.error('Node 18+ required.'); process.exit(1); }
const DEST = path.join(__dirname, '..', '..', 'Assets', 'Fonts');
const FORCE = process.argv.includes('--force');
const G = 'https://raw.githubusercontent.com/google/fonts/main/ofl';
const FONTS = [
  { out: 'Baloo2-VariableFont_wght.ttf',  url: `${G}/baloo2/Baloo2%5Bwght%5D.ttf` },
  { out: 'Nunito-VariableFont_wght.ttf',   url: `${G}/nunito/Nunito%5Bwght%5D.ttf` },
  { out: 'Nunito-Italic-VariableFont_wght.ttf', url: `${G}/nunito/Nunito-Italic%5Bwght%5D.ttf` },
  { out: 'OFL-Baloo2.txt',  url: `${G}/baloo2/OFL.txt` },
  { out: 'OFL-Nunito.txt',  url: `${G}/nunito/OFL.txt` },
];
(async () => {
  fs.mkdirSync(DEST, { recursive: true });
  let got=0,skip=0,fail=0;
  for (const f of FONTS) {
    const out = path.join(DEST, f.out);
    if (fs.existsSync(out) && !FORCE) { console.log(`skip   ${f.out}`); skip++; continue; }
    try {
      const r = await fetch(f.url); if (!r.ok) throw new Error(`HTTP ${r.status}`);
      fs.writeFileSync(out, Buffer.from(await r.arrayBuffer()));
      console.log(`saved  ${f.out}`); got++; await new Promise(r=>setTimeout(r,100));
    } catch (e) { console.error(`FAIL   ${f.out} — ${e.message}`); fail++; }
  }
  console.log(`\nFonts: ${got} saved, ${skip} skipped, ${fail} failed.`);
})();
