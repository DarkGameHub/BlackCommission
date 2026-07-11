using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime proportion correction for the current HQ scene. The player remains the locked 2 m
/// mission scale; the complete desk+CRT unit was authored only 1.05 m tall and therefore read as
/// miniature furniture. Raise that unit to a believable 1.35 m without rescaling doors or the player.
/// </summary>
public static class HqFurnitureProportionPass
{
    const string Marker = "BC_ComputerScale_1p35m";
    const string LayoutMarker = "BC_ShabbyLayout_WallAligned_v3";
    const float TargetComputerHeight = 1.35f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    static void Apply(Scene scene)
    {
        if (!scene.IsValid() || scene.name != "HQ") return;
        GameObject computer = GameObject.Find("Computer_CRT");
        if (computer == null) return;

        ApplyComputerScale(computer);
        ApplyShabbyLayout(computer);
    }

    static void ApplyComputerScale(GameObject computer)
    {
        if (computer.transform.Find(Marker) != null) return;

        Renderer[] renderers = computer.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float beforeHeight = bounds.size.y;
        if (beforeHeight >= TargetComputerHeight - 0.03f) return;

        float scale = TargetComputerHeight / Mathf.Max(0.01f, beforeHeight);
        computer.transform.localScale *= scale;
        var marker = new GameObject(Marker);
        marker.transform.SetParent(computer.transform, false);
        Debug.Log($"[HQ Proportion] Computer desk+CRT {beforeHeight:F2}m -> {TargetComputerHeight:F2}m " +
                  $"({scale:F2}x); player/doors unchanged.");
    }

    static void ApplyShabbyLayout(GameObject computer)
    {
        Transform shabbyRoot = FindAncestor(computer.transform, "HQ_ShabbyOffice");
        if (shabbyRoot == null || shabbyRoot.Find(LayoutMarker) != null) return;

        MoveLocal("FoldingTable", new Vector3(2.40f, 0f, 0.38f));
        MoveLocal("TableLamp", new Vector3(2.60f, 0.75f, 0.40f));
        MoveLocal("BestiaryNotebook", new Vector3(2.17f, 0.775f, 0.36f));
        MoveLocal("BoxStack_A", new Vector3(7.35f, 0f, 2.15f));
        GameObject secondStack = GameObject.Find("BoxStack_B");
        if (secondStack != null && FindAncestor(secondStack.transform, "HQ_ShabbyOffice") != null)
            secondStack.SetActive(false);

        var marker = new GameObject(LayoutMarker);
        marker.transform.SetParent(shabbyRoot, false);
        Debug.Log("[HQ Layout] Wall-aligned printer/lounge zones applied; centre aisle left clear.");
    }

    static void MoveLocal(string name, Vector3 localPosition)
    {
        GameObject go = GameObject.Find(name);
        if (go != null && FindAncestor(go.transform, "HQ_ShabbyOffice") != null)
            go.transform.localPosition = localPosition;
    }

    static Transform FindAncestor(Transform current, string name)
    {
        while (current != null)
        {
            if (current.name == name) return current;
            current = current.parent;
        }
        return null;
    }
}
