# Roadmap

**Status:** maintain. **Last reviewed:** 2026-09-24.

The backlog roulette (library loading, filters, weighted picks, launch-in-Steam) is functionally
complete and Daniel uses it. It is a personal tool with no income path, so it does not get an
active roadmap slot right now: "done" for now means CI stays green and dependencies stay patched.

> How this file is used: Claude Project threads build the first unticked item under **Now**, one item per branch and pull request, and tick it in that same PR as `- [x] ... (#PR)`. Daniel owns the order and the lists; threads never add to Now, Next or Later themselves, they propose under **Ideas**.

## Now

- [ ] **Keep CI green**: the `ci.yml` and security-scan workflows stay passing on `main`. Done when: the latest run on `main` is green.
- [ ] **Keep dependencies patched**: merge Dependabot bumps as they land; 0 open alerts as of 2026-09-24, keep it there. Done when: no open Dependabot alert is older than its grace period.

## Next

- Achievement manager (the SAM half): list and edit achievements/stats via the Steamworks SDK (`ISteamUserStats`) for games you own (parked)

## Later

- Backlog and achievement cross-over: "pick a game I'm one achievement away from 100%-ing" (parked)
- Richer library data: genres/tags and store metadata for genre-based rolls (parked)
- Game art polish: cached capsule art, a spin animation on the pick (parked)

## Ideas

(empty to start; threads add proposals here)

## Done

- [x] Core foundations: models, VDF parser, Steam paths (#1)
- [x] Library loading and the game roulette (#2)
- [x] WPF UI and full README (#3)
- [x] Enrichment layer with metadata/achievement cache (#4), filters (#5), and stability fixes (#6)
- [x] UI redesign with a rolled-game stats panel (#7), dark title bar and slim scrollbars (#8), app icon (#9)
- [x] Steam rating, more filters, and a fixed list highlight (#10)
- [x] Malware check for a game via VirusTotal (#11)
- [x] Bigger game stats panel (#12)
- [x] Standard CI, security scan, and monthly Dependabot workflows (#15)
