# Parking Lot

Ideas and future work, in rough priority order.

## VR support (WebXR) — committed, not started
Build VR-ready as we go; when we start, go straight to the **medium tier**:

- [ ] Camera rig group — headset owns camera pose; all camera logic (lookAt, FOV kick) guarded behind a non-VR check
- [ ] `renderer.xr.enabled` + `VRButton` from three/addons, switch loop to `renderer.setAnimationLoop`
- [ ] In-world HUD (score/multiplier/chain as meshes — DOM HUD invisible in VR)
- [ ] Controller input: trigger = flap, thumbstick = steer
- [ ] Comfort: tunnel vignette during turns, comfort settings toggle
- [ ] VR is 3D-mode only (2D rhythm mode is a flat canvas overlay)
- [ ] Test path: Vercel preview URL opened in Quest browser (HTTPS already satisfied; no sideloading needed)
- [ ] First-person camera option (in main menu since 2026-06) is the intended VR base; headset look-around comes free once the rig exists
- [ ] Possible parallel track: VR prototype on a branch/worktree by a second agent while main dev continues; merge the thin XR layer back rather than forking long-term

## Build-with-VR-in-mind rules (apply during all feature work)
- Keep scene Quest-friendly: low-poly, instancing, shadow resolution can drop in XR
- No screen-space-only effects for gameplay-critical info
- Audio is WebAudio — already VR-compatible

## Smaller items
- [ ] `icon-192.png` referenced by manifest.json is missing (404 on every load)
- [ ] Game name/UI still says "NEON" though neon visuals were removed — branding decision pending
