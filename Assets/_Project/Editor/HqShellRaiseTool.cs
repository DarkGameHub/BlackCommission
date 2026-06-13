using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Raises the HQ shell (walls + ceiling + wall colliders) from 3.38 m to the tower's
/// 4.2 m clear height WITHOUT touching furniture or props (PM 2026-06-12: proportions
/// feel right in the tower map, still cramped in the HQ — the difference is headroom,
/// the furniture heights are already real-world correct).
///
/// Scales each wall's Y about the floor and lifts the ceiling; extends the two door
/// headers up so the openings keep their 2.2 m / 2.72 m clearances. Idempotent.
/// Does NOT save — walk it, then Ctrl+S to keep or Ctrl+Z to revert.
/// </summary>
public static class HqShellRaiseTool
{
    const string ScenePath = "Assets/_Project/Scenes/HQ.unity";
    const float TargetHeight = 4.2f;

    static readonly string[] WallNames =
    {
        "ShellNorthWallOffice", "ShellNorthWallGarage",
        "ShellSouthWallOfficeLeft", "ShellSouthWallOfficeRight",
        "ShellSouthWallGarageLeft", "ShellSouthWallGarageRight",
        "ShellEastWall", "ShellWestWall", "ShellDividerWall", "ShellDividerStub",
        // Matching invisible wall colliders (separate objects, same heights).
        "HQOfficeNorthWallCollider", "HQGarageNorthWallCollider",
        "HQOfficeSouthWallLeftCollider", "HQOfficeSouthWallRightCollider",
        "HQGarageSouthWallLeftCollider", "HQGarageSouthWallRightCollider",
        "HQGarageOuterEastWallCollider", "HQOfficeWestWallCollider",
        "HQOfficeGarageDividerWallCollider", "HQOfficeGarageDividerStubCollider",
    };

    [MenuItem("Tools/Black Commission/MVP/HQ/Raise Shell To 4.2m (tower parity)")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "HQ")
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var probe = GameObject.Find("ShellEastWall");
        if (probe == null) { Debug.LogWarning("[HqRaise] ShellEastWall not found"); return; }
        var probeRend = probe.GetComponent<Renderer>();
        float current = probeRend != null ? probeRend.bounds.size.y : 3.38f;
        if (current > TargetHeight - 0.1f)
        {
            Debug.Log($"[HqRaise] shell already {current:0.00} m — nothing to do");
            return;
        }

        float k = TargetHeight / current;
        int moved = 0;
        foreach (string name in WallNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            Undo.RecordObject(go.transform, "Raise HQ Shell");
            var t = go.transform;
            t.position = new Vector3(t.position.x, t.position.y * k, t.position.z);
            t.localScale = new Vector3(t.localScale.x, t.localScale.y * k, t.localScale.z);
            moved++;
        }

        var ceiling = GameObject.Find("ShellCeiling");
        if (ceiling != null)
        {
            Undo.RecordObject(ceiling.transform, "Raise HQ Shell");
            var t = ceiling.transform;
            t.position = new Vector3(t.position.x, t.position.y * k, t.position.z);
            moved++;
        }

        // Door headers keep their bottom edge (door clearance) and stretch up to
        // meet the raised wall top.
        moved += StretchHeaderTo("ShellGarageDoorHeader", TargetHeight) ? 1 : 0;
        moved += StretchHeaderTo("ShellDividerDoorHeader", TargetHeight) ? 1 : 0;

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[HqRaise] {moved} objects raised ×{k:0.000} → clear height {TargetHeight:0.0} m. " +
                  "Walk it; Ctrl+S to keep, Ctrl+Z to revert.");
    }

    static bool StretchHeaderTo(string name, float topY)
    {
        var go = GameObject.Find(name);
        if (go == null) return false;
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return false;

        Bounds b = rend.bounds;
        float bottom = b.min.y;
        if (topY - bottom < 0.05f || b.size.y < 0.01f) return false;

        Undo.RecordObject(go.transform, "Raise HQ Shell");
        float factor = (topY - bottom) / b.size.y;
        var t = go.transform;
        t.localScale = new Vector3(t.localScale.x, t.localScale.y * factor, t.localScale.z);
        t.position = new Vector3(t.position.x,
            t.position.y + ((bottom + topY) * 0.5f - b.center.y), t.position.z);
        return true;
    }
}
