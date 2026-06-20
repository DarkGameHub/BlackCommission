using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BlackCommission.Level;
using BlackCommission.Level.Generation;
// UnityEngine also defines a GridLayout (2D tilemap); alias to the generation type to avoid CS0104.
using GridLayout = BlackCommission.Level.Generation.GridLayout;

/// <summary>
/// Editor preview for the ADR-0003 grid generator: generates a layout from a random seed and instantiates
/// the matching corridor/junction modules + anchor markers into a scene root, so the layout can be seen
/// and re-rolled in the editor before the runtime wiring (Phase 4). Each click = a fresh seed = a different
/// layout. Idempotent: clears the prior preview root first.
///
/// Menu: Tools > Black Commission > Map > Preview Grid Layout
/// </summary>
public static class GridLayoutPreview
{
    const string ModuleDir = "Assets/_Project/Art/Maps/Shared/Modules";
    const string RootName = "GRID_PREVIEW";

    [MenuItem("Tools/Black Commission/Map/Preview Grid Layout")]
    public static void Preview()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(RootName);

        // Constrained sample: 14×10 grid, three fixed hero anchors (Entry / Mid / Deep).
        var anchors = new List<GridMapGenerator.AnchorSpec>
        {
            new GridMapGenerator.AnchorSpec("ENTRY", new GridCoord(1, 1)),
            new GridMapGenerator.AnchorSpec("MID",   new GridCoord(7, 5)),
            new GridMapGenerator.AnchorSpec("DEEP",  new GridCoord(12, 8)),
        };

        int seed = (int)(System.DateTime.Now.Ticks & 0x7fffffff); // random per click → re-roll to see variation
        GridLayout layout = GridMapGenerator.Generate(14, 10, anchors, seed);

        int placed = GridLayoutInstantiator.Build(layout, root.transform,
            name => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModuleDir}/{name}.prefab"));

        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"[GridLayoutPreview] seed {seed}: placed {placed} module tiles + anchors under '{RootName}'. " +
                  "Click the menu again to re-roll a different layout.");
    }
}
