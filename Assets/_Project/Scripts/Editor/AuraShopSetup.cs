#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuraShopSetup
{
    const string AuraFolder = "Assets/ScriptableObjects/Auras";

    struct Preset
    {
        public string fileName;
        public string id;
        public string displayName;
        public int level;
        public Rarity rarity;
        public float staminaMul;
        public double price;
        public bool unlocked;
        public Color auraCol;
        public Color topCol;
        public Color botCol;
    }

    static readonly Preset[] Presets = new Preset[]
    {
        new Preset
        {
            fileName = "Aura_01_Silver.asset", id = "silver_aura", displayName = "Silver Aura",
            level = 1, rarity = Rarity.Common, staminaMul = 1.5f, price = 0, unlocked = true,
            auraCol = new Color(0.85f, 0.88f, 0.94f, 1f),
            topCol  = new Color(0.92f, 0.94f, 0.98f, 1f),
            botCol  = new Color(0.58f, 0.62f, 0.70f, 1f)
        },
        new Preset
        {
            fileName = "Aura_02_Lime.asset", id = "lime_aura", displayName = "Lime Aura",
            level = 2, rarity = Rarity.Common, staminaMul = 2.0f, price = 5000, unlocked = false,
            auraCol = new Color(0.32f, 0.96f, 0.22f, 1f),
            topCol  = new Color(0.48f, 0.98f, 0.32f, 1f),
            botCol  = new Color(0.18f, 0.66f, 0.14f, 1f)
        },
        new Preset
        {
            fileName = "Aura_03_Cyan.asset", id = "cyan_aura", displayName = "Cyan Aura",
            level = 3, rarity = Rarity.Uncommon, staminaMul = 2.5f, price = 75000, unlocked = false,
            auraCol = new Color(0.18f, 0.90f, 0.98f, 1f),
            topCol  = new Color(0.35f, 0.95f, 1.00f, 1f),
            botCol  = new Color(0.10f, 0.56f, 0.76f, 1f)
        },
        new Preset
        {
            fileName = "Aura_04_Cobalt.asset", id = "cobalt_aura", displayName = "Cobalt Aura",
            level = 4, rarity = Rarity.Uncommon, staminaMul = 3.0f, price = 1500000, unlocked = false,
            auraCol = new Color(0.24f, 0.42f, 0.98f, 1f),
            topCol  = new Color(0.34f, 0.52f, 1.00f, 1f),
            botCol  = new Color(0.14f, 0.24f, 0.74f, 1f)
        },
        new Preset
        {
            fileName = "Aura_05_Violet.asset", id = "violet_aura", displayName = "Violet Aura",
            level = 5, rarity = Rarity.Rare, staminaMul = 4.0f, price = 15000000, unlocked = false,
            auraCol = new Color(0.72f, 0.26f, 0.98f, 1f),
            topCol  = new Color(0.80f, 0.38f, 1.00f, 1f),
            botCol  = new Color(0.44f, 0.14f, 0.72f, 1f)
        },
        new Preset
        {
            fileName = "Aura_06_Crimson.asset", id = "crimson_aura", displayName = "Crimson Aura",
            level = 6, rarity = Rarity.Epic, staminaMul = 5.5f, price = 120000000, unlocked = false,
            auraCol = new Color(0.98f, 0.20f, 0.24f, 1f),
            topCol  = new Color(1.00f, 0.34f, 0.34f, 1f),
            botCol  = new Color(0.64f, 0.10f, 0.14f, 1f)
        },
        new Preset
        {
            fileName = "Aura_07_Golden.asset", id = "golden_aura", displayName = "Golden Aura",
            level = 7, rarity = Rarity.Legendary, staminaMul = 8.0f, price = 1000000000, unlocked = false,
            auraCol = new Color(1.00f, 0.84f, 0.18f, 1f),
            topCol  = new Color(1.00f, 0.90f, 0.34f, 1f),
            botCol  = new Color(0.76f, 0.52f, 0.08f, 1f)
        },
        new Preset
        {
            fileName = "Aura_08_Cosmic.asset", id = "cosmic_aura", displayName = "Cosmic Aura",
            level = 8, rarity = Rarity.Mythic, staminaMul = 12.0f, price = 10000000000, unlocked = false,
            auraCol = new Color(1.00f, 0.32f, 0.82f, 1f),
            topCol  = new Color(1.00f, 0.45f, 0.88f, 1f),
            botCol  = new Color(0.56f, 0.12f, 0.62f, 1f)
        }
    };

    [InitializeOnLoadMethod]
    static void AutoSetupOnce()
    {
        EditorApplication.delayCall += delegate
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            bool needAssets = !AssetDatabase.IsValidFolder(AuraFolder);
            bool needShop = Object.FindObjectOfType<AuraShop>() == null;
            bool needPopup = Object.FindObjectOfType<AurasPopup>() == null;
            PlayerController pc = Object.FindObjectOfType<PlayerController>();
            bool needPlayerCtrl = pc != null && pc.GetComponent<PlayerAuraController>() == null;

            if (needAssets || needShop || needPopup || needPlayerCtrl)
                BuildAurasShopAndPopup();
        };
    }

    [MenuItem("Tools/Ride a Dino/Build Auras Shop & Popup (Scene + Assets)")]
    public static void BuildAurasShopAndPopup()
    {
        List<AuraData> createdOrLoaded = EnsureAuraAssets();

        // 1. Gắn PlayerAuraController + AuraVfxAnchor lên Player trong Scene
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (player != null)
        {
            PlayerAuraController auraCtrl = player.GetComponent<PlayerAuraController>();
            if (auraCtrl == null)
                auraCtrl = Undo.AddComponent<PlayerAuraController>(player.gameObject);
            auraCtrl.EnsureAnchor();
            EditorUtility.SetDirty(player.gameObject);
        }

        // 2. Tạo quầy AuraShop (AURAS) trong Scene cạnh quầy SellDinoShop
        AuraShop shop = Object.FindObjectOfType<AuraShop>();
        if (shop == null)
        {
            GameObject shopGo = new GameObject("AuraShop");
            Undo.RegisterCreatedObjectUndo(shopGo, "Create AuraShop");
            SellDinoShop sellShop = Object.FindObjectOfType<SellDinoShop>();
            if (sellShop != null)
            {
                // Đặt gần SellDinoShop giống ảnh mẫu 1 (người chơi có thể kéo chỉnh vị trí tùy ý trong Scene)
                shopGo.transform.position = sellShop.transform.position + new Vector3(6.8f, 0f, -4.6f);
                shopGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            else
            {
                shopGo.transform.position = new Vector3(-7.1f, -2.1f, -45.6f);
                shopGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            shop = shopGo.AddComponent<AuraShop>();
        }
        shop.BuildVisualsInEditor();
        EditorUtility.SetDirty(shop.gameObject);

        // 3. Dựng sẵn AurasPopupPanel dưới Canvas_Main để người dùng chỉnh trực tiếp ngoài Edit Mode
        GameObject canvasGo = GameObject.Find("Canvas_Main");
        if (canvasGo != null)
        {
            AurasPopup popup = canvasGo.GetComponent<AurasPopup>();
            if (popup == null)
                popup = Undo.AddComponent<AurasPopup>(canvasGo);

            popup.auras = new List<AuraData>(createdOrLoaded);
            popup.RebuildCardsInEditor();

            // Ẩn mặc định trong Scene để không che màn hình, muốn sửa chỉ cần tích bật AurasPopupPanel trong Hierarchy
            if (popup.modalBackdrop != null) popup.modalBackdrop.gameObject.SetActive(false);
            if (popup.panelRoot != null) popup.panelRoot.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvasGo);
        }

        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("[Ride a Dino] Đã tạo bộ AuraData trong " + AuraFolder + ", dựng quầy AuraShop, điểm gắn AuraVfxAnchor trên Player và bảng AurasPopupPanel trong Canvas_Main.");
    }

    static List<AuraData> EnsureAuraAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(AuraFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Auras");

        List<AuraData> list = new List<AuraData>();
        for (int i = 0; i < Presets.Length; i++)
        {
            Preset p = Presets[i];
            string path = AuraFolder + "/" + p.fileName;
            AuraData asset = AssetDatabase.LoadAssetAtPath<AuraData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AuraData>();
                asset.auraId = p.id;
                asset.displayName = p.displayName;
                asset.level = p.level;
                asset.rarity = p.rarity;
                asset.staminaMultiplier = p.staminaMul;
                asset.priceCash = p.price;
                asset.unlockedByDefault = p.unlocked;
                asset.auraColor = p.auraCol;
                asset.cardTopColor = p.topCol;
                asset.cardBottomColor = p.botCol;
                AssetDatabase.CreateAsset(asset, path);
            }
            list.Add(asset);
        }
        return list;
    }
}
#endif
