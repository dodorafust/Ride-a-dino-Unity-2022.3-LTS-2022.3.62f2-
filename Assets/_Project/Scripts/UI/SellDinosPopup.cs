using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup bán nhiều Dino khi đứng vào vòng tròn trước cửa hàng SellDinoShop.
// Toàn bộ UI có thể dựng sẵn trong Scene (Tools > Ride a Dino > Build Sell Dinos Shop & Popup)
// để bạn tự do chỉnh màu, kích thước, sprite, font và thời gian hiệu ứng.
public class SellDinosPopup : MonoBehaviour
{
    public static SellDinosPopup Instance { get; private set; }

    public enum SortMode { Value, Rarity }

    [Header("Tham chiếu UI (Tự tạo hoặc dựng sẵn trong Scene)")]
    public Image modalBackdrop;
    public RectTransform panelRoot;
    public Button closeButton;
    public Button sortValueButton;
    public Image sortValueBg;
    public TMP_Text sortValueArrow;
    public Button sortRarityButton;
    public Image sortRarityBg;
    public TMP_Text sortRarityArrow;
    public Button selectAllButton;
    public Image selectAllDot;
    public TMP_Text equippedNoteText;
    public ScrollRect scrollRect;
    public RectTransform gridContent;
    public RectTransform cardTemplate;
    public TMP_Text totalValueText;
    public Image totalMoneyIcon;
    public Button sellButton;
    public Image sellButtonBg;
    public TMP_Text sellButtonText;
    public TMP_Text soldHistoryText;
    public CanvasGroup soldHistoryGroup;
    public RectTransform fxLayer;

    [Header("Sprite tùy chỉnh (để trống sẽ tự vẽ mặc định)")]
    public Sprite pawSprite;
    public Sprite moneySprite;
    public Sprite checkSprite;
    public Sprite dinoFallbackSprite;

    [Header("Màu sắc")]
    public Color modalDimColor = new Color(0f, 0f, 0f, 0.62f);
    public bool closeOnBackdropClick = false;
    public Color headerBarColor = new Color(0.45f, 0.90f, 0.14f, 1f);
    public Color panelBgColor = new Color(0.14f, 0.16f, 0.18f, 0.97f);
    public Color activeButtonColor = new Color(0.38f, 0.88f, 0.16f, 1f);
    public Color inactiveButtonColor = new Color(0.68f, 0.70f, 0.72f, 1f);
    public Color selectedBorderColor = new Color(0.24f, 0.94f, 0.28f, 1f);
    public Color normalBorderColor = new Color(0.06f, 0.07f, 0.08f, 1f);
    public Color sellActiveColor = new Color(0.42f, 0.90f, 0.14f, 1f);
    public Color sellEmptyColor = new Color(0.75f, 0.77f, 0.79f, 1f);

    [Header("Hiệu ứng & Thời gian")]
    public bool hideOnStart = true;
    public float openBounceDuration = 0.28f;
    public float selectedScale = 1.03f;
    public float cardClickPopDuration = 0.16f;
    public float sellShrinkToCornerDuration = 0.16f;
    public float sellFlyToButtonDuration = 0.40f;
    public float sellCoinArcHeight = 42f;
    public float floatingGainDuration = 1.1f;
    public float soldHistoryDuration = 2.6f;

    [Header("Nội dung chữ")]
    public string windowTitle = "Sell Dinos";
    public string equippedNote = "Dinos equipped in your pen cannot be sold.";
    public string sellButtonFormat = "Sell ({0})";
    public string soldSingularFormat = "Sold 1 dinosaur for ${0}!";
    public string soldPluralFormat = "Sold {0} dinosaurs for ${1}!";
    [Min(3)] public int maxNameChars = 15;

    class CardItem
    {
        public OwnedDino dino;
        public RectTransform slotRoot;
        public RectTransform visualInner;
        public Image borderImage;
        public Image checkBadge;
        public GameObject coinCornerGo;
        public float popTimer;
    }

    readonly List<CardItem> cards = new List<CardItem>();
    readonly HashSet<OwnedDino> selectedDinos = new HashSet<OwnedDino>();
    readonly HashSet<OwnedDino> lockedSelling = new HashSet<OwnedDino>();

    SortMode sortMode = SortMode.Value;
    bool sortDescending = true;
    bool isSellingAnim;
    float historyTimer;
    float sellButtonPunchTimer;
    GameManager gm;
    bool subscribed;
    Coroutine popRoutine;

    Texture2D texPaw, texMoney, texCheck, texDino;
    Sprite spPaw, spMoney, spCheck, spDino;

    public bool Visible { get { return panelRoot != null ? panelRoot.gameObject.activeSelf : gameObject.activeSelf; } }

    void Awake()
    {
        Instance = this;
        WireButtons();
    }

    void Start()
    {
        EnsureBuilt();
        WireButtons();
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
        if (soldHistoryText != null) soldHistoryText.gameObject.SetActive(false);
        if (hideOnStart) Hide();
        else Rebuild();
    }

    void OnEnable()
    {
        TrySubscribe();
        if (panelRoot != null) Rebuild();
    }

    void OnDisable()
    {
        if (gm != null && subscribed) gm.Inventory.Changed -= OnInventoryChanged;
        subscribed = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (texPaw != null) Destroy(texPaw);
        if (texMoney != null) Destroy(texMoney);
        if (texCheck != null) Destroy(texCheck);
        if (texDino != null) Destroy(texDino);
    }

    void TrySubscribe()
    {
        gm = GameManager.Instance;
        if (gm != null && !subscribed)
        {
            gm.Inventory.Changed += OnInventoryChanged;
            subscribed = true;
        }
    }

    void OnInventoryChanged()
    {
        if (!isSellingAnim && Visible) Rebuild();
    }

    public void Open()
    {
        EnsureBuilt();
        WireButtons();
        TrySubscribe();
        if (PenPanel.Instance != null) PenPanel.Instance.Hide();
        if (InventoryPopup.Instance != null) InventoryPopup.Instance.Hide();
        if (DinoIndexPopup.Instance != null) DinoIndexPopup.Instance.Hide();

        if (modalBackdrop != null)
        {
            modalBackdrop.color = modalDimColor;
            modalBackdrop.gameObject.SetActive(true);
            if (panelRoot != null && modalBackdrop.transform.parent == panelRoot.parent)
                modalBackdrop.transform.SetAsLastSibling();
            else
                transform.SetAsLastSibling();
        }
        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(true);
            panelRoot.SetAsLastSibling();
            popRoutine = UIEffectUtil.PlaySpringPop(this, panelRoot, popRoutine, openBounceDuration);
        }
        else
        {
            gameObject.SetActive(true);
        }

        Rebuild();
    }

    public void Hide()
    {
        if (isSellingAnim) return;
        if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(false);
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (Visible) Hide();
        else Open();
    }

    public void BuildInEditor()
    {
        EnsureBuilt();
        WireButtons();
        if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(true);
        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(true);
        if (soldHistoryText != null) soldHistoryText.gameObject.SetActive(false);
    }

    bool wired;
    void WireButtons()
    {
        if (wired) return;
        if (closeButton == null && sellButton == null) return;
        wired = true;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ForceHideFromButton);
            UIEffectUtil.AttachButtonBounce(this, closeButton);
        }
        if (sortValueButton != null)
        {
            sortValueButton.onClick.AddListener(delegate { OnClickSort(SortMode.Value); });
            UIEffectUtil.AttachButtonBounce(this, sortValueButton);
        }
        if (sortRarityButton != null)
        {
            sortRarityButton.onClick.AddListener(delegate { OnClickSort(SortMode.Rarity); });
            UIEffectUtil.AttachButtonBounce(this, sortRarityButton);
        }
        if (selectAllButton != null)
        {
            selectAllButton.onClick.AddListener(OnClickSelectAll);
            UIEffectUtil.AttachButtonBounce(this, selectAllButton);
        }
        if (sellButton != null) sellButton.onClick.AddListener(OnClickSell);

        if (modalBackdrop != null)
        {
            Button bdBtn = modalBackdrop.GetComponent<Button>();
            if (bdBtn != null)
                bdBtn.onClick.AddListener(delegate { if (closeOnBackdropClick) Hide(); });
        }
    }

    void ForceHideFromButton()
    {
        if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(false);
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void EnsureBuilt()
    {
        Transform parent = panelRoot != null && panelRoot.parent != null ? panelRoot.parent : transform;
        if (panelRoot == null && GetComponent<HudController>() != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) parent = canvas.transform;
        }

        // Lớp nền tối Modal chặn cảm ứng/chuột ra ngoài bảng (tạo kể cả khi panelRoot đã dựng sẵn từ trước)
        if (modalBackdrop == null)
        {
            Transform existingBd = parent.Find("SellModalBackdrop");
            GameObject bdGo = existingBd != null
                ? existingBd.gameObject
                : new GameObject("SellModalBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            if (existingBd == null) bdGo.transform.SetParent(parent, false);
            RectTransform bdRt = bdGo.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            modalBackdrop = bdGo.GetComponent<Image>();
            modalBackdrop.color = modalDimColor;
            modalBackdrop.raycastTarget = true;
            Button bdBtn = bdGo.GetComponent<Button>();
            if (bdBtn != null) bdBtn.transition = Selectable.Transition.None;
            if (panelRoot != null && bdGo.transform.parent == panelRoot.parent)
                bdGo.transform.SetSiblingIndex(Mathf.Max(0, panelRoot.GetSiblingIndex()));
        }

        if (panelRoot != null && gridContent != null && cardTemplate != null) return;

        // Khung Popup chính ở giữa màn hình
        GameObject rootGo = new GameObject("SellDinosPanel", typeof(RectTransform), typeof(Image));
        rootGo.transform.SetParent(parent, false);
        panelRoot = rootGo.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.anchoredPosition = Vector2.zero;
        panelRoot.sizeDelta = new Vector2(940f, 610f);
        Image outerBg = rootGo.GetComponent<Image>();
        outerBg.color = new Color(0.05f, 0.06f, 0.07f, 1f);

        GameObject innerGo = new GameObject("InnerBg", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(panelRoot, false);
        RectTransform innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(5f, 5f);
        innerRt.offsetMax = new Vector2(-5f, -5f);
        innerGo.GetComponent<Image>().color = panelBgColor;

        // Thanh tiêu đề xanh lá
        GameObject hdrGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
        hdrGo.transform.SetParent(innerRt, false);
        RectTransform hdrRt = hdrGo.GetComponent<RectTransform>();
        hdrRt.anchorMin = new Vector2(0f, 1f);
        hdrRt.anchorMax = new Vector2(1f, 1f);
        hdrRt.pivot = new Vector2(0.5f, 1f);
        hdrRt.anchoredPosition = Vector2.zero;
        hdrRt.sizeDelta = new Vector2(0f, 70f);
        hdrGo.GetComponent<Image>().color = headerBarColor;

        GameObject pawGo = new GameObject("PawIcon", typeof(RectTransform), typeof(Image));
        pawGo.transform.SetParent(hdrRt, false);
        RectTransform pawRt = pawGo.GetComponent<RectTransform>();
        pawRt.anchorMin = new Vector2(0f, 0.5f);
        pawRt.anchorMax = new Vector2(0f, 0.5f);
        pawRt.pivot = new Vector2(0f, 0.5f);
        pawRt.anchoredPosition = new Vector2(20f, 0f);
        pawRt.sizeDelta = new Vector2(46f, 46f);
        Image pawImg = pawGo.GetComponent<Image>();
        pawImg.sprite = pawSprite != null ? pawSprite : GetPawSprite();
        pawImg.preserveAspect = true;

        CreateTmp(hdrRt, "Title", new Vector2(0f, 0f), new Vector2(0.8f, 1f), new Vector2(82f, 0f), new Vector2(-10f, 0f),
                  36f, Color.white, TextAlignmentOptions.Left, windowTitle);

        // Nút X đóng bảng
        GameObject closeGo = CreateButton(hdrRt, "CloseButton", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                          new Vector2(-14f, 0f), new Vector2(54f, 52f), new Vector2(1f, 0.5f),
                                          new Color(0.90f, 0.25f, 0.34f, 1f), "X", 28f, out _);
        closeButton = closeGo.GetComponent<Button>();

        // Cột trái: Sort By & Select All
        GameObject leftGo = new GameObject("LeftColumn", typeof(RectTransform));
        leftGo.transform.SetParent(innerRt, false);
        RectTransform leftRt = leftGo.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0f, 1f);
        leftRt.pivot = new Vector2(0f, 0.5f);
        leftRt.offsetMin = new Vector2(14f, 96f);
        leftRt.offsetMax = new Vector2(204f, -84f);

        CreateTmp(leftRt, "SortByLabel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -34f), Vector2.zero,
                  22f, Color.white, TextAlignmentOptions.Center, "Sort By:");

        GameObject svGo = CreateButton(leftRt, "SortValueBtn", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                       new Vector2(0f, -68f), new Vector2(166f, 46f), new Vector2(0.5f, 0.5f),
                                       activeButtonColor, "Value", 21f, out _);
        sortValueButton = svGo.GetComponent<Button>();
        sortValueBg = svGo.GetComponent<Image>();
        sortValueArrow = CreateTmp(svGo.transform, "Arrow", new Vector2(0f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero,
                                   22f, Color.white, TextAlignmentOptions.Center, "▼");

        GameObject srGo = CreateButton(leftRt, "SortRarityBtn", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                       new Vector2(0f, -124f), new Vector2(166f, 46f), new Vector2(0.5f, 0.5f),
                                       inactiveButtonColor, "Rarity", 21f, out _);
        sortRarityButton = srGo.GetComponent<Button>();
        sortRarityBg = srGo.GetComponent<Image>();
        sortRarityArrow = CreateTmp(srGo.transform, "Arrow", new Vector2(0f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero,
                                    22f, Color.white, TextAlignmentOptions.Center, "▼");

        GameObject saGo = CreateButton(leftRt, "SelectAllBtn", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                       new Vector2(0f, -188f), new Vector2(166f, 44f), new Vector2(0.5f, 0.5f),
                                       new Color(0.26f, 0.50f, 0.14f, 1f), "   Select All", 19f, out _);
        selectAllButton = saGo.GetComponent<Button>();

        GameObject dotGo = new GameObject("CheckDot", typeof(RectTransform), typeof(Image));
        dotGo.transform.SetParent(saGo.transform, false);
        RectTransform dotRt = dotGo.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0f, 0.5f);
        dotRt.anchorMax = new Vector2(0f, 0.5f);
        dotRt.pivot = new Vector2(0f, 0.5f);
        dotRt.anchoredPosition = new Vector2(10f, 0f);
        dotRt.sizeDelta = new Vector2(24f, 24f);
        selectAllDot = dotGo.GetComponent<Image>();
        selectAllDot.sprite = checkSprite != null ? checkSprite : GetCheckSprite();
        selectAllDot.color = new Color(0.12f, 0.14f, 0.12f, 1f);

        equippedNoteText = CreateTmp(leftRt, "Note", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -310f), new Vector2(-4f, -225f),
                                     16f, new Color(0.88f, 0.90f, 0.92f, 0.92f), TextAlignmentOptions.Top, equippedNote);
        equippedNoteText.enableWordWrapping = true;

        // Khung cuộn danh sách thẻ Dino (3 cột)
        GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        scrollGo.transform.SetParent(innerRt, false);
        RectTransform scrRt = scrollGo.GetComponent<RectTransform>();
        scrRt.anchorMin = new Vector2(0f, 0f);
        scrRt.anchorMax = new Vector2(1f, 1f);
        scrRt.offsetMin = new Vector2(212f, 96f);
        scrRt.offsetMax = new Vector2(-14f, -82f);
        scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.13f, 0.95f);

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(scrRt, false);
        RectTransform vpRt = viewportGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(8f, 8f);
        vpRt.offsetMax = new Vector2(-8f, -8f);

        GameObject contentGo = new GameObject("GridContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(vpRt, false);
        gridContent = contentGo.GetComponent<RectTransform>();
        gridContent.anchorMin = new Vector2(0f, 1f);
        gridContent.anchorMax = new Vector2(1f, 1f);
        gridContent.pivot = new Vector2(0.5f, 1f);
        gridContent.anchoredPosition = Vector2.zero;
        gridContent.sizeDelta = new Vector2(0f, 300f);

        GridLayoutGroup grid = contentGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(218f, 214f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.viewport = vpRt;
        scrollRect.content = gridContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;

        BuildCardTemplate(gridContent);

        // Thanh đáy: Total Value + Nút Sell (N)
        GameObject bottomGo = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
        bottomGo.transform.SetParent(innerRt, false);
        RectTransform botRt = bottomGo.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0f, 0f);
        botRt.anchorMax = new Vector2(1f, 0f);
        botRt.pivot = new Vector2(0.5f, 0f);
        botRt.anchoredPosition = new Vector2(0f, 8f);
        botRt.sizeDelta = new Vector2(-16f, 78f);
        bottomGo.GetComponent<Image>().color = new Color(0.11f, 0.12f, 0.14f, 0.98f);

        CreateTmp(botRt, "TotalLabel", new Vector2(0f, 0f), new Vector2(0.28f, 1f), new Vector2(22f, 0f), Vector2.zero,
                  27f, Color.white, TextAlignmentOptions.Left, "Total Value:");

        GameObject totIconGo = new GameObject("TotalMoneyIcon", typeof(RectTransform), typeof(Image));
        totIconGo.transform.SetParent(botRt, false);
        RectTransform tiRt = totIconGo.GetComponent<RectTransform>();
        tiRt.anchorMin = new Vector2(0.27f, 0.5f);
        tiRt.anchorMax = new Vector2(0.27f, 0.5f);
        tiRt.pivot = new Vector2(0.5f, 0.5f);
        tiRt.sizeDelta = new Vector2(46f, 46f);
        totalMoneyIcon = totIconGo.GetComponent<Image>();
        totalMoneyIcon.sprite = moneySprite != null ? moneySprite : GetMoneySprite();
        totalMoneyIcon.preserveAspect = true;

        totalValueText = CreateTmp(botRt, "TotalValueText", new Vector2(0.31f, 0f), new Vector2(0.64f, 1f), Vector2.zero, Vector2.zero,
                                   30f, Color.white, TextAlignmentOptions.Left, "$0");

        GameObject sellGo = CreateButton(botRt, "SellButton", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                         new Vector2(-18f, 0f), new Vector2(260f, 60f), new Vector2(1f, 0.5f),
                                         sellEmptyColor, "Sell (0)", 28f, out sellButtonText);
        sellButton = sellGo.GetComponent<Button>();
        sellButtonBg = sellGo.GetComponent<Image>();

        // Thông báo lịch sử bán ở mép dưới lưới thẻ
        GameObject histGo = new GameObject("SoldHistoryText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        histGo.transform.SetParent(innerRt, false);
        RectTransform histRt = histGo.GetComponent<RectTransform>();
        histRt.anchorMin = new Vector2(0.23f, 0f);
        histRt.anchorMax = new Vector2(0.98f, 0f);
        histRt.pivot = new Vector2(0.5f, 0f);
        histRt.anchoredPosition = new Vector2(0f, 94f);
        histRt.sizeDelta = new Vector2(0f, 34f);
        soldHistoryText = histGo.GetComponent<TextMeshProUGUI>();
        soldHistoryText.fontSize = 23f;
        soldHistoryText.fontStyle = FontStyles.Bold;
        soldHistoryText.color = new Color(0.32f, 0.96f, 0.36f, 1f);
        soldHistoryText.alignment = TextAlignmentOptions.Center;
        soldHistoryText.raycastTarget = false;
        soldHistoryGroup = histGo.GetComponent<CanvasGroup>();
        histGo.SetActive(false);

        // Layer trên cùng cho hiệu ứng Icon tiền bay
        GameObject fxGo = new GameObject("FxLayer", typeof(RectTransform));
        fxGo.transform.SetParent(panelRoot, false);
        fxLayer = fxGo.GetComponent<RectTransform>();
        fxLayer.anchorMin = Vector2.zero;
        fxLayer.anchorMax = Vector2.one;
        fxLayer.offsetMin = Vector2.zero;
        fxLayer.offsetMax = Vector2.zero;
    }

    void BuildCardTemplate(Transform parent)
    {
        GameObject slotGo = new GameObject("SellCardTemplate", typeof(RectTransform), typeof(Button));
        slotGo.transform.SetParent(parent, false);
        RectTransform slotRt = slotGo.GetComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(218f, 214f);

        // Khung con để thu nhỏ về phía mép trái ô khi bán mà không làm lệch GridLayoutGroup
        GameObject innerGo = new GameObject("VisualInner", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(slotRt, false);
        RectTransform innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.pivot = new Vector2(0.14f, 0.25f);
        innerRt.offsetMin = Vector2.zero;
        innerRt.offsetMax = Vector2.zero;
        innerGo.GetComponent<Image>().color = normalBorderColor;

        GameObject fillGo = new GameObject("FillBg", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(innerRt, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        fillGo.GetComponent<Image>().color = new Color(0.17f, 0.19f, 0.21f, 1f);

        CreateTmp(fillRt, "NameText", new Vector2(0f, 0.80f), new Vector2(0.84f, 0.98f), new Vector2(8f, 0f), Vector2.zero,
                  19f, Color.white, TextAlignmentOptions.Left, "Mammoth");

        CreateTmp(fillRt, "TagsText", new Vector2(0f, 0.68f), new Vector2(0.84f, 0.82f), new Vector2(8f, 0f), Vector2.zero,
                  14f, Color.white, TextAlignmentOptions.Left, "BIG");

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(fillRt, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.20f, 0.28f);
        icRt.anchorMax = new Vector2(0.80f, 0.72f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = dinoFallbackSprite != null ? dinoFallbackSprite : GetDinoFallbackSprite();
        icImg.preserveAspect = true;

        CreateTmp(fillRt, "RateText", new Vector2(0f, 0.18f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero,
                  18f, new Color(0.24f, 0.94f, 0.34f, 1f), TextAlignmentOptions.Center, "$10/s");

        // Thanh giá bán đáy thẻ
        GameObject priceBarGo = new GameObject("PriceBar", typeof(RectTransform), typeof(Image));
        priceBarGo.transform.SetParent(fillRt, false);
        RectTransform pbRt = priceBarGo.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0.04f, 0.03f);
        pbRt.anchorMax = new Vector2(0.96f, 0.20f);
        pbRt.offsetMin = Vector2.zero;
        pbRt.offsetMax = Vector2.zero;
        priceBarGo.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.10f, 0.96f);

        GameObject pIconGo = new GameObject("MoneyIcon", typeof(RectTransform), typeof(Image));
        pIconGo.transform.SetParent(pbRt, false);
        RectTransform piRt = pIconGo.GetComponent<RectTransform>();
        piRt.anchorMin = new Vector2(0f, 0.5f);
        piRt.anchorMax = new Vector2(0f, 0.5f);
        piRt.pivot = new Vector2(0f, 0.5f);
        piRt.anchoredPosition = new Vector2(8f, 0f);
        piRt.sizeDelta = new Vector2(26f, 26f);
        Image piImg = pIconGo.GetComponent<Image>();
        piImg.sprite = moneySprite != null ? moneySprite : GetMoneySprite();
        piImg.preserveAspect = true;

        CreateTmp(pbRt, "PriceText", new Vector2(0.22f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero,
                  20f, Color.white, TextAlignmentOptions.Center, "$300");

        // Huy hiệu dấu tích xanh góc trên phải
        GameObject chkGo = new GameObject("CheckBadge", typeof(RectTransform), typeof(Image));
        chkGo.transform.SetParent(innerRt, false);
        RectTransform chkRt = chkGo.GetComponent<RectTransform>();
        chkRt.anchorMin = new Vector2(1f, 1f);
        chkRt.anchorMax = new Vector2(1f, 1f);
        chkRt.pivot = new Vector2(0.5f, 0.5f);
        chkRt.anchoredPosition = new Vector2(-10f, -10f);
        chkRt.sizeDelta = new Vector2(30f, 30f);
        Image chkImg = chkGo.GetComponent<Image>();
        chkImg.sprite = checkSprite != null ? checkSprite : GetCheckSprite();
        chkImg.color = selectedBorderColor;
        chkGo.SetActive(false);

        cardTemplate = slotRt;
    }

    static GameObject CreateButton(Transform parent, string name, Vector2 aMin, Vector2 aMax,
                                   Vector2 pos, Vector2 size, Vector2 pivot,
                                   Color bgColor, string text, float fontSize, out TMP_Text outTmp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = bgColor;
        outTmp = CreateTmp(rt, "Label", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                           fontSize, Color.white, TextAlignmentOptions.Center, text);
        return go;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, Vector2 aMin, Vector2 aMax,
                                     Vector2 offMin, Vector2 offMax,
                                     float fontSize, Color color, TextAlignmentOptions align, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    public void Rebuild()
    {
        if (gm == null) gm = GameManager.Instance;
        if (gm == null || gridContent == null || cardTemplate == null) return;

        for (int i = 0; i < cards.Count; i++)
            if (cards[i].slotRoot != null) Destroy(cards[i].slotRoot.gameObject);
        cards.Clear();

        // Giữ lại các chọn lựa vẫn còn nằm trong kho
        var list = new List<OwnedDino>();
        for (int i = 0; i < gm.Inventory.Items.Count; i++)
        {
            OwnedDino d = gm.Inventory.Items[i];
            if (d != null && d.data != null && !lockedSelling.Contains(d))
                list.Add(d);
        }
        selectedDinos.RemoveWhere(d => !list.Contains(d));

        list.Sort((a, b) =>
        {
            int cmp = sortMode == SortMode.Value
                ? a.SellValue.CompareTo(b.SellValue)
                : ((int)a.data.rarity).CompareTo((int)b.data.rarity);
            if (cmp == 0) cmp = a.SellValue.CompareTo(b.SellValue);
            return sortDescending ? -cmp : cmp;
        });

        for (int i = 0; i < list.Count; i++)
        {
            OwnedDino dino = list[i];
            RectTransform slotRt = Instantiate(cardTemplate, gridContent);
            slotRt.name = "SellCard_" + dino.data.displayName;
            slotRt.gameObject.SetActive(true);

            RectTransform inner = slotRt.Find("VisualInner") as RectTransform;
            Image borderImg = inner != null ? inner.GetComponent<Image>() : slotRt.GetComponent<Image>();
            Transform fill = inner != null ? inner.Find("FillBg") : slotRt.Find("FillBg");
            Transform chkT = inner != null ? inner.Find("CheckBadge") : slotRt.Find("CheckBadge");
            Image chkImg = chkT != null ? chkT.GetComponent<Image>() : null;

            BindCardTexts(fill, dino);

            CardItem item = new CardItem
            {
                dino = dino,
                slotRoot = slotRt,
                visualInner = inner != null ? inner : slotRt,
                borderImage = borderImg,
                checkBadge = chkImg
            };
            cards.Add(item);

            Button btn = slotRt.GetComponent<Button>();
            if (btn != null)
            {
                CardItem captured = item;
                btn.onClick.AddListener(delegate { ToggleCardSelection(captured); });
            }
            UpdateCardSelectionVisual(item, false);
        }

        RefreshControls();
    }

    void BindCardTexts(Transform fill, OwnedDino d)
    {
        if (fill == null) return;
        TMP_Text nameTmp = fill.Find("NameText")?.GetComponent<TMP_Text>();
        if (nameTmp != null)
        {
            string n = d.data.displayName;
            nameTmp.text = n.Length > maxNameChars ? n.Substring(0, maxNameChars - 1) + "." : n;
            nameTmp.color = DinoColors.Of(d.data.rarity);
        }

        TMP_Text tagsTmp = fill.Find("TagsText")?.GetComponent<TMP_Text>();
        if (tagsTmp != null)
        {
            string vTag = (d.variant != null && !d.variant.isNormal)
                ? "<color=#FFD94A>" + d.variant.displayName.ToUpperInvariant() + "</color>"
                : "";
            string sTag = !string.IsNullOrEmpty(d.SizeTag)
                ? "<color=#" + DinoColors.Hex(d.SizeColor) + ">" + d.SizeTag + "</color>"
                : "";
            tagsTmp.text = string.IsNullOrEmpty(vTag) ? sTag : (string.IsNullOrEmpty(sTag) ? vTag : vTag + " " + sTag);
        }

        Image iconImg = fill.Find("Icon")?.GetComponent<Image>();
        if (iconImg != null)
        {
            bool hasIcon = d.data.icon != null;
            iconImg.sprite = hasIcon ? d.data.icon : (dinoFallbackSprite != null ? dinoFallbackSprite : GetDinoFallbackSprite());
            iconImg.color = hasIcon ? Color.white : DinoColors.Of(d.data.rarity);
        }

        TMP_Text rateTmp = fill.Find("RateText")?.GetComponent<TMP_Text>();
        if (rateTmp != null) rateTmp.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";

        TMP_Text priceTmp = fill.Find("PriceBar/PriceText")?.GetComponent<TMP_Text>();
        if (priceTmp != null) priceTmp.text = "$" + NumberFormat.Short(d.SellValue);
    }

    void ToggleCardSelection(CardItem item)
    {
        if (isSellingAnim || item == null) return;
        if (selectedDinos.Contains(item.dino)) selectedDinos.Remove(item.dino);
        else selectedDinos.Add(item.dino);
        UpdateCardSelectionVisual(item, true);
        RefreshControls();
    }

    void UpdateCardSelectionVisual(CardItem item, bool playPop)
    {
        bool sel = selectedDinos.Contains(item.dino);
        if (item.borderImage != null)
            item.borderImage.color = sel ? selectedBorderColor : normalBorderColor;
        if (item.checkBadge != null)
            item.checkBadge.gameObject.SetActive(sel);
        if (playPop) item.popTimer = Mathf.Max(0.05f, cardClickPopDuration);
        else if (item.visualInner != null)
            item.visualInner.localScale = Vector3.one * (sel ? selectedScale : 1f);
    }

    void OnClickSort(SortMode mode)
    {
        if (isSellingAnim) return;
        if (sortMode == mode) sortDescending = !sortDescending;
        else { sortMode = mode; sortDescending = true; }
        Rebuild();
    }

    void OnClickSelectAll()
    {
        if (isSellingAnim || cards.Count == 0) return;
        bool allSelected = selectedDinos.Count == cards.Count;
        selectedDinos.Clear();
        if (!allSelected)
        {
            for (int i = 0; i < cards.Count; i++)
                selectedDinos.Add(cards[i].dino);
        }
        for (int i = 0; i < cards.Count; i++)
            UpdateCardSelectionVisual(cards[i], true);
        RefreshControls();
    }

    void RefreshControls()
    {
        if (sortValueBg != null) sortValueBg.color = sortMode == SortMode.Value ? activeButtonColor : inactiveButtonColor;
        if (sortRarityBg != null) sortRarityBg.color = sortMode == SortMode.Rarity ? activeButtonColor : inactiveButtonColor;
        string arrow = sortDescending ? "▼" : "▲";
        if (sortValueArrow != null) sortValueArrow.text = sortMode == SortMode.Value ? arrow : "▼";
        if (sortRarityArrow != null) sortRarityArrow.text = sortMode == SortMode.Rarity ? arrow : "▼";

        bool allSel = cards.Count > 0 && selectedDinos.Count == cards.Count;
        if (selectAllDot != null)
            selectAllDot.color = allSel ? activeButtonColor : new Color(0.12f, 0.14f, 0.12f, 1f);

        int count = selectedDinos.Count;
        double total = 0;
        foreach (OwnedDino d in selectedDinos)
            if (d != null) total += d.SellValue;

        if (totalValueText != null) totalValueText.text = "$" + NumberFormat.Short(total);
        if (sellButtonText != null) sellButtonText.text = string.Format(sellButtonFormat, count);
        if (sellButtonBg != null) sellButtonBg.color = count > 0 ? sellActiveColor : sellEmptyColor;
        if (sellButton != null) sellButton.interactable = count > 0 && !isSellingAnim;
    }

    void OnClickSell()
    {
        if (isSellingAnim || selectedDinos.Count == 0) return;
        StartCoroutine(SellSequenceRoutine());
    }

    IEnumerator SellSequenceRoutine()
    {
        isSellingAnim = true;
        RefreshControls();

        List<CardItem> targets = new List<CardItem>();
        List<OwnedDino> soldList = new List<OwnedDino>();
        double totalEarned = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (selectedDinos.Contains(cards[i].dino))
            {
                targets.Add(cards[i]);
                soldList.Add(cards[i].dino);
                lockedSelling.Add(cards[i].dino);
                totalEarned += cards[i].dino.SellValue;
            }
        }

        // Pha 1: Các ô được chọn thu nhỏ về phía góc trái của chính ô đó
        float shrinkDur = Mathf.Max(0.06f, sellShrinkToCornerDuration);
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / shrinkDur);
            float ease = 1f - t * t;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].visualInner != null)
                    targets[i].visualInner.localScale = Vector3.one * Mathf.Lerp(0.18f, selectedScale, ease);
            }
            yield return null;
        }

        // Tạo icon tiền tại góc trái mỗi ô vừa thu nhỏ và ẩn khung ô đó đi
        List<RectTransform> coins = new List<RectTransform>();
        List<Vector3> coinStarts = new List<Vector3>();
        Transform coinParent = fxLayer != null ? fxLayer : panelRoot;
        Sprite moneySp = moneySprite != null ? moneySprite : GetMoneySprite();

        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform inner = targets[i].visualInner;
            Vector3 worldStart = inner != null ? inner.position : Vector3.zero;
            if (inner != null) inner.gameObject.SetActive(false);

            GameObject cGo = new GameObject("FlyingCoin_" + i, typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(coinParent, false);
            RectTransform cRt = cGo.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(46f, 46f);
            cRt.position = worldStart;
            Image cImg = cGo.GetComponent<Image>();
            cImg.sprite = moneySp;
            cImg.preserveAspect = true;
            cImg.raycastTarget = false;
            coins.Add(cRt);
            coinStarts.Add(worldStart);
        }

        // Pha 2: Icon tiền bay nhích lên nhẹ rồi rơi vào nút Sell
        Vector3 destWorld = sellButton != null ? sellButton.transform.position : panelRoot.position;
        float flyDur = Mathf.Max(0.12f, sellFlyToButtonDuration);
        t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / flyDur);
            float ease = t * t * (3f - 2f * t);
            float arcY = Mathf.Sin(t * Mathf.PI) * sellCoinArcHeight + (t < 0.25f ? (t / 0.25f) * 18f : (1f - t) * 18f);
            for (int i = 0; i < coins.Count; i++)
            {
                if (coins[i] == null) continue;
                Vector3 pos = Vector3.Lerp(coinStarts[i], destWorld, ease);
                pos.y += arcY;
                coins[i].position = pos;
                float sc = t < 0.2f ? Mathf.Lerp(0.6f, 1.15f, t / 0.2f) : Mathf.Lerp(1.15f, 0.72f, (t - 0.2f) / 0.8f);
                coins[i].localScale = Vector3.one * sc;
                coins[i].localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 14f);
            }
            yield return null;
        }

        for (int i = 0; i < coins.Count; i++)
            if (coins[i] != null) Destroy(coins[i].gameObject);

        // Pha 3: Cộng tiền thật, nảy nút Sell, bay chữ +$... lên từ nút Sell, hiện lịch sử bán
        if (gm == null) gm = GameManager.Instance;
        if (gm != null)
        {
            for (int i = 0; i < soldList.Count; i++)
                gm.Inventory.Remove(soldList[i]);
            gm.AddCash(totalEarned);
        }

        lockedSelling.Clear();
        selectedDinos.Clear();
        sellButtonPunchTimer = 0.22f;
        SpawnFloatingGain("+$" + NumberFormat.Short(totalEarned), destWorld);
        ShowSoldHistory(soldList.Count, totalEarned);

        isSellingAnim = false;
        Rebuild();
    }

    void SpawnFloatingGain(string text, Vector3 startWorldPos)
    {
        Transform parent = fxLayer != null ? fxLayer : panelRoot;
        GameObject go = new GameObject("FloatingSellGain", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260f, 52f);
        rt.position = startWorldPos + Vector3.up * 26f;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.24f, 0.96f, 0.30f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        StartCoroutine(AnimateFloatingGainRoutine(rt, go.GetComponent<CanvasGroup>()));
    }

    IEnumerator AnimateFloatingGainRoutine(RectTransform rt, CanvasGroup cg)
    {
        float dur = Mathf.Max(0.2f, floatingGainDuration);
        float t = 0f;
        Vector3 start = rt.position;
        while (t < 1f && rt != null)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / dur);
            rt.position = start + Vector3.up * (Mathf.Sin(t * Mathf.PI * 0.5f) * 95f);
            float scale = t < 0.2f ? Mathf.Lerp(0.6f, 1.18f, t / 0.2f) : Mathf.Lerp(1.18f, 1f, (t - 0.2f) / 0.8f);
            rt.localScale = Vector3.one * scale;
            if (cg != null) cg.alpha = t > 0.65f ? 1f - (t - 0.65f) / 0.35f : 1f;
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    void ShowSoldHistory(int count, double total)
    {
        if (soldHistoryText == null) return;
        string moneyStr = NumberFormat.Short(total);
        soldHistoryText.text = count == 1
            ? string.Format(soldSingularFormat, moneyStr)
            : string.Format(soldPluralFormat, count, moneyStr);
        soldHistoryText.gameObject.SetActive(true);
        if (soldHistoryGroup != null) soldHistoryGroup.alpha = 1f;
        historyTimer = soldHistoryDuration;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < cards.Count; i++)
        {
            CardItem item = cards[i];
            if (item.popTimer <= 0f || item.visualInner == null) continue;
            item.popTimer = Mathf.Max(0f, item.popTimer - dt);
            float dur = Mathf.Max(0.05f, cardClickPopDuration);
            float p = 1f - item.popTimer / dur;
            float baseScale = selectedDinos.Contains(item.dino) ? selectedScale : 1f;
            item.visualInner.localScale = Vector3.one * (baseScale + Mathf.Sin(p * Mathf.PI) * 0.09f);
        }

        if (sellButtonPunchTimer > 0f && sellButton != null)
        {
            sellButtonPunchTimer = Mathf.Max(0f, sellButtonPunchTimer - dt);
            float s = 1f + Mathf.Sin((sellButtonPunchTimer / 0.22f) * Mathf.PI) * 0.14f;
            sellButton.transform.localScale = Vector3.one * s;
        }
        else if (sellButton != null)
        {
            sellButton.transform.localScale = Vector3.one;
        }

        if (historyTimer > 0f && soldHistoryText != null && soldHistoryText.gameObject.activeSelf)
        {
            historyTimer -= dt;
            if (soldHistoryGroup != null)
                soldHistoryGroup.alpha = historyTimer < 0.4f ? Mathf.Clamp01(historyTimer / 0.4f) : 1f;
            if (historyTimer <= 0f)
                soldHistoryText.gameObject.SetActive(false);
        }
    }

    Sprite GetPawSprite()
    {
        if (spPaw != null) return spPaw;
        const int s = 32;
        texPaw = MakeTex(s);
        Color orange = new Color(0.98f, 0.58f, 0.12f, 1f);
        FillCircle(texPaw, 16, 12, 7.5f, orange);
        FillCircle(texPaw, 7, 21, 3.2f, orange);
        FillCircle(texPaw, 13, 25, 3.2f, orange);
        FillCircle(texPaw, 19, 25, 3.2f, orange);
        FillCircle(texPaw, 25, 21, 3.2f, orange);
        texPaw.Apply();
        spPaw = Sprite.Create(texPaw, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return spPaw;
    }

    Sprite GetMoneySprite()
    {
        if (spMoney != null) return spMoney;
        const int s = 32;
        texMoney = MakeTex(s);
        Color greenDark = new Color(0.08f, 0.52f, 0.20f, 1f);
        Color greenBright = new Color(0.18f, 0.80f, 0.32f, 1f);
        Color band = new Color(0.95f, 0.82f, 0.22f, 1f);
        FillRect(texMoney, 3, 7, 29, 23, greenDark);
        FillRect(texMoney, 4, 10, 28, 25, greenBright);
        FillRect(texMoney, 13, 8, 19, 25, band);
        texMoney.Apply();
        spMoney = Sprite.Create(texMoney, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return spMoney;
    }

    Sprite GetCheckSprite()
    {
        if (spCheck != null) return spCheck;
        const int s = 32;
        texCheck = MakeTex(s);
        FillCircle(texCheck, 16, 16, 14f, Color.white);
        Color dark = new Color(0.08f, 0.12f, 0.08f, 1f);
        for (int i = 0; i <= 5; i++) FillCircle(texCheck, 9 + i, 15 - i, 1.8f, dark);
        for (int i = 0; i <= 9; i++) FillCircle(texCheck, 14 + i, 10 + i * 1.25f, 1.8f, dark);
        texCheck.Apply();
        spCheck = Sprite.Create(texCheck, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return spCheck;
    }

    Sprite GetDinoFallbackSprite()
    {
        if (spDino != null) return spDino;
        const int s = 32;
        texDino = MakeTex(s);
        FillRect(texDino, 6, 6, 23, 24, Color.white);
        FillRect(texDino, 18, 14, 28, 23, Color.white);
        FillRect(texDino, 18, 19, 20, 21, new Color(0.15f, 0.15f, 0.18f, 1f));
        texDino.Apply();
        spDino = Sprite.Create(texDino, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return spDino;
    }

    static Texture2D MakeTex(int s)
    {
        var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, Color.clear);
        return tex;
    }

    static void FillCircle(Texture2D tex, float cx, float cy, float r, Color col)
    {
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    tex.SetPixel(x, y, col);
    }

    static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
    {
        for (int y = Mathf.Max(0, y0); y <= Mathf.Min(tex.height - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(tex.width - 1, x1); x++)
                tex.SetPixel(x, y, col);
    }
}
