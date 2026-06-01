---
globs: ["assets/data/**/*.json", "Assets/ScriptableObjects/**/*.asset"]
---
# Data Asset Rules

- JSON data files: kebab-case filenames (e.g., squirtle-base.json)
- ScriptableObject .asset files: PascalCase (e.g., Squirtle_Base.asset)
- All numeric balance values must be explicitly commented with GDD section
  reference where the value was specified or approved.
- Never duplicate data between a ScriptableObject and a JSON file.
  ScriptableObjects are the runtime source; JSON is for tooling only.
