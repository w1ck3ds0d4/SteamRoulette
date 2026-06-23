<p align="center">
  <strong>🎲 Steam Roulette</strong>
</p>

**Can't decide what to play? Let your Steam backlog pick for you.**

Steam Roulette is a small Windows desktop app that reads your Steam library and randomly chooses a game to play, with filters that actually fit a backlog: installed-only, never-played, "under N hours," "nothing I've touched in the last N days," and a mode that weights the roll toward games you've barely opened. One click launches the pick straight into Steam. It reads your full owned library (with playtime) when you add a Steam Web API key, and falls back to parsing your installed games locally with no key and no network.

Built with C# and .NET 10 (WPF). A pure `Core` library holds all the logic (Steam-file parsing, Web API, the roulette) with unit tests; the WPF project is a thin UI on top. No installer, no external services.

> **Heads up:** this is the first half of the project. A SteamAchievementManager-style achievement/stat editor is on the roadmap below. Editing achievements talks to your running Steam client for games you own and sits in a gray area of Steam's Subscriber Agreement, so it is deliberately kept separate and opt-in.

---

## Features (Built)

### Library loading
- **Web API source** — full owned-games list with playtime and last-played, via `IPlayerService/GetOwnedGames` (needs a free Web API key + your SteamID).
- **Local source** — installed games parsed from `libraryfolders.vdf` + `appmanifest_*.acf` across every Steam library folder. No key, fully offline.
- **Merged** — uses the Web API when configured (marking which titles are installed from local data), and falls back to local-only otherwise or if the API call fails.

### The roulette
- **🎲 Surprise me** — random pick from whatever matches your current filters.
- **Filters** — installed only · unplayed only · max hours · hide games played in the last N days · name search.
- **Favor my backlog** — weighted draw that biases toward games with little playtime, so the pile you never touch wins more often.
- **Launch in Steam** — fires `steam://run/<appid>` for the chosen game. Double-click any game in the list to make it the pick.

### Persistence
- Web API key, SteamID, and your last-used filters are saved to `%APPDATA%\SteamRoulette\settings.json`.

---

## Roadmap

- **Achievement manager (the SAM half)** — list a game's achievements/stats via the Steamworks SDK (`ISteamUserStats`) and unlock / lock / reset them for games you own, with Steam running.
- **Backlog ↔ achievement cross-over** — "pick a game I'm one achievement away from 100%-ing."
- **Richer library data** — genres/tags and store metadata for genre-based rolls.
- **Game art polish** — cached capsule art, a spin animation on the pick.

---

## Getting started

Requires the **.NET 10 SDK** on Windows (WPF is Windows-only).

```sh
# from the repo root
dotnet run --project src/SteamRoulette.App      # launch the app
dotnet test                                     # run the Core unit tests
```

On first run it lists your **installed** games. To roulette your whole owned library (with playtime filters), open **⚙ Settings** and add:
- a **Steam Web API key** — get one at <https://steamcommunity.com/dev/apikey>
- your **64-bit SteamID** — find it at <https://steamid.io>

## How it works

- `SteamRoulette.Core` — pure, testable logic:
  - `Steam/VdfParser` — minimal Valve KeyValues (VDF/ACF) parser, dependency-free.
  - `Steam/SteamPaths` — locates Steam (registry + defaults) and enumerates library folders.
  - `Steam/LocalLibrarySource` · `WebApiLibrarySource` · `LibraryLoader` — the three load paths.
  - `Roulette/GameRoulette` — filtering + flat/weighted random pick.
  - `Steam/GameLauncher` — `steam://` launch.
- `SteamRoulette.App` — WPF UI (`MainViewModel` + `MainWindow` + `SettingsWindow`).
- `SteamRoulette.Tests` — xUnit tests for the parser and the roulette.

## Project layout

```
src/SteamRoulette.Core/    # logic, no UI
src/SteamRoulette.App/     # WPF desktop app
tests/SteamRoulette.Tests/ # xUnit tests
```
