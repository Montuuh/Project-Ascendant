# Project Ascendant

A fan-made Unity roguelike deckbuilder. Three active Pokémon each contribute 4 moves to a shared hand. The Lead/Swap action-economy tension is the signature moment-to-moment mechanic.

> Portfolio piece. Not affiliated with Nintendo or Game Freak.

---

## Unity Setup

| Requirement | Version |
|---|---|
| Unity | **6000.4.6f1** (Unity 6 LTS) |
| Render Pipeline | URP 2D |
| Input System | New Input System 1.19.0 |
| Scripting Backend | Mono (Editor) / IL2CPP (Builds) |

1. Install **Unity 6000.4.6f1** via [Unity Hub](https://unity.com/download).
2. Clone this repo:
   ```
   git clone <repo-url>
   cd ProjectAscendant
   git lfs pull
   ```
3. Open the project folder in Unity Hub.
4. All required packages install automatically from `Packages/manifest.json`.
5. Open `Assets/Scenes/Boot.unity` — press Play to run.

---

## Project Structure

```
Assets/
  Scripts/
    Core/         ← EventBus, HSM, Factory, GameRNG, SaveSystem
    Combat/       ← Combat loop, damage, intents, status conditions
    Deck/         ← DeckManager, Hand, DiscardPile
    Progression/  ← XP, evolution, TMs, abilities
    Map/          ← RegionMap, NodeController, LoadoutManager
    Roguelike/    ← MetaProgression, TrainerHub, RelicManager
    UI/           ← View-layer MonoBehaviours only
  ScriptableObjects/  ← All game data (PokemonSpeciesSO, MoveSO, etc.)
  Scenes/             ← Boot, MainMenu, Hub, Combat, Map
  Tests/
    EditMode/     ← NUnit logic tests (no MonoBehaviour)
    PlayMode/     ← Integration tests (scene required)
```

## Branch Policy

- Trunk-based development on `main`.
- Never force-push `main`.
- See [CONTRIBUTING.md](CONTRIBUTING.md) for full contribution guide.

## GDD

The Game Design Document lives in Notion. See `.claude/skills/project-ascendant-gdd/SKILL.md`
for the workflow. All 10 GDD topics are locked.

To snapshot GDD topics to local markdown:
```
NOTION_TOKEN=<your_token> node docs/scripts/export-gdd.js
```
