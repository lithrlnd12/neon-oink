# When Pigs Can Fly! 🐷✈️

A browser game about a flying pig. Flap to a beat, flip between a flat 2D world and an open 3D one mid-flight, and — when you feel like it — start a **PIG BRAWL** with laser eyes and exploding poop bombs. Plays on desktop, mobile (installable PWA), and in **VR** (WebXR).

**Play it:** https://neon-oink.vercel.app

> The repo/URL slug is still `neon-oink` (the game's original name was *NEON OINK*); the game itself is now *When Pigs Can Fly!*. Saved scores and bookmarks use the slug, so it stays put.

---

## Modes

### 🎵 FLY — rhythm dimension flyer (the original game)
- Flap **on the beat** to score; chain on-beat flaps for **2× → 3× → 4×** multipliers.
- **Flip anytime** between a flat side-scrolling **2D** view and an open **3D** world — a smooth cinematic morph, not a hard cut.
- 10 levels with escalating score thresholds, rising BPM, and a day→dusk→night palette progression.
- Each run is a seeded world with a shareable **run code** (`PIG-XXXXX`); enter a friend's code as a **CHALLENGE** to fly the same world. Local top-10 leaderboard.

### 🔫 PIG BRAWL — combat sandbox
- You vs. 3 AI pigs in the open 3D world. **90-second** timed match, then a results screen.
- **Laser eyes** (hitscan, with a forward-cone aim-assist and a lock-on crosshair) and **poop bombs** (gravity-dropped, explode into chunks, splatter enemies brown).
- HP bars (yours + floating bars over enemies), KO + points scoring, hit flashes, knockback, pig squeals, and big pig-explosions on death.
- Off-screen enemy arrows so you never lose the fight.
- **The hook:** flip to 2D to line up a clean shot — but enemies that leave your flat plane vanish (like the crows). One 3D simulation; 2D is just a lens.

### 🥽 VR (WebXR)
- Open the site in a headset browser (e.g. Meta Quest) and tap **ENTER VR** — no install, no sideload.
- Camera rig with native head look-around, in-world HUD, comfort tunnel-vignette on turns.
- 2D mode shows up as a floating "theater" screen you watch in-headset.
- The VR button only appears on XR-capable browsers; desktop/mobile are unaffected.

---

## Controls

| Action | Keyboard / Mouse | Touch | VR |
|---|---|---|---|
| Flap | `Space` / Left-click (**hold to glide**) | Tap | Left trigger |
| Flip 2D↔3D | `Tab` / Right-click | FLIP button | Grip |
| Turn (3D) | `A` / `D` | Left-thumb joystick | Left stick X |
| Speed (3D) | `W` / `S` | Joystick up/down | Left stick Y |
| Laser (brawl) | `F` | 🔫 button | Right trigger |
| Poop bomb (brawl) | `C` | 💩 button | Right A/X button |
| Quit brawl | `Esc` | — | — |

Camera: toggle **first-person / third-person** for 3D from the main menu.

---

## Run it locally

It's a static site — **serve it over HTTP** (opening `index.html` as a `file://` URL breaks the service worker and ES-module imports). From the project root:

```bash
python -m http.server 8000     # then open http://localhost:8000
# or:  npx serve .
```

There is **no build step** and no dependencies to install — Three.js loads from a CDN via an import map.

## Deploy

The repo is linked to Vercel and **auto-deploys on push to `master`** (production) or any branch (preview URL). The Vercel CLI is optional:

```bash
vercel          # preview deploy
vercel --prod   # production deploy
```

---

## Tech & architecture

- **Single file:** all the game (HTML, CSS, and one `<script type="module">`) lives in `index.html` (~1700 lines, wrapped in an IIFE). No bundler.
- **3D:** [Three.js](https://threejs.org) r180 (ESM via import map) — ACES filmic tone mapping, soft shadows, PBR materials, instanced ground detail (grass/flowers/rocks), a shader sky dome. Clean low-poly pastoral art direction (no neon/bloom).
- **2D:** a separate Canvas2D overlay (`#c2`) drawn by `draw2D()` — the flat side-scroller.
- **Audio:** procedural music + SFX generated live with the Web Audio API (a beat scheduler drives the rhythm; pig squeals, laser zaps, and poop plops are all synthesized — no audio assets).
- **World:** infinite, chunked, procedurally generated around the pig and recycled as you fly.
- **Combat:** an additive module gated behind a `combat` flag; bots, lasers, poop, and VFX live in 3D world space and render in 2D via the lens.
- **VR:** a WebXR layer (camera rig, controllers, in-world panels) guarded behind `renderer.xr.isPresenting` so non-VR play is identical.
- **PWA:** `manifest.json` + `sw.js` (offline-capable, installable, landscape).

### Project files
| File | What |
|---|---|
| `index.html` | The entire game |
| `sw.js` | Service worker (offline cache) |
| `manifest.json` | PWA metadata |
| `vercel.json` | Static-hosting config |
| `PARKING-LOT.md` | Roadmap & future work (VR polish, multiplayer, known issues) |

### Dev gotchas
- **Bump the cache version** in `sw.js` (`const CACHE='neon-oink-vN'`) whenever you change `index.html`, or the PWA will serve a stale build. To force-refresh during testing: DevTools → Application → Service Workers → Unregister, then reload.
- `icon-192.png` referenced by the manifest is currently missing (harmless 404) — see `PARKING-LOT.md`.

---

## Roadmap

See **[PARKING-LOT.md](PARKING-LOT.md)** for what's planned — notably real-time multiplayer (the brawl is single-player today), which depends first on making the simulation fully deterministic (the 2D obstacle spawner still uses unseeded randomness).

---

*Built with [Claude Code](https://claude.com/claude-code).*
