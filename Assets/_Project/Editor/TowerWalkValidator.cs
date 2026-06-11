using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Geometric sweep of the critical walk paths (PM bug reports: stair head-bonk, island
/// floor steps). The whitebox is axis-aligned boxes, so instead of PhysX (unreliable in
/// background edit mode) this samples collider BOUNDS directly: every 0.2 m it finds the
/// ground (highest collider top reachable by a step), reports any rise over 0.3 m
/// ("needs a jump"), and reports every collider intersecting the player head column
/// (0.3 radius, 0.55..1.78 above ground). Pure read — run any time after a rebuild.
/// </summary>
public static class TowerWalkValidator
{
    const float PlayerRadius = 0.3f;
    const float HeadFrom = 0.55f;   // above ground: skip the step boxes underfoot
    const float HeadTo = 1.78f;     // player crown
    const float MaxStepRise = 0.3f; // CharacterController default stepOffset

    [MenuItem("Tools/Black Commission/MVP/Tower/Validate Walk Paths (stairs + island)")]
    public static void Validate()
    {
        var bounds = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => c.enabled && c.gameObject.activeInHierarchy)
            .Select(c => (b: c.bounds, name: Path(c.transform)))
            .ToList();

        var report = new StringBuilder("[WalkValidator]\n");

        Check(report, bounds, 0f, "B-stair (D5→run1→landing→run2→D30)", new[]
        {
            new Vector2(4.4f, 18f), new Vector2(3.6f, 18f), new Vector2(0.6f, 18f),
            new Vector2(0.6f, 22f), new Vector2(3.4f, 22f), new Vector2(3.8f, 22f),
        });

        Check(report, bounds, 0f, "A-stair (D10→run1→landing→run2→D35)", new[]
        {
            new Vector2(28f, 27.4f), new Vector2(27.4f, 28.6f), new Vector2(27f, 30f),
            new Vector2(27f, 33.8f), new Vector2(28f, 35f), new Vector2(29f, 33.8f),
            new Vector2(29f, 29.8f), new Vector2(28.5f, 28.6f),
        });

        Check(report, bounds, 4.2f, "Island (bridge→SALES→SHOWFLAT→TARGET eco column)", new[]
        {
            new Vector2(13f, 20f), new Vector2(24.5f, 20f), new Vector2(31f, 20f),
            new Vector2(36f, 20f), new Vector2(36f, 14f), new Vector2(37f, 12f),
        });

        Debug.Log(report.ToString());
    }

    static void Check(StringBuilder report, List<(Bounds b, string name)> all,
        float startGround, string label, Vector2[] waypoints)
    {
        report.AppendLine($"── {label}");
        float ground = startGround;
        var blockers = new HashSet<string>();
        var rises = new List<string>();

        foreach (Vector2 p in SamplePolyline(waypoints, 0.2f))
        {
            // Ground = highest collider top under the feet column that is reachable
            // from the current ground by a normal step (or already underfoot).
            float best = float.NegativeInfinity;
            foreach (var (b, _) in all)
            {
                if (p.x + PlayerRadius < b.min.x || p.x - PlayerRadius > b.max.x ||
                    p.y + PlayerRadius < b.min.z || p.y - PlayerRadius > b.max.z) continue;
                float top = b.max.y;
                if (top <= ground + MaxStepRise + 0.02f && top > best) best = top;
            }
            if (float.IsNegativeInfinity(best))
            {
                rises.Add($"  NO REACHABLE GROUND at ({p.x:0.0}, {p.y:0.0}) from y {ground:0.00} — gap or rise >{MaxStepRise}m");
                // try unrestricted ground to keep walking the path
                foreach (var (b, nm) in all)
                {
                    if (p.x < b.min.x || p.x > b.max.x || p.y < b.min.z || p.y > b.max.z) continue;
                    float top = b.max.y;
                    if (top <= ground + 1.2f && top > best) best = top;
                }
                if (float.IsNegativeInfinity(best)) continue;
            }
            ground = best;

            // Head column above the ground.
            float y0 = ground + HeadFrom, y1 = ground + HeadTo;
            foreach (var (b, nm) in all)
            {
                if (p.x + PlayerRadius < b.min.x || p.x - PlayerRadius > b.max.x ||
                    p.y + PlayerRadius < b.min.z || p.y - PlayerRadius > b.max.z) continue;
                if (b.max.y <= y0 || b.min.y >= y1) continue;
                blockers.Add($"  HEAD blocked by '{nm}' at ({p.x:0.0}, {p.y:0.0}) — object y {b.min.y:0.00}..{b.max.y:0.00}, ground {ground:0.00}");
            }
        }

        if (blockers.Count == 0 && rises.Count == 0) report.AppendLine("  CLEAR ✓");
        foreach (string b in blockers.OrderBy(s => s)) report.AppendLine(b);
        foreach (string r in rises) report.AppendLine(r);
    }

    static IEnumerable<Vector2> SamplePolyline(Vector2[] pts, float step)
    {
        for (int i = 0; i < pts.Length - 1; i++)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(pts[i], pts[i + 1]) / step));
            for (int k = 0; k <= n; k++)
                yield return Vector2.Lerp(pts[i], pts[i + 1], (float)k / n);
        }
    }

    static string Path(Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
