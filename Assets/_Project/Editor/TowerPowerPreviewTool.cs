using UnityEditor;
using UnityEngine;

/// <summary>
/// Whitebox-walkthrough helper: PowerGateBreaker is a NetworkBehaviour, so in a
/// non-networked preview (PreviewWalker / direct Play without host-start) the breaker
/// never runs and the F2 debt shutters stay down. This menu flips the power state by
/// hand so the PM can continue the walkthrough. The REAL gate must be tested in a
/// hosted session (HQ flow), where the breaker's 3 s hold drives the same objects.
/// </summary>
public static class TowerPowerPreviewTool
{
    [MenuItem("Tools/Black Commission/MVP/Tower/Preview - Toggle Power (no network)")]
    public static void TogglePower()
    {
        GameObject lights = GameObject.Find("PowerGate/F2_PowerLights");
        // GameObject.Find can't see inactive objects — walk from the root instead.
        var root = GameObject.Find("Tower_v8_Whitebox");
        if (root == null) { Debug.LogError("[PowerPreview] Tower_v8_Whitebox not found."); return; }
        Transform gate = root.transform.Find("PowerGate");
        if (gate == null) { Debug.LogError("[PowerPreview] PowerGate group not found — rebuild first."); return; }

        Transform lightsT = gate.Find("F2_PowerLights");
        bool powerOn = lightsT != null && !lightsT.gameObject.activeSelf; // toggling INTO this state
        if (lightsT != null) lightsT.gameObject.SetActive(powerOn);
        foreach (string nm in new[] { "PowerShutter_D30", "PowerShutter_D35" })
        {
            Transform s = gate.Find(nm);
            if (s != null) s.gameObject.SetActive(!powerOn);
        }
        Debug.Log($"[PowerPreview] Power {(powerOn ? "ON — shutters open, stair lights lit" : "OFF — shutters closed")} " +
                  "(preview only; not synced, not saved unless you save the scene).");
    }
}
