using UnityEngine;
using UnityEngine.SceneManagement;

public static class MvpSceneStyleDirector
{
    const string OfficeRootName = "MVP_RuntimeStyle_Office";
    const string SchoolRootName = "MVP_RuntimeStyle_School";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Apply(SceneManager.GetActiveScene());
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    static void Apply(Scene scene)
    {
        if (!scene.IsValid()) return;

        if (scene.name == "HQ")
            BuildOfficeStyle();
        else if (scene.name.Contains("School"))
            BuildSchoolStyle();
    }

    static void BuildOfficeStyle()
    {
        if (GameObject.Find(OfficeRootName) != null) return;

        var root = new GameObject(OfficeRootName);
        Material wallPaper = MakeMaterial(new Color(0.58f, 0.55f, 0.48f));
        Material warningRed = MakeMaterial(new Color(0.62f, 0.04f, 0.03f));
        Material terminalGreen = MakeMaterial(new Color(0.08f, 0.85f, 0.38f));
        Material darkMetal = MakeMaterial(new Color(0.05f, 0.055f, 0.055f));
        Material tiredFabric = MakeMaterial(new Color(0.18f, 0.19f, 0.16f));
        Material cardboard = MakeMaterial(new Color(0.47f, 0.34f, 0.18f));

        CreateBox("TakeoverWarningBoard", root.transform, new Vector3(-3.8f, 1.75f, 2.85f),
            new Vector3(1.7f, 0.85f, 0.04f), warningRed);
        CreateText("BUYOUT\nWARNING", root.transform, new Vector3(-3.8f, 1.76f, 2.81f),
            Quaternion.Euler(0f, 180f, 0f), 0.16f, Color.white);
        CreateBox("DebtNoticeStack", root.transform, new Vector3(-2.45f, 1.28f, 2.86f),
            new Vector3(0.9f, 0.54f, 0.035f), wallPaper);
        CreateText("OVERDUE\nINVOICES", root.transform, new Vector3(-2.45f, 1.3f, 2.82f),
            Quaternion.Euler(0f, 180f, 0f), 0.11f, Color.black);

        CreateBox("SecondHandSofa", root.transform, new Vector3(2.8f, 0.45f, 2.6f),
            new Vector3(2.1f, 0.35f, 0.75f), tiredFabric);
        CreateBox("SofaBack", root.transform, new Vector3(2.8f, 0.82f, 2.88f),
            new Vector3(2.1f, 0.72f, 0.18f), tiredFabric);
        CreateBox("EquipmentShelf", root.transform, new Vector3(3.8f, 1f, -0.9f),
            new Vector3(1.4f, 1.8f, 0.22f), darkMetal);
        CreateBox("ShelfMedkitIcon", root.transform, new Vector3(3.35f, 1.55f, -1.08f),
            new Vector3(0.28f, 0.18f, 0.16f), wallPaper);
        CreateBox("ShelfSprayIcon", root.transform, new Vector3(3.75f, 1.52f, -1.08f),
            new Vector3(0.13f, 0.3f, 0.13f), terminalGreen);
        CreateBox("ShelfDecoyIcon", root.transform, new Vector3(4.12f, 1.5f, -1.08f),
            new Vector3(0.2f, 0.2f, 0.2f), cardboard);
        CreateBox("TerminalGlowStrip", root.transform, new Vector3(-1.2f, 1.68f, 2.18f),
            new Vector3(1.5f, 0.06f, 0.06f), terminalGreen);

        var lightGo = new GameObject("OfficeTerminalSpill");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.position = new Vector3(-1.2f, 1.35f, 2f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.2f, 1f, 0.55f);
        light.intensity = 1.1f;
        light.range = 4f;
    }

    static void BuildSchoolStyle()
    {
        if (GameObject.Find(SchoolRootName) != null) return;

        var root = new GameObject(SchoolRootName);
        Material coldPaint = MakeMaterial(new Color(0.22f, 0.3f, 0.31f));
        Material warningRed = MakeMaterial(new Color(0.64f, 0.035f, 0.025f));
        Material paper = MakeMaterial(new Color(0.82f, 0.8f, 0.68f));
        Material lightColor = MakeMaterial(new Color(0.56f, 0.92f, 0.86f));
        Material dark = MakeMaterial(new Color(0.045f, 0.05f, 0.055f));

        for (int i = 0; i < 5; i++)
        {
            CreateBox($"ColdLocker_{i + 1}", root.transform, new Vector3(-10.8f, 0.95f, -5.4f + i * 1.3f),
                new Vector3(0.28f, 1.55f, 0.9f), coldPaint);
            CreateBox($"LockerDebtSticker_{i + 1}", root.transform, new Vector3(-10.95f, 1.22f, -5.4f + i * 1.3f),
                new Vector3(0.03f, 0.28f, 0.38f), warningRed);
        }

        CreateBox("HomeworkDebtBanner", root.transform, new Vector3(0f, 2.6f, 5.72f),
            new Vector3(4.8f, 0.55f, 0.035f), warningRed);
        CreateText("HOMEWORK DEBT OFFICE", root.transform, new Vector3(0f, 2.62f, 5.66f),
            Quaternion.Euler(0f, 180f, 0f), 0.16f, Color.white);

        CreateBox("FlickerLight_A", root.transform, new Vector3(-4f, 3.05f, -1f),
            new Vector3(2.6f, 0.06f, 0.1f), lightColor);
        CreateBox("FlickerLight_B", root.transform, new Vector3(4f, 3.05f, -1f),
            new Vector3(2.6f, 0.06f, 0.1f), lightColor);
        CreateBox("PrincipalShadowDoor", root.transform, new Vector3(9.9f, 1.1f, 3.4f),
            new Vector3(0.16f, 2.2f, 1.25f), dark);

        for (int i = 0; i < 8; i++)
        {
            CreateBox($"OverduePaper_{i + 1}", root.transform,
                new Vector3(-3.4f + i * 0.95f, 0.025f, 3.2f + (i % 2) * 0.85f),
                new Vector3(0.34f, 0.02f, 0.24f), paper);
        }

        var lightGo = new GameObject("SchoolColdDebtLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.position = new Vector3(0f, 2.8f, 1f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.52f, 0.95f, 0.9f);
        light.intensity = 0.9f;
        light.range = 7f;
    }

    static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        if (go.TryGetComponent<Collider>(out var collider))
            Object.Destroy(collider);
        if (go.TryGetComponent<Renderer>(out var renderer))
            renderer.sharedMaterial = material;
        return go;
    }

    static GameObject CreateText(string text, Transform parent, Vector3 position, Quaternion rotation, float characterSize, Color color)
    {
        var go = new GameObject($"Text_{text.Replace('\n', '_')}");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = characterSize;
        mesh.fontSize = 48;
        mesh.color = color;
        return go;
    }

    static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Standard");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        return material;
    }
}
