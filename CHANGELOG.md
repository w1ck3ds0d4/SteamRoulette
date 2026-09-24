# Changelog

All notable changes to this project are documented here. No tagged releases exist yet, so
entries are grouped by merged pull request instead of version.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

### Added

- Steam rating, more filters, and a fixed list highlight (#10)
- Malware check for a game via VirusTotal, hash-only (#11)
- Bigger game stats panel (#12)

### Changed

- UI redesign with a rolled-game stats panel (#7)
- Dark title bar and slim themed scrollbars (#8)
- Dice app icon (#9)
- Replaced em dashes with hyphens throughout (#13)

### Infrastructure

- Standard CI, security scan, and monthly Dependabot workflows (#15)

## Earlier

- Core foundations: models, VDF parser, Steam paths (#1)
- Library loading and the game roulette (#2)
- WPF UI and full README (#3)
- Enrichment layer with metadata/achievement cache and disk cache (#4)
- Genre and achievement filters with background enrichment (#5)
- Enrichment stability: crash logging and timeout handling (#6)
