# Contributing to SoulSync

Thanks for wanting to help! 🔗 SoulSync is built so you can contribute **without fighting the
emulator**. This page gets you set up; the specialized guides cover the common contributions.

- 🧩 [Add a game](ADDING_A_GAME.md)
- 🌍 [Add a language](ADDING_A_LANGUAGE.md)
- 🌐 [Host a server](HOSTING_A_SERVER.md)
- 🏗️ [Architecture overview](ARCHITECTURE.md)

---

## The golden rule

> **Game logic never references the emulator.**
> `SoulSync.Core` and `SoulSync.Games` depend only on the `IMemoryReader` abstraction — never
> on BizHawk, WinForms, or the UI. This is what keeps SoulSync testable and contributable.

If your change makes `SoulSync.Core` or `SoulSync.Games` reference BizHawk, it's in the wrong
layer. See [ARCHITECTURE.md](ARCHITECTURE.md).

---

## Two ways to build

SoulSync has **two solutions** on purpose:

| Solution | What it builds | When you need it |
|----------|----------------|------------------|
| **`SoulSync.sln`** | The domain layer (Core, Games, Net, Server, Tests) — **no emulator** | Adding a game, rules, networking. Fast. |
| **`BizHawk.sln`** | The full app (emulator + WebView dashboard) → `output/EmuHawk.exe` | UI integration, end-to-end testing. |

**Most contributions only need `SoulSync.sln`.** You can write and test a whole game adapter
without ever launching the emulator.

```bash
# Domain layer (fast — what you'll use most)
dotnet build SoulSync.sln -c Release
dotnet test  SoulSync.sln -c Release

# Full app (only when integrating UI / testing in the emulator)
dotnet build BizHawk.sln -c Release   # → output/EmuHawk.exe
```

### Prerequisites

- **.NET SDK** pinned in [`global.json`](../global.json) (currently .NET 8).
- For the full app only: **WebView2 runtime** (preinstalled on Windows 11) and Windows.

---

## Project layout

```
src/SoulSync.Core      Pure domain: IMemoryReader, models (Mon/Party), rules engine, i18n.
src/SoulSync.Games     IGameAdapter + one adapter per game + game data.
src/SoulSync.Net       Relay client + server library.
src/SoulSync.Server    Standalone relay server (external hosting).
src/SoulSync.Tests     Unit tests (driven by RAM dumps).
src/BizHawk.Client.EmuHawk/SoulSync   Composition root: IMemoryReader over BizHawk + WebView host.
ui/                    WebView front-end (HTML/CSS/JS) + locales.
docs/                  You are here.
```

---

## Workflow

1. **Fork** `Elkios/SoulSync-Emu` and branch off `soulsync`:
   `git switch -c feat/my-thing`
2. Make your change in the **right layer** (see the golden rule).
3. **Add/keep tests green:** `dotnet test SoulSync.sln -c Release`.
4. Open a **pull request** against `soulsync` with a clear description.

### Commit style

- Short, imperative subject. English or French both fine for commit messages.
- Reference an issue when there is one.

### Code style

- Target **.NET 8 / C# 12**, `Nullable` enabled.
- Match the surrounding code. Keep domain code free of I/O and emulator types.
- New `SoulSync.*` projects stay **self-contained** (they don't import BizHawk's build props).

---

## Good first issues

- Add a **language** ([guide](ADDING_A_LANGUAGE.md)) — no C# required.
- Add a **game adapter** for a Gen you know ([guide](ADDING_A_GAME.md)).
- Improve the **dashboard UI** (`ui/`, plain web tech, mockable in a browser).

Questions? Open an [issue](../../issues). Happy hacking! 💀🔗
