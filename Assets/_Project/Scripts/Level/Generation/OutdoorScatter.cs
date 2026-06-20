using System.Collections.Generic;

namespace BlackCommission.Level.Generation
{
    /// <summary>One scattered outdoor element (whitebox): planar position + type/scale/yaw. Pure data,
    /// no UnityEngine — so outdoor scatter is deterministic + unit-testable headless like the grid generator.</summary>
    public readonly struct ScatterItem
    {
        public readonly float X, Z, Scale, Yaw;
        public readonly int Type; // 0 = tree, 1 = rock, 2 = bush (the builder maps these to whitebox shapes)

        public ScatterItem(float x, float z, int type, float scale, float yaw)
        {
            X = x; Z = z; Type = type; Scale = scale; Yaw = yaw;
        }
    }

    /// <summary>
    /// Deterministic seeded scatter for the outdoor approach (ADR-0003 outdoor follow-up; whitebox until
    /// nature assets exist). A jittered grid over a rectangle: each grid cell yields one candidate whose RNG
    /// draws are ALL consumed BEFORE the keep/skip test, so the sequence — and thus the layout — is identical
    /// on every machine for a given seed regardless of the exclusion mask (same discipline as
    /// <see cref="GridMapGenerator"/>: one seeded System.Random, stable iteration order). The builder passes
    /// <paramref name="isBlocked"/> to carve out the building footprint, the van path, and the drop-off pad.
    /// </summary>
    public static class OutdoorScatterGenerator
    {
        public static List<ScatterItem> Generate(
            float minX, float minZ, float maxX, float maxZ,
            int seed, float cell, System.Func<float, float, bool> isBlocked, double fillChance = 0.55)
        {
            var items = new List<ScatterItem>();
            if (cell <= 0f || maxX <= minX || maxZ <= minZ) return items;

            var rng = new System.Random(seed);
            int nx = (int)((maxX - minX) / cell);
            int nz = (int)((maxZ - minZ) / cell);

            for (int ix = 0; ix < nx; ix++)
                for (int iz = 0; iz < nz; iz++)
                {
                    // Consume a FIXED set of draws per cell up front → the RNG stream is independent of which
                    // cells are kept, so exclusions never shift the deterministic layout.
                    double keep = rng.NextDouble();
                    float jx = (float)rng.NextDouble();
                    float jz = (float)rng.NextDouble();
                    int type = rng.Next(3);
                    float scale = 0.7f + (float)rng.NextDouble() * 1.6f;
                    float yaw = (float)rng.NextDouble() * 360f;

                    if (keep > fillChance) continue;

                    float x = minX + (ix + jx) * cell;
                    float z = minZ + (iz + jz) * cell;
                    if (isBlocked != null && isBlocked(x, z)) continue;

                    items.Add(new ScatterItem(x, z, type, scale, yaw));
                }

            return items;
        }
    }
}
