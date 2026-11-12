using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup "Dinosaurs". Gắn lên chính panel; HudController bật/tắt panel bằng Toggle().
public class InventoryPopup : MonoBehaviour
{
    public RectTransform content;            // nơi sinh ra các ô (có GridLayoutGroup)
    public InventoryCellUI cellPrefab;
    public TMP_Text countText;
    public TMP_Text footerText;
    public Button closeButton;
    public Button sellButton;

    GameManager gm;
    bool subscribed;

    public bool Visible { get { return gameObject.activeSelf; } }

    public void Toggle() { gameObject.SetActive(!gameObject.activeSelf); }
    public void Hide() { gameObject.SetActive(false); }

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (sellButton != null) sellButton.onClick.AddListener(SellHeld);
    }

    // Bảng để mở sẵn trong scene cho dễ chỉnh; vào Play thì tự ẩn
    public bool hideOnStart = true;

    void Start()
    {
        if (hideOnStart) gameObject.SetActive(false);
    }

    void OnEnable()
    {
        TrySubscribe();
        Rebuild();
    }

    void OnDisable()
    {
        if (gm != null && subscribed) gm.Inventory.Changed -= Rebuild;
        subscribed = false;
    }

    void TrySubscribe()
    {
        gm = GameManager.Instance;
        if (gm != null && !subscribed)
        {
            gm.Inventory.Changed += Rebuild;
            subscribed = true;
        }
    }

    void SellHeld()
    {
        if (gm != null) gm.Sell(gm.Inventory.Held);
    }

    void Rebuild()
    {
        if (gm == null || content == null || cellPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject c = content.GetChild(i).gameObject;
            c.SetActive(false);
            Destroy(c);
        }

        Inventory inv = gm.Inventory;
        if (countText != null) countText.text = "Dinos: " + inv.Items.Count;

        var list = new List<OwnedDino>(inv.Items);
        list.Sort((a, b) => b.MoneyPerSecond.CompareTo(a.MoneyPerSecond));
        for (int i = 0; i < list.Count; i++)
        {
            InventoryCellUI cell = Instantiate(cellPrefab, content);
            cell.Bind(list[i], list[i] == inv.Held, OnCellClicked);
        }

        OwnedDino held = inv.Held;
        if (sellButton != null) sellButton.gameObject.SetActive(held != null);
        if (footerText != null)
            footerText.text = held == null
                ? "Tap a dino to hold it, then tap your pen floor"
                : "<color=#" + DinoColors.Hex(DinoColors.Of(held.data.rarity)) + ">" + held.Title + "</color>  $" +
                  NumberFormat.Short(held.MoneyPerSecond) + "/s   (sell $" + NumberFormat.Short(held.SellValue) + ")";
    }

    void OnCellClicked(OwnedDino d)
    {
        if (gm != null) gm.Inventory.ToggleHold(d);
    }
}
