# Adding a game

Want SoulSync to support another game (HeartGold/SoulSilver, Diamond/Pearl, an older Gen…)?
You only need to write **one adapter** that turns the game's memory into SoulSync's normalized
party model. **No emulator hacking.** You can build and test it entirely with `SoulSync.sln`.

> Real, working example: [`src/SoulSync.Games/Gen5/BlackWhite2Adapter.cs`](../src/SoulSync.Games/Gen5/BlackWhite2Adapter.cs).

---

## The contract

Implement [`IGameAdapter`](../src/SoulSync.Games/IGameAdapter.cs):

```csharp
public interface IGameAdapter
{
    string Id { get; }                 // "hgss", "dppt", …
    EmuSystem System { get; }          // NDS, GBA, …
    IReadOnlyList<string> Regions { get; }

    bool  Matches(IMemoryReader mem, RomInfo rom);  // is this my game?
    Party ReadParty(IMemoryReader mem);             // → normalized team
}
```

- You receive an [`IMemoryReader`](../src/SoulSync.Core/Memory/IMemoryReader.cs)
  (`ReadU8/U16/U32/ReadBytes(domain, address)`, little-endian). **That's your only window into
  the game** — you never touch BizHawk.
- You return a [`Party`](../src/SoulSync.Core/Models/Party.cs) of
  [`Mon`](../src/SoulSync.Core/Models/Mon.cs)`(Pid, Species, Level, Hp, MaxHp, InParty)`.
  Everything downstream (rules, UI, networking) is game-agnostic from here.

---

## Steps

### 1. Create the adapter

Put it under a Gen folder, e.g. `src/SoulSync.Games/Gen4/HeartGoldSoulSilverAdapter.cs`.
Share crypto/helpers in a `GenXCrypto.cs` (see [`Gen5Crypto.cs`](../src/SoulSync.Games/Gen5/Gen5Crypto.cs)).

### 2. Find the addresses

You need, per version/region:
- the **party base address** and **slot size**,
- offsets for **PID, level, current HP, max HP, species**,
- the **decryption scheme** (Gen 4/5 encrypt party data with a PID/checksum-seeded LCG).

Good sources:
- Existing trackers (e.g. **NDS-Ironmon-Tracker**, **PokeStats/yPokeStats**, **PokeStreamer-Tools**).
- **PKHeX** source (`PokeCrypto`) for the exact crypto.
- **BizHawk → Tools → RAM Search / RAM Watch** to locate values live.
- **Data Crystal** (datacrystal.tcrf.net) RAM maps.

> Tip: capture a **RAM dump** to develop offline — in BizHawk, `Tools → Hex Editor → File →
> Save (Main RAM)`. Commit a small dump? No — keep dumps local; ship a synthetic buffer in tests.

### 3. Register it

Add your adapter to [`GameRegistry.All`](../src/SoulSync.Games/GameRegistry.cs):

```csharp
public static IReadOnlyList<IGameAdapter> All { get; } = new IGameAdapter[]
{
    new BlackWhite2Adapter(),
    new HeartGoldSoulSilverAdapter(),   // ← your adapter
};
```

`GameRegistry.Detect(mem, rom)` calls `Matches(...)` to pick the active adapter on ROM load.

### 4. Test it (no emulator!)

Use [`DumpMemoryReader`](../src/SoulSync.Tests/DumpMemoryReader.cs) — a fake `IMemoryReader`
backed by a `byte[]`. Write an encrypted slot into the buffer, then assert your adapter decodes
it. See [`BlackWhite2AdapterTests.cs`](../src/SoulSync.Tests/BlackWhite2AdapterTests.cs) for the
pattern (encryption is the same XOR keystream as decryption, so you can synthesize test data).

```bash
dotnet test SoulSync.sln -c Release
```

---

## Checklist

- [ ] `IGameAdapter` implemented under the right `GenX/` folder
- [ ] `Matches` reliably detects the game (and version/region)
- [ ] `ReadParty` returns correct PID / level / HP / maxHP / species
- [ ] Registered in `GameRegistry.All`
- [ ] At least one test with a synthetic RAM buffer, green
- [ ] PR against `soulsync` 🎉

Welcome aboard — every new game makes more crews able to link up. 🔗
