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

    public void Bind(OwnedDino d, bool held, Action<OwnedDino> onClick)
    {
        Color rc = DinoColors.Of(d.data.rarity);
        if (border != null) border.color = held ? Color.white : rc;
        if (icon != null) { icon.sprite = d.data.icon; icon.enabled = d.data.icon != null; }
        if (rarityText != null) { rarityText.text = d.data.rarity.ToString(); rarityText.color = rc; }
        if (nameText != null) nameText.text = d.Title;
        if (rateText != null) rateText.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { if (onClick != null) onClick(d); });
        }
    }
}
