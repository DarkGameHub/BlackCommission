using System.Collections.Generic;

namespace BlackCommission.Level
{
    /// <summary>
    /// Pure, Unity-free 1-D slot search for door-fill clearance: given a wall-hugging prop centred at
    /// <c>along</c> (offset from the room centre, metres) and a set of forbidden bands on that wall
    /// (each door opening plus the footprints of other props), find the nearest offset that lands the
    /// prop clear of every band and within the wall. Deterministic and EditMode-testable — no scene,
    /// transform, or engine types (the assembly is <c>noEngineReferences</c>).
    /// </summary>
    public static class DoorClearance
    {
        /// <summary>Breathing room kept on each side of a door opening (metres). Small — on a 4 m wall
        /// a 2 m door (the plan's max face fraction) leaves only 1 m a side, so a big margin would force
        /// otherwise-slideable props to be removed instead.</summary>
        public const float Clearance = 0.15f;

        /// <summary>
        /// Nearest centre offset to <paramref name="along"/>, inside [-<paramref name="maxOffset"/>,
        /// +<paramref name="maxOffset"/>], that lies in none of the <paramref name="forbidden"/> bands.
        /// Candidates are the prop's current position, the two wall ends, and every band edge (so the
        /// prop comes to rest just clear of whatever blocks it). Returns NaN when no free spot exists
        /// (e.g. the prop is wider than the wall, or the wall is fully covered) — the caller then
        /// removes the prop.
        /// </summary>
        public static float NearestFree(float along, float maxOffset,
            IReadOnlyList<(float lo, float hi)> forbidden)
        {
            if (maxOffset < 0f) return float.NaN; // prop is wider than the wall

            var candidates = new List<float> { along, -maxOffset, maxOffset };
            for (int i = 0; i < forbidden.Count; i++)
            {
                candidates.Add(forbidden[i].lo);
                candidates.Add(forbidden[i].hi);
            }

            float best = float.NaN, bestDist = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                float c = candidates[i];
                if (c < -maxOffset) c = -maxOffset;
                else if (c > maxOffset) c = maxOffset;
                if (InsideAny(c, forbidden)) continue;
                float dist = c < along ? along - c : c - along;
                if (dist < bestDist) { bestDist = dist; best = c; }
            }
            return best;
        }

        // A point exactly on a band edge counts as clear (strict interior, small epsilon).
        static bool InsideAny(float c, IReadOnlyList<(float lo, float hi)> bands)
        {
            const float e = 1e-3f;
            for (int i = 0; i < bands.Count; i++)
                if (c > bands[i].lo + e && c < bands[i].hi - e) return true;
            return false;
        }
    }
}
