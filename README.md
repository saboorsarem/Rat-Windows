# Rat for Windows

<p>
  <a href="https://github.com/saboorsarem/Rat-Windows/actions/workflows/build.yml"><img alt="Build" src="https://github.com/saboorsarem/Rat-Windows/actions/workflows/build.yml/badge.svg"></a>
  <a href="https://github.com/saboorsarem/Rat-Windows/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/saboorsarem/Rat-Windows?label=release"></a>
  <a href="LICENSE"><img alt="License" src="https://img.shields.io/badge/license-MIT-blue.svg"></a>
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-0078D4">
</p>

A native **Windows** network monitor and application firewall in the spirit of Little Snitch — a port of the macOS app [ImanMontajabi/Rat](https://github.com/ImanMontajabi/Rat). It shows which app talks to which server, asks before unknown connections, and enforces allow/deny rules with Windows Firewall.

## Download

Every release ships a single self-contained executable — no .NET install, no setup wizard:

**[Rat-win-x64.exe](https://github.com/saboorsarem/Rat-Windows/releases/latest/download/Rat-win-x64.exe)** · [zip](https://github.com/saboorsarem/Rat-Windows/releases/latest/download/Rat-win-x64.zip)

Run it, accept the UAC prompt (blocking needs admin), and the monitor opens with a tray icon. Windows 10/11 x64.

## The dashboard

Three panes, like the macOS original:

| | |
| --- | --- |
| **Apps** (left) | Every process with an open connection, its icon and connection count. Search filters the list. The `✕` / `✓` buttons toggle a permanent deny/allow rule for the app and refresh instantly; click a filled button again to remove the rule. Double-click an app for its connection inspector. The bottom of the pane shows a live sent/received traffic chart (last ~4 minutes, logarithmic scale). |
| **World map** (center) | World silhouette with glowing arcs from *This PC* to every connected country, colored by decision: blue = allowed, grey = unconfirmed, red = denied. Country chips mark the endpoints. Country lookup is fully offline. |
| **Summary** (right) | Session sent/received totals, active connections, blocked servers, rule count, and the top processes, domains and countries by connection count. |

## Features

| | |
| --- | --- |
| **Live monitor** | Per-app list (app, open connections, servers) with a connection inspector: server address, reverse-DNS hostname, port, state, first seen. Refreshes every 2 s. |
| **Connection alerts** | Silent allow by default. In *Ask mode* unknown connections trigger a dialog: scope (*this app / any server*, this server, any app for this server, this port) and lifetime (*once*, *until quit*, *forever*), then Allow or Deny. |
| **Rules** | Rule table with enable toggle, manual add/delete and JSON import/export. Most-specific rule wins; deny beats allow on a tie. |
| **Enforcement** | Deny cuts the live connection (`SetTcpEntry` / DELETE_TCB) and adds an idempotent outbound block rule to Windows Firewall (group `Rat`). |
| **Background** | Closing the window keeps monitoring in the tray; live down/up throughput in the toolbar. |
| **Tests** | Rules-engine unit tests on every build. |

## How it works

- Connections: `GetExtendedTcpTable` (IPv4, established / connecting) mapped to processes via the owning PID.
- Hostnames: on-demand reverse DNS with a short timeout, cached per session.
- GeoIP: the DB-IP Lite country database is downloaded at build time, embedded gzipped (~4.5 MB) and searched with a binary search over IPv4 ranges. Country centroids come from a small built-in table; the world outline is a Natural Earth GeoJSON embedded in the app.
- Blocking: `netsh advfirewall firewall add rule ... dir=out action=block remoteip=... group=Rat`, preceded by an idempotent delete of the same rule name.
- Rules and settings: JSON in `%LocalAppData%\Rat\` (`rules.json`, `settings.json`), human-editable.

## Honest limits

Like the upstream macOS app, Rat is **reactive**, not preventive — it sees a connection after it starts (no kernel filter driver), then cuts it and blocks the server.

- The map is **country-level**: arcs end at country centroids, not at the real city of the server (the upstream macOS app ships a city database; this port stays with the ~4.5 MB country database). The "This PC" marker is schematic, not your real location.
- A blocked address is blocked for **every** app — that is how Windows Firewall address rules work (same as the upstream `pf` table).
- IPv4 only. No per-app traffic totals or profiles with automatic network switching (not ported).
- Administrator rights required for enforcement — the manifest requests elevation at launch.
- The app is not code-signed; SmartScreen may warn on first launch (`More info → Run anyway`).

## Build from source

```
./scripts/fetch-geoip.ps1   # downloads the DB-IP Lite country database (~4.5 MB)
dotnet build
dotnet test
dotnet publish src/Rat -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Without the GeoIP step the app still builds and runs; the map falls back to a "geo database not bundled" note.

The icon asset is generated by `scripts/make-icon.ps1` (PowerShell + System.Drawing).

## Cleanup

Every firewall rule Rat adds is named `Rat block <ip>` in group `Rat`. Remove all of them with:

```
netsh advfirewall firewall delete rule name=all group="Rat"
```

## Layout

| Path | Contents |
| --- | --- |
| `src/Rat/Core` | Rules model and matcher, connection sampler, connection cut, netsh blocker, JSON store, throughput, GeoIP lookup, country table, world-map geometry, icon cache. |
| `src/Rat` | WPF app: three-pane dashboard, inspector, alert, rules and rule-editor windows, dark theme, tray. |
| `src/Rat/Data` | Embedded world outline and (downloaded, git-ignored) GeoIP database. |
| `tests/Rat.Tests` | Rules-engine tests (specificity, deny-beats-allow, lifetimes, hostname/app-name matching). |

## Roadmap

- Preventive mode through a Windows Filtering Platform callout driver.
- IPv6 connections.
- City-level map and per-app traffic totals via ETW.
- Profiles with automatic network switching.

## Credits

- Original design and app: [ImanMontajabi/Rat](https://github.com/ImanMontajabi/Rat) (macOS, MIT).
- GeoIP data: [DB-IP Lite](https://db-ip.com/db) country database, CC BY 4.0.
- World outline: [Natural Earth](https://www.naturalearthdata.com/) via [johan/world.geo.json](https://github.com/johan/world.geo.json).
- This port: MIT, see [LICENSE](LICENSE).

> The name comes from the upstream project — "monitor your network and find the Rat". This is a defensive tool for your own machine, **not** a remote access trojan.
