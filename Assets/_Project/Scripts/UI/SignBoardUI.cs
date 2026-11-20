using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI gắn trực tiếp lên mặt biển (World Space Canvas) cho UpgradeSign / DinoMachine.
// Bạn có thể chỉnh trực tiếp vị trí, kích thước, ảnh nền, icon, font và mẫu chữ trong Scene View / Inspector.
public class SignBoardUI : MonoBehaviour
{
    [Header("Thành phần UI")]
    public Image backgroundImage;
    public Image iconImage;
    public TMP_Text titleText;
    public TMP_Text detailText;
    public TMP_Text bonusText;
    public Image priceBackground;
    public TMP_Text priceText;

    [Header("Màu nút giá tiền")]
    public Color affordableColor = new Color(0.16f, 0.78f, 0.24f, 1f);
    public Color unaffordableColor = new Color(0.86f, 0.20f, 0.20f, 1f);
    public Color maxLevelColor = new Color(0.25f, 0.55f, 0.90f, 1f);

    [Header("Mẫu nội dung (dùng {0}, {1} để tự điền số)")]
    public string titleFormat = "Upgrade {0}";
    public string levelFormat = "Level {0} > Level {1}";
    public string bonusFormat = "{0}";
    public string priceFormat = "${0}";
    public string maxLevelText = "MAX LEVEL";

    public void SetUpgradeInfo(string upgradeName, int level, string nextInfo, double cost, bool affordable, bool isMaxLevel,
                               string detailOverride = null, Sprite iconOverride = null)
    {
        if (titleText != null)
        {
            string prettyName = ToTitleCase(upgradeName);
            titleText.text = SafeFormat(titleFormat, prettyName);
        }

        if (iconOverride != null && iconImage != null)
            iconImage.sprite = iconOverride;

        if (isMaxLevel)
        {
            if (detailText != null) detailText.text = !string.IsNullOrEmpty(detailOverride) ? detailOverride : ("Level " + level);
            if (bonusText != null) bonusText.text = maxLevelText;
            if (priceText != null) priceText.text = maxLevelText;
            if (priceBackground != null) priceBackground.color = maxLevelColor;
        }
        else
        {
            if (detailText != null)
                detailText.text = !string.IsNullOrEmpty(detailOverride)
                    ? detailOverride
                    : SafeFormat(levelFormat, level, level + 1);
            if (bonusText != null)
                bonusText.text = SafeFormat(bonusFormat, nextInfo);
            if (priceText != null)
                priceText.text = SafeFormat(priceFormat, NumberFormat.Short(cost));
            if (priceBackground != null)
                priceBackground.color = affordable ? affordableColor : unaffordableColor;
        }

        RefreshIconVisibility();
    }

    static string ToTitleCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }

    public void SetCustomInfo(string title, string detail, string bonus, string price, Color priceBgColor)
    {
        if (titleText != null) titleText.text = title;
        if (detailText != null) detailText.text = detail;
        if (bonusText != null) bonusText.text = bonus;
        if (priceText != null) priceText.text = price;
        if (priceBackground != null) priceBackground.color = priceBgColor;
        RefreshIconVisibility();
    }

    void RefreshIconVisibility()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(iconImage.sprite != null);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UpgradeSign parentSign = GetComponentInParent<UpgradeSign>();
        if (parentSign != null) parentSign.RefreshPreviewInEditor();
    }
#endif

    static string SafeFormat(string fmt, object arg0)
    {
        if (string.IsNullOrEmpty(fmt)) return arg0 != null ? arg0.ToString() : "";
        try { return string.Format(fmt, arg0); }
        catch { return fmt; }
    }

    static string SafeFormat(string fmt, object arg0, object arg1)
    {
        if (string.IsNullOrEmpty(fmt)) return "";
        try { return string.Format(fmt, arg0, arg1); }
        catch { return fmt; }
    }

    // Dựng nhanh World-Space Canvas mặc định khớp mặt biển (3m x 1.4m)
    public static SignBoardUI CreateOnBoard(Transform parent, Vector3 localPos, Vector3 localEuler, Vector2 sizeMeters)
    {
        GameObject canvasGo = new GameObject("SignCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.localPosition = localPos;
        canvasGo.transform.localEulerAngles = localEuler;
        canvasGo.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(Mathf.Max(120f, sizeMeters.x * 100f), Mathf.Max(80f, sizeMeters.y * 100f));

        SignBoardUI ui = canvasGo.AddComponent<SignBoardUI>();

        // Nền tối giống biển game gốc
        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(root, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);
        bgImg.raycastTarget = false;
        ui.backgroundImage = bgImg;

        // Icon bên trái (tự hiện khi bạn kéo Sprite vào)
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0f, 0.5f);
        icRt.anchorMax = new Vector2(0f, 0.5f);
        icRt.pivot = new Vector2(0f, 0.5f);
        icRt.anchoredPosition = new Vector2(12f, 0f);
        icRt.sizeDelta = new Vector2(78f, 78f);
        Image icImg = iconGo.GetComponent<Image>();
        icImg.preserveAspect = true;
        icImg.raycastTarget = false;
        iconGo.SetActive(false);
        ui.iconImage = icImg;

        // Tiêu đề
        ui.titleText = CreateTmp(root, "TitleText", new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.96f),
                                 26f, Color.white, FontStyles.Bold, "Upgrade");
        // Cấp độ / phụ đề
        ui.detailText = CreateTmp(root, "DetailText", new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.72f),
                                  20f, new Color(0.32f, 0.94f, 0.36f, 1f), FontStyles.Bold, "Level 1 > Level 2");
        // Chỉ số cộng thêm
        ui.bonusText = CreateTmp(root, "BonusText", new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.52f),
                                 17f, new Color(0.88f, 0.90f, 0.95f, 1f), FontStyles.Bold, "+2 Stamina/s");

        // Nút giá tiền
        GameObject priceBoxGo = new GameObject("PriceBox", typeof(RectTransform), typeof(Image));
        priceBoxGo.transform.SetParent(root, false);
        RectTransform pbRt = priceBoxGo.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0.12f, 0.06f);
        pbRt.anchorMax = new Vector2(0.88f, 0.31f);
        pbRt.offsetMin = Vector2.zero;
        pbRt.offsetMax = Vector2.zero;
        Image pbImg = priceBoxGo.GetComponent<Image>();
        pbImg.color = ui.affordableColor;
        pbImg.raycastTarget = false;
        ui.priceBackground = pbImg;

        ui.priceText = CreateTmp(pbRt, "PriceText", Vector2.zero, Vector2.one,
                                 22f, Color.white, FontStyles.Bold, "$100");

        return ui;
    }

    static TextMeshProUGUI CreateTmp(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                     float fontSize, Color color, FontStyles style, string defaultText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.text = defaultText;
        return tmp;
    }
}
