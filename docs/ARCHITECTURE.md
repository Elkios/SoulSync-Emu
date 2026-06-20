# SoulSync — Architecture

> SoulSync is a fork of [BizHawk](https://github.com/TASEmulators/BizHawk) that turns the
> emulator into an all-in-one **Nuzlocke Soul-Link** companion: randomize → play → live
> team tracking → real-time soul-links across players, in a **single window**.

This document describes how the project is organized and how to extend it. All project
documentation is written in **English**.

---

## 1. Goals (non-negotiables)

| Goal | Consequence on the architecture |
|------|----------------------------------|
| **Single window** | The dashboard is a WebView2 panel docked inside EmuHawk. |
| **Multi-game** | Game-specific logic lives behind a `IGameAdapter` abstraction; one adapter per game. |
| **Multilingual (i18n)** | All user-facing strings come from locale resources, never hard-coded. |
| **Self-hostable networking** | A relay server can run **locally** (in-process) or **externally** (deployed); the client targets a configurable URL. |
| **Open source & contributable** | Strict layering + a dependency rule so contributors can add a game/language without touching the emulator internals. |

---

## 2. The dependency rule (most important principle)

Dependencies always point **inward**. Nothing in the domain knows about BizHawk or the UI.

```
            ┌──────────────────────────────────────────────┐
            │  BizHawk.Client.EmuHawk  (composition root)   │  ← only place that
            │  • implements IMemoryReader over BizHawk      │    references BizHawk
            │  • hosts the WebView2 panel                    │
            │  • wires Adapter → Engine → UI → Net          │
            └───────────────┬───────────────┬───────────────┘
                            │               │
              ┌─────────────▼──┐     ┌──────▼───────────┐
              │ SoulSync.Games │     │  SoulSync.Net    │
              │ (IGameAdapter) │     │ (client+server)  │
              └─────────────┬──┘     └──────────────────┘
                            │
                   ┌────────▼─────────┐
                   │  SoulSync.Core   │  ← pure domain, ZERO external deps
                   │ models + engine  │
                   │ + i18n           │
                   └──────────────────┘
```

**Rule:** `SoulSync.Core` and `SoulSync.Games` must **never** reference BizHawk, WinForms,
or the UI. They depend only on abstractions (`IMemoryReader`). This makes them unit-testable
with a fake reader (a RAM dump) and keeps the emulator replaceable.

---

## 3. Projects (single .NET monorepo)

```
/src
  /SoulSync.Core        Domain models, rules engine, i18n. No BizHawk/UI/WinForms deps.
  /SoulSync.Games       IGameAdapter + one adapter per game + per-game data (JSON).
  /SoulSync.Net         Relay client + relay server library (local or external).
  /SoulSync.Server      Standalone console host for external deployment.
  /SoulSync.Tests       Unit tests for Core + Games (driven by RAM dumps).
  /BizHawk.Client.EmuHawk
        /SoulSync        Integration only: IMemoryReader impl over BizHawk,
                         WebView2 panel host, message bridge, composition root.
/ui                     WebView front-end (HTML/CSS/JS) — presentation only.
  /locales              en.json, fr.json, … (UI strings)
/docs                   ARCHITECTURE.md, CONTRIBUTING.md, ADDING_A_GAME.md,
                        ADDING_A_LANGUAGE.md, HOSTING_A_SERVER.md
```

Why C# everywhere (not a JS layer): chosen for a single-language codebase fully integrated
in the fork. The UI is the only web layer (HTML/CSS/JS in the WebView), and it is
**presentation-only** — it receives ready-to-render JSON from C# and sends user intents back.

---

## 4. Core abstractions

### 4.1 `IMemoryReader` — the seam between games and the emulator

```csharp
namespace SoulSync.Core.Memory;

public interface IMemoryReader
{
    byte   ReadU8 (string domain, long address);
    ushort ReadU16(string domain, long address);   // little-endian
    uint   ReadU32(string domain, long address);
    byte[] ReadBytes(string domain, long address, int length);
}
```

- Implemented once in `EmuHawk/SoulSync` over BizHawk `MemoryDomains` (e.g. "Main RAM").
- Implemented again in tests as `DumpMemoryReader` (reads from a `.bin` RAM dump).
- Game adapters receive an `IMemoryReader` — they never see BizHawk.

### 4.2 `IGameAdapter` — the multi-game contract

```csharp
namespace SoulSync.Games;

public interface IGameAdapter
{
    string   Id          { get; }   // "b2w2", "hgss", "dppt", …
    EmuSystem System     { get; }   // NDS, GBA, …
    IReadOnlyList<string> Regions { get; } // "EU","US","JP"

    bool  Matches(IMemoryReader mem, RomInfo rom);  // is this the right game/version?
    Party ReadParty(IMemoryReader mem);             // → normalized team
}
```

Each game decrypts/reads in its own way (Gen 5 PID/checksum LCG, Gen 4, Gen 3, …) but all
return the **same normalized model**, so everything downstream is game-agnostic.

### 4.3 Normalized domain model (`SoulSync.Core`)

```csharp
public sealed record Mon(uint Pid, int Species, int Level, int Hp, int MaxHp, bool InParty);
public sealed record Party(string GameId, string Region, IReadOnlyList<Mon> Members);
```

### 4.4 Rules engine (`SoulSync.Core`)

Game-agnostic Nuzlocke / Soul-Link logic (port of the validated JS engine): catch tracking,
pairing by catch order, death cascade across linked players, species clause, game-over,
revive, evolution. Consumes `Party` snapshots + events; emits state + notifications.
**Pure** → 100% unit-tested.

### 4.5 i18n

User-facing strings come from locale resources. Two surfaces:
- **C# side** (engine notifications, errors): resource files keyed by id.
- **UI side** (`/ui/locales/*.json`): all interface text. Web contributors can add a
  language by dropping a JSON file — no C# needed.

---

## 5. Data flow (one frame/tick)

```
Emulator RAM
   │  (BizHawk MemoryDomains)
   ▼
IMemoryReader (C#, in EmuHawk)
   ▼
GameRegistry.Active.ReadParty(mem)        → Party (normalized)
   ▼
SoulLinkEngine.Apply(party, events)       → State + Notifications   (rules, cascade)
   ├──────────────► WebView UI   via CoreWebView2.PostWebMessageAsJson(state)
   └──────────────► SoulSync.Net broadcast to peers (through the relay)
```

The UI never computes rules; it renders `State` and plays notifications. Peers receive the
same `State`/notifications over the network so every player sees every team live.

---

## 6. Game registry & detection

`GameRegistry` holds all `IGameAdapter`s. On ROM load, it calls `Matches(...)` to select the
active adapter (game + region). Adapters are self-registering so adding one requires no edits
to the registry. See **ADDING_A_GAME.md**.

---

## 7. Networking (local or external)

- `SoulSync.Net` contains both the **relay server library** and the **client**.
- **Local hosting:** the host player runs the server in-process; peers connect to the host IP.
- **External hosting:** deploy `SoulSync.Server` (console app) to a VPS / Render / Railway;
  all clients dial out to its URL (no port-forwarding). See **HOSTING_A_SERVER.md**.
- The server is **host-authoritative**: the engine is the single source of truth; the server
  relays normalized state/notifications to room members.

---

## 8. Contribution surfaces

| I want to… | I touch… | Build emulator? |
|------------|----------|-----------------|
| Add a game | `src/SoulSync.Games/<game>/` (+ data JSON) + a test with a RAM dump | No (test in isolation); yes to ship |
| Add a language (UI) | `ui/locales/<lang>.json` | No |
| Add a language (engine) | `src/SoulSync.Core` resources | Yes |
| Improve the dashboard | `ui/` | No (mockable in a browser) |
| Host a server | run/deploy `src/SoulSync.Server` | No |

---

## 9. Build & run

- Requires the .NET SDK pinned in `global.json` (currently .NET 8).
- Build: `dotnet build BizHawk.sln -c Release` → `output/EmuHawk.exe`.
- The WebView2 runtime is required at runtime (bundled on Windows 11).
- See **CONTRIBUTING.md** for the full setup.

---

## 10. Design principles (summary)

1. **Dependency rule** — only the composition root (EmuHawk) references BizHawk/UI.
2. **Games are pure transforms** — `IMemoryReader → Party`; testable with RAM dumps.
3. **Core is pure** — no I/O, fully unit-tested.
4. **One source of truth per concern** — game data in JSON, locales in JSON/resources,
   rules in the engine, design tokens in the brand.
5. **The UI is dumb** — it renders state and forwards intents; it computes nothing.
```
