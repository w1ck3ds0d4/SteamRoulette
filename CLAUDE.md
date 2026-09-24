# SteamRoulette

A small Windows desktop app (C# / .NET 10, WPF) that reads your Steam library and picks a
random game to play, with backlog-aware filters. `SteamRoulette.Core` holds all logic
(Steam file parsing, Web API, the roulette) with unit tests; `SteamRoulette.App` is a thin
WPF UI on top. No installer, no external services beyond the optional Steam Web API.

Status: maintain. No new features without Daniel's go. See ROADMAP.md.

## Commands

```bash
dotnet run --project src/SteamRoulette.App      # launch the app
dotnet test                                     # run the Core unit tests
dotnet build                                    # build the whole solution (SteamRoulette.slnx)
```

Requires the .NET 10 SDK on Windows; WPF is Windows-only.

## Layout

| Path | What it is |
| --- | --- |
| `src/SteamRoulette.Core/` | Pure logic: VDF/ACF parsing, Steam path discovery, library sources, the roulette, the launcher. No UI. |
| `src/SteamRoulette.App/` | WPF UI: `MainViewModel`, `MainWindow`, `SettingsWindow`. |
| `tests/SteamRoulette.Tests/` | xUnit tests for the parser, filters, roulette, and enrichment. |

## Conventions

- Commit format: `(type) lowercase summary`, no body, no em dashes or en dashes.
- One feature branch per change, one PR into `main`.
- ASCII hyphens only in prose and code comments.
- CI (`.github/workflows/ci.yml`) and a security scan run on push/PR to `main`.

## Do not read

`bin/`, `obj/`, build output.
