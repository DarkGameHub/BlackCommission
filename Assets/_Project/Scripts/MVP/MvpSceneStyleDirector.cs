using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MvpSceneStyleDirector
{
    const string OfficeRootName = "MVP_RuntimeStyle_Office_ExteriorDispatch";
    const string SchoolRootName = "MVP_RuntimeStyle_School";

    static readonly Color CivicTeal = Rgb(0x2F, 0x4F, 0x4B);
    static readonly Color CivicTealDark = Rgb(0x17, 0x24, 0x22);
    static readonly Color CivicTealShadow = Rgb(0x20, 0x35, 0x32);
    static readonly Color DeadRubber = Rgb(0x11, 0x14, 0x13);
    static readonly Color DeadRubberSoft = Rgb(0x23, 0x28, 0x25);
    static readonly Color AgedPaper = Rgb(0xD6, 0xC8, 0x9B);
    static readonly Color AgedPaperDark = Rgb(0x86, 0x7A, 0x58);
    static readonly Color CheapCardboard = Rgb(0x73, 0x50, 0x2A);
    static readonly Color SecondHandWood = Rgb(0x4A, 0x31, 0x19);
    static readonly Color DispatchGreen = Rgb(0x7B, 0xCF, 0x8A);
    static readonly Color DispatchGreenDark = Rgb(0x22, 0x59, 0x3A);
    static readonly Color StampRed = Rgb(0xC2, 0x3A, 0x2B);
    static readonly Color StampRedDark = Rgb(0x63, 0x18, 0x14);
    static readonly Color SodiumAmber = Rgb(0xD9, 0x9A, 0x31);
    static readonly Color SodiumAmberPale = Rgb(0xE2, 0xC2, 0x78);
    static readonly Color DirtyBone = Rgb(0xC9, 0xC2, 0xAA);
    static readonly Color TiredFabric = Rgb(0x2C, 0x32, 0x2B);
    static readonly Color OldGlass = Rgb(0x1C, 0x3C, 0x3E);
    static readonly Vector3 SchoolNotebookPosition = new(8.15f, 0.96f, 5.7f);
    static readonly Vector3 SchoolLedgerPosition = new(-7.55f, 0.96f, 1.38f);

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
        ClearExistingOfficeStyleRoots();

        var root = new GameObject(OfficeRootName);
        EnsureHqMenuCamera(root.transform);
        OpenOriginalBackWallForExteriorExit();
        HideOriginalOfficeBlockoutProps();
        CreateWalkableCollider("HQExteriorSafetyGround", root.transform,
            new Vector3(1.8f, -0.08f, -4.2f), new Vector3(10.5f, 0.32f, 11.8f));
        CreateWalkableCollider("GarageExitWalkableThreshold", root.transform,
            new Vector3(2.45f, -0.04f, -3.35f), new Vector3(3.65f, 0.22f, 2.25f));
        CreateWalkableCollider("ExteriorDispatchYardWalkable", root.transform,
            new Vector3(2.45f, -0.04f, -6.2f), new Vector3(5.75f, 0.22f, 4.65f));
        CreateOfficeBoundaryColliders(root.transform);
        ApplyOfficeAtmosphere();
        Material stainedWall = MakeOfficeMaterial("Office_CivicTealWall", CivicTeal, CivicTealDark, OfficePattern.Grime);
        Material dirtyFloor = MakeOfficeMaterial("Office_WornCivicFloor", DeadRubberSoft, CivicTealShadow, OfficePattern.Tile);
        Material paper = MakeOfficeMaterial("Office_AgedPaper", AgedPaper, StampRedDark, OfficePattern.Notice);
        Material warningRed = MakeOfficeMaterial("Office_StampRedDebt", StampRed, AgedPaper, OfficePattern.Warning);
        Material terminalGreen = MakeOfficeMaterial("Office_DispatchGreen", DispatchGreenDark, DispatchGreen, OfficePattern.Scanline);
        Material darkMetal = MakeOfficeMaterial("Office_DeadRubberMetal", DeadRubber, DeadRubberSoft, OfficePattern.Scratched);
        Material tiredFabric = MakeOfficeMaterial("Office_TiredFabric", TiredFabric, DeadRubber, OfficePattern.Fabric);
        Material cardboard = MakeOfficeMaterial("Office_CheapCardboard", CheapCardboard, AgedPaperDark, OfficePattern.Cardboard);
        Material wood = MakeOfficeMaterial("Office_SecondHandWood", SecondHandWood, CheapCardboard, OfficePattern.Wood);
        Material cable = MakeMaterial(DeadRubber);
        Material black = MakeMaterial(DeadRubber);
        Material grime = MakeMaterial(CivicTealDark);
        Material oldTape = MakeMaterial(SodiumAmberPale);
        Material oldPlaster = MakeOfficeMaterial("Office_PeeledPlaster", DirtyBone, CivicTealDark, OfficePattern.Grime);
        Material columnRed = MakeOfficeMaterial("Office_DarkStampColumnPaint", StampRedDark, DeadRubber, OfficePattern.Scratched);
        Material tileLight = MakeMaterial(DirtyBone);
        Material tileDark = MakeMaterial(DeadRubberSoft);
        Material carpet = MakeOfficeMaterial("Office_ThreadbareCarpet", TiredFabric, CivicTealDark, OfficePattern.Fabric);
        Material garageConcrete = MakeOfficeMaterial("Office_GarageConcrete", DeadRubberSoft, DeadRubber, OfficePattern.Grime);
        Material bayDoor = MakeOfficeMaterial("Office_DentedBayDoor", CivicTealShadow, DeadRubber, OfficePattern.Scratched);
        Material hazardStripe = MakeOfficeMaterial("Office_SodiumHazardStripe", SodiumAmber, DeadRubber, OfficePattern.Warning);
        Material vanBody = MakeOfficeMaterial("Office_CivicFleetVanBody", CivicTeal, DirtyBone, OfficePattern.Scratched);
        Material vanGlass = MakeOfficeMaterial("Office_OldGlass", OldGlass, DeadRubber, OfficePattern.Scanline);
        Material rubber = MakeMaterial(DeadRubber);
        Material headlight = MakeEmissiveMaterial(SodiumAmberPale, SodiumAmberPale, 0.9f);

        BuildCleanReadableOfficeAndExterior(root.transform, stainedWall, dirtyFloor, paper, warningRed,
            terminalGreen, darkMetal, tiredFabric, cardboard, wood, garageConcrete, bayDoor, hazardStripe,
            vanBody, vanGlass, rubber, headlight);
#pragma warning disable CS0162
        return;

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
        CreateBox("SouthWallStainPanel_LeftOfExit", root.transform, new Vector3(-2.05f, 1.55f, -3.765f),
            new Vector3(5.05f, 2.9f, 0.035f), stainedWall);
        CreateBox("SouthWallStainPanel_RightOfExit", root.transform, new Vector3(4.65f, 1.55f, -3.765f),
            new Vector3(0.85f, 2.9f, 0.035f), stainedWall);
        CreateBox("SouthWallExitHeader", root.transform, new Vector3(2.35f, 2.62f, -3.765f),
            new Vector3(3.3f, 0.62f, 0.04f), stainedWall);
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
        light.intensity = 1.45f;
        light.range = 4.8f;

        var warmLightGo = new GameObject("OfficeSicklyOverhead");
        warmLightGo.transform.SetParent(root.transform, false);
        warmLightGo.transform.position = new Vector3(0.2f, 2.65f, 0.1f);
        var warmLight = warmLightGo.AddComponent<Light>();
        warmLight.type = LightType.Point;
        warmLight.color = new Color(1f, 0.88f, 0.62f);
        warmLight.intensity = 1.45f;
        warmLight.range = 7.2f;

        var roomFillGo = new GameObject("OfficeReadableFillLight");
        roomFillGo.transform.SetParent(root.transform, false);
        roomFillGo.transform.position = new Vector3(0f, 2.35f, -1.75f);
        var roomFill = roomFillGo.AddComponent<Light>();
        roomFill.type = LightType.Point;
        roomFill.color = new Color(0.75f, 0.92f, 0.78f);
        roomFill.intensity = 0.8f;
        roomFill.range = 8.5f;
#pragma warning restore CS0162
    }

    static void BuildCleanReadableOfficeAndExterior(
        Transform root,
        Material wall,
        Material floor,
        Material paper,
        Material warningRed,
        Material terminalGreen,
        Material darkMetal,
        Material fabric,
        Material cardboard,
        Material wood,
        Material concrete,
        Material bayDoor,
        Material hazardStripe,
        Material vanBody,
        Material vanGlass,
        Material rubber,
        Material headlight)
    {
        Material exitGreen = MakeEmissiveMaterial(DispatchGreenDark, DispatchGreen, 0.45f);
        Material lightPanel = MakeEmissiveMaterial(DirtyBone, SodiumAmberPale, 0.34f);
        Material asphalt = MakeOfficeMaterial("Office_ExteriorDeadAsphalt", DeadRubber, DeadRubberSoft, OfficePattern.Grime);

        CreateBox("HQReadableInteriorFloor", root, new Vector3(0f, 0.025f, 0f),
            new Vector3(6.15f, 0.04f, 5.1f), floor);
        CreateBox("HQReadableNorthWall", root, new Vector3(0f, 1.45f, 2.55f),
            new Vector3(6.15f, 2.85f, 0.08f), wall);
        CreateBox("HQReadableLeftWall", root, new Vector3(-3.05f, 1.45f, 0f),
            new Vector3(0.08f, 2.85f, 5.1f), wall);
        CreateBox("HQReadableRightWall", root, new Vector3(3.05f, 1.45f, 0.65f),
            new Vector3(0.08f, 2.85f, 3.65f), wall);
        CreateBox("HQReadableCeiling", root, new Vector3(0f, 2.88f, 0f),
            new Vector3(6.1f, 0.06f, 5.05f), MakeOfficeMaterial("Office_CheapCeilingTile", Rgb(0x7C, 0x78, 0x66), DeadRubberSoft, OfficePattern.Tile));

        CreateBox("HQExitHeader", root, new Vector3(2.1f, 2.55f, -2.55f),
            new Vector3(2.9f, 0.6f, 0.1f), wall);
        CreateBox("HQExitFrameLeft", root, new Vector3(0.62f, 1.22f, -2.62f),
            new Vector3(0.14f, 2.35f, 0.18f), darkMetal);
        CreateBox("HQExitFrameRight", root, new Vector3(3.58f, 1.22f, -2.62f),
            new Vector3(0.14f, 2.35f, 0.18f), darkMetal);
        CreateBox("HQExitThresholdHazard", root, new Vector3(2.1f, 0.07f, -2.82f),
            new Vector3(3.05f, 0.035f, 0.36f), hazardStripe);
        CreateBox("HQExitSignPanel", root, new Vector3(2.1f, 2.23f, -2.64f),
            new Vector3(1.35f, 0.28f, 0.035f), exitGreen);
        CreateBox("HQExitIconLeftStroke", root, new Vector3(1.82f, 2.23f, -2.69f),
            new Vector3(0.44f, 0.055f, 0.025f), darkMetal).transform.rotation = Quaternion.Euler(0f, 0f, 28f);
        CreateBox("HQExitIconRightStroke", root, new Vector3(2.38f, 2.23f, -2.69f),
            new Vector3(0.44f, 0.055f, 0.025f), darkMetal).transform.rotation = Quaternion.Euler(0f, 0f, -28f);

        CreateBox("HQExteriorParkingPad", root, new Vector3(2.45f, 0.03f, -6.15f),
            new Vector3(5.45f, 0.045f, 4.45f), asphalt);
        CreateBox("HQExteriorConcreteApron", root, new Vector3(2.1f, 0.045f, -3.65f),
            new Vector3(3.3f, 0.025f, 0.72f), concrete);
        CreateBox("HQParkingLeftLine", root, new Vector3(1.2f, 0.08f, -6.15f),
            new Vector3(0.08f, 0.018f, 3.45f), paper);
        CreateBox("HQParkingRightLine", root, new Vector3(4.0f, 0.08f, -6.15f),
            new Vector3(0.08f, 0.018f, 3.45f), paper);
        CreateBox("HQExteriorCurbLeft", root, new Vector3(-0.48f, 0.16f, -6.1f),
            new Vector3(0.16f, 0.18f, 4.55f), darkMetal);
        CreateBox("HQExteriorCurbRight", root, new Vector3(5.38f, 0.16f, -6.1f),
            new Vector3(0.16f, 0.18f, 4.55f), darkMetal);
        CreateBox("HQExteriorStreetStopCurb", root, new Vector3(2.45f, 0.18f, -8.55f),
            new Vector3(5.85f, 0.2f, 0.16f), darkMetal);
        CreateBox("HQExteriorBackFenceLeft", root, new Vector3(0.06f, 0.72f, -3.64f),
            new Vector3(1.36f, 1.18f, 0.08f), darkMetal);
        CreateBox("HQExteriorBackFenceRight", root, new Vector3(4.56f, 0.72f, -3.64f),
            new Vector3(1.86f, 1.18f, 0.08f), darkMetal);
        CreateBox("HQGarageDoorLeftSafetyPost", root, new Vector3(0.54f, 1.12f, -2.82f),
            new Vector3(0.16f, 2.15f, 0.16f), darkMetal);
        CreateBox("HQGarageDoorRightSafetyPost", root, new Vector3(3.66f, 1.12f, -2.82f),
            new Vector3(0.16f, 2.15f, 0.16f), darkMetal);

        CreateBox("HQRouteLineInside", root, new Vector3(2.08f, 0.075f, -1.25f),
            new Vector3(0.16f, 0.02f, 2.05f), terminalGreen);
        CreateBox("HQRouteLineOutside", root, new Vector3(2.45f, 0.08f, -4.95f),
            new Vector3(0.18f, 0.02f, 2.85f), terminalGreen);
        GameObject insideArrowLeft = CreateBox("HQRouteArrowInsideLeft", root, new Vector3(2.08f, 0.09f, -2.12f),
            new Vector3(0.64f, 0.02f, 0.12f), terminalGreen);
        insideArrowLeft.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject insideArrowRight = CreateBox("HQRouteArrowInsideRight", root, new Vector3(2.08f, 0.09f, -2.12f),
            new Vector3(0.64f, 0.02f, 0.12f), terminalGreen);
        insideArrowRight.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
        GameObject outsideArrowLeft = CreateBox("HQRouteArrowOutsideLeft", root, new Vector3(2.45f, 0.09f, -6.35f),
            new Vector3(0.7f, 0.02f, 0.12f), terminalGreen);
        outsideArrowLeft.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject outsideArrowRight = CreateBox("HQRouteArrowOutsideRight", root, new Vector3(2.45f, 0.09f, -6.35f),
            new Vector3(0.7f, 0.02f, 0.12f), terminalGreen);
        outsideArrowRight.transform.rotation = Quaternion.Euler(0f, -35f, 0f);

        CreateBox("HQDispatchDeskTop", root, new Vector3(-1.35f, 0.58f, 1.62f),
            new Vector3(1.9f, 0.09f, 0.86f), wood);
        CreateBox("HQDispatchDeskLeftLeg", root, new Vector3(-1.98f, 0.3f, 1.62f),
            new Vector3(0.1f, 0.56f, 0.68f), darkMetal);
        CreateBox("HQDispatchDeskRightLeg", root, new Vector3(-0.72f, 0.3f, 1.62f),
            new Vector3(0.1f, 0.56f, 0.68f), darkMetal);
        CreateBox("HQDeskReceiptPrinter", root, new Vector3(-1.92f, 0.686f, 1.62f),
            new Vector3(0.3f, 0.12f, 0.22f), paper);
        CreateBox("HQDeskReceiptTrailA", root, new Vector3(-1.94f, 0.635f, 1.43f),
            new Vector3(0.18f, 0.018f, 0.22f), paper);
        CreateBox("HQDeskReceiptTrailB", root, new Vector3(-1.88f, 0.635f, 1.29f),
            new Vector3(0.16f, 0.016f, 0.14f), paper).transform.rotation = Quaternion.Euler(0f, 8f, 0f);
        CreateReadableComputerTerminal(root, darkMetal, terminalGreen, paper, lightPanel);

        CreateBox("HQDebtBoard", root, new Vector3(-2.45f, 1.65f, 2.49f),
            new Vector3(0.95f, 0.72f, 0.04f), warningRed);
        CreateBox("HQDebtBoardHeader", root, new Vector3(-2.45f, 1.92f, 2.455f),
            new Vector3(0.8f, 0.1f, 0.025f), darkMetal);
        CreateBox("HQDebtBoardTallyA", root, new Vector3(-2.68f, 1.58f, 2.445f),
            new Vector3(0.045f, 0.42f, 0.025f), paper);
        CreateBox("HQDebtBoardTallyB", root, new Vector3(-2.52f, 1.58f, 2.445f),
            new Vector3(0.045f, 0.42f, 0.025f), paper);
        CreateBox("HQDebtBoardTallyC", root, new Vector3(-2.36f, 1.58f, 2.445f),
            new Vector3(0.045f, 0.42f, 0.025f), paper);
        CreateBox("HQDebtBoardTallySlash", root, new Vector3(-2.52f, 1.58f, 2.44f),
            new Vector3(0.045f, 0.56f, 0.025f), paper).transform.rotation = Quaternion.Euler(0f, 0f, -24f);
        for (int i = 0; i < 4; i++)
            CreateBox($"HQSimpleNotice_{i + 1}", root, new Vector3(-1.08f + i * 0.32f, 1.58f, 2.49f),
                new Vector3(0.22f, 0.28f, 0.03f), i == 1 ? warningRed : paper);

        CreateBox("HQCompanyMarkBackplate", root, new Vector3(1.28f, 1.72f, 2.49f),
            new Vector3(0.9f, 0.58f, 0.035f), darkMetal);
        CreateBox("HQCompanyMarkTop", root, new Vector3(1.28f, 1.9f, 2.455f),
            new Vector3(0.55f, 0.055f, 0.025f), terminalGreen);
        CreateBox("HQCompanyMarkLeft", root, new Vector3(1.02f, 1.69f, 2.455f),
            new Vector3(0.07f, 0.42f, 0.025f), terminalGreen);
        CreateBox("HQCompanyMarkRight", root, new Vector3(1.54f, 1.69f, 2.455f),
            new Vector3(0.07f, 0.42f, 0.025f), terminalGreen);
        CreateBox("HQCompanyMarkDebtSlash", root, new Vector3(1.28f, 1.68f, 2.445f),
            new Vector3(0.08f, 0.62f, 0.025f), warningRed).transform.rotation = Quaternion.Euler(0f, 0f, -24f);
        CreateBox("HQDebtPressureGaugeTrack", root, new Vector3(2.14f, 1.35f, 2.49f),
            new Vector3(0.52f, 0.08f, 0.03f), darkMetal);
        for (int i = 0; i < 4; i++)
            CreateBox($"HQDebtPressureGaugeBar_{i + 1}", root, new Vector3(1.92f + i * 0.14f, 1.35f, 2.455f),
                new Vector3(0.08f, 0.16f + i * 0.055f, 0.025f), warningRed);

        CreateBox("HQEquipmentShelfLeftUpright", root, new Vector3(2.16f, 0.82f, 1.8f),
            new Vector3(0.08f, 1.55f, 0.28f), darkMetal);
        CreateBox("HQEquipmentShelfRightUpright", root, new Vector3(2.94f, 0.82f, 1.8f),
            new Vector3(0.08f, 1.55f, 0.28f), darkMetal);
        CreateBox("HQEquipmentShelfBackBrace", root, new Vector3(2.55f, 1.55f, 1.92f),
            new Vector3(0.86f, 0.08f, 0.08f), darkMetal);
        for (int i = 0; i < 3; i++)
            CreateBox($"HQEquipmentShelfPlank_{i + 1}", root, new Vector3(2.55f, 0.28f + i * 0.48f, 1.8f),
                new Vector3(0.9f, 0.055f, 0.32f), darkMetal);
        CreateBox("HQMedkitBox", root, new Vector3(2.25f, 1.368f, 1.74f),
            new Vector3(0.28f, 0.2f, 0.16f), paper);
        CreateBox("HQMedkitCrossH", root, new Vector3(2.25f, 1.368f, 1.648f),
            new Vector3(0.18f, 0.035f, 0.025f), warningRed);
        CreateBox("HQMedkitCrossV", root, new Vector3(2.25f, 1.368f, 1.648f),
            new Vector3(0.035f, 0.14f, 0.025f), warningRed);
        CreateBox("HQToolBox", root, new Vector3(2.58f, 0.888f, 1.76f),
            new Vector3(0.36f, 0.2f, 0.18f), warningRed);
        CreateCylinder("HQSprayCan", root, new Vector3(2.85f, 0.97f, 1.8f),
            Quaternion.identity, new Vector3(0.08f, 0.18f, 0.08f), terminalGreen);
        CreateCylinder("HQDecoyBell", root, new Vector3(2.25f, 0.4f, 1.74f),
            Quaternion.identity, new Vector3(0.12f, 0.09f, 0.12f), cardboard);
        CreateCylinder("HQFlashlightBody", root, new Vector3(2.74f, 0.34f, 1.78f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.06f, 0.22f, 0.06f), darkMetal);
        CreateBox("HQCardboardSupplyBox", root, new Vector3(2.84f, 0.14f, 1.72f),
            new Vector3(0.22f, 0.18f, 0.22f), cardboard);
        CreateBox("HQEquipmentShelfLeftFoot", root, new Vector3(2.16f, 0.067f, 1.8f),
            new Vector3(0.22f, 0.045f, 0.36f), darkMetal);
        CreateBox("HQEquipmentShelfRightFoot", root, new Vector3(2.94f, 0.067f, 1.8f),
            new Vector3(0.22f, 0.045f, 0.36f), darkMetal);
        CreateOfficeGroundStoragePads(root, terminalGreen, warningRed, paper, darkMetal, cardboard);
        CreateBox("HQSofaContactShadow", root, new Vector3(1.05f, 0.058f, 2.18f),
            new Vector3(1.22f, 0.018f, 0.6f), darkMetal);
        CreateBox("HQSofaSeat", root, new Vector3(1.05f, 0.215f, 2.18f),
            new Vector3(1.1f, 0.34f, 0.5f), fabric);
        CreateBox("HQSofaBack", root, new Vector3(1.05f, 0.435f, 2.43f),
            new Vector3(1.1f, 0.78f, 0.16f), fabric);
        CreateBox("HQSofaMissingCushion", root, new Vector3(0.72f, 0.405f, 1.96f),
            new Vector3(0.3f, 0.035f, 0.26f), darkMetal);
        CreateBox("HQFilingCabinetContactShadow", root, new Vector3(-2.64f, 0.058f, 1.55f),
            new Vector3(0.5f, 0.018f, 0.44f), darkMetal);
        CreateBox("HQGroundedFilingCabinet", root, new Vector3(-2.64f, 0.645f, 1.55f),
            new Vector3(0.42f, 1.2f, 0.36f), darkMetal);
        for (int i = 0; i < 3; i++)
            CreateBox($"HQFilingCabinetHandle_{i + 1}", root, new Vector3(-2.64f, 0.98f - i * 0.26f, 1.36f),
                new Vector3(0.26f, 0.035f, 0.025f), paper);

        CreateBox("HQOfficeMainLightPanel", root, new Vector3(0f, 2.82f, 0.75f),
            new Vector3(1.9f, 0.045f, 0.2f), lightPanel);
        CreateBox("HQExitLightPanel", root, new Vector3(2.1f, 2.72f, -2.05f),
            new Vector3(1.25f, 0.045f, 0.2f), lightPanel);
        CreateBox("HQParkingBayDoorRolledUp", root, new Vector3(2.1f, 2.55f, -2.85f),
            new Vector3(3.1f, 0.28f, 0.12f), bayDoor);
        CreateBox("HQGarageWorkLightBar", root, new Vector3(2.1f, 2.42f, -3.08f),
            new Vector3(2.35f, 0.045f, 0.16f), lightPanel);
        CreateBox("HQYardFloodLightHousing", root, new Vector3(2.45f, 2.82f, -4.02f),
            new Vector3(1.15f, 0.12f, 0.18f), darkMetal);
        CreateBox("HQVanOverheadLightPanel", root, new Vector3(2.65f, 2.64f, -6.25f),
            new Vector3(2.55f, 0.05f, 0.28f), lightPanel);
        CreateBox("HQVanRearWorkLightPanel", root, new Vector3(2.65f, 2.16f, -7.72f),
            new Vector3(1.45f, 0.05f, 0.18f), lightPanel);
        CreateBox("HQGarageVisibleLightPoolUnderVan", root, new Vector3(2.65f, 0.112f, -6.35f),
            new Vector3(3.25f, 0.018f, 3.55f), MakeEmissiveMaterial(SodiumAmber, SodiumAmberPale, 0.18f));

        if (!CreateGeneratedDispatchVanIfAvailable(root, terminalGreen))
            CreateCleanFallbackExteriorVan(root, vanBody, vanGlass, rubber, darkMetal, terminalGreen, warningRed, paper, headlight);

        var officeLightGo = new GameObject("HQReadableOfficeLight");
        officeLightGo.transform.SetParent(root, false);
        officeLightGo.transform.position = new Vector3(0f, 2.35f, 0.55f);
        var officeLight = officeLightGo.AddComponent<Light>();
        officeLight.type = LightType.Point;
        officeLight.color = SodiumAmberPale;
        officeLight.intensity = 1.35f;
        officeLight.range = 6.4f;

        var exitLightGo = new GameObject("HQReadableExitLight");
        exitLightGo.transform.SetParent(root, false);
        exitLightGo.transform.position = new Vector3(2.1f, 2f, -2.25f);
        var exitLight = exitLightGo.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.color = DispatchGreen;
        exitLight.intensity = 0.5f;
        exitLight.range = 3.6f;

        CreateSpotLight("HQGarageWorkLight", root, new Vector3(2.1f, 2.35f, -3.1f),
            new Vector3(2.1f, 0.08f, -4.1f), SodiumAmberPale, 3.8f, 9.2f, 86f);
        CreateSpotLight("HQVanOverheadWorkLight", root, new Vector3(2.65f, 2.55f, -6.25f),
            new Vector3(2.65f, 0.35f, -6.35f), SodiumAmberPale, 4.2f, 8.5f, 88f);
        CreatePointLight("HQGarageVehicleFillLight", root, new Vector3(2.55f, 1.55f, -5.85f),
            SodiumAmberPale, 3.2f, 8.6f);
        CreatePointLight("HQGarageFloorBounceLight", root, new Vector3(2.65f, 0.85f, -6.45f),
            SodiumAmber, 1.4f, 6.8f);
        CreatePointLight("HQGarageGreenServiceLamp", root, new Vector3(1.05f, 1.42f, -3.85f),
            DispatchGreen, 0.85f, 4.6f);
        CreateSpotLight("HQYardFloodLight", root, new Vector3(2.45f, 3.05f, -4.25f),
            new Vector3(2.45f, 0.05f, -6.05f), SodiumAmber, 3.6f, 10.2f, 80f);
        CreateSpotLight("HQVanHeadlightCone", root, new Vector3(2.65f, 0.7f, -7.65f),
            new Vector3(2.65f, 0.18f, -8.35f), SodiumAmberPale, 1.25f, 4.2f, 52f);
    }

    static void CreateReadableComputerTerminal(
        Transform root,
        Material darkMetal,
        Material terminalGreen,
        Material paper,
        Material lightPanel)
    {
        OfficeComputer computer = Object.FindAnyObjectByType<OfficeComputer>();
        if (computer == null) return;

        if (computer.TryGetComponent<Renderer>(out var renderer))
            renderer.enabled = false;

        if (computer.TryGetComponent<BoxCollider>(out var collider))
        {
            collider.isTrigger = true;
            collider.center = new Vector3(0f, -0.05f, 3f);
            collider.size = new Vector3(1.25f, 1.35f, 2.6f);
        }

        Vector3 p = computer.transform.position;
        Vector3 screenCenter = new Vector3(p.x, 0.98f, 1.84f);

        CreateBox("HQComputerMonitorShell", root, screenCenter + new Vector3(0f, 0f, 0.08f),
            new Vector3(0.92f, 0.46f, 0.16f), darkMetal);
        CreateBox("HQComputerScreenGlass", root, screenCenter,
            new Vector3(0.7f, 0.32f, 0.035f), terminalGreen);
        CreateBox("HQComputerScreenScanlineA", root, screenCenter + new Vector3(0f, 0.06f, -0.025f),
            new Vector3(0.6f, 0.026f, 0.02f), paper);
        CreateBox("HQComputerScreenScanlineB", root, screenCenter + new Vector3(-0.08f, -0.035f, -0.025f),
            new Vector3(0.48f, 0.022f, 0.02f), paper);
        CreateBox("HQComputerScreenCursor", root, screenCenter + new Vector3(0.24f, -0.1f, -0.025f),
            new Vector3(0.07f, 0.07f, 0.02f), lightPanel);

        CreateBox("HQComputerNeck", root, new Vector3(p.x, 0.76f, 1.98f),
            new Vector3(0.14f, 0.24f, 0.12f), darkMetal);
        CreateBox("HQComputerBase", root, new Vector3(p.x, 0.63f, 1.88f),
            new Vector3(0.58f, 0.07f, 0.32f), darkMetal);
        CreateBox("HQComputerKeyboardDeck", root, new Vector3(p.x, 0.652f, 1.37f),
            new Vector3(0.78f, 0.05f, 0.24f), darkMetal);

        for (int i = 0; i < 4; i++)
            CreateBox($"HQComputerKeyRow_{i + 1}", root, new Vector3(p.x - 0.24f + i * 0.16f, 0.685f, 1.305f),
                new Vector3(0.09f, 0.016f, 0.055f), paper);

        CreateBox("HQComputerMousePad", root, new Vector3(p.x + 0.46f, 0.637f, 1.4f),
            new Vector3(0.24f, 0.022f, 0.2f), darkMetal);
        CreateBox("HQComputerMouse", root, new Vector3(p.x + 0.46f, 0.672f, 1.4f),
            new Vector3(0.12f, 0.045f, 0.15f), terminalGreen);
        CreateBox("HQComputerStatusLight", root, screenCenter + new Vector3(-0.44f, 0.23f, -0.08f),
            new Vector3(0.1f, 0.1f, 0.025f), lightPanel);

        var glowGo = new GameObject("HQComputerReadableScreenGlow");
        glowGo.transform.SetParent(root, false);
        glowGo.transform.position = screenCenter + new Vector3(0f, 0f, -0.25f);
        var glow = glowGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = DispatchGreen;
        glow.intensity = 0.82f;
        glow.range = 2.6f;
    }

    static void CreateOfficeGroundStoragePads(
        Transform root,
        Material terminalGreen,
        Material warningRed,
        Material paper,
        Material darkMetal,
        Material cardboard)
    {
        CreateBox("HQGroundStorageMat", root, new Vector3(2.42f, 0.082f, 0.72f),
            new Vector3(1.55f, 0.024f, 0.92f), MakeOfficeMaterial("Office_GroundStorageMat",
                DeadRubberSoft, DispatchGreenDark, OfficePattern.Warning));
        CreateBox("HQGroundStorageTapeFront", root, new Vector3(2.42f, 0.105f, 0.26f),
            new Vector3(1.48f, 0.018f, 0.055f), warningRed);
        CreateBox("HQGroundStorageTapeBack", root, new Vector3(2.42f, 0.105f, 1.18f),
            new Vector3(1.48f, 0.018f, 0.055f), terminalGreen);

        CreateGroundStoragePickup("HQStoredMedkitPickup", root, MvpHotbarItemId.Medkit,
            new Vector3(1.86f, 0.22f, 0.52f), new Vector3(0.28f, 0.2f, 0.22f), paper, warningRed);
        CreateGroundStoragePickup("HQStoredDecoyPickup", root, MvpHotbarItemId.Decoy,
            new Vector3(2.28f, 0.2f, 0.52f), new Vector3(0.22f, 0.18f, 0.22f), cardboard, terminalGreen);
        CreateGroundStoragePickup("HQStoredSprayPickup", root, MvpHotbarItemId.StunSpray,
            new Vector3(2.7f, 0.28f, 0.52f), new Vector3(0.11f, 0.24f, 0.11f), terminalGreen, darkMetal);
        CreateGroundStoragePickup("HQStoredFlashlightPickup", root, MvpHotbarItemId.Flashlight,
            new Vector3(2.42f, 0.2f, 0.94f), new Vector3(0.1f, 0.28f, 0.1f), darkMetal, paper)
            .transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    static GameObject CreateGroundStoragePickup(
        string name,
        Transform root,
        MvpHotbarItemId itemId,
        Vector3 position,
        Vector3 scale,
        Material material,
        Material accent)
    {
        GameObject pickup = itemId == MvpHotbarItemId.StunSpray || itemId == MvpHotbarItemId.Flashlight
            ? CreateCylinder(name, root, position, Quaternion.identity, scale, material)
            : CreateBox(name, root, position, scale, material);
        var collider = pickup.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(2.1f, 2.2f, 2.1f);
        collider.center = new Vector3(0f, 0.2f, 0f);
        var item = pickup.AddComponent<OfficeGroundItemPickup>();
        item.Configure(itemId);

        if (itemId == MvpHotbarItemId.Medkit)
        {
            CreateBox($"{name}_CrossH", pickup.transform, position + new Vector3(0f, 0.02f, -0.115f),
                new Vector3(0.18f, 0.035f, 0.02f), accent);
            CreateBox($"{name}_CrossV", pickup.transform, position + new Vector3(0f, 0.02f, -0.12f),
                new Vector3(0.035f, 0.14f, 0.02f), accent);
        }

        return pickup;
    }

    static void CreateCleanFallbackExteriorVan(
        Transform root,
        Material vanBody,
        Material vanGlass,
        Material rubber,
        Material darkMetal,
        Material terminalGreen,
        Material warningRed,
        Material paper,
        Material headlight)
    {
        Vector3 center = new Vector3(2.65f, 0.82f, -6.35f);
        CreateBox("FallbackExteriorVanBody", root, center,
            new Vector3(1.65f, 1.05f, 2.45f), vanBody);
        CreateBox("FallbackExteriorVanNose", root, center + new Vector3(0f, -0.18f, -1.24f),
            new Vector3(1.48f, 0.68f, 0.72f), vanBody);
        CreateBox("FallbackExteriorVanCabRoof", root, center + new Vector3(0f, 0.58f, -0.62f),
            new Vector3(1.45f, 0.32f, 0.85f), vanBody);
        CreateBox("FallbackExteriorVanWindshield", root, center + new Vector3(0f, 0.22f, -1.65f),
            new Vector3(1.12f, 0.38f, 0.04f), vanGlass);
        CreateBox("FallbackExteriorVanLeftWindow", root, center + new Vector3(-0.84f, 0.18f, -0.72f),
            new Vector3(0.04f, 0.36f, 0.62f), vanGlass);
        CreateBox("FallbackExteriorVanRightWindow", root, center + new Vector3(0.84f, 0.18f, -0.72f),
            new Vector3(0.04f, 0.36f, 0.62f), vanGlass);
        CreateBox("FallbackExteriorVanRearDoorA", root, center + new Vector3(-0.42f, 0.05f, 1.25f),
            new Vector3(0.04f, 0.78f, 0.58f), darkMetal);
        CreateBox("FallbackExteriorVanRearDoorB", root, center + new Vector3(0.42f, 0.05f, 1.25f),
            new Vector3(0.04f, 0.78f, 0.58f), darkMetal);
        CreateBox("FallbackExteriorVanBumper", root, center + new Vector3(0f, -0.47f, -1.67f),
            new Vector3(1.65f, 0.17f, 0.14f), darkMetal);
        CreateBox("FallbackExteriorVanPlate", root, center + new Vector3(0f, -0.3f, -1.75f),
            new Vector3(0.62f, 0.16f, 0.035f), paper);
        CreateBox("FallbackExteriorVanHeadlightL", root, center + new Vector3(-0.5f, -0.18f, -1.72f),
            new Vector3(0.28f, 0.16f, 0.04f), headlight);
        CreateBox("FallbackExteriorVanHeadlightR", root, center + new Vector3(0.5f, -0.18f, -1.72f),
            new Vector3(0.28f, 0.16f, 0.04f), headlight);
        CreatePointLight("FallbackExteriorVanHeadlightSpill", root, center + new Vector3(0f, -0.08f, -1.95f),
            SodiumAmberPale, 0.35f, 2.4f);

        CreateCylinder("FallbackExteriorVanWheelFL", root, center + new Vector3(-0.93f, -0.48f, -0.88f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);
        CreateCylinder("FallbackExteriorVanWheelFR", root, center + new Vector3(0.93f, -0.48f, -0.88f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);
        CreateCylinder("FallbackExteriorVanWheelBL", root, center + new Vector3(-0.93f, -0.48f, 0.78f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);
        CreateCylinder("FallbackExteriorVanWheelBR", root, center + new Vector3(0.93f, -0.48f, 0.78f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.18f, 0.34f), rubber);

        CreateBox("FallbackExteriorVanLogoTop", root, center + new Vector3(-0.85f, 0.28f, 0.12f),
            new Vector3(0.04f, 0.08f, 0.6f), terminalGreen);
        CreateBox("FallbackExteriorVanLogoLeft", root, center + new Vector3(-0.86f, 0.1f, 0.12f),
            new Vector3(0.04f, 0.42f, 0.08f), terminalGreen);
        CreateBox("FallbackExteriorVanLogoRight", root, center + new Vector3(-0.86f, 0.1f, 0.68f),
            new Vector3(0.04f, 0.42f, 0.08f), terminalGreen);
        GameObject slash = CreateBox("FallbackExteriorVanDebtSlash", root, center + new Vector3(-0.87f, 0.1f, 0.4f),
            new Vector3(0.045f, 0.52f, 0.08f), warningRed);
        slash.transform.rotation = Quaternion.Euler(0f, 0f, -25f);

        CreateBlockingCollider("FallbackExteriorVanSolidBodyCollider", root,
            center + new Vector3(0f, 0.03f, 0f), new Vector3(1.85f, 1.45f, 2.7f));
        CreateBlockingCollider("FallbackExteriorVanSolidNoseCollider", root,
            center + new Vector3(0f, -0.12f, -1.28f), new Vector3(1.55f, 0.85f, 0.72f));

        GameObject trigger = CreateInteractionTrigger("FallbackExteriorVanDepartureTrigger", root,
            center + new Vector3(1.35f, 0.1f, 0f), new Vector3(0.95f, 1.85f, 3.35f));
        trigger.AddComponent<OfficeDepartureVan>();
        CreateBox("FallbackExteriorVanBoardingPad", root, center + new Vector3(1.35f, -0.71f, 0f),
            new Vector3(0.95f, 0.025f, 3.1f), terminalGreen);
    }

    static void BuildSchoolStyle()
    {
        RemoveGeneratedWorldText();
        if (GameObject.Find(SchoolRootName) != null) return;

        var root = new GameObject(SchoolRootName);
        ApplySchoolAtmosphere();
        HideOriginalSchoolBlockoutProps();
        Material coldPaint = MakeMaterial(CivicTealShadow);
        Material warningRed = MakeMaterial(StampRed);
        Material paper = MakeMaterial(AgedPaper);
        Material lightColor = MakeMaterial(DirtyBone);
        Material dark = MakeMaterial(DeadRubber);
        Material exitGreen = MakeOfficeMaterial("School_DispatchExitGreen", DispatchGreenDark, DispatchGreen, OfficePattern.Scanline);
        Material vanBody = MakeOfficeMaterial("School_ReturnVanBody", CivicTeal, DirtyBone, OfficePattern.Scratched);
        Material vanGlass = MakeOfficeMaterial("School_ReturnVanGlass", OldGlass, DeadRubber, OfficePattern.Scanline);
        Material tire = MakeMaterial(DeadRubber);

        PrepareSchoolExteriorEntry(root.transform, coldPaint, warningRed, paper, dark, exitGreen);
        CalibrateSchoolMissionObjects(root.transform, warningRed, paper, exitGreen);
        CreateSchoolSafetyColliders(root.transform);

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

        CreateSchoolRouteComplexity(root.transform, coldPaint, warningRed, paper, dark);
        CreateExpandedSchoolPlayLayout(root.transform, coldPaint, warningRed, paper, dark, exitGreen);
        CreateSchoolReadableRoomDressing(root.transform, coldPaint, warningRed, paper, dark, exitGreen);
        CreateBonusEvidenceItem(root.transform, paper, warningRed);
        CreateWrongHomeworkDecoys(root.transform, paper, warningRed);
        CreateHidingLocker(root.transform, new Vector3(-4.75f, 0.95f, 4.7f), Quaternion.Euler(0f, 90f, 0f), coldPaint, warningRed);
        CreateHidingLocker(root.transform, new Vector3(4.75f, 0.95f, 4.7f), Quaternion.Euler(0f, -90f, 0f), coldPaint, warningRed);
        CreateHidingLocker(root.transform, new Vector3(-10.55f, 0.95f, 2.1f), Quaternion.identity, coldPaint, warningRed);
        CreateHidingLocker(root.transform, new Vector3(10.55f, 0.95f, 6.55f), Quaternion.Euler(0f, 180f, 0f), coldPaint, warningRed);
        RefreshSchoolMonsterPatrolRoute(root.transform);
        CreateSchoolExtractionVan(root.transform, vanBody, vanGlass, tire, dark, exitGreen, warningRed, paper);

        var lightGo = new GameObject("SchoolColdDebtLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.position = new Vector3(0f, 2.8f, 1f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.58f, 0.74f, 0.68f);
        light.intensity = 0.72f;
        light.range = 6.5f;

        var timeDirector = new GameObject("SchoolTimeOfDayDirector");
        timeDirector.transform.SetParent(root.transform, false);
        timeDirector.AddComponent<MissionTimeOfDayDirector>();
    }

    static void PrepareSchoolExteriorEntry(
        Transform root,
        Material coldPaint,
        Material warningRed,
        Material paper,
        Material dark,
        Material exitGreen)
    {
        GameObject southWall = GameObject.Find("Wall_South");
        if (southWall != null)
            southWall.SetActive(false);
        DisableObjectIfPresent("Wall_South_LeftOfEntrance");
        DisableObjectIfPresent("Wall_South_RightOfEntrance");
        DisableObjectIfPresent("Wall_South_EntranceHeader");

        GameObject spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn != null)
            spawn.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, -11.45f), Quaternion.identity);

        foreach (var exit in Object.FindObjectsByType<SchoolExitPoint>(FindObjectsSortMode.None))
        {
            if (exit != null)
                exit.transform.position = new Vector3(0f, 0.08f, -12.85f);
        }

        CreateBox("SchoolExteriorForecourt", root, new Vector3(0f, -0.045f, -12.4f),
            new Vector3(10.5f, 0.06f, 7.6f), MakeMaterial(DeadRubberSoft));
        CreateWalkableCollider("SchoolExteriorForecourtCollider", root,
            new Vector3(0f, -0.05f, -12.4f), new Vector3(10.65f, 0.26f, 7.7f));
        CreateBlockingCollider("SchoolExteriorBackFenceCollider", root,
            new Vector3(0f, 0.9f, -16.2f), new Vector3(10.7f, 1.8f, 0.3f));
        CreateBlockingCollider("SchoolExteriorLeftFenceCollider", root,
            new Vector3(-5.35f, 0.9f, -12.4f), new Vector3(0.3f, 1.8f, 7.7f));
        CreateBlockingCollider("SchoolExteriorRightFenceCollider", root,
            new Vector3(5.35f, 0.9f, -12.4f), new Vector3(0.3f, 1.8f, 7.7f));

        CreateSchoolObstacle("SchoolSouthWall_LeftOfDoor", root, new Vector3(-7.45f, 1.55f, -9f),
            new Vector3(9.1f, 3.1f, 0.28f), coldPaint);
        CreateSchoolObstacle("SchoolSouthWall_RightOfDoor", root, new Vector3(7.45f, 1.55f, -9f),
            new Vector3(9.1f, 3.1f, 0.28f), coldPaint);
        CreateSchoolObstacle("SchoolSouthWall_DoorHeader", root, new Vector3(0f, 2.75f, -9f),
            new Vector3(3.7f, 0.75f, 0.28f), coldPaint);

        CreateBox("SchoolEntranceDoorFrameLeft", root, new Vector3(-1.1f, 1.28f, -9.18f),
            new Vector3(0.18f, 2.55f, 0.2f), dark);
        CreateBox("SchoolEntranceDoorFrameRight", root, new Vector3(1.1f, 1.28f, -9.18f),
            new Vector3(0.18f, 2.55f, 0.2f), dark);
        CreateBox("SchoolEntranceAwning", root, new Vector3(0f, 2.55f, -9.75f),
            new Vector3(3.2f, 0.16f, 0.88f), dark);
        CreateBox("SchoolEntranceSignPanel", root, new Vector3(0f, 2.13f, -9.32f),
            new Vector3(2.35f, 0.34f, 0.035f), warningRed);
        CreateBox("SchoolEntranceSignStripe", root, new Vector3(0f, 2.13f, -9.36f),
            new Vector3(1.65f, 0.055f, 0.02f), paper);

        GameObject door = CreateBox("SchoolEntranceDoor", root, new Vector3(0f, 1.18f, -9.1f),
            new Vector3(1.7f, 2.25f, 0.12f), dark);
        var doorCollider = door.AddComponent<BoxCollider>();
        doorCollider.size = Vector3.one;
        GameObject handle = CreateBox("SchoolEntranceDoorHandle", root, new Vector3(0.62f, 1.12f, -9.19f),
            new Vector3(0.12f, 0.12f, 0.055f), exitGreen);
        handle.transform.SetParent(door.transform, true);
        door.AddComponent<SchoolEntranceDoor>();

        CreateBox("SchoolEntranceSafetyPaint", root, new Vector3(0f, 0.025f, -10.05f),
            new Vector3(2.65f, 0.024f, 0.42f), exitGreen);
        CreateBox("SchoolExteriorDispatchArrowStem", root, new Vector3(0f, 0.03f, -11.2f),
            new Vector3(0.18f, 0.02f, 1.85f), exitGreen);
        GameObject arrowLeft = CreateBox("SchoolExteriorDispatchArrowLeft", root, new Vector3(-0.32f, 0.04f, -10.35f),
            new Vector3(0.68f, 0.02f, 0.12f), exitGreen);
        arrowLeft.transform.rotation = Quaternion.Euler(0f, -34f, 0f);
        GameObject arrowRight = CreateBox("SchoolExteriorDispatchArrowRight", root, new Vector3(0.32f, 0.04f, -10.35f),
            new Vector3(0.68f, 0.02f, 0.12f), exitGreen);
        arrowRight.transform.rotation = Quaternion.Euler(0f, 34f, 0f);

        CreatePointLight("SchoolExteriorDoorLamp", root, new Vector3(0f, 2.25f, -9.85f),
            SodiumAmberPale, 1.3f, 5.2f);
        CreatePointLight("SchoolExteriorVanSafetyLamp", root, new Vector3(-1.25f, 1.35f, -12.85f),
            DispatchGreen, 0.62f, 4.2f);
    }

    static void DisableObjectIfPresent(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            go.SetActive(false);
    }

    static void CreateSchoolRouteComplexity(Transform root, Material coldPaint, Material warningRed, Material paper, Material dark)
    {
        CreateSchoolObstacle("AdminRecords_LeftWall", root, new Vector3(-8.45f, 0.92f, 1.15f),
            new Vector3(0.22f, 1.84f, 4.4f), coldPaint);
        CreateSchoolObstacle("AdminRecords_BackWall", root, new Vector3(-7.0f, 0.92f, 3.2f),
            new Vector3(2.75f, 1.84f, 0.22f), coldPaint);
        CreateSchoolObstacle("AdminRecords_Counter", root, new Vector3(-7.85f, 0.55f, -0.45f),
            new Vector3(1.1f, 1.1f, 0.38f), dark);

        CreateSchoolObstacle("OverdueShelf_A", root, new Vector3(7.2f, 0.82f, -3.3f),
            new Vector3(3.1f, 1.64f, 0.34f), dark);
        CreateSchoolObstacle("OverdueShelf_B", root, new Vector3(9.15f, 0.82f, -1.25f),
            new Vector3(0.34f, 1.64f, 3.0f), dark);
        CreateSchoolObstacle("TippedDesk_Blocker", root, new Vector3(1.4f, 0.42f, -1.9f),
            new Vector3(2.2f, 0.55f, 0.62f), coldPaint).transform.rotation = Quaternion.Euler(0f, 19f, 0f);

        CreateBox("AdminRecords_Sign", root, new Vector3(-7f, 1.62f, -0.68f),
            new Vector3(1.35f, 0.28f, 0.035f), warningRed);
        CreateBox("OverdueShelf_PaperStack_A", root, new Vector3(7.2f, 1.68f, -3.3f),
            new Vector3(2.4f, 0.12f, 0.22f), paper);
        CreateBox("OverdueShelf_PaperStack_B", root, new Vector3(9.15f, 1.68f, -1.25f),
            new Vector3(0.22f, 0.12f, 2.2f), paper);

        CreateSchoolObstacle("HallQueueRail_Left", root, new Vector3(-2.35f, 0.55f, -6.25f),
            new Vector3(0.16f, 1.1f, 2.25f), dark);
        CreateSchoolObstacle("HallQueueRail_Right", root, new Vector3(2.35f, 0.55f, -6.25f),
            new Vector3(0.16f, 1.1f, 2.25f), dark);
        CreateSchoolObstacle("HallQueueRail_CenterA", root, new Vector3(-0.55f, 0.45f, -5.35f),
            new Vector3(0.14f, 0.9f, 1.45f), coldPaint);
        CreateSchoolObstacle("HallQueueRail_CenterB", root, new Vector3(0.55f, 0.45f, -6.95f),
            new Vector3(0.14f, 0.9f, 1.45f), coldPaint);

        CreateSchoolObstacle("MainHall_JumpableBench", root, new Vector3(0.1f, 0.28f, -4.35f),
            new Vector3(2.35f, 0.46f, 0.32f), coldPaint);
        CreateBox("MainHall_BenchWarningStripe", root, new Vector3(0.1f, 0.525f, -4.35f),
            new Vector3(2.05f, 0.025f, 0.2f), warningRed);

        CreateSchoolObstacle("CrouchShortcut_LeftStack", root, new Vector3(3.15f, 0.86f, -2.35f),
            new Vector3(0.26f, 1.72f, 2.25f), dark);
        CreateSchoolObstacle("CrouchShortcut_RightStack", root, new Vector3(5.25f, 0.86f, -2.35f),
            new Vector3(0.26f, 1.72f, 2.25f), dark);
        CreateSchoolObstacle("CrouchShortcut_LowPipe", root, new Vector3(4.2f, 1.65f, -2.35f),
            new Vector3(1.86f, 0.7f, 1.8f), coldPaint);
        CreateBox("CrouchShortcut_GreenFloorGuide", root, new Vector3(4.2f, 0.035f, -2.35f),
            new Vector3(1.35f, 0.02f, 1.5f), MakeEmissiveMaterial(DispatchGreenDark, DispatchGreen, 0.18f));

        CreateSchoolObstacle("CafeteriaCart_Blocker", root, new Vector3(-4.2f, 0.48f, -2.85f),
            new Vector3(1.35f, 0.72f, 0.55f), coldPaint).transform.rotation = Quaternion.Euler(0f, -17f, 0f);
        CreateSchoolObstacle("FallenNoticeBoard_Blocker", root, new Vector3(-2.9f, 0.62f, -0.55f),
            new Vector3(1.85f, 0.28f, 0.95f), dark).transform.rotation = Quaternion.Euler(0f, 24f, 10f);

        CreateSchoolObstacle("ClassroomTeacherDesk", root, new Vector3(0f, 0.55f, 2.02f),
            new Vector3(2.25f, 0.72f, 0.48f), dark);
        CreateSchoolObstacle("ClassroomLeftPile", root, new Vector3(-4.4f, 0.46f, 3.5f),
            new Vector3(0.82f, 0.82f, 1.35f), coldPaint);
        CreateSchoolObstacle("ClassroomRightPile", root, new Vector3(4.72f, 0.46f, 3.42f),
            new Vector3(0.74f, 0.82f, 1.25f), coldPaint);

        CreateBox("TargetDeskDebtCircle_A", root, new Vector3(SchoolNotebookPosition.x, 0.075f, SchoolNotebookPosition.z),
            new Vector3(1.25f, 0.02f, 0.1f), warningRed);
        CreateBox("TargetDeskDebtCircle_B", root, new Vector3(SchoolNotebookPosition.x, 0.078f, SchoolNotebookPosition.z),
            new Vector3(0.1f, 0.02f, 1.0f), warningRed);
        CreatePointLight("NotebookWeakGoldLight", root, SchoolNotebookPosition + new Vector3(0f, 0.42f, 0f),
            SodiumAmberPale, 0.75f, 3.2f);

        for (int i = 0; i < 5; i++)
        {
            float z = -7.25f + i * 1.15f;
            CreateBox($"HallDebtArrowStep_{i + 1}", root, new Vector3(-1.2f + (i % 2) * 2.4f, 0.04f, z),
                new Vector3(0.72f, 0.02f, 0.12f), i % 2 == 0 ? warningRed : paper);
        }
    }

    static void CreateExpandedSchoolPlayLayout(
        Transform root,
        Material coldPaint,
        Material warningRed,
        Material paper,
        Material dark,
        Material exitGreen)
    {
        Material floorGuide = MakeEmissiveMaterial(DispatchGreenDark, DispatchGreen, 0.12f);
        Material deadRubberSoft = MakeMaterial(DeadRubberSoft);

        CreateSchoolObstacle("WestRecordsShelfMaze_North", root, new Vector3(-10.1f, 0.78f, 2.6f),
            new Vector3(2.3f, 1.56f, 0.32f), dark);
        CreateSchoolObstacle("WestRecordsShelfMaze_South", root, new Vector3(-10.1f, 0.78f, -1.85f),
            new Vector3(2.3f, 1.56f, 0.32f), dark);
        CreateSchoolObstacle("WestRecordsShelfMaze_Side", root, new Vector3(-9.05f, 0.78f, 0.32f),
            new Vector3(0.32f, 1.56f, 2.6f), dark);
        CreateSchoolObstacle("WestRecordsServiceCounter", root, new Vector3(-7.55f, 0.52f, 1.05f),
            new Vector3(1.35f, 0.64f, 0.42f), coldPaint);
        CreateBox("WestRecordsLedgerGreenRoute", root, new Vector3(-7.55f, 0.045f, -0.35f),
            new Vector3(0.22f, 0.02f, 2.5f), floorGuide);
        CreateBox("WestRecordsStampRedWarning", root, new Vector3(-9.35f, 1.63f, 2.42f),
            new Vector3(0.72f, 0.22f, 0.035f), warningRed);
        for (int i = 0; i < 7; i++)
        {
            CreateBox($"WestRecordsLooseForm_{i + 1}", root,
                new Vector3(-10.9f + i * 0.38f, 0.055f, -0.95f + (i % 2) * 0.34f),
                new Vector3(0.24f, 0.018f, 0.16f), paper);
        }

        CreateSchoolObstacle("EastLostPropertyShelf_A", root, new Vector3(7.35f, 0.8f, -5.85f),
            new Vector3(3.15f, 1.6f, 0.34f), dark);
        CreateSchoolObstacle("EastLostPropertyShelf_B", root, new Vector3(10.1f, 0.8f, -4.05f),
            new Vector3(0.34f, 1.6f, 2.9f), dark);
        CreateSchoolObstacle("EastLostPropertyCart", root, new Vector3(8.05f, 0.42f, -1.35f),
            new Vector3(1.25f, 0.58f, 0.54f), coldPaint).transform.rotation = Quaternion.Euler(0f, -18f, 0f);
        CreateBox("LostPropertyReturnArrow", root, new Vector3(7.15f, 0.045f, -2.55f),
            new Vector3(0.82f, 0.02f, 0.12f), floorGuide).transform.rotation = Quaternion.Euler(0f, -34f, 0f);

        CreateSchoolObstacle("DebtOfficeLeftPartition", root, new Vector3(6.25f, 1.12f, 5.45f),
            new Vector3(0.28f, 2.24f, 3.8f), coldPaint);
        CreateSchoolObstacle("DebtOfficeBackPartition", root, new Vector3(8.7f, 1.12f, 7.5f),
            new Vector3(4.75f, 2.24f, 0.28f), coldPaint);
        CreateSchoolObstacle("DebtOfficeQueueCounter_A", root, new Vector3(8.15f, 0.54f, 4.25f),
            new Vector3(1.8f, 0.72f, 0.42f), dark);
        CreateSchoolObstacle("DebtOfficeQueueCounter_B", root, new Vector3(10.2f, 0.54f, 5.8f),
            new Vector3(0.42f, 0.72f, 2.0f), dark);
        CreateSchoolObstacle("DebtOfficePrincipalDesk", root, new Vector3(SchoolNotebookPosition.x, 0.55f, SchoolNotebookPosition.z),
            new Vector3(1.5f, 0.72f, 0.72f), dark);
        CreateBox("DebtOfficeHomeworkBanner", root, new Vector3(8.65f, 2.12f, 7.34f),
            new Vector3(2.25f, 0.32f, 0.04f), warningRed);
        CreateBox("DebtOfficeApprovedReturnMarker", root, new Vector3(9.55f, 0.045f, 4.62f),
            new Vector3(0.78f, 0.02f, 0.12f), exitGreen).transform.rotation = Quaternion.Euler(0f, 28f, 0f);
        CreateBox("DebtOfficeBannerPaperLine", root, new Vector3(8.65f, 2.12f, 7.3f),
            new Vector3(1.55f, 0.055f, 0.025f), paper);
        for (int i = 0; i < 9; i++)
        {
            CreateBox($"DebtOfficeWallNotice_{i + 1}", root,
                new Vector3(7.18f + (i % 3) * 0.5f, 1.42f + (i / 3) * 0.25f, 7.31f),
                new Vector3(0.32f, 0.18f, 0.025f), i % 4 == 0 ? warningRed : paper);
        }

        CreateSchoolObstacle("CrouchReturnShortcut_LeftStack", root, new Vector3(-4.9f, 0.86f, -6.35f),
            new Vector3(0.28f, 1.72f, 2.35f), dark);
        CreateSchoolObstacle("CrouchReturnShortcut_RightStack", root, new Vector3(-2.8f, 0.86f, -6.35f),
            new Vector3(0.28f, 1.72f, 2.35f), dark);
        CreateSchoolObstacle("CrouchReturnShortcut_LowFormsPipe", root, new Vector3(-3.85f, 1.55f, -6.35f),
            new Vector3(1.86f, 0.66f, 1.9f), coldPaint);
        CreateBox("CrouchReturnShortcutRouteMark", root, new Vector3(-3.85f, 0.04f, -6.35f),
            new Vector3(1.28f, 0.02f, 1.45f), floorGuide);

        CreateSchoolObstacle("JumpShortcut_TippedBench", root, new Vector3(2.95f, 0.24f, -6.8f),
            new Vector3(1.65f, 0.38f, 0.34f), coldPaint).transform.rotation = Quaternion.Euler(0f, 11f, 0f);
        CreateBox("JumpShortcut_BenchRedLip", root, new Vector3(2.95f, 0.455f, -6.8f),
            new Vector3(1.38f, 0.025f, 0.16f), warningRed);
        CreateSchoolObstacle("MainSpineSplitNoticeRack", root, new Vector3(0f, 0.7f, -0.15f),
            new Vector3(0.4f, 1.4f, 1.85f), deadRubberSoft);
        CreateBox("MainSpineSplitRouteLeft", root, new Vector3(-0.62f, 0.04f, -0.15f),
            new Vector3(0.68f, 0.02f, 0.12f), floorGuide).transform.rotation = Quaternion.Euler(0f, -32f, 0f);
        CreateBox("MainSpineSplitRouteRight", root, new Vector3(0.62f, 0.04f, -0.15f),
            new Vector3(0.68f, 0.02f, 0.12f), floorGuide).transform.rotation = Quaternion.Euler(0f, 32f, 0f);

        CreatePointLight("WestRecordsColdTaskLight", root, new Vector3(-8.8f, 2.25f, 1.25f),
            new Color(0.58f, 0.74f, 0.68f), 0.55f, 4.4f);
        CreatePointLight("DebtOfficeStampRedThreatLight", root, new Vector3(8.85f, 2.1f, 5.6f),
            StampRed, 0.48f, 4.2f);
        CreatePointLight("LostPropertyShelfDimLight", root, new Vector3(8.65f, 2.0f, -4.35f),
            SodiumAmberPale, 0.42f, 3.8f);
    }

    static void CreateSchoolReadableRoomDressing(
        Transform root,
        Material coldPaint,
        Material warningRed,
        Material paper,
        Material dark,
        Material exitGreen)
    {
        Material softShadow = MakeMaterial(DeadRubberSoft);
        Material shelfEdge = MakeOfficeMaterial("School_ShelfEdgeRubber", DeadRubber, CivicTealShadow, OfficePattern.Scratched);

        CreateRoomDoorMarker(root, "Records", new Vector3(-5.95f, 1.45f, 0.8f), Quaternion.Euler(0f, 90f, 0f),
            "RECORDS", warningRed, paper, dark);
        CreateRoomDoorMarker(root, "LostProperty", new Vector3(5.95f, 1.45f, -3.95f), Quaternion.Euler(0f, -90f, 0f),
            "LOST\nPROPERTY", exitGreen, paper, dark);
        CreateRoomDoorMarker(root, "DebtOffice", new Vector3(6.05f, 1.45f, 4.0f), Quaternion.Euler(0f, -90f, 0f),
            "HOMEWORK\nDEBT", warningRed, paper, dark);

        AddShelfRun(root, "WestRecordsNorth", new Vector3(-10.1f, 1.07f, 2.6f), true, 2.3f, dark, shelfEdge, paper, warningRed);
        AddShelfRun(root, "WestRecordsSouth", new Vector3(-10.1f, 1.07f, -1.85f), true, 2.3f, dark, shelfEdge, paper, warningRed);
        AddShelfRun(root, "WestRecordsSide", new Vector3(-9.05f, 1.07f, 0.32f), false, 2.6f, dark, shelfEdge, paper, warningRed);
        AddShelfRun(root, "EastLostPropertyA", new Vector3(7.35f, 1.08f, -5.85f), true, 3.15f, dark, shelfEdge, paper, exitGreen);
        AddShelfRun(root, "EastLostPropertyB", new Vector3(10.1f, 1.08f, -4.05f), false, 2.9f, dark, shelfEdge, paper, exitGreen);

        CreateBox("CafeteriaCart_Handle", root, new Vector3(-4.95f, 0.86f, -2.78f),
            new Vector3(0.08f, 0.62f, 0.72f), dark).transform.rotation = Quaternion.Euler(0f, -17f, 0f);
        CreateBox("CafeteriaCart_ServiceTray", root, new Vector3(-4.2f, 0.89f, -2.85f),
            new Vector3(1.18f, 0.08f, 0.47f), paper).transform.rotation = Quaternion.Euler(0f, -17f, 0f);
        CreateCylinder("CafeteriaCart_WheelA", root, new Vector3(-4.82f, 0.2f, -3.12f),
            Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 0.045f, 0.12f), softShadow);
        CreateCylinder("CafeteriaCart_WheelB", root, new Vector3(-3.58f, 0.2f, -2.62f),
            Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 0.045f, 0.12f), softShadow);

        CreateBox("FallenNoticeBoard_PaperFace", root, new Vector3(-2.9f, 0.78f, -0.55f),
            new Vector3(1.55f, 0.035f, 0.72f), paper).transform.rotation = Quaternion.Euler(0f, 24f, 10f);
        CreateBox("FallenNoticeBoard_Stamp", root, new Vector3(-2.45f, 0.86f, -0.38f),
            new Vector3(0.42f, 0.04f, 0.16f), warningRed).transform.rotation = Quaternion.Euler(0f, 24f, 10f);
        CreateBox("FallenNoticeBoard_Kickstand", root, new Vector3(-3.38f, 0.38f, -0.95f),
            new Vector3(0.08f, 0.82f, 0.08f), dark).transform.rotation = Quaternion.Euler(22f, 24f, -16f);

        CreateBox("DebtOfficeDeskGreenLampBase", root, SchoolNotebookPosition + new Vector3(-0.48f, 0.12f, -0.18f),
            new Vector3(0.22f, 0.08f, 0.18f), dark);
        CreateBox("DebtOfficeDeskGreenLampShade", root, SchoolNotebookPosition + new Vector3(-0.48f, 0.36f, -0.18f),
            new Vector3(0.38f, 0.12f, 0.24f), exitGreen);
        CreatePointLight("DebtOfficeDeskLampGlow", root, SchoolNotebookPosition + new Vector3(-0.48f, 0.58f, -0.18f),
            DispatchGreen, 0.52f, 2.6f);
        for (int i = 0; i < 6; i++)
        {
            CreateBox($"DebtOfficeDeskPaperStack_{i + 1}", root,
                SchoolNotebookPosition + new Vector3(-0.18f + i * 0.11f, 0.09f + i * 0.012f, 0.22f + (i % 2) * 0.08f),
                new Vector3(0.28f, 0.016f, 0.18f), i == 2 ? warningRed : paper)
                .transform.rotation = Quaternion.Euler(0f, -7f + i * 5f, 0f);
        }

        CreateBox("MainHallLowRiskRoutePlaque", root, new Vector3(-1.58f, 0.055f, -0.85f),
            new Vector3(0.8f, 0.022f, 0.12f), exitGreen).transform.rotation = Quaternion.Euler(0f, -34f, 0f);
        CreateBox("MainHallHighRiskRoutePlaque", root, new Vector3(1.58f, 0.055f, -0.85f),
            new Vector3(0.8f, 0.022f, 0.12f), warningRed).transform.rotation = Quaternion.Euler(0f, 34f, 0f);
    }

    static void CreateRoomDoorMarker(
        Transform root,
        string id,
        Vector3 position,
        Quaternion rotation,
        string label,
        Material sign,
        Material paper,
        Material dark)
    {
        GameObject frame = CreateBox($"School{id}DoorFrame", root, position,
            new Vector3(0.12f, 1.85f, 1.62f), dark);
        frame.transform.rotation = rotation;
        GameObject signPanel = CreateBox($"School{id}SignPanel", root, position + Vector3.up * 0.72f,
            new Vector3(0.035f, 0.34f, 1.05f), sign);
        signPanel.transform.rotation = rotation;
        CreateText(label, root, position + Vector3.up * 0.73f + rotation * new Vector3(-0.035f, 0f, 0f),
            rotation * Quaternion.Euler(0f, -90f, 0f), 0.105f, AgedPaper);
        CreateBox($"School{id}PaperNotice", root, position + Vector3.down * 0.08f,
            new Vector3(0.035f, 0.36f, 0.28f), paper).transform.rotation = rotation;
    }

    static void AddShelfRun(
        Transform root,
        string id,
        Vector3 center,
        bool alongX,
        float length,
        Material body,
        Material edge,
        Material paper,
        Material accent)
    {
        for (int level = 0; level < 3; level++)
        {
            Vector3 levelOffset = Vector3.up * (-0.48f + level * 0.42f);
            Vector3 shelfScale = alongX
                ? new Vector3(length * 0.92f, 0.055f, 0.36f)
                : new Vector3(0.36f, 0.055f, length * 0.92f);
            CreateBox($"{id}_ShelfBoard_{level + 1}", root, center + levelOffset, shelfScale, edge);
        }

        int boxCount = Mathf.Max(3, Mathf.RoundToInt(length * 2f));
        for (int i = 0; i < boxCount; i++)
        {
            float t = boxCount == 1 ? 0f : Mathf.Lerp(-0.42f, 0.42f, i / (float)(boxCount - 1));
            Vector3 axisOffset = alongX ? new Vector3(t * length, 0f, 0f) : new Vector3(0f, 0f, t * length);
            Vector3 boxSize = alongX
                ? new Vector3(0.26f, 0.18f + (i % 2) * 0.08f, 0.18f)
                : new Vector3(0.18f, 0.18f + (i % 2) * 0.08f, 0.26f);
            CreateBox($"{id}_LabeledBox_{i + 1}", root, center + axisOffset + Vector3.up * (-0.22f + (i % 3) * 0.42f),
                boxSize, i % 4 == 0 ? accent : paper);
        }

        CreateBox($"{id}_BackShadow", root, center + Vector3.up * 0.05f,
            alongX ? new Vector3(length, 1.18f, 0.035f) : new Vector3(0.035f, 1.18f, length), body);
    }

    static void RefreshSchoolMonsterPatrolRoute(Transform root)
    {
        Vector3[] positions =
        {
            new Vector3(8.7f, 0.08f, 5.6f),
            new Vector3(8.8f, 0.08f, -4.65f),
            new Vector3(0.1f, 0.08f, -6.9f),
            new Vector3(-8.9f, 0.08f, -1.6f),
            new Vector3(-7.6f, 0.08f, 2.1f),
            new Vector3(2.8f, 0.08f, 4.9f)
        };

        Transform[] points = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            var point = new GameObject($"RuntimeMonsterPatrolPoint_{i + 1}");
            point.transform.SetParent(root, false);
            point.transform.position = positions[i];
            points[i] = point.transform;
        }

        foreach (var monster in Object.FindObjectsByType<SchoolMonsterAI>(FindObjectsSortMode.None))
        {
            if (monster != null)
                monster.OverridePatrolPoints(points);
        }
    }

    static void CalibrateSchoolMissionObjects(Transform root, Material warningRed, Material paper, Material exitGreen)
    {
        MoveObjectIfPresent("PlayerSpawnPoint", new Vector3(0f, 0.1f, -11.45f), Quaternion.identity);
        MoveObjectIfPresent("LostHomeworkNotebook", SchoolNotebookPosition, Quaternion.Euler(0f, -7f, 0f));
        MoveObjectIfPresent("OverdueLedgerEvidence", SchoolLedgerPosition, Quaternion.Euler(0f, 12f, 0f));

        foreach (var exit in Object.FindObjectsByType<SchoolExitPoint>(FindObjectsSortMode.None))
        {
            if (exit == null) continue;
            exit.transform.position = new Vector3(0f, 0.08f, -12.85f);
            if (exit.TryGetComponent<BoxCollider>(out var collider))
            {
                collider.isTrigger = true;
                SetBoxColliderWorldCenter(collider, new Vector3(0f, 0.78f, -12.85f));
                SetBoxColliderWorldSize(collider, new Vector3(4.6f, 1.35f, 1.35f));
            }
        }

        CreateBox("SchoolVanReturnPadOutline_A", root, new Vector3(0f, 0.035f, -12.2f),
            new Vector3(4.6f, 0.02f, 0.1f), exitGreen);
        CreateBox("SchoolVanReturnPadOutline_B", root, new Vector3(0f, 0.038f, -13.5f),
            new Vector3(4.6f, 0.02f, 0.1f), exitGreen);
        CreateBox("SchoolVanReturnPadOutline_L", root, new Vector3(-2.25f, 0.041f, -12.85f),
            new Vector3(0.1f, 0.02f, 1.4f), exitGreen);
        CreateBox("SchoolVanReturnPadOutline_R", root, new Vector3(2.25f, 0.041f, -12.85f),
            new Vector3(0.1f, 0.02f, 1.4f), exitGreen);
        CreateBox("SchoolLedgerTablePatch", root, SchoolLedgerPosition + new Vector3(0f, -0.05f, 0f),
            new Vector3(0.92f, 0.055f, 0.62f), paper);
        CreateBox("SchoolLedgerWarningTab", root, SchoolLedgerPosition + new Vector3(0.35f, 0.02f, -0.3f),
            new Vector3(0.28f, 0.025f, 0.12f), warningRed);
    }

    static void MoveObjectIfPresent(string name, Vector3 position, Quaternion rotation)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return;
        go.transform.SetPositionAndRotation(position, rotation);
    }

    static void HideOriginalSchoolBlockoutProps()
    {
        string[] names =
        {
            "SchoolEntranceDoor",
            "SchoolEntranceSign",
            "AdminRecords_LeftWall",
            "AdminRecords_BackWall",
            "AdminRecords_Counter",
            "OverdueShelf_A",
            "OverdueShelf_B"
        };

        foreach (string name in names)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
                go.SetActive(false);
        }
    }

    static void CreateSchoolSafetyColliders(Transform root)
    {
        CreateWalkableCollider("SchoolMainHallSafetyFloorCollider", root,
            new Vector3(0f, -0.08f, 0f), new Vector3(24.6f, 0.18f, 18.6f));
        CreateBlockingCollider("SchoolNorthSafetyWallCollider", root,
            new Vector3(0f, 1.55f, 9.18f), new Vector3(24.6f, 3.1f, 0.36f));
        CreateBlockingCollider("SchoolWestSafetyWallCollider", root,
            new Vector3(-12.18f, 1.55f, 0f), new Vector3(0.36f, 3.1f, 18.6f));
        CreateBlockingCollider("SchoolEastSafetyWallCollider", root,
            new Vector3(12.18f, 1.55f, 0f), new Vector3(0.36f, 3.1f, 18.6f));
        CreateBlockingCollider("SchoolEntranceLeftSafetyWallCollider", root,
            new Vector3(-7.45f, 1.55f, -9.18f), new Vector3(9.1f, 3.1f, 0.36f));
        CreateBlockingCollider("SchoolEntranceRightSafetyWallCollider", root,
            new Vector3(7.45f, 1.55f, -9.18f), new Vector3(9.1f, 3.1f, 0.36f));
        CreateBlockingCollider("SchoolExteriorFarSafetyFenceCollider", root,
            new Vector3(0f, 1f, -16.35f), new Vector3(11.1f, 2f, 0.42f));
        CreateBlockingCollider("SchoolExteriorLeftSafetyFenceCollider", root,
            new Vector3(-5.55f, 1f, -12.45f), new Vector3(0.42f, 2f, 8f));
        CreateBlockingCollider("SchoolExteriorRightSafetyFenceCollider", root,
            new Vector3(5.55f, 1f, -12.45f), new Vector3(0.42f, 2f, 8f));
    }

    static GameObject CreateSchoolObstacle(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = CreateBox(name, parent, position, scale, material);
        var collider = go.AddComponent<BoxCollider>();
        collider.size = Vector3.one;
        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.size = Vector3.one;
        return go;
    }

    static void CreateBonusEvidenceItem(Transform root, Material paper, Material warningRed)
    {
        if (GameObject.Find("OverdueLedgerEvidence") != null) return;

        GameObject ledger = CreateBox("OverdueLedgerEvidence", root, new Vector3(-7.55f, 0.96f, 1.38f),
            new Vector3(0.7f, 0.08f, 0.48f), paper);
        var collider = ledger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(2.6f, 16f, 3f);
        collider.center = new Vector3(0f, 7f, 0f);
        ledger.AddComponent<SchoolBonusEvidenceItem>();
        CreateBox("OverdueLedgerStamp", ledger.transform, ledger.transform.position + new Vector3(0.16f, 0.07f, -0.05f),
            new Vector3(0.24f, 0.025f, 0.16f), warningRed);
    }

    static void CreateWrongHomeworkDecoys(Transform root, Material paper, Material warningRed)
    {
        if (GameObject.Find("WrongHomeworkDecoy_LostProperty") != null) return;

        Vector3[] positions =
        {
            new Vector3(8.42f, 0.92f, -3.72f),
            new Vector3(-3.85f, 0.84f, 3.62f),
            new Vector3(1.2f, 0.62f, -1.92f)
        };
        Quaternion[] rotations =
        {
            Quaternion.Euler(0f, -18f, 0f),
            Quaternion.Euler(0f, 22f, 0f),
            Quaternion.Euler(0f, 51f, 0f)
        };
        string[] names =
        {
            "WrongHomeworkDecoy_LostProperty",
            "WrongHomeworkDecoy_ClassroomPile",
            "WrongHomeworkDecoy_MainHallForms"
        };

        for (int i = 0; i < positions.Length; i++)
            CreateWrongHomeworkDecoy(names[i], root, positions[i], rotations[i], paper, warningRed);
    }

    static void CreateWrongHomeworkDecoy(
        string name,
        Transform root,
        Vector3 position,
        Quaternion rotation,
        Material paper,
        Material warningRed)
    {
        var decoy = new GameObject(name);
        decoy.transform.SetParent(root, false);
        decoy.transform.SetPositionAndRotation(position, rotation);

        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_MissingHomeworkNotebook");
        if (prefab != null)
        {
            GameObject visual = Object.Instantiate(prefab, decoy.transform);
            visual.name = $"{name}_ASV4Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 0.72f;
            foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>())
                childCollider.enabled = false;
        }
        else
        {
            CreateBox($"{name}_Paper", decoy.transform, position, new Vector3(0.55f, 0.06f, 0.36f), paper);
            CreateBox($"{name}_RedStamp", decoy.transform, position + new Vector3(0.14f, 0.045f, -0.08f),
                new Vector3(0.18f, 0.025f, 0.12f), warningRed);
        }

        var collider = decoy.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(1.7f, 1.25f, 1.45f);
        collider.center = new Vector3(0f, 0.48f, 0f);
        decoy.AddComponent<WrongHomeworkItem>();

        CreateBox($"{name}_LooseRedTab", decoy.transform, position + rotation * new Vector3(0.24f, 0.045f, -0.12f),
            new Vector3(0.18f, 0.025f, 0.1f), warningRed).transform.rotation = rotation;
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

    static GameObject CreateWalkableCollider(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var collider = go.AddComponent<BoxCollider>();
        collider.size = size;
        return go;
    }

    static void CreateOfficeBoundaryColliders(Transform root)
    {
        CreateBlockingCollider("HQInteriorNorthWallCollider", root,
            new Vector3(0f, 1.35f, 2.66f), new Vector3(6.45f, 2.7f, 0.28f));
        CreateBlockingCollider("HQInteriorLeftWallCollider", root,
            new Vector3(-3.18f, 1.35f, 0f), new Vector3(0.28f, 2.7f, 5.35f));
        CreateBlockingCollider("HQInteriorRightWallCollider", root,
            new Vector3(3.18f, 1.35f, 0.72f), new Vector3(0.28f, 2.7f, 3.9f));
        CreateBlockingCollider("HQInteriorSouthWallLeftCollider", root,
            new Vector3(-1.38f, 1.35f, -2.66f), new Vector3(3.65f, 2.7f, 0.28f));
        CreateBlockingCollider("HQInteriorSouthWallRightCollider", root,
            new Vector3(4.15f, 1.35f, -2.66f), new Vector3(1.2f, 2.7f, 0.28f));
        CreateBlockingCollider("HQExteriorStreetBoundaryCollider", root,
            new Vector3(2.45f, 0.85f, -8.66f), new Vector3(6.05f, 1.7f, 0.34f));
        CreateBlockingCollider("HQExteriorLeftCurbCollider", root,
            new Vector3(-0.55f, 0.85f, -6.1f), new Vector3(0.34f, 1.7f, 4.9f));
        CreateBlockingCollider("HQExteriorRightCurbCollider", root,
            new Vector3(5.45f, 0.85f, -6.1f), new Vector3(0.34f, 1.7f, 4.9f));
        CreateBlockingCollider("HQExteriorBackLeftFenceCollider", root,
            new Vector3(0.06f, 0.85f, -3.64f), new Vector3(1.36f, 1.7f, 0.32f));
        CreateBlockingCollider("HQExteriorBackRightFenceCollider", root,
            new Vector3(4.56f, 0.85f, -3.64f), new Vector3(1.86f, 1.7f, 0.32f));
        CreateBlockingCollider("HQGarageDoorLeftSafetyPostCollider", root,
            new Vector3(0.54f, 0.9f, -2.82f), new Vector3(0.32f, 1.8f, 0.62f));
        CreateBlockingCollider("HQGarageDoorRightSafetyPostCollider", root,
            new Vector3(3.66f, 0.9f, -2.82f), new Vector3(0.32f, 1.8f, 0.62f));
    }

    static GameObject CreateBlockingCollider(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var collider = go.AddComponent<BoxCollider>();
        collider.size = size;
        return go;
    }

    static void SetBoxColliderWorldSize(BoxCollider collider, Vector3 worldSize)
    {
        Vector3 scale = collider.transform.lossyScale;
        collider.size = new Vector3(
            SafeDivide(worldSize.x, Mathf.Abs(scale.x)),
            SafeDivide(worldSize.y, Mathf.Abs(scale.y)),
            SafeDivide(worldSize.z, Mathf.Abs(scale.z)));
    }

    static void SetBoxColliderWorldCenter(BoxCollider collider, Vector3 worldCenter)
    {
        collider.center = collider.transform.InverseTransformPoint(worldCenter);
    }

    static float SafeDivide(float value, float divisor)
    {
        return divisor > 0.0001f ? value / divisor : value;
    }

    static Light CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    static Light CreateSpotLight(string name, Transform parent, Vector3 position, Vector3 target, Color color, float intensity, float range, float spotAngle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        Vector3 direction = target - position;
        if (direction.sqrMagnitude > 0.001f)
            go.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        return light;
    }

    static void EnsureHqMenuCamera(Transform root)
    {
        if (Camera.main != null) return;

        var cameraGo = new GameObject("HQMenuCamera");
        cameraGo.transform.SetParent(root, false);
        cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 1.55f, 1.25f), Quaternion.Euler(6f, 180f, 0f));
        cameraGo.tag = "MainCamera";

        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = DeadRubber;
        camera.depth = -10f;
        camera.fieldOfView = 62f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 80f;
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

        var collider = locker.GetComponent<BoxCollider>();
        if (collider == null)
            collider = locker.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(1.35f, 1.15f, 1.35f);
        collider.center = new Vector3(0f, 0f, -0.15f);

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
        CreateBox("SchoolReturnVan_ClockScreen", root, new Vector3(exitPosition.x, 1.42f, exitPosition.z - 0.31f),
            new Vector3(0.72f, 0.2f, 0.035f), exitGreen);
        CreateBox("SchoolReturnVan_ClockBezel", root, new Vector3(exitPosition.x, 1.42f, exitPosition.z - 0.34f),
            new Vector3(0.88f, 0.28f, 0.025f), darkMetal);
        CreateBox("SchoolReturnVan_ClockLineA", root, new Vector3(exitPosition.x - 0.13f, 1.43f, exitPosition.z - 0.285f),
            new Vector3(0.18f, 0.035f, 0.02f), paper);
        CreateBox("SchoolReturnVan_ClockLineB", root, new Vector3(exitPosition.x + 0.18f, 1.43f, exitPosition.z - 0.285f),
            new Vector3(0.22f, 0.035f, 0.02f), paper);

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
        exitLight.color = DispatchGreen;
        exitLight.intensity = 0.7f;
        exitLight.range = 3.8f;
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
        RenderSettings.fogColor = CivicTealDark;
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.17f);

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light == null || light.name != "OfficeLight") continue;
            light.intensity = Mathf.Max(light.intensity, 1.15f);
            light.color = SodiumAmberPale;
        }
    }

    static void ApplySchoolAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = CivicTealDark;
        RenderSettings.fogDensity = 0.021f;
        RenderSettings.ambientLight = new Color(0.035f, 0.044f, 0.04f);
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

        CreateBox("ExteriorDispatchYardConcrete", root, new Vector3(2.45f, 0.065f, -6.2f),
            new Vector3(5.35f, 0.028f, 4.25f), concrete);
        CreateBox("ExteriorCurbLeft", root, new Vector3(-0.25f, 0.18f, -5.95f),
            new Vector3(0.14f, 0.18f, 3.6f), darkMetal);
        CreateBox("ExteriorCurbRight", root, new Vector3(5.1f, 0.18f, -5.95f),
            new Vector3(0.14f, 0.18f, 3.6f), darkMetal);
        CreateBox("ExteriorStreetEdge", root, new Vector3(2.45f, 0.18f, -8.25f),
            new Vector3(5.45f, 0.18f, 0.12f), darkMetal);

        for (int i = 0; i < 3; i++)
        {
            CreateBox($"GarageBayDoorRolledUpPanel_{i + 1}", root, new Vector3(2.45f, 2.28f + i * 0.16f, -3.715f),
                new Vector3(3.35f, 0.23f, 0.055f), bayDoor);
            CreateBox($"GarageBayDoorRolledUpSeam_{i + 1}", root, new Vector3(2.45f, 2.17f + i * 0.16f, -3.755f),
                new Vector3(3.45f, 0.035f, 0.035f), darkMetal);
        }
        CreateBox("GarageOpenDoorDarkInterior", root, new Vector3(2.45f, 1.18f, -3.79f),
            new Vector3(3.24f, 1.9f, 0.03f), MakeMaterial(new Color(0.015f, 0.02f, 0.018f)));
        CreateBox("GarageExitThreshold", root, new Vector3(2.45f, 0.13f, -3.86f),
            new Vector3(3.2f, 0.045f, 0.24f), hazardStripe);

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
        CreateBox("DispatchRoutePaint_ToDoor", root, new Vector3(2.45f, 0.13f, -2.55f),
            new Vector3(0.16f, 0.018f, 2.55f), terminalGreen);
        CreateBox("ExteriorDispatchRoutePaint", root, new Vector3(2.45f, 0.095f, -5.45f),
            new Vector3(0.18f, 0.018f, 2.45f), terminalGreen);
        GameObject arrowLeft = CreateBox("DispatchRouteArrow_L", root, new Vector3(1.65f, 0.12f, -1.08f),
            new Vector3(0.62f, 0.018f, 0.12f), terminalGreen);
        arrowLeft.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject arrowRight = CreateBox("DispatchRouteArrow_R", root, new Vector3(1.65f, 0.12f, -1.08f),
            new Vector3(0.62f, 0.018f, 0.12f), terminalGreen);
        arrowRight.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
        GameObject exteriorArrowLeft = CreateBox("ExteriorDispatchRouteArrow_L", root, new Vector3(2.45f, 0.1f, -6.48f),
            new Vector3(0.55f, 0.018f, 0.12f), terminalGreen);
        exteriorArrowLeft.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject exteriorArrowRight = CreateBox("ExteriorDispatchRouteArrow_R", root, new Vector3(2.45f, 0.1f, -6.48f),
            new Vector3(0.55f, 0.018f, 0.12f), terminalGreen);
        exteriorArrowRight.transform.rotation = Quaternion.Euler(0f, -35f, 0f);

        if (CreateGeneratedDispatchVanIfAvailable(root, terminalGreen))
            return;

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
            new Vector3(2.65f, 0.86f, -0.85f), new Vector3(3.8f, 1.8f, 0.95f));
        trigger.AddComponent<OfficeDepartureVan>();
        CreateBlockingCollider("DispatchVanSolidBodyCollider", root,
            new Vector3(2.65f, 0.86f, -2.35f), new Vector3(1.9f, 1.45f, 2.7f));
        CreateBlockingCollider("DispatchVanSolidNoseCollider", root,
            new Vector3(2.65f, 0.68f, -3.52f), new Vector3(1.55f, 0.85f, 0.78f));
        CreateBox("DispatchVanBoardingPad", root, new Vector3(2.65f, 0.13f, -0.86f),
            new Vector3(3.65f, 0.025f, 0.82f), terminalGreen);

        var beaconGo = new GameObject("DispatchVanSicklyBeacon");
        beaconGo.transform.SetParent(root, false);
        beaconGo.transform.position = new Vector3(2.65f, 1.72f, -2.45f);
        var beacon = beaconGo.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.color = SodiumAmber;
        beacon.intensity = 0.35f;
        beacon.range = 2.5f;

        var bayLightGo = new GameObject("GarageAmberBayLight");
        bayLightGo.transform.SetParent(root, false);
        bayLightGo.transform.position = new Vector3(2.55f, 2.48f, -2.2f);
        var bayLight = bayLightGo.AddComponent<Light>();
        bayLight.type = LightType.Point;
        bayLight.color = SodiumAmberPale;
        bayLight.intensity = 0.85f;
        bayLight.range = 4.8f;
    }

    static bool CreateGeneratedDispatchVanIfAvailable(Transform root, Material terminalGreen)
    {
        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_PlayableDepartureVan");
        if (prefab == null) return false;

        GameObject van = Object.Instantiate(prefab, root);
        van.name = "DispatchVan_ASV4_PlayableGenerated";
        van.transform.localPosition = new Vector3(2.65f, 0.08f, -6.35f);
        van.transform.localRotation = Quaternion.identity;
        van.transform.localScale = Vector3.one;
        ConfigureGeneratedDepartureVan(van);
        PatchGeneratedVanLeftSideVisual(root, van.transform.position, terminalGreen);

        if (van.GetComponentInChildren<OfficeDepartureVan>() == null)
        {
            GameObject trigger = CreateInteractionTrigger("DispatchVanASV4DepartureTrigger", root,
                new Vector3(2.65f, 0.86f, -4.93f), new Vector3(4.35f, 1.85f, 0.95f));
            trigger.AddComponent<OfficeDepartureVan>();
        }

        var beaconGo = new GameObject("DispatchVanASV4SicklyBeacon");
        beaconGo.transform.SetParent(root, false);
        beaconGo.transform.position = new Vector3(2.65f, 1.72f, -6.35f);
        var beacon = beaconGo.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.color = SodiumAmber;
        beacon.intensity = 0.35f;
        beacon.range = 2.4f;

        CreateSpotLight("GarageAmberBayLight", root, new Vector3(2.55f, 2.48f, -3.85f),
            new Vector3(2.55f, 0.12f, -5.75f), SodiumAmberPale, 4.0f, 9.5f, 86f);
        CreateSpotLight("GarageVanOverheadWorkLight", root, new Vector3(2.65f, 2.55f, -6.25f),
            new Vector3(2.65f, 0.35f, -6.35f), SodiumAmberPale, 4.4f, 8.5f, 88f);
        CreatePointLight("GarageVehicleFillLight", root, new Vector3(2.6f, 1.55f, -6.1f),
            SodiumAmberPale, 3.2f, 8.6f);
        CreatePointLight("GarageVehicleFloorBounceLight", root, new Vector3(2.65f, 0.85f, -6.45f),
            SodiumAmber, 1.4f, 6.8f);
        CreatePointLight("GarageDoorGreenWorkLight", root, new Vector3(1.15f, 1.35f, -4.1f),
            DispatchGreen, 0.9f, 4.6f);
        CreateSpotLight("DispatchVanASV4HeadlightCone", root, new Vector3(2.65f, 0.7f, -7.7f),
            new Vector3(2.65f, 0.18f, -8.35f), SodiumAmberPale, 1.25f, 4.2f, 52f);

        CreateBox("DispatchVanASV4RoutePickupMark", root, new Vector3(2.65f, 0.1f, -6.35f),
            new Vector3(0.28f, 0.018f, 2.7f), terminalGreen);
        return true;
    }

    static void ConfigureGeneratedDepartureVan(GameObject van)
    {
        if (van == null) return;

        if (van.TryGetComponent<BoxCollider>(out var boardingTrigger))
        {
            boardingTrigger.isTrigger = true;
            boardingTrigger.center = new Vector3(0f, 0.78f, 1.42f);
            boardingTrigger.size = new Vector3(4.35f, 1.85f, 0.95f);
        }

        AddLocalBlockingCollider(van.transform, "ASV4DepartureVanSolidBodyCollider",
            new Vector3(0f, 0.86f, 0f), new Vector3(3.55f, 1.45f, 1.7f));
        AddLocalBlockingCollider(van.transform, "ASV4DepartureVanFrontBulkCollider",
            new Vector3(-1.65f, 0.68f, 0f), new Vector3(0.55f, 0.92f, 1.35f));
        AddLocalBlockingCollider(van.transform, "ASV4DepartureVanRearBulkCollider",
            new Vector3(1.55f, 0.66f, 0f), new Vector3(0.36f, 0.82f, 1.28f));
    }

    static void PatchGeneratedVanLeftSideVisual(Transform root, Vector3 vanCenter, Material terminalGreen)
    {
        Material repairPanel = MakeOfficeMaterial("Office_ASV4VanLeftSideRepairPanel",
            CivicTeal, DirtyBone, OfficePattern.Scratched);
        Material repairDirt = MakeOfficeMaterial("Office_ASV4VanLeftSideLowerDirt",
            DeadRubberSoft, DeadRubber, OfficePattern.Grime);
        Material repairPaper = MakeOfficeMaterial("Office_ASV4VanLeftSidePaperLabel",
            AgedPaper, DeadRubberSoft, OfficePattern.Warning);
        Material repairDebt = MakeOfficeMaterial("Office_ASV4VanLeftSideDebtSlash",
            StampRed, AgedPaper, OfficePattern.Warning);

        CreateBox("DispatchVanASV4LeftSideSkinRepair", root, vanCenter + new Vector3(0f, 0.86f, -0.755f),
            new Vector3(3.08f, 0.72f, 0.035f), repairPanel);
        CreateBox("DispatchVanASV4LeftLowerDirtyPanel", root, vanCenter + new Vector3(-0.06f, 0.47f, -0.745f),
            new Vector3(1.48f, 0.095f, 0.032f), repairDirt);
        CreateBox("DispatchVanASV4LeftCompanyPatch", root, vanCenter + new Vector3(0.45f, 0.72f, -0.776f),
            new Vector3(0.34f, 0.12f, 0.024f), terminalGreen);
        CreateBox("DispatchVanASV4LeftServiceLabel", root, vanCenter + new Vector3(-0.12f, 0.58f, -0.778f),
            new Vector3(0.42f, 0.06f, 0.024f), repairPaper);
        GameObject slash = CreateBox("DispatchVanASV4LeftDebtSlash", root, vanCenter + new Vector3(1.04f, 0.79f, -0.78f),
            new Vector3(0.27f, 0.045f, 0.026f), repairDebt);
        slash.transform.rotation = Quaternion.Euler(0f, 0f, -10f);
        CreateBox("DispatchVanASV4BoardingPad", root, vanCenter + new Vector3(0f, 0.03f, 1.42f),
            new Vector3(4.05f, 0.025f, 0.82f), terminalGreen);
    }

    static void AddLocalBlockingCollider(Transform parent, string name, Vector3 localCenter, Vector3 size)
    {
        if (parent == null || parent.Find(name) != null) return;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localCenter;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = size;
    }

    static bool CreateGeneratedOfficeVisualIfAvailable(Transform root)
    {
        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_HQ_RundownCommissionOffice");
        if (prefab == null) return false;

        GameObject office = Object.Instantiate(prefab, root);
        office.name = "HQ_ASV4_BlenderOfficeVisual";
        office.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        office.transform.localRotation = Quaternion.identity;
        office.transform.localScale = Vector3.one;

        foreach (Collider collider in office.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        return true;
    }

    static void ClearExistingOfficeStyleRoots()
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (GameObject obj in objects)
        {
            if (!obj.name.StartsWith("MVP_RuntimeStyle_Office")) continue;
            obj.SetActive(false);
            Object.Destroy(obj);
        }
    }

    static void OpenOriginalBackWallForExteriorExit()
    {
        GameObject backWall = GameObject.Find("Wall_Back");
        if (backWall == null) return;

        foreach (Collider collider in backWall.GetComponentsInChildren<Collider>())
            collider.enabled = false;
        foreach (Renderer renderer in backWall.GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
    }

    static void HideOriginalOfficeBlockoutProps()
    {
        string[] names =
        {
            "Desk",
            "Shelf",
            "Shelf_Top",
            "Whiteboard",
            "Sign_ZeroAccident",
            "OldFan",
            "DoorFrame"
        };

        foreach (string name in names)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null) continue;

            foreach (Collider collider in obj.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
        }
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

    static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
