using UnityEngine;
using UnityEngine.SceneManagement;

public static class MvpSceneStyleDirector
{
    const string OfficeRootName = "MVP_RuntimeStyle_Office";
    const string SchoolRootName = "MVP_RuntimeStyle_School";

    enum OfficePattern
    {
        Grime,
        Tile,
        Notice,
        Warning,
        Scanline,
        Scratched,
        Fabric,
        Cardboard,
        Wood,
        Blinds
    }

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
        RemoveGeneratedWorldText();
        if (GameObject.Find(OfficeRootName) != null) return;

        var root = new GameObject(OfficeRootName);
        ApplyOfficeAtmosphere();
        Material stainedWall = MakeOfficeMaterial("Office_StainedWall", new Color(0.42f, 0.49f, 0.39f), new Color(0.2f, 0.24f, 0.18f), OfficePattern.Grime);
        Material dirtyFloor = MakeOfficeMaterial("Office_WornFloor", new Color(0.16f, 0.18f, 0.15f), new Color(0.36f, 0.31f, 0.22f), OfficePattern.Tile);
        Material paper = MakeOfficeMaterial("Office_DirtyPaper", new Color(0.78f, 0.75f, 0.58f), new Color(0.55f, 0.08f, 0.06f), OfficePattern.Notice);
        Material warningRed = MakeOfficeMaterial("Office_DebtRed", new Color(0.58f, 0.035f, 0.025f), new Color(0.95f, 0.78f, 0.62f), OfficePattern.Warning);
        Material terminalGreen = MakeOfficeMaterial("Office_TerminalGreen", new Color(0.04f, 0.62f, 0.25f), new Color(0.36f, 1f, 0.58f), OfficePattern.Scanline);
        Material darkMetal = MakeOfficeMaterial("Office_DeadMetal", new Color(0.045f, 0.05f, 0.05f), new Color(0.18f, 0.2f, 0.18f), OfficePattern.Scratched);
        Material tiredFabric = MakeOfficeMaterial("Office_TiredFabric", new Color(0.16f, 0.17f, 0.13f), new Color(0.08f, 0.09f, 0.07f), OfficePattern.Fabric);
        Material cardboard = MakeOfficeMaterial("Office_CheapCardboard", new Color(0.43f, 0.29f, 0.14f), new Color(0.72f, 0.5f, 0.24f), OfficePattern.Cardboard);
        Material wood = MakeOfficeMaterial("Office_SecondHandWood", new Color(0.28f, 0.18f, 0.09f), new Color(0.52f, 0.33f, 0.16f), OfficePattern.Wood);
        Material cable = MakeMaterial(new Color(0.015f, 0.018f, 0.018f));
        Material black = MakeMaterial(new Color(0.015f, 0.017f, 0.016f));
        Material grime = MakeMaterial(new Color(0.08f, 0.11f, 0.08f));
        Material oldTape = MakeMaterial(new Color(0.72f, 0.61f, 0.36f));
        Material oldPlaster = MakeOfficeMaterial("Office_PeeledPlaster", new Color(0.68f, 0.62f, 0.48f), new Color(0.23f, 0.2f, 0.14f), OfficePattern.Grime);
        Material columnRed = MakeOfficeMaterial("Office_DebtColumnPaint", new Color(0.32f, 0.08f, 0.065f), new Color(0.08f, 0.035f, 0.03f), OfficePattern.Scratched);
        Material tileLight = MakeMaterial(new Color(0.48f, 0.46f, 0.36f));
        Material tileDark = MakeMaterial(new Color(0.08f, 0.09f, 0.085f));
        Material carpet = MakeOfficeMaterial("Office_ThreadbareCarpet", new Color(0.12f, 0.21f, 0.16f), new Color(0.04f, 0.08f, 0.06f), OfficePattern.Fabric);
        Material garageConcrete = MakeOfficeMaterial("Office_GarageConcrete", new Color(0.19f, 0.21f, 0.19f), new Color(0.045f, 0.055f, 0.05f), OfficePattern.Grime);
        Material bayDoor = MakeOfficeMaterial("Office_DentedBayDoor", new Color(0.13f, 0.16f, 0.145f), new Color(0.035f, 0.04f, 0.035f), OfficePattern.Scratched);
        Material hazardStripe = MakeOfficeMaterial("Office_HazardStripe", new Color(0.78f, 0.58f, 0.12f), new Color(0.025f, 0.026f, 0.022f), OfficePattern.Warning);
        Material vanBody = MakeOfficeMaterial("Office_CompanyVanBody", new Color(0.08f, 0.115f, 0.095f), new Color(0.27f, 0.42f, 0.28f), OfficePattern.Scratched);
        Material vanGlass = MakeOfficeMaterial("Office_SmokedGlass", new Color(0.015f, 0.03f, 0.033f), new Color(0.07f, 0.18f, 0.15f), OfficePattern.Scanline);
        Material rubber = MakeMaterial(new Color(0.008f, 0.009f, 0.008f));
        Material headlight = MakeEmissiveMaterial(new Color(0.8f, 0.72f, 0.38f), new Color(1f, 0.86f, 0.38f), 1.2f);

        CreateBox("OfficeFloorSkin", root.transform, new Vector3(0f, 0.012f, 0f),
            new Vector3(9.6f, 0.025f, 7.6f), dirtyFloor);
        CreateDispatchStationBase(root.transform, garageConcrete, darkMetal, hazardStripe, terminalGreen);
        CreateBox("DispatchOfficeRubberMat", root.transform, new Vector3(-1.2f, 0.115f, 1.22f),
            new Vector3(3.2f, 0.025f, 1.75f), carpet);
        CreateBox("BackWallStainPanel", root.transform, new Vector3(0f, 1.55f, 2.965f),
            new Vector3(9.4f, 2.9f, 0.035f), stainedWall);
        CreateBox("LeftWallStainPanel", root.transform, new Vector3(-4.965f, 1.55f, 0f),
            new Vector3(0.035f, 2.9f, 7.1f), stainedWall);
        CreateBox("RightWallStainPanel", root.transform, new Vector3(4.965f, 1.55f, 0f),
            new Vector3(0.035f, 2.9f, 7.1f), stainedWall);
        CreateBox("SouthWallStainPanel", root.transform, new Vector3(0f, 1.55f, -3.765f),
            new Vector3(9.4f, 2.9f, 0.035f), stainedWall);
        CreateBox("BackWallBottomMold", root.transform, new Vector3(0f, 0.33f, 2.925f),
            new Vector3(9.35f, 0.12f, 0.05f), darkMetal);
        CreateBox("RightWallBottomMold", root.transform, new Vector3(4.925f, 0.33f, 0f),
            new Vector3(0.05f, 0.12f, 7.0f), darkMetal);
        CreateBox("LeftWallBottomMold", root.transform, new Vector3(-4.925f, 0.33f, 0f),
            new Vector3(0.05f, 0.12f, 7.0f), darkMetal);
        CreateDispatchStationShell(root.transform, columnRed, darkMetal, oldPlaster, bayDoor, terminalGreen, warningRed, paper, vanGlass);
        CreateBox("CheapCeilingTiles", root.transform, new Vector3(0f, 2.96f, 0f),
            new Vector3(9.4f, 0.035f, 7.2f), MakeOfficeMaterial("Office_CeilingTiles", new Color(0.38f, 0.4f, 0.35f), new Color(0.16f, 0.17f, 0.15f), OfficePattern.Tile));
        CreateBox("FloorTapeOutline_Upgrade", root.transform, new Vector3(1.6f, 0.04f, -2.0f),
            new Vector3(1.5f, 0.025f, 0.08f), warningRed);
        CreateBox("FloorTapeOutline_Upgrade_B", root.transform, new Vector3(1.6f, 0.04f, -1.2f),
            new Vector3(1.5f, 0.025f, 0.08f), warningRed);
        CreateOfficeWallStains(root.transform, grime, warningRed, paper, oldTape);

        CreateBox("TakeoverWarningBoard", root.transform, new Vector3(-3.8f, 1.75f, 2.85f),
            new Vector3(1.7f, 0.85f, 0.04f), warningRed);
        CreateBox("TakeoverBoardBlackHeader", root.transform, new Vector3(-3.8f, 2.09f, 2.805f),
            new Vector3(1.48f, 0.12f, 0.025f), black);
        CreateBox("TakeoverBoardTallyA", root.transform, new Vector3(-4.23f, 1.75f, 2.8f),
            new Vector3(0.045f, 0.55f, 0.025f), oldTape);
        CreateBox("TakeoverBoardTallyB", root.transform, new Vector3(-4.08f, 1.75f, 2.8f),
            new Vector3(0.045f, 0.55f, 0.025f), oldTape);
        CreateBox("TakeoverBoardTallyC", root.transform, new Vector3(-3.93f, 1.75f, 2.8f),
            new Vector3(0.045f, 0.55f, 0.025f), oldTape);
        CreateBox("DebtNoticeStack", root.transform, new Vector3(-2.45f, 1.28f, 2.86f),
            new Vector3(0.9f, 0.54f, 0.035f), paper);
        for (int i = 0; i < 9; i++)
        {
            float x = -4.25f + (i % 3) * 0.42f;
            float y = 1.18f + (i / 3) * 0.26f;
            CreateBox($"OverdueNotice_{i + 1}", root.transform, new Vector3(x, y, 2.81f),
                new Vector3(0.3f, 0.2f, 0.025f), i % 4 == 0 ? warningRed : paper);
        }

        CreateBox("ComputerDeskTop", root.transform, new Vector3(-1.2f, 0.75f, 2.08f),
            new Vector3(2.35f, 0.12f, 0.92f), wood);
        CreateBox("ComputerDeskLeftLeg", root.transform, new Vector3(-2.18f, 0.38f, 2.08f),
            new Vector3(0.14f, 0.72f, 0.72f), darkMetal);
        CreateBox("ComputerDeskRightLeg", root.transform, new Vector3(-0.22f, 0.38f, 2.08f),
            new Vector3(0.14f, 0.72f, 0.72f), darkMetal);
        CreateBox("KeyboardSlab", root.transform, new Vector3(-1.2f, 0.86f, 1.58f),
            new Vector3(0.95f, 0.05f, 0.26f), darkMetal);
        CreateBox("CheapMousePad", root.transform, new Vector3(-0.38f, 0.865f, 1.58f),
            new Vector3(0.35f, 0.035f, 0.28f), tiredFabric);
        CreateBox("CoffeeStainRing", root.transform, new Vector3(-1.9f, 0.875f, 1.67f),
            new Vector3(0.26f, 0.018f, 0.26f), MakeMaterial(new Color(0.13f, 0.07f, 0.025f)));
        CreateBox("UnpaidStampBlock", root.transform, new Vector3(-1.75f, 0.9f, 2.25f),
            new Vector3(0.36f, 0.08f, 0.22f), warningRed);
        CreateBox("DeskPaperPile", root.transform, new Vector3(-1.08f, 0.91f, 2.28f),
            new Vector3(0.52f, 0.035f, 0.34f), paper);
        CreateBox("SecondHandSofa", root.transform, new Vector3(2.8f, 0.45f, 2.6f),
            new Vector3(2.1f, 0.35f, 0.75f), tiredFabric);
        CreateBox("SofaBack", root.transform, new Vector3(2.8f, 0.82f, 2.88f),
            new Vector3(2.1f, 0.72f, 0.18f), tiredFabric);
        CreateBox("MissingSofaCushionHole", root.transform, new Vector3(3.42f, 0.66f, 2.22f),
            new Vector3(0.55f, 0.05f, 0.48f), darkMetal);
        CreateBox("DentedFilingCabinet", root.transform, new Vector3(-4.28f, 0.86f, -0.8f),
            new Vector3(0.78f, 1.55f, 0.58f), darkMetal);
        for (int i = 0; i < 3; i++)
            CreateBox($"FilingCabinetHandle_{i}", root.transform, new Vector3(-4.69f, 1.25f - i * 0.38f, -0.8f),
                new Vector3(0.035f, 0.045f, 0.35f), paper);
        CreateBox("EquipmentShelf", root.transform, new Vector3(3.8f, 1f, -0.9f),
            new Vector3(1.4f, 1.8f, 0.22f), darkMetal);
        CreateBox("ShelfMedkitIcon", root.transform, new Vector3(3.35f, 1.55f, -1.08f),
            new Vector3(0.28f, 0.18f, 0.16f), paper);
        CreateBox("ShelfSprayIcon", root.transform, new Vector3(3.75f, 1.52f, -1.08f),
            new Vector3(0.13f, 0.3f, 0.13f), terminalGreen);
        CreateBox("ShelfDecoyIcon", root.transform, new Vector3(4.12f, 1.5f, -1.08f),
            new Vector3(0.2f, 0.2f, 0.2f), cardboard);
        CreateBox("TerminalGlowStrip", root.transform, new Vector3(-1.2f, 1.68f, 2.18f),
            new Vector3(1.5f, 0.06f, 0.06f), terminalGreen);
        for (int i = 0; i < 5; i++)
        {
            CreateBox($"ArchiveBox_{i + 1}", root.transform, new Vector3(3.3f + (i % 2) * 0.58f, 0.28f + (i / 2) * 0.38f, -2.65f),
                new Vector3(0.52f, 0.34f, 0.48f), cardboard);
            CreateBox($"ArchiveBoxLabel_{i + 1}", root.transform, new Vector3(3.3f + (i % 2) * 0.58f, 0.3f + (i / 2) * 0.38f, -2.9f),
                new Vector3(0.32f, 0.11f, 0.025f), paper);
        }
        for (int i = 0; i < 4; i++)
            CreateBox($"CableRun_{i + 1}", root.transform, new Vector3(-1.42f + i * 0.2f, 0.05f, 1.75f - i * 0.16f),
                new Vector3(0.42f, 0.035f, 0.035f), cable);
        CreateBox("BrokenFluorescentLight", root.transform, new Vector3(0.25f, 2.87f, 0.05f),
            new Vector3(2.1f, 0.055f, 0.12f), MakeMaterial(new Color(0.75f, 0.82f, 0.68f)));
        CreateBox("GreenWindowBlind", root.transform, new Vector3(4.75f, 1.85f, 1.4f),
            new Vector3(0.045f, 1.15f, 1.85f), MakeOfficeMaterial("Office_DirtyBlinds", new Color(0.22f, 0.33f, 0.26f), new Color(0.08f, 0.12f, 0.1f), OfficePattern.Blinds));
        CreateBox("ExitDoorScuffPlate", root.transform, new Vector3(0.02f, 0.7f, -3.72f),
            new Vector3(1.15f, 0.32f, 0.035f), darkMetal);
        CreateOfficeGarageAndVan(root.transform, garageConcrete, bayDoor, hazardStripe, vanBody, vanGlass, rubber, darkMetal, terminalGreen, warningRed, paper, headlight);
        CreateDispatchOfficeBooth(root.transform, darkMetal, terminalGreen, warningRed, paper, vanGlass, cardboard, wood);
        CreateBox("CrookedCompanyMark_A", root.transform, new Vector3(2.95f, 2.08f, 2.81f),
            new Vector3(0.72f, 0.11f, 0.025f), terminalGreen);
        CreateBox("CrookedCompanyMark_B", root.transform, new Vector3(2.66f, 1.83f, 2.81f),
            new Vector3(0.11f, 0.55f, 0.025f), terminalGreen);
        CreateBox("CrookedCompanyMark_C", root.transform, new Vector3(3.24f, 1.83f, 2.81f),
            new Vector3(0.11f, 0.55f, 0.025f), terminalGreen);
        CreateBox("CrookedCompanyMark_DebtSlash", root.transform, new Vector3(2.95f, 1.82f, 2.8f),
            new Vector3(0.13f, 0.78f, 0.025f), warningRed).transform.rotation = Quaternion.Euler(0f, 0f, -28f);

        var lightGo = new GameObject("OfficeTerminalSpill");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.position = new Vector3(-1.2f, 1.35f, 2f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.2f, 1f, 0.55f);
        light.intensity = 1.1f;
        light.range = 4f;

        var warmLightGo = new GameObject("OfficeSicklyOverhead");
        warmLightGo.transform.SetParent(root.transform, false);
        warmLightGo.transform.position = new Vector3(0.2f, 2.65f, 0.1f);
        var warmLight = warmLightGo.AddComponent<Light>();
        warmLight.type = LightType.Point;
        warmLight.color = new Color(0.9f, 0.8f, 0.55f);
        warmLight.intensity = 0.55f;
        warmLight.range = 5.5f;
    }

    static void BuildSchoolStyle()
    {
        RemoveGeneratedWorldText();
        if (GameObject.Find(SchoolRootName) != null) return;

        var root = new GameObject(SchoolRootName);
        ApplySchoolAtmosphere();
        Material coldPaint = MakeMaterial(new Color(0.22f, 0.3f, 0.31f));
        Material warningRed = MakeMaterial(new Color(0.64f, 0.035f, 0.025f));
        Material paper = MakeMaterial(new Color(0.82f, 0.8f, 0.68f));
        Material lightColor = MakeMaterial(new Color(0.56f, 0.92f, 0.86f));
        Material dark = MakeMaterial(new Color(0.045f, 0.05f, 0.055f));
        Material exitGreen = MakeOfficeMaterial("School_ExitGreen", new Color(0.05f, 0.62f, 0.24f), new Color(0.35f, 1f, 0.58f), OfficePattern.Scanline);
        Material vanBody = MakeOfficeMaterial("School_ReturnVanBody", new Color(0.065f, 0.1f, 0.08f), new Color(0.24f, 0.37f, 0.24f), OfficePattern.Scratched);
        Material vanGlass = MakeOfficeMaterial("School_ReturnVanGlass", new Color(0.012f, 0.025f, 0.03f), new Color(0.08f, 0.18f, 0.16f), OfficePattern.Scanline);
        Material tire = MakeMaterial(new Color(0.006f, 0.007f, 0.006f));

        for (int i = 0; i < 5; i++)
        {
            CreateBox($"ColdLocker_{i + 1}", root.transform, new Vector3(-10.8f, 0.95f, -5.4f + i * 1.3f),
                new Vector3(0.28f, 1.55f, 0.9f), coldPaint);
            CreateBox($"LockerDebtSticker_{i + 1}", root.transform, new Vector3(-10.95f, 1.22f, -5.4f + i * 1.3f),
                new Vector3(0.03f, 0.28f, 0.38f), warningRed);
        }

        CreateBox("HomeworkDebtBanner", root.transform, new Vector3(0f, 2.6f, 5.72f),
            new Vector3(4.8f, 0.55f, 0.035f), warningRed);

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

        CreateHidingLocker(root.transform, new Vector3(-4.75f, 0.95f, 4.7f), Quaternion.Euler(0f, 90f, 0f), coldPaint, warningRed);
        CreateHidingLocker(root.transform, new Vector3(4.75f, 0.95f, 4.7f), Quaternion.Euler(0f, -90f, 0f), coldPaint, warningRed);
        CreateSchoolExtractionVan(root.transform, vanBody, vanGlass, tire, dark, exitGreen, warningRed, paper);

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

    static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        if (go.TryGetComponent<Collider>(out var collider))
            Object.Destroy(collider);
        if (go.TryGetComponent<Renderer>(out var renderer))
            renderer.sharedMaterial = material;
        return go;
    }

    static GameObject CreateInteractionTrigger(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
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

    static GameObject CreateHidingLocker(Transform parent, Vector3 position, Quaternion rotation, Material body, Material warning)
    {
        GameObject locker = CreateBox("HidingLocker", parent, position, new Vector3(0.75f, 1.9f, 0.55f), body);
        locker.transform.rotation = rotation;
        locker.AddComponent<HidingSpot>();

        if (locker.TryGetComponent<BoxCollider>(out var collider))
        {
            collider.isTrigger = true;
            collider.size = new Vector3(1.35f, 1.15f, 1.35f);
            collider.center = new Vector3(0f, 0f, -0.15f);
        }

        CreateBox("HidingLocker_DebtSticker", locker.transform, position + locker.transform.forward * -0.31f + Vector3.up * 0.28f,
            new Vector3(0.42f, 0.22f, 0.035f), warning);
        return locker;
    }

    static void CreateSchoolExtractionVan(
        Transform root,
        Material vanBody,
        Material vanGlass,
        Material tire,
        Material darkMetal,
        Material exitGreen,
        Material warningRed,
        Material paper)
    {
        SchoolExitPoint exit = Object.FindAnyObjectByType<SchoolExitPoint>();
        Vector3 exitPosition = exit != null ? exit.transform.position : new Vector3(0f, 0.08f, -7.6f);
        Vector3 vanCenter = new Vector3(exitPosition.x, 0.78f, exitPosition.z - 1.55f);

        CreateBox("SchoolReturnVan_RearRamp", root, new Vector3(exitPosition.x, 0.12f, exitPosition.z - 0.22f),
            new Vector3(2.2f, 0.035f, 0.96f), exitGreen);
        for (int i = 0; i < 5; i++)
        {
            GameObject stripe = CreateBox($"SchoolReturnRampStripe_{i + 1}", root,
                new Vector3(exitPosition.x - 0.8f + i * 0.4f, 0.15f, exitPosition.z - 0.18f),
                new Vector3(0.26f, 0.02f, 0.08f), warningRed);
            stripe.transform.rotation = Quaternion.Euler(0f, 32f, 0f);
        }

        CreateBox("SchoolReturnVan_Body", root, vanCenter,
            new Vector3(2.05f, 1.18f, 2.42f), vanBody);
        CreateBox("SchoolReturnVan_RaisedRoof", root, vanCenter + new Vector3(0f, 0.72f, -0.1f),
            new Vector3(1.65f, 0.28f, 1.55f), vanBody);
        CreateBox("SchoolReturnVan_RearDoorLeft", root, new Vector3(exitPosition.x - 0.43f, 0.86f, exitPosition.z - 0.35f),
            new Vector3(0.78f, 0.86f, 0.055f), darkMetal);
        CreateBox("SchoolReturnVan_RearDoorRight", root, new Vector3(exitPosition.x + 0.43f, 0.86f, exitPosition.z - 0.35f),
            new Vector3(0.78f, 0.86f, 0.055f), darkMetal);
        CreateBox("SchoolReturnVan_RearWindowLeft", root, new Vector3(exitPosition.x - 0.43f, 1.12f, exitPosition.z - 0.41f),
            new Vector3(0.48f, 0.28f, 0.035f), vanGlass);
        CreateBox("SchoolReturnVan_RearWindowRight", root, new Vector3(exitPosition.x + 0.43f, 1.12f, exitPosition.z - 0.41f),
            new Vector3(0.48f, 0.28f, 0.035f), vanGlass);
        CreateBox("SchoolReturnVan_Bumper", root, new Vector3(exitPosition.x, 0.38f, exitPosition.z - 0.28f),
            new Vector3(1.92f, 0.18f, 0.12f), darkMetal);
        CreateBox("SchoolReturnVan_Plate", root, new Vector3(exitPosition.x, 0.55f, exitPosition.z - 0.22f),
            new Vector3(0.58f, 0.15f, 0.035f), paper);

        CreateCylinder("SchoolReturnVan_Wheel_LA", root, vanCenter + new Vector3(-1.08f, -0.42f, -0.7f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.18f, 0.32f), tire);
        CreateCylinder("SchoolReturnVan_Wheel_RA", root, vanCenter + new Vector3(1.08f, -0.42f, -0.7f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.18f, 0.32f), tire);
        CreateCylinder("SchoolReturnVan_Wheel_LB", root, vanCenter + new Vector3(-1.08f, -0.42f, 0.68f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.18f, 0.32f), tire);
        CreateCylinder("SchoolReturnVan_Wheel_RB", root, vanCenter + new Vector3(1.08f, -0.42f, 0.68f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.18f, 0.32f), tire);

        CreateBox("SchoolReturnVan_GreenLogoTop", root, new Vector3(exitPosition.x, 1.62f, exitPosition.z - 0.9f),
            new Vector3(1.0f, 0.06f, 0.12f), exitGreen);
        CreateBox("SchoolReturnVan_GreenLogoLeft", root, new Vector3(exitPosition.x - 0.42f, 1.43f, exitPosition.z - 0.9f),
            new Vector3(0.12f, 0.36f, 0.08f), exitGreen);
        CreateBox("SchoolReturnVan_GreenLogoRight", root, new Vector3(exitPosition.x + 0.42f, 1.43f, exitPosition.z - 0.9f),
            new Vector3(0.12f, 0.36f, 0.08f), exitGreen);
        GameObject slash = CreateBox("SchoolReturnVan_DebtSlash", root, new Vector3(exitPosition.x, 1.43f, exitPosition.z - 0.9f),
            new Vector3(0.12f, 0.54f, 0.08f), warningRed);
        slash.transform.rotation = Quaternion.Euler(0f, 0f, -26f);

        var exitLightGo = new GameObject("SchoolReturnVanExitBeacon");
        exitLightGo.transform.SetParent(root, false);
        exitLightGo.transform.position = new Vector3(exitPosition.x, 1.55f, exitPosition.z - 0.35f);
        var exitLight = exitLightGo.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.color = new Color(0.22f, 1f, 0.5f);
        exitLight.intensity = 1.1f;
        exitLight.range = 4.2f;
    }

    static void CreateOfficeWallStains(Transform root, Material grime, Material warningRed, Material paper, Material tape)
    {
        Vector3[] backWall =
        {
            new Vector3(-0.35f, 1.6f, 2.79f),
            new Vector3(1.55f, 2.12f, 2.79f),
            new Vector3(3.85f, 1.22f, 2.79f),
            new Vector3(-3.05f, 0.82f, 2.79f)
        };
        Vector3[] backScales =
        {
            new Vector3(1.3f, 0.62f, 0.025f),
            new Vector3(0.82f, 0.38f, 0.025f),
            new Vector3(0.95f, 0.52f, 0.025f),
            new Vector3(1.1f, 0.3f, 0.025f)
        };

        for (int i = 0; i < backWall.Length; i++)
            CreateBox($"BackWallGrimePatch_{i + 1}", root, backWall[i], backScales[i], grime);

        for (int i = 0; i < 6; i++)
        {
            float y = 1.2f + (i % 3) * 0.28f;
            CreateBox($"RightWallNotice_{i + 1}", root, new Vector3(4.79f, y, -1.4f + i * 0.42f),
                new Vector3(0.025f, 0.22f, 0.32f), i == 2 ? warningRed : paper);
            CreateBox($"RightWallTape_{i + 1}", root, new Vector3(4.765f, y + 0.13f, -1.4f + i * 0.42f),
                new Vector3(0.025f, 0.045f, 0.2f), tape);
        }

        for (int i = 0; i < 5; i++)
        {
            CreateBox($"PipeRun_{i + 1}", root, new Vector3(-4.78f, 2.35f, -2.7f + i * 1.05f),
                new Vector3(0.04f, 0.08f, 0.75f), grime);
        }

        CreateBox("WaterDamage_AboveComputer", root, new Vector3(-1.15f, 2.45f, 2.785f),
            new Vector3(1.45f, 0.36f, 0.025f), grime);
        CreateBox("RentPastDue_LongNotice", root, new Vector3(1.42f, 1.48f, 2.79f),
            new Vector3(0.7f, 0.48f, 0.025f), warningRed);
        CreateBox("PermitExpired_Notice", root, new Vector3(2.02f, 1.1f, 2.79f),
            new Vector3(0.52f, 0.34f, 0.025f), paper);
    }

    static void CreateOfficeCheckerFloor(Transform root, Material lightTile, Material darkTile)
    {
        const int columns = 10;
        const int rows = 8;
        const float tile = 0.92f;
        Vector3 origin = new Vector3(-4.14f, 0.055f, -3.15f);

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Material mat = (x + z) % 2 == 0 ? lightTile : darkTile;
                CreateBox($"OfficeFloorTile_{x}_{z}", root,
                    origin + new Vector3(x * tile, 0f, z * tile),
                    new Vector3(tile * 0.96f, 0.02f, tile * 0.96f), mat);
            }
        }
    }

    static void CreateOfficeArchitecturalTrim(Transform root, Material columnRed, Material darkMetal, Material plaster)
    {
        Vector3[] columns =
        {
            new Vector3(-4.72f, 1.35f, 2.72f),
            new Vector3(4.72f, 1.35f, 2.72f),
            new Vector3(-4.72f, 1.35f, -3.55f),
            new Vector3(4.72f, 1.35f, -3.55f)
        };

        foreach (Vector3 position in columns)
        {
            CreateBox("OfficeOldColumn", root, position, new Vector3(0.32f, 2.3f, 0.32f), columnRed);
            CreateBox("OfficeColumnFoot", root, position + Vector3.down * 1.12f, new Vector3(0.55f, 0.18f, 0.55f), darkMetal);
            CreateBox("OfficeColumnCap", root, position + Vector3.up * 1.15f, new Vector3(0.6f, 0.16f, 0.6f), darkMetal);
        }

        CreateBox("OfficeBackArchBeam", root, new Vector3(0f, 2.58f, 2.72f),
            new Vector3(9.4f, 0.22f, 0.28f), darkMetal);
        CreateBox("OfficeLeftArchBeam", root, new Vector3(-4.72f, 2.58f, -0.4f),
            new Vector3(0.28f, 0.22f, 6.4f), darkMetal);
        CreateBox("OfficeRightArchBeam", root, new Vector3(4.72f, 2.58f, -0.4f),
            new Vector3(0.28f, 0.22f, 6.4f), darkMetal);

        CreateBox("PeelingWallPatch_Large", root, new Vector3(1.95f, 1.95f, 2.785f),
            new Vector3(1.25f, 0.72f, 0.025f), plaster);
        CreateBox("PeelingWallPatch_Right", root, new Vector3(4.79f, 1.88f, 0.65f),
            new Vector3(0.025f, 0.86f, 1.1f), plaster);
        CreateBox("PeelingWallPatch_Left", root, new Vector3(-4.79f, 1.42f, -1.65f),
            new Vector3(0.025f, 0.62f, 0.86f), plaster);
    }

    static void CreateDispatchStationBase(Transform root, Material concrete, Material darkMetal, Material hazardStripe, Material terminalGreen)
    {
        CreateBox("DispatchStationPouredConcrete", root, new Vector3(0f, 0.085f, -0.18f),
            new Vector3(9.55f, 0.055f, 7.25f), concrete);
        CreateBox("DispatchVehicleLane", root, new Vector3(2.4f, 0.125f, -1.98f),
            new Vector3(3.85f, 0.018f, 3.45f), MakeMaterial(new Color(0.105f, 0.12f, 0.11f)));
        CreateBox("DispatchLaneCenterLine_A", root, new Vector3(0.55f, 0.15f, -1.98f),
            new Vector3(0.08f, 0.018f, 2.8f), terminalGreen);
        CreateBox("DispatchLaneCenterLine_B", root, new Vector3(4.25f, 0.15f, -1.98f),
            new Vector3(0.08f, 0.018f, 2.8f), terminalGreen);

        for (int i = 0; i < 7; i++)
        {
            GameObject slash = CreateBox($"DispatchLaneChevron_{i + 1}", root,
                new Vector3(0.85f + i * 0.52f, 0.16f, -0.78f),
                new Vector3(0.34f, 0.02f, 0.09f), hazardStripe);
            slash.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
        }

        CreateBox("DispatchFloorDrain", root, new Vector3(3.95f, 0.16f, -2.95f),
            new Vector3(0.75f, 0.018f, 0.2f), darkMetal);
        for (int i = 0; i < 5; i++)
            CreateBox($"DispatchFloorDrainSlot_{i + 1}", root, new Vector3(3.7f + i * 0.12f, 0.18f, -2.95f),
                new Vector3(0.035f, 0.012f, 0.24f), concrete);
    }

    static void CreateDispatchStationShell(
        Transform root,
        Material columnRed,
        Material darkMetal,
        Material plaster,
        Material bayDoor,
        Material terminalGreen,
        Material warningRed,
        Material paper,
        Material glass)
    {
        CreateBox("DispatchStationBackCorrugatedWall", root, new Vector3(0f, 1.42f, 2.815f),
            new Vector3(9.25f, 2.38f, 0.045f), bayDoor);
        for (int i = 0; i < 9; i++)
            CreateBox($"DispatchBackWallRib_{i + 1}", root, new Vector3(-4.15f + i * 1.04f, 1.42f, 2.765f),
                new Vector3(0.045f, 2.35f, 0.05f), darkMetal);

        CreateBox("DispatchLeftServiceWall", root, new Vector3(-4.78f, 1.42f, -0.4f),
            new Vector3(0.08f, 2.35f, 6.2f), bayDoor);
        CreateBox("DispatchRightServiceWall", root, new Vector3(4.78f, 1.42f, -0.4f),
            new Vector3(0.08f, 2.35f, 6.2f), bayDoor);
        CreateBox("DispatchCeilingBlackVoid", root, new Vector3(0f, 2.94f, -0.2f),
            new Vector3(9.35f, 0.055f, 7.0f), darkMetal);

        for (int i = 0; i < 4; i++)
            CreateBox($"DispatchOverheadTrack_{i + 1}", root, new Vector3(-3.2f + i * 2.1f, 2.72f, -1.05f),
                new Vector3(0.08f, 0.08f, 4.8f), columnRed);

        CreateBox("DispatchServiceWindowFrame", root, new Vector3(-1.2f, 1.62f, 1.42f),
            new Vector3(2.65f, 1.25f, 0.09f), darkMetal);
        CreateBox("DispatchServiceWindowGlass", root, new Vector3(-1.2f, 1.62f, 1.37f),
            new Vector3(2.28f, 0.78f, 0.035f), glass);
        CreateBox("DispatchServiceSlot", root, new Vector3(-1.2f, 1.02f, 1.33f),
            new Vector3(1.65f, 0.13f, 0.04f), terminalGreen);
        for (int i = 0; i < 8; i++)
            CreateBox($"DispatchServiceWindowBar_{i + 1}", root, new Vector3(-2.18f + i * 0.28f, 1.62f, 1.29f),
                new Vector3(0.035f, 0.88f, 0.035f), darkMetal);

        CreateBox("DispatchPunitiveRateBoard", root, new Vector3(-3.55f, 1.78f, 2.69f),
            new Vector3(1.3f, 0.76f, 0.035f), warningRed);
        CreateBox("DispatchFutureMapBoard", root, new Vector3(1.32f, 1.78f, 2.69f),
            new Vector3(1.35f, 0.82f, 0.035f), paper);
        CreateBox("DispatchMapPin_A", root, new Vector3(1.0f, 1.88f, 2.65f),
            new Vector3(0.1f, 0.1f, 0.035f), terminalGreen);
        CreateBox("DispatchMapPin_B", root, new Vector3(1.48f, 1.58f, 2.65f),
            new Vector3(0.1f, 0.1f, 0.035f), warningRed);

        CreateBox("DispatchPeelingPatch_UnderWindow", root, new Vector3(-0.35f, 0.74f, 1.31f),
            new Vector3(1.05f, 0.25f, 0.025f), plaster);
    }

    static void CreateDispatchOfficeBooth(
        Transform root,
        Material darkMetal,
        Material terminalGreen,
        Material warningRed,
        Material paper,
        Material glass,
        Material cardboard,
        Material wood)
    {
        CreateBox("DispatchCounterFront", root, new Vector3(-1.2f, 0.72f, 1.18f),
            new Vector3(2.95f, 0.62f, 0.18f), darkMetal);
        CreateBox("DispatchCounterTop", root, new Vector3(-1.2f, 1.04f, 1.18f),
            new Vector3(3.05f, 0.09f, 0.58f), wood);
        CreateBox("DispatchCounterGreenEdge", root, new Vector3(-1.2f, 1.1f, 0.86f),
            new Vector3(2.82f, 0.035f, 0.045f), terminalGreen);
        CreateBox("DispatchCounterWarningTape", root, new Vector3(0.42f, 1.115f, 1.18f),
            new Vector3(0.42f, 0.035f, 0.58f), warningRed).transform.rotation = Quaternion.Euler(0f, 18f, 0f);

        CreateBox("DispatchOldMonitorBackplate", root, new Vector3(-1.2f, 1.35f, 1.72f),
            new Vector3(1.1f, 0.74f, 0.08f), darkMetal);
        CreateBox("DispatchOldMonitorGlow", root, new Vector3(-1.2f, 1.36f, 1.66f),
            new Vector3(0.86f, 0.46f, 0.035f), terminalGreen);
        CreateBox("DispatchReceiptPrinter", root, new Vector3(-2.12f, 1.15f, 1.2f),
            new Vector3(0.52f, 0.24f, 0.34f), paper);
        for (int i = 0; i < 4; i++)
            CreateBox($"DispatchReceiptTrail_{i + 1}", root, new Vector3(-2.14f, 1.03f - i * 0.07f, 0.83f - i * 0.06f),
                new Vector3(0.32f, 0.025f, 0.2f), paper);

        CreateBox("DispatchCrateStack_A", root, new Vector3(-3.48f, 0.32f, 1.08f),
            new Vector3(0.64f, 0.42f, 0.58f), cardboard);
        CreateBox("DispatchCrateStack_B", root, new Vector3(-3.05f, 0.76f, 1.1f),
            new Vector3(0.58f, 0.38f, 0.52f), cardboard);
        CreateBox("DispatchCrateGreenLabel", root, new Vector3(-3.48f, 0.37f, 0.77f),
            new Vector3(0.34f, 0.12f, 0.035f), terminalGreen);

        var boothLightGo = new GameObject("DispatchBoothTerminalPool");
        boothLightGo.transform.SetParent(root, false);
        boothLightGo.transform.position = new Vector3(-1.2f, 1.32f, 1.15f);
        var boothLight = boothLightGo.AddComponent<Light>();
        boothLight.type = LightType.Point;
        boothLight.color = new Color(0.18f, 1f, 0.5f);
        boothLight.intensity = 0.9f;
        boothLight.range = 3.3f;
    }

    static void ApplyOfficeAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.045f, 0.06f, 0.05f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.ambientLight = new Color(0.035f, 0.044f, 0.038f);

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light == null || light.name != "OfficeLight") continue;
            light.intensity = Mathf.Min(light.intensity, 0.35f);
            light.color = new Color(0.72f, 0.78f, 0.58f);
        }
    }

    static void ApplySchoolAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.06f);
        RenderSettings.fogDensity = 0.025f;
        RenderSettings.ambientLight = new Color(0.025f, 0.032f, 0.038f);
    }

    static void CreateOfficeGarageAndVan(
        Transform root,
        Material concrete,
        Material bayDoor,
        Material hazardStripe,
        Material vanBody,
        Material vanGlass,
        Material rubber,
        Material darkMetal,
        Material terminalGreen,
        Material warningRed,
        Material paper,
        Material headlight)
    {
        CreateBox("GarageBayConcreteInset", root, new Vector3(2.2f, 0.075f, -2.32f),
            new Vector3(4.9f, 0.03f, 2.25f), concrete);
        CreateBox("GarageOilPool_A", root, new Vector3(1.35f, 0.098f, -2.15f),
            new Vector3(0.9f, 0.012f, 0.42f), darkMetal);
        CreateBox("GarageOilPool_B", root, new Vector3(3.05f, 0.099f, -2.8f),
            new Vector3(0.62f, 0.012f, 0.3f), darkMetal);

        for (int i = 0; i < 6; i++)
        {
            CreateBox($"GarageBayDoorPanel_{i + 1}", root, new Vector3(2.45f, 0.82f + i * 0.31f, -3.715f),
                new Vector3(3.35f, 0.23f, 0.055f), bayDoor);
            CreateBox($"GarageBayDoorSeam_{i + 1}", root, new Vector3(2.45f, 0.67f + i * 0.31f, -3.755f),
                new Vector3(3.45f, 0.035f, 0.035f), darkMetal);
        }

        CreateBox("GarageBayLeftPost", root, new Vector3(0.62f, 1.5f, -3.67f),
            new Vector3(0.16f, 2.45f, 0.16f), darkMetal);
        CreateBox("GarageBayRightPost", root, new Vector3(4.3f, 1.5f, -3.67f),
            new Vector3(0.16f, 2.45f, 0.16f), darkMetal);
        CreateBox("GarageBayTopMotor", root, new Vector3(2.45f, 2.68f, -3.6f),
            new Vector3(1.0f, 0.24f, 0.22f), darkMetal);

        for (int i = 0; i < 8; i++)
        {
            GameObject stripe = CreateBox($"GarageFloorHazardStripe_{i + 1}", root,
                new Vector3(0.5f + i * 0.55f, 0.12f, -1.16f),
                new Vector3(0.32f, 0.018f, 0.09f), hazardStripe);
            stripe.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 25f : -25f, 0f);
        }

        CreateBox("GarageCageTopRail", root, new Vector3(0.22f, 2.12f, -1.28f),
            new Vector3(2.1f, 0.08f, 0.08f), darkMetal);
        CreateBox("GarageCageBottomRail", root, new Vector3(0.22f, 0.82f, -1.28f),
            new Vector3(2.1f, 0.08f, 0.08f), darkMetal);
        for (int i = 0; i < 7; i++)
            CreateBox($"GarageCageVertical_{i + 1}", root, new Vector3(-0.78f + i * 0.33f, 1.46f, -1.28f),
                new Vector3(0.045f, 1.26f, 0.045f), darkMetal);

        CreateBox("DispatchRoutePaint_A", root, new Vector3(-0.25f, 0.115f, -0.25f),
            new Vector3(0.12f, 0.016f, 2.0f), terminalGreen);
        CreateBox("DispatchRoutePaint_B", root, new Vector3(0.72f, 0.115f, -1.08f),
            new Vector3(1.95f, 0.016f, 0.12f), terminalGreen);
        GameObject arrowLeft = CreateBox("DispatchRouteArrow_L", root, new Vector3(1.65f, 0.12f, -1.08f),
            new Vector3(0.62f, 0.018f, 0.12f), terminalGreen);
        arrowLeft.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject arrowRight = CreateBox("DispatchRouteArrow_R", root, new Vector3(1.65f, 0.12f, -1.08f),
            new Vector3(0.62f, 0.018f, 0.12f), terminalGreen);
        arrowRight.transform.rotation = Quaternion.Euler(0f, -35f, 0f);

        CreateBox("DispatchVan_Body", root, new Vector3(2.65f, 0.82f, -2.35f),
            new Vector3(1.65f, 1.08f, 2.45f), vanBody);
        CreateBox("DispatchVan_Nose", root, new Vector3(2.65f, 0.62f, -3.48f),
            new Vector3(1.5f, 0.72f, 0.72f), vanBody);
        CreateBox("DispatchVan_RoofRack", root, new Vector3(2.65f, 1.48f, -2.25f),
            new Vector3(1.3f, 0.11f, 1.55f), darkMetal);
        CreateBox("DispatchVan_Windshield", root, new Vector3(2.65f, 1.08f, -3.88f),
            new Vector3(1.18f, 0.42f, 0.045f), vanGlass);
        CreateBox("DispatchVan_LeftWindow", root, new Vector3(1.8f, 1.05f, -2.65f),
            new Vector3(0.045f, 0.38f, 0.72f), vanGlass);
        CreateBox("DispatchVan_RightWindow", root, new Vector3(3.5f, 1.05f, -2.65f),
            new Vector3(0.045f, 0.38f, 0.72f), vanGlass);
        CreateBox("DispatchVan_LeftHeadlight", root, new Vector3(2.08f, 0.65f, -3.86f),
            new Vector3(0.28f, 0.16f, 0.04f), headlight);
        CreateBox("DispatchVan_RightHeadlight", root, new Vector3(3.22f, 0.65f, -3.86f),
            new Vector3(0.28f, 0.16f, 0.04f), headlight);
        CreateBox("DispatchVan_Bumper", root, new Vector3(2.65f, 0.35f, -3.91f),
            new Vector3(1.68f, 0.18f, 0.12f), darkMetal);
        CreateBox("DispatchVan_Plate", root, new Vector3(2.65f, 0.5f, -3.98f),
            new Vector3(0.62f, 0.16f, 0.035f), paper);

        CreateCylinder("DispatchVan_Wheel_FL", root, new Vector3(1.72f, 0.34f, -3.06f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);
        CreateCylinder("DispatchVan_Wheel_RL", root, new Vector3(3.58f, 0.34f, -3.06f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);
        CreateCylinder("DispatchVan_Wheel_FB", root, new Vector3(1.72f, 0.34f, -1.65f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);
        CreateCylinder("DispatchVan_Wheel_RB", root, new Vector3(3.58f, 0.34f, -1.65f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);

        CreateBox("DispatchVan_LogoTop", root, new Vector3(1.79f, 1.04f, -2.02f),
            new Vector3(0.04f, 0.08f, 0.58f), terminalGreen);
        CreateBox("DispatchVan_LogoLeft", root, new Vector3(1.78f, 0.88f, -2.02f),
            new Vector3(0.04f, 0.42f, 0.08f), terminalGreen);
        CreateBox("DispatchVan_LogoRight", root, new Vector3(1.78f, 0.88f, -1.48f),
            new Vector3(0.04f, 0.42f, 0.08f), terminalGreen);
        GameObject slash = CreateBox("DispatchVan_DebtSlash", root, new Vector3(1.76f, 0.88f, -1.75f),
            new Vector3(0.045f, 0.52f, 0.08f), warningRed);
        slash.transform.rotation = Quaternion.Euler(0f, 0f, -25f);

        GameObject trigger = CreateInteractionTrigger("DispatchVanDepartureTrigger", root,
            new Vector3(2.65f, 0.92f, -2.52f), new Vector3(2.65f, 1.85f, 3.35f));
        trigger.AddComponent<OfficeDepartureVan>();

        var beaconGo = new GameObject("DispatchVanSicklyBeacon");
        beaconGo.transform.SetParent(root, false);
        beaconGo.transform.position = new Vector3(2.65f, 1.72f, -2.45f);
        var beacon = beaconGo.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.color = new Color(0.25f, 1f, 0.55f);
        beacon.intensity = 0.65f;
        beacon.range = 3.1f;

        var bayLightGo = new GameObject("GarageAmberBayLight");
        bayLightGo.transform.SetParent(root, false);
        bayLightGo.transform.position = new Vector3(2.55f, 2.48f, -2.2f);
        var bayLight = bayLightGo.AddComponent<Light>();
        bayLight.type = LightType.Point;
        bayLight.color = new Color(1f, 0.74f, 0.32f);
        bayLight.intensity = 0.75f;
        bayLight.range = 4.5f;
    }

    static void RemoveGeneratedWorldText()
    {
        TextMesh[] meshes = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude);
        foreach (var mesh in meshes)
        {
            if (mesh == null || mesh.gameObject == null) continue;
            if (mesh.gameObject.name.StartsWith("Text_"))
                Object.Destroy(mesh.gameObject);
        }
    }

    static Material MakeOfficeMaterial(string name, Color baseColor, Color accentColor, OfficePattern pattern)
    {
        Material material = MakeMaterial(baseColor);
        material.name = name;

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.name = $"{name}_Texture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float noise = Hash01(x, y, (int)pattern * 17);
                Color color = Color.Lerp(baseColor * 0.82f, baseColor * 1.18f, noise);

                switch (pattern)
                {
                    case OfficePattern.Grime:
                        if (y % 19 == 0 || x % 31 == 0) color = Color.Lerp(color, accentColor, 0.35f);
                        if (noise > 0.88f) color = Color.Lerp(color, accentColor, 0.45f);
                        break;
                    case OfficePattern.Tile:
                        if (x % 16 == 0 || y % 16 == 0) color = accentColor * 0.65f;
                        if ((x / 16 + y / 16) % 2 == 0) color *= 0.88f;
                        break;
                    case OfficePattern.Notice:
                        if (x < 3 || y < 3 || x > 60 || y > 60) color = accentColor * 0.45f;
                        if ((x > 12 && x < 52 && (y == 20 || y == 35 || y == 48))) color = accentColor;
                        break;
                    case OfficePattern.Warning:
                        if ((x + y) % 18 < 7) color = Color.Lerp(baseColor, accentColor, 0.18f);
                        if (x < 4 || y < 4 || x > 59 || y > 59) color = accentColor * 0.75f;
                        break;
                    case OfficePattern.Scanline:
                        color = y % 4 == 0 ? accentColor : Color.Lerp(baseColor, Color.black, 0.18f);
                        if (noise > 0.96f) color = accentColor;
                        break;
                    case OfficePattern.Scratched:
                        if (noise > 0.92f || (x + y * 3) % 37 == 0) color = accentColor;
                        if (y % 13 == 0) color *= 0.7f;
                        break;
                    case OfficePattern.Fabric:
                        if (x % 5 == 0 || y % 7 == 0) color = Color.Lerp(color, accentColor, 0.4f);
                        break;
                    case OfficePattern.Cardboard:
                        if (x % 11 == 0) color = Color.Lerp(color, accentColor, 0.45f);
                        if (y > 28 && y < 35) color *= 0.72f;
                        break;
                    case OfficePattern.Wood:
                        float grain = Mathf.Sin((y + noise * 18f) * 0.35f) * 0.5f + 0.5f;
                        color = Color.Lerp(baseColor, accentColor, grain * 0.55f);
                        break;
                    case OfficePattern.Blinds:
                        color = y % 8 < 3 ? baseColor : accentColor;
                        if (noise > 0.94f) color *= 0.55f;
                        break;
                }

                color.a = 1f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
        }
        else if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", new Vector2(3f, 3f));
        }
        return material;
    }

    static float Hash01(int x, int y, int seed)
    {
        uint h = unchecked((uint)(x * 374761393 + y * 668265263 + seed * 1442695041));
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0x00ffffff) / 16777215f;
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
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.08f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        return material;
    }

    static Material MakeEmissiveMaterial(Color baseColor, Color emissionColor, float intensity)
    {
        Material material = MakeMaterial(baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor * intensity);
        }
        return material;
    }
}
