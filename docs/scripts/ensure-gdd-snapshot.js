/**
 * ensure-gdd-snapshot.js
 * If today's snapshot is missing or stale, re-export GDD from Notion.
 * Loads NOTION_TOKEN from .env when present.
 *
 * Usage: node docs/scripts/ensure-gdd-snapshot.js
 */

const { spawnSync } = require("child_process");
const fs = require("fs");
const path = require("path");

require("dotenv").config({ path: path.resolve(__dirname, "../../.env") });

const scriptsDir = __dirname;
const check = spawnSync(process.execPath, [path.join(scriptsDir, "check-gdd-snapshot.js"), "--quiet"], {
  encoding: "utf8",
});

if (check.status === 0) {
  console.log("✅  GDD snapshot already fresh for today — no export needed.");
  process.exit(0);
}

if (!process.env.NOTION_TOKEN) {
  console.error("❌  GDD snapshot is stale and NOTION_TOKEN is not set.");
  console.error("    Add NOTION_TOKEN to .env, then re-run this script.");
  console.error("    Or run export manually after configuring Notion access.");
  process.exit(1);
}

console.log("📥  Snapshot stale — exporting GDD from Notion...\n");

const exp = spawnSync(process.execPath, [path.join(scriptsDir, "export-gdd.js")], {
  stdio: "inherit",
  env: process.env,
});

process.exit(exp.status ?? 1);
