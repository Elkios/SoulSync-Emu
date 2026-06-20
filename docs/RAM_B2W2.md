# RAM map — Pokémon Black 2 / White 2 (NDS, melonDS)

Addresses **verified live** on a real White 2 (FR/PAL) ROM via a Lua debug probe
(2026-06-20). This is the reference for `SoulSync.Games/Gen5/BlackWhite2Adapter`.

> Memory domain: **"Main RAM"**. All addresses below are absolute DS addresses;
> read at `address - 0x02000000` within that domain. Multi-byte = little-endian.

## Party
- Base: **White 2 `0x0221E42C`**, **Black 2 `0x0221E3EC`** (region PAL/FR == US; JP differs).
- Slot size `0xDC` (220 bytes), 6 slots. Detect game by which base holds a coherent slot 0.

## Per-Pokémon (Gen 5 encrypted block, see Gen5Crypto)
| Field | Where | Notes |
|-------|-------|-------|
| PID | `slot + 0x00` (u32) | link/identity key |
| Level / curHP / maxHP | battle-stats region `0x88+`, **PID-seeded** LCG | level = u16#2 low byte, curHP = #3, maxHP = #4 |
| Species | block A, logical `0x08` (u16) | checksum-seeded blocks + PID block shuffle |
| TID / SID | block A, logical `0x0C` / `0x0E` (u16) | the OT's; for own mons = player's → shiny calc |
| Nickname | block C, ~`0x48` (u16 chars) | game char encoding |
| **Met location (LINK KEY)** | block D, logical **`0x80`** (u16) | the route/place id — pairs the Soul Link |
| Met level | block D, logical `0x84` (u16, low byte) | level when caught |

Verified: Tepig/Gruikui = species 498, met80 117 (start town); Patrat/Ratentif = 504,
met80 124 (first route). TID/SID 35529/7008.

## World / battle
| Data | Address | Verified value |
|------|---------|----------------|
| Current map id (player position) | `0x02246848` (u16) | town 427 → first route 437 |
| In-battle flag | `0x021B5178` (u16) | `0x2100` in battle, overworld value otherwise |
| Wild vs trainer | `0x02257332` (u16, "enemyTID") | **0 = wild**, non-zero = trainer/overworld |
| Enemy Pokémon (in battle) | base `0x02258874` (decode like a party slot) | wild species (e.g. 509 Purrloin) |

> Two distinct id spaces: **map id** (`0x02246848`, current position → zone-entry
> notifications) vs **met-location id** (block D `0x80`, the capture place → Soul Link
> pairing). Each needs its own id→name table (per game + language).

## Notes / caveats
- **In-battle HP doesn't update** in Gen 5 — read HP at battle end (death detection).
- **PC box** is NOT read from RAM: the engine infers "boxed" from a Pokémon leaving the
  party between snapshots (it already has the mon's data from when it was in the party).
- Enemy base `0x02258874` is only valid while the in-battle flag is set.
- Shiny is derived: `(TID ^ SID ^ PID_hi ^ PID_lo) < 8`.
