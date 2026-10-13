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
| Left-click a colonist, deposit, foundation or cargo pod | Opens an action menu (mine, build, plan a mission, stop) |
| Left-click / drag on the ground | Select colonists by dragging a box (Shift adds to the selection) |
| Right-click | Shortcut: move, mine a deposit, build a foundation, or plan the cargo pod mission with the selected colonists |
| 1-4 | Place Habitat, Solar Array, Comms Tower, Oxygen Plant |
| R | Rotate while placing |
| Esc / right-click | Cancel placing |

**Loop**

- Colonists mine **regolith** (near the lander) and **ice** (in the shadowed crater), then haul it to the lander or a habitat.
- Buildings must be within **power range** of the lander (18 m) or a solar array (14 m). Chain solar arrays to reach further out.
- **Power:** the lander's fuel cell makes 3 power. Solar arrays make 6 each, but only in daylight. Any surplus charges the battery, which carries you through the night. If the battery runs flat while demand is higher than supply, you get a **brownout**: the Oxygen Plant stops and life support leaks oxygen.
- **Oxygen:** the crew uses oxygen all the time. The Oxygen Plant turns 1 ice into 2 oxygen every 2 seconds.
- **Day/night:** a sol is 150 s of daylight and 60 s of night. The sun circles low around the horizon, as it does at the pole.

**Objectives:** recover the stray cargo pod (a turn-based mission, see below), build a Habitat, a Solar Array and a Comms Tower, survive the first night, build an Oxygen Plant, then produce 60 oxygen before Sol 3 ends.

**You lose if** oxygen runs out, Sol 3 ends before the target is met, or every colonist is lost.

## Missions

Clicking the stray cargo pod opens a mission instead of a simple walk-over:

1. **Ready room.** The view zooms into the lander airlock (or the habitat once one is built). Assign up to 3 crew. On foot, that's the limit. Give each one a suit, two tools and one consumable from the colony's gear locker. Injured crew and crew at their dose limit can't go.
2. **Transit.** The team walks to the site. The colony runs at 6x speed for the 20 s of colony time the walk takes, and everyone else keeps working. Walking also costs each person 2 suit oxygen and 4 mSv of dose.
3. **Tactical map.** Turn-based, with the colony clock paused. Each crew member has 2 actions per turn: move (4 cells, or 2 when hauling something heavy), leap up to 3 cells over obstacles (+1 oxygen), or use a context action (pry, seal, pick up, carry, patch, refill, scan). Every turn costs 1 oxygen.
4. **Debrief.** The mission ends when the cargo pallet reaches the staging zone, turn 12 runs out, the cargo is destroyed, or nobody is left standing. You can also withdraw once everyone standing is in the staging zone. The results are applied to the colony, then the survivors walk home: another 20 s of colony time.

**The cargo pod mission:** the hatch is jammed, a tank vents at the end of turns 3 and 6 and ruptures on turn 9, and the crater rim's shadow sweeps across the site from turn 5 (cold costs suit integrity, sunlight adds dose). Downed crew have 3 turns: patch them, refill their oxygen, or carry them to the staging zone. Each colonist has a perk: Mateo hauls heavy loads at full speed, Hana pries and seals without tools, and Amara can scan for supply crates. Crew lost on a mission are gone for good.

| Tactical control | Action |
| --- | --- |
| Left-click a crew member / 1-3 / Tab | Select |
| Left-click a blue cell | Move there |
| L, then left-click a purple cell | Leap |
| Action panel (bottom right) | Context actions |
| Space / Enter | End turn |
| WASD, Q/E, mouse wheel | Pan, rotate 90°, zoom |

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
| `Scripts/Missions` | `MissionDirector` (phase flow, transit, debrief), `ReadyRoom` (squad and loadout), `TacticalMission` (turn-based map: setup, `.Rules`, `.UI`), `TacticalGrid`, `TacticalUnit`, `Gear` (gear and locker), `MissionTypes`, `UiKit` |

**Swapping in real art later:** replace the primitives in `Cutouts.BuildingVisual` and `WorldBuilder.CreateColonist`/`CreateDeposit` with prefab instantiation.

## Troubleshooting

- **Error about `UnityEngine.Input` and the Input System package:** go to **Edit > Project Settings > Player > Active Input Handling** and choose **Input Manager (Old)** or **Both**.
- **Everything is pink:** the project is set to URP/HDRP. The demo expects the Built-in Render Pipeline.
