using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the proportion corrections established by HqProportionAuditTool
/// (production/qa/hq-proportion-audit.md, 2026-06-12):
///
///   1. Van is 1.97 m tall — exactly player height — reads toy-like. Scale the van
///      group ×1.2 about its ground centre (→ ~2.36 m, real light-van band).
///   2. Two desk lamps float at 2.2–2.7 m (their desk was removed). Settle them
///      down onto whatever surface is actually beneath them.
///   3. The fluorescent tube by the computer hangs at 1.91 m (head height). Raise
///      it to 2.75 m, proper industrial hang under the 3.36 m ceiling.
///   4. The office↔garage doorway is full-wall height (3.4 m clear). Add a header
///      beam so the opening reads as a 2.2 m door.
///   5. The computer interactable (MVP_OfficeComputer) floats 10 cm above the
///      physical CRT model. Re-seat it on the model's actual screen.
///
/// Idempotent: each fix checks current state and skips if already applied.
/// Does NOT save — walk it in Play mode, then Ctrl+S to keep or Ctrl+Z to revert.
/// </summary>
public static class HqProportionFixTool
{
    const string ScenePath = "Assets/_Project/Scenes/HQ.unity";

    const float VanTargetHeight = 2.36f;
    const float DoorHeaderClearance = 2.2f;
    const float FluorescentBottom = 2.75f;

    [MenuItem("Tools/Black Commission/MVP/HQ/Fix Proportions (audit 2026-06-12)")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "HQ")
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int applied = 0;
        applied += FixVan() ? 1 : 0;
        applied += SettleLamp("ShellLampDesk_A") ? 1 : 0;
        applied += SettleLamp("ShellLampDesk_B") ? 1 : 0;
        applied += RaiseFluorescent() ? 1 : 0;
        applied += AddDoorHeader() ? 1 : 0;
        applied += ReseatComputerInteractable() ? 1 : 0;

        if (applied > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[HqFix] {applied} proportion fixes applied. Walk it in Play mode; " +
                  "Ctrl+S to keep, Ctrl+Z to revert. Re-run the audit to verify numbers.");
    }

    // ── 1. Van ×1.2 about its ground centre ─────────────────────────────

    static bool FixVan()
    {
        var model = GameObject.Find("AS_OfficeVan_Model");
        if (model == null) { Debug.LogWarning("[HqFix] van model not found"); return false; }
        var rend = model.GetComponent<Renderer>();
        if (rend == null) return false;
        if (rend.bounds.size.y > 2.2f) return false; // already fixed

        float k = VanTargetHeight / rend.bounds.size.y;
        Vector3 center = rend.bounds.center;
        center.y = 0f; // scale about the ground so the wheels stay planted

        string[] vanGroup =
        {
            "ShellVan_Generated",
            "BlenderHQ_ASV4VanBodyCollider", "BlenderHQ_ASV4VanFrontCollider",
            "BlenderHQ_ASV4VanRearCollider", "BlenderHQ_ASV4DepartureTrigger",
        };
        foreach (string name in vanGroup)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            Undo.RecordObject(go.transform, "HQ Fix Van");
            go.transform.position = center + (go.transform.position - center) * k;
            go.transform.localScale *= k;
        }
        Debug.Log($"[HqFix] van scaled ×{k:0.00} → height {VanTargetHeight:0.00} m");
        return true;
    }

    // ── 2. Floating desk lamps → settle onto the surface beneath ────────

    static bool SettleLamp(string parentName)
    {
        var lamp = GameObject.Find(parentName);
        if (lamp == null) { Debug.LogWarning($"[HqFix] {parentName} not found"); return false; }
        var rend = lamp.GetComponentInChildren<Renderer>();
        if (rend == null) return false;

        Bounds lb = rend.bounds;
        if (lb.min.y < 1.6f) return false; // already settled

        // Highest renderer top below the lamp whose footprint contains the lamp centre.
        float surface = 0f;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == rend || r.transform.IsChildOf(lamp.transform)) continue;
            Bounds b = r.bounds;
            if (b.max.y > lb.min.y - 0.02f) continue;             // not below the lamp
            if (b.size.y < 0.02f && b.max.y < 0.1f) { surface = Mathf.Max(surface, b.max.y); continue; } // floor
            if (lb.center.x < b.min.x || lb.center.x > b.max.x) continue;
            if (lb.center.z < b.min.z || lb.center.z > b.max.z) continue;
            surface = Mathf.Max(surface, b.max.y);
        }

        Undo.RecordObject(lamp.transform, "HQ Fix Lamp");
        lamp.transform.position += Vector3.up * (surface + 0.005f - lb.min.y);
        Debug.Log($"[HqFix] {parentName} settled: bottom {lb.min.y:0.00} → {surface + 0.005f:0.00} m");
        return true;
    }

    // ── 3. Head-height fluorescent tube → industrial hang ───────────────

    static bool RaiseFluorescent()
    {
        var lamp = GameObject.Find("ShellLampFluorescent_Computer");
        if (lamp == null) { Debug.LogWarning("[HqFix] fluorescent (computer) not found"); return false; }
        var rend = lamp.GetComponentInChildren<Renderer>();
        if (rend == null) return false;
        if (rend.bounds.min.y > 2.4f) return false; // already raised

        Undo.RecordObject(lamp.transform, "HQ Fix Fluorescent");
        lamp.transform.position += Vector3.up * (FluorescentBottom - rend.bounds.min.y);
        // Keep any Light child pointing the same way; range is unaffected.
        Debug.Log($"[HqFix] fluorescent raised to bottom {FluorescentBottom:0.00} m");
        return true;
    }

    // ── 4. Office↔garage doorway header (3.4 m clear → 2.2 m door) ──────

    static bool AddDoorHeader()
    {
        if (GameObject.Find("ShellDividerDoorHeader") != null) return false; // already added

        var wall = GameObject.Find("ShellDividerWall");
        var stub = GameObject.Find("ShellDividerStub");
        if (wall == null || stub == null)
        { Debug.LogWarning("[HqFix] divider wall/stub not found"); return false; }

        Bounds wb = wall.GetComponent<Renderer>().bounds;
        Bounds sb = stub.GetComponent<Renderer>().bounds;

        // The doorway is the z-gap between the stub (south) and the wall (north).
        float zLo = sb.max.z, zHi = wb.min.z;
        if (zHi - zLo < 0.4f || zHi - zLo > 2.5f)
        { Debug.LogWarning($"[HqFix] unexpected doorway gap {zHi - zLo:0.00} m — skipped"); return false; }

        float top = wb.max.y;
        var header = GameObject.CreatePrimitive(PrimitiveType.Cube);
        header.name = "ShellDividerDoorHeader";
        header.transform.SetParent(wall.transform.parent, true);
        header.transform.position = new Vector3(
            wb.center.x, (DoorHeaderClearance + top) * 0.5f, (zLo + zHi) * 0.5f);
        header.transform.localScale = Vector3.one; // set world size via lossy-corrected scale
        Vector3 parentLossy = header.transform.lossyScale;
        header.transform.localScale = new Vector3(
            wb.size.x / parentLossy.x,
            (top - DoorHeaderClearance) / parentLossy.y,
            (zHi - zLo + 0.06f) / parentLossy.z);

        var wallRend = wall.GetComponent<Renderer>();
        header.GetComponent<Renderer>().sharedMaterial = wallRend.sharedMaterial;
        Undo.RegisterCreatedObjectUndo(header, "HQ Fix Door Header");
        Debug.Log($"[HqFix] door header added: clearance {DoorHeaderClearance:0.0} m, gap z {zLo:0.00}..{zHi:0.00}");
        return true;
    }

    // ── 5. Computer interactable re-seated on the actual CRT ────────────

    static bool ReseatComputerInteractable()
    {
        var interactable = GameObject.Find("MVP_OfficeComputer");
        var model = GameObject.Find("AS_OfficeComputer_Model");
        if (interactable == null || model == null)
        { Debug.LogWarning("[HqFix] computer interactable/model not found"); return false; }

        Bounds mb = model.GetComponent<Renderer>().bounds;
        // CRT sits on the desk part of the combo model: screen centre ≈ 80 % up,
        // front face = the -Z side (it faces the floor guide).
        Vector3 target = new(mb.center.x, mb.min.y + mb.size.y * 0.80f, mb.min.z + 0.02f);
        if ((interactable.transform.position - target).magnitude < 0.05f) return false; // already seated

        Undo.RecordObject(interactable.transform, "HQ Fix Computer Interactable");
        interactable.transform.position = target;
        Debug.Log($"[HqFix] MVP_OfficeComputer re-seated at {target} (was floating above the CRT)");
        return true;
    }
}
