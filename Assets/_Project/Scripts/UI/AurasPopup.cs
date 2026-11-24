using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bảng Shop Auras (AURAS): Mua, Equip, Unequip các Aura nhân Stamina & các buff mở rộng.
// UI được dựng sẵn trong Scene (Tools > Ride a Dino > Build Auras Shop & Popup) để bạn tự do chỉnh sửa trong Edit Mode.
public class AurasPopup : MonoBehaviour
{
    public static AurasPopup Instance { get; private set; }

    [Header("1. Danh sách Aura bán trong Shop (Tự động nạp từ Assets/ScriptableObjects/Auras nếu để trống)")]
    public List<AuraData> auras = new List<AuraData>();
    [Tooltip("Bật: Tự động tìm và thêm các file AuraData mới tạo trong Project vào danh sách Shop.")]
    public bool autoIncludeAllAuraAssets = true;

    [Header("2. Tham chiếu UI (Dựng sẵn trong Scene để chỉnh sửa trực tiếp)")]
    public Image modalBackdrop;
    public RectTransform panelRoot;
    public Image headerBarImage;
    public Image headerIconImage;
    public TMP_Text headerTitleText;
    public Button closeButton;
    public ScrollRect scrollRect;
    public RectTransform cardsContent;
    public Scrollbar horizontalScrollbar;

    [Header("3. Màu sắc & Giao diện Thẻ")]
    public bool hideOnStart = true;
    public bool closeOnBackdropClick = false;
    public Color modalDimColor = new Color(0f, 0f, 0f, 0.58f);
    public Color headerBarColor = new Color(0.74f, 0.72f, 0.94f, 1f);
    public Color panelBgColor = new Color(0.18f, 0.17f, 0.22f, 0.98f);
    public Vector2 cardSize = new Vector2(236f, 470f);
    public Color buyButtonColor = new Color(0.32f, 0.88f, 0.16f, 1f);
    public Color buyLockedColor = new Color(0.46f, 0.50f, 0.54f, 1f);
    public Color equipButtonColor = new Color(0.32f, 0.88f, 0.16f, 1f);
    public Color unequipButtonColor = new Color(0.92f, 0.34f, 0.22f, 1f);
    public Color equippedCardBorderColor = new Color(1f, 0.96f, 0.35f, 1f);
    public Color normalCardBorderColor = new Color(0.06f, 0.06f, 0.08f, 1f);

    [Header("4. Nội dung chữ & Thời gian hiệu ứng")]
    public string windowTitle = "AURAS";
    public string equipButtonLabel = "Equip";
    public string unequipButtonLabel = "Unequip";
    public float openBounceDuration = 0.28f;

    class AuraCardView
    {
        public AuraData data;
        public RectTransform root;
        public Image outerBorder;
        public Image cardBg;
        public TMP_Text nameText;
        public TMP_Text levelRarityText;
        public Image previewGlowBg;
        public Image previewIcon;
        public TMP_Text buffText;
        public Button actionButton;
        public Image actionButtonBg;
        public TMP_Text actionButtonText;
    }

    readonly List<AuraCardView> cardViews = new List<AuraCardView>();
    readonly HashSet<AuraData> ownedAuras = new HashSet<AuraData>();

    GameManager gm;
    bool subscribed;
    bool initialized;
    bool wired;
    Coroutine popRoutine;

    static Sprite auraCharacterFallbackSprite;
    static Sprite auraFlamesFallbackSprite;
    static Sprite lightningSprite;

    public bool Visible => panelRoot != null && panelRoot.gameObject.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        gm = GameManager.Instance;
        EnsureSceneUI();
        WireButtonsOnce();
        LoadAuraCatalog();
        RebuildCards();

        if (hideOnStart)
            SetVisibleImmediate(false);
        else
            RefreshAllCards();
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this) Instance = null;
    }

    void Subscribe()
    {
        if (subscribed) return;
        if (gm == null) gm = GameManager.Instance;
        if (gm != null)
        {
            gm.StatsChanged += RefreshAllCards;
            subscribed = true;
        }
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        if (gm != null) gm.StatsChanged -= RefreshAllCards;
        subscribed = false;
    }

    public bool IsOwned(AuraData aura)
    {
        if (aura == null) return false;
        return aura.unlockedByDefault || aura.priceCash <= 0 || ownedAuras.Contains(aura);
    }

    public void Open()
    {
        EnsureSceneUI();
        WireButtonsOnce();
        LoadAuraCatalog();
        if (cardViews.Count == 0) RebuildCards();

        SetVisibleImmediate(true);
        Subscribe();
        RefreshAllCards();

        if (panelRoot != null)
            popRoutine = UIEffectUtil.PlaySpringPop(this, panelRoot, popRoutine, openBounceDuration, 0.72f, 1.06f);
    }

    public void Hide()
    {
        Unsubscribe();
        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }
        if (panelRoot != null) panelRoot.localScale = Vector3.one;
        SetVisibleImmediate(false);
    }

    public void Toggle()
    {
        if (Visible) Hide();
        else Open();
    }

    void SetVisibleImmediate(bool v)
    {
        if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(v);
        if (panelRoot != null) panelRoot.gameObject.SetActive(v);
    }

    void LoadAuraCatalog()
    {
        if (autoIncludeAllAuraAssets || auras.Count == 0)
        {
            AuraData[] loaded = Resources.FindObjectsOfTypeAll<AuraData>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && !auras.Contains(loaded[i]))
                    auras.Add(loaded[i]);
            }
        }

        auras.RemoveAll(a => a == null);
        auras.Sort((a, b) =>
        {
            int cmp = a.level.CompareTo(b.level);
            if (cmp != 0) return cmp;
            return a.priceCash.CompareTo(b.priceCash);
        });

        for (int i = 0; i < auras.Count; i++)
        {
            if (auras[i].unlockedByDefault || auras[i].priceCash <= 0)
                ownedAuras.Add(auras[i]);
        }
    }

    void WireButtonsOnce()
    {
        if (wired) return;
        wired = true;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
            UIEffectUtil.AttachButtonBounce(this, closeButton);
        }
        if (modalBackdrop != null && closeOnBackdropClick)
        {
            Button b = modalBackdrop.GetComponent<Button>();
            if (b == null) b = modalBackdrop.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Hide);
        }
    }

    public void RebuildCardsInEditor()
    {
        EnsureSceneUI();
        LoadAuraCatalog();
        RebuildCards();
        RefreshAllCards();
    }

    void RebuildCards()
    {
        if (cardsContent == null) return;

        for (int i = cardsContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = cardsContent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        cardViews.Clear();

        for (int i = 0; i < auras.Count; i++)
        {
            AuraData aura = auras[i];
            if (aura == null) continue;
            AuraCardView card = CreateAuraCard(cardsContent, aura);
            cardViews.Add(card);
        }
    }

    AuraCardView CreateAuraCard(Transform parent, AuraData aura)
    {
        AuraCardView v = new AuraCardView { data = aura };

        // Viền ngoài của thẻ
        GameObject rootGo = new GameObject("AuraCard_" + aura.displayName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rootGo.transform.SetParent(parent, false);
        v.root = rootGo.GetComponent<RectTransform>();
        v.root.sizeDelta = cardSize;

        LayoutElement le = rootGo.GetComponent<LayoutElement>();
        le.preferredWidth = cardSize.x;
        le.preferredHeight = cardSize.y;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;

        v.outerBorder = rootGo.GetComponent<Image>();
        v.outerBorder.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
        v.outerBorder.type = Image.Type.Sliced;
        v.outerBorder.color = normalCardBorderColor;

        // Thân thẻ màu theo từng Aura
        GameObject bgGo = new GameObject("CardBody", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(v.root, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(4f, 4f);
        bgRt.offsetMax = new Vector2(-4f, -4f);
        v.cardBg = bgGo.GetComponent<Image>();
        v.cardBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 8);
        v.cardBg.type = Image.Type.Sliced;
        v.cardBg.color = Color.Lerp(aura.cardTopColor, aura.cardBottomColor, 0.35f);

        // Ánh sáng đỉnh thẻ (Highlight Top)
        GameObject topGlowGo = new GameObject("TopHighlight", typeof(RectTransform), typeof(Image));
        topGlowGo.transform.SetParent(bgRt, false);
        RectTransform tgRt = topGlowGo.GetComponent<RectTransform>();
        tgRt.anchorMin = new Vector2(0f, 0.78f);
        tgRt.anchorMax = new Vector2(1f, 1f);
        tgRt.offsetMin = new Vector2(4f, 0f);
        tgRt.offsetMax = new Vector2(-4f, -4f);
        Image tgImg = topGlowGo.GetComponent<Image>();
        tgImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 8);
        tgImg.type = Image.Type.Sliced;
        Color topC = Color.Lerp(aura.cardTopColor, Color.white, 0.28f);
        topC.a = 0.42f;
        tgImg.color = topC;
        tgImg.raycastTarget = false;

        // Tên Aura (ví dụ: Silver Aura, Lime Aura...)
        v.nameText = CreateTMP(bgRt, "AuraName", aura.displayName, 26f, Color.white, FontStyles.Bold);
        RectTransform nameRt = v.nameText.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -10f);
        nameRt.sizeDelta = new Vector2(-12f, 34f);
        v.nameText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        v.nameText.outlineWidth = 0.28f;

        // Cấp độ & Độ hiếm (ví dụ: Lvl 1 Common, Lvl 3 Uncommon)
        string rarityHex = ColorUtility.ToHtmlStringRGB(GetRarityColor(aura.rarity));
        string lvlStr = "Lvl " + aura.level + " <color=#" + rarityHex + ">" + aura.rarity + "</color>";
        v.levelRarityText = CreateTMP(bgRt, "LevelRarity", lvlStr, 20f, Color.white, FontStyles.Bold);
        RectTransform lrRt = v.levelRarityText.rectTransform;
        lrRt.anchorMin = new Vector2(0f, 1f);
        lrRt.anchorMax = new Vector2(1f, 1f);
        lrRt.pivot = new Vector2(0.5f, 1f);
        lrRt.anchoredPosition = new Vector2(0f, -42f);
        lrRt.sizeDelta = new Vector2(-12f, 28f);
        v.levelRarityText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        v.levelRarityText.outlineWidth = 0.26f;

        // Khung vuông hiển thị nhân vật bọc Aura ở giữa thẻ
        GameObject frameBorderGo = new GameObject("PreviewFrameBorder", typeof(RectTransform), typeof(Image));
        frameBorderGo.transform.SetParent(bgRt, false);
        RectTransform fbRt = frameBorderGo.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0.5f, 1f);
        fbRt.anchorMax = new Vector2(0.5f, 1f);
        fbRt.pivot = new Vector2(0.5f, 1f);
        fbRt.anchoredPosition = new Vector2(0f, -76f);
        fbRt.sizeDelta = new Vector2(198f, 204f);
        Image fbImg = frameBorderGo.GetComponent<Image>();
        fbImg.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

        GameObject frameInnerGo = new GameObject("PreviewInnerBg", typeof(RectTransform), typeof(Image));
        frameInnerGo.transform.SetParent(fbRt, false);
        RectTransform fiRt = frameInnerGo.GetComponent<RectTransform>();
        fiRt.anchorMin = Vector2.zero;
        fiRt.anchorMax = Vector2.one;
        fiRt.offsetMin = new Vector2(3f, 3f);
        fiRt.offsetMax = new Vector2(-3f, -3f);
        Image fiImg = frameInnerGo.GetComponent<Image>();
        fiImg.color = new Color(0.96f, 0.95f, 0.88f, 1f);

        // Lớp ngọn lửa hào quang phía sau nhân vật (nếu chưa có icon riêng)
        GameObject flamesGo = new GameObject("AuraGlowFlames", typeof(RectTransform), typeof(Image));
        flamesGo.transform.SetParent(fiRt, false);
        RectTransform flRt = flamesGo.GetComponent<RectTransform>();
        flRt.anchorMin = Vector2.zero;
        flRt.anchorMax = Vector2.one;
        flRt.offsetMin = new Vector2(6f, 6f);
        flRt.offsetMax = new Vector2(-6f, -6f);
        v.previewGlowBg = flamesGo.GetComponent<Image>();
        v.previewGlowBg.sprite = GetAuraFlamesFallbackSprite();
        v.previewGlowBg.preserveAspect = true;
        v.previewGlowBg.color = aura.auraColor;
        v.previewGlowBg.raycastTarget = false;

        // Hình nhân vật hoặc Icon tùy chỉnh của AuraData
        GameObject iconGo = new GameObject("AuraPreviewIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(fiRt, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = Vector2.zero;
        icRt.anchorMax = Vector2.one;
        icRt.offsetMin = new Vector2(10f, 10f);
        icRt.offsetMax = new Vector2(-10f, -10f);
        v.previewIcon = iconGo.GetComponent<Image>();
        v.previewIcon.preserveAspect = true;
        v.previewIcon.raycastTarget = false;
        if (aura.icon != null)
        {
            v.previewIcon.sprite = aura.icon;
            v.previewIcon.color = Color.white;
            v.previewGlowBg.gameObject.SetActive(false);
        }
        else
        {
            v.previewIcon.sprite = GetAuraCharacterFallbackSprite();
            v.previewIcon.color = Color.Lerp(aura.auraColor, Color.white, 0.25f);
            v.previewGlowBg.gameObject.SetActive(true);
        }

        // Dải nền tối chứa dòng chữ Buff (x1.5 Stamina, x2 Stamina...)
        GameObject buffStripGo = new GameObject("BuffStrip", typeof(RectTransform), typeof(Image));
        buffStripGo.transform.SetParent(bgRt, false);
        RectTransform bsRt = buffStripGo.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0f, 1f);
        bsRt.anchorMax = new Vector2(1f, 1f);
        bsRt.pivot = new Vector2(0.5f, 1f);
        bsRt.anchoredPosition = new Vector2(0f, -288f);
        bsRt.sizeDelta = new Vector2(-12f, 52f);
        Image bsImg = buffStripGo.GetComponent<Image>();
        bsImg.color = new Color(0.05f, 0.08f, 0.06f, 0.76f);

        v.buffText = CreateTMP(bsRt, "BuffText", aura.BuildBuffDisplayText(), 24f, Color.white, FontStyles.Bold);
        RectTransform btRt = v.buffText.rectTransform;
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = new Vector2(6f, 2f);
        btRt.offsetMax = new Vector2(-6f, -2f);
        v.buffText.enableWordWrapping = true;
        v.buffText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        v.buffText.outlineWidth = 0.28f;

        // Nút hành động chính (Mua $5K / Equip / Unequip)
        GameObject btnBorderGo = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnBorderGo.transform.SetParent(bgRt, false);
        RectTransform btnRt = btnBorderGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 24f);
        btnRt.sizeDelta = new Vector2(198f, 56f);
        Image btnBorderImg = btnBorderGo.GetComponent<Image>();
        btnBorderImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
        btnBorderImg.type = Image.Type.Sliced;
        btnBorderImg.color = new Color(0.06f, 0.08f, 0.06f, 0.96f);

        GameObject btnFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        btnFillGo.transform.SetParent(btnRt, false);
        RectTransform bfRt = btnFillGo.GetComponent<RectTransform>();
        bfRt.anchorMin = Vector2.zero;
        bfRt.anchorMax = Vector2.one;
        bfRt.offsetMin = new Vector2(3f, 3f);
        bfRt.offsetMax = new Vector2(-3f, -3f);
        v.actionButtonBg = btnFillGo.GetComponent<Image>();
        v.actionButtonBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 8);
        v.actionButtonBg.type = Image.Type.Sliced;
        v.actionButtonBg.color = buyButtonColor;

        v.actionButtonText = CreateTMP(bfRt, "Label", "Equip", 26f, Color.white, FontStyles.Bold);
        RectTransform abtRt = v.actionButtonText.rectTransform;
        abtRt.anchorMin = Vector2.zero;
        abtRt.anchorMax = Vector2.one;
        abtRt.offsetMin = Vector2.zero;
        abtRt.offsetMax = Vector2.zero;
        v.actionButtonText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        v.actionButtonText.outlineWidth = 0.28f;

        v.actionButton = btnBorderGo.GetComponent<Button>();
        v.actionButton.targetGraphic = v.actionButtonBg;
        UIEffectUtil.AttachButtonBounce(this, v.actionButton);

        AuraData capturedAura = aura;
        RectTransform capturedCardRoot = v.root;
        v.actionButton.onClick.AddListener(delegate { OnCardActionClicked(capturedAura, capturedCardRoot); });

        return v;
    }

    void OnCardActionClicked(AuraData aura, RectTransform cardRoot)
    {
        if (aura == null) return;
        if (gm == null) gm = GameManager.Instance;
        if (gm == null) return;

        bool owned = IsOwned(aura);
        if (!owned)
        {
            if (!gm.TrySpend(aura.priceCash))
            {
                HudController.Say("Not enough cash! Need $" + NumberFormat.Short(aura.priceCash), 2f);
                return;
            }
            ownedAuras.Add(aura);
            gm.SetEquippedAura(aura);
            HudController.Say("Unlocked & Equipped " + aura.displayName + " (" + aura.BuildBuffDisplayText().Replace("\n", " ") + ")!", 2.4f);
        }
        else
        {
            if (gm.EquippedAura == aura)
            {
                gm.SetEquippedAura(null);
                HudController.Say("Unequipped " + aura.displayName, 1.8f);
            }
            else
            {
                gm.SetEquippedAura(aura);
                HudController.Say("Equipped " + aura.displayName + " (" + aura.BuildBuffDisplayText().Replace("\n", " ") + ")!", 2.2f);
            }
        }

        if (cardRoot != null)
            UIEffectUtil.PlaySpringPop(this, cardRoot, null, 0.20f, 0.92f, 1.05f);

        RefreshAllCards();
    }

    public void RefreshAllCards()
    {
        if (gm == null) gm = GameManager.Instance;
        double myCash = gm != null ? gm.Cash : 0;
        AuraData equipped = gm != null ? gm.EquippedAura : null;

        for (int i = 0; i < cardViews.Count; i++)
        {
            AuraCardView c = cardViews[i];
            if (c == null || c.data == null) continue;

            AuraData a = c.data;
            bool owned = IsOwned(a);
            bool isEquipped = (equipped == a);

            if (c.nameText != null) c.nameText.text = a.displayName;
            if (c.levelRarityText != null)
            {
                string rarityHex = ColorUtility.ToHtmlStringRGB(GetRarityColor(a.rarity));
                c.levelRarityText.text = "Lvl " + a.level + " <color=#" + rarityHex + ">" + a.rarity + "</color>";
            }
            if (c.buffText != null) c.buffText.text = a.BuildBuffDisplayText();

            if (c.outerBorder != null)
                c.outerBorder.color = isEquipped ? equippedCardBorderColor : normalCardBorderColor;

            if (c.actionButtonBg != null && c.actionButtonText != null)
            {
                if (!owned)
                {
                    c.actionButtonText.text = "$" + NumberFormat.Short(a.priceCash);
                    bool canAfford = !Application.isPlaying || myCash >= a.priceCash;
                    c.actionButtonBg.color = canAfford ? buyButtonColor : buyLockedColor;
                }
                else if (isEquipped)
                {
                    c.actionButtonText.text = unequipButtonLabel;
                    c.actionButtonBg.color = unequipButtonColor;
                }
                else
                {
                    c.actionButtonText.text = equipButtonLabel;
                    c.actionButtonBg.color = equipButtonColor;
                }
            }
        }
    }

    public void EnsureSceneUI()
    {
        if (initialized && panelRoot != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject mainC = GameObject.Find("Canvas_Main");
            if (mainC != null) canvas = mainC.GetComponent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
        }
        if (canvas == null) return;

        // 1. Nền mờ khóa cảm ứng (Modal Backdrop)
        if (modalBackdrop == null)
        {
            Transform existingBd = canvas.transform.Find("AurasModalBackdrop");
            if (existingBd != null) modalBackdrop = existingBd.GetComponent<Image>();
        }
        if (modalBackdrop == null)
        {
            GameObject bdGo = new GameObject("AurasModalBackdrop", typeof(RectTransform), typeof(Image));
            bdGo.transform.SetParent(canvas.transform, false);
            RectTransform bdRt = bdGo.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            modalBackdrop = bdGo.GetComponent<Image>();
            modalBackdrop.color = modalDimColor;
            modalBackdrop.raycastTarget = true;
        }

        // 2. Khung chính AurasPopupPanel
        if (panelRoot == null)
        {
            Transform existingPanel = canvas.transform.Find("AurasPopupPanel");
            if (existingPanel != null) panelRoot = existingPanel.GetComponent<RectTransform>();
        }
        if (panelRoot == null)
        {
            GameObject pGo = new GameObject("AurasPopupPanel", typeof(RectTransform), typeof(Image));
            pGo.transform.SetParent(canvas.transform, false);
            panelRoot = pGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = new Vector2(0f, 18f);
            panelRoot.sizeDelta = new Vector2(1010f, 595f);

            Image outerImg = pGo.GetComponent<Image>();
            outerImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
            outerImg.type = Image.Type.Sliced;
            outerImg.color = new Color(0.05f, 0.05f, 0.07f, 0.98f);
        }

        // Thân trong của bảng
        Transform bodyTr = panelRoot.Find("BodyBg");
        RectTransform bodyRt;
        if (bodyTr == null)
        {
            GameObject bodyGo = new GameObject("BodyBg", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(panelRoot, false);
            bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(5f, 5f);
            bodyRt.offsetMax = new Vector2(-5f, -5f);
            Image bImg = bodyGo.GetComponent<Image>();
            bImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
            bImg.type = Image.Type.Sliced;
            bImg.color = panelBgColor;
        }
        else
        {
            bodyRt = bodyTr.GetComponent<RectTransform>();
        }

        // 3. Thanh tiêu đề AURAS (Header Bar)
        Transform headerTr = panelRoot.Find("HeaderBar");
        RectTransform headerRt;
        if (headerTr == null)
        {
            GameObject hGo = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            hGo.transform.SetParent(panelRoot, false);
            headerRt = hGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -5f);
            headerRt.sizeDelta = new Vector2(-10f, 64f);
            headerBarImage = hGo.GetComponent<Image>();
            headerBarImage.sprite = UIEffectUtil.GetRoundedRectSprite(64, 8);
            headerBarImage.type = Image.Type.Sliced;
            headerBarImage.color = headerBarColor;
        }
        else
        {
            headerRt = headerTr.GetComponent<RectTransform>();
            headerBarImage = headerTr.GetComponent<Image>();
        }

        // Icon tia sét bên trái Header
        Transform iconTr = headerRt.Find("LightningIcon");
        if (iconTr == null)
        {
            GameObject icGo = new GameObject("LightningIcon", typeof(RectTransform), typeof(Image));
            icGo.transform.SetParent(headerRt, false);
            RectTransform icRt = icGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0f, 0.5f);
            icRt.anchorMax = new Vector2(0f, 0.5f);
            icRt.pivot = new Vector2(0f, 0.5f);
            icRt.anchoredPosition = new Vector2(16f, 0f);
            icRt.sizeDelta = new Vector2(44f, 44f);
            headerIconImage = icGo.GetComponent<Image>();
            headerIconImage.sprite = GetLightningSprite();
            headerIconImage.preserveAspect = true;
        }
        else
        {
            headerIconImage = iconTr.GetComponent<Image>();
        }

        // Chữ tiêu đề AURAS ở giữa Header
        Transform titleTr = headerRt.Find("TitleText");
        if (titleTr == null)
        {
            headerTitleText = CreateTMP(headerRt, "TitleText", windowTitle, 42f, Color.white, FontStyles.Bold);
            RectTransform tRt = headerTitleText.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(70f, 0f);
            tRt.offsetMax = new Vector2(-70f, 0f);
            headerTitleText.outlineColor = new Color(0f, 0f, 0f, 0.96f);
            headerTitleText.outlineWidth = 0.30f;
        }
        else
        {
            headerTitleText = titleTr.GetComponent<TMP_Text>();
        }

        // Nút X đóng ở góc phải Header
        Transform closeTr = headerRt.Find("CloseButton");
        if (closeTr == null)
        {
            GameObject cGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cGo.transform.SetParent(headerRt, false);
            RectTransform cRt = cGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 0.5f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.anchoredPosition = new Vector2(-8f, 0f);
            cRt.sizeDelta = new Vector2(54f, 50f);
            Image cImg = cGo.GetComponent<Image>();
            cImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 8);
            cImg.type = Image.Type.Sliced;
            cImg.color = new Color(0.92f, 0.22f, 0.22f, 1f);
            closeButton = cGo.GetComponent<Button>();

            TMP_Text xTxt = CreateTMP(cRt, "X", "X", 32f, Color.white, FontStyles.Bold);
            xTxt.rectTransform.anchorMin = Vector2.zero;
            xTxt.rectTransform.anchorMax = Vector2.one;
            xTxt.rectTransform.offsetMin = Vector2.zero;
            xTxt.rectTransform.offsetMax = Vector2.zero;
            xTxt.outlineColor = new Color(0f, 0f, 0f, 0.95f);
            xTxt.outlineWidth = 0.28f;
        }
        else
        {
            closeButton = closeTr.GetComponent<Button>();
        }

        // 4. Vùng cuộn ngang chứa các thẻ Aura (Horizontal ScrollView)
        Transform scrollTr = panelRoot.Find("CardsScrollView");
        RectTransform scrollRt;
        if (scrollTr == null)
        {
            GameObject sGo = new GameObject("CardsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(RectMask2D));
            sGo.transform.SetParent(panelRoot, false);
            scrollRt = sGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(16f, 36f);
            scrollRt.offsetMax = new Vector2(-16f, -76f);
            Image maskBg = sGo.GetComponent<Image>();
            maskBg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollRect = sGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;
        }
        else
        {
            scrollRt = scrollTr.GetComponent<RectTransform>();
            scrollRect = scrollTr.GetComponent<ScrollRect>();
        }

        Transform contentTr = scrollRt.Find("Content");
        if (contentTr == null)
        {
            GameObject ctGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            ctGo.transform.SetParent(scrollRt, false);
            cardsContent = ctGo.GetComponent<RectTransform>();
            cardsContent.anchorMin = new Vector2(0f, 0f);
            cardsContent.anchorMax = new Vector2(0f, 1f);
            cardsContent.pivot = new Vector2(0f, 0.5f);
            cardsContent.anchoredPosition = Vector2.zero;
            cardsContent.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup hlg = ctGo.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 4, 4);
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            ContentSizeFitter csf = ctGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
        else
        {
            cardsContent = contentTr.GetComponent<RectTransform>();
        }
        scrollRect.viewport = scrollRt;
        scrollRect.content = cardsContent;

        // 5. Thanh cuộn ngang bên dưới (Horizontal Scrollbar)
        Transform sbTr = panelRoot.Find("HorizontalScrollbar");
        if (sbTr == null)
        {
            GameObject sbGo = new GameObject("HorizontalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(panelRoot, false);
            RectTransform sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0f, 0f);
            sbRt.anchorMax = new Vector2(1f, 0f);
            sbRt.pivot = new Vector2(0.5f, 0f);
            sbRt.anchoredPosition = new Vector2(0f, 12f);
            sbRt.sizeDelta = new Vector2(-36f, 14f);
            Image sbBg = sbGo.GetComponent<Image>();
            sbBg.sprite = UIEffectUtil.GetRoundedRectSprite(32, 6);
            sbBg.type = Image.Type.Sliced;
            sbBg.color = new Color(0.10f, 0.10f, 0.14f, 0.90f);

            GameObject slideArea = new GameObject("SlidingArea", typeof(RectTransform));
            slideArea.transform.SetParent(sbRt, false);
            RectTransform saRt = slideArea.GetComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(2f, 2f);
            saRt.offsetMax = new Vector2(-2f, -2f);

            GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(saRt, false);
            RectTransform hRt = handleGo.GetComponent<RectTransform>();
            hRt.sizeDelta = Vector2.zero;
            Image hImg = handleGo.GetComponent<Image>();
            hImg.sprite = UIEffectUtil.GetRoundedRectSprite(32, 5);
            hImg.type = Image.Type.Sliced;
            hImg.color = new Color(0.56f, 0.54f, 0.72f, 0.95f);

            horizontalScrollbar = sbGo.GetComponent<Scrollbar>();
            horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;
            horizontalScrollbar.handleRect = hRt;
            horizontalScrollbar.targetGraphic = hImg;
        }
        else
        {
            horizontalScrollbar = sbTr.GetComponent<Scrollbar>();
        }
        if (scrollRect != null && horizontalScrollbar != null)
            scrollRect.horizontalScrollbar = horizontalScrollbar;

        initialized = true;
    }

    static TMP_Text CreateTMP(Transform parent, string name, string text, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Color GetRarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common: return new Color(0.86f, 0.88f, 0.92f, 1f);
            case Rarity.Uncommon: return new Color(0.35f, 0.96f, 0.42f, 1f);
            case Rarity.Rare: return new Color(0.32f, 0.75f, 1.0f, 1f);
            case Rarity.Epic: return new Color(0.76f, 0.36f, 1.0f, 1f);
            case Rarity.Legendary: return new Color(1.0f, 0.82f, 0.22f, 1f);
            case Rarity.Mythic: return new Color(1.0f, 0.28f, 0.36f, 1f);
            case Rarity.Secret: return new Color(0.25f, 1.0f, 0.92f, 1f);
            case Rarity.Divine: return new Color(1.0f, 0.95f, 0.65f, 1f);
            default: return Color.white;
        }
    }

    static Sprite GetLightningSprite()
    {
        if (lightningSprite != null) return lightningSprite;
        if (GameManager.Instance != null && GameManager.Instance.staminaIcon != null)
            return GameManager.Instance.staminaIcon;

        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                bool inside = false;
                if (v >= 0.45f)
                {
                    float left = Mathf.Lerp(0.25f, 0.52f, (v - 0.45f) / 0.50f);
                    float right = left + 0.28f;
                    inside = (u >= left && u <= right && v <= 0.94f);
                }
                else if (v >= 0.06f)
                {
                    float left = Mathf.Lerp(0.36f, 0.38f, (v - 0.06f) / 0.48f);
                    float right = Mathf.Lerp(0.42f, 0.72f, (v - 0.06f) / 0.48f);
                    inside = (u >= left && u <= right);
                }
                if (inside)
                    px[y * w + x] = new Color32(255, 210, 45, 255);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        lightningSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return lightningSprite;
    }

    // Vẽ bóng nhân vật khối vuông (giống hình mẫu trong ô vuông Aura) khi người dùng chưa kéo Sprite riêng vào
    static Sprite GetAuraCharacterFallbackSprite()
    {
        if (auraCharacterFallbackSprite != null) return auraCharacterFallbackSprite;
        int w = 96, h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                // Đầu, thân, 2 tay và 2 chân kiểu nhân vật khối nghiêng năng động
                bool head = (u >= 0.37f && u <= 0.61f && v >= 0.67f && v <= 0.89f);
                bool torso = (u >= 0.35f && u <= 0.63f && v >= 0.36f && v <= 0.65f);
                bool leftArm = (u >= 0.17f && u <= 0.34f && v >= 0.42f && v <= 0.66f);
                bool rightArm = (u >= 0.64f && u <= 0.81f && v >= 0.40f && v <= 0.64f);
                bool leftLeg = (u >= 0.34f && u <= 0.48f && v >= 0.12f && v <= 0.35f);
                bool rightLeg = (u >= 0.50f && u <= 0.64f && v >= 0.12f && v <= 0.35f);

                if (head || torso || leftArm || rightArm || leftLeg || rightLeg)
                {
                    bool border = (head && (u < 0.40f || u > 0.58f || v < 0.70f || v > 0.86f))
                               || (torso && (u < 0.38f || u > 0.60f || v < 0.39f || v > 0.62f))
                               || (leftArm && (u < 0.20f || u > 0.32f || v < 0.45f || v > 0.63f))
                               || (rightArm && (u < 0.66f || u > 0.78f || v < 0.43f || v > 0.61f))
                               || (leftLeg && (u < 0.37f || u > 0.46f || v < 0.15f))
                               || (rightLeg && (u < 0.52f || u > 0.61f || v < 0.15f));
                    px[y * w + x] = border ? new Color32(25, 28, 34, 255) : new Color32(240, 244, 250, 255);
                }
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        auraCharacterFallbackSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return auraCharacterFallbackSprite;
    }

    static Sprite GetAuraFlamesFallbackSprite()
    {
        if (auraFlamesFallbackSprite != null) return auraFlamesFallbackSprite;
        int w = 96, h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                float dx = (u - 0.5f) * 2f;
                float dy = (v - 0.48f) * 2f;
                float angle = Mathf.Atan2(dy, dx);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float wave = 0.74f + 0.18f * Mathf.Sin(angle * 8f + v * 10f) + 0.08f * Mathf.Cos(angle * 13f);
                if (dist <= wave && dist >= 0.25f)
                {
                    float a = Mathf.Clamp01((wave - dist) / 0.25f);
                    bool darkEdge = dist > wave - 0.05f;
                    px[y * w + x] = darkEdge
                        ? new Color32(30, 35, 40, (byte)Mathf.RoundToInt(a * 220f))
                        : new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 245f));
                }
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        auraFlamesFallbackSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return auraFlamesFallbackSprite;
    }
}
