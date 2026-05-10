# DynamicScoreBug

A custom broadcast-style scoreboard mod for [Puck](https://store.steampowered.com/app/2660980/Puck/). Replaces the default in-game scorebug with a configurable scoreboard that supports team logos, custom colors, goal/win animations, period and game summaries, save-percentage popups, shootout tracking, three-stars, and integration with [ToasterReskinLoader](https://github.com/) for shared team-color theming.

## Features

- **Broadcast scorebug** — score, period, time, team logos, league logo, customizable colors and gradients.
- **Goal animations** — team-color goal overlay, hat-trick detection, OT win animation, shutout/regular-win banner.
- **Popups** — shot speed (per team), save % (per team or both), lineup, scoring summary, period summary, end-of-game summary with player stats.
- **Shootout support** — separate scoreboard mode with shooter order, hits/misses tracking, win banner.
- **Three stars** — listens for native and oomtm450_stats announcements.
- **Settings UI** — in-game config panel via the unified ModMenuHub button. General, Advanced, Presets, Tests tabs; live hot-reload of colors/logos.
- **Preset system** — share JSON preset packs of team configs; export/import via the UI.
- **TRL color sync** — when ToasterReskinLoader is installed, scoreboard colors push through to equipment colors (sticks, helmets, jerseys).
- **External Stats mod integration** — when `oomtm450_stats` is installed, the advanced summary panel surfaces hits, passes, takeaways, plus/minus, time on ice, save details.

## Building

This project references the game's compiled assemblies (Unity engine modules, Puck.dll, etc.) which are **not** redistributed in this repo. You need a local Puck installation to build.

1. Install [.NET SDK 9](https://dotnet.microsoft.com/download).
2. Clone this repo.
3. Copy the contents of your Puck Managed folder into `libs/`:
   ```
   "C:\Program Files (x86)\Steam\steamapps\common\Puck\Puck_Data\Managed\*"  →  libs\
   ```
   On Windows PowerShell:
   ```powershell
   New-Item -ItemType Directory libs -Force
   Copy-Item "C:\Program Files (x86)\Steam\steamapps\common\Puck\Puck_Data\Managed\*" libs\ -Force
   ```
4. Build:
   ```
   dotnet build
   ```
   The output DLL is written directly into `Plugins\ScoreBug\` inside your Puck install (configured via `OutputPath` in `Scoreboard.csproj`). Adjust the `OutputPath` if your install lives elsewhere.

## Installing for users (no build needed)

1. Grab `Scoreboard.dll` from a [Releases](../../releases) build (when published).
2. Drop it into `<Puck install>\Plugins\ScoreBug\`.
3. Drop `CustomScoreboard.json` and `ScoreboardPresets.json` into `<Puck install>\config\ModHub\Scoreboard\` (or let the mod create them on first run).
4. Optional: copy `scorebuglogos\*.png` into `<Puck install>\config\ModHub\Scoreboard\scorebuglogos\` for default team logos.
5. Optional: install [ToasterReskinLoader](https://github.com/) for synced equipment colors.
6. Optional: install `oomtm450_stats` for advanced player-stat columns.

## Configuration

Two JSON files live in `<Puck install>\config\ModHub\Scoreboard\`:

- **`CustomScoreboard.json`** — main config (team names, colors, layout, animations, fonts, popups).
- **`ScoreboardPresets.json`** — saved team/size presets you can apply from the UI.

Both auto-migrate from older locations and reload at runtime. The in-game settings UI (opened via the `Modifications` button in the main/pause menu) provides a friendlier editor.

## Chat commands

While in a game:

| Command | Effect |
|---|---|
| `/shotspeed` | Test shot-speed popup |
| `/shotspeedb` / `/shotspeedr` | Show last blue / red shot speed |
| `/save%` | Show goalie save % for both teams |
| `/save%b` / `/save%r` | Save % for one team |
| `/lineup` | Test lineup popup |
| `/summarys` | Test scoring summary |
| `/summaryp` | Test period summary |
| `/summaryg` | Test end-of-game summary |

These are intercepted client-side and never sent to chat.

## Project layout

```
src/
  Client.Entry*.cs              — IPuckMod entry, Harmony patches, chat command intercept
  CustomBroadcastScoreboard*.cs — main scoreboard MonoBehaviour (UI creation, events, animations,
                                  popups, summaries, shootout, stats integration)
  ScoreboardConfig.cs           — config schema and path resolution
  ScoreboardUIManager*.cs       — in-game settings UI (tabs, controls, presets, search, export)
  ModMenuHub.cs                 — shared menu button hub used by Ponce mods
  Integration/
    ToasterReskinLoaderColorSync.cs — reflection-based bridge to TRL
```

## Status

Targets Puck 3.10 (game version 2026-05-07). Migrated from the 202 → 310 API. See `src/PUCK_202_TO_310_MIGRATION_GUIDE.md` for the migration notes used during the port.

## License

See [LICENSE](LICENSE).
