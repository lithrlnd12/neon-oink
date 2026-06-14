# When Pigs Can Fly! — Unity Asset Map

Port reference mapping every visual/audio element from `index.html` (Three.js / Canvas2D / Web Audio) to a Unity equivalent.

## Rendering baseline

| Browser setting | Unity equivalent | Notes |
|-----------------|------------------|-------|
| Tone mapping: ACESFilmic, exposure 0.92 | URP → Camera → **ACES** tone mapping, exposure 0.92 | Match in Volume Overrides or Camera component. |
| Color space: sRGB | Player Settings → **Linear** color space with sRGB output | Default for URP; keep it. |
| Shadows: PCFSoftShadowMap | URP → Soft Shadows, cascade 2 splits | Directional light shadow resolution 2048 (1024 on low-end / XR). |
| Bloom: UnrealBloomPass (strength 0.28, radius 0.5, threshold 0.88) | URP **Bloom** volume override: intensity 0.28, scatter 0.5, threshold 0.88, dirt disabled | Subtle, non-neon use. |
| Environment map: RoomEnvironment, intensity 0.55 | URP Reflection Probe (Baked/Realtime), intensity 0.55 | Or use a simple skybox / HDRI. |
| Fog: Fog(color, 70, 240) | URP Fog volume: Linear, start 70, end 240, color from palette | Sync with `applyPalette()` level colors. |
| Sky: custom shader dome | **Skybox/Gradient** or URP Shader Graph sky dome | See Environment > Sky Dome. |

---

## 1. Player Pig

Group created by `buildPig(bodyCol=0xff9ec0, darkCol=0xe87aa4)`. All parts are primitive geometry; no external model required.

| Element | Browser (Three.js) | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|-------------------|------------------|---------------------------|------|
| Body | `SphereGeometry(0.85,16,12)`, scale `(1.15,1,1)` | Procedural Mesh / Primitive: **Sphere** scaled 1.15× X | Color `#ff9ec0`, `MeshStandardMaterial` roughness 0.6 → URP **Lit**, smoothness ~0.4, metallic 0 | MVP |
| Head | `SphereGeometry(0.55,14,10)` at `(0.95,0.25,0)` | Sphere child, radius 0.55, local position `(0.95,0.25,0)` | Same pink body material | MVP |
| Snout | `CylinderGeometry(0.22,0.26,0.25,12)`, rotated Z 90° at `(1.5,0.2,0)` | Cylinder, rotated, radius top 0.22 / bottom 0.26, height 0.25 | Color `#e87aa4`, URP Lit roughness 0.6 | MVP |
| Eyes | `SphereGeometry(0.07,8,6)` at `(1.25,0.5,±0.22)` | Two small spheres | Color `#222222`, unlit material (no roughness) | MVP |
| Ears | `ConeGeometry(0.16,0.32,8)` at `(1.05,0.75,±0.3)`, rotated X ±0.4 rad | Two cones | Dark material `#e87aa4` | MVP |
| Wings | Group of Box + Cone primitives per wing | Prefab with 4 primitives per wing, mirrored for R wing | Color `#ffffff`, transparent opacity 0.92, roughness 0.45 → URP Lit, alpha 0.92, smoothness 0.55 | MVP |
| Tail | `TorusGeometry(0.15,0.05,8,12,4.5)` at `(-1,0.15,0)` | Torus / ring segment | Dark material `#e87aa4` | MVP |
| Wing animation | Code-driven `wing.rotation.x` flap | Animator or script on wing root: sine-driven rotation | Speed tied to `pig.vy` and glide state | MVP |
| Body pitch/bank | `rotateZ(vy*0.045)` and `rotateX(-bank)` | Script on pig root: pitch from vertical velocity, bank from turn input | Limits: bank ±0.55 rad; pitch ±0.6 rad | MVP |

### Wing construction detail

Each wing is a child group containing:

1. Inner feather: `Box(0.6,0.08,0.55)` at `(0.15,0.04,0.32)`, rot X 0.25
2. Mid feather: `Box(0.9,0.07,0.42)` at `(-0.4,0.04,0.28)`, rot X 0.18, scale Z 0.7
3. Tip: `Cone(0.18,0.55,7)` rotated Z 90° at `(-0.95,0.03,0.24)`, scale Y 0.7, Z 0.55
4. Trail: `Box(0.65,0.06,0.32)` at `(-0.3,0.02,0.52)`, rot X 0.45, scale Z 0.6

Left wing root position `(-0.25,0.58,0.55)` scale `(1.25,1.25,1.25)`.  
Right wing root position `(-0.25,0.58,-0.55)` scale `(1.25,1.25,-1.25)` (mirrored).

---

## 2. Enemy Pigs (Pig Brawl)

Same geometry as player, but tinted.

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Enemy bodies | `buildPig(tint, darken(tint))` | Reuse player pig prefab, swap body & dark material colors | Tints: `#8fd0ff`, `#bcff8f`, `#ffd27a`, `#c69bff`; dark = tint × 0.74 | MVP |
| Floating HP bar bg | `Sprite`, black, opacity 0.55, scale `(1.7,0.22,1)` | World-space **Slider** or Sprite/Quad above pig | Black `#000000` alpha 0.55, size 1.7×0.22 | MVP |
| Floating HP bar fill | `Sprite`, green/yellow/red | World-space Slider fill | `#7fff6a` (>50%), `#ffd24a` (>25%), `#ff5a5a` (<25%) | MVP |
| Hit flash | Body material color set to white for 0.12s | Script toggles material color override or emissive white | White `#ffffff` | MVP |
| Poop stain | Body material color set to brown while `poopT > 0` | Material color override `#6b4423` | Brown `#6b4423` | MVP |
| 2D lens representation | Canvas2D ellipses | Reuse 2D sprite sheet or procedural CanvasTexture | Same tints, tiny 32×4 HP pip | MVP |

---

## 3. Obstacles

### 3.1 Barn

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Barn body | `BoxGeometry(w,h,d)` | Cube primitive / ProBuilder | Color `#d9534f`, roughness ~0.9; `w = 5–8`, `h = 6–10`, `d = 5–8` | MVP |
| Roof | `ConeGeometry(max(w,d)*0.75, 3, 4)`, rot Y 45° | Pyramid / Cone, 4 sides | Color `#8a2f2c` | MVP |
| Door | `PlaneGeometry(w*0.28, h*0.45)` | Quad on front face | Color `#6e1f1f`, roughness 0.9 | MVP |
| Loft window | `PlaneGeometry(w*0.18, w*0.18)` | Quad | Color `#fff4c4`, emissive `#221a0a` low intensity | MVP |

### 3.2 Silo

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Silo cylinder | `CylinderGeometry(r,r,h,12)` | Cylinder | Color `#c9cdd4`; `r = 1.8–2.8`, `h = 10+rand*8+level*0.8` | MVP |
| Dome top | `SphereGeometry(r,12,8,…,π/2)` | Hemisphere | Color `#9aa1ab` | MVP |
| Bands | `CylinderGeometry(r+0.04,r+0.04,0.18,12)` ×3 | Three thin cylinders | Color `#aab0b8`, roughness 0.5 | MVP |

### 3.3 Tree

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Trunk | `CylinderGeometry(0.35,0.5,h*0.85,8)` | Cylinder | Color `#8b5a2b`; `h = 4–9` | MVP |
| Canopy | 5 `SphereGeometry(lr*0.55,9,7)` blobs | Group of 5 low-poly spheres | Leaf colors `#3e8e41`, `#4a9e4d`, `#5dae52`; `lr = 2–3.5` | MVP |

### 3.4 Fence / Neon Gate

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Pylons | `CylinderGeometry(0.5,0.5,CEIL+2,8)` ×2 | Two cylinders | Color `#39d2ff`, emissive `#0066ff` intensity 0.35, roughness 0.4 | MVP |
| Beams | `BoxGeometry(HALF_W*2,1,1)` ×2 | Two scaled cubes | Same emissive cyan material; `HALF_W = 6` | MVP |
| Moving gate | Beams scale Y to open/close gap | Animated scale + position on beam objects | Gap moves vertically over time at level ≥5 | MVP |

`CEIL = 30`. Low beam fills from ground to `gapY - gapH/2`; high beam from `gapY + gapH/2` to `CEIL+2`.

### 3.5 Balloon

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Balloon | `SphereGeometry(1.6,12,10)`, scale Y 1.2 | Sphere | Color `#b084f5`, roughness 0.35, radius 1.6 | MVP |
| Bob animation | `pos.y = baseY + sin(time)*amp` | Script sine bob | Amplitude 0 at low levels, 1+ at level ≥3 | MVP |

### 3.6 Ring (collectible)

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Ring | `TorusGeometry(2.4,0.3,10,24)` | Torus | Color `#ffd700`, emissive `#ffaa00` intensity 0.6, metalness 0.85, roughness 0.25 | MVP |
| Rotation | `rotation.y += dt*0.5` | Script rotation | Constant spin | MVP |
| Taken state | Clone material, opacity 0.25 | Swap to transparent material or fade alpha | Alpha 0.25 | MVP |

### 3.7 Coin (collectible)

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Coin | `CylinderGeometry(1,1,0.25,5)`, rot X 90° | Cylinder (pentagon) | Color `#9ef01a`, emissive same intensity 0.55, metalness 0.6, roughness 0.3 | MVP |
| Spin | `rotation.z = now*0.003` | Script rotation | Spin on Z | MVP |

### 3.8 Crow

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Body | `SphereGeometry(0.5,10,8)`, scale `(1.4,1,1)` | Scaled sphere | Color `#23232b` | MVP |
| Head | `SphereGeometry(0.3,8,6)` at `(0.65,0.2,0)` | Sphere | Same black | MVP |
| Beak | `ConeGeometry(0.1,0.3,6)`, rot Z -90° | Cone | Color `#ffaa00` | MVP |
| Wings | `BoxGeometry(0.7,0.06,1)` ×2 | Two flat boxes | Color `#16161c` | MVP |
| Flight animation | Wings rotate X ±sin | Script wing flap | Orbit radius 2–6, speed ~0.6–1.5 | MVP |

---

## 4. Environment

| Element | Browser | Unity equivalent | Colors / Materials / Sizes | MVP? |
|---------|---------|------------------|---------------------------|------|
| Ground plane | `PlaneGeometry(900,900)`, CanvasTexture | Large quad / terrain with procedural texture | Base `#74c365`; texture has 600 random dots `#6bb85a`/`#82cc6e` and 80 soft darker spots | MVP |
| Grass patches | `CircleGeometry(3,10)` ×60 | 60 flat circle meshes or decal projector | Color `#5fae52`, random scale 0.5–2, scattered in 380×380 area | MVP |
| Grass tufts | `InstancedMesh` of `ConeGeometry(0.16,0.9,5)`, 420 instances | Unity **GPU Instancer** or `Graphics.DrawMeshInstanced` | Color `#5a9e4e`, flat shaded, height scale 0.4–1.1 | MVP |
| Flowers | `InstancedMesh` of `SphereGeometry(0.18,6,5)`, 120 instances | GPU instanced spheres | Colors: `#ffffff`, `#ffd1e8`, `#fff3a8`, `#ffb1a1`, `#cfe8ff` | MVP |
| Rocks | `InstancedMesh` of `DodecahedronGeometry(0.6)`, 36 instances | GPU instanced dodecahedrons | Color `#9aa1ab`, roughness 0.95, random rotation/scale 0.5–1.8 | MVP |
| Clouds | Groups of 3 spheres, 16 clouds | Prefab: 3 overlapping spheres | Color `#ffffff`, opacity 0.9, flat shaded, random scale 1–2.5 | MVP |
| Mountains | Groups of cones, 14 mountains | Prefab: main cone + secondary cone | Color derived from palette × 0.72, flat shaded; main height 45–100, radius 34–70 | MVP |
| Sun / Moon | `SphereGeometry(13,16,12)` | Sphere, unlit | Day `#fff3b0`, dusk `#ffc48a`, night `#dfe2ff` | MVP |
| Stars | `PointsMaterial`, 500 points | Particle system or VFX graph point cloud | Color `#cfe8ff`, size 1.7, opacity 0→0.95 at night | MVP |
| Sky dome | `SphereGeometry(420,32,20)` + custom gradient shader | Shader Graph skybox or large inverted sphere | Gradient from fog color (horizon) to deeper zenith; sun disc via pow falloff | MVP |
| Wind streaks | `CylinderGeometry(0.025,0.025,1.2,4)` ×48 | Object pool of thin cylinders | Color `#ffffff`, opacity 0.55, additive/unlit | Polish |

### Palette progression (level 1 → 10)

Browser stores 10 palettes. Level-up calls `applyPalette()`. Unity should replicate the same 10 ScriptableObjects or a lookup table keyed by level.

Key color transitions:

| Level | Sky/fog | Ground | Grass | Sun color | Cloud opacity | Flowers | Hemi intensity |
|-------|---------|--------|-------|-----------|---------------|---------|----------------|
| 1–2 | `#8ecae6` | `#74c365` | `#5a9e4e` | `#fff4dd` | 0.9 | on | 0.9 |
| 3 | `#9cb6e0` | `#7cb868` | — | — | — | — | — |
| 4 | `#9a90c8` | `#86a05a` | — | — | — | — | — |
| 5 | `#8a6cb8` | `#6f7c4f` | — | `#ffc48a` (celMat) | — | — | — |
| 6 | `#5a4a98` | `#4a5c48` | — | — | 0.4 | — | 0.55 |
| 7 | `#3d3478` | `#37424a` | — | `#9fb4ff` | — | — | 0.35 |
| 8 | `#281e58` | `#252e3e` | — | — | 0.35 | off | 0.35 |
| 9 | `#180f40` | `#1a2030` | — | — | — | — | — |
| 10 | `#0c0820` | `#101522` | — | — | — | — | — |

Ground/grass/patch colors are multiplied by the palette each level:
- `mtnMat.color = palette[4] * 0.72`
- `grassMat.color = palette[4] * 0.85`
- `patchMat.color = palette[4] * 0.78`

---

## 5. VFX

| Element | Browser | Unity equivalent | Colors / Materials / Settings | MVP? |
|---------|---------|------------------|------------------------------|------|
| Laser beam | `CylinderGeometry(0.13,0.13,len,6)`, `MeshBasicMaterial` | Cylinder stretched between eye and target, unlit additive | Player `#bff4ff`; enemy `#ff5a5a`; opacity 0.95→0; life 0.16s | MVP |
| Poop puff | `SphereGeometry(0.6,8,6)`, expanding + fading | Particle System burst or VFX Graph | Color `#6b4423`; scale 1→4 over 0.5s; opacity 0.9→0 | MVP |
| Poop gibs | 14 small spheres | Particle System with physics | Brown `#5a3a1e`/`#7a4f2a`; random velocity; gravity -30 | MVP |
| Pig explosion puff | White expanding sphere | Particle burst | `#ffffff`; same expansion | MVP |
| Pig explosion gibs | 18 tinted/pink/white spheres | Physics particles | Colors: enemy tint, `#ff9ec0`, `#ffffff`, `#e87aa4` | MVP |
| Kill popup | DOM text `KO! +50` | World-space **TextMeshPro** or UI Text animation | Font bold, `#9ef01a`, scale pop + float up | MVP |
| Beat popup | DOM `+BEAT!` / `+BEAT! 2×` | UI Text + Animator | White `#ffffff`, shadow glow `#39d2ff` | MVP |
| Level banner | DOM overlay | UI Text / TextMeshPro | Big `#9ef01a` with glow, sub white | MVP |
| Hurt vignette | DOM radial gradient red | UI Image radial gradient | Red `#ff0000` alpha 0.5, fade over 0.32s | MVP |
| Screen flash | DOM white overlay | Full-screen UI Image flash | White alpha 0.6→0 over 0.35s | MVP |
| Flip flash | DOM white overlay during 2D↔3D | UI Image + script | White alpha 0.45–0.75 | MVP |
| Ceiling warning | Red bar at top of 2D canvas | UI Image | `#ff5050` alpha 0.18 | MVP |
| Beat bar (2D) | Canvas2D rectangles | UI Slider or horizontal layout | Background `rgba(0,0,10,0.55)`, tick `#39d2ff` / center `#ff2fb9` | Polish |
| Wind streaks | Object pool of cylinders | Object pool + script | White, opacity 0.55, spawn when speed > 14 in 3D | Polish |
| Off-screen enemy arrows | DOM CSS triangles | UI Image arrow | Tinted to enemy color, clamp to screen edge | MVP |
| Crosshair | DOM circle + dot | UI Image | 26px circle, white 75% opacity; lock-on turns red `#ff4040` with bg `#ff3c3c` alpha 0.2 | MVP |

---

## 6. UI / HUD

Most browser HUD is DOM/CSS. Unity equivalents use **uGUI** Canvas in Screen Space - Overlay (or World Space for VR).

| Element | Browser | Unity equivalent | Colors / Fonts / Sizes | MVP? |
|---------|---------|------------------|------------------------|------|
| Score | `#score`, 38px bold white | uGUI TextMeshPro | `#ffffff`, bold 38px, shadow 0 0 16 white | MVP |
| Rings count | `#rings`, 13px bold gold | TextMeshPro | `#ffd700`, bold 13px, prefix `⭕` | MVP |
| Level / BPM | `#lvl`, 12px bold green | TextMeshPro | `#9ef01a`, bold 12px, letter spacing 1px | MVP |
| Multiplier | `#mult`, 22px bold pink | TextMeshPro | `#ff2fb9`, bold 22px, shadow glow | MVP |
| Chain | `#chain`, 11px light pink | TextMeshPro | `#ff9de2`, bold 11px | MVP |
| Mode indicator | `#mode` box | uGUI Panel + TextMeshPro | Black bg alpha 0.5, white border, `2D` cyan `#39d2ff`, `3D` pink `#ff2fb9` | MVP |
| Heading | `#heading`, 10px teal | TextMeshPro | `#8aa`, 10px | MVP |
| Player HP bar | `#hpbar` 190×15 | uGUI Slider | Black bg alpha 0.5, border white 35%; fill `#7fff6a`/`#ffd24a`/`#ff5a5a` | MVP |
| Crosshair | `#xhair` | UI Image circle + dot | 26px, white; lock red | MVP |
| Hurt overlay | `#hurt` | Full-screen UI Image radial gradient | Red vignette | MVP |
| Vignette | `#vig` | Post-process Vignette or UI Image | Black radial alpha 0.32 | MVP |
| Beat bar | 2D canvas bottom bar | UI horizontal group | See VFX section | Polish |
| Menu overlay | `#overlay` | uGUI Panel with buttons | Gradient title text, dark bg `rgba(4,4,16,0.86)`, blur backdrop | MVP |
| Buttons (Start, Combat, Codes, Camera, Kids) | DOM buttons | uGUI Button + TextMeshPro | Gradient / solid styles per button spec | MVP |
| Leaderboard | `#lb` | Scroll View + row prefabs | Cyan header, green highlight for “me” | Polish |
| VR HUD panel | CanvasTexture on quad | World Space Canvas | Panel `rgba(12,14,26,0.8)`, white score, gold rings, green level | VR phase |
| VR message panel | CanvasTexture on quad | World Space Canvas | Panel `rgba(12,14,26,0.85)`, large white title | VR phase |
| VR tunnel vignette | Alpha-gradient quad parented to camera | VFX or image on camera-aligned quad | Black radial gradient, opacity 0–0.6 | VR phase |
| VR 2D theater | `CanvasTexture(flatCanvas)` on plane | Render Texture + Quad | 3.4m in front, aspect-fit scale | VR phase |
| Mobile joystick | DOM `#joyBase` / `#joyKnob` | uGUI joystick pack or custom | Cyan ring alpha 0.5, knob alpha 0.55 | MVP (mobile) |
| Combat buttons | DOM `#laserBtn`, `#poopBtn` | uGUI buttons | Cyan gun, brown poop | MVP (mobile/brawl) |

---

## 7. Audio

All browser audio is procedural Web Audio. In Unity, use **Audio Source** + runtime generated clips, or a simple synthesizer plugin. For MVP, generated clips are easiest.

| Sound | Browser synthesis | Unity equivalent | Settings | MVP? |
|-------|-------------------|------------------|----------|------|
| Kick drum | Oscillator 150→35Hz + gain envelope | Synthesized clip or FMOD/AudioKit kick | Duration ~0.2s, gain 0.9→0.001 | MVP |
| Closed hi-hat | Noise buffer, highpass 7kHz, short gain | Noise clip + highpass | Gain 0.15, decay 0.05s | MVP |
| Open hi-hat | Same, longer decay | Noise clip | Gain 0.25, decay 0.18s | Level ≥4 |
| Bass | Sawtooth at freq/2, lowpass 400+level×60 | Synthesized clip | Gain 0.28, dur = beat*0.9 | MVP (level ≥2) |
| Arp | Triangle freq×2 | Synthesized clip | Gain 0.12, 0.1s per note | Level ≥3 |
| Sub bass | Sawtooth freq/2, dur beat×3.5 | Synthesized clip | Every 4 beats | Level ≥5 |
| Pad | Square, lowpass 900Hz, slow attack/decay | Synthesized clip | Gain up to 0.06, every 2 beats | Level ≥7 |
| Beat ding | Sine 880×(1+multiplier×0.18) | Synthesized clip | Gain 0.3, decay 0.22s | MVP |
| Swoosh (flip) | Sine sweep 200↔900Hz | Synthesized clip | Gain 0.25, 0.32s | MVP |
| Squeal (hit) | Sawtooth + square, bandpass 1100Hz Q=5, LFO 26Hz | Synthesized clip | Big 0.55s / small 0.22s | MVP |
| Zap (laser) | Square sweep 320/900→900/120 | Synthesized clip | Gain 0.18, 0.16s | MVP (brawl) |
| Plop (poop spawn) | Sine 360→80 | Synthesized clip | Gain 0.28, 0.22s | MVP (brawl) |
| Splat (explosion) | Noise, lowpass 600Hz | Synthesized clip | Gain 0.4, 0.3s | MVP (brawl) |

### BPM schedule

- BPM = 90 + (level-1)×8
- Beat interval = 60/BPM
- Scheduler runs every 25ms and queues notes ahead 0.12s

Unity recommendation: implement a `BeatScheduler` coroutine / audio DSP callback that spawns AudioSource instances per event, or use a dedicated audio middleware.

---

## 8. Lighting

| Element | Browser | Unity equivalent | Settings | MVP? |
|---------|---------|------------------|----------|------|
| Hemisphere light | `HemisphereLight(0xbfdfff,0x74c365,0.9)` | URP ambient / Skybox + ground reflection | Sky `#bfdfff`, ground tint from palette, intensity 0.35–0.9 by level | MVP |
| Directional sun | `DirectionalLight(0xfff4dd,2.4)` | URP Directional Light | Color `#fff4dd`→`#9fb4ff`; intensity 0.8–2.2 by level; shadow resolution 2048; shadow distance ~180; bias -0.0006; normal bias 0.3 | MVP |
| Shadows | `PCFSoftShadowMap` | URP soft shadows | 2 cascades; near 10, far 180; ortho size ±55 | MVP |

---

## 9. VR / 2D Theater notes

The browser uses a camera rig group and a separate 2D canvas overlay. In Unity:

- Use an **XR Rig** (XR Origin + Camera Offset).
- Parent world-space HUD panels to the rig or to the camera offset.
- 2D mode in VR: render the main game camera to a **Render Texture** and display it on a world-space quad ~3.4m in front of the player.
- Comfort tunnel vignette: camera-aligned quad with radial gradient texture, opacity driven by turn rate and vertical speed.
- Camera in non-VR: script-driven third-person (offset -11, +4.2y, looking ahead +10) or first-person (snout ride).

---

## 10. Recommendations for preserving the low-poly stylized look

1. **Use flat shading.** The browser sets `flatShading:true` on grass, flowers, rocks, mountains, tree canopy. In URP Lit, set **Smoothness to 0** and disable normal maps, or use a custom Shader Graph with flat normals.
2. **Keep primitive geometry.** Almost every object is a sphere, box, cone, cylinder, or torus. Avoid high-poly models unless a future art pass demands them. ProBuilder + primitives will match the source exactly.
3. **Replicate the palette system.** Store the 10 palettes in a ScriptableObject or enum-driven gradient. Update material colors, light colors, fog, and sky in a single `ApplyPalette(level)` call.
4. **Match materials, not just colors.** URP Lit parameters:
   - Pink pig body: smoothness ~0.4, metallic 0
   - Ring/coin: metallic 0.6–0.85, smoothness 0.25–0.3, emissive
   - Ground: smoothness 0.05 (roughness 0.95)
   - Fence: emissive cyan + low smoothness
5. **Keep bloom subtle.** The browser bloom is intentionally low (strength 0.28, threshold 0.88). Do not crank it up; the game is pastel/pastoral, not neon.
6. **Use GPU instancing for ground details.** 420 grass, 120 flowers, 36 rocks: use `Graphics.DrawMeshInstanced` or Unity’s GPU Instancing to match the chunked recycling behavior.
7. **Keep the camera FOV punch.** In 3D, base FOV 62, speed-reactive up to 76, plus a +24 FOV kick during 2D↔3D morph. Use a Cinemachine camera with FOV override or animate Camera.fieldOfView.
8. **2D layer is a lens, not a separate game.** In Unity, keep a single 3D simulation and render the 2D view via an orthographic camera or a CanvasTexture/RenderTexture that projects the world onto a flat plane. Off-plane enemies and obstacles should vanish.
9. **Audio: synthesize or ship tiny clips.** The original has zero audio files. For fidelity, generate WAV clips at runtime (C# `AudioClip.Create`) using the same envelope shapes, or pre-render short clips and keep the beat scheduler in C#.
10. **Keep shadow frustum tracking the pig.** The directional light and its shadow camera follow the player. Use URP shadow cascades centered on the player, or script the light’s position each frame.
