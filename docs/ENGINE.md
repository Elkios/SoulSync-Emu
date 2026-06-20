# SoulSync — Rules Engine (v2)

The engine is the **single source of truth** for a Soul Link run. It is **game-agnostic**
and **pure** (no I/O, no emulator types) so it can be fully unit-tested. The RAM layer feeds
it events; it emits **state** + **notifications**.

> Key correction from v1: Pokémon are linked **by zone (route)**, NOT by catch order.

---

## 1. Concepts

- **Run**: 2+ players, same generation, played simultaneously.
- **Zone**: a route/area. Each player gets **one binding first encounter per zone**.
- **TrackedMon**: a caught Pokémon (PID, species, level, hp, **zone of capture**, owner, state).
- **Link group**: keyed by **zone** — the set of Pokémon (one per player) caught on that zone.
  A group is what lives or dies together.

---

## 2. Zone status (per player, per zone)

```
Unseen ──enter──▶ Open ──catch──▶ Caught
                   │
                   ├──kill/flee/whiteout (no catch)──▶ Failed
                   └──first wild is an owned species & DupesClause──▶ (stay Open, re-roll)
```

- **Unseen**: never entered.
- **Open**: entered, encounter not yet resolved.
- **Caught**: player caught their first encounter here.
- **Failed**: player lost the first encounter (fainted it / fled / blacked out) → no catch.

Dupes clause: if the first wild is a species the player already owns, the encounter is
**re-rolled** (zone stays Open, notify) — it does not burn the zone.

---

## 3. Link group lifecycle (per zone)

```
Forming ──all players Caught here──▶ Linked
   │
   ├──any player Failed here────────▶ Void   (every caught member must be BOXED)
   └──any member faints─────────────▶ Dead   (every surviving member must be BOXED)
```

- **Forming**: some players caught here, others not yet resolved. Caught mons are usable
  (optimistically) while Forming.
- **Linked**: everyone caught → permanent partners.
- **Void**: a partner failed the zone → the link can never complete → box the orphans.
- **Dead**: a linked member fainted → the whole group dies → box the survivors.

**Box sync**: forcing one member to the box forces all members. You may not use a member
while its partner is boxed.

---

## 4. Rules (toggles)

| Rule | Effect |
|------|--------|
| **SoulLink** | Enables linking + cascade. Off = plain co-op Nuzlocke (no cascade). |
| **DupesClause** | First encounter of an already-owned species → re-roll (zone stays Open). |
| **SpeciesClause** | A player can't keep two of the same evolutionary line (flagged on catch). |
| **FirstBattleProtect** | The first battle is non-lethal: a faint there does NOT kill/cascade. Host lifts it after. |
| **LevelCap** | Optional. Warn when a mon exceeds the cap (by badge count). Not enforced. |
| **ShinyClause** | A shiny may be caught even if it's not the first encounter. |

---

## 5. Events (inputs)

The engine consumes high-level events (produced by the RAM layer, or directly in tests):

| Event | Meaning |
|-------|---------|
| `AddPlayer(id, name)` | Register a player. |
| `EnterZone(player, zone)` | Player walked into a zone → drives entry notifications. |
| `Encounter(player, zone, species)` | First wild appeared (used for dupes/species clause). |
| `Catch(player, zone, mon)` | Player caught their encounter (mon carries its capture zone). |
| `Fail(player, zone)` | Player lost the first encounter (kill / flee / whiteout, no catch). |
| `Faint(player, pid)` | A mon's HP hit 0. |
| `Evolve(player, pid, species)` | A mon evolved (species changes, PID stays). |
| `Sync(player, zone, party[])` | Live snapshot — engine diffs it to derive Catch/Faint/HP/blackout. |

`Sync` is how the emulator drives the engine in practice; the explicit events exist for
clarity and testing. The RAM layer reads each mon's **met location** to know its zone.

---

## 6. Outputs

- **EngineState**: per player → their mons with `{pid, species, level, hp, maxHp, zone,
  state: Alive|Fainted|Boxed, link: {zone, status, partners}}`; per-zone map of statuses;
  `gameOver`. Pushed to the WebView (`window.soulsync.update`).
- **Notifications** (`Note{kind, player, text}`): `zone` (new/caught/failed/void),
  `catch`, `link` (formed), `dupe`, `void`, `cascade`, `faint`, `evolve`, `levelcap`,
  `blackout`, `gameover`.

---

## 7. Game over

A player **blacks out** when their whole **party** (in-party, alive) is wiped. Because a
faint cascades (partners boxed), a wipe on one side propagates to the other → the run ends
for everyone. → **Game over when any player has no alive Pokémon left in their party.**

---

## 8. Worked examples

1. **Both catch (Route 1):** J1 catches Starly, J2 catches Bidoof → group `Route 1` = Linked.
2. **One fails:** J1 catches, J2 kills the wild → group `Route 1` = Void → J1 must box Starly.
3. **A linked mon faints:** J1's Starly faints → group Dead → J2 must box Bidoof (cascade).
4. **Dupe:** J2's first wild on Route 2 is a Bidoof (already owned) + DupesClause → re-roll,
   zone stays Open, no penalty.
5. **Wipe:** J1's last party mon faints → J1 blackout → cascade boxes J2's partners → game over.

---

## 9. Detection notes (RAM layer)

- **Zone change**: read current map id; on change → `EnterZone`.
- **Catch**: a new PID appears in the party/box with `metZone` = current zone → `Catch`.
- **Fail**: a **wild** battle (enemyTrainerID == 0) on an unresolved zone that ends with no
  new catch → `Fail`. A manual override button exists if detection is uncertain.
- **Faint / blackout**: party HP at 0 (read at battle end — Gen 5 doesn't update HP mid-battle).
