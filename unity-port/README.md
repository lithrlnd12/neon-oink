# When Pigs Can Fly! — Unity Port

This folder contains the Unity project for porting the browser game *When Pigs Can Fly!* from Three.js to Unity.

## Unity Project

The actual Unity project lives at:

```
unity-port/WhenPigsCanFlyUnity/
```

Open it in **Unity 6000.4.10f1** or newer.

## Packages Already Configured

The project manifest includes:
- **Universal Render Pipeline (URP)** 17.4.0
- **Input System** 1.19.0
- **Cinemachine** 3.1.6
- **AI Assistant** 2.11.0-pre.1 (enables Unity MCP)
- **uGUI** 2.0.0

Unity will resolve these on first open.

## Unity MCP Connection

This project is set up to be controlled by AI agents via Unity MCP.

1. Open `WhenPigsCanFlyUnity` in the Unity Editor.
2. Go to **Edit → Project Settings → AI → Unity MCP**.
3. Confirm the **Unity Bridge** is **Running**.
4. Restart opencode so it loads the new `unity-mcp` MCP server from `.opencode/opencode.json`.
5. The first time opencode connects, approve the pending connection in Unity's MCP settings.
6. Once connected, you can ask opencode to inspect scenes, create GameObjects, edit scripts, and build the project through the Unity Editor.

## Scene Setup Notes

- Create a `Pig` GameObject with a `CharacterController` and attach `PigController.cs`.
- Create a `GameManager` GameObject and attach `GameManager.cs`, `RhythmManager.cs`, `AudioManager.cs`, `SaveManager.cs`, `DimensionFlipper.cs`, and `WorldGenerator.cs`.
- Create a `BrawlManager` GameObject and attach `PigBrawlManager.cs`.
- Create a `Canvas` with TextMeshPro HUD elements and attach `UIManager.cs`.
- Create the main `Camera` and assign it to `DimensionFlipper`.
- Create prefabs for barn, silo, tree, balloon, ring, coin, crow, fence and assign them in `WorldGenerator`.
- Create bot prefabs with `BotPigAI.cs` and assign them in `PigBrawlManager`.
- Optionally create an XR Origin / Camera Offset rig and assign it to `VRManager`.

## Script Overview

| Script | Purpose |
|--------|---------|
| `PigController.cs` | Player pig movement, flap, glide, 3D turn/speed, banking |
| `DimensionFlipper.cs` | 2D/3D mode switching and camera transition |
| `RhythmManager.cs` | Beat timing, BPM scaling, multiplier system |
| `WorldGenerator.cs` | Infinite chunked world, obstacle spawning |
| `PigBrawlManager.cs` | Combat mode setup, bots, timer, scoring |
| `BotPigAI.cs` | Enemy pig AI (approach/retreat/strafe/fire) |
| `LaserWeapon.cs` | Hitscan laser with aim assist |
| `PoopBomb.cs` | Gravity bomb, explosion, splatter |
| `GameManager.cs` | Game state, start/die/restart, score, level progression |
| `UIManager.cs` | HUD, menus, overlays |
| `AudioManager.cs` | Procedural/generated audio equivalent |
| `SaveManager.cs` | Leaderboard, run codes, settings persistence |
| `VRManager.cs` | XR rig setup (optional stub) |

## Key Constants from the Browser Version

These values are already wired into the `[SerializeField]` fields and can be tuned in the Inspector:

- Gravity: `-30`
- Flap velocity: `11`
- Ceiling: `30`
- Floor: `0.7`
- Speed min/max: `7` / `26`
- Turn rate: `1.7`
- Chunk size: `60`
- View chunks: `3`
- Corridor width: `3.2`
- Level thresholds: `0, 30, 70, 125, 195, 285, 400, 540, 710, 920`
- Combat HP: `100`, laser damage: `22`, bot damage: `12`, poop damage: `60`
- Match duration: `90` seconds

## Documentation

- `PORT-PLAN.md` — full architecture and phase plan
- `ASSET-MAP.md` — browser→Unity asset mapping

## Next Steps

- Implement `WorldGenerator` chunk spawning/recycling and obstacle dynamics.
- Implement `AudioManager` runtime AudioClip synthesis or replace with authored clips.
- Wire `PigBrawlManager` damage callbacks through `BotPigAI` and `LaserWeapon`.
- Build the flat 2D overlay renderer or use a second orthographic camera.
- Add animator/ParticleSystem effects for beats, level up, explosions, and gibs.
