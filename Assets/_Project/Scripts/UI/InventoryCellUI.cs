using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô trong popup Dinosaurs. Là PREFAB (Assets/_Project/Prefabs/UI/InventoryCell).
public class InventoryCellUI : MonoBehaviour
{
    public Button button;
    public Image border;
    public Image icon;
    public TMP_Text rarityText;
    public TMP_Text nameText;
    public TMP_Text rateText;

    [Header("Tự động căn bố cục & cỡ chữ vừa ô (kiểu bản gốc)")]
    public bool useOriginalCardLayout = true;
    public bool useRoundedCardStyle = true;
    public int cardCornerRadius = 14;
    public Color selectionOutlineColor = Color.white;
    public float selectionOutlinePadding = 4f;
    public int selectionOutlineThickness = 4;
    public float nameFontMin = 9f;
    public float nameFontMax = 18f;
    public float tagFontMin = 9f;
    public float tagFontMax = 15f;
    public float rateFontMin = 10f;
    public float rateFontMax = 17f;

    bool layoutApplied;
    GameObject selectionFrame;

    public void Bind(OwnedDino d, bool held, Action<OwnedDino> onClick)
    {
        if (d == null || d.data == null) return;
        if (useOriginalCardLayout && !layoutApplied)
        {
            ApplyOriginalCardLayout();
            layoutApplied = true;
        }

        Color rc = DinoColors.Of(d.data.rarity);
        if (border != null) border.color = held ? selectionOutlineColor : rc;
        SetSelectionFrameActive(held);
        if (icon != null)
        {
            icon.sprite = d.data.icon;
            icon.enabled = d.data.icon != null;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        if (rarityText != null)
        {
            string tags = BuildTagsText(d);
            rarityText.text = tags;
            rarityText.gameObject.SetActive(!string.IsNullOrEmpty(tags));
        }

        if (nameText != null)
        {
            nameText.text = d.data.displayName;
            nameText.color = rc;
        }

        if (rateText != null)
        {
            rateText.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";
            rateText.color = Color.white;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { if (onClick != null) onClick(d); });
            UIEffectUtil.AttachButtonBounce(this, button);
        }
    }

    static string BuildTagsText(OwnedDino d)
    {
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

    void ApplyOriginalCardLayout()
    {
        if (useRoundedCardStyle)
        {
            Sprite roundSp = UIEffectUtil.GetRoundedRectSprite(64, cardCornerRadius);
            if (border != null && border.sprite == null)
            {
                border.sprite = roundSp;
                border.type = Image.Type.Sliced;
            }
            Transform innerT = transform.Find("Inner");
            Image innerImg = innerT != null ? innerT.GetComponent<Image>() : null;
            if (innerImg != null && innerImg.sprite == null)
            {
                innerImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, Mathf.Max(6, cardCornerRadius - 3));
                innerImg.type = Image.Type.Sliced;
            }
        }

        if (icon != null)
        {
            RectTransform rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0.14f, 0.34f);
            rt.anchorMax = new Vector2(0.86f, 0.90f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            icon.preserveAspect = true;
        }

        if (rarityText != null)
        {
            RectTransform rt = rarityText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.56f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(6f, 0f);
            rt.offsetMax = new Vector2(-6f, -4f);
            rarityText.alignment = TextAlignmentOptions.TopLeft;
            rarityText.fontStyle = FontStyles.Bold;
            rarityText.enableAutoSizing = true;
            rarityText.fontSizeMin = tagFontMin;
            rarityText.fontSizeMax = tagFontMax;
            rarityText.enableWordWrapping = true;
            rarityText.raycastTarget = false;
            rarityText.transform.SetAsLastSibling();
        }

        if (nameText != null)
        {
            RectTransform rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.17f);
            rt.anchorMax = new Vector2(1f, 0.35f);
            rt.offsetMin = new Vector2(5f, 0f);
            rt.offsetMax = new Vector2(-5f, 0f);
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = nameFontMin;
            nameText.fontSizeMax = nameFontMax;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.raycastTarget = false;
        }

        if (rateText != null)
        {
            RectTransform rt = rateText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.02f);
            rt.anchorMax = new Vector2(1f, 0.19f);
            rt.offsetMin = new Vector2(5f, 2f);
            rt.offsetMax = new Vector2(-5f, 0f);
            rateText.alignment = TextAlignmentOptions.Center;
            rateText.fontStyle = FontStyles.Bold;
            rateText.enableAutoSizing = true;
            rateText.fontSizeMin = rateFontMin;
            rateText.fontSizeMax = rateFontMax;
            rateText.enableWordWrapping = false;
            rateText.raycastTarget = false;
        }
    }

    void SetSelectionFrameActive(bool on)
    {
        if (!useRoundedCardStyle) return;
        if (on && selectionFrame == null)
        {
            selectionFrame = new GameObject("SelectionOutline", typeof(RectTransform), typeof(Image));
            selectionFrame.transform.SetParent(transform, false);
            selectionFrame.transform.SetAsLastSibling();
            RectTransform rt = selectionFrame.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            float pad = selectionOutlinePadding;
            rt.offsetMin = new Vector2(-pad, -pad);
            rt.offsetMax = new Vector2(pad, pad);
            Image img = selectionFrame.GetComponent<Image>();
            img.sprite = UIEffectUtil.GetRoundedOutlineSprite(64, cardCornerRadius + 2, selectionOutlineThickness);
            img.type = Image.Type.Sliced;
            img.color = selectionOutlineColor;
            img.raycastTarget = false;
        }
        if (selectionFrame != null && selectionFrame.activeSelf != on)
            selectionFrame.SetActive(on);
    }
}
