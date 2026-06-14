# Unity Port Architecture: When Pigs Can Fly!

**Source:** Browser game built with Three.js in `index.html` (~1,886 lines).  
**Goal:** Recreate the same gameplay loop, art direction, and feel in Unity while expanding platform reach.  
**Constraint:** Do not modify any existing browser files (`index.html`, `sw.js`, `manifest.json`, `README.md`).

---

## 1. Feature Mapping: Browser → Unity

| Browser Feature | Unity Equivalent | Notes |
|-----------------|------------------|-------|
| **Flight physics** | Custom `Rigidbody`-driven pig controller | Gravity `GRAV = -30`, flap impulse `FLAP_V = 11`, glide caps vertical velocity at `-3.2`. Use `Rigidbody` with interpolation + velocity directly set each `FixedUpdate` for arcade feel rather than true simulation. |
| **2D/3D flip** | Morphing camera rig + dimension state machine | Single world simulation; 2D is a rendering/input lens. A `DimensionManager` toggles `Is3D`, drives a cinematic FOV/blur/crossfade, swaps camera rig targets, and changes input interpretation. |
| **Rhythm system** | `AudioSource` + `Metronome` + beat window | Web Audio scheduler becomes a C# `Metronome` driven by `AudioSettings.dspTime` or `AudioSource.time`. Beat window: 0.09s (0.22s kids). `OnBeatFlap` increments chain → 2×/3×/4× multipliers. |
| **Procedural world** | `WorldGenerator` + chunk object pools | Seedable RNG (`System.Random` with saved seed). Chunks are `60m` cells; spawn barns, silos, trees, fences, crows, balloons, coins, rings based on level density curves. |
| **2D corridor injection** | `CorridorInjector` | Spawns guaranteed hazards/rewards in front of the pig in 2D mode using deterministic seeded RNG (fixes the browser's `Math.random` non-determinism noted in `PARKING-LOT.md`). |
| **Obstacles & dynamics** | Component-based obstacle actors | Each obstacle type gets a MonoBehaviour: `CrowSwarmer`, `FenceGate`, `BalloonBobber`, `CoinSpinner`, `RingSpinner`. Pool meshes for performance. |
| **Palette/level progression** | `LevelProfile` ScriptableObjects | 10 levels with per-level BPM (90→162), palette hex list, fog color, light intensities, star opacity, sun/moon color. `LevelManager` swaps profiles and lerps global lighting/fog. |
| **Pig BRAWL combat** | `CombatManager` + `BotAgent` | 3 AI bots with state machine (approach/retreat/strafe/bob). Hitscan lasers with cone aim assist. Physics-free poop bombs with gravity arc and AoE splat. Floating HP bars, KOs, timer, points. |
| **VR support** | OpenXR + camera rig + controllers | Recreate the WebXR camera rig: headset drives `Camera.main` inside a parent rig. Left stick = turn/speed, triggers = flap/laser, grip = flip. In-world Canvas HUD + theater screen for 2D mode. |
| **PWA → standalone** | Unity Player builds | WebGL replaces/supplements browser version; PC/Mac, Android/iOS, Quest VR prioritized. Use Addressables or asset bundles only if needed; keep single-scene architecture. |
| **Audio synthesis** | AudioMixer + generated clips / Wwise or FMOD optional | Browser uses live Web Audio synthesis. In Unity, generate short SFX clips at runtime with `AudioClip.Create` for pure procedural parity, or author short WAVs for quality. Music is a quantized event system. |
| **Save/leaderboard** | `PlayerPrefs` / JSON + cloud later | Local top-10 run code, score, level, best chain. Unity `PlayerPrefs` or a JSON file in `Application.persistentDataPath`. |

---

## 2. Recommended Unity Project Structure

```
Assets/
├── _Project/
│   ├── Audio/
│   │   ├── AudioMixer.mixer
│   │   ├── Music/
│   │   └── SFX/
│   ├── Materials/
│   │   ├── Pig_Pink.mat
│   │   ├── Pig_Wing.mat
│   │   ├── Ground_Grass.mat
│   │   ├── Sky_Gradient.mat
│   │   └── Obstacles/
│   ├── Models/
│   │   └── (Blender source files / Asset Store imports)
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Obstacles/
│   │   ├── VFX/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── Bootstrap.unity
│   │   └── Game.unity
│   ├── ScriptableObjects/
│   │   ├── Levels/
│   │   ├── PigSkins/
│   │   └── GameConfig.asset
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Flight/
│   │   ├── World/
│   │   ├── Rhythm/
│   │   ├── Combat/
│   │   ├── VR/
│   │   ├── UI/
│   │   └── Audio/
│   ├── Settings/
│   │   ├── URP-ForwardRenderer.asset
│   │   ├── InputSystem_Actions.inputactions
│   │   └── OpenXR/
│   ├── Shaders/
│   │   ├── SkyDome.shader
│   │   └── WorldGradient.shader
│   └── Sprites/
│       └── UI/
├── Plugins/
└── StreamingAssets/
```

**Scenes:**
- `Bootstrap`: initializes save/leaderboard, audio, input, then loads `Game`.
- `Game`: contains the player, world generator, lighting, cameras, UI canvases, and `GameDirector`.

**Prefabs:**
- `PlayerPig` (model + collider + flight controller + combat shooter)
- `Chunk` (empty parent for spawned obstacles)
- `Barn`, `Silo`, `Tree`, `FenceGate`, `Crow`, `Balloon`, `Coin`, `Ring`
- `VFX_Poof`, `VFX_Splat`, `VFX_LaserBeam`, `VFX_PoopGibs`
- `UI_HUD`, `UI_MainMenu`, `UI_VRScreen`, `UI_VRHUD`

---

## 3. Specific Unity Systems per Feature

| Feature | Unity Systems/Packages |
|---------|------------------------|
| **Rendering** | Universal Render Pipeline (URP) for cross-platform consistency, bloom via URP Volume, soft shadows. Custom shader for the sky dome (gradient + sun disc + horizon fog). |
| **Input** | **Input System** package with action maps: `Desktop`, `Touch`, `XR`. Separate action maps for `Fly` and `Brawl`. Use `PlayerInput` or a custom `InputRouter`. |
| **Camera** | **Cinemachine** for non-VR third-person/first-person blending. Custom `VRCameraRig` for OpenXR. A `CameraDirector` chooses between 2D canvas, 3D third-person, 3D first-person, and VR rigs. |
| **Physics** | Physics-driven arcade flight using `Rigidbody` (not `CharacterController`). Colliders for pig/obstacles; use trigger checks for rings/coins. Combat lasers are raycasts, not physics. |
| **XR / VR** | **OpenXR Plugin** + **XR Plugin Management**. Target `Oculus` loader for Quest. Left controller: turn/speed/flap; right controller: laser/poop; grip: dimension flip. |
| **VFX** | **Visual Effect Graph** (targeting compatible platforms) or particle systems for poof, splat, gibs, laser beams, speed streaks. Use object pools for mobile/Quest. |
| **Audio** | **AudioMixer** with exposed volume parameters. Groups: Master, Music, SFX. Procedural SFX can be generated with `AudioClip.Create`; music scheduling via custom `Metronome` reading DSP time. |
| **UI** | Unity UI (uGUI) for menus/HUD; **TextMeshPro** for crisp text. For VR, use World Space Canvases parented to the rig. |
| **Terrain/World** | No Unity Terrain. Ground is a large scrolling plane or tile system. Instanced `Graphics.DrawMeshInstanced` or GPU Instancing for grass/flowers/rocks. |

---

## 4. C# Scripts & Responsibilities

### Core
- `GameDirector` — top-level state machine (`Menu`, `Flying`, `Dead`, `Brawl`, `Paused`). Owns `playing`, `dead`, `combat`, dimension state.
- `GameConfig` (ScriptableObject) — constants: gravity, flap velocity, speeds, level thresholds, beat windows.
- `SaveManager` — `PlayerPrefs`/JSON persistence for leaderboard, run codes, camera preference, kids mode.
- `RunCodeUtility` — encode/decode `PIG-XXXXX` seeds.

### Flight
- `PigController` — position, velocity, yaw, bank, stun, flap input, glide, ceiling/floor clamps. Emits events for `Flap`, `GlideStart/End`.
- `FlightCameraDirector` — manages 2D canvas, 3D third-person, 3D first-person, and morph crossfade state.
- `DimensionManager` — toggles `Is3D`, triggers morph, updates camera rig, input mode, and UI visibility.

### Rhythm
- `Metronome` — schedules beats using `AudioSettings.dspTime`, exposes `CurrentBPM`, `BeatPhase`, `IsInWindow()`.
- `RhythmComboSystem` — chain tracking, multiplier computation, beat-pop UI events.
- `MusicScheduler` — per-level instrumentation (kick, hat, bass, arp, pad) triggered on each beat.

### World
- `WorldGenerator` — seeded RNG, chunk coordinate management, spawn/despawn chunks around the pig.
- `Chunk` — container for a cell's obstacles.
- `ObstacleSpawner` — deterministic placement based on level density/chance curves.
- `CorridorInjector` — seeded guaranteed spawns in front of the pig during 2D flight.
- `DynamicObjectManager` — updates crows, balloons, fences, coins, rings each frame; handles recycling.
- `GroundDecorator` — grass/flower/rock instancing, cloud scattering, mountain scattering.

### Obstacle Behaviours
- `CrowSwarmer` — orbital movement, wing flap animation.
- `FenceGate` — moving/vertical gap logic, beam scale updates.
- `BalloonBobber` — sine bobbing.
- `CoinSpinner` — rotation + collection.
- `RingSpinner` — rotation + collection.

### Audio
- `ProceduralAudio` — runtime `AudioClip` generation for squeals, zaps, plops, splats.
- `SfxEmitter` — pooled `AudioSource` playback.
- `AudioMixerController` — exposed parameter snapshots.

### Combat
- `CombatManager` — starts/ends brawl, match timer, score, KOs, deaths, UI updates.
- `BotAgent` — AI state machine (approach/retreat/strafe/bob), firing logic, HP, respawn.
- `PlayerCombat` — laser fire, poop spawn, cooldowns, damage, invulnerability.
- `LaserBeam` — visual beam + hitscan.
- `PoopBomb` — gravity arc, collision/explosion, AoE damage.
- `Damageable` — HP interface, death events, hit flash.
- `FloatingHpBar` — world-space sprite/Canvas HP bar above bots.
- `OffScreenArrow` — screen-edge indicator for enemies.

### VR
- `VRCameraRig` — headset camera parent, rig position/rotation lerping, 3D/2D theater modes.
- `VRInputAdapter` — reads OpenXR controllers, maps to game actions.
- `VRHud` — world-space Canvas drawing score/rings/level.
- `VRTheaterScreen` — displays the 2D render texture in VR.
- `ComfortVignette` — alpha vignette quad tied to turn rate/vertical speed.

### UI
- `MainMenuUI` — start, brawl, run-code challenge, leaderboard, camera/kids toggles.
- `HUD` — score, rings, level/BPM, multiplier, chain, heading, HP bar, timer.
- `LevelBanner` — level-up animation.
- `BeatBar` — 2D rhythm visualization.
- `DeathScreen` — final score, run code, retry.

---

## 5. Asset Creation Plan

### Procedural / Code-Generated (keep like browser)
- **Pig model** — construct from primitive capsules/spheres/cones at runtime (matches current `buildPig()`). This preserves the charming low-poly look and makes tinting/skinning trivial.
- **Crow model** — same approach: spheres + boxes.
- **Barn, Silo, Tree, Fence, Balloon, Coin, Ring** — primitive-composite prefabs (Box, Cylinder, Cone, Torus equivalents).
- **Ground texture** — generate a 512×512 noise/grass texture at runtime via `Texture2D.SetPixels`.
- **Sky dome** — custom shader (no texture asset needed).
- **Clouds** — groups of spheres.
- **Mountains** — cones + smaller sub-peaks.
- **Grass/flowers/rocks** — `Graphics.DrawMeshInstanced` from a few simple meshes.
- **Speed streaks** — pooled cylinder meshes.
- **VFX** — particle systems or VFX Graph using built-in spheres.
- **Audio** — generated `AudioClip`s for parity, or authored short clips if memory/quality demands it.

### Actual 3D Assets (Blender / Unity Asset Store)
- **None required for MVP.** The browser game intentionally uses a primitive low-poly pastoral aesthetic.
- **Optional polish later:**
  - One rigged pig model from Blender if we want smoother wing animations/idle states.
  - A single stylized cloud mesh pack for nicer silhouettes.
  - UI icons/sfx clips if runtime generation proves too CPU-heavy on mobile.
  - Fonts: use TextMeshPro default or a free font matching the UI style.

**Decision:** Start 100% procedural/code-generated to match the source, reduce asset pipeline, and keep all platforms lightweight.

---

## 6. Phase-by-Phase Implementation Plan

### Phase 0 — Project Setup (1–2 days)
- Create Unity project (2022.3 LTS recommended) with URP, Input System, OpenXR packages.
- Configure URP Forward Renderer, PC/Mobile/Quest quality tiers.
- Set up folder structure, ScriptableObject templates, scene architecture.
- Create `GameConfig` constants matching `index.html`.

### Phase 1 — MVP Core Flight (1 week)
- Implement `PigController` with flap, glide, gravity, ceiling/floor.
- Implement 2D flight: auto-forward side-scroller feel using the `draw2D` logic (Canvas overlay + primitive world).
- Implement 3D flight: yaw/turn, speed control, bank.
- Implement `DimensionManager` with basic morph toggle (no cinematic polish yet).
- Implement `WorldGenerator` chunk spawning and obstacle primitive prefabs.
- Collision detection → die/restart loop.
- Basic HUD.

### Phase 2 — Rhythm & Progression (1 week)
- `Metronome` and `MusicScheduler`.
- Beat-window flap detection, combo multipliers, score, level thresholds.
- 10 level profiles with palette swaps, BPM, density curves.
- Level-up banner and beat bar UI.
- Run code generation/challenge + local leaderboard.

### Phase 3 — 2D/3D Polish (3–4 days)
- Cinematic morph: FOV kick, crossfade, blur, zoom, smoothstep easing.
- First-person/third-person camera toggle.
- Sky shader, lighting/fog lerping, starfield, day→dusk→night transitions.
- Speed streaks, wind feel, better wing animation.

### Phase 4 — Pig BRAWL (1 week)
- `CombatManager`, `BotAgent` AI, spawning.
- Player laser (hitscan + cone aim assist + lock-on crosshair).
- Poop bombs (arc + AoE splat + gibs).
- HP bars, KOs, points, 90-second timer, death/respawn.
- 2D lens rendering for bots/beams/poops.
- Mobile on-screen combat buttons.

### Phase 5 — VR (1 week)
- OpenXR setup, rig, controller input.
- In-world HUD and message panels.
- 2D theater screen via RenderTexture.
- Comfort vignette.
- Playtest on Quest.

### Phase 6 — Platforms & Polish (1 week)
- WebGL build optimization (compressed textures, code stripping).
- PC/Mac standalone.
- Android/iOS touch UI tuning.
- Quest VR build + performance pass.
- Audio final mix, save/leaderboard QA.

---

## 7. Platform Build Targets (Prioritized)

1. **WebGL** — primary target to replace/supplement the browser version. Use Unity 2022.3 LTS with Brotli compression, linear color space, URP. Keep build size under 25 MB compressed. Host on Vercel/Netlify alongside existing static files.
2. **PC / Mac Standalone** — fastest iteration and demo target. Full quality settings, mouse/keyboard, gamepad support via Input System.
3. **Android / iOS** — touch controls (tap to flap, FLIP button, virtual joystick in 3D), orientation lock to landscape, safe-area UI padding, performance tiering.
4. **Quest VR (Android-based)** — OpenXR with Quest loader, reduced shadow resolution, single-pass instanced rendering, foveated rendering if available, comfort vignette. Build as Android APK or App Lab submission.

---

## 8. Technical Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Determinism for multiplayer/ghosts** | High | Replace all `UnityEngine.Random` with seeded `System.Random`. Use fixed timestep for world updates. Store run seed and input replay for ghosts. This fixes the browser's `Math.random` 2D injector issue. |
| **WebGL build size & load time** | High | Strip unused engine code, use Brotli, compress textures, keep assets procedural, load assets from Addressables only if necessary. |
| **2D/3D morph feels as smooth as browser** | Medium | Replicate exact smoothstep timing (`MORPH_DUR = 0.6s`), FOV curve, blur via URP volume or canvas, crossfade. A/B test against browser recording. |
| **VR comfort / motion sickness** | High | Lerp rig height/yaw, never snap camera, comfort vignette on turns, maintain high frame rate (72/90 Hz), test on real Quest hardware early. |
| **Audio latency / beat sync on different platforms** | Medium | Use `AudioSettings.dspTime` for scheduling; pre-buffer one beat ahead; allow small platform-specific offset calibration. |
| **Performance on Quest / mobile** | High | Use GPU instancing for ground details, object pools, low shadow resolution in XR, LOD or simple culling, keep draw calls under 200 on Quest. |
| **Input complexity (desktop/touch/VR)** | Medium | Central `InputRouter` normalizes all sources into game-level actions. Avoid platform-specific code outside adapter classes. |
| **URP bloom/browser bloom mismatch** | Low | Tune URP Bloom intensity/threshold to match `UnrealBloomPass(0.28, 0.5, 0.88)`. Keep bloom subtle per current art direction. |
| **Touch vs. mouse conflict** | Low | Use Input System's control schemes and `Pointer` actions with proper touch filtering. |
| **Procedural audio CPU cost** | Medium | Generate clips once and cache; fall back to authored WAVs if profiling shows spikes, especially on mobile. |

---

## Key Architectural Decisions

1. **Single world, dual lens.** The 3D world is the source of truth. 2D mode is a camera/input/UI lens over the same data, exactly like the browser version. This preserves the core design and the "flip" mechanic.
2. **Arcade physics over realistic flight.** Use direct velocity manipulation on a `Rigidbody` instead of aerodynamic forces; this matches the tight, responsive feel of the original.
3. **Fully deterministic, seeded world.** Replace randomness with seeded `System.Random` for world generation, including the 2D corridor injector. This unblocks future multiplayer/ghost runs and fixes the browser's known determinism gap.
4. **Procedural assets for MVP.** All models and textures are generated from primitives/shaders to match the low-poly style, avoid asset pipeline overhead, and keep builds small.
5. **Input abstraction layer.** A single `InputRouter` consumes actions from the Input System and maps them to game commands, making touch/keyboard/gamepad/VR coexist cleanly.
6. **VR is a camera rig mode, not a separate game.** The same `PigController`, `CombatManager`, and `WorldGenerator` run in VR; only the camera rig and UI presentation change.
7. **Build priority: WebGL first.** The WebGL build is the direct successor to the browser version; other platforms are additive.
