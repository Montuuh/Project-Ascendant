# Background Generation Prompts — 8 Scenes (Gemini / Imagen)

> For the backgrounds in `06 §D` + the Gym arena. **Art style: Modern Illustrated**, sharpened toward
> the bright cel-shaded **adventure-RPG environment look** — the cosy, rounded, saturated "diorama
> towns and routes" feeling of a classic handheld creature-collecting game. Paired with PokéAPI
> creature renders (`08`). Tuned for **Google AI Pro** (Gemini app; Imagen 3 / 2.5 Flash Image). Each
> scene is a **complete, standalone, copy-paste prompt** — style repeated in full every time.
> Target **16:9, 1920×1080**.
>
> **What changed:** the style language now leads with concrete *environment* descriptors (trimmed
> route grass with darker tall-grass patches, low ledges, wooden signposts, winding paths, tidy towns
> with brightly coloured pitched rooftops, the red-roofed healing centre, the grand gym building) and
> the warm cel-shaded diorama rendering. These are **place/architecture and art-style** cues only —
> no branded characters, no creatures in the plate, no text. Your creatures come from the sprite
> pipeline (`08`) and layer on top.

---

## The environment style anchor (baked into every prompt below)

> Bright, cheerful, polished cel-shaded JRPG environment art in the wholesome all-ages style of a
> classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
> saturated friendly colours, soft rounded toy-like geometry, gentle warm global illumination, cosy
> miniature-diorama charm. Outdoor scenes add the genre's world-building: neatly trimmed grass with
> darker square-ish patches of tall wild grass, low stone ledges, wooden signposts and fences, winding
> dirt paths, tidy towns with brightly coloured pitched rooftops, flower beds.

---

## Workflow for a consistent set (read once)

1. **Generate #6 (Main-Menu vista) FIRST** as the *style key* — it carries the full environment look.
   Iterate until palette, linework, stylisation, and warmth are right.
2. For the rest, in the Gemini app **attach the approved key image** and add: *"Match the exact art
   style, colour palette, linework, cel-shading, level of stylisation, and lighting of the attached
   reference image."* (Or use **Whisk** for image-to-image style transfer.)
3. Always request **"16:9 widescreen, 1920×1080."** Crop if it returns square, preserving calm UI zones.
4. **Budget:** all scenes free in the Gemini app; keep the **$10 API credit** for batch icon/move-art.
5. Outputs carry a SynthID watermark and are usable per Google's terms.

---

## #6 — Main-Menu vista  ← GENERATE FIRST (style key)

**UI on top:** title lockup upper third, button stack centre-left — keep those calmer.

```
Bright, cheerful, polished cel-shaded JRPG environment art in the wholesome all-ages style of a classic
handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading, saturated
friendly colours, soft rounded toy-like geometry, gentle warm global illumination, cosy miniature-
diorama charm. A sweeping golden-hour establishing vista of an adventure region: rolling green hills of
neatly trimmed grass with darker square-ish patches of tall wild grass, a winding dirt path with low
stone ledges and wooden signposts leading toward a cosy distant town of brightly coloured pitched
rooftops — among them a red-roofed healing centre and a small shop — flower beds, a sparkling stream,
a far blue mountain under big soft drifting clouds. Strong layered atmospheric depth for a parallax
title screen; calm open sky across the upper third and a calmer left-centre area for menu UI. Wholesome
and bright, never grim. No creatures, no characters, no text, no words, no logos, no UI. 16:9
widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #1 — Combat backdrop · Region 1 (route)

**UI on top:** enemy zone (top 40%), team zone (mid), card tray (bottom). Keep centre + lower third calm.

```
Bright, cheerful, polished cel-shaded JRPG environment art in the wholesome all-ages style of a classic
handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading, saturated
friendly colours, soft rounded toy-like geometry, gentle warm light, cosy diorama charm. A sunny
outdoor adventure route clearing: neatly trimmed grass with thick darker square-ish patches of tall
wild grass clustered along the left and right foreground edges, low stone ledges, a wooden signpost and
fence, friendly leafy trees and wildflowers framing the sides, a worn dirt path, a calm pastel sky with
soft clouds. Wide establishing battlefield view with an open, uncluttered grassy clearing across the
centre and foreground; far background softly blurred into atmospheric parallax layers. Keep the centre
and lower third low-contrast and calm so overlaid battle sprites and a card tray read clearly. Whole-
some and bright. No creatures, no characters, no text, no UI. 16:9 widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #2 — Map backdrop · Region 1

**UI on top:** team/Box panel (left third), node graph (right two-thirds), utility bar (bottom). Keep an
even, focal-point-free field.

```
Bright, cheerful, polished cel-shaded JRPG overworld environment art in the wholesome all-ages style of
a classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
saturated friendly colours, soft rounded toy-like geometry, gentle warm light, cosy miniature-diorama
charm. A cosy adventure region from a gentle elevated three-quarter angle: winding dirt paths with low
ledges and wooden signposts threading through warm meadows of trimmed grass dotted with darker square-
ish patches of tall wild grass, small groves, a sparkling stream, and friendly landmark buildings
nestled in — a red-roofed healing centre, a little shop, and a grand gym building with banners and
brightly coloured pitched rooftops, flower beds. Even, calm, map-like composition with no single strong
focal subject, soft and uncluttered so a branching path-and-node graph and side panels overlay clearly.
Like a charming friendly diorama world map. Wholesome and bright, gentle depth. No creatures, no
characters, no text, no UI. 16:9 widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #3 — Healing Center interior  (the warmest scene — sets the Pillar-5 tone)

**UI on top:** team row + service buttons centre/lower. Keep central floor calm.

```
Bright, cheerful, polished cel-shaded JRPG interior environment art in the wholesome all-ages style of
a classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
saturated friendly colours, soft rounded toy-like geometry, gentle warm golden afternoon light, cosy
diorama charm. The iconic interior of a friendly creature healing centre: a welcoming curved reception
counter with a softly glowing rounded healing machine behind it, a warm cream-and-red colour scheme
(matching the red roof outside), rounded wooden-and-pastel furniture, potted plants, big warm windows,
a soft rug, a cosy waiting nook. Restful and safe, with uncluttered central floor space — the visual
embodiment of "you are safe and cared for here." Wholesome and bright. No creatures, no characters, no
text, no UI. 16:9 widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #4 — Item shop interior

**UI on top:** stock grid centre. Keep centre calm.

```
Bright, cheerful, polished cel-shaded JRPG interior environment art in the wholesome all-ages style of
a classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
saturated friendly colours, soft rounded toy-like geometry, gentle warm lantern light, cosy diorama
charm. A friendly little adventure-supply shop interior: rounded wooden shelves stocked with colourful
potions, bottles and wrapped boxes (no labels, no text), a welcoming sales counter, hanging plants,
warm market-stall cosiness, brightly coloured walls. Cheerful and inviting, gently detailed at the
edges but with a calm, uncluttered central area for product UI cards. Wholesome and bright. No
creatures, no characters, no text, no UI. 16:9 widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #5 — Dojo / Tutor interior

**UI on top:** target Pokémon + teachable list centre. Keep centre calm.

```
Bright, cheerful, polished cel-shaded JRPG interior environment art in the wholesome all-ages style of
a classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
saturated friendly colours, soft rounded toy-like geometry, gentle warm sunbeams, cosy diorama charm.
A bright, warm move-training hall: polished wooden floor with a painted practice ring, soft paper-screen
shoji walls, hanging plain banners (no text), rounded training dummies and cushions, warm sunlight
streaming through windows. Calm and focused but cheerful and bright, not austere; open uncluttered
central floor. Wholesome and warm. No creatures, no characters, no text, no UI. 16:9 widescreen,
1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #7 — Trainer Hub kiosk room

**UI on top:** two kiosk cards side-by-side, centre. Keep centre calm.

```
Bright, cheerful, polished cel-shaded JRPG interior environment art in the wholesome all-ages style of
a classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
saturated friendly colours, soft rounded toy-like geometry, gentle warm golden lamp light, cosy diorama
charm. A cosy personal trainer's room: a warm rug, a couple of friendly rounded standing kiosk /
terminal stations against the walls, soft shelves displaying trophies and a tidy badge-collection case,
a big window with warm light, plants, and brightly coloured cosy furnishings — a lived-in, inviting
home base. Lounge-like and personal, with an uncluttered centre so two kiosk UI cards overlay cleanly.
Wholesome and bright. No creatures, no characters, no text, no UI. 16:9 widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

## #8 — Gym arena  (the Region climax / boss stage)

**UI on top:** boss zone (top), team zone (mid), card tray (bottom). Keep centre + lower third calm; the
drama lives in the upper background.

```
Bright, dramatic, polished cel-shaded JRPG interior environment art in the wholesome all-ages style of
a classic handheld creature-collecting adventure game — clean crisp linework, smooth cel-shading,
saturated colours, bold directional spotlights, cosy-but-epic diorama charm. A grand indoor gym battle
arena: a wide painted battle floor with arena markings, tall pillars and a high vaulted ceiling, a
type-themed colour motif, a large glowing gym-badge emblem high on the far wall, banners hanging (no
text), warm spotlights beaming down onto the floor. Epic and climactic but still bright and wholesome
rather than grim. Keep the centre battle floor and lower third calm and lower-contrast so battle sprites
and a card tray read clearly; the awe lives in the upper background architecture. No creatures, no
characters, no text, no UI. 16:9 widescreen, 1920x1080.
Avoid: text, words, letters, numbers, UI, buttons, HUD, watermark, logo, signature, creatures,
characters, people, photorealistic, 3D photoreal render, dark, gloomy, grim, scary, cluttered, harsh
shadows, neon, low quality, blurry, jpeg artifacts.
```

---

## After generation

- Save finals to `Assets/Sprites/UI/Backgrounds/` (kebab-case): `bg-mainmenu.png`, `bg-combat-r1.png`,
  `bg-map-r1.png`, `bg-center.png`, `bg-shop.png`, `bg-dojo.png`, `bg-hub.png`, `bg-gym-r1.png`. Import
  as Sprite (2D), no aggressive compression. Update **`08`** status.
- These environment plates are **creature-free on purpose** — the gameplay creatures (PokéAPI sprites,
  `08`) layer on top, so backgrounds + sprites read as one game without fighting for attention.
- Tip: if a scene doesn't feel stylised enough, append *"more stylised, chunkier rounded shapes, flatter
  cel-shaded colours, less realistic, more toy-diorama"* — that pushes it further toward the look.
- R2/R3 backdrops are out of VS (§10.11) — Region-1 only for now.
- If a scene reads too busy under the UI, append *"even calmer, lower contrast, more empty central
  space."* UI legibility wins (Pillar 5).
