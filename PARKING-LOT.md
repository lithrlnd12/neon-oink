# Parking Lot

Ideas and future work, in rough priority order.

## PIG BRAWL — combat (IN PROGRESS, on `feature/combat`)
Single-player combat sandbox built as a v1. Status:
- [x] Menu button "PIG BRAWL", own HUD (KOs + HP), Esc to quit
- [x] 3 dumb AI enemy pigs (tinted) — approach/retreat/strafe/bob, shoot back
- [x] Laser eyes (hitscan + forward-cone aim assist), keyboard F
- [x] Poop bombs (gravity drop + AoE splat), keyboard C
- [x] Health, hit reactions, KO + respawn, soft floor (no death pit in brawl)
- [x] The hook: flip to 2D to line up shots on-plane; off-plane enemies vanish like crows
- [x] VR controls wired (LEFT trigger flap, RIGHT trigger laser, RIGHT A/X poop, grip flip) — UNTESTED on headset
- [x] Mobile on-screen 🔫 / 💩 buttons
- [ ] DETERMINISM (prereq for any real multiplayer): 2D injected-obstacle spawner uses Math.random, not the seed — must be seeded + fixed-timestep before fair racing/ghosts/netcode
- [ ] Realtime netcode (PartyKit/Durable Objects) for 2–4 players, scale to 6
- [ ] Game modes (FFA, last-pig-flying, team poop war), power-ups (burrito/bacon/energy drink/tinfoil), beat-powered shots
- [ ] Polish: invuln-blink on player is overridden by the render block (cosmetic), tune AI difficulty, VR combat playtest
- [ ] Decide: merge to production or keep gated on branch until netcode-ready

## VR support (WebXR) — committed, not started
Build VR-ready as we go; when we start, go straight to the **medium tier**:

- [ ] Camera rig group — headset owns camera pose; all camera logic (lookAt, FOV kick) guarded behind a non-VR check
- [ ] `renderer.xr.enabled` + `VRButton` from three/addons, switch loop to `renderer.setAnimationLoop`
- [x] In-world HUD (score/multiplier/chain as meshes — DOM HUD invisible in VR)
- [x] Controller input: trigger = flap, thumbstick = steer, grip = flip 2D/3D
- [x] Comfort: tunnel vignette during turns
- [x] 2D mode IN VR via floating "theater" screen (2D canvas → CanvasTexture on a panel; stable level rig). Replaces the earlier "VR is 3D-only" plan.
- [x] Shipped to production (VR button only shows on XR headsets; non-VR unaffected)
- [ ] POLISH (needs headset): VR mode-switch jumps rig x/z instantly (height already lerps) — lerp position for a smooth in-VR transition; tune theater screen distance/size, vignette strength, HUD placement
- [ ] POLISH: comfort settings toggle (vignette on/off/strength)
- [x] Test path: Vercel preview URL opened in Quest browser (HTTPS already satisfied; no sideloading needed)
- [ ] First-person camera option (in main menu since 2026-06) is the intended VR base; headset look-around comes free once the rig exists
- [ ] Possible parallel track: VR prototype on a branch/worktree by a second agent while main dev continues; merge the thin XR layer back rather than forking long-term

## Build-with-VR-in-mind rules (apply during all feature work)
- Keep scene Quest-friendly: low-poly, instancing, shadow resolution can drop in XR
- No screen-space-only effects for gameplay-critical info
- Audio is WebAudio — already VR-compatible

## Smaller items
- [ ] `icon-192.png` referenced by manifest.json is missing (404 on every load)
- [ ] Game name/UI still says "NEON" though neon visuals were removed — branding decision pending
