using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Drops a host-testable scavenging rig into the ACTIVE scene so the P1b loop can be played
/// without waiting on the full modular-mission scene: a <see cref="LootSpawner"/> (spawns loot
/// at the scene's LootAnchors on host start) + a <see cref="ScavengeCargoZone"/> van bay (the
/// weight-gated deposit volume from quick-spec §3). Use it on Module_Showcase (which carries
/// LootAnchors) to test spawn → carry → deposit → weight-limit end to end.
///
/// Menu: Tools > Black Commission > MVP > Scavenge > Build Cargo Zone Test Rig
/// Idempotent: re-running reuses/repositions the existing rig. The rig auto-places next to the
/// scene's LootAnchors when present. This marks the scene dirty but does NOT save — review the
/// placement, then save yourself. Host the scene (QuickNetworkUI) to exercise it.
/// </summary>
public static class ScavengeTestbedBuilder
{
    const string RigName  = "Scavenge_TestRig";
    const string ZoneName = "VanCargoZone";
    const string SpawnName = "LootSpawner";

    [MenuItem("Tools/Black Commission/MVP/Scavenge/Build Cargo Zone Test Rig")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[ScavengeTestbedBuilder] No valid active scene open.");
            return;
        }

        GameObject rig = GameObject.Find(RigName) ?? new GameObject(RigName);

        Vector3 anchorCentroid = FindLootAnchorCentroid(out int anchorCount);
        // Park the van a few metres clear of the loot cluster so items must be carried to it.
        Vector3 vanPos = anchorCentroid + new Vector3(0f, 0f, -4f);

        GameObject zone = EnsureChild(rig, ZoneName);
        zone.transform.position = vanPos;
        ConfigureCargoZone(zone);

        GameObject spawner = EnsureChild(rig, SpawnName);
        spawner.transform.position = anchorCentroid;
        ConfigureLootSpawner(spawner);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = zone;

        Debug.Log($"[ScavengeTestbedBuilder] Rig ready in '{scene.name}' " +
                  $"({anchorCount} LootAnchor(s) found). Cargo bay at {vanPos}. " +
                  "Move it where you like, SAVE the scene, then host (QuickNetworkUI): " +
                  "loot spawns at anchors → carry an item into the bay → it loads if the weight fits.");
    }

    static void ConfigureCargoZone(GameObject zone)
    {
        var box = zone.GetComponent<BoxCollider>() ?? zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 1f, 0f);
        box.size = new Vector3(3f, 2f, 3f); // open cargo bed volume

        if (zone.GetComponent<NetworkObject>() == null) zone.AddComponent<NetworkObject>();
        if (zone.GetComponent<ScavengeCargoZone>() == null) zone.AddComponent<ScavengeCargoZone>();
        // ScavengeCargoZone.zone auto-resolves from this BoxCollider in Awake; capacity reads
        // ScavengingConfig (default 12). Nothing else to wire.

        EnsureBayMarker(zone, box.center, box.size);
    }

    // A translucent teal box so the bay is visible in-editor and in play (no collider, no network).
    static void EnsureBayMarker(GameObject zone, Vector3 center, Vector3 size)
    {
        Transform existing = zone.transform.Find("BayMarker");
        GameObject marker = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "BayMarker";
        marker.transform.SetParent(zone.transform, false);
        marker.transform.localPosition = center;
        marker.transform.localScale = size;

        var col = marker.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        // Translucent civic teal (matches V8_Civic_TealPaint); alpha so loot inside is visible.
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);     // URP transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);          // alpha blend
        mat.renderQueue = 3000;
        var teal = new Color(0.247f, 0.373f, 0.361f, 0.18f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", teal);
        mat.color = teal;
        marker.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void ConfigureLootSpawner(GameObject spawner)
    {
        if (spawner.GetComponent<NetworkObject>() == null) spawner.AddComponent<NetworkObject>();
        if (spawner.GetComponent<LootSpawner>() == null) spawner.AddComponent<LootSpawner>();
    }

    static GameObject EnsureChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    // Centre of the scene's LootAnchors (so the rig lands near the loot); origin if none exist.
    static Vector3 FindLootAnchorCentroid(out int count)
    {
        var anchors = Object.FindObjectsByType<BlackCommission.Level.LootAnchor>(FindObjectsSortMode.None);
        count = anchors.Length;
        if (count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var a in anchors) sum += a.transform.position;
        return sum / count;
    }
}
