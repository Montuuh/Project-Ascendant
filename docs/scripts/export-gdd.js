/**
 * export-gdd.js
 * Snapshots all Project Ascendant GDD topic pages from Notion to
 * docs/gdd/topic-N-<slug>.md. Run on demand after a topic locks.
 *
 * Usage:
 *   npm install -g notion-to-md @notionhq/client
 *   NOTION_TOKEN=<your_integration_token> node docs/scripts/export-gdd.js
 *
 * Output: docs/gdd/topic-1-game-overview.md ... topic-10-art-ui-audio.md
 * Then:   git add docs/gdd && git commit -m "docs: snapshot GDD — <topics>"
 *
 * The exported files are READ-ONLY ARCHIVES. Do not edit them directly.
 * All canonical edits go through Notion. See .claude/skills/project-ascendant-gdd/
 */

const { Client } = require("@notionhq/client");
const { NotionToMarkdown } = require("notion-to-md");
const fs = require("fs");
const path = require("path");

const NOTION_TOKEN = process.env.NOTION_TOKEN;
if (!NOTION_TOKEN) {
  console.error("Error: NOTION_TOKEN environment variable is not set.");
  console.error("Get your token from: notion.so → Settings → Connections → your integration");
  process.exit(1);
}

const notion = new Client({ auth: NOTION_TOKEN });
const n2m = new NotionToMarkdown({ notionClient: notion });

// Topic pages in order. Update page IDs here if pages are ever recreated.
const TOPICS = [
  { n: 1, slug: "game-overview",          id: "3610450715b481a287bdd5c72573b9d7" },
  { n: 2, slug: "core-gameplay-loop",     id: "3610450715b481048a3bd46eb1d31a07" },
  { n: 3, slug: "micro-loop",             id: "3610450715b481e08404ded0b96924c9" },
  { n: 4, slug: "combat-system",          id: "3610450715b4818bb876f6d9fd5d2ab0" },
  { n: 5, slug: "progression",            id: "3610450715b4813ea29ae0c992898d01" },
  { n: 6, slug: "roguelike-progression",  id: "3610450715b4816c83d2c74682cef77c" },
  { n: 7, slug: "scenario-nodes",         id: "3610450715b48146b3a0fe94ca2bd05c" },
  { n: 8, slug: "items-relics",           id: "3610450715b48173bab9e5239b63f813" },
  { n: 9, slug: "technical-architecture", id: "3610450715b4811b83cae23d6ed2a154" },
  { n: 10, slug: "art-ui-audio",          id: "3610450715b4815192fae42ed745b3d0" },
];

const OUT_DIR = path.join(__dirname, "..", "gdd");

async function exportTopic(topic) {
  const filename = `topic-${topic.n}-${topic.slug}.md`;
  const outPath = path.join(OUT_DIR, filename);

  try {
    // Fetch page metadata to get the real last-edit time from Notion.
    // Using this (not new Date()) means the header only changes when
    // someone actually edited the page — unchanged files stay identical
    // in git across consecutive export runs.
    const pageMeta = await notion.pages.retrieve({ page_id: topic.id });
    const lastEdited = pageMeta.last_edited_time; // ISO-8601 from Notion

    const mdBlocks = await n2m.pageToMarkdown(topic.id);
    const mdString = n2m.toMarkdownString(mdBlocks);

    // Two-line header only — no run-time timestamp.
    // Files will only diff in git when Notion content actually changed.
    const header = [
      `<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->`,
      `<!-- Last updated from Notion: ${lastEdited} -->`,
      ``,
    ].join("\n");

    fs.writeFileSync(outPath, header + mdString.parent, "utf8");
    console.log(`✅  Exported: ${filename}  (Notion last edited: ${lastEdited})`);
  } catch (err) {
    console.error(`❌  Failed to export topic ${topic.n} (${topic.slug}):`, err.message);
  }
}

async function main() {
  if (!fs.existsSync(OUT_DIR)) {
    fs.mkdirSync(OUT_DIR, { recursive: true });
    console.log(`Created output directory: ${OUT_DIR}`);
  }

  console.log(`Exporting ${TOPICS.length} GDD topics from Notion...\n`);

  for (const topic of TOPICS) {
    await exportTopic(topic);
  }

  console.log(`\nDone. Commit with:`);
  console.log(`  git add docs/gdd && git commit -m "docs: snapshot GDD — <describe what changed>"`);
}

main();
