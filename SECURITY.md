# Security

SteamRoulette is a local Windows desktop app. It reads your Steam installation, optionally
calls the Steam Web API, and opens a browser link to VirusTotal on request. It has no server
component and no account system.

## What the code touches

- **Local files**: Steam's `libraryfolders.vdf` and `appmanifest_*.acf` files (read-only), to
  discover installed games and their install paths.
- **Network**: the Steam Web API (`IPlayerService/GetOwnedGames`), only when a Web API key is
  configured; the Steam Store API for enrichment metadata; `steam://run/<appid>` to launch a
  game through the installed Steam client. No network calls happen with no Web API key
  configured, other than store enrichment.
- **The malware-check feature**: hashes the game's likely main executable with SHA-256 locally
  and opens `https://www.virustotal.com/gui/file/<hash>` in the default browser. The file
  itself is never uploaded, only the hash is sent, as part of a normal browser navigation.
- **Permissions**: runs as the logged-in user, no elevation, no driver, no background service.

## Secrets handling

The only credential is an optional Steam Web API key, paired with a SteamID. Both are stored
in plain JSON at `%APPDATA%\SteamRoulette\settings.json`, the same folder the app owns for its
own settings. This is standard practice for a Steam Web API key (it is scoped to public
player data, not your Steam account password), but the file is unencrypted, so anyone with
access to that Windows profile can read it. A cache of enrichment data is kept separately
under `%LOCALAPPDATA%\SteamRoulette\cache`.

## Known risks

- `settings.json` is plaintext; losing the key only exposes your public Steam library data
  via the Web API, not your account.
- `AppSettings.Load()` swallows deserialization errors and falls back to defaults rather than
  surfacing a corrupt file, which is a usability tradeoff, not a security one.
- No code signing on the built executable, so Windows SmartScreen may warn on first run.

## Reporting a vulnerability

Use GitHub's private vulnerability reporting on this repo (Security tab -> Report a
vulnerability). Include a short description, the minimum repro, and your assessment of
impact. This is a personal tool maintained in spare time, so response time varies.
