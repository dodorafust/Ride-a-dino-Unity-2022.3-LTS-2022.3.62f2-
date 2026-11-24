#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DinoSizeAssetSetup
{
    const string SizesFolder = "Assets/ScriptableObjects/Sizes";
    const string SpawnTablesFolder = "Assets/ScriptableObjects/SpawnTables";

    struct Preset
    {
        public string fileName;
        public string displayName;
        public bool isNormal;
        public float scale;
        public float money;
        public float stamina;
        public int rankBonus;
        public Color color;
        public DinoSizeType legacy;
        public float defaultWeight;
    }

    static readonly Preset[] Presets = new Preset[]
    {
        new Preset { fileName = "Size_Normal",   displayName = "NORMAL",   isNormal = true,  scale = 1.00f, money = 1.00f, stamina = 1.00f, rankBonus =  0, color = Color.white,                     legacy = DinoSizeType.Normal,   defaultWeight = 70f  },
        new Preset { fileName = "Size_Tiny",     displayName = "TINY",     isNormal = false, scale = 0.65f, money = 0.85f, stamina = 1.15f, rankBonus = -1, color = new Color(0.58f, 0.90f, 1.00f),  legacy = DinoSizeType.Tiny,     defaultWeight = 12f  },
        new Preset { fileName = "Size_Big",      displayName = "BIG",      isNormal = false, scale = 1.30f, money = 1.25f, stamina = 1.30f, rankBonus =  1, color = new Color(0.45f, 1.00f, 0.45f),  legacy = DinoSizeType.Big,      defaultWeight = 10f  },
        new Preset { fileName = "Size_Huge",     displayName = "HUGE",     isNormal = false, scale = 1.60f, money = 1.50f, stamina = 1.60f, rankBonus =  2, color = new Color(1.00f, 0.85f, 0.25f),  legacy = DinoSizeType.Huge,     defaultWeight = 5f   },
        new Preset { fileName = "Size_Giant",    displayName = "GIANT",    isNormal = false, scale = 1.95f, money = 2.00f, stamina = 2.00f, rankBonus =  3, color = new Color(1.00f, 0.55f, 0.18f),  legacy = DinoSizeType.Giant,    defaultWeight = 2.5f },
        new Preset { fileName = "Size_Colossal", displayName = "COLOSSAL", isNormal = false, scale = 2.40f, money = 3.00f, stamina = 2.60f, rankBonus =  4, color = new Color(1.00f, 0.25f, 0.25f),  legacy = DinoSizeType.Colossal, defaultWeight = 0.5f }
    };

    [InitializeOnLoadMethod]
    static void AutoEnsureOnce()
    {
        EditorApplication.delayCall += delegate
        {
            if (AssetDatabase.LoadAssetAtPath<DinoSizeAsset>(SizesFolder + "/Size_Normal.asset") == null)
                BuildAndAssign();
        };
    }

    [MenuItem("Tools/Ride a Dino/Build Size Assets & Assign Tables")]
    public static void BuildAndAssign()
    {
        EnsureFolder(SizesFolder);

        var created = new List<(DinoSizeAsset asset, float weight, DinoSizeType legacy)>();
        for (int i = 0; i < Presets.Length; i++)
        {
            Preset p = Presets[i];
            string path = SizesFolder + "/" + p.fileName + ".asset";
            DinoSizeAsset asset = AssetDatabase.LoadAssetAtPath<DinoSizeAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DinoSizeAsset>();
                asset.displayName = p.displayName;
                asset.isNormal = p.isNormal;
                asset.scaleMultiplier = p.scale;
                asset.moneyMultiplier = p.money;
                asset.staminaMultiplier = p.stamina;
                asset.rankBonus = p.rankBonus;
                asset.tagColor = p.color;
                asset.legacyType = p.legacy;
                AssetDatabase.CreateAsset(asset, path);
            }
            created.Add((asset, p.defaultWeight, p.legacy));
        }

        string[] guids = AssetDatabase.FindAssets("t:DinoSpawnTable", new[] { SpawnTablesFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string tablePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            DinoSpawnTable tbl = AssetDatabase.LoadAssetAtPath<DinoSpawnTable>(tablePath);
            if (tbl == null) continue;

            bool needsFill = tbl.sizes == null || tbl.sizes.Count == 0;
            if (!needsFill)
            {
                bool anyAsset = false;
                for (int j = 0; j < tbl.sizes.Count; j++)
                    if (tbl.sizes[j] != null && tbl.sizes[j].sizeAsset != null) { anyAsset = true; break; }
                needsFill = !anyAsset;
            }

            if (needsFill)
            {
                tbl.sizes = new List<DinoSpawnTable.SizeWeight>();
                for (int k = 0; k < created.Count; k++)
                {
                    tbl.sizes.Add(new DinoSpawnTable.SizeWeight
                    {
                        sizeAsset = created[k].asset,
                        sizeType = created[k].legacy,
                        weight = created[k].weight
                    });
                }
                EditorUtility.SetDirty(tbl);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
