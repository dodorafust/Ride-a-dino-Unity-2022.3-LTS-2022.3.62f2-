using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng trong bảng quản lý chuồng. Là PREFAB (Assets/_Project/Prefabs/UI/PenRow).
public class PenRowUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text titleText;
    public TMP_Text rateText;
    public Button rideButton;
    public Button unequipButton;
    public TMP_Text rideLabel;          // chữ trên nút Ride ("Ride" / "Riding")

    public void Bind(OwnedDino d, bool isMount, Action<OwnedDino> onRide, Action<OwnedDino> onUnequip)
    {
        if (icon != null) { icon.sprite = d.data.icon; icon.enabled = d.data.icon != null; }
        if (titleText != null)
        {
            titleText.text = d.Title;
            titleText.color = DinoColors.Of(d.data.rarity);
        }
        if (rateText != null) rateText.text = "$" + NumberFormat.Short(d.MoneyPerSecond) + "/s";
        if (rideLabel != null) rideLabel.text = isMount ? "Riding" : "Ride";

        if (rideButton != null)
        {
            rideButton.onClick.RemoveAllListeners();
            rideButton.onClick.AddListener(delegate { if (onRide != null) onRide(d); });
        }
        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(delegate { if (onUnequip != null) onUnequip(d); });
        }
    }
}
