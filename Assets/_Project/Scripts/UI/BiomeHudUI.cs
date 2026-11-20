using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hiển thị thanh tiến trình Biome trên đầu màn hình (tự ẩn khi ở ngoài vùng chạy)
// và hiệu ứng Banner chữ khi bước sang Biome mới.
public class BiomeHudUI : MonoBehaviour
{
    [Header("Vị trí & Kích thước thanh Biome (Top Center)")]
    public Vector2 barPos = new Vector2(0f, -16f);
    public Vector2 cardSize = new Vector2(84f, 74f);
    public float cardSpacing = 4f;
    public Sprite playerAvatarSprite;

    [Header("Hiệu ứng Banner chuyển Biome")]
    public Vector2 bannerPos = new Vector2(0f, -205f);
    public string bannerSubtitle = "ENTERING BIOME";
    public float bannerFadeIn = 0.26f;
    public float bannerHold = 1.75f;
    public float bannerFadeOut = 0.5f;

    struct CardVisual
    {
        public BiomeSection section;
        public Image borderImage;
        public Image fillImage;
        public Image iconImage;
        public Image groundBandImage;
        public TextMeshProUGUI nameText;
    }

    readonly List<CardVisual> cards = new List<CardVisual>();

    RectTransform barRoot;
    CanvasGroup barGroup;
    RectTransform cardsContainer;
    RectTransform playerMarker;
    TextMeshProUGUI distanceText;
    RectTransform bestMarker;
    TextMeshProUGUI bestText;

    RectTransform bannerRoot;
    CanvasGroup bannerGroup;
    Image bannerTopLine;
    Image bannerBottomLine;
    TextMeshProUGUI bannerSubText;
    TextMeshProUGUI bannerTitleText;

    BiomeManager subscribedManager;
    PlayerController player;
    Sprite defaultCircleSprite;
    Sprite defaultHeadSprite;
    Texture2D generatedCircleTex;
    Texture2D generatedHeadTex;

    bool uiBuilt;
    bool showingBanner;
    float bannerElapsed;
    float totalStripWidth = 172f;
    int lastBestDisplay = -1;
    int lastDistDisplay = -1;

    void Start()
    {
        EnsureBuilt();
        BindManager();
        RebuildCards();
        UpdateBarVisibility(false, true);
    }

    void OnDestroy()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnSectionsChanged -= RebuildCards;
            subscribedManager.OnBiomeChanged -= HandleBiomeChanged;
        }
        if (generatedCircleTex != null) Destroy(generatedCircleTex);
        if (generatedHeadTex != null) Destroy(generatedHeadTex);
    }

    void BindManager()
    {
        BiomeManager bm = BiomeManager.Instance != null ? BiomeManager.Instance : FindObjectOfType<BiomeManager>();
        if (bm == null || bm == subscribedManager) return;

        if (subscribedManager != null)
        {
            subscribedManager.OnSectionsChanged -= RebuildCards;
            subscribedManager.OnBiomeChanged -= HandleBiomeChanged;
        }
        subscribedManager = bm;
        subscribedManager.OnSectionsChanged += RebuildCards;
        subscribedManager.OnBiomeChanged += HandleBiomeChanged;
    }

    void EnsureBuilt()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        CreateProceduralSprites();
        BuildProgressBar(parent);
        BuildBanner(parent);
    }

    void CreateProceduralSprites()
    {
        // Sprite hình tròn trắng mịn cho khung Avatar
        const int size = 64;
        generatedCircleTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float c = (size - 1) * 0.5f;
        float rOuter = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float alpha = Mathf.Clamp01((rOuter - d) * 1.4f);
                generatedCircleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        generatedCircleTex.Apply();
        defaultCircleSprite = Sprite.Create(generatedCircleTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        // Avatar nhân vật đơn giản (mặt + tóc nâu giống nhân vật trong game)
        generatedHeadTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color skin = new Color(0.95f, 0.78f, 0.62f, 1f);
        Color hair = new Color(0.28f, 0.16f, 0.10f, 1f);
        Color eye = new Color(0.12f, 0.12f, 0.14f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01((rOuter - d) * 1.4f);
                if (a <= 0.01f)
                {
                    generatedHeadTex.SetPixel(x, y, Color.clear);
                    continue;
                }
                Color col = skin;
                // Tóc phía trên
                if (y > size * 0.63f || (y > size * 0.50f && (x < size * 0.22f || x > size * 0.78f)))
                    col = hair;
                // Mắt
                float leftEye = Vector2.Distance(new Vector2(x, y), new Vector2(size * 0.38f, size * 0.47f));
                float rightEye = Vector2.Distance(new Vector2(x, y), new Vector2(size * 0.62f, size * 0.47f));
                if (leftEye < 3.4f || rightEye < 3.4f)
                    col = eye;
                col.a = a;
                generatedHeadTex.SetPixel(x, y, col);
            }
        }
        generatedHeadTex.Apply();
        defaultHeadSprite = Sprite.Create(generatedHeadTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void BuildProgressBar(Transform canvasParent)
    {
        GameObject rootGo = new GameObject("BiomeProgressBar", typeof(RectTransform), typeof(CanvasGroup));
        rootGo.transform.SetParent(canvasParent, false);
        barRoot = rootGo.GetComponent<RectTransform>();
        barRoot.anchorMin = new Vector2(0.5f, 1f);
        barRoot.anchorMax = new Vector2(0.5f, 1f);
        barRoot.pivot = new Vector2(0.5f, 1f);
        barRoot.anchoredPosition = barPos;
        barRoot.sizeDelta = new Vector2(240f, cardSize.y + 42f);

        barGroup = rootGo.GetComponent<CanvasGroup>();
        barGroup.interactable = false;
        barGroup.blocksRaycasts = false;

        GameObject cardsGo = new GameObject("Cards", typeof(RectTransform));
        cardsGo.transform.SetParent(barRoot, false);
        cardsContainer = cardsGo.GetComponent<RectTransform>();
        cardsContainer.anchorMin = new Vector2(0.5f, 1f);
        cardsContainer.anchorMax = new Vector2(0.5f, 1f);
        cardsContainer.pivot = new Vector2(0.5f, 1f);
        cardsContainer.anchoredPosition = Vector2.zero;

        // Mốc kỷ lục BEST XXX m
        GameObject bestGo = new GameObject("BestMarker", typeof(RectTransform));
        bestGo.transform.SetParent(barRoot, false);
        bestMarker = bestGo.GetComponent<RectTransform>();
        bestMarker.anchorMin = new Vector2(0.5f, 1f);
        bestMarker.anchorMax = new Vector2(0.5f, 1f);
        bestMarker.pivot = new Vector2(0.5f, 0.5f);
        bestMarker.anchoredPosition = new Vector2(0f, -cardSize.y - 2f);
        bestMarker.sizeDelta = new Vector2(180f, 34f);

        GameObject diamondGo = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
        diamondGo.transform.SetParent(bestMarker, false);
        RectTransform dRt = diamondGo.GetComponent<RectTransform>();
        dRt.sizeDelta = new Vector2(14f, 14f);
        dRt.anchoredPosition = new Vector2(0f, 3f);
        dRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image dImg = diamondGo.GetComponent<Image>();
        dImg.color = new Color(1f, 0.84f, 0.18f, 1f);
        dImg.raycastTarget = false;
        Outline dOut = diamondGo.AddComponent<Outline>();
        dOut.effectColor = Color.black;
        dOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject bestPillGo = new GameObject("BestPill", typeof(RectTransform), typeof(Image));
        bestPillGo.transform.SetParent(bestMarker, false);
        RectTransform bpRt = bestPillGo.GetComponent<RectTransform>();
        bpRt.sizeDelta = new Vector2(128f, 22f);
        bpRt.anchoredPosition = new Vector2(0f, 22f);
        Image bpImg = bestPillGo.GetComponent<Image>();
        bpImg.color = new Color(0.05f, 0.06f, 0.09f, 0.72f);
        bpImg.raycastTarget = false;

        GameObject bestTxtGo = new GameObject("BestText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bestTxtGo.transform.SetParent(bestPillGo.transform, false);
        RectTransform btRt = bestTxtGo.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;
        bestText = bestTxtGo.GetComponent<TextMeshProUGUI>();
        bestText.fontSize = 14f;
        bestText.fontStyle = FontStyles.Bold;
        bestText.alignment = TextAlignmentOptions.Center;
        bestText.color = new Color(1f, 0.86f, 0.25f, 1f);
        bestText.raycastTarget = false;
        bestText.text = "BEST 0 m";
        bestMarker.gameObject.SetActive(false);

        // Con trỏ vị trí người chơi
        GameObject markerGo = new GameObject("PlayerMarker", typeof(RectTransform));
        markerGo.transform.SetParent(barRoot, false);
        playerMarker = markerGo.GetComponent<RectTransform>();
        playerMarker.anchorMin = new Vector2(0.5f, 1f);
        playerMarker.anchorMax = new Vector2(0.5f, 1f);
        playerMarker.pivot = new Vector2(0.5f, 0.5f);
        playerMarker.anchoredPosition = new Vector2(0f, -cardSize.y + 4f);
        playerMarker.sizeDelta = new Vector2(120f, 66f);

        GameObject outerRing = new GameObject("OuterRing", typeof(RectTransform), typeof(Image));
        outerRing.transform.SetParent(playerMarker, false);
        RectTransform orRt = outerRing.GetComponent<RectTransform>();
        orRt.sizeDelta = new Vector2(36f, 36f);
        orRt.anchoredPosition = new Vector2(0f, 8f);
        Image orImg = outerRing.GetComponent<Image>();
        orImg.sprite = defaultCircleSprite;
        orImg.color = new Color(0.08f, 0.10f, 0.10f, 1f);
        orImg.raycastTarget = false;

        GameObject greenRing = new GameObject("GreenRing", typeof(RectTransform), typeof(Image));
        greenRing.transform.SetParent(outerRing.transform, false);
        RectTransform grRt = greenRing.GetComponent<RectTransform>();
        grRt.anchorMin = Vector2.zero;
        grRt.anchorMax = Vector2.one;
        grRt.offsetMin = new Vector2(2f, 2f);
        grRt.offsetMax = new Vector2(-2f, -2f);
        Image grImg = greenRing.GetComponent<Image>();
        grImg.sprite = defaultCircleSprite;
        grImg.color = new Color(0.24f, 0.88f, 0.36f, 1f);
        grImg.raycastTarget = false;

        GameObject avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avatarGo.transform.SetParent(greenRing.transform, false);
        RectTransform avRt = avatarGo.GetComponent<RectTransform>();
        avRt.anchorMin = Vector2.zero;
        avRt.anchorMax = Vector2.one;
        avRt.offsetMin = new Vector2(3f, 3f);
        avRt.offsetMax = new Vector2(-3f, -3f);
        Image avImg = avatarGo.GetComponent<Image>();
        avImg.sprite = playerAvatarSprite != null ? playerAvatarSprite : defaultHeadSprite;
        avImg.preserveAspect = true;
        avImg.raycastTarget = false;

        GameObject distPill = new GameObject("DistPill", typeof(RectTransform), typeof(Image));
        distPill.transform.SetParent(playerMarker, false);
        RectTransform dpRt = distPill.GetComponent<RectTransform>();
        dpRt.sizeDelta = new Vector2(92f, 22f);
        dpRt.anchoredPosition = new Vector2(0f, -19f);
        Image dpImg = distPill.GetComponent<Image>();
        dpImg.color = new Color(0.05f, 0.06f, 0.09f, 0.78f);
        dpImg.raycastTarget = false;

        GameObject distTxtGo = new GameObject("DistText", typeof(RectTransform), typeof(TextMeshProUGUI));
        distTxtGo.transform.SetParent(distPill.transform, false);
        RectTransform dtRt = distTxtGo.GetComponent<RectTransform>();
        dtRt.anchorMin = Vector2.zero;
        dtRt.anchorMax = Vector2.one;
        dtRt.offsetMin = Vector2.zero;
        dtRt.offsetMax = Vector2.zero;
        distanceText = distTxtGo.GetComponent<TextMeshProUGUI>();
        distanceText.fontSize = 15f;
        distanceText.fontStyle = FontStyles.Bold;
        distanceText.alignment = TextAlignmentOptions.Center;
        distanceText.color = Color.white;
        distanceText.raycastTarget = false;
        distanceText.text = "0 m";
    }

    void BuildBanner(Transform canvasParent)
    {
        GameObject go = new GameObject("BiomeTransitionBanner", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasParent, false);
        bannerRoot = go.GetComponent<RectTransform>();
        bannerRoot.anchorMin = new Vector2(0.5f, 1f);
        bannerRoot.anchorMax = new Vector2(0.5f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 0.5f);
        bannerRoot.anchoredPosition = bannerPos;
        bannerRoot.sizeDelta = new Vector2(560f, 104f);

        bannerGroup = go.GetComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.interactable = false;
        bannerGroup.blocksRaycasts = false;

        GameObject bgGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(bannerRoot, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.11f, 0.80f);
        bgImg.raycastTarget = false;

        GameObject topLineGo = new GameObject("TopAccent", typeof(RectTransform), typeof(Image));
        topLineGo.transform.SetParent(bannerRoot, false);
        RectTransform tlRt = topLineGo.GetComponent<RectTransform>();
        tlRt.anchorMin = new Vector2(0.08f, 1f);
        tlRt.anchorMax = new Vector2(0.92f, 1f);
        tlRt.pivot = new Vector2(0.5f, 1f);
        tlRt.sizeDelta = new Vector2(0f, 4f);
        bannerTopLine = topLineGo.GetComponent<Image>();
        bannerTopLine.raycastTarget = false;

        GameObject botLineGo = new GameObject("BottomAccent", typeof(RectTransform), typeof(Image));
        botLineGo.transform.SetParent(bannerRoot, false);
        RectTransform blRt = botLineGo.GetComponent<RectTransform>();
        blRt.anchorMin = new Vector2(0.08f, 0f);
        blRt.anchorMax = new Vector2(0.92f, 0f);
        blRt.pivot = new Vector2(0.5f, 0f);
        blRt.sizeDelta = new Vector2(0f, 4f);
        bannerBottomLine = botLineGo.GetComponent<Image>();
        bannerBottomLine.raycastTarget = false;

        GameObject subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(bannerRoot, false);
        RectTransform subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0.62f);
        subRt.anchorMax = new Vector2(1f, 0.94f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;
        bannerSubText = subGo.GetComponent<TextMeshProUGUI>();
        bannerSubText.fontSize = 18f;
        bannerSubText.fontStyle = FontStyles.Bold;
        bannerSubText.characterSpacing = 6f;
        bannerSubText.alignment = TextAlignmentOptions.Center;
        bannerSubText.color = new Color(0.86f, 0.90f, 0.95f, 0.95f);
        bannerSubText.raycastTarget = false;
        bannerSubText.text = bannerSubtitle;

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(bannerRoot, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.06f);
        titleRt.anchorMax = new Vector2(1f, 0.66f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        bannerTitleText = titleGo.GetComponent<TextMeshProUGUI>();
        bannerTitleText.fontSize = 44f;
        bannerTitleText.fontStyle = FontStyles.Bold;
        bannerTitleText.alignment = TextAlignmentOptions.Center;
        bannerTitleText.color = Color.white;
        bannerTitleText.raycastTarget = false;

        bannerRoot.gameObject.SetActive(false);
    }

    public void RebuildCards()
    {
        EnsureBuilt();
        BindManager();

        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            Destroy(cardsContainer.GetChild(i).gameObject);
        cards.Clear();

        if (subscribedManager == null || subscribedManager.Sections.Count == 0)
        {
            totalStripWidth = cardSize.x;
            return;
        }

        IReadOnlyList<BiomeSection> secs = subscribedManager.Sections;
        int count = secs.Count;
        float w = cardSize.x;
        if (count > 10) w = Mathf.Max(56f, 920f / count - cardSpacing);
        float h = cardSize.y;

        totalStripWidth = count * w + Mathf.Max(0, count - 1) * cardSpacing;
        cardsContainer.sizeDelta = new Vector2(totalStripWidth, h);
        barRoot.sizeDelta = new Vector2(Mathf.Max(220f, totalStripWidth + 24f), h + 44f);

        float startX = -totalStripWidth * 0.5f + w * 0.5f;
        for (int i = 0; i < count; i++)
        {
            BiomeSection sec = secs[i];
            GameObject cardGo = new GameObject("BiomeCard_" + sec.BiomeName, typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(cardsContainer, false);
            RectTransform rt = cardGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(startX + i * (w + cardSpacing), 0f);

            // Khung viền ngoài tối
            Image outerImg = cardGo.GetComponent<Image>();
            outerImg.color = new Color(0.06f, 0.07f, 0.09f, 0.96f);
            outerImg.raycastTarget = false;

            // Viền màu đặc trưng của Biome
            GameObject borderGo = new GameObject("ColorBorder", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(rt, false);
            RectTransform brt = borderGo.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(2f, 2f);
            brt.offsetMax = new Vector2(-2f, -2f);
            Image borderImg = borderGo.GetComponent<Image>();
            borderImg.color = sec.BorderColor;
            borderImg.raycastTarget = false;

            // Nền thẻ bên trong
            GameObject innerGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            innerGo.transform.SetParent(brt, false);
            RectTransform irt = innerGo.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3f, 3f);
            irt.offsetMax = new Vector2(-3f, -3f);
            Image fillImg = innerGo.GetComponent<Image>();
            fillImg.color = sec.CardColor;
            fillImg.raycastTarget = false;

            // Ảnh icon Biome (nếu có gắn trong BiomeData)
            Image iconImg = null;
            if (sec.Icon != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(irt, false);
                RectTransform icRt = iconGo.GetComponent<RectTransform>();
                icRt.anchorMin = Vector2.zero;
                icRt.anchorMax = Vector2.one;
                icRt.offsetMin = Vector2.zero;
                icRt.offsetMax = Vector2.zero;
                iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = sec.Icon;
                iconImg.preserveAspect = false;
                iconImg.raycastTarget = false;
            }

            // Dải nền chân trời trang trí nhẹ ở nửa dưới nếu chưa gắn Sprite icon
            GameObject bandGo = new GameObject("HorizonBand", typeof(RectTransform), typeof(Image));
            bandGo.transform.SetParent(irt, false);
            RectTransform bandRt = bandGo.GetComponent<RectTransform>();
            bandRt.anchorMin = new Vector2(0f, 0f);
            bandRt.anchorMax = new Vector2(1f, 0.38f);
            bandRt.offsetMin = Vector2.zero;
            bandRt.offsetMax = Vector2.zero;
            Image bandImg = bandGo.GetComponent<Image>();
            bandImg.color = new Color(sec.CardColor.r * 0.65f, sec.CardColor.g * 0.65f, sec.CardColor.b * 0.65f, sec.Icon != null ? 0f : 0.75f);
            bandImg.raycastTarget = false;

            // Thanh tối nhỏ phía trên để chữ tên Biome luôn nổi bật
            GameObject titleBg = new GameObject("TitleShade", typeof(RectTransform), typeof(Image));
            titleBg.transform.SetParent(irt, false);
            RectTransform tbRt = titleBg.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 0.56f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.offsetMin = Vector2.zero;
            tbRt.offsetMax = Vector2.zero;
            Image tbImg = titleBg.GetComponent<Image>();
            tbImg.color = new Color(0.05f, 0.06f, 0.08f, 0.42f);
            tbImg.raycastTarget = false;

            GameObject lblGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(irt, false);
            RectTransform lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.04f, 0.54f);
            lrt.anchorMax = new Vector2(0.96f, 0.98f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI nameTmp = lblGo.GetComponent<TextMeshProUGUI>();
            nameTmp.text = sec.BiomeName;
            nameTmp.fontSize = 15f;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 9f;
            nameTmp.fontSizeMax = 16f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;

            cards.Add(new CardVisual
            {
                section = sec,
                borderImage = borderImg,
                fillImage = fillImg,
                iconImage = iconImg,
                groundBandImage = bandImg,
                nameText = nameTmp
            });
        }
    }

    void HandleBiomeChanged(int newIndex, BiomeSection section, bool isTransition)
    {
        if (!isTransition || section == null) return;
        ShowTransitionBanner(section);
    }

    public void ShowTransitionBanner(BiomeSection section)
    {
        EnsureBuilt();
        if (section == null || bannerRoot == null) return;

        Color accent = section.BorderColor;
        if (bannerTopLine != null) bannerTopLine.color = accent;
        if (bannerBottomLine != null) bannerBottomLine.color = accent;
        if (bannerSubText != null) bannerSubText.text = bannerSubtitle;
        if (bannerTitleText != null)
        {
            bannerTitleText.text = section.BiomeName.ToUpperInvariant();
            bannerTitleText.color = Color.Lerp(Color.white, accent, 0.35f);
        }

        showingBanner = true;
        bannerElapsed = 0f;
        bannerRoot.anchoredPosition = bannerPos;
        bannerRoot.gameObject.SetActive(true);
    }

    void UpdateBarVisibility(bool visible, bool instant = false)
    {
        if (barRoot == null || barGroup == null) return;
        float target = visible ? 1f : 0f;
        if (instant)
        {
            barGroup.alpha = target;
            barRoot.gameObject.SetActive(visible);
            return;
        }

        if (visible && !barRoot.gameObject.activeSelf)
            barRoot.gameObject.SetActive(true);

        barGroup.alpha = Mathf.MoveTowards(barGroup.alpha, target, 8f * Time.deltaTime);
        if (!visible && barGroup.alpha <= 0.001f && barRoot.gameObject.activeSelf)
            barRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        BindManager();
        if (player == null) player = PlayerController.Instance;

        bool inRun = player != null && player.RunActive;
        UpdateBarVisibility(inRun);

        if (inRun && subscribedManager != null && cards.Count > 0)
        {
            float dist = subscribedManager.CurrentDistance;
            float best = subscribedManager.BestDistance;

            float t = subscribedManager.GetProgressAlongBar01(dist);
            if (playerMarker != null)
                playerMarker.anchoredPosition = new Vector2((t - 0.5f) * totalStripWidth, -cardSize.y + 4f);

            int distInt = Mathf.FloorToInt(dist);
            if (distInt != lastDistDisplay && distanceText != null)
            {
                lastDistDisplay = distInt;
                distanceText.text = distInt.ToString("N0") + " m";
            }

            if (bestMarker != null)
            {
                bool showBest = best >= 1f;
                if (bestMarker.gameObject.activeSelf != showBest)
                    bestMarker.gameObject.SetActive(showBest);

                if (showBest)
                {
                    float bestT = subscribedManager.GetProgressAlongBar01(best);
                    bestMarker.anchoredPosition = new Vector2((bestT - 0.5f) * totalStripWidth, -cardSize.y - 2f);
                    int bestInt = Mathf.FloorToInt(best);
                    if (bestInt != lastBestDisplay && bestText != null)
                    {
                        lastBestDisplay = bestInt;
                        bestText.text = "BEST " + bestInt.ToString("N0") + " m";
                    }
                }
            }

            int activeIdx = subscribedManager.ActiveBiomeIndex;
            for (int i = 0; i < cards.Count; i++)
            {
                bool active = i == activeIdx;
                CardVisual cv = cards[i];
                if (cv.borderImage != null)
                    cv.borderImage.color = active ? Color.Lerp(cv.section.BorderColor, Color.white, 0.35f) : cv.section.BorderColor * 0.72f;
                if (cv.fillImage != null)
                    cv.fillImage.color = active ? cv.section.CardColor : cv.section.CardColor * 0.82f;
            }
        }

        UpdateBanner(inRun);
    }

    void UpdateBanner(bool inRun)
    {
        if (!showingBanner || bannerRoot == null) return;
        if (!inRun)
        {
            showingBanner = false;
            bannerGroup.alpha = 0f;
            bannerRoot.gameObject.SetActive(false);
            return;
        }

        bannerElapsed += Time.deltaTime;
        float fadeIn = Mathf.Max(0.05f, bannerFadeIn);
        float hold = Mathf.Max(0.1f, bannerHold);
        float fadeOut = Mathf.Max(0.05f, bannerFadeOut);
        float total = fadeIn + hold + fadeOut;

        if (bannerElapsed >= total)
        {
            showingBanner = false;
            bannerGroup.alpha = 0f;
            bannerRoot.gameObject.SetActive(false);
            return;
        }

        float alpha;
        float scale = 1f;
        if (bannerElapsed < fadeIn)
        {
            float p = Mathf.Clamp01(bannerElapsed / fadeIn);
            alpha = p;
            // Pop nhẹ từ 0.75 -> 1.06 -> 1.0
            scale = p < 0.7f
                ? Mathf.Lerp(0.75f, 1.06f, p / 0.7f)
                : Mathf.Lerp(1.06f, 1f, (p - 0.7f) / 0.3f);
        }
        else if (bannerElapsed < fadeIn + hold)
        {
            alpha = 1f;
            scale = 1f;
        }
        else
        {
            float p = Mathf.Clamp01((bannerElapsed - fadeIn - hold) / fadeOut);
            alpha = 1f - p;
            scale = Mathf.Lerp(1f, 0.96f, p);
        }

        bannerGroup.alpha = alpha;
        bannerRoot.localScale = Vector3.one * scale;
    }
}
