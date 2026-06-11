using UnityEngine;

public static class PlayerGestures
{
    public const int Count = 5;
    public const float Duration = 2f;

    public struct GesturePose
    {
        public Vector3 fpRightPos;
        public Vector3 fpRightRot;
        public Vector3 fpLeftPos;
        public Vector3 fpLeftRot;
        public Vector3 tpRightArmRot;
        public Vector3 tpLeftArmRot;
        public bool hideHeldItem;
    }

    public static readonly string[] Names   = { "", "Shush", "Come Here", "Stop",  "Middle Finger", "Shrug" };
    public static readonly string[] NamesCN = { "", "嘘",    "过来",      "别动",  "竖中指",        "耸肩"  };

    public static readonly GesturePose[] Poses =
    {
        default,

        // 1: Shush — right index finger up in front of face
        new()
        {
            fpRightPos = new Vector3(0.05f, 0.08f, 0.38f),
            fpRightRot = new Vector3(-10f, 0f, 0f),
            fpLeftPos = new Vector3(-0.22f, -0.18f, 0.08f),
            fpLeftRot = new Vector3(30f, -12f, 4f),
            tpRightArmRot = new Vector3(-80f, 0f, -20f),
            tpLeftArmRot = new Vector3(0f, 0f, 10f),
            hideHeldItem = true,
        },

        // 2: Come here — right hand beckoning forward
        new()
        {
            fpRightPos = new Vector3(0.18f, 0.02f, 0.42f),
            fpRightRot = new Vector3(-30f, 15f, -15f),
            fpLeftPos = new Vector3(-0.22f, -0.18f, 0.08f),
            fpLeftRot = new Vector3(30f, -12f, 4f),
            tpRightArmRot = new Vector3(-70f, 20f, -15f),
            tpLeftArmRot = new Vector3(0f, 0f, 10f),
            hideHeldItem = true,
        },

        // 3: Stop — both palms pushed forward
        new()
        {
            fpRightPos = new Vector3(0.15f, 0.06f, 0.46f),
            fpRightRot = new Vector3(-60f, 10f, 0f),
            fpLeftPos = new Vector3(-0.15f, 0.06f, 0.46f),
            fpLeftRot = new Vector3(-60f, -10f, 0f),
            tpRightArmRot = new Vector3(-85f, 0f, -10f),
            tpLeftArmRot = new Vector3(-85f, 0f, 10f),
            hideHeldItem = true,
        },

        // 4: Middle finger — right fist raised
        new()
        {
            fpRightPos = new Vector3(0.12f, 0.18f, 0.32f),
            fpRightRot = new Vector3(-90f, 0f, 0f),
            fpLeftPos = new Vector3(-0.22f, -0.18f, 0.08f),
            fpLeftRot = new Vector3(30f, -12f, 4f),
            tpRightArmRot = new Vector3(-160f, 0f, -5f),
            tpLeftArmRot = new Vector3(0f, 0f, 10f),
            hideHeldItem = true,
        },

        // 5: Shrug — both arms spread to sides, palms up
        new()
        {
            fpRightPos = new Vector3(0.32f, 0.02f, 0.22f),
            fpRightRot = new Vector3(15f, 40f, -25f),
            fpLeftPos = new Vector3(-0.32f, 0.02f, 0.22f),
            fpLeftRot = new Vector3(15f, -40f, 25f),
            tpRightArmRot = new Vector3(-30f, 0f, -65f),
            tpLeftArmRot = new Vector3(-30f, 0f, 65f),
            hideHeldItem = true,
        },
    };

    public static GesturePose Get(int id)
    {
        if (id < 0 || id >= Poses.Length) return default;
        return Poses[id];
    }

    public static string GetName(int id, int language = 0)
    {
        string[] names = language == 0 ? Names : NamesCN;
        if (id < 0 || id >= names.Length) return "";
        return names[id];
    }
}
