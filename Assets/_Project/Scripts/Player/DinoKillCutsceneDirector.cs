using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class DinoKillCutsceneDirector : MonoBehaviour
{
    public static DinoKillCutsceneDirector Instance { get; private set; }

    [Header("1. Thời gian các pha Kill & Chờ sau khi Kill (Giây)")]
    [Tooltip("Pha 1: Thời gian người chơi bị hất văng khỏi lưng Dino.")]
    [Min(0.15f)] public float throwPhaseDuration = 0.50f;
    [Tooltip("Pha 2: Thời gian Dino lao tới đớp / hút vào miệng / nhấc chân giẫm.")]
    [Min(0.12f)] public float strikePhaseDuration = 0.28f;
    [Tooltip("Pha 3: Thời gian Dino thực hiện động tác giết (cạp xé / nuốt ực / giẫm đè).")]
    [Min(0.20f)] public float killActionDuration = 0.55f;
    [Tooltip("Pha 4: Thời gian chờ SAU KHI KILL XONG (mở viền đen, hiện bảng chữ DEVOURED!/DEAD!, nhìn xác bất động hoặc đi theo Dino vừa nuốt trước khi hồi sinh).")]
    [Min(0.30f)] public float postKillWaitDuration = 1.65f;

    [Header("2. Chữ & Ảnh Chữ hiển thị sau khi bị Kill")]
    [Tooltip("Tích vào đây khi đang ở ngoài Edit Mode để bật xem thử bảng chữ trực tiếp trên màn hình Game/Scene!")]
    public bool previewBannerInEditMode = false;
    [Tooltip("Kiểu chết dùng để xem thử chữ khi bật Preview Banner In Edit Mode.")]
    public DinoKillCutsceneStyle previewStyle = DinoKillCutsceneStyle.TossAndSwallow;
    [Tooltip("Bật: Mọi kiểu chết đều dùng chung chữ/ảnh ở ô Common Title bên dưới (ví dụ chữ DEAD!). Tắt: Tự đổi chữ riêng theo từng kiểu chết.")]
    public bool useSingleTitleForAllStyles = false;
    public string commonTitleText = "DEAD!";
    [Tooltip("Nếu kéo ảnh chữ (Sprite PNG) vào đây, hệ thống sẽ hiện ảnh chữ này thay cho chữ TextMeshPro.")]
    public Sprite commonTitleSprite;

    [Tooltip("Chữ & Ảnh chữ riêng khi bị Hất lên & Nuốt chửng (TossAndSwallow).")]
    public string swallowTitleText = "DEVOURED!";
    public Sprite swallowTitleSprite;

    [Tooltip("Chữ & Ảnh chữ riêng khi bị Vồ & Cạp (PounceAndBite).")]
    public string biteTitleText = "MAULED!";
    public Sprite biteTitleSprite;

    [Tooltip("Chữ & Ảnh chữ riêng khi bị Giẫm bẹp (Stomp).")]
    public string stompTitleText = "CRUSHED!";
    public Sprite stompTitleSprite;

    [Tooltip("Dòng chữ phụ bên dưới (để trống nếu không muốn hiện khung chữ phụ).")]
    public string subtitleText = "Jump off before your stamina runs out!";
    [Tooltip("Kéo ảnh vào đây nếu muốn dùng ảnh riêng thay cho khung chữ phụ bên dưới.")]
    public Sprite customSubtitleSprite;

    [Header("3. Kiểu dáng & Hiệu ứng Bảng Chữ (Banner Style)")]
    [Tooltip("Góc nghiêng của bảng chữ (bản mẫu nghiêng khoảng 4 độ).")]
    [Range(-20f, 20f)] public float bannerTiltAngle = 4f;
    public Vector2 bannerAnchoredOffset = new Vector2(0f, 25f);
    [Min(24f)] public float titleFontSize = 90f;
    public Color titleTextColor = new Color(1f, 0.36f, 0.30f, 1f);
    public Color titleOutlineColor = new Color(0.05f, 0.02f, 0.02f, 1f);
    [Range(0f, 0.6f)] public float titleOutlineWidth = 0.34f;
    [Tooltip("Kích thước hiển thị khi dùng Ảnh chữ (Sprite) thay cho Text.")]
    public Vector2 customTitleSpriteSize = new Vector2(680f, 155f);
    public Color bannerBandColor = new Color(0.04f, 0.05f, 0.07f, 0.82f);
    public Color bannerBorderColor = new Color(0.95f, 0.22f, 0.20f, 0.92f);
    [Min(14f)] public float subtitleFontSize = 33f;
    public Color subtitleTextColor = Color.white;
    public Color subtitleBgColor = new Color(0.08f, 0.10f, 0.13f, 0.92f);
    public Color subtitleBorderColor = new Color(0.88f, 0.28f, 0.28f, 0.85f);
    [Min(0.08f)] public float bannerPopDuration = 0.28f;
    [Range(1f, 1.4f)] public float bannerPopOvershoot = 1.15f;

    [Header("4. Ẩn UI & Camera sau khi bị Kill")]
    [Tooltip("Ẩn toàn bộ nút UI gameplay (Hotbar, Jump, Stamina, Cash...) trong suốt lúc bị kill và hiện lại khi hồi sinh.")]
    public bool hideGameplayUI = true;
    [Tooltip("Thu hẹp 2 viền đen trên/dưới (Letterbox) trong lúc Dino đang thực hiện động tác kill (tự mở rộng ra ở pha hiện chữ sau khi kill).")]
    public bool showCinemaLetterbox = true;
    [Min(20f)] public float letterboxBarHeight = 74f;
    public bool showRedImpactFlash = true;
    [Tooltip("Khoảng cách & độ cao camera đi theo sau lưng con Dino vừa nuốt người chơi.")]
    [Min(1.5f)] public float followDinoDistanceAfterSwallow = 4.6f;
    [Min(0.5f)] public float followDinoHeightAfterSwallow = 2.35f;
    [Tooltip("Khoảng cách & độ cao camera nhìn xuống xác người chơi đang nằm bất động sau khi bị cạp/giẫm.")]
    [Min(1.2f)] public float corpseViewDistance = 3.1f;
    [Min(0.5f)] public float corpseViewHeight = 1.55f;

    [Header("5. Test nhanh, Slow-Motion & Rung Camera")]
    [Tooltip("Ghi đè kiểu chết cho mọi Dino để test nhanh (để AutoBySpecies = lấy theo cài đặt riêng của từng loài DinoData).")]
    public DinoKillCutsceneStyle overrideKillStyle = DinoKillCutsceneStyle.AutoBySpecies;
    [Tooltip("Ghi đè góc quay camera cho mọi Dino (để RandomMultiAngle = lấy theo DinoData hoặc tự chuyển nhiều góc quay).")]
    public DinoKillCameraStyle overrideCameraStyle = DinoKillCameraStyle.RandomMultiAngle;
    [Range(0.15f, 1f)] public float apexSlowMoScale = 0.36f;
    [Min(0f)] public float impactShakeIntensity = 0.52f;
    [Min(0f)] public float impactShakeDuration = 0.30f;

    CinemachineVirtualCamera cutsceneVcam;
    Transform camTransform;
    Canvas overlayCanvas;
    RectTransform topBar, bottomBar;
    Image flashImage;

    // Thành phần Banner kết thúc (DEVOURED! / DEAD!)
    RectTransform bannerRoot;
    RectTransform mainBandRect;
    Image mainBandBg, topBorderLine, bottomBorderLine;
    TextMeshProUGUI titleTmp;
    Image customTitleImg;
    RectTransform subtitlePillRect;
    Image subtitlePillBg, subtitlePillOutline, customSubtitleImg;
    TextMeshProUGUI subtitleTmp;

    CanvasGroup hiddenHudGroup;
    Coroutine activeRoutine;
    float camShakeTimer, camShakeDur, camShakeAmp;
    float baseFov = 58f;

    static Sprite fadeBandSprite;
    static Sprite fadeLineSprite;

    public bool IsPlaying { get; private set; }

    public static DinoKillCutsceneDirector GetOrCreate(PlayerController player)
    {
        if (Instance != null) return Instance;
        DinoKillCutsceneDirector existing = FindObjectOfType<DinoKillCutsceneDirector>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject host = player != null ? player.gameObject : new GameObject("DinoKillCutsceneDirector");
        Instance = host.AddComponent<DinoKillCutsceneDirector>();
        return Instance;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (Application.isPlaying)
        {
            previewBannerInEditMode = false;
            EnsureCutsceneCamera();
            EnsureOverlayUI();
            SetBannerVisible(false, DinoKillCutsceneStyle.TossAndSwallow, 1f);
            SetOverlayImmediate(0f, 0f);
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (previewBannerInEditMode)
            {
                EnsureOverlayUI();
                ApplyBannerStyleAndContent(previewStyle);
                SetBannerVisible(true, previewStyle, 1f);
                SetOverlayImmediate(0f, 0f);
            }
            else if (bannerRoot != null && bannerRoot.gameObject.activeSelf)
            {
                bannerRoot.gameObject.SetActive(false);
            }
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (previewBannerInEditMode)
            {
                EnsureOverlayUI();
                ApplyBannerStyleAndContent(previewStyle);
                SetBannerVisible(true, previewStyle, 1f);
            }
            else if (bannerRoot != null)
            {
                bannerRoot.gameObject.SetActive(false);
            }
        }
        else if (bannerRoot != null)
        {
            ApplyBannerStyleAndContent(previewStyle);
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            Time.timeScale = 1f;
            if (cutsceneVcam != null) cutsceneVcam.Priority = 0;
            SetOverlayImmediate(0f, 0f);
            SetBannerVisible(false, previewStyle, 1f);
            SetGameplayHudVisible(true);
            IsPlaying = false;
        }
    }

    void SetGameplayHudVisible(bool visible)
    {
        if (!hideGameplayUI && !visible) return;

        if (hiddenHudGroup == null)
        {
            HudController hud = HudController.Instance != null ? HudController.Instance : FindObjectOfType<HudController>();
            if (hud != null)
            {
                Canvas rootCanvas = hud.GetComponentInParent<Canvas>();
                GameObject targetGo = rootCanvas != null ? rootCanvas.gameObject : hud.gameObject;
                hiddenHudGroup = targetGo.GetComponent<CanvasGroup>();
                if (hiddenHudGroup == null)
                    hiddenHudGroup = targetGo.AddComponent<CanvasGroup>();
            }
        }

        if (hiddenHudGroup != null)
        {
            hiddenHudGroup.alpha = visible ? 1f : 0f;
            hiddenHudGroup.interactable = visible;
            hiddenHudGroup.blocksRaycasts = visible;
        }
    }

    void EnsureCutsceneCamera()
    {
        if (cutsceneVcam != null) return;
        GameObject existing = GameObject.Find("VCam_DinoKillCutscene");
        GameObject go = existing != null ? existing : new GameObject("VCam_DinoKillCutscene");
        camTransform = go.transform;
        cutsceneVcam = go.GetComponent<CinemachineVirtualCamera>();
        if (cutsceneVcam == null) cutsceneVcam = go.AddComponent<CinemachineVirtualCamera>();
        cutsceneVcam.Priority = 0;
        if (CameraRig.Instance != null && CameraRig.Instance.vcam != null)
        {
            cutsceneVcam.m_Lens = CameraRig.Instance.vcam.m_Lens;
            baseFov = cutsceneVcam.m_Lens.FieldOfView > 10f ? cutsceneVcam.m_Lens.FieldOfView : 58f;
        }
    }

    static Sprite GetFadeBandSprite()
    {
        if (fadeBandSprite != null) return fadeBandSprite;
        int w = 256, h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)(w - 1);
            // Mờ dần 2 đầu trái/phải giống dải băng DEVOURED! bản mẫu
            float edge = Mathf.Min(u / 0.18f, (1f - u) / 0.18f);
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge));
            byte a = (byte)Mathf.RoundToInt(alpha * 255f);
            for (int y = 0; y < h; y++)
                px[y * w + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        fadeBandSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return fadeBandSprite;
    }

    static Sprite GetFadeLineSprite()
    {
        if (fadeLineSprite != null) return fadeLineSprite;
        fadeLineSprite = GetFadeBandSprite();
        return fadeLineSprite;
    }

    void EnsureOverlayUI()
    {
        if (overlayCanvas == null)
        {
            GameObject existing = GameObject.Find("DinoKillCutsceneOverlay");
            if (existing != null) overlayCanvas = existing.GetComponent<Canvas>();
        }

        if (overlayCanvas == null)
        {
            GameObject canvasGo = new GameObject("DinoKillCutsceneOverlay");
            overlayCanvas = canvasGo.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 950;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform root = overlayCanvas.transform;

        // 1. Flash đỏ khi bị cắn / nuốt / giẫm
        Transform flashTr = root.Find("ImpactFlash");
        if (flashTr == null)
        {
            GameObject flashGo = new GameObject("ImpactFlash");
            flashGo.transform.SetParent(root, false);
            flashImage = flashGo.AddComponent<Image>();
            flashImage.raycastTarget = false;
            flashImage.color = new Color(0.85f, 0.05f, 0.05f, 0f);
            RectTransform flashRt = flashImage.rectTransform;
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
        }
        else
        {
            flashImage = flashTr.GetComponent<Image>();
        }

        // 2. 2 thanh đen Letterbox trên & dưới
        topBar = GetOrCreateBar(root, "TopLetterbox", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        bottomBar = GetOrCreateBar(root, "BottomLetterbox", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

        // 3. Banner chữ kết thúc (DEVOURED! / DEAD! + Subtitle Pill giống ảnh mẫu)
        Transform bannerTr = root.Find("KillBannerRoot");
        if (bannerTr == null)
        {
            GameObject bGo = new GameObject("KillBannerRoot", typeof(RectTransform));
            bGo.transform.SetParent(root, false);
            bannerRoot = bGo.GetComponent<RectTransform>();
            bannerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRoot.pivot = new Vector2(0.5f, 0.5f);
            bannerRoot.sizeDelta = new Vector2(1320f, 290f);
        }
        else
        {
            bannerRoot = bannerTr.GetComponent<RectTransform>();
        }

        // Dải băng đen ngang chính
        Transform bandTr = bannerRoot.Find("TitleBannerBand");
        if (bandTr == null)
        {
            GameObject bandGo = new GameObject("TitleBannerBand", typeof(RectTransform), typeof(Image));
            bandGo.transform.SetParent(bannerRoot, false);
            mainBandRect = bandGo.GetComponent<RectTransform>();
            mainBandRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainBandRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainBandRect.pivot = new Vector2(0.5f, 0.5f);
            mainBandRect.anchoredPosition = new Vector2(0f, 38f);
            mainBandRect.sizeDelta = new Vector2(1280f, 146f);
            mainBandBg = bandGo.GetComponent<Image>();
            mainBandBg.raycastTarget = false;
        }
        else
        {
            mainBandRect = bandTr.GetComponent<RectTransform>();
            mainBandBg = bandTr.GetComponent<Image>();
        }
        if (mainBandBg != null) mainBandBg.sprite = GetFadeBandSprite();

        // Viền đỏ trên & dưới của dải băng
        topBorderLine = GetOrCreateBandLine(mainBandRect, "TopRedLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        bottomBorderLine = GetOrCreateBandLine(mainBandRect, "BottomRedLine", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

        // Chữ tiêu đề lớn (DEVOURED! / MAULED! / CRUSHED! / DEAD!)
        Transform titleTr = mainBandRect.Find("TitleText");
        if (titleTr == null)
        {
            GameObject tGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            tGo.transform.SetParent(mainBandRect, false);
            RectTransform tRt = tGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(20f, -10f);
            tRt.offsetMax = new Vector2(-20f, 10f);
            titleTmp = tGo.GetComponent<TextMeshProUGUI>();
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.enableWordWrapping = false;
            titleTmp.raycastTarget = false;
        }
        else
        {
            titleTmp = titleTr.GetComponent<TextMeshProUGUI>();
        }

        // Ảnh chữ tiêu đề tùy chọn (nếu người dùng kéo Sprite chữ tự thiết kế vào)
        Transform customImgTr = mainBandRect.Find("CustomTitleImage");
        if (customImgTr == null)
        {
            GameObject cGo = new GameObject("CustomTitleImage", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(mainBandRect, false);
            RectTransform cRt = cGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            customTitleImg = cGo.GetComponent<Image>();
            customTitleImg.preserveAspect = true;
            customTitleImg.raycastTarget = false;
        }
        else
        {
            customTitleImg = customImgTr.GetComponent<Image>();
        }

        // Khung thuốc tròn (Subtitle Pill: "Jump off before your stamina runs out!")
        Transform subPillTr = bannerRoot.Find("SubtitlePill");
        if (subPillTr == null)
        {
            GameObject pGo = new GameObject("SubtitlePill", typeof(RectTransform), typeof(Image));
            pGo.transform.SetParent(bannerRoot, false);
            subtitlePillRect = pGo.GetComponent<RectTransform>();
            subtitlePillRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitlePillRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitlePillRect.pivot = new Vector2(0.5f, 0.5f);
            subtitlePillRect.anchoredPosition = new Vector2(0f, -84f);
            subtitlePillRect.sizeDelta = new Vector2(760f, 64f);
            subtitlePillBg = pGo.GetComponent<Image>();
            subtitlePillBg.raycastTarget = false;
        }
        else
        {
            subtitlePillRect = subPillTr.GetComponent<RectTransform>();
            subtitlePillBg = subPillTr.GetComponent<Image>();
        }
        if (subtitlePillBg != null)
        {
            subtitlePillBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 28);
            subtitlePillBg.type = Image.Type.Sliced;
        }

        Transform subOutlineTr = subtitlePillRect.Find("Outline");
        if (subOutlineTr == null)
        {
            GameObject oGo = new GameObject("Outline", typeof(RectTransform), typeof(Image));
            oGo.transform.SetParent(subtitlePillRect, false);
            RectTransform oRt = oGo.GetComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = Vector2.zero;
            oRt.offsetMax = Vector2.zero;
            subtitlePillOutline = oGo.GetComponent<Image>();
            subtitlePillOutline.raycastTarget = false;
        }
        else
        {
            subtitlePillOutline = subOutlineTr.GetComponent<Image>();
        }
        if (subtitlePillOutline != null)
        {
            subtitlePillOutline.sprite = UIEffectUtil.GetRoundedOutlineSprite(64, 28, 3);
            subtitlePillOutline.type = Image.Type.Sliced;
        }

        Transform subTextTr = subtitlePillRect.Find("SubtitleText");
        if (subTextTr == null)
        {
            GameObject stGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            stGo.transform.SetParent(subtitlePillRect, false);
            RectTransform stRt = stGo.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = new Vector2(24f, 4f);
            stRt.offsetMax = new Vector2(-24f, -4f);
            subtitleTmp = stGo.GetComponent<TextMeshProUGUI>();
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.fontStyle = FontStyles.Bold;
            subtitleTmp.enableWordWrapping = false;
            subtitleTmp.raycastTarget = false;
        }
        else
        {
            subtitleTmp = subTextTr.GetComponent<TextMeshProUGUI>();
        }

        Transform subImgTr = subtitlePillRect.Find("CustomSubtitleImage");
        if (subImgTr == null)
        {
            GameObject siGo = new GameObject("CustomSubtitleImage", typeof(RectTransform), typeof(Image));
            siGo.transform.SetParent(subtitlePillRect, false);
            RectTransform siRt = siGo.GetComponent<RectTransform>();
            siRt.anchorMin = Vector2.zero;
            siRt.anchorMax = Vector2.one;
            siRt.offsetMin = Vector2.zero;
            siRt.offsetMax = Vector2.zero;
            customSubtitleImg = siGo.GetComponent<Image>();
            customSubtitleImg.preserveAspect = true;
            customSubtitleImg.raycastTarget = false;
        }
        else
        {
            customSubtitleImg = subImgTr.GetComponent<Image>();
        }

        ApplyBannerStyleAndContent(previewStyle);
    }

    Image GetOrCreateBandLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform tr = parent.Find(name);
        Image img;
        RectTransform rt;
        if (tr == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
            img.raycastTarget = false;
        }
        else
        {
            rt = tr.GetComponent<RectTransform>();
            img = tr.GetComponent<Image>();
        }
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 4.5f);
        img.sprite = GetFadeLineSprite();
        return img;
    }

    RectTransform GetOrCreateBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform tr = parent.Find(name);
        RectTransform rt;
        Image img;
        if (tr == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
        }
        else
        {
            rt = tr.GetComponent<RectTransform>();
            img = tr.GetComponent<Image>();
        }
        img.color = new Color(0.03f, 0.03f, 0.04f, 0.96f);
        img.raycastTarget = false;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(0f, 0f);
        return rt;
    }

    void ResolveTitleForStyle(DinoKillCutsceneStyle style, out string text, out Sprite sprite)
    {
        if (useSingleTitleForAllStyles)
        {
            text = commonTitleText;
            sprite = commonTitleSprite;
            return;
        }

        switch (style)
        {
            case DinoKillCutsceneStyle.TossAndSwallow:
                text = !string.IsNullOrEmpty(swallowTitleText) ? swallowTitleText : commonTitleText;
                sprite = swallowTitleSprite != null ? swallowTitleSprite : commonTitleSprite;
                break;
            case DinoKillCutsceneStyle.Stomp:
                text = !string.IsNullOrEmpty(stompTitleText) ? stompTitleText : commonTitleText;
                sprite = stompTitleSprite != null ? stompTitleSprite : commonTitleSprite;
                break;
            default:
                text = !string.IsNullOrEmpty(biteTitleText) ? biteTitleText : commonTitleText;
                sprite = biteTitleSprite != null ? biteTitleSprite : commonTitleSprite;
                break;
        }
    }

    void ApplyBannerStyleAndContent(DinoKillCutsceneStyle style)
    {
        if (bannerRoot == null) return;

        bannerRoot.anchoredPosition = bannerAnchoredOffset;
        bannerRoot.localRotation = Quaternion.Euler(0f, 0f, bannerTiltAngle);

        if (mainBandBg != null) mainBandBg.color = bannerBandColor;
        if (topBorderLine != null) topBorderLine.color = bannerBorderColor;
        if (bottomBorderLine != null) bottomBorderLine.color = bannerBorderColor;

        ResolveTitleForStyle(style, out string resolvedTitle, out Sprite resolvedSprite);
        bool useSprite = resolvedSprite != null;

        if (customTitleImg != null)
        {
            customTitleImg.gameObject.SetActive(useSprite);
            if (useSprite)
            {
                customTitleImg.sprite = resolvedSprite;
                customTitleImg.rectTransform.sizeDelta = customTitleSpriteSize;
            }
        }

        if (titleTmp != null)
        {
            titleTmp.gameObject.SetActive(!useSprite);
            titleTmp.text = resolvedTitle ?? "";
            titleTmp.fontSize = titleFontSize;
            titleTmp.color = titleTextColor;
            titleTmp.outlineColor = titleOutlineColor;
            titleTmp.outlineWidth = titleOutlineWidth;
        }

        bool hasSubSprite = customSubtitleSprite != null;
        bool hasSubText = !string.IsNullOrEmpty(subtitleText);
        if (subtitlePillRect != null)
            subtitlePillRect.gameObject.SetActive(hasSubSprite || hasSubText);

        if (subtitlePillBg != null)
        {
            subtitlePillBg.enabled = !hasSubSprite;
            subtitlePillBg.color = subtitleBgColor;
        }
        if (subtitlePillOutline != null)
        {
            subtitlePillOutline.enabled = !hasSubSprite;
            subtitlePillOutline.color = subtitleBorderColor;
        }
        if (customSubtitleImg != null)
        {
            customSubtitleImg.gameObject.SetActive(hasSubSprite);
            if (hasSubSprite) customSubtitleImg.sprite = customSubtitleSprite;
        }
        if (subtitleTmp != null)
        {
            subtitleTmp.gameObject.SetActive(!hasSubSprite && hasSubText);
            subtitleTmp.text = subtitleText ?? "";
            subtitleTmp.fontSize = subtitleFontSize;
            subtitleTmp.color = subtitleTextColor;
            subtitleTmp.outlineColor = new Color(0f, 0f, 0f, 0.92f);
            subtitleTmp.outlineWidth = 0.22f;
        }
    }

    void SetBannerVisible(bool visible, DinoKillCutsceneStyle style, float scale)
    {
        if (bannerRoot == null) return;
        if (visible)
        {
            ApplyBannerStyleAndContent(style);
            if (!bannerRoot.gameObject.activeSelf)
                bannerRoot.gameObject.SetActive(true);
            bannerRoot.localScale = Vector3.one * scale;
        }
        else if (bannerRoot.gameObject.activeSelf)
        {
            bannerRoot.gameObject.SetActive(false);
        }
    }

    void SetOverlayImmediate(float barHeight, float flashAlpha)
    {
        if (topBar != null) topBar.sizeDelta = new Vector2(0f, showCinemaLetterbox ? barHeight : 0f);
        if (bottomBar != null) bottomBar.sizeDelta = new Vector2(0f, showCinemaLetterbox ? barHeight : 0f);
        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = showRedImpactFlash ? Mathf.Clamp01(flashAlpha) : 0f;
            flashImage.color = c;
        }
    }

    public void Play(PlayerController player, Dino killer)
    {
        if (!Application.isPlaying || player == null || killer == null) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(RunKillCutsceneRoutine(player, killer));
    }

    DinoKillCutsceneStyle ChooseKillStyle(Dino killer)
    {
        if (overrideKillStyle != DinoKillCutsceneStyle.AutoBySpecies)
            return overrideKillStyle;
        if (killer != null && killer.Data != null)
            return killer.Data.ResolveKillStyle();
        return DinoKillCutsceneStyle.PounceAndBite;
    }

    DinoKillCameraStyle ChooseCameraStyle(Dino killer, int phaseIndex)
    {
        DinoKillCameraStyle mode = overrideCameraStyle;
        if (mode == DinoKillCameraStyle.RandomMultiAngle && killer != null && killer.Data != null)
            mode = killer.Data.killCameraStyle;

        if (mode != DinoKillCameraStyle.RandomMultiAngle)
            return mode;

        if (phaseIndex == 0)
            return Random.value < 0.55f ? DinoKillCameraStyle.SideOrbitAction : DinoKillCameraStyle.LowAnglePredator;
        return Random.value < 0.5f ? DinoKillCameraStyle.CloseUpVictimPOV : DinoKillCameraStyle.LowAnglePredator;
    }

    void TriggerCutsceneShake(float intensity, float duration)
    {
        camShakeAmp = Mathf.Max(camShakeAmp, intensity);
        camShakeDur = Mathf.Max(0.05f, duration);
        camShakeTimer = camShakeDur;
        if (CameraRig.Instance != null)
            CameraRig.Instance.Shake(intensity, duration);
    }

    void PositionCutsceneCamera(
        DinoKillCameraStyle camStyle,
        DinoKillCutsceneStyle killStyle,
        PlayerController player,
        Dino killer,
        Vector3 playerFocus,
        Vector3 dinoFocus,
        float scale,
        float orbitPhase,
        float fov,
        float dutchDeg,
        bool instantCut)
    {
        if (cutsceneVcam == null || camTransform == null) return;

        Vector3 dinoPos = killer != null ? killer.transform.position : playerFocus;
        Vector3 dinoFwd = killer != null ? Vector3.ProjectOnPlane(killer.transform.forward, Vector3.up).normalized : Vector3.forward;
        if (dinoFwd.sqrMagnitude < 0.001f) dinoFwd = Vector3.forward;
        Vector3 dinoRight = Vector3.Cross(Vector3.up, dinoFwd).normalized;

        float s = Mathf.Clamp(scale, 0.75f, 2.6f);
        Vector3 desiredPos;
        Vector3 lookTarget;

        switch (camStyle)
        {
            case DinoKillCameraStyle.LowAnglePredator:
            {
                float sideSign = (orbitPhase >= 0f) ? 1f : -1f;
                desiredPos = playerFocus
                    + dinoRight * (sideSign * 2.5f * s)
                    + dinoFwd * (1.1f * s)
                    + Vector3.up * (0.45f * s);
                lookTarget = Vector3.Lerp(playerFocus + Vector3.up * 0.45f, dinoFocus + Vector3.up * (0.45f * s), 0.58f);
                break;
            }
            case DinoKillCameraStyle.CloseUpVictimPOV:
            {
                if (killStyle == DinoKillCutsceneStyle.TossAndSwallow)
                {
                    desiredPos = playerFocus + dinoFwd * (1.85f * s) + dinoRight * (0.95f * s) + Vector3.up * (0.45f * s);
                    lookTarget = Vector3.Lerp(playerFocus, dinoFocus, 0.65f);
                }
                else
                {
                    desiredPos = playerFocus + dinoFwd * (1.55f * s) + dinoRight * (0.75f * s) + Vector3.up * (0.55f * s);
                    lookTarget = Vector3.Lerp(dinoFocus, playerFocus + Vector3.up * 0.35f, 0.35f);
                }
                break;
            }
            default:
            {
                float angleRad = orbitPhase * Mathf.Deg2Rad;
                Vector3 center = Vector3.Lerp(dinoPos + Vector3.up * (1.0f * s), playerFocus + Vector3.up * 0.6f, 0.55f);
                float radius = 3.8f * s;
                Vector3 offset = (dinoRight * Mathf.Cos(angleRad) + dinoFwd * Mathf.Sin(angleRad)) * radius
                               + Vector3.up * (1.45f * s);
                desiredPos = center + offset;
                lookTarget = center;
                break;
            }
        }

        ApplyCutsceneCameraTransform(player, desiredPos, lookTarget, fov, dutchDeg, instantCut);
    }

    void ApplyCutsceneCameraTransform(PlayerController player, Vector3 desiredPos, Vector3 lookTarget, float fov, float dutchDeg, bool instantCut)
    {
        if (cutsceneVcam == null || camTransform == null) return;

        float groundY = player != null ? player.GetGroundSurfaceY(desiredPos) : desiredPos.y;
        if (desiredPos.y < groundY + 0.38f)
            desiredPos.y = groundY + 0.38f;

        if (camShakeTimer > 0f)
        {
            camShakeTimer = Mathf.Max(0f, camShakeTimer - Time.unscaledDeltaTime);
            float k = camShakeDur > 0.001f ? (camShakeTimer / camShakeDur) : 0f;
            desiredPos += Random.insideUnitSphere * (camShakeAmp * k * 0.45f);
        }

        if (instantCut)
        {
            camTransform.position = desiredPos;
            Vector3 dir = lookTarget - desiredPos;
            if (dir.sqrMagnitude > 0.0001f)
                camTransform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            cutsceneVcam.PreviousStateIsValid = false;
        }
        else
        {
            float lerpK = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f);
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPos, lerpK);
            Vector3 dir = lookTarget - camTransform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                camTransform.rotation = Quaternion.Slerp(camTransform.rotation, targetRot, lerpK);
            }
        }

        var lens = cutsceneVcam.m_Lens;
        lens.FieldOfView = fov;
        lens.Dutch = dutchDeg;
        cutsceneVcam.m_Lens = lens;
    }

    float EvaluateBannerSpringScale(float elapsed)
    {
        float dur = Mathf.Max(0.08f, bannerPopDuration);
        float t = Mathf.Clamp01(elapsed / dur);
        if (t < 0.58f)
        {
            float p = t / 0.58f;
            float ease = 1f - (1f - p) * (1f - p);
            return Mathf.Lerp(0.18f, bannerPopOvershoot, ease);
        }
        if (t < 0.82f)
        {
            float p = (t - 0.58f) / 0.24f;
            return Mathf.Lerp(bannerPopOvershoot, 0.96f, p);
        }
        return Mathf.Lerp(0.96f, 1f, (t - 0.82f) / 0.18f);
    }

    IEnumerator RunKillCutsceneRoutine(PlayerController player, Dino killer)
    {
        IsPlaying = true;
        EnsureCutsceneCamera();
        EnsureOverlayUI();

        DinoKillCutsceneStyle style = ChooseKillStyle(killer);
        DinoKillCameraStyle camPhase1 = ChooseCameraStyle(killer, 0);
        DinoKillCameraStyle camPhase2 = ChooseCameraStyle(killer, 1);

        // 1. Ẩn toàn bộ UI nút bấm trên màn hình lúc bắt đầu bị giết
        SetGameplayHudVisible(false);
        SetBannerVisible(false, style, 0.2f);

        Vector3 startSeatPos = (killer.seat != null) ? killer.seat.position : player.transform.position;
        player.BeginKillCutsceneState();
        killer.BeginKillCutscene();

        float scale = Mathf.Clamp(killer.VisualScale, 0.7f, 2.5f);
        Vector3 dinoStartPos = killer.transform.position;
        Vector3 dinoFwd = Vector3.ProjectOnPlane(killer.transform.forward, Vector3.up).normalized;
        if (dinoFwd.sqrMagnitude < 0.001f) dinoFwd = Vector3.forward;
        Vector3 dinoRight = Vector3.Cross(Vector3.up, dinoFwd).normalized;

        Transform dinoVisual = killer.VisualRoot;
        Vector3 dinoBaseLocalPos = killer.VisualBaseLocalPos;
        Quaternion dinoBaseLocalRot = killer.VisualBaseLocalRot;

        Transform playerVisual = player.animator != null ? player.animator.transform : player.transform;
        Vector3 playerBaseScale = player.VisualBaseLocalScale;

        cutsceneVcam.Priority = 120;
        float sideSign = Random.value < 0.5f ? 1f : -1f;
        float barTarget = letterboxBarHeight;
        float curBar = 0f;
        float flashAlpha = 0f;
        Vector3 corpseWorldPos = player.transform.position;

        if (style == DinoKillCutsceneStyle.TossAndSwallow)
        {
            // ================= KIỂU 2: HẤT LÊN KHÔNG & ĐỚP NUỐT CHỬNG VÀO BỤNG =================
            player.PlayKilledByDinoAirTumbleAnim();

            float tossDur = Mathf.Max(0.25f, throwPhaseDuration);
            float t = 0f;
            bool cutToPhase2Cam = false;

            while (t < tossDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / tossDur);

                Time.timeScale = (p > 0.42f && p < 0.92f) ? apexSlowMoScale : 1f;
                curBar = Mathf.MoveTowards(curBar, barTarget, udt * 420f);
                SetOverlayImmediate(curBar, 0f);

                if (dinoVisual != null)
                {
                    float buckArc = Mathf.Sin(Mathf.Clamp01(p * 2.2f) * Mathf.PI);
                    float waitMouthPitch = p > 0.45f ? Mathf.Lerp(0f, -22f, (p - 0.45f) / 0.55f) : 22f * buckArc;
                    dinoVisual.localPosition = dinoBaseLocalPos + Vector3.up * (buckArc * 0.45f * scale);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(waitMouthPitch, 0f, 0f);
                }

                Vector3 mouthPos = killer.GetMouthWorldPosition();
                Vector3 apexTarget = mouthPos + dinoFwd * (1.55f * scale) + Vector3.up * (1.35f * scale);
                float ease = 1f - (1f - p) * (1f - p);
                float parabola = 4f * p * (1f - p) * (1.45f * scale);

                player.transform.position = Vector3.Lerp(startSeatPos, apexTarget, ease) + Vector3.up * parabola;
                if (playerVisual != null)
                {
                    playerVisual.localPosition = player.VisualBaseLocalPos;
                    playerVisual.rotation = Quaternion.LookRotation(-dinoFwd) * Quaternion.Euler(p * 320f, p * 180f, 0f);
                }

                bool instant = (t <= udt * 1.1f);
                DinoKillCameraStyle activeCam = camPhase1;
                if (p >= 0.48f)
                {
                    activeCam = camPhase2;
                    if (!cutToPhase2Cam) { cutToPhase2Cam = true; instant = true; }
                }
                float orbit = sideSign * Mathf.Lerp(28f, 68f, p);
                float fov = Mathf.Lerp(baseFov, baseFov - 8f, p);
                PositionCutsceneCamera(activeCam, style, player, killer, player.transform.position, mouthPos, scale, orbit, fov, sideSign * 4f, instant);
                yield return null;
            }

            // Pha 2: Dino lao đầu tới đớp & hút người chơi chui tọt vào miệng
            Time.timeScale = 1f;
            Vector3 swallowStartPos = player.transform.position;
            Quaternion swallowStartRot = playerVisual != null ? playerVisual.rotation : player.transform.rotation;
            float swallowDur = Mathf.Max(0.15f, strikePhaseDuration);
            t = 0f;

            while (t < swallowDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / swallowDur);
                float easeIn = p * p;

                if (dinoVisual != null)
                {
                    float snapPitch = Mathf.Lerp(-22f, 16f, easeIn);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(snapPitch, 0f, 0f);
                    dinoVisual.localPosition = dinoBaseLocalPos + new Vector3(0f, 0.25f * scale * Mathf.Sin(p * Mathf.PI), 0.35f * scale * Mathf.Sin(p * Mathf.PI));
                }

                Vector3 mouthPos = killer.GetMouthWorldPosition();
                player.transform.position = Vector3.Lerp(swallowStartPos, mouthPos, easeIn);
                if (playerVisual != null)
                {
                    playerVisual.localScale = Vector3.Lerp(playerBaseScale, playerBaseScale * 0.06f, easeIn);
                    playerVisual.rotation = swallowStartRot * Quaternion.Euler(p * 140f, p * 220f, 0f);
                }

                float fov = Mathf.Lerp(baseFov - 8f, baseFov - 16f, easeIn);
                PositionCutsceneCamera(camPhase2, style, player, killer, mouthPos, mouthPos, scale, sideSign * 75f, fov, -sideSign * 5f, false);
                yield return null;
            }

            // Pha 3: Người chơi biến mất hoàn toàn vào bụng Dino & Dino nuốt ực
            player.SetPlayerVisualVisible(false);
            flashAlpha = 0.55f;
            SetOverlayImmediate(barTarget, flashAlpha);
            TriggerCutsceneShake(impactShakeIntensity, impactShakeDuration);

            if (killer != null && killer.carnivoreSpawnDebris)
            {
                int debrisCount = killer.Data != null ? killer.Data.eatDebrisCount : 6;
                Obstacle.SpawnDinoEatenDebris(killer.GetMouthWorldPosition(), dinoFwd, 1f, debrisCount);
            }

            float gulpDur = Mathf.Max(0.20f, killActionDuration);
            t = 0f;
            while (t < gulpDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / gulpDur);

                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, udt * 1.6f);
                SetOverlayImmediate(barTarget, flashAlpha);

                if (dinoVisual != null)
                {
                    float gulpWave = Mathf.Sin(p * Mathf.PI * 2f) * Mathf.Exp(-p * 2.5f);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(-14f * gulpWave, 8f * gulpWave, 0f);
                    dinoVisual.localPosition = dinoBaseLocalPos + Vector3.up * (0.18f * scale * Mathf.Abs(gulpWave));
                }

                Vector3 mouthPos = killer.GetMouthWorldPosition();
                PositionCutsceneCamera(camPhase2, style, player, killer, mouthPos, killer.transform.position + Vector3.up * scale, scale, sideSign * (75f + p * 14f), baseFov - 12f, 0f, false);
                yield return null;
            }
        }
        else if (style == DinoKillCutsceneStyle.Stomp)
        {
            // ================= KIỂU 3: NGÃ NGỬA & BỊ DINO GIẪM BẸP =================
            Vector3 landXZ = dinoStartPos + dinoFwd * (2.65f * scale);
            float landY = player.GetGroundPlayerRootY(landXZ);
            Vector3 landPos = new Vector3(landXZ.x, landY, landXZ.z);
            corpseWorldPos = landPos;

            player.PlayKilledByDinoSupineAnim();
            if (playerVisual != null)
                playerVisual.rotation = Quaternion.LookRotation(-dinoFwd);

            float fallDur = Mathf.Max(0.22f, throwPhaseDuration * 0.9f);
            float t = 0f;
            while (t < fallDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / fallDur);
                curBar = Mathf.MoveTowards(curBar, barTarget, udt * 420f);
                SetOverlayImmediate(curBar, 0f);

                float arc = 4f * p * (1f - p) * (0.85f * scale);
                player.transform.position = Vector3.Lerp(startSeatPos, landPos, p) + Vector3.up * arc;
                if (playerVisual != null)
                    playerVisual.rotation = Quaternion.LookRotation(-dinoFwd);

                PositionCutsceneCamera(camPhase1, style, player, killer, player.transform.position, killer.GetMouthWorldPosition(), scale, sideSign * Mathf.Lerp(20f, 45f, p), baseFov, 0f, t <= udt * 1.1f);
                yield return null;
            }
            player.transform.position = landPos;

            // Pha 2: Dino tiến lên sát người chơi và nhấc thân trước / chân lên cao
            Vector3 stompDinoPos = landPos - dinoFwd * (1.15f * scale);
            stompDinoPos.y = dinoStartPos.y;
            float rearDur = Mathf.Max(0.20f, strikePhaseDuration + 0.08f);
            t = 0f;
            while (t < rearDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / rearDur);
                float ease = Mathf.SmoothStep(0f, 1f, p);

                Time.timeScale = (p > 0.55f) ? apexSlowMoScale : 1f;
                killer.transform.position = Vector3.Lerp(dinoStartPos, stompDinoPos, ease);
                if (dinoVisual != null)
                {
                    dinoVisual.localPosition = dinoBaseLocalPos + Vector3.up * (ease * 1.15f * scale);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(-28f * ease, 0f, 0f);
                }

                PositionCutsceneCamera(DinoKillCameraStyle.LowAnglePredator, style, player, killer, landPos, killer.transform.position + Vector3.up * (1.8f * scale), scale, sideSign * 35f, Mathf.Lerp(baseFov, baseFov - 10f, ease), sideSign * 6f, t <= udt * 1.1f);
                yield return null;
            }

            // Pha 3: Giáng chân / thân xuống giẫm bẹp người chơi!
            Time.timeScale = 1f;
            float slamDur = 0.11f;
            t = 0f;
            while (t < slamDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / slamDur);
                float slamEase = p * p;

                if (dinoVisual != null)
                {
                    dinoVisual.localPosition = dinoBaseLocalPos + Vector3.up * ((1f - slamEase) * 1.15f * scale);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(Mathf.Lerp(-28f, 10f, slamEase), 0f, 0f);
                }
                yield return null;
            }

            flashAlpha = 0.62f;
            SetOverlayImmediate(barTarget, flashAlpha);
            TriggerCutsceneShake(impactShakeIntensity * 1.35f, impactShakeDuration * 1.25f);
            if (playerVisual != null)
            {
                playerVisual.localScale = new Vector3(playerBaseScale.x * 1.35f, playerBaseScale.y * 0.14f, playerBaseScale.z * 1.28f);
            }
            int stompDebris = killer != null && killer.Data != null ? Mathf.Max(4, killer.Data.eatDebrisCount) : 6;
            Obstacle.SpawnDinoEatenDebris(landPos + Vector3.up * 0.15f, Vector3.up, 0.85f, stompDebris);

            float pressDur = Mathf.Max(0.20f, killActionDuration);
            t = 0f;
            while (t < pressDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / pressDur);
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, udt * 1.6f);
                SetOverlayImmediate(barTarget, flashAlpha);

                if (dinoVisual != null)
                {
                    float press = Mathf.Sin(p * Mathf.PI) * 5f;
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(6f + press, 0f, 0f);
                }

                PositionCutsceneCamera(camPhase2, style, player, killer, landPos, killer.transform.position + Vector3.up * scale, scale, sideSign * (35f + p * 16f), baseFov - 12f, 0f, false);
                yield return null;
            }
        }
        else
        {
            // ================= KIỂU 1: VỒ & CẠP KHI NGƯỜI CHƠI NẰM NGỬA =================
            Vector3 landXZ = dinoStartPos + dinoFwd * (2.85f * scale);
            float landY = player.GetGroundPlayerRootY(landXZ);
            Vector3 landPos = new Vector3(landXZ.x, landY, landXZ.z);
            corpseWorldPos = landPos;

            player.PlayKilledByDinoSupineAnim();
            if (playerVisual != null)
                playerVisual.rotation = Quaternion.LookRotation(-dinoFwd);

            float fallDur = Mathf.Max(0.22f, throwPhaseDuration);
            float t = 0f;
            while (t < fallDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / fallDur);
                curBar = Mathf.MoveTowards(curBar, barTarget, udt * 420f);
                SetOverlayImmediate(curBar, 0f);

                float arc = 4f * p * (1f - p) * (0.95f * scale);
                player.transform.position = Vector3.Lerp(startSeatPos, landPos, p) + Vector3.up * arc;
                if (playerVisual != null)
                    playerVisual.rotation = Quaternion.LookRotation(-dinoFwd);

                if (dinoVisual != null)
                {
                    float rear = Mathf.Sin(p * Mathf.PI * 0.5f);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(-16f * rear, 0f, 0f);
                }

                PositionCutsceneCamera(camPhase1, style, player, killer, player.transform.position, killer.GetMouthWorldPosition(), scale, sideSign * Mathf.Lerp(25f, 52f, p), baseFov, 0f, t <= udt * 1.1f);
                yield return null;
            }
            player.transform.position = landPos;

            // Pha 2: Dino lao chồm tới (Pounce) và cắm đầu xuống cạp
            Vector3 pounceDinoPos = landPos - dinoFwd * (1.45f * scale);
            pounceDinoPos.y = dinoStartPos.y;
            float pounceDur = Mathf.Max(0.15f, strikePhaseDuration);
            t = 0f;
            while (t < pounceDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / pounceDur);
                float ease = p * p;

                Time.timeScale = (p < 0.45f) ? apexSlowMoScale : 1f;
                killer.transform.position = Vector3.Lerp(dinoStartPos, pounceDinoPos, ease);
                if (dinoVisual != null)
                {
                    float hop = 4f * p * (1f - p) * (0.45f * scale);
                    float bitePitch = Mathf.Lerp(-18f, 26f, ease);
                    dinoVisual.localPosition = dinoBaseLocalPos + new Vector3(0f, hop - ease * 0.32f * scale, ease * 0.25f * scale);
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(bitePitch, 0f, 0f);
                }

                PositionCutsceneCamera(camPhase2, style, player, killer, landPos, killer.GetMouthWorldPosition(), scale, sideSign * 58f, Mathf.Lerp(baseFov, baseFov - 14f, ease), -sideSign * 5f, t <= udt * 1.1f);
                yield return null;
            }

            // Pha 3: Cạp mạnh & lắc đầu xé mồi 2 nhịp
            Time.timeScale = 1f;
            flashAlpha = 0.58f;
            SetOverlayImmediate(barTarget, flashAlpha);
            TriggerCutsceneShake(impactShakeIntensity, impactShakeDuration);

            float maulDur = Mathf.Max(0.25f, killActionDuration);
            t = 0f;
            bool secondBiteShook = false;
            while (t < maulDur)
            {
                if (killer == null || player == null) break;
                float udt = Time.unscaledDeltaTime;
                t += udt;
                float p = Mathf.Clamp01(t / maulDur);

                if (p >= 0.48f && !secondBiteShook)
                {
                    secondBiteShook = true;
                    flashAlpha = 0.42f;
                    TriggerCutsceneShake(impactShakeIntensity * 0.85f, impactShakeDuration * 0.8f);
                }
                else
                {
                    flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, udt * 1.6f);
                }
                SetOverlayImmediate(barTarget, flashAlpha);

                if (dinoVisual != null)
                {
                    float thrashYaw = Mathf.Sin(p * Mathf.PI * 4f) * 16f;
                    float chompPitch = 22f + Mathf.Cos(p * Mathf.PI * 4f) * 8f;
                    dinoVisual.localRotation = dinoBaseLocalRot * Quaternion.Euler(chompPitch, thrashYaw, thrashYaw * 0.35f);
                    dinoVisual.localPosition = dinoBaseLocalPos + new Vector3(0f, -0.28f * scale, 0.22f * scale);
                }

                if (playerVisual != null)
                {
                    float jolt = Mathf.Sin(p * Mathf.PI * 4f) * 0.08f;
                    playerVisual.localPosition = player.VisualBaseLocalPos + new Vector3(jolt, 0f, 0f);
                }

                PositionCutsceneCamera(camPhase2, style, player, killer, landPos, killer.GetMouthWorldPosition(), scale, sideSign * (58f + p * 18f), baseFov - 14f, 0f, false);
                yield return null;
            }
            if (playerVisual != null)
                playerVisual.localPosition = player.VisualBaseLocalPos;
        }

        // ================= PHA 4: SAU KHI KẾT THÚC KILL (HIỆN CHỮ DEVOURED!/DEAD! & NHÌN XÁC / THEO DINO) =================
        Time.timeScale = 1f;

        // Thả Dino chạy tiếp về phía trước
        if (killer != null) killer.EndKillCutscene();

        // Giữ xác người chơi nằm hoàn toàn bất động trên đất (nếu không phải bị nuốt)
        if (style != DinoKillCutsceneStyle.TossAndSwallow && player != null && player.animator != null)
        {
            player.animator.speed = 0f;
        }

        float waitDur = Mathf.Max(0.35f, postKillWaitDuration);
        float postTimer = 0f;
        while (postTimer < waitDur)
        {
            if (player == null) break;
            float udt = Time.unscaledDeltaTime;
            postTimer += udt;

            // Mở rộng lại 2 thanh đen trên/dưới (không thu hẹp màn hình ở pha sau khi bị kill)
            curBar = Mathf.MoveTowards(curBar, 0f, udt * 520f);
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, udt * 2f);
            SetOverlayImmediate(curBar, flashAlpha);

            // Hiện bảng chữ DEVOURED! / MAULED! / CRUSHED! với hiệu ứng bung lò xo
            float popScale = EvaluateBannerSpringScale(postTimer);
            SetBannerVisible(true, style, popScale);

            if (style == DinoKillCutsceneStyle.TossAndSwallow && killer != null)
            {
                // Bị nuốt: camera đi theo sau lưng con Dino vừa nuốt mình (giống hệt ảnh mẫu DEVOURED!)
                Vector3 kFwd = Vector3.ProjectOnPlane(killer.transform.forward, Vector3.up).normalized;
                if (kFwd.sqrMagnitude < 0.001f) kFwd = dinoFwd;
                Vector3 followPos = killer.transform.position
                    - kFwd * (followDinoDistanceAfterSwallow * scale)
                    + Vector3.up * (followDinoHeightAfterSwallow * scale);
                Vector3 lookAtDino = killer.transform.position
                    + kFwd * (1.2f * scale)
                    + Vector3.up * (1.1f * scale);
                ApplyCutsceneCameraTransform(player, followPos, lookAtDino, baseFov, 0f, false);
            }
            else
            {
                // Bị vồ cạp / giẫm: camera nhìn vào xác của bản thân đang nằm bất động trên mặt đất
                Vector3 corpseCamPos = corpseWorldPos
                    - dinoFwd * (corpseViewDistance * 0.55f)
                    + dinoRight * (sideSign * corpseViewDistance * 0.85f)
                    + Vector3.up * corpseViewHeight;
                Vector3 lookAtCorpse = corpseWorldPos + Vector3.up * 0.25f;
                ApplyCutsceneCameraTransform(player, corpseCamPos, lookAtCorpse, baseFov - 4f, 0f, false);
            }

            yield return null;
        }

        // Kết thúc hoàn toàn: ẩn bảng chữ, khôi phục tốc độ Animator, tắt VCam cutscene, hồi sinh & hiện lại đầy đủ UI
        Time.timeScale = 1f;
        if (player != null && player.animator != null)
            player.animator.speed = 1f;
        if (cutsceneVcam != null) cutsceneVcam.Priority = 0;
        SetOverlayImmediate(0f, 0f);
        SetBannerVisible(false, style, 1f);

        if (player != null) player.FinishKillCutsceneAndRespawn();
        SetGameplayHudVisible(true);

        IsPlaying = false;
        activeRoutine = null;
    }
}
