using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô hotbar. Là PREFAB (Assets/_Project/Prefabs/UI/HotbarSlot): sửa hình / kích thước / font trong prefab.
public class HotbarSlotUI : MonoBehaviour
{
    public Button button;
    public Image border;                 // viền đổi màu theo Rarity (trắng = đang chọn)
    public Image icon;                   // icon Dino (tắt nếu DinoData chưa có icon)
    public TMP_Text nameText;
    public TMP_Text rateText;
    public TMP_Text keyText;
    public Color emptyBorderColor = new Color(0.25f, 0.25f, 0.28f, 0.9f);

    public OwnedDino Owned { get; private set; }

    public void SetDino(OwnedDino d, bool selected)
    {
        Owned = d;
        if (d == null) { Clear(); return; }

        if (border != null) border.color = selected ? Color.white : DinoColors.Of(d.data.rarity);
        if (icon != null) { icon.sprite = d.data.icon; icon.enabled = d.data.icon != null; }

        string n = d.data.displayName;
        if (nameText != null) nameText.text = n.Length > 10 ? n.Substring(0, 9) + "." : n;
        if (rateText != null) rateText.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";
    }

    public void Clear()
    {
        Owned = null;
        if (border != null) border.color = emptyBorderColor;
        if (icon != null) icon.enabled = false;
        if (nameText != null) nameText.text = "";
        if (rateText != null) rateText.text = "";
    }

    public void SetKey(string k) { if (keyText != null) keyText.text = k; }
}
