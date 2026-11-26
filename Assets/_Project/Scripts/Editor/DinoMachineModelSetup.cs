#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DinoMachineModelSetup
{
    const string DodoFbxPath = "Assets/_Project/Models/Dino Machine/Dodo/Dodo toy.fbx";
    const string CeratoFbxPath = "Assets/_Project/Models/Dino Machine/Cerato/Cerato toy.fbx";
    const string TriceFbxPath = "Assets/_Project/Models/Dino Machine/Trice/Trice toy.fbx";
    const string CarnoFbxPath = "Assets/_Project/Models/Dino Machine/Carno/Carno.fbx";
    const string AlloFbxPath = "Assets/_Project/Models/Dino Machine/Allo/Allo.fbx";
    const string BrontoFbxPath = "Assets/_Project/Models/Dino Machine/Bronto/Bronto.fbx";
    const string AcroFbxPath = "Assets/_Project/Models/Dino Machine/Acrocanthosaurus/Acrocanthosaurus.fbx";
    const string JugaFbxPath = "Assets/_Project/Models/Dino Machine/Juga/Juga.fbx";
    const string Alpha6FbxPath = "Assets/_Project/Models/Dino Machine/Alpha6/Alpha6.fbx";
    const string PrefabFolder = "Assets/_Project/Prefabs/DinoMachine";
    const string DodoPrefabPath = PrefabFolder + "/Machine_Lv1_Dodo.prefab";
    const string CeratoPrefabPath = PrefabFolder + "/Machine_Lv3_Cerato.prefab";
    const string TricePrefabPath = PrefabFolder + "/Machine_Lv4_Trice.prefab";
    const string CarnoPrefabPath = PrefabFolder + "/Machine_Lv5_Carno.prefab";
    const string AlloPrefabPath = PrefabFolder + "/Machine_Lv6_Allo.prefab";
    const string BrontoPrefabPath = PrefabFolder + "/Machine_Lv7_Bronto.prefab";
    const string AcroPrefabPath = PrefabFolder + "/Machine_Lv8_Acrocanthosaurus.prefab";
    const string JugaPrefabPath = PrefabFolder + "/Machine_Lv9_Juga.prefab";
    const string Alpha6PrefabPath = PrefabFolder + "/Machine_Lv10_Alpha6.prefab";
    const string AppliedKey = "RAD_DodoMachinePrefabSetup_v1";
    const string CeratoAppliedKey = "RAD_CeratoMachinePrefabSetup_v1";
    const string TriceAppliedKey = "RAD_TriceMachinePrefabSetup_v1";
    const string CarnoAppliedKey = "RAD_CarnoMachinePrefabSetup_v1";
    const string AlloAppliedKey = "RAD_AlloMachinePrefabSetup_v1";
    const string BrontoAppliedKey = "RAD_BrontoMachinePrefabSetup_v1";
    const string AcroAppliedKey = "RAD_AcroMachinePrefabSetup_v1";
    const string JugaAppliedKey = "RAD_JugaMachinePrefabSetup_v1";
    const string Alpha6AppliedKey = "RAD_Alpha6MachinePrefabSetup_v1";
    const string ReorderBalanceKey = "RAD_DinoMachine10OrderBalance_v1";
    const string RexPrefabPath = PrefabFolder + "/Machine_Lv2_Rex.prefab";

    [InitializeOnLoadMethod]
    static void AutoSetupOnce()
    {
        EditorApplication.delayCall += delegate
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            if (!SessionState.GetBool(AppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(DodoFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(DodoPrefabPath) == null)
            {
                SessionState.SetBool(AppliedKey, true);
                BuildAndAssignDodoMachine();
            }

            if (!SessionState.GetBool(CeratoAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(CeratoFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(CeratoPrefabPath) == null)
            {
                SessionState.SetBool(CeratoAppliedKey, true);
                BuildAndAssignCeratoMachine();
            }

            if (!SessionState.GetBool(TriceAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(TriceFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(TricePrefabPath) == null)
            {
                SessionState.SetBool(TriceAppliedKey, true);
                BuildAndAssignTriceMachine();
            }

            if (!SessionState.GetBool(CarnoAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(CarnoFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(CarnoPrefabPath) == null)
            {
                SessionState.SetBool(CarnoAppliedKey, true);
                BuildAndAssignCarnoMachine();
            }

            if (!SessionState.GetBool(AlloAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(AlloFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(AlloPrefabPath) == null)
            {
                SessionState.SetBool(AlloAppliedKey, true);
                BuildAndAssignAlloMachine();
            }

            if (!SessionState.GetBool(BrontoAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(BrontoFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(BrontoPrefabPath) == null)
            {
                SessionState.SetBool(BrontoAppliedKey, true);
                BuildAndAssignBrontoMachine();
            }

            if (!SessionState.GetBool(AcroAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(AcroFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(AcroPrefabPath) == null)
            {
                SessionState.SetBool(AcroAppliedKey, true);
                BuildAndAssignAcroMachine();
            }

            if (!SessionState.GetBool(JugaAppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(JugaFbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(JugaPrefabPath) == null)
            {
                SessionState.SetBool(JugaAppliedKey, true);
                BuildAndAssignJugaMachine();
            }

            if (!SessionState.GetBool(Alpha6AppliedKey, false)
                && AssetDatabase.LoadAssetAtPath<GameObject>(Alpha6FbxPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(Alpha6PrefabPath) == null)
            {
                SessionState.SetBool(Alpha6AppliedKey, true);
                BuildAndAssignAlpha6Machine();
            }

            if (!SessionState.GetBool(ReorderBalanceKey, false))
            {
                SessionState.SetBool(ReorderBalanceKey, true);
                ReorderAndBalance10Machines();
            }
        };
    }

    struct MachineBalanceRow
    {
        public string displayName;
        public string prefabPath;
        public float staminaPerSecond;
        public double upgradeCost;
    }

    [MenuItem("Tools/Ride a Dino/Reorder & Balance 10 Dino Machines")]
    public static void ReorderAndBalance10Machines()
    {
        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy DinoMachine trong Scene hiện tại.");
            return;
        }

        // Thứ tự 10 máy theo bảng người dùng gửi + chỉ số cân bằng sát bản mẫu game gốc
        MachineBalanceRow[] rows = new MachineBalanceRow[]
        {
            new MachineBalanceRow { displayName = "Dodo",             prefabPath = DodoPrefabPath,   staminaPerSecond = 1f,    upgradeCost = 0 },                // Lv 1  - Common (Free / Tutorial)
            new MachineBalanceRow { displayName = "Triceratops",      prefabPath = TricePrefabPath,  staminaPerSecond = 2f,    upgradeCost = 250 },              // Lv 2  - Common
            new MachineBalanceRow { displayName = "Ceratosaurus",     prefabPath = CeratoPrefabPath, staminaPerSecond = 5f,    upgradeCost = 8000 },             // Lv 3  - Uncommon (+5)
            new MachineBalanceRow { displayName = "Carnotaurus",      prefabPath = CarnoPrefabPath,  staminaPerSecond = 12f,   upgradeCost = 250000 },           // Lv 4  - Rare (+12)
            new MachineBalanceRow { displayName = "Allosaurus",       prefabPath = AlloPrefabPath,   staminaPerSecond = 30f,   upgradeCost = 5000000 },          // Lv 5  - Epic (+30 / $5M)
            new MachineBalanceRow { displayName = "T-Rex",            prefabPath = RexPrefabPath,    staminaPerSecond = 80f,   upgradeCost = 120000000 },        // Lv 6  - Legendary (+80 / $120M)
            new MachineBalanceRow { displayName = "Brontosaurus",     prefabPath = BrontoPrefabPath, staminaPerSecond = 100f,  upgradeCost = 3000000000 },       // Lv 7  - Mythic (+100 / $3B)
            new MachineBalanceRow { displayName = "Acrocanthosaurus", prefabPath = AcroPrefabPath,   staminaPerSecond = 200f,  upgradeCost = 75000000000 },      // Lv 8  - Secret (+200 / $75B)
            new MachineBalanceRow { displayName = "Juga",             prefabPath = JugaPrefabPath,   staminaPerSecond = 500f,  upgradeCost = 3000000000000 },    // Lv 9  - Eternal (+500 / $3T)
            new MachineBalanceRow { displayName = "Alpha 6",          prefabPath = Alpha6PrefabPath, staminaPerSecond = 1200f, upgradeCost = 100000000000000 }   // Lv 10 - Divine (+1200 / $100T)
        };

        // Lưu lại các config cũ theo model để giữ nguyên icon/vfx/springBendAxis nếu người dùng đã chỉnh
        System.Collections.Generic.Dictionary<GameObject, MachineLevelConfig> oldByModel =
            new System.Collections.Generic.Dictionary<GameObject, MachineLevelConfig>();
        if (machine.levels != null)
        {
            for (int i = 0; i < machine.levels.Length; i++)
            {
                if (machine.levels[i] != null && machine.levels[i].model != null && !oldByModel.ContainsKey(machine.levels[i].model))
                    oldByModel[machine.levels[i].model] = machine.levels[i];
            }
        }

        MachineLevelConfig[] newLevels = new MachineLevelConfig[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rows[i].prefabPath);
            MachineLevelConfig cfg = new MachineLevelConfig();
            if (prefab != null && oldByModel.TryGetValue(prefab, out MachineLevelConfig prev))
            {
                cfg.icon = prev.icon;
                cfg.seatOverride = prev.seatOverride;
                cfg.springBonePrefix = string.IsNullOrEmpty(prev.springBonePrefix) ? "Spring_" : prev.springBonePrefix;
                cfg.springBendAxis = prev.springBendAxis.sqrMagnitude > 0.001f ? prev.springBendAxis : new Vector3(1f, 0f, 0f);
                cfg.vfx = prev.vfx;
                cfg.upgradeBurstVfx = prev.upgradeBurstVfx;
            }
            else
            {
                cfg.springBonePrefix = "Spring_";
                cfg.springBendAxis = new Vector3(1f, 0f, 0f);
            }
            cfg.displayName = rows[i].displayName;
            cfg.model = prefab;
            cfg.staminaPerSecond = rows[i].staminaPerSecond;
            cfg.upgradeCost = rows[i].upgradeCost;
            newLevels[i] = cfg;
        }

        machine.levels = newLevels;
        machine.maxLevel = rows.Length;
        EditorUtility.SetDirty(machine);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("[Ride a Dino] Đã sắp xếp lại đúng thứ tự 10 Dino Machine (Dodo -> Trice -> Cerato -> Carno -> Allo -> Rex -> Bronto -> Acro -> Juga -> Alpha) và cân bằng chỉ số!");
    }

    [MenuItem("Tools/Ride a Dino/Build Alpha6 DinoMachine Prefab & Assign Lv10")]
    public static void BuildAndAssignAlpha6Machine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Alpha6FbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + Alpha6FbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv10_Alpha6");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Alpha6_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, Alpha6PrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 10)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 10);
                for (int i = oldLen; i < 10; i++)
                {
                    float prevStam = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].staminaPerSecond : 160f;
                    double prevCost = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].upgradeCost : 405000;
                    machine.levels[i] = new MachineLevelConfig
                    {
                        staminaPerSecond = Mathf.Round(prevStam * 2f),
                        upgradeCost = Math.Round(prevCost * 3.0)
                    };
                }
            }
            if (machine.levels[9] == null)
                machine.levels[9] = new MachineLevelConfig();

            machine.levels[9].displayName = "Alpha 6";
            machine.levels[9].model = savedPrefab;
            machine.levels[9].springBonePrefix = "Spring_";
            if (machine.levels[9].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[9].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + Alpha6PrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và mở rộng + gán vào Level 10 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Juga DinoMachine Prefab & Assign Lv9")]
    public static void BuildAndAssignJugaMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(JugaFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + JugaFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv9_Juga");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Juga_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, JugaPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 9)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 9);
                for (int i = oldLen; i < 9; i++)
                {
                    float prevStam = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].staminaPerSecond : 80f;
                    double prevCost = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].upgradeCost : 135000;
                    machine.levels[i] = new MachineLevelConfig
                    {
                        staminaPerSecond = Mathf.Round(prevStam * 2f),
                        upgradeCost = Math.Round(prevCost * 3.0)
                    };
                }
            }
            if (machine.levels[8] == null)
                machine.levels[8] = new MachineLevelConfig();

            machine.levels[8].displayName = "Juga";
            machine.levels[8].model = savedPrefab;
            machine.levels[8].springBonePrefix = "Spring_";
            if (machine.levels[8].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[8].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + JugaPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và mở rộng + gán vào Level 9 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Acrocanthosaurus DinoMachine Prefab & Assign Lv8")]
    public static void BuildAndAssignAcroMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AcroFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + AcroFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv8_Acrocanthosaurus");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Acrocanthosaurus_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, AcroPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 8)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 8);
                for (int i = oldLen; i < 8; i++)
                {
                    float prevStam = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].staminaPerSecond : 40f;
                    double prevCost = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].upgradeCost : 45000;
                    machine.levels[i] = new MachineLevelConfig
                    {
                        staminaPerSecond = Mathf.Round(prevStam * 2f),
                        upgradeCost = Math.Round(prevCost * 3.0)
                    };
                }
            }
            if (machine.levels[7] == null)
                machine.levels[7] = new MachineLevelConfig();

            machine.levels[7].displayName = "Acrocanthosaurus";
            machine.levels[7].model = savedPrefab;
            machine.levels[7].springBonePrefix = "Spring_";
            if (machine.levels[7].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[7].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + AcroPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và mở rộng + gán vào Level 8 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Bronto DinoMachine Prefab & Assign Lv7")]
    public static void BuildAndAssignBrontoMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(BrontoFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + BrontoFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv7_Bronto");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Bronto_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, BrontoPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 7)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 7);
                for (int i = oldLen; i < 7; i++)
                {
                    float prevStam = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].staminaPerSecond : 20f;
                    double prevCost = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].upgradeCost : 15000;
                    machine.levels[i] = new MachineLevelConfig
                    {
                        staminaPerSecond = Mathf.Round(prevStam * 2f),
                        upgradeCost = Math.Round(prevCost * 3.0)
                    };
                }
            }
            if (machine.levels[6] == null)
                machine.levels[6] = new MachineLevelConfig();

            machine.levels[6].displayName = "Brontosaurus";
            machine.levels[6].model = savedPrefab;
            machine.levels[6].springBonePrefix = "Spring_";
            if (machine.levels[6].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[6].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + BrontoPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và mở rộng + gán vào Level 7 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Allo DinoMachine Prefab & Assign Lv6")]
    public static void BuildAndAssignAlloMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AlloFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + AlloFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv6_Allo");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Allo_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, AlloPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 6)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 6);
                for (int i = oldLen; i < 6; i++)
                {
                    float prevStam = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].staminaPerSecond : 10f;
                    double prevCost = (i > 0 && machine.levels[i - 1] != null) ? machine.levels[i - 1].upgradeCost : 5000;
                    machine.levels[i] = new MachineLevelConfig
                    {
                        staminaPerSecond = Mathf.Round(prevStam * 2f),
                        upgradeCost = Math.Round(prevCost * 3.0)
                    };
                }
            }
            if (machine.levels[5] == null)
                machine.levels[5] = new MachineLevelConfig();

            machine.levels[5].displayName = "Allosaurus";
            machine.levels[5].model = savedPrefab;
            machine.levels[5].springBonePrefix = "Spring_";
            if (machine.levels[5].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[5].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + AlloPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và mở rộng + gán vào Level 6 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Carno DinoMachine Prefab & Assign Lv5")]
    public static void BuildAndAssignCarnoMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CarnoFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + CarnoFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv5_Carno");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Carno_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, CarnoPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 5)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 5);
                for (int i = oldLen; i < 5; i++)
                    machine.levels[i] = new MachineLevelConfig();
            }
            if (machine.levels[4] == null)
                machine.levels[4] = new MachineLevelConfig();

            machine.levels[4].displayName = "Carnotaurus";
            machine.levels[4].model = savedPrefab;
            machine.levels[4].springBonePrefix = "Spring_";
            if (machine.levels[4].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[4].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + CarnoPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và gán vào Level 5 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Trice DinoMachine Prefab & Assign Lv4")]
    public static void BuildAndAssignTriceMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(TriceFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + TriceFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv4_Trice");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Trice_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, TricePrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 4)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 4);
                for (int i = oldLen; i < 4; i++)
                    machine.levels[i] = new MachineLevelConfig();
            }
            if (machine.levels[3] == null)
                machine.levels[3] = new MachineLevelConfig();

            machine.levels[3].displayName = "Triceratops";
            machine.levels[3].model = savedPrefab;
            machine.levels[3].springBonePrefix = "Spring_";
            if (machine.levels[3].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[3].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + TricePrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và gán vào Level 4 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Cerato DinoMachine Prefab & Assign Lv3")]
    public static void BuildAndAssignCeratoMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CeratoFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + CeratoFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        GameObject tempRoot = new GameObject("Machine_Lv3_Cerato");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Cerato_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        modelInst.transform.localScale = new Vector3(1.34f, 1.34f, 1.34f);

        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);
        seatGo.transform.position = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, CeratoPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length < 3)
            {
                int oldLen = machine.levels != null ? machine.levels.Length : 0;
                Array.Resize(ref machine.levels, 3);
                for (int i = oldLen; i < 3; i++)
                    machine.levels[i] = new MachineLevelConfig();
            }
            if (machine.levels[2] == null)
                machine.levels[2] = new MachineLevelConfig();

            machine.levels[2].displayName = "Cerato";
            machine.levels[2].model = savedPrefab;
            machine.levels[2].springBonePrefix = "Spring_";
            if (machine.levels[2].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[2].springBendAxis = new Vector3(1f, 0f, 0f);

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + CeratoPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và gán vào Level 3 của DinoMachine!");
    }

    [MenuItem("Tools/Ride a Dino/Build Dodo DinoMachine Prefab & Assign Lv1")]
    public static void BuildAndAssignDodoMachine()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(DodoFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy model tại: " + DodoFbxPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        // 1. Tạo Prefab Machine_Lv1_Dodo gộp trọn bộ (Đế + Lò xo Spring_01..Spring_07 + Thân Dodo có yên + điểm Seat)
        GameObject tempRoot = new GameObject("Machine_Lv1_Dodo");
        GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, tempRoot.transform);
        modelInst.name = "Dodo_Model";
        modelInst.transform.localPosition = Vector3.zero;
        modelInst.transform.localRotation = Quaternion.identity;
        modelInst.transform.localScale = Vector3.one;

        // Tìm xương đỉnh lò xo (Spring_07) để gắn điểm Seat làm con -> khi lò xo uốn, nhân vật ngồi trên yên nhún theo
        Transform topSpringBone = FindHighestSpringBone(modelInst.transform, "Spring_");
        Transform seatParent = topSpringBone != null ? topSpringBone : modelInst.transform;

        GameObject seatGo = new GameObject("Seat");
        seatGo.transform.SetParent(seatParent, false);

        // Ước lượng vị trí mặt yên dựa theo Bounds của mesh Saddle (nếu có) hoặc toàn bộ model
        Vector3 seatWorldGuess = EstimateSaddleSeatPosition(modelInst.transform);
        seatGo.transform.position = seatWorldGuess;
        seatGo.transform.rotation = tempRoot.transform.rotation;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, DodoPrefabPath);
        UnityEngine.Object.DestroyImmediate(tempRoot);

        // 2. Gắn vào Level 1 (levels[0]) của DinoMachine trong Scene
        DinoMachine machine = UnityEngine.Object.FindObjectOfType<DinoMachine>();
        if (machine != null && savedPrefab != null)
        {
            if (machine.levels == null || machine.levels.Length == 0)
            {
                machine.levels = new MachineLevelConfig[1];
                machine.levels[0] = new MachineLevelConfig
                {
                    displayName = "Dodo",
                    staminaPerSecond = Mathf.Max(1f, machine.baseStaminaPerSecond),
                    upgradeCost = 0
                };
            }
            if (machine.levels[0] == null)
                machine.levels[0] = new MachineLevelConfig();

            machine.levels[0].displayName = string.IsNullOrEmpty(machine.levels[0].displayName) ? "Dodo" : machine.levels[0].displayName;
            machine.levels[0].model = savedPrefab;
            machine.levels[0].springBonePrefix = "Spring_";
            if (machine.levels[0].springBendAxis.sqrMagnitude < 0.001f)
                machine.levels[0].springBendAxis = new Vector3(1f, 0f, 0f);

            // Ẩn mesh cũ tạm (Base/Spring cũ) trên DinoMachine nếu có để không đè lên Prefab mới
            HideLegacyPlaceholderChild(machine.transform, "Base");
            HideLegacyPlaceholderChild(machine.transform, "Spring");

            EditorUtility.SetDirty(machine);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ride a Dino] Đã tạo Prefab " + DodoPrefabPath + " (có sẵn Seat gắn trên xương đỉnh lò xo) và gán vào Level 1 của DinoMachine!");
    }

    static Transform FindHighestSpringBone(Transform root, string prefix)
    {
        Transform best = null;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (best == null || string.Compare(all[i].name, best.name, StringComparison.OrdinalIgnoreCase) > 0)
                best = all[i];
        }
        return best;
    }

    static Vector3 EstimateSaddleSeatPosition(Transform root)
    {
        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] != null && rs[i].name.IndexOf("Saddle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Bounds b = rs[i].bounds;
                return new Vector3(b.center.x, b.max.y + 0.1f, b.center.z);
            }
        }
        bool hasAny = false;
        Bounds total = default(Bounds);
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            if (!hasAny) { total = rs[i].bounds; hasAny = true; }
            else total.Encapsulate(rs[i].bounds);
        }
        return hasAny ? new Vector3(total.center.x, total.center.y + total.extents.y * 0.45f, total.center.z) : new Vector3(0f, 1.8f, 0f);
    }

    static void HideLegacyPlaceholderChild(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) t.gameObject.SetActive(false);
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        if (slash > 0)
        {
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }
    }
}
#endif
