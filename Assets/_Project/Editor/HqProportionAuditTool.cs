using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Read-only proportion audit for the HQ scene (PM 2026-06-12: 人物比例和事务所的
/// 比例不对，仔细看). Dumps every visible renderer's world bounds — size, bottom and
/// top height — plus the human-scale reference bands, to the console AND to
/// production/qa/hq-proportion-audit.md so the report can be reviewed outside Unity.
/// Changes nothing in the scene.
/// </summary>
public static class HqProportionAuditTool
{
    const string ScenePath = "Assets/_Project/Scenes/HQ.unity";
    const string ReportPath = "production/qa/hq-proportion-audit.md";

    // Player constants (Player.prefab): capsule 2.0 m, standing eye 1.7 m.
    const float PlayerHeight = 2.0f;
    const float PlayerEye = 1.7f;

    // Reference bands scaled to a 2.0 m human. (band.x .. band.y = plausible range)
    struct Band { public string match; public string what; public Vector2 range; public string axis;
        public Band(string m, string w, float lo, float hi, string ax) { match = m; what = w; range = new Vector2(lo, hi); axis = ax; } }

    static readonly Band[] Bands =
    {
        new("desk",      "桌面高 (top)",          0.72f, 0.95f, "top"),
        new("sofa",      "沙发座高 (top≈座面)",   0.40f, 0.60f, "top"),
        new("ceiling",   "净空 (bottom)",          2.60f, 3.60f, "bottom"),
        new("door",      "门洞高 (height)",        2.00f, 2.40f, "height"),
        new("van",       "厢式车高 (height)",      2.20f, 2.70f, "height"),
        new("rack",      "工具架高 (height)",      1.60f, 2.10f, "height"),
        new("shelf",     "置物架高 (top)",         1.10f, 2.00f, "top"),
        new("computer",  "屏幕中心 (center)",      1.00f, 1.45f, "center"),
        new("whiteboard","白板中心 (center)",      1.30f, 1.70f, "center"),
    };

    [MenuItem("Tools/Black Commission/MVP/HQ/Audit Proportions (report only)")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "HQ")
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var sb = new StringBuilder();
        sb.AppendLine("# HQ Proportion Audit");
        sb.AppendLine();
        sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm}  |  Scene: {scene.name}");
        sb.AppendLine($"Player: height {PlayerHeight:0.00} m, standing eye {PlayerEye:0.00} m");
        sb.AppendLine();
        sb.AppendLine("| object | size W×H×D (m) | bottom | top | h/player | flag |");
        sb.AppendLine("|---|---|---|---|---|---|");

        var rows = new List<(float top, string line)>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r.enabled) continue;
            if (r is ParticleSystemRenderer) continue;
            Bounds b = r.bounds;
            if (b.size.sqrMagnitude < 0.0001f) continue;

            string name = r.gameObject.name;
            string flag = Evaluate(name, b);
            string line = $"| {FullPath(r.transform)} | {b.size.x:0.00}×{b.size.y:0.00}×{b.size.z:0.00} " +
                          $"| {b.min.y:0.00} | {b.max.y:0.00} | {b.size.y / PlayerHeight:0.00}× | {flag} |";
            rows.Add((b.max.y, line));
        }

        foreach (var row in rows.OrderByDescending(t => t.top))
            sb.AppendLine(row.line);

        sb.AppendLine();
        sb.AppendLine("## Doorway / opening probes");
        sb.AppendLine();
        ProbeGap(sb, "办公区↔车库 门洞", new Vector3(0.06f, 0f, -2.35f), Vector3.right);
        ProbeGap(sb, "车库卷帘门洞", new Vector3(3.44f, 0f, -4.06f), Vector3.forward);

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log($"[HqAudit] {rows.Count} renderers audited → {ReportPath}\n" + sb.ToString());
    }

    static string Evaluate(string name, Bounds b)
    {
        string lower = name.ToLowerInvariant();
        foreach (var band in Bands)
        {
            if (!lower.Contains(band.match)) continue;
            float v = band.axis switch
            {
                "top" => b.max.y,
                "bottom" => b.min.y,
                "center" => b.center.y,
                _ => b.size.y,
            };
            if (v < band.range.x) return $"⚠ LOW {band.what}={v:0.00} (期望 {band.range.x:0.0}–{band.range.y:0.0})";
            if (v > band.range.y) return $"⚠ HIGH {band.what}={v:0.00} (期望 {band.range.x:0.0}–{band.range.y:0.0})";
            return $"✓ {band.what}={v:0.00}";
        }
        return "";
    }

    // Sweeps a vertical stack of probe boxes through the doorway and intersects them
    // with collider BOUNDS — PhysX queries are unreliable in edit mode (see
    // TowerWalkValidator), axis-aligned bounds are exact enough for the whitebox HQ.
    static void ProbeGap(StringBuilder sb, string label, Vector3 at, Vector3 across)
    {
        var colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(c => c.enabled && !c.isTrigger).ToArray();
        Vector3 half = new(
            across == Vector3.right ? 0.05f : 0.45f, 0.05f,
            across == Vector3.forward ? 0.05f : 0.45f);

        float clearHeight = 0f;
        for (float y = 0.15f; y <= 3.6f; y += 0.1f)
        {
            var probe = new Bounds(new Vector3(at.x, y, at.z), half * 2f);
            bool blocked = colliders.Any(c => c.bounds.Intersects(probe));
            if (!blocked) clearHeight = y;
            else if (clearHeight > 0.3f) break;
        }
        sb.AppendLine($"- {label} @ ({at.x:0.00},{at.z:0.00}): 净高 ≈ {clearHeight:0.0} m " +
            (clearHeight > 2.4f ? "⚠ 高于真实门洞 (2.0–2.4)" : "✓"));
    }

    static string FullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
