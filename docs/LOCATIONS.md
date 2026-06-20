# Gen V (Black 2 / White 2) Met-Location Names

This document describes how `src/SoulSync.Games/Gen5/data/locations.b2w2.json` was built
and evaluates whether the names could instead be extracted directly from the NDS ROM
(so they would automatically arrive in the ROM's own language).

## What the JSON contains

A map `id -> name` in two languages (`en`, `fr`), where `id` is the **Generation V
location index number** — the same value stored in a Pokémon's encrypted block D at
offset `0x80` ("met location"), and also used as the "current location" id.

- **EN:** ids 1–153 (the full Unova / Gen 5 overworld index — cities, towns, every
  route, caves, gates, bridges, towers, landmarks), plus the Entralink-internal map
  duplicates (76–105) and a few region/trade ids (30004–30007 = Kanto/Johto/Hoenn/Sinnoh,
  30015 = Pokémon Dream Radar).
- **FR:** every settlement + the great majority of routes/caves/bridges/landmarks,
  cross-checked against Poképédia. Where no authoritative FR name was found it is
  **omitted on purpose** (UI should fall back to EN) rather than guessed — see the
  `_meta.coverage.fr` field in the JSON for the exact omission list (gates, Clay Tunnel,
  Gear Station, Royal Unova, Giant-Chasm sub-chambers, etc.).

### Verified anchors

| id  | EN              | FR             |
|-----|-----------------|----------------|
| 117 | Aspertia City   | Pavonnay       |
| 124 | Route 19        | Route 19       |

Both match the values verified live against a running game, so the index alignment is
correct.

### Notable accuracy points

- **Water routes in FR** use "Chenal" instead of "Route": id 30 (Route 17) = `Chenal 17`,
  id 126 (Route 21) = `Chenal 21` (official in-game FR names per Poképédia). All other
  routes are `Route N` in both languages.
- **Corrected a bug in the previous starter file:** it listed id 118 (Virbank City)
  FR = "Méanville". That is wrong — *Méanville* is **Nimbasa City** (id 9). Virbank City
  is **Ondes-sur-Mer**.
- id 36 in B2W2 is the **Pokémon World Tournament** (it reuses the BW "Cold Storage" map id).

## Sources

1. **Bulbapedia — "List of locations by index number (Generation V)"**
   (https://bulbapedia.bulbagarden.net/wiki/List_of_locations_by_index_number_(Generation_V))
   — authoritative EN names and the complete numeric index (0–153, plus the 30000/40000/60000
   special blocks).
2. **Poképédia — "Catégorie:Lieu d'Unys"** and individual location pages
   (https://www.pokepedia.fr/Cat%C3%A9gorie:Lieu_d%27Unys) — FR names. Ambiguous mappings
   were resolved by opening each page and reading its "Nom anglais" infobox, e.g.
   *Veine Souterraine* = Wellspring Cave, *Grotte Cyclopéenne* = Giant Chasm,
   *Vestiges du Rêve* = Dreamyard, *Bois des Illusions* = Lostlorn Forest,
   *Antre d'Entraînement* = Challenger's Cave.
3. **Pokébip — "Pokémon Noir 2 / Blanc 2 > Guide des Lieux"**
   (https://www.pokebip.com/page/jeux-video/pokemon-noir-2-blanc-2/guide-des-lieux/index)
   — FR cross-check of the full location list.

---

## Feasibility: extracting names directly from the ROM

**Verdict: technically feasible, but not recommended as the primary path for this app.**
Use the static JSON; optionally back-fill other languages from PKHeX.Core's resources.
Full ROM parsing is overkill here.

### Where the names live in the ROM

B2W2 game text (Pokémon names, item names, place/met-location names, story dialogue,
etc.) is stored in a **NARC** archive inside the ROM filesystem at:

```
a/0/0/2     <- main text NARC ("message" archive) for Black 2 / White 2
```

(Story-event text is in a separate archive; `a/0/0/2` holds the "system" strings,
including the location/place-name tables.) The NARC is a container of many *message
banks*; the place-name list is one bank, indexed in the same order as the location
index numbers used above.

### The text format

Gen 5 message banks are **not plain UTF-16**. Each bank:

- uses a 16-bit-per-character custom char map, and
- is **XOR-obfuscated**: every string is scrambled with a key derived from a seed
  stored at the start of the entry (the key advances per character/per line).

So you cannot just `strings` the file — you must implement the Gen 5 decode (seed →
keystream → XOR → char-map lookup), then optionally strip the variable/control codes
(`\n`, `\f`, gender/version branches, etc.).

### Tooling that already does this

- **Nitro Explorer / ndstool / Tinke** — unpack the NDS filesystem and pull out
  `a/0/0/2`.
- **PPTXT** (Project Pokémon / community) — opens the renamed `2.narc` and exports/imports
  all banks with the correct XOR handling. Good for a one-off dump.
- **"Pokémon NARC Text Tool"** and **Frost's Gen 5 Editor** — CLI/GUI tools that
  export/import Gen 5 NARC text and handle the encryption.
- **ndspy** (Python library) — reads NARC containers; would need a Gen 5 text decoder
  layered on top.
- **PKHeX.Core** (C#/.NET) — already ships the **Gen 5 met-location string tables in
  multiple languages** as embedded resources (the `text_*` / location string sets it uses
  to label met locations). This is the easiest way to get more languages *without touching
  a ROM at all*.

### Recommendation

| Option | Pros | Cons |
|--------|------|------|
| **Static JSON (this file)** | Zero ROM dependency, offline, instant, reviewed for accuracy, EN+FR now | Manual to extend to more languages; must be maintained by hand |
| **Reuse PKHeX.Core location tables** | Free EN/FR/DE/IT/ES/JA/KO, already encrypted-decoded and battle-tested, pure .NET | Adds a dependency; names are PKHeX's wording (occasionally differs slightly from in-game) |
| **Parse the ROM's `a/0/0/2`** | Names come in the *exact* in-game wording and in whatever language the user's cartridge/ROM is | Requires bundling/locating a ROM, implementing the XOR+charmap decoder, and mapping the place-name bank to the index — significant effort for marginal benefit |

**Concrete suggestion:** keep the static JSON as the source of truth for the UI, and if
multi-language support is wanted later, populate the extra languages from **PKHeX.Core**
(or a one-time PPTXT dump of `a/0/0/2`) into the same JSON shape — rather than parsing the
ROM at runtime.
