#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DinoBalanceApplier
{
    const string DinoFolder = "Assets/ScriptableObjects/Dinos";
    const string BiomeFolder = "Assets/ScriptableObjects/Biomes";
    const string TableFolder = "Assets/ScriptableObjects/SpawnTables";
    const string AppliedKey = "RAD_51Dinos10BiomesBalance_v3";
    const double RewardNerfMultiplier = 0.5; // Giảm 50% phần thưởng Unlock Dino, All Sizes/Variants và Quà hoàn thành Biome

    struct BiomeSpec
    {
        public int zone;
        public string fileName;
        public string tableFileName;
        public string biomeName;
        public Color cardColor;
        public Color borderColor;
        public Color fogColor;
        public double completeCash;
        public float completeStamina;
    }

    struct DinoSpec
    {
        public int zone;
        public int slotIndex; // 1..6 (yếu -> mạnh)
        public string name;
        public DinoDiet diet;
        public Rarity rarity;
        public double baseMoney;
        public float reqStamina;
        public float tameTime;
        public float runSpeed;
        public float riddenSpeedMul;
        public bool killOnEmptyIfUntamed;
        public double unlockCash;
        public float unlockStamina;

        public DinoSpec(
            int zone, int slotIndex, string name, DinoDiet diet, Rarity rarity,
            double baseMoney, float reqStamina, float tameTime, float runSpeed,
            float riddenSpeedMul, bool killOnEmpty, double unlockCash, float unlockStamina)
        {
            this.zone = zone;
            this.slotIndex = slotIndex;
            this.name = name;
            this.diet = diet;
            this.rarity = rarity;
            this.baseMoney = baseMoney;
            this.reqStamina = reqStamina;
            this.tameTime = tameTime;
            this.runSpeed = runSpeed;
            this.riddenSpeedMul = riddenSpeedMul;
            this.killOnEmptyIfUntamed = killOnEmpty;
            this.unlockCash = unlockCash;
            this.unlockStamina = unlockStamina;
        }
    }

    static readonly BiomeSpec[] Biomes = new BiomeSpec[]
    {
        new BiomeSpec { zone = 1, fileName = "Biome_Plains.asset",      tableFileName = "SpawnTable_Plains.asset",      biomeName = "Plains",       cardColor = new Color(0.38f, 0.74f, 0.22f), borderColor = new Color(0.60f, 0.96f, 0.26f), fogColor = new Color(0.72f, 0.86f, 0.96f), completeCash = 8500,          completeStamina = 120f },
        new BiomeSpec { zone = 2, fileName = "Biome_Forest.asset",      tableFileName = "SpawnTable_Forest.asset",      biomeName = "Forest",       cardColor = new Color(0.18f, 0.52f, 0.24f), borderColor = new Color(0.32f, 0.82f, 0.36f), fogColor = new Color(0.54f, 0.76f, 0.62f), completeCash = 180000,        completeStamina = 850f },
        new BiomeSpec { zone = 3, fileName = "Biome_Desert.asset",      tableFileName = "SpawnTable_Desert.asset",      biomeName = "Desert",       cardColor = new Color(0.84f, 0.64f, 0.26f), borderColor = new Color(1.00f, 0.82f, 0.34f), fogColor = new Color(0.95f, 0.84f, 0.64f), completeCash = 4200000,       completeStamina = 5600f },
        new BiomeSpec { zone = 4, fileName = "Biome_Jungle.asset",      tableFileName = "SpawnTable_Jungle.asset",      biomeName = "Jungle",       cardColor = new Color(0.12f, 0.56f, 0.34f), borderColor = new Color(0.24f, 0.88f, 0.52f), fogColor = new Color(0.46f, 0.78f, 0.64f), completeCash = 45000000,      completeStamina = 35000f },
        new BiomeSpec { zone = 5, fileName = "Biome_Snow.asset",        tableFileName = "SpawnTable_Snow.asset",        biomeName = "Snow",         cardColor = new Color(0.46f, 0.78f, 0.96f), borderColor = new Color(0.75f, 0.94f, 1.00f), fogColor = new Color(0.82f, 0.92f, 0.98f), completeCash = 380000000,     completeStamina = 220000f },
        new BiomeSpec { zone = 6, fileName = "Biome_GiantForest.asset", tableFileName = "SpawnTable_GiantForest.asset", biomeName = "Giant Forest", cardColor = new Color(0.22f, 0.44f, 0.18f), borderColor = new Color(0.46f, 0.76f, 0.30f), fogColor = new Color(0.48f, 0.66f, 0.50f), completeCash = 2600000000,    completeStamina = 1500000f },
        new BiomeSpec { zone = 7, fileName = "Biome_AbyssOcean.asset",  tableFileName = "SpawnTable_AbyssOcean.asset",  biomeName = "Abyss Ocean",  cardColor = new Color(0.10f, 0.34f, 0.66f), borderColor = new Color(0.22f, 0.72f, 0.98f), fogColor = new Color(0.18f, 0.42f, 0.68f), completeCash = 15000000000,   completeStamina = 10000000f },
        new BiomeSpec { zone = 8, fileName = "Biome_Volcano.asset",     tableFileName = "SpawnTable_Volcano.asset",     biomeName = "Volcano",      cardColor = new Color(0.74f, 0.22f, 0.14f), borderColor = new Color(1.00f, 0.46f, 0.18f), fogColor = new Color(0.56f, 0.26f, 0.20f), completeCash = 52000000000,   completeStamina = 75000000f },
        new BiomeSpec { zone = 9, fileName = "Biome_Extinction.asset",  tableFileName = "SpawnTable_Extinction.asset",  biomeName = "Extinction",   cardColor = new Color(0.36f, 0.30f, 0.34f), borderColor = new Color(0.78f, 0.70f, 0.58f), fogColor = new Color(0.42f, 0.36f, 0.38f), completeCash = 125000000000,  completeStamina = 420000000f },
        new BiomeSpec { zone = 10, fileName = "Biome_Cosmic.asset",     tableFileName = "SpawnTable_Cosmic.asset",      biomeName = "Cosmic",       cardColor = new Color(0.30f, 0.14f, 0.58f), borderColor = new Color(0.84f, 0.42f, 1.00f), fogColor = new Color(0.18f, 0.10f, 0.34f), completeCash = 256500000000,  completeStamina = 1900000000f }
    };

    static readonly DinoSpec[] Dinos = new DinoSpec[]
    {
        // Map 1: Plains (6/6)
        new DinoSpec(1, 1, "Dodo",               DinoDiet.Herbivore, Rarity.Common,    1,          5f,          3.5f, 16f, 1.00f, false, 100,           20f),
        new DinoSpec(1, 2, "Pachycephalosaurus", DinoDiet.Herbivore, Rarity.Common,    3,          12f,         4.0f, 19f, 1.00f, false, 300,           30f),
        new DinoSpec(1, 3, "Galimimus",          DinoDiet.Herbivore, Rarity.Common,    6,          22f,         4.0f, 24f, 1.05f, false, 600,           40f),
        new DinoSpec(1, 4, "Terror Bird",        DinoDiet.Herbivore, Rarity.Uncommon,  12,         38f,         4.5f, 25f, 1.05f, false, 1200,          55f),
        new DinoSpec(1, 5, "Dilophosaurus",      DinoDiet.Carnivore, Rarity.Uncommon,  24,         60f,         4.5f, 24f, 1.00f, false, 2400,          75f),
        new DinoSpec(1, 6, "Aurochs",            DinoDiet.Herbivore, Rarity.Rare,      45,         95f,         5.0f, 22f, 1.00f, false, 4500,          100f),

        // Map 2: Forest (6/6)
        new DinoSpec(2, 1, "Ankylosaurus",       DinoDiet.Herbivore, Rarity.Uncommon,  100,        160f,        5.0f, 20f, 1.00f, false, 10000,         150f),
        new DinoSpec(2, 2, "Pachyrhinosaurus",   DinoDiet.Herbivore, Rarity.Rare,      200,        260f,        5.2f, 22f, 1.00f, false, 20000,         210f),
        new DinoSpec(2, 3, "Triceratops",        DinoDiet.Herbivore, Rarity.Rare,      350,        400f,        5.5f, 22f, 1.00f, false, 35000,         290f),
        new DinoSpec(2, 4, "Velociraptor",       DinoDiet.Carnivore, Rarity.Rare,      520,        580f,        5.5f, 29f, 1.08f, false, 52000,         380f),
        new DinoSpec(2, 5, "Sarcosuchus",        DinoDiet.Carnivore, Rarity.Epic,      750,        820f,        6.0f, 25f, 1.02f, false, 75000,         500f),
        new DinoSpec(2, 6, "Allosaurus",         DinoDiet.Carnivore, Rarity.Epic,      1200,       1200f,       6.0f, 28f, 1.05f, false, 120000,        680f),

        // Map 3: Desert (6/6)
        new DinoSpec(3, 1, "Kentrosaurus",       DinoDiet.Herbivore, Rarity.Rare,      2000,       1800f,       6.0f, 23f, 1.00f, false, 200000,        900f),
        new DinoSpec(3, 2, "Centipede",          DinoDiet.Carnivore, Rarity.Epic,      3500,       2600f,       6.0f, 27f, 1.02f, false, 350000,        1100f),
        new DinoSpec(3, 3, "Scorpion",           DinoDiet.Carnivore, Rarity.Epic,      5800,       3800f,       6.2f, 27f, 1.02f, false, 580000,        1300f),
        new DinoSpec(3, 4, "Desert Worm",        DinoDiet.Carnivore, Rarity.Epic,      8500,       5400f,       6.5f, 28f, 1.05f, false, 850000,        1450f),
        new DinoSpec(3, 5, "Baryonyx",           DinoDiet.Carnivore, Rarity.Legendary, 12500,      7500f,       6.8f, 29f, 1.05f, false, 1250000,       1550f),
        new DinoSpec(3, 6, "Carnotaurus",        DinoDiet.Carnivore, Rarity.Mythic,    18500,      10500f,      7.0f, 31f, 1.08f, false, 1850000,       1600f),

        // Map 4: Jungle (6/6)
        new DinoSpec(4, 1, "Parasaurolophus",    DinoDiet.Herbivore, Rarity.Epic,      28000,      15000f,      6.5f, 25f, 1.00f, false, 2800000,       2600f),
        new DinoSpec(4, 2, "Stegosaurus",        DinoDiet.Herbivore, Rarity.Epic,      45000,      22000f,      6.8f, 24f, 1.02f, false, 4500000,       3800f),
        new DinoSpec(4, 3, "Ceratosaurus",       DinoDiet.Carnivore, Rarity.Legendary, 70000,      32000f,      7.0f, 30f, 1.05f, false, 7000000,       5500f),
        new DinoSpec(4, 4, "Deinocheirus",       DinoDiet.Carnivore, Rarity.Legendary, 105000,     46000f,      7.2f, 28f, 1.05f, false, 10500000,      7800f),
        new DinoSpec(4, 5, "Therizinosaurus",    DinoDiet.Herbivore, Rarity.Mythic,    145000,     65000f,      7.5f, 28f, 1.05f, true,  14500000,      10500f),
        new DinoSpec(4, 6, "Tyrannosaurus",      DinoDiet.Carnivore, Rarity.Mythic,    200000,     92000f,      7.5f, 32f, 1.08f, false, 20000000,      14000f),

        // Map 5: Snow (6/6 — Yudon & Indominus Rex bậc Secret)
        new DinoSpec(5, 1, "Amargasaurus",       DinoDiet.Herbivore, Rarity.Legendary, 300000,     135000f,     7.2f, 26f, 1.02f, false, 30000000,      22000f),
        new DinoSpec(5, 2, "Mammoth",            DinoDiet.Herbivore, Rarity.Mythic,    480000,     210000f,     7.5f, 26f, 1.04f, true,  48000000,      35000f),
        new DinoSpec(5, 3, "Yutyrannus",         DinoDiet.Carnivore, Rarity.Mythic,    750000,     310000f,     7.8f, 32f, 1.08f, false, 75000000,      54000f),
        new DinoSpec(5, 4, "Phosphoracus",       DinoDiet.Herbivore, Rarity.Mythic,    1100000,    450000f,     8.0f, 33f, 1.10f, false, 110000000,     80000f),
        new DinoSpec(5, 5, "Yudon",              DinoDiet.Carnivore, Rarity.Secret,    1500000,    640000f,     8.2f, 34f, 1.10f, false, 150000000,     115000f),
        new DinoSpec(5, 6, "Indominus Rex",      DinoDiet.Carnivore, Rarity.Secret,    2000000,    900000f,     8.5f, 35f, 1.12f, false, 200000000,     160000f),

        // Map 6: Giant Forest (5/6 — 1 slot trống)
        new DinoSpec(6, 1, "Styracosaurus",      DinoDiet.Herbivore, Rarity.Legendary, 2800000,    1300000f,    7.8f, 26f, 1.04f, false, 280000000,     230000f),
        new DinoSpec(6, 2, "Camarasaurus",       DinoDiet.Herbivore, Rarity.Legendary, 3800000,    1850000f,    8.0f, 26f, 1.04f, false, 380000000,     330000f),
        new DinoSpec(6, 3, "Diplodocus",         DinoDiet.Herbivore, Rarity.Mythic,    5200000,    2600000f,    8.2f, 27f, 1.05f, false, 520000000,     470000f),
        new DinoSpec(6, 4, "Brachiosaurus",      DinoDiet.Herbivore, Rarity.Mythic,    6800000,    3600000f,    8.5f, 27f, 1.05f, true,  680000000,     650000f),
        new DinoSpec(6, 5, "Spinosaurus",        DinoDiet.Carnivore, Rarity.Secret,    9000000,    5000000f,    8.5f, 33f, 1.10f, false, 900000000,     900000f),

        // Map 7: Abyss Ocean (6/6)
        new DinoSpec(7, 1, "Kaprosuchus",        DinoDiet.Carnivore, Rarity.Mythic,    11000000,   7200000f,    8.2f, 33f, 1.08f, false, 1100000000,    1300000f),
        new DinoSpec(7, 2, "Ichthyovenator",     DinoDiet.Carnivore, Rarity.Mythic,    13000000,   10000000f,   8.5f, 33f, 1.08f, false, 1300000000,    1850000f),
        new DinoSpec(7, 3, "Plesiosaurus",       DinoDiet.Carnivore, Rarity.Mythic,    15000000,   14000000f,   8.6f, 34f, 1.10f, false, 1500000000,    2600000f),
        new DinoSpec(7, 4, "Prognathodon",       DinoDiet.Carnivore, Rarity.Secret,    18500000,   19500000f,   8.8f, 35f, 1.10f, false, 1850000000,    3600000f),
        new DinoSpec(7, 5, "Mosasaurus",         DinoDiet.Carnivore, Rarity.Secret,    23000000,   27000000f,   9.0f, 36f, 1.12f, false, 2300000000,    5000000f),
        new DinoSpec(7, 6, "Megalodon",          DinoDiet.Carnivore, Rarity.Secret,    29000000,   38000000f,   9.0f, 37f, 1.12f, false, 2900000000,    7000000f),

        // Map 8: Volcano (3/6 — 3 slot trống)
        new DinoSpec(8, 1, "Giganotosaurus",     DinoDiet.Carnivore, Rarity.Secret,    45000000,   58000000f,   9.0f, 37f, 1.12f, false, 4500000000,    11000000f),
        new DinoSpec(8, 2, "Blaze Carnotaurus",  DinoDiet.Carnivore, Rarity.Secret,    70000000,   92000000f,   9.2f, 39f, 1.14f, false, 7000000000,    18000000f),
        new DinoSpec(8, 3, "Dodo Rex",           DinoDiet.Carnivore, Rarity.Eternal,   110000000,  145000000f,  9.5f, 40f, 1.14f, false, 11000000000,   28000000f),

        // Map 9: Extinction (6/6 — bổ sung Fossil Tyrannosaurus / Ske_Rex)
        new DinoSpec(9, 1, "Fossil Utahraptor",    DinoDiet.Carnivore, Rarity.Secret,  150000000,  210000000f,  9.2f, 42f, 1.14f, false, 15000000000,   40000000f),
        new DinoSpec(9, 2, "Fossil Triceratop",    DinoDiet.Herbivore, Rarity.Secret,  200000000,  290000000f,  9.5f, 36f, 1.10f, true,  20000000000,   52000000f),
        new DinoSpec(9, 3, "Fossil Brachiosaurus", DinoDiet.Herbivore, Rarity.Eternal, 270000000,  390000000f,  9.5f, 36f, 1.10f, true,  27000000000,   66000000f),
        new DinoSpec(9, 4, "Fossil Spinosaurus",   DinoDiet.Carnivore, Rarity.Eternal, 360000000,  520000000f,  9.8f, 40f, 1.14f, false, 36000000000,   80000000f),
        new DinoSpec(9, 5, "Fossil Mosasaurus",    DinoDiet.Carnivore, Rarity.Eternal, 480000000,  680000000f,  9.8f, 42f, 1.15f, false, 48000000000,   96000000f),
        new DinoSpec(9, 6, "Fossil Tyrannosaurus", DinoDiet.Carnivore, Rarity.Eternal, 650000000,  900000000f,  10.0f, 44f, 1.16f, false, 65000000000,  112000000f),

        // Map 10: Cosmic (1/6 — 5 slot trống)
        new DinoSpec(10, 1, "Zilla",             DinoDiet.Carnivore, Rarity.Divine,    1000000000, 1250000000f, 10.0f, 46f, 1.18f, false, 100000000000, 130000000f)
    };

    static readonly float[] SlotWeights = { 30f, 24f, 18f, 13f, 9f, 6f };

    [InitializeOnLoadMethod]
    static void AutoApplyOnce()
    {
        EditorApplication.delayCall += delegate
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            if (SessionState.GetBool(AppliedKey, false)) return;
            SessionState.SetBool(AppliedKey, true);
            ApplyAllBalance();
        };
    }

    [MenuItem("Tools/Ride a Dino/Apply 51 Dinos & 10 Biomes Balance")]
    public static void ApplyAllBalance()
    {
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder(DinoFolder);
        EnsureFolder(BiomeFolder);
        EnsureFolder(TableFolder);

        // Nạp toàn bộ DinoData hiện có để đối chiếu theo tên file hoặc displayName
        Dictionary<string, DinoData> existingByKey = new Dictionary<string, DinoData>(StringComparer.OrdinalIgnoreCase);
        string[] dinoGuids = AssetDatabase.FindAssets("t:DinoData", new[] { DinoFolder });
        for (int i = 0; i < dinoGuids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(dinoGuids[i]);
            DinoData d = AssetDatabase.LoadAssetAtPath<DinoData>(p);
            if (d == null) continue;
            if (!existingByKey.ContainsKey(d.name)) existingByKey[d.name] = d;
            if (!string.IsNullOrEmpty(d.displayName) && !existingByKey.ContainsKey(d.displayName))
                existingByKey[d.displayName] = d;
        }

        // Bảng mẫu để sao chép danh sách Variants & Sizes cho các SpawnTable mới
        DinoSpawnTable templateTable = AssetDatabase.LoadAssetAtPath<DinoSpawnTable>(TableFolder + "/SpawnTable_Plains.asset");
        if (templateTable == null)
            templateTable = AssetDatabase.LoadAssetAtPath<DinoSpawnTable>(TableFolder + "/SpawnTable_Default.asset");

        // 1. Tạo / cập nhật 10 Biome & 10 SpawnTable
        Dictionary<int, BiomeData> biomeByZone = new Dictionary<int, BiomeData>();
        Dictionary<int, DinoSpawnTable> tableByZone = new Dictionary<int, DinoSpawnTable>();
        List<BiomeData> orderedBiomes = new List<BiomeData>();

        for (int i = 0; i < Biomes.Length; i++)
        {
            BiomeSpec bs = Biomes[i];
            string tPath = TableFolder + "/" + bs.tableFileName;
            DinoSpawnTable tbl = AssetDatabase.LoadAssetAtPath<DinoSpawnTable>(tPath);
            if (tbl == null)
            {
                tbl = ScriptableObject.CreateInstance<DinoSpawnTable>();
                if (templateTable != null)
                {
                    tbl.variants = CloneVariants(templateTable.variants);
                    tbl.sizes = CloneSizes(templateTable.sizes);
                }
                AssetDatabase.CreateAsset(tbl, tPath);
            }
            tbl.entries.Clear();
            tableByZone[bs.zone] = tbl;

            string bPath = BiomeFolder + "/" + bs.fileName;
            BiomeData b = AssetDatabase.LoadAssetAtPath<BiomeData>(bPath);
            if (b == null)
            {
                b = ScriptableObject.CreateInstance<BiomeData>();
                b.cardColor = bs.cardColor;
                b.borderColor = bs.borderColor;
                b.fogColor = bs.fogColor;
                b.overrideFog = true;
                AssetDatabase.CreateAsset(b, bPath);
            }
            b.biomeName = bs.biomeName;
            b.spawnTable = tbl;
            b.indexCompleteCash = Math.Round(bs.completeCash * RewardNerfMultiplier);
            b.indexCompleteStamina = Mathf.Max(5f, Mathf.Round(bs.completeStamina * (float)RewardNerfMultiplier));
            EditorUtility.SetDirty(b);

            biomeByZone[bs.zone] = b;
            orderedBiomes.Add(b);
        }

        // 2. Cập nhật chỉ số 50 Dino & gắn vào đúng SpawnTable của từng Map (theo thứ tự yếu -> mạnh)
        int updatedCount = 0;
        int createdCount = 0;
        for (int i = 0; i < Dinos.Length; i++)
        {
            DinoSpec s = Dinos[i];
            DinoData d;
            if (!existingByKey.TryGetValue(s.name, out d) || d == null)
            {
                string newPath = DinoFolder + "/" + s.name + ".asset";
                d = AssetDatabase.LoadAssetAtPath<DinoData>(newPath);
                if (d == null)
                {
                    d = ScriptableObject.CreateInstance<DinoData>();
                    AssetDatabase.CreateAsset(d, newPath);
                    createdCount++;
                }
            }

            d.displayName = s.name;
            d.diet = s.diet;
            d.rarity = s.rarity;
            d.zone = s.zone;
            d.baseMoney = s.baseMoney;
            d.requiredStamina = s.reqStamina;
            d.tameTime = s.tameTime;
            d.runSpeed = s.runSpeed;
            d.riddenSpeedMultiplier = s.riddenSpeedMul;
            d.killOnStaminaEmptyIfUntamed = s.killOnEmptyIfUntamed;
            double nerfedUnlockCash = Math.Round(s.unlockCash * RewardNerfMultiplier);
            d.indexRewardCash = nerfedUnlockCash;
            d.indexRewardStamina = Mathf.Max(5f, Mathf.Round(s.unlockStamina * (float)RewardNerfMultiplier));
            d.indexAllSizesRewardCash = nerfedUnlockCash * 2.0;
            d.indexAllVariantsRewardCash = nerfedUnlockCash * 8.0;
            EditorUtility.SetDirty(d);
            updatedCount++;

            if (tableByZone.TryGetValue(s.zone, out DinoSpawnTable tbl) && tbl != null)
            {
                int wIdx = Mathf.Clamp(s.slotIndex - 1, 0, SlotWeights.Length - 1);
                tbl.entries.Add(new DinoSpawnTable.Entry
                {
                    dino = d,
                    weight = SlotWeights[wIdx],
                    herdMin = s.slotIndex <= 2 ? 2 : 1,
                    herdMax = s.slotIndex <= 2 ? 4 : 2,
                    speedJitter = 0.5f
                });
                EditorUtility.SetDirty(tbl);
            }
        }

        // 3. Cập nhật danh sách 10 Biome trên DinoIndexPopup trong Scene hiện tại
        DinoIndexPopup indexPopup = UnityEngine.Object.FindObjectOfType<DinoIndexPopup>(true);
        if (indexPopup != null)
        {
            indexPopup.biomes = new List<BiomeData>(orderedBiomes);
            EditorUtility.SetDirty(indexPopup);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã áp dụng bảng cân bằng cho " + updatedCount + " Dino (tạo mới " + createdCount + " asset) và 10 Biome + 10 SpawnTable!");
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        if (slash > 0)
        {
            string parent = folder.Substring(0, slash);
            string child = folder.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static List<DinoSpawnTable.VariantWeight> CloneVariants(List<DinoSpawnTable.VariantWeight> src)
    {
        List<DinoSpawnTable.VariantWeight> list = new List<DinoSpawnTable.VariantWeight>();
        if (src == null) return list;
        for (int i = 0; i < src.Count; i++)
        {
            if (src[i] == null) continue;
            list.Add(new DinoSpawnTable.VariantWeight { variant = src[i].variant, weight = src[i].weight });
        }
        return list;
    }

    static List<DinoSpawnTable.SizeWeight> CloneSizes(List<DinoSpawnTable.SizeWeight> src)
    {
        List<DinoSpawnTable.SizeWeight> list = new List<DinoSpawnTable.SizeWeight>();
        if (src == null) return list;
        for (int i = 0; i < src.Count; i++)
        {
            if (src[i] == null) continue;
            list.Add(new DinoSpawnTable.SizeWeight { sizeAsset = src[i].sizeAsset, sizeType = src[i].sizeType, weight = src[i].weight });
        }
        return list;
    }
}
#endif
