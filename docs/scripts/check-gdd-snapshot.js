/**
 * check-gdd-snapshot.js
 * Returns exit 0 if docs/gdd was exported today (local calendar date).
 * Exit 1 if stale or missing status. Exit 2 if no topic files exist.
 *
 * Usage: node docs/scripts/check-gdd-snapshot.js [--quiet]
 */

const fs = require("fs");
const path = require("path");

const GDD_DIR = path.join(__dirname, "..", "gdd");
const STATUS_PATH = path.join(GDD_DIR, "snapshot-status.json");
const quiet = process.argv.includes("--quiet");

function localCalendarDate(d = new Date()) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function log(msg) {
  if (!quiet) console.log(msg);
}

function main() {
  const today = localCalendarDate();
  const topicFiles = fs.existsSync(GDD_DIR)
    ? fs.readdirSync(GDD_DIR).filter((f) => /^topic-\d+-.+\.md$/.test(f))
    : [];

  if (topicFiles.length === 0) {
    log("❌  No GDD topic files in docs/gdd/. Run: node docs/scripts/export-gdd.js");
    process.exit(2);
  }

  if (!fs.existsSync(STATUS_PATH)) {
    log(`⚠️  Missing ${path.relative(process.cwd(), STATUS_PATH)}`);
    log(`   Treating snapshot as STALE (no export recorded for today: ${today}).`);
    log("   Run: node docs/scripts/ensure-gdd-snapshot.js");
    process.exit(1);
  }

  let status;
  try {
    status = JSON.parse(fs.readFileSync(STATUS_PATH, "utf8"));
  } catch (e) {
    log(`❌  Invalid JSON in snapshot-status.json: ${e.message}`);
    process.exit(1);
  }

  const exportDate = status.exportCalendarDate;
  if (exportDate === today) {
    log(`✅  GDD snapshot fresh (exported ${exportDate}, ${status.topicCount ?? "?"} topics).`);
    process.exit(0);
  }

  log(`⚠️  GDD snapshot STALE. Last export: ${exportDate ?? "unknown"} — today: ${today}.`);
  log("   Run: node docs/scripts/ensure-gdd-snapshot.js");
  log("   Or:  node docs/scripts/export-gdd.js  (requires NOTION_TOKEN in .env)");
  process.exit(1);
}

main();
