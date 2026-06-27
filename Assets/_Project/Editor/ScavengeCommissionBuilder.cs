using UnityEditor;
using UnityEngine;
using BlackCommission.Scavenge;

/// <summary>
/// One-shot content builder for the scavenging commission loop. Generates the data the terminal +
/// loot spawner consume so the two-commission, two-map loop is real:
///   • An item library (Resources/Scavenge/Items) across the 12 canonical categories
///     (scavenging-item-system §Item Categories) — names are civic-noir Earth-Coast flavour,
///     baseValue is PLACEHOLDER per the GDD (balance pass owns real numbers). LootSpawner picks the
///     whitebox prefab by weight class, so the def's prefab field is intentionally left null.
///   • Commission #1 — Tower (Resources/Tasks/TowerEarthCoast_01): a COMMISSIONED job whose Mars
///     client favours residents' personal effects / civic documents / residential fixtures
///     (scavenging-core-loop §0 D-B). favouredCategoryIds is set from ScavengeCategory (cast to int —
///     OfficeTaskDefinition lives in Office.Core which cannot reference the Scavenge assembly).
///   • Commission #2 — Map2 (Resources/Tasks/FreeSalvage_Map2): FREE SALVAGE, market rate, no
///     favoured categories, sceneName = Map2_Procedural.
/// OfficeComputer loads every OfficeTaskDefinition under Resources/Tasks, so after this runs the
/// terminal lists BOTH commissions automatically (no prefab wiring).
///
/// Menu: Tools ▸ Black Commission ▸ Scavenge ▸ Build Commissions + Item Library  (re-run = idempotent upsert)
/// </summary>
public static class ScavengeCommissionBuilder
{
    const string ItemsDir = "Assets/Resources/Scavenge/Items";
    const string TasksDir = "Assets/Resources/Tasks";

    [MenuItem("Tools/Black Commission/Scavenge/Build Commissions + Item Library")]
    public static void Build()
    {
        EnsureFolder(ItemsDir);
        EnsureFolder(TasksDir);
        int items = BuildItemLibrary();
        BuildTowerCommission();
        BuildMap2Commission();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ScavengeCommissionBuilder] item library = {items} defs in {ItemsDir}; " +
                  "commissions = Tower (Commissioned, favours effects/civic/fixtures) + Map2 (Free Salvage). " +
                  "The terminal pool reads Resources/Tasks, so both now appear + are selectable.");
    }

    readonly struct ItemDef
    {
        public readonly string Id, Name;
        public readonly ScavengeCategory Cat;
        public readonly WeightClass W;
        public readonly int Val;
        public ItemDef(string id, string name, ScavengeCategory cat, WeightClass w, int val)
        { Id = id; Name = name; Cat = cat; W = w; Val = val; }
    }

    // ~15 items spread across all 12 categories + the three weight classes. Eco-column repurposed as a
    // Heavy residential-fixture item (scavenging-core-loop §0 D-F), not deleted.
    static readonly ItemDef[] Library =
    {
        new("corr_family_letter",  "泛黄的家书",       ScavengeCategory.PersonalCorrespondence, WeightClass.Light,  45),
        new("photo_family",        "全家福相框",       ScavengeCategory.FamilyPhotography,      WeightClass.Light,  50),
        new("child_crayon",        "孩子的蜡笔画",     ScavengeCategory.ChildrensArtifacts,     WeightClass.Light,  35),
        new("med_pill_bottle",     "处方药瓶",         ScavengeCategory.MedicalPharmaceutical,  WeightClass.Light,  40),
        new("civic_debt_stub",     "拖欠通知存根",     ScavengeCategory.CivicDocuments,         WeightClass.Light,  30),
        new("civic_deed",          "公产权证",         ScavengeCategory.CivicDocuments,         WeightClass.Light,  65),
        new("pub_gazetteer",       "绝版地方志",       ScavengeCategory.CulturalPublications,   WeightClass.Medium, 70),
        new("eff_enamel_mug",      "搪瓷水杯",         ScavengeCategory.PersonalEffects,        WeightClass.Light,  25),
        new("eff_pocket_watch",    "旧怀表",           ScavengeCategory.PersonalEffects,        WeightClass.Light,  90),
        new("tech_deskphone",      "停产的座机",       ScavengeCategory.HouseholdTechnology,    WeightClass.Medium, 55),
        new("tool_foreman_tape",   "工头的卷尺",       ScavengeCategory.ProfessionalTools,      WeightClass.Medium, 45),
        new("plant_soil_core",     "封存的海岸土芯",   ScavengeCategory.NativePlantSpecimens,   WeightClass.Medium, 80),
        new("relig_home_altar",    "家用神龛",         ScavengeCategory.ReligiousCeremonial,    WeightClass.Medium, 65),
        new("fix_showflat_lamp",   "样板间灯具",       ScavengeCategory.ResidentialFixtures,    WeightClass.Heavy,  120),
        new("fix_eco_column",      "生态柱构件",       ScavengeCategory.ResidentialFixtures,    WeightClass.Heavy,  140),
    };

    static int BuildItemLibrary()
    {
        int n = 0;
        foreach (var d in Library)
        {
            string path = $"{ItemsDir}/Item_{d.Id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<ScavengeItemDefinition>(path);
            bool created = def == null;
            if (created) def = ScriptableObject.CreateInstance<ScavengeItemDefinition>();
            def.id = d.Id;
            def.displayName = d.Name;
            def.category = d.Cat;
            def.weight = d.W;
            def.baseValue = d.Val;
            def.allowedSurfaces = System.Array.Empty<LootSurface>();
            def.prefab = null; // LootSpawner resolves the whitebox prefab by weight class
            if (created) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);
            n++;
        }
        return n;
    }

    static void BuildTowerCommission()
    {
        const string path = TasksDir + "/TowerEarthCoast_01.asset";
        var t = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(path);
        bool created = t == null;
        if (created) t = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
        t.taskId = "tower_earthcoast_01";
        t.title = "「真实海岸」住户清空";
        t.category = MvpTaskCategory.LostItemRecovery;
        t.client = "火星 · 地球遗产征集事务所";
        t.description = "客户是火星的「地球遗产征集」机构——他们尤其想要原住户留下的东西:私人物件、公产文件、住宅家具。" +
                        "把烂尾楼里值钱的都带回来,他们按偏好估价。";
        t.locationName = "地球海岸壹号 · 烂尾楼";
        t.sceneName = "Tower_EarthCoast_01";
        t.moneyReward = 300;
        t.clientType = CommissionClientType.Commissioned;
        t.favouredCategoryIds = new[]
        {
            (int)ScavengeCategory.PersonalEffects,
            (int)ScavengeCategory.CivicDocuments,
            (int)ScavengeCategory.ResidentialFixtures,
        };
        if (created) AssetDatabase.CreateAsset(t, path); else EditorUtility.SetDirty(t);
    }

    static void BuildMap2Commission()
    {
        const string path = TasksDir + "/FreeSalvage_Map2.asset";
        var t = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(path);
        bool created = t == null;
        if (created) t = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
        t.taskId = "freesalvage_map2_01";
        t.title = "自由采集 · 外围废址";
        t.category = MvpTaskCategory.LostItemRecovery;
        t.client = "事务所自留";
        t.description = "没有客户、没有偏好——市价回收。能搬多少是多少,够付这个月的安全开销就行。";
        t.locationName = "外围 · 程序生成废址";
        t.sceneName = "Map2_Procedural";
        t.moneyReward = 0;
        t.clientType = CommissionClientType.FreeSalvage;
        t.favouredCategoryIds = System.Array.Empty<int>();
        if (created) AssetDatabase.CreateAsset(t, path); else EditorUtility.SetDirty(t);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
