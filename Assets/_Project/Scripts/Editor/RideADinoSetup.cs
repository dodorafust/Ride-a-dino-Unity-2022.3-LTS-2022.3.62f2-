using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu Tools > Ride a Dino:
//   1. Create Sample Data   : Variant + Dino + SpawnTable mẫu
//   2. Build UI             : (RideADinoUIBuilder) dựng Canvas + prefab UI
//   3. Setup World Objects  : GameManager, chuồng, thú nhún, bảng nâng cấp, RunZone, gán DinoSpawner
//   Clean Old Build-2 Objects : xóa các object Build 2 bản cũ trước khi chạy lại
public static class RideADinoSetup
{
    const string DinoFolder = "Assets/ScriptableObjects/Dinos";
    const string VariantFolder = "Assets/ScriptableObjects/Variants";
    const string TableFolder = "Assets/ScriptableObjects/SpawnTables";
    const string TablePath = "Assets/ScriptableObjects/SpawnTables/SpawnTable_Default.asset";
    const string MatFolder = "Assets/_Project/Materials";

    // ============================================================ 1. Data mẫu
    [MenuItem("Tools/Ride a Dino/1. Create Sample Data")]
    public static void CreateSampleData()
    {
        EnsureFolder(DinoFolder);
        EnsureFolder(VariantFolder);
        EnsureFolder(TableFolder);

        // Asset DinoDatabase của bản cũ không còn dùng
        AssetDatabase.DeleteAsset("Assets/ScriptableObjects/DinoDatabase.asset");

        DinoVariant normal  = Variant("Normal",  true,  1f,  false, Color.white);
        DinoVariant gold    = Variant("Gold",    false, 2f,  true,  new Color(1f, 0.84f, 0.1f));
        DinoVariant diamond = Variant("Diamond", false, 4f,  true,  new Color(0.6f, 0.92f, 1f));
        DinoVariant magma   = Variant("Magma",   false, 8f,  true,  new Color(1f, 0.35f, 0.05f));
        DinoVariant emerald = Variant("Emerald", false, 16f, true,  new Color(0.1f, 0.85f, 0.45f));

        DinoSpawnTable table = LoadOrCreate<DinoSpawnTable>(TablePath);
        table.entries.Clear();
        table.variants.Clear();

        //          tên                  rarity            zone money req    tame speed  weight
        AddEntry(table, "Dodo",               Rarity.Common,    1, 1f,  5f,   4f,   5.0f, 40f);
        AddEntry(table, "Dilophosaurus",      Rarity.Uncommon,  1, 2f,  10f,  4.5f, 5.5f, 25f);
        AddEntry(table, "Ankylosaurus",       Rarity.Uncommon,  1, 3f,  15f,  4.5f, 6.0f, 20f);
        AddEntry(table, "Pachycephalosaurus", Rarity.Rare,      2, 6f,  40f,  5f,   6.5f, 10f);
        AddEntry(table, "Triceratops",        Rarity.Rare,      2, 8f,  60f,  5f,   7.0f, 8f);
        AddEntry(table, "Suzhousaurus",       Rarity.Epic,      3, 15f, 120f, 6f,   7.5f, 4f);
        AddEntry(table, "Brachiosaurus",      Rarity.Legendary, 3, 30f, 300f, 7f,   8.0f, 2f);
        AddEntry(table, "Tyrannosaurus",      Rarity.Mythic,    4, 80f, 800f, 8f,   8.5f, 1f);

        AddVariant(table, normal, 80f);
        AddVariant(table, gold, 10f);
        AddVariant(table, diamond, 5f);
        AddVariant(table, magma, 3f);
        AddVariant(table, emerald, 2f);

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = table;
        Debug.Log("Ride a Dino: đã tạo " + table.entries.Count + " Dino, " + table.variants.Count + " Variant, 1 SpawnTable.");
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null)
        {
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
        }
        return a;
    }

    static DinoVariant Variant(string name, bool isNormal, float mul, bool tint, Color color)
    {
        DinoVariant v = LoadOrCreate<DinoVariant>(VariantFolder + "/Variant_" + name + ".asset");
        v.displayName = name;
        v.isNormal = isNormal;
        v.moneyMultiplier = mul;
        v.useTint = tint;
        v.tint = color;
        EditorUtility.SetDirty(v);
        return v;
    }

    static void AddEntry(DinoSpawnTable table, string name, Rarity r, int zone, float money, float req,
                         float tame, float speed, float weight)
    {
        DinoData d = LoadOrCreate<DinoData>(DinoFolder + "/" + name + ".asset");
        d.displayName = name;
        d.rarity = r;
        d.zone = zone;
        d.baseMoney = money;
        d.requiredStamina = req;
        d.tameTime = tame;
        d.runSpeed = speed;
        EditorUtility.SetDirty(d);

        var e = new DinoSpawnTable.Entry();
        e.dino = d;
        e.weight = weight;
        table.entries.Add(e);
    }

    static void AddVariant(DinoSpawnTable table, DinoVariant v, float weight)
    {
        var w = new DinoSpawnTable.VariantWeight();
        w.variant = v;
        w.weight = weight;
        table.variants.Add(w);
    }

    // ============================================================ 3. Dựng world
    [MenuItem("Tools/Ride a Dino/3. Setup World Objects")]
    public static void SetupWorld()
    {
        DinoSpawnTable table = AssetDatabase.LoadAssetAtPath<DinoSpawnTable>(TablePath);
        if (table == null)
        {
            EditorUtility.DisplayDialog("Ride a Dino", "Chưa có SpawnTable. Chạy '1. Create Sample Data' trước.", "OK");
            return;
        }

        RideADinoUIBuilder.EnsureWorldAssets();
        if (RideADinoUIBuilder.LabelPrefab == null || RideADinoUIBuilder.PopupPrefab == null) return;   // thiếu TMP Essentials

        EnsureFolder(MatFolder);
        Material matFloor   = Mat("Mat_PenFloor", new Color(0.85f, 0.75f, 0.5f));
        Material matFloor2  = Mat("Mat_PenFloorGold", new Color(0.95f, 0.8f, 0.3f));
        Material matWood    = Mat("Mat_Wood", new Color(0.5f, 0.32f, 0.15f));
        Material matWhite   = Mat("Mat_FenceWhite", new Color(0.92f, 0.92f, 0.92f));
        Material matMachine = Mat("Mat_Machine", new Color(0.2f, 0.45f, 0.9f));
        Material matSign    = Mat("Mat_Sign", new Color(0.9f, 0.7f, 0.2f));

        // --- GameManager + WorldTapper
        GameObject gmGo = FindOrCreate("_GameManager");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gmGo);
        GameManager gm = GetOrAdd<GameManager>(gmGo);
        GetOrAdd<WorldTapper>(gmGo);
        gm.worldLabelPrefab = RideADinoUIBuilder.LabelPrefab;
        gm.popupPrefab = RideADinoUIBuilder.PopupPrefab;
        gm.staminaIcon = RideADinoUIBuilder.BoltSprite;
        gm.cashIcon = RideADinoUIBuilder.CoinSprite;

        // --- PlayerSpawn
        GameObject spawn = FindOrCreate("PlayerSpawn");
        if (spawn.transform.position == Vector3.zero) spawn.transform.position = new Vector3(0f, 1f, -5f);
        gm.respawnPoint = spawn.transform;
        EditorUtility.SetDirty(gm);

        // --- RunZone (chỉnh kích thước cho khớp đường chạy của bạn)
        RunZone zone = Object.FindObjectOfType<RunZone>();
        if (zone == null)
        {
            var zg = new GameObject("RunZone");
            zg.transform.position = new Vector3(0f, 3f, 150f);
            BoxCollider zb = zg.AddComponent<BoxCollider>();
            zb.isTrigger = true;
            zb.size = new Vector3(20f, 6f, 300f);
            zone = zg.AddComponent<RunZone>();
        }

        // --- Pen
        GameObject penGo = GameObject.Find("Pen");
        Pen pen;
        if (penGo == null)
        {
            penGo = new GameObject("Pen");
            penGo.transform.position = new Vector3(0f, 0f, -24f);

            var areaGo = new GameObject("Area");
            areaGo.transform.SetParent(penGo.transform, false);
            BoxCollider box = areaGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 2f, 0f);
            box.size = new Vector3(16f, 4f, 10f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(penGo.transform, false);
            GameObject t1 = PenTier(visuals.transform, "Tier_Lv1", matFloor, matWood, 1f);
            GameObject t4 = PenTier(visuals.transform, "Tier_Lv4", matFloor2, matWhite, 1.6f);
            t4.SetActive(false);

            pen = penGo.AddComponent<Pen>();
            pen.area = box;
            pen.levelVisuals = new LevelVisual[]
            {
                new LevelVisual { fromLevel = 1, root = t1 },
                new LevelVisual { fromLevel = 4, root = t4 }
            };
        }
        else
        {
            pen = GetOrAdd<Pen>(penGo);
        }

        // --- Bảng nâng cấp chuồng (trái cửa chuồng)
        GameObject penSign = GameObject.Find("PenUpgradeSign");
        if (penSign == null) penSign = MakeSign("PenUpgradeSign", new Vector3(-10.5f, 0f, -18f), matWood, matSign);
        UpgradeSign ps = GetOrAdd<UpgradeSign>(penSign);
        ps.target = pen;

        // --- Thú nhún (phải cửa chuồng)
        GameObject machGo = GameObject.Find("DinoMachine");
        DinoMachine machine;
        if (machGo == null)
        {
            machGo = new GameObject("DinoMachine");
            machGo.transform.position = new Vector3(10.5f, 0f, -18f);

            Cube("Base", machGo.transform, new Vector3(0f, 0.2f, 0f), new Vector3(3.2f, 0.4f, 3.2f), matWood, false, true);

            // SpringRoot đặt ở đáy thú nhún (pivot xoay), đung đưa trước-sau quanh trục X
            var springRoot = new GameObject("SpringRoot");
            springRoot.transform.SetParent(machGo.transform, false);
            springRoot.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            Cube("Body", springRoot.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 1.2f, 2.4f), matMachine, false, true);

            var seat = new GameObject("Seat");
            seat.transform.SetParent(springRoot.transform, false);
            seat.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var exit = new GameObject("Exit");
            exit.transform.SetParent(machGo.transform, false);
            exit.transform.localPosition = new Vector3(0f, 1f, 3.5f);

            machine = machGo.AddComponent<DinoMachine>();
            machine.spring = springRoot.transform;
            machine.seat = seat.transform;
            machine.exitPoint = exit.transform;
        }
        else
        {
            machine = GetOrAdd<DinoMachine>(machGo);
        }

        // --- Bảng nâng cấp thú nhún
        GameObject machSign = GameObject.Find("MachineUpgradeSign");
        if (machSign == null) machSign = MakeSign("MachineUpgradeSign", new Vector3(14.5f, 0f, -18f), matWood, matSign);
        UpgradeSign ms = GetOrAdd<UpgradeSign>(machSign);
        ms.target = machine;

        // --- Người chơi: MountController
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (player != null) GetOrAdd<MountController>(player.gameObject);
        else Debug.LogWarning("Ride a Dino: không thấy Player trong scene.");

        // --- DinoSpawner
        DinoSpawner spawner = Object.FindObjectOfType<DinoSpawner>();
        if (spawner != null)
        {
            spawner.table = table;
            spawner.zone = zone;
            EditorUtility.SetDirty(spawner);
        }
        else Debug.LogWarning("Ride a Dino: không thấy DinoSpawner trong scene (Build 1).");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Ride a Dino: Setup World xong. Nhớ lưu scene (Ctrl+S).");
    }

    // ============================================================ Dọn bản cũ
    [MenuItem("Tools/Ride a Dino/Clean Old Build-2 Objects")]
    public static void CleanOld()
    {
        if (!EditorUtility.DisplayDialog("Ride a Dino",
            "Xóa các object: Pen, PenUpgradeSign, MachineUpgradeSign, DinoMachine, ObstacleField (nếu có) khỏi scene?\n" +
            "Dùng khi nâng cấp từ bản Build 2 cũ.", "Xóa", "Hủy")) return;

        string[] names = { "Pen", "PenUpgradeSign", "MachineUpgradeSign", "DinoMachine", "ObstacleField" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // ============================================================ helpers
    static GameObject PenTier(Transform parent, string name, Material floorMat, Material fenceMat, float fenceHeight)
    {
        var tier = new GameObject(name);
        tier.transform.SetParent(parent, false);

        // Placeholder KHÔNG có collider (đi xuyên qua được). Thay bằng model Blender của bạn.
        Cube("Floor", tier.transform, new Vector3(0f, 0.05f, 0f), new Vector3(16f, 0.1f, 10f), floorMat, false, false);
        float h = fenceHeight;
        Cube("FenceLeft",  tier.transform, new Vector3(-8.1f, h * 0.5f, 0f),   new Vector3(0.2f, h, 10.2f), fenceMat, false, false);
        Cube("FenceRight", tier.transform, new Vector3(8.1f, h * 0.5f, 0f),    new Vector3(0.2f, h, 10.2f), fenceMat, false, false);
        Cube("FenceBack",  tier.transform, new Vector3(0f, h * 0.5f, -5.1f),   new Vector3(16.4f, h, 0.2f), fenceMat, false, false);
        // Mặt trước (+Z) chừa cửa ở giữa
        Cube("FenceFrontL", tier.transform, new Vector3(-5.5f, h * 0.5f, 5.1f), new Vector3(5f, h, 0.2f), fenceMat, false, false);
        Cube("FenceFrontR", tier.transform, new Vector3(5.5f, h * 0.5f, 5.1f),  new Vector3(5f, h, 0.2f), fenceMat, false, false);
        return tier;
    }

    static GameObject MakeSign(string name, Vector3 pos, Material post, Material board)
    {
        var s = new GameObject(name);
        s.transform.position = pos;
        Cube("Post", s.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.3f, 2.5f, 0.3f), post, false, true);
        Cube("Board", s.transform, new Vector3(0f, 2.6f, 0f), new Vector3(3f, 1.4f, 0.3f), board, false, true);
        return s;
    }

    static GameObject FindOrCreate(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
                           bool trigger, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;

        Collider col = go.GetComponent<Collider>();
        if (!keepCollider) Object.DestroyImmediate(col);
        else col.isTrigger = trigger;
        return go;
    }

    static Material Mat(string name, Color c)
    {
        string path = MatFolder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            m.color = c;
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
