using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bảng quản lý chuồng (x/y Active, stored, Ride / Unequip, Equip Best, Collect).
// Gắn lên chính panel; nút bên phải màn hình (PenButton) bật/tắt bằng Toggle(), không cần đứng trong chuồng.
public class PenPanel : MonoBehaviour
{
    public RectTransform content;            // nơi sinh các dòng (có VerticalLayoutGroup)
    public PenRowUI rowPrefab;
    public TMP_Text headerText;
    public TMP_Text storedText;
    public Button equipBestButton;
    public Button collectButton;
    public Button closeButton;

    Pen pen;
    GameManager gm;
    MountController mount;
    bool subscribed;

    public bool Visible { get { return gameObject.activeSelf; } }

    public void Toggle() { gameObject.SetActive(!gameObject.activeSelf); }
    public void Hide() { gameObject.SetActive(false); }

    void Awake()
    {
        if (equipBestButton != null) equipBestButton.onClick.AddListener(delegate { if (pen != null) pen.EquipBest(); });
        if (collectButton != null) collectButton.onClick.AddListener(delegate { if (pen != null) pen.Collect(true); });
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
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
        if (subscribed)
        {
            if (pen != null) pen.Changed -= Rebuild;
            if (gm != null) gm.Inventory.Changed -= Rebuild;
            if (mount != null) mount.Changed -= Rebuild;
        }
        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        gm = GameManager.Instance;
        pen = FindObjectOfType<Pen>();
        mount = MountController.Instance;
        if (pen == null || gm == null) return;

        pen.Changed += Rebuild;
        gm.Inventory.Changed += Rebuild;
        if (mount != null) mount.Changed += Rebuild;
        subscribed = true;
    }

    void Rebuild()
    {
        if (pen == null || gm == null || content == null || rowPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject c = content.GetChild(i).gameObject;
            c.SetActive(false);
            Destroy(c);
        }

        if (headerText != null) headerText.text = pen.Active.Count + "/" + pen.Slots + " Active";
        if (storedText != null) storedText.text = gm.Inventory.Items.Count + " stored";

        var list = new List<OwnedDino>(pen.Active);
        list.Sort((a, b) => b.MoneyPerSecond.CompareTo(a.MoneyPerSecond));
        for (int i = 0; i < list.Count; i++)
        {
            PenRowUI row = Instantiate(rowPrefab, content);
            bool isMount = mount != null && mount.SlotDino == list[i];
            row.Bind(list[i], isMount, OnRide, OnUnequip);
        }
    }

    void OnRide(OwnedDino d)
    {
        if (mount != null) mount.Ride(d);
    }

    void OnUnequip(OwnedDino d)
    {
        if (pen != null) pen.Withdraw(d);
    }
}
