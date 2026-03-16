# match-card-unity

Memory card matching game built in Unity.

## Overview

This project implements a menu-driven memory card matching game with dynamic board layouts, save/load support, score tracking, audio feedback, and a TextMeshPro-based UI.

## Features

- Menu screen with `Continue`, layout selection, clear-save, and quit options
- Dynamic board layouts: `2x2`, `2x3`, `4x4`, `5x6`
- Card flip, match, mismatch, scoring, save/load, and game-over flow
- Audio feedback for flip, match, mismatch, and game completion
- TextMeshProUGUI across project UI and prefabs

## Architecture

The runtime is organized around a small set of focused systems:

- `GameManager`
  - Entry point for the playable flow
  - Creates the UI, controls menu state, starts new games, restores saved games, resolves selections, updates score text, and triggers game-over behavior
- `BoardManager`
  - Builds and rebuilds the board inside the board container
  - Creates card instances, applies the selected layout, sizes cards for the available space, and preserves deck ordering for save/load
- `CardView`
  - View/controller for an individual card
  - Stores runtime card data, handles placeholder flip animation, face visibility, interactivity, and matched-state visuals
- `ScoreManager`
  - Keeps score state isolated from UI
  - Applies `+100` for matches, `-10` for mismatches, and supports a simple combo multiplier
- `AudioManager`
  - Loads and plays the four required sound effects from `Resources/Audio`
- `SaveSystem`
  - Serializes and restores game state to JSON in `Application.persistentDataPath`
- `GameSaveData`
  - Serializable data transfer object for board layout, score, deck order, and matched card indices

## Script Responsibilities

- `Assets/Scripts/GameManager.cs`
  - Menu flow, board session lifecycle, card selection pipeline, save/load orchestration
- `Assets/Scripts/BoardManager.cs`
  - Deck creation, card spawning, `GridLayoutGroup` configuration, responsive cell sizing
- `Assets/Scripts/CardView.cs`
  - Card label setup, flip animation, matched-state appearance, button wiring
- `Assets/Scripts/ScoreManager.cs`
  - Score and combo logic
- `Assets/Scripts/AudioManager.cs`
  - Flip, match, mismatch, and game-over audio playback
- `Assets/Scripts/SaveSystem.cs`
  - JSON file persistence helpers
- `Assets/Scripts/GameSaveData.cs`
  - Saved snapshot structure
- `Assets/Scripts/GameLayoutView.cs`
  - References to the root UI regions used by `GameManager`
- `Assets/Scripts/BoardLayoutPreset.cs`
  - Supported board presets
- `Assets/Scripts/BoardLayoutDefinition.cs`
  - Rows, columns, pair counts, and preset-to-layout mapping
- `Assets/Scripts/CardDefinition.cs`
  - Immutable runtime card identity and symbol data
- `Assets/Scripts/BoardAreaLayoutWatcher.cs`
  - Triggers board cell recalculation when the container size changes

## Gameplay Flow

1. `GameManager` creates the menu overlay on top of the game layout prefab.
2. The player either starts a fresh game or continues from the saved JSON snapshot.
3. `BoardManager` generates a shuffled deck and spawns cards into a responsive grid.
4. `CardView` handles flip presentation while `GameManager` evaluates pair selection.
5. `ScoreManager` updates score on match or mismatch.
6. `SaveSystem` stores board state after progress changes.
7. When all cards are matched, the game-over sound plays and the menu is shown again.

## UI

- Core layout uses Unity UI
- All project text uses `TextMeshProUGUI`
- Menu UI is created at runtime by `GameManager`
- Card and game layout structure are stored in prefabs under `Assets/Prefabs`

## Save Data

Saved JSON contains:

- Selected board layout
- Current score
- Matched card indices
- Pair ID order for the shuffled deck
- Card symbols used by the current board

Save file location:

- `Application.persistentDataPath/memory-card-save.json`

## Audio

The project includes four required effects:

- `card-flip`
- `match`
- `mismatch`
- `game-over`

## Project Structure

- `Assets/`
  - `Prefabs/`
  - `Resources/Audio/`
  - `Scenes/`
  - `Scripts/`
  - `UI/`
- `Packages/`
- `ProjectSettings/`
