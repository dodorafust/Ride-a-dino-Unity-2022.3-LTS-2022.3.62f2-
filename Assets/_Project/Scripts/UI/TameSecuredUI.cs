using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hiển thị thông báo khi Tame thành công:
// 1) Ô chữ nhật "IT'S YOURS!" xổ ra -> 2) Thu gọn bay về ô vuông đầu tiên -> 3) Tăng số "X SECURED" & xếp lưới.
public class TameSecuredUI : MonoBehaviour
{
    public static TameSecuredUI Instance { get; private set; }

    [Header("Tham chiếu UI (Tự tạo hoặc dựng sẵn trong Scene)")]
    public RectTransform bannerRoot;
    public CanvasGroup bannerCanvasGroup;
    public Image bannerBorderImage;
    public Image bannerFillImage;
    public Image bannerIconImage;
    public TMP_Text bannerTitleText;
    public TMP_Text bannerSubText;
    public RectTransform securedRoot;
    public TMP_Text securedHeaderText;
    public RectTransform cardsGridRoot;
    public RectTransform cardTemplate;

    [Header("Vị trí & Kích thước (kéo trực tiếp trên Scene hoặc bật Override để nhập số)")]
    public bool overrideLayoutFromInspector = false;
    public Vector2 bannerAnchoredPos = new Vector2(245f, -335f);
    public Vector2 bannerSize = new Vector2(390f, 88f);
    public float bannerTiltAngle = 8f;
    public Vector2 securedAnchoredPos = new Vector2(48f, -540f);
    public Vector2 cardSize = new Vector2(78f, 78f);
    public Vector2 cardSpacing = new Vector2(8f, 8f);
    [Min(1)] public int columns = 4;
    [Min(1)] public int maxVisibleCards = 8;

    [Header("Màu sắc & Nội dung chữ")]
    public string bannerMessage = "IT'S YOURS!";
    public string securedHeaderFormat = "{0} SECURED";
    [Min(3)] public int shortNameMaxLength = 7;
    public Color bannerBorderColor = new Color(0.18f, 0.88f, 0.28f, 1f);
    public Color bannerFillColor = new Color(0.06f, 0.22f, 0.09f, 0.96f);
    public Color bannerTitleColor = new Color(0.22f, 0.94f, 0.32f, 1f);
    public Color securedHeaderColor = new Color(0.28f, 0.96f, 0.36f, 1f);
    public bool colorCardByRarity = true;
    public Color defaultCardColor = new Color(0.24f, 0.56f, 0.88f, 0.95f);
    public bool hideDefaultToastOnTame = true;

    [Header("Thời gian Hiệu ứng")]
    public float popInDuration = 0.22f;
    public float bannerHoldDuration = 1.05f;
    public float iconWiggleDuration = 0.24f;
    public float iconWiggleAngle = 16f;
    public float dropIconDuration = 0.30f;
    public float cardShiftSpeed = 14f;

    enum AnimPhase { Idle, PopIn, Hold, DropIcon }

    class CardSlot
    {
        public RectTransform root;
        public OwnedDino dino;
        public Vector2 targetPos;
        public float popTimer;
    }

    readonly List<CardSlot> activeCards = new List<CardSlot>();
    OwnedDino pendingDino;
    AnimPhase phase = AnimPhase.Idle;
    float phaseTimer;
    float headerPunchTimer;
    int securedCount;
    bool wasInRun;
    bool reserveFirstSlot;
    Image flyingIconImage;
    Vector3 dropStartWorldPos;
    Vector2 dropStartSize;
    float dropStartRotZ;
    Texture2D fallbackIconTex;
    Sprite fallbackIconSprite;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureBuilt();
        SyncLayoutFromSceneIfNeeded();
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
        ClearRunSecured();
    }

    void SyncLayoutFromSceneIfNeeded()
    {
        if (overrideLayoutFromInspector) return;
        if (bannerRoot != null)
        {
            bannerAnchoredPos = bannerRoot.anchoredPosition;
            bannerSize = bannerRoot.sizeDelta;
            bannerTiltAngle = Mathf.DeltaAngle(0f, bannerRoot.localEulerAngles.z);
        }
        if (securedRoot != null)
            securedAnchoredPos = securedRoot.anchoredPosition;
        if (cardTemplate != null && cardTemplate.sizeDelta.x > 1f && cardTemplate.sizeDelta.y > 1f)
            cardSize = cardTemplate.sizeDelta;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (fallbackIconTex != null) Destroy(fallbackIconTex);
    }

    public void BuildInEditor()
    {
        EnsureBuilt();
        if (bannerTitleText != null) bannerTitleText.text = bannerMessage;
        if (bannerSubText != null) bannerSubText.text = "Dodo";
        if (securedHeaderText != null) securedHeaderText.text = FormatSecuredHeader(1);
        if (bannerRoot != null) bannerRoot.gameObject.SetActive(true);
        if (securedRoot != null) securedRoot.gameObject.SetActive(true);
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(true);
    }

    public void EnsureBuilt()
    {
        if (bannerRoot != null && securedRoot != null && cardTemplate != null) return;

        Transform parent = transform;
        if (GetComponent<Canvas>() == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && GetComponent<HudController>() != null)
            {
                Transform existing = canvas.transform.Find("TameSecuredUI");
                parent = existing != null ? existing : CreateUIRoot(canvas.transform);
            }
        }

        if (bannerRoot == null) BuildBanner(parent);
        if (securedRoot == null) BuildSecuredContainer(parent);
        if (cardTemplate == null) BuildCardTemplate(cardsGridRoot != null ? cardsGridRoot : securedRoot);
    }

    static Transform CreateUIRoot(Transform canvasParent)
    {
        GameObject go = new GameObject("TameSecuredUI", typeof(RectTransform));
        go.transform.SetParent(canvasParent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    void BuildBanner(Transform parent)
    {
        GameObject go = new GameObject("TameBanner", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        bannerRoot = go.GetComponent<RectTransform>();
        bannerRoot.anchorMin = new Vector2(0f, 1f);
        bannerRoot.anchorMax = new Vector2(0f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 0.5f);
        bannerRoot.anchoredPosition = bannerAnchoredPos;
        bannerRoot.sizeDelta = bannerSize;
        bannerRoot.localEulerAngles = new Vector3(0f, 0f, bannerTiltAngle);
        bannerCanvasGroup = go.GetComponent<CanvasGroup>();
        bannerCanvasGroup.interactable = false;
        bannerCanvasGroup.blocksRaycasts = false;

        GameObject outerGo = new GameObject("OuterBorder", typeof(RectTransform), typeof(Image));
        outerGo.transform.SetParent(bannerRoot, false);
        RectTransform oRt = outerGo.GetComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero;
        oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero;
        oRt.offsetMax = Vector2.zero;
        Image outerImg = outerGo.GetComponent<Image>();
        outerImg.color = new Color(0.04f, 0.08f, 0.05f, 0.98f);
        outerImg.raycastTarget = false;

        GameObject borderGo = new GameObject("ColorBorder", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(oRt, false);
        RectTransform bRt = borderGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(3f, 3f);
        bRt.offsetMax = new Vector2(-3f, -3f);
        bannerBorderImage = borderGo.GetComponent<Image>();
        bannerBorderImage.color = bannerBorderColor;
        bannerBorderImage.raycastTarget = false;

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(bRt, false);
        RectTransform fRt = fillGo.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(5f, 5f);
        fRt.offsetMax = new Vector2(-5f, -5f);
        bannerFillImage = fillGo.GetComponent<Image>();
        bannerFillImage.color = bannerFillColor;
        bannerFillImage.raycastTarget = false;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(fRt, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0f, 0.5f);
        icRt.anchorMax = new Vector2(0f, 0.5f);
        icRt.pivot = new Vector2(0f, 0.5f);
        icRt.anchoredPosition = new Vector2(12f, 0f);
        icRt.sizeDelta = new Vector2(64f, 64f);
        bannerIconImage = iconGo.GetComponent<Image>();
        bannerIconImage.preserveAspect = true;
        bannerIconImage.raycastTarget = false;

        bannerTitleText = CreateTmp(fRt, "TitleText", new Vector2(0.24f, 0.05f), new Vector2(0.97f, 0.95f),
                                    34f, bannerTitleColor, TextAlignmentOptions.Center, bannerMessage);

        GameObject subPill = new GameObject("SubLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        subPill.transform.SetParent(bannerRoot, false);
        RectTransform sRt = subPill.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 0f);
        sRt.anchorMax = new Vector2(0.5f, 0f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -6f);
        sRt.sizeDelta = new Vector2(360f, 34f);
        TextMeshProUGUI subTmp = subPill.GetComponent<TextMeshProUGUI>();
        subTmp.fontSize = 23f;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.color = Color.white;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;
        bannerSubText = subTmp;
    }

    void BuildSecuredContainer(Transform parent)
    {
        GameObject go = new GameObject("SecuredContainer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        securedRoot = go.GetComponent<RectTransform>();
        securedRoot.anchorMin = new Vector2(0f, 1f);
        securedRoot.anchorMax = new Vector2(0f, 1f);
        securedRoot.pivot = new Vector2(0f, 1f);
        securedRoot.anchoredPosition = securedAnchoredPos;
        securedRoot.sizeDelta = new Vector2(columns * (cardSize.x + cardSpacing.x), 240f);

        GameObject hdrGo = new GameObject("SecuredHeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hdrGo.transform.SetParent(securedRoot, false);
        RectTransform hRt = hdrGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0f, 1f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(320f, 30f);
        TextMeshProUGUI hTmp = hdrGo.GetComponent<TextMeshProUGUI>();
        hTmp.fontSize = 22f;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.color = securedHeaderColor;
        hTmp.alignment = TextAlignmentOptions.Left;
        hTmp.raycastTarget = false;
        hTmp.text = FormatSecuredHeader(0);
        securedHeaderText = hTmp;

        GameObject gridGo = new GameObject("CardsGrid", typeof(RectTransform));
        gridGo.transform.SetParent(securedRoot, false);
        cardsGridRoot = gridGo.GetComponent<RectTransform>();
        cardsGridRoot.anchorMin = new Vector2(0f, 1f);
        cardsGridRoot.anchorMax = new Vector2(1f, 1f);
        cardsGridRoot.pivot = new Vector2(0f, 1f);
        cardsGridRoot.anchoredPosition = new Vector2(0f, -34f);
        cardsGridRoot.sizeDelta = new Vector2(0f, 200f);
    }

    void BuildCardTemplate(Transform parent)
    {
        GameObject cardGo = new GameObject("SecuredCardTemplate", typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(parent, false);
        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = cardSize;
        Image outerImg = cardGo.GetComponent<Image>();
        outerImg.color = new Color(0.05f, 0.07f, 0.09f, 0.96f);
        outerImg.raycastTarget = false;

        GameObject bgGo = new GameObject("CardBg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(rt, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(2.5f, 2.5f);
        bgRt.offsetMax = new Vector2(-2.5f, -2.5f);
        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = defaultCardColor;
        bgImg.raycastTarget = false;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(bgRt, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.14f, 0.24f);
        icRt.anchorMax = new Vector2(0.86f, 0.90f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = GetFallbackSprite();
        icImg.preserveAspect = true;
        icImg.raycastTarget = false;

        CreateTmp(bgRt, "BadgeText", new Vector2(0.06f, 0.70f), new Vector2(0.94f, 0.96f),
                  11f, Color.white, TextAlignmentOptions.TopLeft, "");
        CreateTmp(bgRt, "NameText", new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.30f),
                  13f, Color.white, TextAlignmentOptions.Bottom, "Dino");

        cardTemplate = rt;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, Vector2 aMin, Vector2 aMax,
                                     float fontSize, Color color, TextAlignmentOptions align, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 9f;
        tmp.fontSizeMax = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    public void NotifyTamed(OwnedDino dino)
    {
        if (dino == null || dino.data == null) return;
        EnsureBuilt();

        // Nếu con trước chưa thu gọn xong mà đã bắt được con mới -> chốt luôn con trước vào lưới
        if (pendingDino != null)
            CommitPendingDinoToGrid();

        pendingDino = dino;
        phase = AnimPhase.PopIn;
        phaseTimer = 0f;

        if (bannerBorderImage != null) bannerBorderImage.color = bannerBorderColor;
        if (bannerFillImage != null) bannerFillImage.color = bannerFillColor;
        if (bannerTitleText != null)
        {
            bannerTitleText.text = bannerMessage;
            bannerTitleText.color = bannerTitleColor;
        }
        if (bannerSubText != null)
        {
            bannerSubText.text = dino.Title;
            bannerSubText.color = DinoColors.Of(dino.data.rarity);
            bannerSubText.alpha = 1f;
        }
        if (bannerIconImage != null)
        {
            bool hasIcon = dino.data.icon != null;
            bannerIconImage.sprite = hasIcon ? dino.data.icon : GetFallbackSprite();
            bannerIconImage.color = hasIcon ? Color.white : DinoColors.Of(dino.data.rarity);
            bannerIconImage.enabled = true;
            bannerIconImage.rectTransform.localScale = Vector3.one;
            bannerIconImage.rectTransform.localEulerAngles = Vector3.zero;
        }
        if (flyingIconImage != null) flyingIconImage.gameObject.SetActive(false);
        reserveFirstSlot = false;
        LayoutCards();

        if (bannerRoot != null)
        {
            bannerRoot.anchoredPosition = bannerAnchoredPos;
            bannerRoot.sizeDelta = bannerSize;
            bannerRoot.localEulerAngles = new Vector3(0f, 0f, bannerTiltAngle);
            bannerRoot.localScale = Vector3.one * 0.55f;
            bannerRoot.gameObject.SetActive(true);
        }
        if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;
    }

    public void ClearRunSecured()
    {
        pendingDino = null;
        phase = AnimPhase.Idle;
        phaseTimer = 0f;
        securedCount = 0;
        reserveFirstSlot = false;
        for (int i = 0; i < activeCards.Count; i++)
            if (activeCards[i].root != null) Destroy(activeCards[i].root.gameObject);
        activeCards.Clear();

        if (flyingIconImage != null) flyingIconImage.gameObject.SetActive(false);
        if (bannerIconImage != null) bannerIconImage.enabled = true;
        if (bannerRoot != null) bannerRoot.gameObject.SetActive(false);
        if (securedRoot != null) securedRoot.gameObject.SetActive(false);
    }

    [ContextMenu("Test Tame Notification")]
    public void DebugTestTame()
    {
        DinoSpawner sp = FindObjectOfType<DinoSpawner>();
        DinoData data = null;
        if (sp != null && sp.table != null && sp.table.entries.Count > 0)
            data = sp.table.entries[Random.Range(0, sp.table.entries.Count)].dino;
        if (data == null)
        {
            DinoData[] all = Resources.FindObjectsOfTypeAll<DinoData>();
            if (all != null && all.Length > 0) data = all[Random.Range(0, all.Length)];
        }
        if (data != null) NotifyTamed(new OwnedDino(data));
    }

    void CommitPendingDinoToGrid()
    {
        OwnedDino d = pendingDino;
        pendingDino = null;
        phase = AnimPhase.Idle;
        if (d == null || d.data == null) return;

        securedCount++;
        if (securedRoot != null)
        {
            securedRoot.anchoredPosition = securedAnchoredPos;
            securedRoot.gameObject.SetActive(true);
        }
        if (securedHeaderText != null)
        {
            securedHeaderText.text = FormatSecuredHeader(securedCount);
            securedHeaderText.color = securedHeaderColor;
        }
        headerPunchTimer = 0.22f;

        if (cardTemplate != null && cardsGridRoot != null)
        {
            reserveFirstSlot = false;
            RectTransform cardRt = Instantiate(cardTemplate, cardsGridRoot);
            cardRt.name = "SecuredCard_" + d.data.displayName;
            cardRt.gameObject.SetActive(true);
            cardRt.anchorMin = new Vector2(0f, 1f);
            cardRt.anchorMax = new Vector2(0f, 1f);
            cardRt.pivot = new Vector2(0f, 1f);
            cardRt.sizeDelta = cardSize;
            cardRt.anchoredPosition = Vector2.zero;
            BindCardVisual(cardRt, d);
            activeCards.Insert(0, new CardSlot { root = cardRt, dino = d, targetPos = Vector2.zero, popTimer = 0.22f });

            while (activeCards.Count > Mathf.Max(1, maxVisibleCards))
            {
                int last = activeCards.Count - 1;
                if (activeCards[last].root != null) Destroy(activeCards[last].root.gameObject);
                activeCards.RemoveAt(last);
            }
            LayoutCards();
        }
    }

    void BindCardVisual(RectTransform cardRt, OwnedDino d)
    {
        Transform bgT = cardRt.Find("CardBg");
        Image bgImg = bgT != null ? bgT.GetComponent<Image>() : cardRt.GetComponent<Image>();
        if (bgImg != null)
        {
            Color c = colorCardByRarity ? DinoColors.Of(d.data.rarity) : defaultCardColor;
            c.a = 0.92f;
            bgImg.color = c;
        }

        Transform icT = bgT != null ? bgT.Find("Icon") : cardRt.Find("Icon");
        Image icImg = icT != null ? icT.GetComponent<Image>() : null;
        if (icImg != null)
        {
            bool hasIcon = d.data.icon != null;
            icImg.sprite = hasIcon ? d.data.icon : GetFallbackSprite();
            icImg.color = hasIcon ? Color.white : Color.Lerp(DinoColors.Of(d.data.rarity), Color.white, 0.35f);
        }

        Transform badgeT = bgT != null ? bgT.Find("BadgeText") : cardRt.Find("BadgeText");
        TMP_Text badgeTmp = badgeT != null ? badgeT.GetComponent<TMP_Text>() : null;
        if (badgeTmp != null)
        {
            if (!string.IsNullOrEmpty(d.SizeTag))
            {
                badgeTmp.text = d.SizeTag;
                badgeTmp.color = DinoSizeModifiers.ColorOf(d.sizeType);
            }
            else if (d.variant != null && !d.variant.isNormal)
            {
                badgeTmp.text = d.variant.displayName.ToUpperInvariant();
                badgeTmp.color = new Color(1f, 0.92f, 0.3f, 1f);
            }
            else
            {
                badgeTmp.text = "";
            }
        }

        Transform nameT = bgT != null ? bgT.Find("NameText") : cardRt.Find("NameText");
        TMP_Text nameTmp = nameT != null ? nameT.GetComponent<TMP_Text>() : null;
        if (nameTmp != null)
        {
            string n = d.data.displayName;
            nameTmp.text = n.Length > shortNameMaxLength ? n.Substring(0, shortNameMaxLength) + "." : n;
        }
    }

    void LayoutCards()
    {
        int cols = Mathf.Max(1, columns);
        int offset = reserveFirstSlot ? 1 : 0;
        for (int i = 0; i < activeCards.Count; i++)
        {
            RectTransform rt = activeCards[i].root;
            if (rt == null) continue;
            int slotIndex = i + offset;
            int col = slotIndex % cols;
            int row = slotIndex / cols;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = cardSize;
            activeCards[i].targetPos = new Vector2(col * (cardSize.x + cardSpacing.x),
                                                   -row * (cardSize.y + cardSpacing.y));
        }
    }

    Image EnsureFlyingIcon()
    {
        if (flyingIconImage != null) return flyingIconImage;
        Transform parent = bannerRoot != null && bannerRoot.parent != null ? bannerRoot.parent : transform;
        GameObject go = new GameObject("FlyingTameIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        flyingIconImage = go.GetComponent<Image>();
        flyingIconImage.preserveAspect = true;
        flyingIconImage.raycastTarget = false;
        go.SetActive(false);
        return flyingIconImage;
    }

    Vector3 GetFirstCardCenterWorldPos()
    {
        if (cardsGridRoot != null)
        {
            Vector3 localTopLeft = Vector3.zero;
            Vector3 localCenter = new Vector3(localTopLeft.x + cardSize.x * 0.5f, localTopLeft.y - cardSize.y * 0.5f, 0f);
            return cardsGridRoot.TransformPoint(localCenter);
        }
        if (securedRoot != null) return securedRoot.position;
        return bannerRoot != null ? bannerRoot.position : Vector3.zero;
    }

    string FormatSecuredHeader(int count)
    {
        if (string.IsNullOrEmpty(securedHeaderFormat)) return count + " SECURED";
        try { return string.Format(securedHeaderFormat, count); }
        catch { return count + " SECURED"; }
    }

    void Update()
    {
        PlayerController pc = PlayerController.Instance;
        bool inRun = pc != null && pc.RunActive && pc.State != PlayerState.Dead;
        if (wasInRun && !inRun && (securedCount > 0 || pendingDino != null))
            ClearRunSecured();
        wasInRun = inRun;

        if (securedRoot != null && securedRoot.gameObject.activeSelf)
        {
            securedRoot.anchoredPosition = securedAnchoredPos;
            if (headerPunchTimer > 0f && securedHeaderText != null)
            {
                headerPunchTimer = Mathf.Max(0f, headerPunchTimer - Time.deltaTime);
                float p = headerPunchTimer / 0.22f;
                float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.22f;
                securedHeaderText.rectTransform.localScale = Vector3.one * s;
            }
            else if (securedHeaderText != null)
            {
                securedHeaderText.rectTransform.localScale = Vector3.one;
            }

            float shiftK = 1f - Mathf.Exp(-Mathf.Max(1f, cardShiftSpeed) * Time.deltaTime);
            for (int i = 0; i < activeCards.Count; i++)
            {
                CardSlot slot = activeCards[i];
                if (slot.root == null) continue;
                slot.root.anchoredPosition = Vector2.Lerp(slot.root.anchoredPosition, slot.targetPos, shiftK);
                if (slot.popTimer > 0f)
                {
                    slot.popTimer = Mathf.Max(0f, slot.popTimer - Time.deltaTime);
                    float sp = 1f + Mathf.Sin((slot.popTimer / 0.22f) * Mathf.PI) * 0.18f;
                    slot.root.localScale = Vector3.one * sp;
                }
                else
                {
                    slot.root.localScale = Vector3.one;
                }
            }
        }

        if (phase == AnimPhase.Idle || bannerRoot == null) return;

        float dt = Time.deltaTime;
        phaseTimer += dt;

        if (phase == AnimPhase.PopIn)
        {
            float dur = Mathf.Max(0.05f, popInDuration);
            float t = Mathf.Clamp01(phaseTimer / dur);
            float s = t < 0.72f ? Mathf.Lerp(0.55f, 1.08f, t / 0.72f) : Mathf.Lerp(1.08f, 1f, (t - 0.72f) / 0.28f);
            bannerRoot.anchoredPosition = bannerAnchoredPos;
            bannerRoot.sizeDelta = bannerSize;
            bannerRoot.localScale = Vector3.one * s;
            bannerRoot.localEulerAngles = new Vector3(0f, 0f, bannerTiltAngle);
            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = Mathf.Clamp01(t * 1.4f);

            if (t >= 1f)
            {
                phase = AnimPhase.Hold;
                phaseTimer = 0f;
            }
        }
        else if (phase == AnimPhase.Hold)
        {
            float dur = Mathf.Max(0.15f, bannerHoldDuration);
            float t = Mathf.Clamp01(phaseTimer / dur);
            bannerRoot.anchoredPosition = bannerAnchoredPos;
            bannerRoot.localScale = Vector3.one;
            float sway = Mathf.Sin(phaseTimer * 6f) * 1.1f;
            bannerRoot.localEulerAngles = new Vector3(0f, 0f, bannerTiltAngle + sway);
            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f;

            // Cuối pha Hold: chỉ riêng Icon lắc động đậy chuẩn bị rơi xuống
            float wiggleDur = Mathf.Clamp(iconWiggleDuration, 0.05f, dur * 0.8f);
            float wiggleStart = dur - wiggleDur;
            if (bannerIconImage != null)
            {
                if (phaseTimer >= wiggleStart)
                {
                    float wt = Mathf.Clamp01((phaseTimer - wiggleStart) / wiggleDur);
                    float wAngle = Mathf.Sin(wt * Mathf.PI * 5f) * iconWiggleAngle * (1f - wt * 0.3f);
                    float wScale = 1f + Mathf.Sin(wt * Mathf.PI) * 0.22f;
                    bannerIconImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, wAngle);
                    bannerIconImage.rectTransform.localScale = Vector3.one * wScale;
                }
                else
                {
                    bannerIconImage.rectTransform.localEulerAngles = Vector3.zero;
                    bannerIconImage.rectTransform.localScale = Vector3.one;
                }
            }

            if (t >= 1f)
            {
                phase = AnimPhase.DropIcon;
                phaseTimer = 0f;

                if (securedRoot != null)
                {
                    securedRoot.anchoredPosition = securedAnchoredPos;
                    if (!securedRoot.gameObject.activeSelf)
                    {
                        securedRoot.gameObject.SetActive(true);
                        if (securedHeaderText != null) securedHeaderText.text = FormatSecuredHeader(securedCount);
                    }
                }

                // Các ô vuông hiện có dịch sang phải mở trống ô đầu tiên
                reserveFirstSlot = true;
                LayoutCards();

                // Khởi chạy riêng Icon rơi từ bảng xuống ô đầu tiên
                Image fly = EnsureFlyingIcon();
                if (bannerIconImage != null)
                {
                    dropStartWorldPos = bannerIconImage.rectTransform.position;
                    dropStartSize = bannerIconImage.rectTransform.rect.size;
                    dropStartRotZ = bannerIconImage.rectTransform.eulerAngles.z;
                    fly.sprite = bannerIconImage.sprite;
                    fly.color = bannerIconImage.color;
                    bannerIconImage.enabled = false;
                }
                else
                {
                    dropStartWorldPos = bannerRoot.position;
                    dropStartSize = new Vector2(64f, 64f);
                    dropStartRotZ = bannerTiltAngle;
                    fly.sprite = GetFallbackSprite();
                    fly.color = Color.white;
                }
                fly.rectTransform.sizeDelta = dropStartSize;
                fly.rectTransform.position = dropStartWorldPos;
                fly.rectTransform.localScale = Vector3.one;
                fly.rectTransform.eulerAngles = new Vector3(0f, 0f, dropStartRotZ);
                fly.gameObject.SetActive(true);
            }
        }
        else if (phase == AnimPhase.DropIcon)
        {
            float dur = Mathf.Max(0.08f, dropIconDuration);
            float t = Mathf.Clamp01(phaseTimer / dur);

            // Bảng chữ nhật đứng nguyên tại chỗ và mờ dần biến mất
            bannerRoot.anchoredPosition = bannerAnchoredPos;
            bannerRoot.localScale = Vector3.one;
            if (bannerSubText != null) bannerSubText.alpha = 1f - Mathf.Clamp01(t * 2f);
            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t * 1.4f);

            // Icon rơi cong nhẹ xuống đúng tâm ô đầu tiên
            if (flyingIconImage != null)
            {
                Vector3 targetWorldPos = GetFirstCardCenterWorldPos();
                float easePos = t * t * (3f - 2f * t);
                Vector3 pos = Vector3.Lerp(dropStartWorldPos, targetWorldPos, easePos);
                pos.y += Mathf.Sin(t * Mathf.PI) * 26f;
                flyingIconImage.rectTransform.position = pos;

                Vector2 targetIconSize = new Vector2(cardSize.x * 0.68f, cardSize.y * 0.64f);
                flyingIconImage.rectTransform.sizeDelta = Vector2.Lerp(dropStartSize, targetIconSize, easePos);
                float rotZ = Mathf.LerpAngle(dropStartRotZ, 0f, easePos);
                flyingIconImage.rectTransform.eulerAngles = new Vector3(0f, 0f, rotZ);
                Color fc = flyingIconImage.color;
                fc.a = 1f;
                flyingIconImage.color = fc;
            }

            if (t >= 1f)
            {
                if (flyingIconImage != null) flyingIconImage.gameObject.SetActive(false);
                if (bannerIconImage != null) bannerIconImage.enabled = true;
                bannerRoot.gameObject.SetActive(false);
                CommitPendingDinoToGrid();
            }
        }
    }

    Sprite GetFallbackSprite()
    {
        if (fallbackIconSprite != null) return fallbackIconSprite;

        const int s = 32;
        fallbackIconTex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        fallbackIconTex.filterMode = FilterMode.Point;
        Color clear = Color.clear;
        Color white = Color.white;
        Color dark = new Color(0.15f, 0.15f, 0.18f, 1f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                fallbackIconTex.SetPixel(x, y, clear);

        // Vẽ đầu Dino khối hộp đơn giản ở giữa
        for (int y = 6; y < 25; y++)
            for (int x = 6; x < 24; x++)
                fallbackIconTex.SetPixel(x, y, white);
        for (int y = 14; y < 24; y++)
            for (int x = 18; x < 29; x++)
                fallbackIconTex.SetPixel(x, y, white);
        // Mắt
        for (int y = 19; y < 22; y++)
            for (int x = 18; x < 21; x++)
                fallbackIconTex.SetPixel(x, y, dark);

        fallbackIconTex.Apply();
        fallbackIconSprite = Sprite.Create(fallbackIconTex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return fallbackIconSprite;
    }
}
