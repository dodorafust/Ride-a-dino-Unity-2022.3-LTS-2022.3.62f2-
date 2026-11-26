using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng trong bảng quản lý chuồng. Là PREFAB (Assets/_Project/Prefabs/UI/PenRow).
public class PenRowUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text titleText;
    public TMP_Text subTagText;
    public TMP_Text rateText;
    public Button rideButton;
    public Button unequipButton;
    public TMP_Text rideLabel;          // chữ trên nút Ride ("Ride" / "Riding")

    public void Bind(OwnedDino d, bool isMount, Action<OwnedDino> onRide, Action<OwnedDino> onUnequip)
    {
        Image rowBg = GetComponent<Image>();
        if (rowBg != null)
        {
            rowBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
            rowBg.type = Image.Type.Sliced;
        }

        if (icon != null) { icon.sprite = d.data.icon; icon.enabled = d.data.icon != null; }

        string tagLine = BuildTagLine(d);
        bool hasTag = !string.IsNullOrEmpty(tagLine);

        if (titleText != null)
        {
            titleText.text = d.data != null ? d.data.displayName : d.Title;
            titleText.color = Color.white;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 14f;
            titleText.fontSizeMax = 24f;
            titleText.enableWordWrapping = false;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.outlineWidth = 0.22f;
            titleText.outlineColor = Color.black;
            Vector2 pos = titleText.rectTransform.anchoredPosition;
            titleText.rectTransform.anchoredPosition = new Vector2(pos.x, hasTag ? 22f : 14f);
        }

        EnsureSubTagText();
        if (subTagText != null)
        {
            subTagText.gameObject.SetActive(hasTag);
            if (hasTag)
            {
                subTagText.text = tagLine;
                Vector2 pos = subTagText.rectTransform.anchoredPosition;
                subTagText.rectTransform.anchoredPosition = new Vector2(pos.x, 1f);
            }
        }

        if (rateText != null)
        {
            rateText.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";
            rateText.outlineWidth = 0.22f;
            rateText.outlineColor = Color.black;
            Vector2 pos = rateText.rectTransform.anchoredPosition;
            rateText.rectTransform.anchoredPosition = new Vector2(pos.x, hasTag ? -20f : -14f);
        }
        if (rideLabel != null) rideLabel.text = isMount ? "Riding" : "Ride";

        if (rideButton != null)
        {
            Image rBg = rideButton.GetComponent<Image>();
            if (rBg != null) { rBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10); rBg.type = Image.Type.Sliced; }
            rideButton.onClick.RemoveAllListeners();
            rideButton.onClick.AddListener(delegate { if (onRide != null) onRide(d); });
            UIEffectUtil.AttachButtonBounce(this, rideButton);
        }
        if (unequipButton != null)
        {
            Image uBg = unequipButton.GetComponent<Image>();
            if (uBg != null) { uBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10); uBg.type = Image.Type.Sliced; }
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(delegate { if (onUnequip != null) onUnequip(d); });
            UIEffectUtil.AttachButtonBounce(this, unequipButton);
        }
    }

    void EnsureSubTagText()
    {
        if (subTagText != null || titleText == null) return;
        GameObject go = new GameObject("SubTag", typeof(RectTransform));
        go.transform.SetParent(titleText.transform.parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        RectTransform src = titleText.rectTransform;
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = new Vector2(src.anchoredPosition.x, 1f);
        rt.sizeDelta = new Vector2(src.sizeDelta.x, 24f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = titleText.font;
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontSize = 16f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 16f;
        tmp.alignment = titleText.alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = Color.black;
        subTagText = tmp;
    }

    static string BuildTagLine(OwnedDino d)
    {
        if (d == null) return "";
        string part1 = "";
        if (d.variant != null && !d.variant.isNormal && !string.IsNullOrEmpty(d.variant.displayName))
        {
            Color vc = (d.variant.useTint && d.variant.tint.maxColorComponent > 0.15f)
                ? d.variant.tint
                : new Color(1f, 0.82f, 0.25f);
            part1 = "<color=#" + ColorUtility.ToHtmlStringRGB(vc) + ">" + d.variant.displayName.ToUpperInvariant() + "</color>";
        }
        string part2 = "";
        string sTag = d.SizeTag;
        if (!string.IsNullOrEmpty(sTag))
        {
            Color sc = d.sizeAsset != null ? d.sizeAsset.tagColor : new Color(0.95f, 0.85f, 0.45f);
            part2 = "<color=#" + ColorUtility.ToHtmlStringRGB(sc) + ">" + sTag.ToUpperInvariant() + "</color>";
        }
        if (!string.IsNullOrEmpty(part1) && !string.IsNullOrEmpty(part2))
            return part1 + "  " + part2;
        return !string.IsNullOrEmpty(part1) ? part1 : part2;
    }
}
