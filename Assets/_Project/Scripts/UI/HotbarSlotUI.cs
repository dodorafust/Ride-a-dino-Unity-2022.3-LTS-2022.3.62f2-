using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô hotbar. Là PREFAB (Assets/_Project/Prefabs/UI/HotbarSlot): sửa hình / kích thước / font trong prefab.
public class HotbarSlotUI : MonoBehaviour
{
    public Button button;
    public Image border;                 // viền đổi màu theo Rarity
    public Image icon;                   // icon Dino (tắt nếu DinoData chưa có icon)
    public TMP_Text nameText;
    public TMP_Text rateText;
    public TMP_Text keyText;
    public Color emptyBorderColor = new Color(0.25f, 0.25f, 0.28f, 0.9f);

    [Header("Giao diện giống bản gốc + Khung trắng khi chọn")]
    public bool useOriginalBarStyle = true;
    public bool showRateText = false;
    public Color selectionOutlineColor = Color.white;
    public float selectionOutlineThickness = 4f;
    public float selectionOutlinePadding = 5f;
    public float nameFontMin = 10f;
    public float nameFontMax = 18f;

    public OwnedDino Owned { get; private set; }

    bool isRideSlot;
    string keyLabel = "";
    bool layoutApplied;
    GameObject selectionFrame;

    public void SetDino(OwnedDino d, bool selected)
    {
        Owned = d;
        if (useOriginalBarStyle && !layoutApplied)
        {
            ApplyOriginalLayout();
            layoutApplied = true;
        }

        if (d == null) { Clear(); return; }

        Color rc = DinoColors.Of(d.data.rarity);
        if (border != null) border.color = rc;
        if (icon != null)
        {
            icon.sprite = d.data.icon;
            icon.enabled = d.data.icon != null;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        if (nameText != null)
        {
            string n = d.data.displayName;
            nameText.text = n.Length > 10 ? n.Substring(0, 9) + "." : n;
            nameText.color = Color.white;
        }

        if (rateText != null)
        {
            rateText.gameObject.SetActive(showRateText);
            if (showRateText) rateText.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";
        }

        if (keyText != null)
        {
            if (useOriginalBarStyle && !isRideSlot)
            {
                keyText.text = BuildTagsText(d);
            }
            else if (isRideSlot)
            {
                string tags = BuildTagsText(d);
                keyText.text = string.IsNullOrEmpty(tags) ? keyLabel : keyLabel + " " + tags;
            }
        }

        SetSelectionFrameActive(selected);
    }

    public void Clear()
    {
        Owned = null;
        if (border != null) border.color = emptyBorderColor;
        if (icon != null) icon.enabled = false;
        if (nameText != null) nameText.text = "";
        if (rateText != null) rateText.text = "";
        if (keyText != null) keyText.text = (useOriginalBarStyle && !isRideSlot) ? "" : keyLabel;
        SetSelectionFrameActive(false);
    }

    public void SetKey(string k)
    {
        keyLabel = k ?? "";
        isRideSlot = string.Equals(keyLabel, "Q", System.StringComparison.OrdinalIgnoreCase);
        if (keyText != null)
            keyText.text = (useOriginalBarStyle && !isRideSlot && Owned == null) ? "" : keyLabel;
    }

    static string BuildTagsText(OwnedDino d)
    {
        if (d == null) return "";
        string vTag = (d.variant != null && !d.variant.isNormal)
            ? "<color=#FFD94A>" + d.variant.displayName.ToUpperInvariant() + "</color>"
            : "";
        string sTag = !string.IsNullOrEmpty(d.SizeTag)
            ? "<color=#" + DinoColors.Hex(d.SizeColor) + ">" + d.SizeTag + "</color>"
            : "";
        if (string.IsNullOrEmpty(vTag)) return sTag;
        if (string.IsNullOrEmpty(sTag)) return vTag;
        return vTag + "\n" + sTag;
    }

    void ApplyOriginalLayout()
    {
        if (icon != null)
        {
            RectTransform rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0.10f, 0.24f);
            rt.anchorMax = new Vector2(0.90f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            icon.preserveAspect = true;
        }

        if (keyText != null)
        {
            RectTransform rt = keyText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.55f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(5f, 0f);
            rt.offsetMax = new Vector2(-4f, -3f);
            keyText.alignment = TextAlignmentOptions.TopLeft;
            keyText.fontStyle = FontStyles.Bold;
            keyText.enableAutoSizing = true;
            keyText.fontSizeMin = 9f;
            keyText.fontSizeMax = 15f;
            keyText.color = Color.white;
            keyText.transform.SetAsLastSibling();
        }

        if (nameText != null)
        {
            RectTransform rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.30f);
            rt.offsetMin = new Vector2(3f, 2f);
            rt.offsetMax = new Vector2(-3f, 0f);
            nameText.alignment = TextAlignmentOptions.Bottom;
            nameText.fontStyle = FontStyles.Bold;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = nameFontMin;
            nameText.fontSizeMax = nameFontMax;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.transform.SetAsLastSibling();
        }

        if (rateText != null)
            rateText.gameObject.SetActive(showRateText);
    }

    void SetSelectionFrameActive(bool on)
    {
        if (!useOriginalBarStyle) return;
        if (on && selectionFrame == null) CreateSelectionFrame();
        if (selectionFrame != null && selectionFrame.activeSelf != on)
            selectionFrame.SetActive(on);
    }

    void CreateSelectionFrame()
    {
        selectionFrame = new GameObject("SelectionOutline", typeof(RectTransform));
        selectionFrame.transform.SetParent(transform, false);
        selectionFrame.transform.SetAsLastSibling();

        RectTransform rootRt = selectionFrame.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        float pad = selectionOutlinePadding;
        rootRt.offsetMin = new Vector2(-pad, -pad);
        rootRt.offsetMax = new Vector2(pad, pad);

        float t = Mathf.Max(1f, selectionOutlineThickness);
        AddEdge(rootRt, "Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -t), Vector2.zero);
        AddEdge(rootRt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, t));
        AddEdge(rootRt, "Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(t, 0f));
        AddEdge(rootRt, "Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-t, 0f), Vector2.zero);
    }

    void AddEdge(RectTransform parent, string edgeName, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = new GameObject(edgeName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        Image img = go.GetComponent<Image>();
        img.color = selectionOutlineColor;
        img.raycastTarget = false;
    }
}
