using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    public enum TerrainType
    {
        Open,
        Boulder,
        Debris,
    }

    // Movement costs and back-links from one start cell, for move previews and paths.
    public class PathMap
    {
        public Vector2Int Start;
        public readonly Dictionary<Vector2Int, int> Cost = new Dictionary<Vector2Int, int>();
        public readonly Dictionary<Vector2Int, Vector2Int> Parent = new Dictionary<Vector2Int, Vector2Int>();

        public bool Contains(Vector2Int cell) => Cost.ContainsKey(cell);

        public List<Vector2Int> PathTo(Vector2Int target)
        {
            var path = new List<Vector2Int>();
            if (!Cost.ContainsKey(target)) return path;
            for (var c = target; c != Start; c = Parent[c]) path.Add(c);
            path.Reverse();
            return path;
        }
    }

    // A square grid laid on the ground. Cells are (x, y) where y runs away from the staging zone.
    public class TacticalGrid
    {
        public const float CellSize = 2f;

        static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        public readonly int Width;
        public readonly int Depth;
        public readonly Vector3 Origin;

        readonly TerrainType[,] terrain;
        readonly Material[,] tiles;

        public TacticalGrid(int width, int depth, Vector3 origin)
        {
            Width = width;
            Depth = depth;
            Origin = origin;
            terrain = new TerrainType[width, depth];
            tiles = new Material[width, depth];
        }

        public TerrainType this[Vector2Int c]
        {
            get => terrain[c.x, c.y];
            set => terrain[c.x, c.y] = value;
        }

        public Vector3 Center => Origin + new Vector3(Width * CellSize * 0.5f, 0f, Depth * CellSize * 0.5f);

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Depth;

        public Vector3 CellCenter(Vector2Int c) => Origin + new Vector3((c.x + 0.5f) * CellSize, 0f, (c.y + 0.5f) * CellSize);

        public bool TryGetCell(Vector3 world, out Vector2Int cell)
        {
            Vector3 local = world - Origin;
            cell = new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.z / CellSize));
            return InBounds(cell);
        }

        // Chebyshev distance: diagonal steps count as one, as in XCOM.
        public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        public static int EnterCost(TerrainType t) => t == TerrainType.Debris ? 2 : 1;

        public void SetTileColor(Vector2Int c, Color color) => tiles[c.x, c.y].color = color;

        // Builds tiles, boulders and wreckage under root. Root must sit at the world origin of the grid.
        public void BuildVisuals(Transform root, int seed)
        {
            var rng = new System.Random(seed);
            Vector3 rootPos = root.position;
            var ground = Cutouts.Part(root, PrimitiveType.Plane, Center - rootPos + Vector3.down * 0.02f,
                new Vector3(Width * CellSize * 0.3f, 1f, Depth * CellSize * 0.3f), new Color(0.3f, 0.3f, 0.29f), true);
            ground.name = "Ground";

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Depth; y++)
                {
                    var c = new Vector2Int(x, y);
                    Vector3 local = CellCenter(c) - rootPos;
                    var tile = Cutouts.Part(root, PrimitiveType.Quad, local + Vector3.up * 0.02f,
                        new Vector3(CellSize * 0.94f, CellSize * 0.94f, 1f), Color.grey, false, new Vector3(90f, 0f, 0f));
                    tile.name = $"Tile {x},{y}";
                    tiles[x, y] = tile.GetComponent<Renderer>().material;

                    if (this[c] == TerrainType.Boulder)
                    {
                        float s = 1.4f + (float)rng.NextDouble() * 0.5f;
                        float grey = 0.36f + (float)rng.NextDouble() * 0.12f;
                        Cutouts.Part(root, PrimitiveType.Sphere, local + Vector3.up * 0.5f, new Vector3(s, s * 0.8f, s),
                            new Color(grey, grey, grey * 0.97f), false, new Vector3(0f, (float)rng.NextDouble() * 360f, 0f)).name = "Boulder";
                    }
                    else if (this[c] == TerrainType.Debris)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var offset = new Vector3((float)rng.NextDouble() - 0.5f, 0.12f, (float)rng.NextDouble() - 0.5f) * 1.2f;
                            var euler = new Vector3((float)rng.NextDouble() * 50f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 50f);
                            Cutouts.Part(root, PrimitiveType.Cube, local + offset, new Vector3(0.6f, 0.08f, 0.35f),
                                i == 0 ? new Color(1f, 0.5f, 0.1f) : new Color(0.7f, 0.7f, 0.72f), false, euler).name = "Wreckage";
                        }
                    }
                }
            }
        }

        // Dijkstra over 8 neighbours. Boulders and blocked cells are impassable; diagonals can't cut their corners.
        public PathMap Reachable(Vector2Int start, int budget, Func<Vector2Int, bool> blocked)
        {
            var map = new PathMap { Start = start };
            map.Cost[start] = 0;
            var open = new List<Vector2Int> { start };
            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (map.Cost[open[i]] < map.Cost[open[best]]) best = i;
                var current = open[best];
                open.RemoveAt(best);
                int cost = map.Cost[current];

                foreach (var d in Directions)
                {
                    var next = current + d;
                    if (IsWall(next, blocked)) continue;
                    if (d.x != 0 && d.y != 0 &&
                        (IsWall(current + new Vector2Int(d.x, 0), blocked) || IsWall(current + new Vector2Int(0, d.y), blocked))) continue;
                    int nextCost = cost + EnterCost(this[next]);
                    if (nextCost > budget) continue;
                    if (map.Cost.TryGetValue(next, out int old) && old <= nextCost) continue;
                    map.Cost[next] = nextCost;
                    map.Parent[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return map;
        }

        bool IsWall(Vector2Int c, Func<Vector2Int, bool> blocked) => !InBounds(c) || this[c] == TerrainType.Boulder || blocked(c);
    }
}
