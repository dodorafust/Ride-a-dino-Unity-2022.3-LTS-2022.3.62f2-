using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Dựng sẵn UI dạng object thật trong scene + prefab, để bạn chỉnh bằng tay (anchor, kích thước, icon, font).
// Chạy 1 lần. Muốn dựng lại phải xác nhận xóa Canvas_Main (sẽ mất chỉnh sửa tay).
public static class RideADinoUIBuilder
{
    public const string SpriteDir = "Assets/_Project/Textures/UI";
    public const string UiPrefabDir = "Assets/_Project/Prefabs/UI";
    public const string WorldPrefabDir = "Assets/_Project/Prefabs/World";

    public static WorldLabel LabelPrefab;
    public static FloatingPopup PopupPrefab;
    public static Sprite RingSprite, DiscSprite, BoltSprite, CoinSprite;

    static readonly Color Dark = new Color(0.07f, 0.09f, 0.11f, 0.92f);
    static readonly Color Green = new Color(0.25f, 0.85f, 0.3f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);
    static readonly Color Red = new Color(0.9f, 0.2f, 0.25f);
    static readonly Color Blue = new Color(0.2f, 0.45f, 0.95f);
    static readonly Color Orange = new Color(0.95f, 0.5f, 0.15f);
    static readonly Color Pink = new Color(1f, 0.4f, 0.65f);
    static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 BC = new Vector2(0.5f, 0f);
    static readonly Vector2 ML = new Vector2(0f, 0.5f);
    static readonly Vector2 MR = new Vector2(1f, 0.5f);

    // =============================================================== Menu
    [MenuItem("Tools/Ride a Dino/2. Build UI (Canvas + Prefabs)")]
    public static void BuildUI()
    {
        if (!CheckTMP()) return;

        GameObject old = GameObject.Find("Canvas_Main");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Ride a Dino",
                "Đã có Canvas_Main. Dựng lại sẽ XÓA toàn bộ chỉnh sửa tay trên Canvas hiện tại. Tiếp tục?", "Dựng lại", "Hủy"))
                return;
            Object.DestroyImmediate(old);
        }

        EnsureSprites();
        EnsureWorldAssets();

        HotbarSlotUI slotPrefab = BuildSlotPrefab();
        InventoryCellUI cellPrefab = BuildCellPrefab();
        PenRowUI rowPrefab = BuildRowPrefab();
        BuildCanvas(slotPrefab, cellPrefab, rowPrefab);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Ride a Dino: đã dựng Canvas_Main. Hãy chỉnh UI trực tiếp trong scene / prefab, rồi lưu scene.");
    }

    static bool CheckTMP()
    {
        if (TMP_Settings.instance != null) return true;
        EditorUtility.DisplayDialog("Ride a Dino",
            "Chưa import TextMeshPro Essentials.\n\nVào Window > TextMeshPro > Import TMP Essential Resources, " +
            "bấm Import, rồi chạy lại lệnh này.", "OK");
        return false;
    }

    // =============================================================== Sprites
    delegate Color PixelFn(float u, float v, float px);

    static Sprite GetSprite(string name, int size, PixelFn fn)
    {
        RideADinoSetup.EnsureFolder(SpriteDir);
        string path = SpriteDir + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float p = 1f / size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = fn((x + 0.5f) * p, (y + 0.5f) * p, p);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static readonly Vector2[] BoltPoly =
    {
        new Vector2(0.58f, 0.97f), new Vector2(0.20f, 0.46f), new Vector2(0.44f, 0.46f),
        new Vector2(0.34f, 0.03f), new Vector2(0.80f, 0.58f), new Vector2(0.55f, 0.58f)
    };

    static bool Inside(float x, float y, Vector2[] p)
    {
        bool c = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        {
            if (((p[i].y > y) != (p[j].y > y)) &&
                (x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                c = !c;
        }
        return c;
    }

    public static void EnsureSprites()
    {
        RingSprite = GetSprite("Ring", 256, delegate (float u, float v, float px)
        {
            float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            float a = Mathf.Clamp01((0.5f - d) / px) * Mathf.Clamp01((d - 0.36f) / px);
            return new Color(1f, 1f, 1f, a);
        });

        DiscSprite = GetSprite("Disc", 256, delegate (float u, float v, float px)
        {
            float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01((0.5f - d) / px));
        });

        BoltSprite = GetSprite("Icon_Bolt", 128, delegate (float u, float v, float px)
        {
            int hit = 0;
            for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (Inside(u + (sx - 0.5f) * px * 0.5f, v + (sy - 0.5f) * px * 0.5f, BoltPoly)) hit++;
            return new Color(1f, 0.88f, 0.15f, hit / 4f);
        });

        CoinSprite = GetSprite("Icon_Coin", 128, delegate (float u, float v, float px)
        {
            float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            float a = Mathf.Clamp01((0.48f - d) / px);
            Color col = d > 0.36f ? new Color(0.8f, 0.55f, 0.05f) : new Color(1f, 0.85f, 0.2f);
            col.a = a;
            return col;
        });
    }

    // =============================================================== World prefabs (label + popup)
    public static void EnsureWorldAssets()
    {
        EnsureSprites();
        RideADinoSetup.EnsureFolder(WorldPrefabDir);

        string labelPath = WorldPrefabDir + "/WorldLabel.prefab";
        string popupPath = WorldPrefabDir + "/FloatingPopup.prefab";

        GameObject labelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(labelPath);
        GameObject popupAsset = AssetDatabase.LoadAssetAtPath<GameObject>(popupPath);

        if ((labelAsset == null || popupAsset == null) && !CheckTMP()) return;

        if (labelAsset == null)
        {
            var go = new GameObject("WorldLabel");
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "Label";
            tmp.fontSize = 5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(10f, 4f);
            go.AddComponent<WorldLabel>();
            labelAsset = PrefabUtility.SaveAsPrefabAsset(go, labelPath);
            Object.DestroyImmediate(go);
        }

        if (popupAsset == null)
        {
            var root = new GameObject("FloatingPopup");
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(root.transform, false);
            iconGo.transform.localPosition = new Vector3(-0.7f, 0f, 0f);
            iconGo.transform.localScale = Vector3.one * 0.25f;
            SpriteRenderer sr = iconGo.AddComponent<SpriteRenderer>();
            sr.sprite = BoltSprite;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            TextMeshPro tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = "+1";
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

            FloatingPopup fp = root.AddComponent<FloatingPopup>();
            fp.text = tmp;
            fp.icon = sr;
            popupAsset = PrefabUtility.SaveAsPrefabAsset(root, popupPath);
            Object.DestroyImmediate(root);
        }

        LabelPrefab = labelAsset != null ? labelAsset.GetComponent<WorldLabel>() : null;
        PopupPrefab = popupAsset != null ? popupAsset.GetComponent<FloatingPopup>() : null;
    }

    // =============================================================== UI helpers
    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    static Image NewImage(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = true)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    static TextMeshProUGUI NewText(Transform parent, string name, string text, float size, Color color,
                                   TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        Stretch(rt);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = FontStyles.Bold;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button NewButton(Transform parent, string name, string label, Color color, float fontSize,
                            out TextMeshProUGUI labelText, Sprite sprite = null)
    {
        Image img = NewImage(parent, name, color, sprite, true);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        labelText = NewText(img.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center);
        return b;
    }

    static Button NewButton(Transform parent, string name, string label, Color color, float fontSize)
    {
        TextMeshProUGUI t;
        return NewButton(parent, name, label, color, fontSize, out t);
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        GameObject p = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return p;
    }

    static RectTransform NewScroll(Transform parent, string name, out RectTransform content)
    {
        Image root = NewImage(parent, name, new Color(0f, 0f, 0f, 0.25f), null, true);
        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = NewRect("Viewport", root.transform);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return root.rectTransform;
    }

    static void SetupRadial(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.fillAmount = 1f;
        img.raycastTarget = false;
    }

    // =============================================================== UI prefabs
    static HotbarSlotUI BuildSlotPrefab()
    {
        RideADinoSetup.EnsureFolder(UiPrefabDir);
        var root = new GameObject("HotbarSlot", typeof(RectTransform));
        ((RectTransform)root.transform).sizeDelta = new Vector2(100f, 100f);
        Image border = root.AddComponent<Image>();
        border.color = new Color(0.25f, 0.25f, 0.28f, 0.9f);
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = border;

        Image inner = NewImage(root.transform, "Inner", new Color(0.08f, 0.1f, 0.12f, 0.95f), null, false);
        Stretch(inner.rectTransform, 4f);

        Image icon = NewImage(inner.transform, "Icon", Color.white, null, false);
        Stretch(icon.rectTransform, 12f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TextMeshProUGUI key = NewText(inner.transform, "Key", "", 18f, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.TopLeft);
        key.rectTransform.offsetMin = new Vector2(5f, 0f);
        key.rectTransform.offsetMax = new Vector2(0f, -2f);

        TextMeshProUGUI name = NewText(inner.transform, "Name", "", 19f, Color.white, TextAlignmentOptions.Center);
        Place(name.rectTransform, BC, BC, new Vector2(0f, 3f), new Vector2(92f, 26f));

        TextMeshProUGUI rate = NewText(inner.transform, "Rate", "", 15f, Green, TextAlignmentOptions.Center);
        Place(rate.rectTransform, BC, BC, new Vector2(0f, 28f), new Vector2(92f, 22f));

        HotbarSlotUI ui = root.AddComponent<HotbarSlotUI>();
        ui.button = btn;
        ui.border = border;
        ui.icon = icon;
        ui.nameText = name;
        ui.rateText = rate;
        ui.keyText = key;

        GameObject prefab = SavePrefab(root, UiPrefabDir + "/HotbarSlot.prefab");
        return prefab.GetComponent<HotbarSlotUI>();
    }

    static InventoryCellUI BuildCellPrefab()
    {
        var root = new GameObject("InventoryCell", typeof(RectTransform));
        ((RectTransform)root.transform).sizeDelta = new Vector2(150f, 130f);
        Image border = root.AddComponent<Image>();
        border.color = Color.white;
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = border;

        Image inner = NewImage(root.transform, "Inner", new Color(0.1f, 0.12f, 0.14f, 1f), null, false);
        Stretch(inner.rectTransform, 4f);

        Image icon = NewImage(inner.transform, "Icon", Color.white, null, false);
        Stretch(icon.rectTransform, 16f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TextMeshProUGUI rarity = NewText(inner.transform, "Rarity", "Common", 18f, Color.white, TextAlignmentOptions.Top);
        rarity.rectTransform.offsetMax = new Vector2(0f, -4f);

        TextMeshProUGUI name = NewText(inner.transform, "Name", "Dino", 20f, Color.white, TextAlignmentOptions.Center);
        TextMeshProUGUI rate = NewText(inner.transform, "Rate", "$0/s", 19f, Green, TextAlignmentOptions.Bottom);
        rate.rectTransform.offsetMin = new Vector2(0f, 6f);

        InventoryCellUI ui = root.AddComponent<InventoryCellUI>();
        ui.button = btn;
        ui.border = border;
        ui.icon = icon;
        ui.rarityText = rarity;
        ui.nameText = name;
        ui.rateText = rate;

        GameObject prefab = SavePrefab(root, UiPrefabDir + "/InventoryCell.prefab");
        return prefab.GetComponent<InventoryCellUI>();
    }

    static PenRowUI BuildRowPrefab()
    {
        var root = new GameObject("PenRow", typeof(RectTransform));
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.14f, 0.17f, 1f);
        LayoutElement le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 84f;
        le.minHeight = 84f;

        Image icon = NewImage(root.transform, "Icon", Color.white, null, false);
        Place(icon.rectTransform, ML, ML, new Vector2(10f, 0f), new Vector2(64f, 64f));
        icon.preserveAspect = true;
        icon.enabled = false;

        TextMeshProUGUI title = NewText(root.transform, "Title", "Dino", 28f, Color.white, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, ML, ML, new Vector2(84f, 16f), new Vector2(210f, 36f));

        TextMeshProUGUI rate = NewText(root.transform, "Rate", "$0/s", 24f, Green, TextAlignmentOptions.MidlineLeft);
        Place(rate.rectTransform, ML, ML, new Vector2(84f, -16f), new Vector2(210f, 32f));

        TextMeshProUGUI rideLabel;
        Button ride = NewButton(root.transform, "RideButton", "Ride", Blue, 26f, out rideLabel);
        Place((RectTransform)ride.transform, MR, MR, new Vector2(-160f, 0f), new Vector2(140f, 56f));

        Button un = NewButton(root.transform, "UnequipButton", "Unequip", Red, 26f);
        Place((RectTransform)un.transform, MR, MR, new Vector2(-10f, 0f), new Vector2(140f, 56f));

        PenRowUI ui = root.AddComponent<PenRowUI>();
        ui.icon = icon;
        ui.titleText = title;
        ui.rateText = rate;
        ui.rideButton = ride;
        ui.unequipButton = un;
        ui.rideLabel = rideLabel;

        GameObject prefab = SavePrefab(root, UiPrefabDir + "/PenRow.prefab");
        return prefab.GetComponent<PenRowUI>();
    }

    // =============================================================== Canvas
    static void BuildCanvas(HotbarSlotUI slotPrefab, InventoryCellUI cellPrefab, PenRowUI rowPrefab)
    {
        var canvasGo = new GameObject("Canvas_Main", typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ---- 1. LookPad (phủ toàn màn hình, dưới cùng)
        Image lookPad = NewImage(root, "LookPad", Clear, null, true);
        Stretch(lookPad.rectTransform);
        lookPad.gameObject.AddComponent<LookPad>();

        // ---- 2. Joystick (vùng bên trái, pivot giữa)
        Image joyArea = NewImage(root, "Joystick", Clear, null, true);
        Place(joyArea.rectTransform, BL, C, new Vector2(340f, 330f), new Vector2(620f, 660f));
        Image joyBase = NewImage(joyArea.transform, "Base", new Color(1f, 1f, 1f, 0.35f), RingSprite, false);
        Place(joyBase.rectTransform, C, C, new Vector2(-80f, -70f), new Vector2(240f, 240f));
        Image joyHandle = NewImage(joyBase.transform, "Handle", new Color(1f, 1f, 1f, 0.6f), DiscSprite, false);
        Place(joyHandle.rectTransform, C, C, Vector2.zero, new Vector2(110f, 110f));
        VirtualJoystick vj = joyArea.gameObject.AddComponent<VirtualJoystick>();
        vj.area = joyArea.rectTransform;
        vj.baseImage = joyBase.rectTransform;
        vj.handle = joyHandle.rectTransform;

        // ---- 3. HUD
        RectTransform hud = NewRect("HUD", root);
        Stretch(hud);
        HudController hc = hud.gameObject.AddComponent<HudController>();

        // Chỉ số (trái trên)
        TextMeshProUGUI staminaVal, cashVal;
        MakeStatRow(hud, "StaminaRow", BoltSprite, -20f, out staminaVal);
        MakeStatRow(hud, "CashRow", CoinSprite, -100f, out cashVal);
        hc.staminaText = staminaVal;
        hc.cashText = cashVal;

        // Nút trái: Shop / Index
        Button shop = NewButton(hud, "ShopButton", "Shop", Red, 38f);
        Place((RectTransform)shop.transform, ML, ML, new Vector2(20f, 40f), new Vector2(240f, 84f));
        Button index = NewButton(hud, "IndexButton", "Index", Blue, 38f);
        Place((RectTransform)index.transform, ML, ML, new Vector2(20f, -60f), new Vector2(240f, 84f));
        hc.shopButton = shop;
        hc.indexButton = index;

        // Nút phải: quản lý chuồng / danh sách Dinos
        Button pen = NewButton(hud, "PenButton", "PEN", Orange, 34f);
        Place((RectTransform)pen.transform, MR, MR, new Vector2(-20f, 70f), new Vector2(120f, 120f));
        Button inv = NewButton(hud, "DinosButton", "DINOS", Blue, 30f);
        Place((RectTransform)inv.transform, MR, MR, new Vector2(-20f, -70f), new Vector2(120f, 120f));
        hc.penButton = pen;
        hc.inventoryButton = inv;

        // Nút nhảy / bắt (phải dưới)
        TextMeshProUGUI jumpLabel;
        Button jump = NewButton(hud, "JumpButton", "JUMP", new Color(0.2f, 0.75f, 0.3f, 0.85f), 40f, out jumpLabel, DiscSprite);
        Place((RectTransform)jump.transform, BR, BR, new Vector2(-60f, 80f), new Vector2(220f, 220f));
        jump.gameObject.AddComponent<JumpButton>();

        // Hotbar: ô cưỡi + 6 ô
        RectTransform bar = NewRect("Hotbar", hud);
        Place(bar, BC, BC, new Vector2(0f, 16f), new Vector2(760f, 104f));
        HorizontalLayoutGroup hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8f;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = false;
        hl.childControlHeight = false;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        HotbarSlotUI rideSlot = ((GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, bar)).GetComponent<HotbarSlotUI>();
        rideSlot.name = "RideSlot";
        hc.rideSlot = rideSlot;
        hc.slots = new HotbarSlotUI[6];
        for (int i = 0; i < 6; i++)
        {
            HotbarSlotUI s = ((GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, bar)).GetComponent<HotbarSlotUI>();
            s.name = "Slot" + (i + 1);
            hc.slots[i] = s;
        }

        // Thông báo + gợi ý
        TextMeshProUGUI toast = NewText(hud, "Toast", "", 48f, Color.white, TextAlignmentOptions.Center);
        Place(toast.rectTransform, TC, TC, new Vector2(0f, -120f), new Vector2(1400f, 70f));
        hc.toastText = toast;

        TextMeshProUGUI hint = NewText(hud, "DepositHint", "GO TO YOUR PEN TO DEPOSIT", 36f, Color.white, TextAlignmentOptions.Center);
        Place(hint.rectTransform, BC, BC, new Vector2(0f, 150f), new Vector2(1000f, 60f));
        hc.depositHint = hint.gameObject;

        // Vòng Stamina + Tame
        RectTransform rings = NewRect("RideRings", hud);
        Place(rings, C, C, Vector2.zero, Vector2.zero);
        Image sBg = NewImage(rings, "StaminaRing", new Color(0f, 0f, 0f, 0.55f), RingSprite, false);
        Place(sBg.rectTransform, C, C, new Vector2(170f, 40f), new Vector2(110f, 110f));
        Image sFill = NewImage(sBg.transform, "Fill", Green, RingSprite, false);
        Stretch(sFill.rectTransform);
        SetupRadial(sFill);
        Image tBg = NewImage(rings, "TameRing", new Color(0f, 0f, 0f, 0.55f), DiscSprite, false);
        Place(tBg.rectTransform, C, C, new Vector2(70f, 85f), new Vector2(70f, 70f));
        Image tFill = NewImage(tBg.transform, "Fill", Pink, DiscSprite, false);
        Stretch(tFill.rectTransform, 5f);
        SetupRadial(tFill);
        RideRingsUI rr = rings.gameObject.AddComponent<RideRingsUI>();
        rr.staminaFill = sFill;
        rr.tameFill = tFill;
        hc.rings = rr;

        // Ô "Ride" trên đầu Dino trong chuồng
        Image promptBg = NewImage(hud, "RidePrompt", Dark, null, true);
        Place(promptBg.rectTransform, C, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(340f, 96f));
        Button promptBtn = promptBg.gameObject.AddComponent<Button>();
        promptBtn.targetGraphic = promptBg;
        Image keyBg = NewImage(promptBg.transform, "KeyIcon", new Color(1f, 1f, 1f, 0.25f), DiscSprite, false);
        Place(keyBg.rectTransform, ML, ML, new Vector2(14f, 0f), new Vector2(64f, 64f));
        NewText(keyBg.transform, "KeyText", "E", 34f, Color.white, TextAlignmentOptions.Center);
        TextMeshProUGUI pTitle = NewText(promptBg.transform, "Title", "Dino", 22f, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.MidlineLeft);
        Place(pTitle.rectTransform, ML, ML, new Vector2(94f, 22f), new Vector2(230f, 30f));
        TextMeshProUGUI pAction = NewText(promptBg.transform, "Action", "Ride", 38f, Color.white, TextAlignmentOptions.MidlineLeft);
        Place(pAction.rectTransform, ML, ML, new Vector2(94f, -16f), new Vector2(230f, 44f));
        WorldPromptUI prompt = promptBg.gameObject.AddComponent<WorldPromptUI>();
        prompt.button = promptBtn;
        prompt.titleText = pTitle;
        prompt.actionText = pAction;
        hc.ridePrompt = prompt;

        // ---- 4. Popup Dinosaurs
        Image invPanel = NewImage(root, "InventoryPopup", Dark, null, true);
        Place(invPanel.rectTransform, C, C, new Vector2(0f, 20f), new Vector2(1000f, 700f));
        InventoryPopup ip = invPanel.gameObject.AddComponent<InventoryPopup>();

        TextMeshProUGUI ipTitle = NewText(invPanel.transform, "Title", "Dinosaurs", 48f, Color.white, TextAlignmentOptions.TopLeft);
        Place(ipTitle.rectTransform, TL, TL, new Vector2(24f, -14f), new Vector2(500f, 60f));
        TextMeshProUGUI ipCount = NewText(invPanel.transform, "Count", "Dinos: 0", 28f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.TopLeft);
        Place(ipCount.rectTransform, TL, TL, new Vector2(24f, -72f), new Vector2(400f, 36f));
        Button ipClose = NewButton(invPanel.transform, "CloseButton", "X", Red, 38f);
        Place((RectTransform)ipClose.transform, TR, TR, new Vector2(-14f, -14f), new Vector2(70f, 70f));

        RectTransform ipContent;
        RectTransform ipScroll = NewScroll(invPanel.transform, "Scroll", out ipContent);
        ipScroll.anchorMin = Vector2.zero;
        ipScroll.anchorMax = Vector2.one;
        ipScroll.offsetMin = new Vector2(16f, 90f);
        ipScroll.offsetMax = new Vector2(-16f, -120f);
        GridLayoutGroup grid = ipContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.cellSize = new Vector2(150f, 130f);
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(10, 10, 10, 10);

        TextMeshProUGUI ipFooter = NewText(invPanel.transform, "Footer", "", 28f, Color.white, TextAlignmentOptions.MidlineLeft);
        Place(ipFooter.rectTransform, BL, BL, new Vector2(24f, 16f), new Vector2(700f, 60f));
        Button ipSell = NewButton(invPanel.transform, "SellButton", "Sell", Green, 34f);
        Place((RectTransform)ipSell.transform, BR, BR, new Vector2(-20f, 16f), new Vector2(190f, 64f));

        ip.content = ipContent;
        ip.cellPrefab = cellPrefab;
        ip.countText = ipCount;
        ip.footerText = ipFooter;
        ip.closeButton = ipClose;
        ip.sellButton = ipSell;
        hc.inventoryPopup = ip;

        // ---- 5. Bảng quản lý chuồng (bên phải)
        Image penPanelImg = NewImage(root, "PenPanel", Dark, null, true);
        Place(penPanelImg.rectTransform, MR, MR, new Vector2(-160f, 0f), new Vector2(700f, 780f));
        PenPanel pp = penPanelImg.gameObject.AddComponent<PenPanel>();

        TextMeshProUGUI ppHeader = NewText(penPanelImg.transform, "Header", "0/3 Active", 46f, Color.white, TextAlignmentOptions.TopLeft);
        Place(ppHeader.rectTransform, TL, TL, new Vector2(24f, -14f), new Vector2(380f, 60f));
        TextMeshProUGUI ppStored = NewText(penPanelImg.transform, "Stored", "0 stored", 30f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.TopRight);
        Place(ppStored.rectTransform, TR, TR, new Vector2(-100f, -22f), new Vector2(240f, 40f));
        Button ppClose = NewButton(penPanelImg.transform, "CloseButton", "X", Red, 38f);
        Place((RectTransform)ppClose.transform, TR, TR, new Vector2(-14f, -14f), new Vector2(70f, 70f));

        RectTransform ppContent;
        RectTransform ppScroll = NewScroll(penPanelImg.transform, "Scroll", out ppContent);
        ppScroll.anchorMin = Vector2.zero;
        ppScroll.anchorMax = Vector2.one;
        ppScroll.offsetMin = new Vector2(16f, 100f);
        ppScroll.offsetMax = new Vector2(-16f, -92f);
        VerticalLayoutGroup vl = ppContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 8f;
        vl.padding = new RectOffset(8, 8, 8, 8);
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        Button ppBest = NewButton(penPanelImg.transform, "EquipBestButton", "Equip Best", Green, 34f);
        Place((RectTransform)ppBest.transform, BR, BR, new Vector2(-16f, 16f), new Vector2(260f, 70f));
        Button ppCollect = NewButton(penPanelImg.transform, "CollectButton", "Collect", Yellow, 34f);
        Place((RectTransform)ppCollect.transform, BL, BL, new Vector2(16f, 16f), new Vector2(260f, 70f));

        pp.content = ppContent;
        pp.rowPrefab = rowPrefab;
        pp.headerText = ppHeader;
        pp.storedText = ppStored;
        pp.equipBestButton = ppBest;
        pp.collectButton = ppCollect;
        pp.closeButton = ppClose;
        hc.penPanel = pp;
    }

    static void MakeStatRow(Transform parent, string name, Sprite icon, float y, out TextMeshProUGUI value)
    {
        Image row = NewImage(parent, name, Dark, null, false);
        Place(row.rectTransform, TL, TL, new Vector2(20f, y), new Vector2(320f, 70f));
        Image ic = NewImage(row.transform, "Icon", Color.white, icon, false);
        Place(ic.rectTransform, ML, ML, new Vector2(10f, 0f), new Vector2(52f, 52f));
        ic.preserveAspect = true;
        value = NewText(row.transform, "Value", "0", 40f, Color.white, TextAlignmentOptions.MidlineLeft);
        value.rectTransform.offsetMin = new Vector2(76f, 0f);
        value.rectTransform.offsetMax = new Vector2(-10f, 0f);
    }
}
