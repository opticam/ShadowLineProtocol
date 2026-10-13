# Shadow Line Protocol: Era 1 Demo

A playable sketch of Era 1 ("Landfall") from the design docs in [Docs/](Docs/). Every visual is a cardboard-cutout stand-in built from Unity primitives at runtime. There are no prefabs, scenes, materials or art assets yet.

## Opening it in Unity

1. Install **Unity 2022.3 LTS or Unity 6** with Unity Hub.
2. In Unity Hub, choose **Add > Add project from disk** and pick this `shadow-line-protocol` folder. Hub will ask which editor version to use; pick the one you installed.
3. When the editor opens you'll be in an empty untitled scene. Press **Play**.

The game builds itself when Play starts (`Era1Game` uses `RuntimeInitializeOnLoadMethod`), so it runs from any scene. To keep a scene around, use **File > Save As** to save it as `Assets/Scenes/Era1Demo.unity`.

**Note:** Unity creates a large `Library/` folder. OneDrive syncing it can slow Unity down or lock files, so consider moving the project out of OneDrive or excluding `Library/`, `Temp/` and `Logs/`.

## How to play

You've landed near Shackleton crater with three colonists, a lander and limited oxygen. Pass Earth's Sponsor Review by getting oxygen production online before the end of Sol 3.

| Control | Action |
| --- | --- |
| WASD / arrow keys | Pan the camera (hold Shift for faster) |
| Q / E | Rotate the camera |
| Mouse wheel | Zoom |
| Hover | Outlines what's under the cursor (yellow); selected colonists are outlined green |
| Left-click a colonist, deposit, foundation or cargo pod | Opens an action menu (mine, build, recover, stop) |
| Left-click / drag on the ground | Select colonists by dragging a box (Shift adds to the selection) |
| Right-click | Shortcut: move, mine a deposit, or build a foundation with the selected colonists |
| 1-4 | Place Habitat, Solar Array, Comms Tower, Oxygen Plant |
| R | Rotate while placing |
| Esc / right-click | Cancel placing |

**Loop**

- Colonists mine **regolith** (near the lander) and **ice** (in the shadowed crater), then haul it to the lander or a habitat.
- Buildings must be within **power range** of the lander (18 m) or a solar array (14 m). Chain solar arrays to reach further out.
- **Power:** the lander's fuel cell makes 3 power. Solar arrays make 6 each, but only in daylight. Any surplus charges the battery, which carries you through the night. If the battery runs flat while demand is higher than supply, you get a **brownout**: the Oxygen Plant stops and life support leaks oxygen.
- **Oxygen:** the crew uses oxygen all the time. The Oxygen Plant turns 1 ice into 2 oxygen every 2 seconds.
- **Day/night:** a sol is 150 s of daylight and 60 s of night. The sun circles low around the horizon, as it does at the pole.

**Objectives:** recover the stray cargo pod, build a Habitat, a Solar Array and a Comms Tower, survive the first night, build an Oxygen Plant, then produce 60 oxygen before Sol 3 ends.

**You lose if** oxygen runs out or Sol 3 ends before the target is met.

## Code layout

| Folder | Contents |
| --- | --- |
| `Scripts/Core` | `Era1Game` (bootstrap, win/lose), `Colony` (stockpiles, power, oxygen), `DayNightCycle`, `Era1Objectives` |
| `Scripts/World` | `WorldBuilder` (map layout), `Cutouts` (placeholder art), `Selectable` (click box + outline), `MapLabel` |
| `Scripts/Buildings` | `BuildingDef` (stats), `Building`, `BuildingPlacer` |
| `Scripts/Units` | `Colonist` (move, mine, haul, build) |
| `Scripts/Economy` | `ResourceType`, `ResourceNode` |
| `Scripts/Input` | `SelectionManager` |
| `Scripts/Camera` | `RTSCamera` |
| `Scripts/UI` | `HUD` (IMGUI) |

**Swapping in real art later:** replace the primitives in `Cutouts.BuildingVisual` and `WorldBuilder.CreateColonist`/`CreateDeposit` with prefab instantiation.

## Troubleshooting

- **Error about `UnityEngine.Input` and the Input System package:** go to **Edit > Project Settings > Player > Active Input Handling** and choose **Input Manager (Old)** or **Both**.
- **Everything is pink:** the project is set to URP/HDRP. The demo expects the Built-in Render Pipeline.
