# SoulSync — Brand

<img src="assets/logo.png" width="96" alt="SoulSync logo">

The canonical brand reference. For the visual showcase, open
`soulsync-design/brandbook.html`.

---

## Foundations

- **Mission** — Make multiplayer **Soul Link Nuzlocke** simple, fun, and zero-setup.
- **Vision** — The go-to tool for randomized Nuzlocke runs with friends — all in one window.
- **Personality** — Playful · Buddy · Retro-DS · Warm · A little competitive.
- **Tagline** — *Your souls, linked in real time.*

SoulSync was born from Soul Link and is growing into a platform for multiplayer randomized
challenges — for friends **and** streamers.

---

## Logo

A pixel Poké Ball wrapped in a thick gold ring with two diagonal arrows (↗ top-right,
↙ bottom-left): the **link** (sync) between two trainers, in a DS-game spirit.

- Master grid: **32×32** pixel art.
- Files: `docs/assets/logo.png`, `logo.svg`, `favicon.ico`; sizes 16→512 in `soulsync-design/dist/`.
- **Minimum size:** 16 px. Clear space ≥ the ring's thickness around the logo.

**Do:** keep Braise & Or colors · keep pixels crisp · respect clear space.
**Don't:** stretch/skew · recolor outside the palette · add gradients/shadows · blur the pixels.

---

## Colors

Palette **"Braise & Or"** — the heat of the challenge + the value of the bond. Flat, high-contrast.

### Primary
| Name | HEX | RGB | Role |
|------|-----|-----|------|
| Braise | `#E6392C` | 230, 57, 44 | Primary accent (the ball, energy) |
| Or (Gold) | `#F5C451` | 245, 196, 81 | Secondary accent (the sync, the precious) |
| Encre (Ink) | `#0E0F14` | 14, 15, 20 | Background / outline |
| Blanc (White) | `#FFFFFF` | 255, 255, 255 | Ball base |

### Functional (game states)
| Name | HEX | Use |
|------|-----|-----|
| Vie (Life) | `#5AD94E` | HP ok / alive |
| Alerte | `#FFD23F` | Low HP |
| Danger | `#FF4D57` | Critical / KO |

### Players (up to 4)
`#E6392C` · `#36D1DC` · `#A78BFA` · `#FF9F45`

---

## Typography

- **Display — Jersey 10** (pixel, condensed): titles, wordmark, big numbers (levels, HP).
- **Body — Nunito** (400/600/700/800): UI text, menus, descriptions.
- **Hierarchy:** H1/wordmark & H2 → Jersey 10 · sub-headers → Nunito 700 caps · body → Nunito ·
  numeric data → Jersey 10.

---

## Tone of voice

Like the friend who runs the party night: **direct, fun, encouraging** — never corporate.

- ✓ "Here we go! The emulator is starting…" · "💀 Charmander fainted — its link too."
- ✗ "Please wait during initialization." · "An error occurred (0x4)." · jargon, cold, formal.

---

## Design tokens

```css
:root{
  --braise:#e6392c;  --or:#f5c451;  --encre:#0e0f14;  --blanc:#ffffff;
  --vie:#5ad94e;     --alerte:#ffd23f;  --danger:#ff4d57;
  --p1:#e6392c; --p2:#36d1dc; --p3:#a78bfa; --p4:#ff9f45;
  --bg:#0f1118; --surface:#181c26; --line:#262c3a; --txt:#e8ebf2; --mut:#9aa0b2;
  --font-display:'Jersey 10', sans-serif;
  --font-body:'Nunito', system-ui, sans-serif;
  --radius:14px; --space:24px;
}
```

---

## Imagery

- Pokémon sprites: animated Gen 5 GIFs (PokeAPI) — **never** ROM rips.
- Pixel-art icons/frames, crisp outlines, no gradients.
- Menus: light & airy. In-game overlay: dark (Ink) with Gold accents. DS-window framing.
