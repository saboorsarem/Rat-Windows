# Rat for Windows

A network monitor and application firewall for **Windows**, ported from [ImanMontajabi/Rat](https://github.com/ImanMontajabi/Rat) (a Little Snitch-style monitor for macOS, MIT).

## Download

Releases carry `Rat-win-x64.exe` — one file, self-contained, no .NET install needed:

```
https://github.com/saboorsarem/Rat-Windows/releases/latest/download/Rat-win-x64.exe
```

Unzip-free: run it, accept the UAC prompt (blocking needs admin), and a monitor window plus tray icon appear. The macOS original is at [saboorsarem/Rat](https://github.com/saboorsarem/Rat).


> **The name** comes from the upstream project: "monitor your network and find the Rat." This is a **defensive** tool — it watches the outgoing connections of your own apps. It has no relation to remote access trojans.

It shows which app talks to which server, asks before unknown connections, lets you allow or deny them with rules and lifetimes, and blocks denied servers with **Windows Firewall** plus a live connection cut.

## Features

- **Network monitor** — per-app list (app, open connections, servers) and a per-app connection inspector (server, reverse-DNS hostname, port, state, first seen). Auto-refresh every 2 s.
- **Connection alerts** — **Silent Allow** is the default, so Rat never nags on first launch. Switch to **Ask mode** and a dialog appears for unknown connections: scope (*This app (any server)* by default, this server, any app for this server, this port) and lifetime (Once / Until quit / Forever), then Allow or Deny.
- **Rules window** — all saved rules in one table (On, Action, App, Server, Port, Lifetime, Description) with manual add, delete and **JSON import/export**. The most specific rule wins and deny beats allow on a tie (`RulesEngine`).
- **Firewall enforcement** — Deny adds an outbound Windows Firewall block rule for the server address (rule group `Rat`, idempotent: re-adding deletes the old one first) and cuts the live connection via `SetTcpEntry` (DELETE_TCB).
- **Tray icon** — Rat keeps monitoring in the background after the window closes; tray shows the app, `Open`/`Exit`.

## Honest limits

Like the upstream macOS app, Rat is **reactive**, not preventive:

- It notices a connection **after** it starts (it has no kernel filter driver — no WFP callout). On Deny it cuts the connection and blocks the server address for the future.
- A blocked address is blocked for **every** app, not just the one that triggered the alert — that is how Windows Firewall address rules work (same as the upstream `pf` table).
- **IPv4 only.** No offline geo/world map (upstream bundles DB-IP; skipped here — hostnames come from on-demand reverse DNS).
- **Administrator required** to enforce blocking — the app requests UAC elevation on launch.
- Per-app traffic totals/sparklines and profiles with automatic network switching are not ported.

## Build and run

```
dotnet build
dotnet test                          # rules-engine tests
dotnet publish src/Rat -c Release   # produces Rat.exe
```

Run the published `Rat.exe` (accept the UAC prompt). Data lives in `%LocalAppData%\Rat\rules.json` and `settings.json`.

Example `rules.json`:

```json
[
  {
    "Enabled": true,
    "Action": "Allow",
    "Lifetime": "Forever",
    "AppPath": "C:\\Program Files\\Mozilla Firefox\\firefox.exe",
    "Server": null,
    "Port": null
  }
]
```

To clean up every firewall rule Rat ever added:

```
netsh advfirewall firewall delete rule name=all group="Rat"
```

## Layout

| Path | Contents |
| --- | --- |
| `src/Rat/Core` | Rules model + matching engine, `GetExtendedTcpTable` sampler, `SetTcpEntry` close, netsh blocker, JSON store, throughput sampler. |
| `src/Rat` | WPF app: monitor, alert panel, rules window, rule editor, tray. |
| `tests/Rat.Tests` | Rules-engine tests (specificity, deny-beats-allow, lifetime handling). |

## Credits

Design and name by [ImanMontajabi/Rat](https://github.com/ImanMontajabi/Rat) (Little Snitch clone for macOS). MIT license.
