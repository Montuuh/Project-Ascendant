// style-type-icons.js
// Adapts the imported type-icon SVGs to the project style, IN PLACE:
//   1) recolours the white glyph fill to the type's own palette colour (§10.1.3.2)
//   2) recentres the glyph by retargeting the viewBox to the glyph's bounding box
//      (square, centred, with padding) so every type icon fills its box evenly.
// The glyph ARTWORK is never redrawn — only the `fill` colour and `viewBox` change.
//
// Usage:
//   npm i -D svg-path-bounds
//   node docs/scripts/style-type-icons.js
//
// Tip: commit the Type/ folder first so you can diff / revert.

const fs = require('fs');
const path = require('path');

let pathBounds;
try {
  pathBounds = require('svg-path-bounds');
} catch {
  console.error('Missing dependency. Run:  npm i -D svg-path-bounds');
  process.exit(1);
}

const DIR = path.join(__dirname, '..', '..', 'Assets', 'Sprites', 'UI', 'Icons', 'Type');
const PAD = 0.12; // breathing room around the glyph (12% of its longest side)

// Canonical type palette (§10.1.3.2) + later-gen types the imported set includes.
const COLORS = {
  normal: '#A8A878', fire: '#F08030', water: '#6890F0', electric: '#F8D030',
  grass: '#78C850', ice: '#98D8D8', fighting: '#C03028', poison: '#A040A0',
  ground: '#E0C068', flying: '#A890F0', psychic: '#F85888', bug: '#A8B820',
  rock: '#B8A038', ghost: '#705898', dragon: '#7038F8',
  dark: '#705848', steel: '#B8B8D0', fairy: '#EE99AC',
};

let done = 0;
for (const [type, color] of Object.entries(COLORS)) {
  const file = path.join(DIR, `${type}.svg`);
  if (!fs.existsSync(file)) continue;
  let svg = fs.readFileSync(file, 'utf8');

  // 1) recolour: white fills -> type colour (leaves fill="none" untouched)
  svg = svg.replace(/fill\s*=\s*"(#fff(?:fff)?|white)"/gi, `fill="${color}"`);

  // 2) recentre: union bbox of every path's d -> centred square viewBox
  const ds = [...svg.matchAll(/\sd\s*=\s*"([^"]+)"/g)].map((m) => m[1]);
  if (ds.length) {
    let L = Infinity, T = Infinity, R = -Infinity, B = -Infinity;
    for (const d of ds) {
      try {
        const [l, t, r, b] = pathBounds(d);
        L = Math.min(L, l); T = Math.min(T, t); R = Math.max(R, r); B = Math.max(B, b);
      } catch { /* skip unparseable path */ }
    }
    if (isFinite(L)) {
      const w = R - L, h = B - T, side = Math.max(w, h);
      const pad = side * PAD;
      const box = side + pad * 2;
      const cx = (L + R) / 2, cy = (T + B) / 2;
      const vb = `${(cx - box / 2).toFixed(2)} ${(cy - box / 2).toFixed(2)} ${box.toFixed(2)} ${box.toFixed(2)}`;
      svg = svg.replace(/viewBox\s*=\s*"[^"]*"/, `viewBox="${vb}"`);
      svg = svg
        .replace(/(<svg[^>]*?)\swidth\s*=\s*"[^"]*"/, '$1 width="32"')
        .replace(/(<svg[^>]*?)\sheight\s*=\s*"[^"]*"/, '$1 height="32"');
      if (!/preserveAspectRatio\s*=/.test(svg)) {
        svg = svg.replace('<svg', '<svg preserveAspectRatio="xMidYMid meet"');
      }
    }
  }

  fs.writeFileSync(file, svg);
  console.log(`styled  ${type}.svg  ->  ${color}`);
  done++;
}
console.log(`\nDone. ${done} type icon(s) recoloured + recentred.`);
