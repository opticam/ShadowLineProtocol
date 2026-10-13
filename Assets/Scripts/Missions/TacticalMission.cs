using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadowLineProtocol
{
    // The turn-based mission map. Split across partial files: setup and input (here), rules (.Rules) and the UI (.UI).
    public partial class TacticalMission : MonoBehaviour
    {
        const int Width = 11;
        const int Depth = 15;
        const int StagingRows = 2;
        const int LeapRange = 3;
        const int RevealRange = 2;
        const int ScanRange = 6;
        const int VentDamage = 2;
        const int RuptureDamage = 3;
        const int VentRadius = 1;
        const int RuptureRadius = 2;
        const int RuptureTurn = 9;
        const int ShadowStartTurn = 5;
        const float SunDosePerTurn = 2f;
        const float CameraPitch = 55f;
        static readonly int[] VentTurns = { 3, 6 };

        // Top row is the far edge (y = Depth - 1). '#' boulder, 'D' wreckage, 'P' pod, 'T' tank, 'h' hatch, 'c' supply crate.
        static readonly string[] Layout =
        {
            "......#....",
            "...DD.D...c",
            "..#.DPTD...",
            ".....h..#..",
            ".#..DD.....",
            "c........D.",
            "..##...D...",
            "......#...#",
            "D.....#....",
            "...D.......",
            ".#.....##c.",
            "...........",
            "...#.....#.",
            "...........",
            "...........",
        };

        public event Action<MissionResult> Finished;

        MissionDef def;
        Camera cam;
        TacticalGrid grid;
        readonly List<TacticalUnit> units = new List<TacticalUnit>();
        readonly List<TacticalItem> crates = new List<TacticalItem>();
        TacticalItem cargo;
        TacticalUnit selected;
        PathMap moveMap;
        int turn = 1;
        Vector2Int podCell, tankCell, hatchCell;
        bool hatchOpen, tankSealed, tankRuptured, cargoRuined, finished, leapMode, confirmWithdraw;
        GameObject hatchDoor, tankBody, tankSeal;
        readonly List<string> log = new List<string>();
        Vector2Int? hoverCell;

        Vector3 camFocus;
        float camYaw, camYawTarget, camDistance = 34f;

        public void Init(MissionDef def, List<CrewLoadout> squad, Camera cam)
        {
            this.def = def;
            this.cam = cam;
            grid = new TacticalGrid(Width, Depth, transform.position);
            ParseLayout();
            grid.BuildVisuals(transform, 11);
            BuildPod();
            SetUpCrates();

            for (int i = 0; i < squad.Count; i++)
            {
                var cell = new Vector2Int(Width / 2 - 1 + i, 0);
                var go = new GameObject(squad[i].Colonist.ShortName);
                go.transform.SetParent(transform, false);
                var unit = go.AddComponent<TacticalUnit>();
                unit.Init(squad[i], cell, grid.CellCenter(cell), squad[i].SuitDef.Oxygen - def.TransitOxygen, def.TransitDose);
                units.Add(unit);
            }

            camFocus = grid.CellCenter(new Vector2Int(Width / 2, 5));
            Log($"Turn 1. The team reaches the {def.Site.ToLower()}.");
            Log("Open the hatch, then haul the cargo pallet back to the staging zone.");
            RevealNearbyCrates();
            Select(units[0]);
        }

        void ParseLayout()
        {
            for (int y = 0; y < Depth; y++)
            {
                string row = Layout[Depth - 1 - y];
                for (int x = 0; x < Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    switch (row[x])
                    {
                        case '#': grid[c] = TerrainType.Boulder; break;
                        case 'D': grid[c] = TerrainType.Debris; break;
                        case 'P': podCell = c; break;
                        case 'T': tankCell = c; break;
                        case 'h': hatchCell = c; break;
                        case 'c': crates.Add(new TacticalItem { Cell = c, Revealed = false }); break;
                    }
                }
            }
        }

        Vector3 Local(Vector2Int c) => grid.CellCenter(c) - transform.position;

        void BuildPod()
        {
            var pod = new GameObject("Cargo pod").transform;
            pod.SetParent(transform, false);
            pod.localPosition = Local(podCell);
            Cutouts.Part(pod, PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.1f), new Vector3(1.8f, 1.7f, 1.8f), new Color(1f, 0.5f, 0.1f), false, new Vector3(8f, 12f, -6f));
            Cutouts.Part(pod, PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.1f), new Vector3(1.85f, 0.25f, 1.85f), Color.white, false, new Vector3(8f, 12f, -6f));
            hatchDoor = Cutouts.Part(pod, PrimitiveType.Cube, new Vector3(0f, 0.8f, -0.85f), new Vector3(1.1f, 1.1f, 0.1f), new Color(0.25f, 0.25f, 0.3f), false, new Vector3(0f, 12f, 0f));

            var tank = new GameObject("Venting tank").transform;
            tank.SetParent(transform, false);
            tank.localPosition = Local(tankCell);
            tankBody = Cutouts.Part(tank, PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(1f, 0.8f, 1f), new Color(0.92f, 0.92f, 0.95f));
            Cutouts.Part(tank, PrimitiveType.Cylinder, new Vector3(0f, 1.1f, 0f), new Vector3(1.03f, 0.08f, 1.03f), new Color(0.9f, 0.15f, 0.1f));
            tankSeal = Cutouts.Part(tank, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(1.06f, 0.1f, 1.06f), new Color(0.95f, 0.8f, 0.2f));
            tankSeal.SetActive(false);
        }

        void SetUpCrates()
        {
            // In parse order: nearest the staging zone first.
            var contents = new (string label, int regolith, int ice, GearId? gear)[]
            {
                ("oxygen canister", 0, 0, GearId.OxygenCanister),
                ("15 regolith binder", 15, 0, null),
                ("10 ice", 0, 10, null),
            };
            for (int i = 0; i < crates.Count; i++)
            {
                var crate = crates[i];
                var (label, regolith, ice, gear) = contents[i % contents.Length];
                crate.Name = $"supply crate ({label})";
                crate.Regolith = regolith;
                crate.Ice = ice;
                crate.Gear = gear;
                crate.Visual = CreateItemVisual("Supply crate", crate.Cell, new Vector3(0.8f, 0.6f, 0.8f), new Color(0.85f, 0.85f, 0.8f), new Color(1f, 0.5f, 0.1f));
                crate.Visual.SetActive(false);
            }
        }

        GameObject CreateItemVisual(string name, Vector2Int cell, Vector3 size, Color body, Color stripe)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Local(cell) + new Vector3(0.45f, 0f, 0.45f);
            Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, size.y * 0.5f, 0f), size, body);
            Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x + 0.04f, size.y * 0.2f, size.z + 0.04f), stripe);
            return go;
        }

        void PlaceItemVisual(TacticalItem item)
        {
            item.Visual.SetActive(true);
            item.Visual.transform.localPosition = Local(item.Cell) + new Vector3(0.45f, 0f, 0.45f);
        }

        IEnumerable<TacticalItem> AllItems()
        {
            if (cargo != null) yield return cargo;
            foreach (var crate in crates) yield return crate;
        }

        // ---- Grid queries ----

        bool InStaging(Vector2Int c) => c.y < StagingRows;
        bool Blocked(Vector2Int c) => c == podCell || c == tankCell;
        static bool Adjacent(Vector2Int a, Vector2Int b) => TacticalGrid.Distance(a, b) <= 1;

        bool Occupied(Vector2Int c, TacticalUnit ignore = null) =>
            units.Any(u => u != ignore && u.Status != UnitStatus.Lost && u.CarriedBy == null && u.Cell == c);

        bool Free(Vector2Int c, TacticalUnit ignore = null) =>
            grid.InBounds(c) && grid[c] != TerrainType.Boulder && !Blocked(c) && !Occupied(c, ignore);

        static int ShadowRowsAt(int t) => Mathf.Clamp(t - ShadowStartTurn + 1, 0, Depth);
        bool InShadow(Vector2Int c) => c.y >= Depth - ShadowRowsAt(turn);
        bool ShadowNext(Vector2Int c) => !InShadow(c) && c.y >= Depth - ShadowRowsAt(turn + 1);

        Vector2Int FreeCellNear(Vector2Int center, Func<Vector2Int, bool> extra = null)
        {
            for (int r = 0; r <= 4; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var c = center + new Vector2Int(dx, dy);
                        if (Free(c) && (extra == null || extra(c))) return c;
                    }
                }
            }
            return center;
        }

        bool CanLeapTo(TacticalUnit u, Vector2Int c)
        {
            int d = TacticalGrid.Distance(u.Cell, c);
            return d >= 1 && d <= LeapRange && Free(c) && !u.CarryingHeavy && u.Oxygen > 1;
        }

        void RefreshMoveMap()
        {
            moveMap = selected != null && selected.Standing && selected.ActionPoints > 0
                ? grid.Reachable(selected.Cell, selected.MoveRange, Blocked)
                : null;
        }

        void Select(TacticalUnit unit)
        {
            if (selected != null) selected.SetSelected(false);
            selected = unit;
            leapMode = false;
            if (selected != null) selected.SetSelected(true);
            RefreshMoveMap();
        }

        void SelectNextReady()
        {
            int start = selected != null ? units.IndexOf(selected) : -1;
            for (int i = 1; i <= units.Count; i++)
            {
                var u = units[(start + i + units.Count) % units.Count];
                if (u.Standing && u.ActionPoints > 0)
                {
                    Select(u);
                    return;
                }
            }
            RefreshMoveMap();
        }

        void Log(string line)
        {
            log.Add(line);
            if (log.Count > 40) log.RemoveAt(0);
        }

        // ---- Frame loop ----

        void Update()
        {
            UpdateCamera();
            if (!finished)
            {
                hoverCell = MouseCell();
                HandleInput();
            }
            else
            {
                hoverCell = null;
            }
            PaintTiles();
        }

        void HandleInput()
        {
            bool busy = units.Any(u => u.Animating);

            if (Input.GetKeyDown(KeyCode.Tab)) SelectNextReady();
            for (int i = 0; i < units.Count && i < 3; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) && units[i].Standing) Select(units[i]);
            }
            if (Input.GetKeyDown(KeyCode.L)) ToggleLeap();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                leapMode = false;
                confirmWithdraw = false;
            }
            if (busy) return;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) EndTurn();
            if (Input.GetMouseButtonDown(0) && hoverCell.HasValue) Click(hoverCell.Value);
        }

        void ToggleLeap()
        {
            if (selected == null || !selected.Standing || selected.ActionPoints <= 0 || selected.CarryingHeavy) return;
            leapMode = !leapMode;
        }

        void Click(Vector2Int c)
        {
            var unitThere = units.FirstOrDefault(u => u.Standing && u.CarriedBy == null && u.Cell == c);
            if (unitThere != null)
            {
                Select(unitThere);
                return;
            }
            if (selected == null || !selected.Standing || selected.ActionPoints <= 0) return;

            if (leapMode)
            {
                if (CanLeapTo(selected, c)) DoLeap(selected, c);
            }
            else if (moveMap != null && moveMap.Contains(c) && c != selected.Cell && Free(c))
            {
                DoMove(selected, c);
            }
        }

        Vector2Int? MouseCell()
        {
            Vector2 mouse = Input.mousePosition;
            if (OverUI(mouse)) return null;
            var ray = cam.ScreenPointToRay(mouse);
            if (!new Plane(Vector3.up, grid.Origin).Raycast(ray, out float enter)) return null;
            return grid.TryGetCell(ray.GetPoint(enter), out var cell) ? cell : (Vector2Int?)null;
        }

        void UpdateCamera()
        {
            float dt = Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.Q)) camYawTarget += 90f;
            if (Input.GetKeyDown(KeyCode.E)) camYawTarget -= 90f;
            camYaw = Mathf.MoveTowardsAngle(camYaw, camYawTarget, 360f * dt);

            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.z -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            camFocus += Quaternion.Euler(0f, camYaw, 0f) * input.normalized * camDistance * 0.8f * dt;
            Vector3 min = grid.Origin, max = grid.Origin + new Vector3(Width, 0f, Depth) * TacticalGrid.CellSize;
            camFocus.x = Mathf.Clamp(camFocus.x, min.x, max.x);
            camFocus.z = Mathf.Clamp(camFocus.z, min.z, max.z);

            if (!OverUI(Input.mousePosition))
                camDistance = Mathf.Clamp(camDistance * (1f - Input.mouseScrollDelta.y * 0.1f), 14f, 55f);

            var rotation = Quaternion.Euler(CameraPitch, camYaw, 0f);
            cam.transform.SetPositionAndRotation(camFocus - rotation * Vector3.forward * camDistance, rotation);
        }

        void PaintTiles()
        {
            HashSet<Vector2Int> hoverPath = null;
            bool canAct = !finished && selected != null && selected.Standing && selected.ActionPoints > 0;
            if (canAct && !leapMode && hoverCell.HasValue && moveMap != null && moveMap.Contains(hoverCell.Value) && Free(hoverCell.Value))
                hoverPath = new HashSet<Vector2Int>(moveMap.PathTo(hoverCell.Value));

            bool tankLive = !tankSealed && !tankRuptured;
            bool tankFiresNow = tankLive && (VentTurns.Contains(turn) || turn == RuptureTurn);
            int hazardRadius = turn == RuptureTurn ? RuptureRadius : VentRadius;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Depth; y++)
                {
                    var c = new Vector2Int(x, y);
                    Color col = grid[c] == TerrainType.Debris ? new Color(0.4f, 0.33f, 0.27f)
                        : grid[c] == TerrainType.Boulder ? new Color(0.3f, 0.3f, 0.3f)
                        : new Color(0.47f, 0.46f, 0.44f);
                    if (InStaging(c)) col = Color.Lerp(col, new Color(0.25f, 0.6f, 0.35f), 0.55f);
                    if (tankLive && TacticalGrid.Distance(c, tankCell) <= hazardRadius)
                        col = Color.Lerp(col, new Color(1f, 0.45f, 0.1f), tankFiresNow ? 0.6f : 0.25f);
                    if (InShadow(c)) col = Color.Lerp(col * 0.35f, new Color(0.08f, 0.1f, 0.25f), 0.35f);
                    else if (ShadowNext(c)) col *= 0.75f;

                    if (canAct)
                    {
                        if (leapMode && CanLeapTo(selected, c)) col = Color.Lerp(col, new Color(0.75f, 0.45f, 1f), 0.45f);
                        else if (!leapMode && moveMap != null && moveMap.Contains(c) && c != selected.Cell && Free(c))
                            col = Color.Lerp(col, new Color(0.3f, 0.6f, 1f), 0.4f);
                    }
                    if (hoverPath != null && hoverPath.Contains(c)) col = Color.Lerp(col, new Color(0.55f, 0.85f, 1f), 0.55f);
                    if (hoverCell == c) col = Color.Lerp(col, new Color(1f, 0.92f, 0.4f), 0.5f);
                    col.a = 1f;
                    grid.SetTileColor(c, col);
                }
            }
        }
    }
}
