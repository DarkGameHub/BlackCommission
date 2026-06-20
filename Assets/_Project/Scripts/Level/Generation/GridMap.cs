using System.Collections.Generic;

namespace BlackCommission.Level.Generation
{
    /// <summary>What occupies a grid cell in a generated map. Pure data — no Unity types.
    /// <see cref="Room"/> = part of an open multi-cell room (no internal walls; owner = the room/anchor id).</summary>
    public enum CellKind { Empty, Anchor, Corridor, Room }

    /// <summary>Integer grid cell coordinate (value type, equatable for use as a dictionary/set key).</summary>
    public readonly struct GridCoord : System.IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y) { X = x; Y = y; }

        public bool Equals(GridCoord o) => X == o.X && Y == o.Y;
        public override bool Equals(object o) => o is GridCoord g && Equals(g);
        public override int GetHashCode() => unchecked((X * 73856093) ^ (Y * 19349663));
        public override string ToString() => $"({X},{Y})";

        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>
    /// Pure-data result of map generation (ADR-0003): a grid of cell kinds, anchor ownership per
    /// cell, and an undirected corridor link graph between adjacent cells. Engine-independent
    /// (no UnityEngine) so generation is deterministic and unit-testable headless; the Unity layer
    /// reads this to instantiate tile prefabs. Mirrors the pure-logic style of LootSpawnPlanner.
    /// </summary>
    public sealed class GridLayout
    {
        public int Width { get; }
        public int Height { get; }

        readonly CellKind[,] _cells;
        readonly string[,] _owner;
        readonly HashSet<(int, int, int, int)> _links = new HashSet<(int, int, int, int)>();

        public GridLayout(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new CellKind[System.Math.Max(0, width), System.Math.Max(0, height)];
            _owner = new string[System.Math.Max(0, width), System.Math.Max(0, height)];
        }

        public bool InBounds(GridCoord c) => c.X >= 0 && c.X < Width && c.Y >= 0 && c.Y < Height;
        public CellKind Kind(GridCoord c) => _cells[c.X, c.Y];
        public string Owner(GridCoord c) => _owner[c.X, c.Y];

        public void Set(GridCoord c, CellKind kind, string owner = null)
        {
            _cells[c.X, c.Y] = kind;
            _owner[c.X, c.Y] = owner;
        }

        /// <summary>Record a walkable connection between two cells; undirected (a–b == b–a).</summary>
        public void Link(GridCoord a, GridCoord b) => _links.Add(Key(a, b));

        /// <summary>True if a walkable connection was recorded between the two cells.</summary>
        public bool Linked(GridCoord a, GridCoord b) => _links.Contains(Key(a, b));

        public int LinkCount => _links.Count;

        static (int, int, int, int) Key(GridCoord a, GridCoord b)
        {
            // Canonical (order-independent) key so Link(a,b) and Link(b,a) collapse to one edge.
            if (a.X < b.X || (a.X == b.X && a.Y <= b.Y))
                return (a.X, a.Y, b.X, b.Y);
            return (b.X, b.Y, a.X, a.Y);
        }
    }
}
