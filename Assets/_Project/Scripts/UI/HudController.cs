using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gắn lên Canvas / HUD. Mọi thành phần UI đều là object trong scene (menu Tools > Ride a Dino > 2. Build UI
// đã dựng sẵn và gán các ô bên dưới). Bạn tự chỉnh vị trí, kích thước, icon, font bằng tay.
// Thiếu ô nào (để trống) thì phần đó bị bỏ qua, không lỗi.
public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    public static void Say(string msg, float seconds = 2.2f)
    {
        if (Instance != null) Instance.ShowMessage(msg, seconds);
    }

    [Header("Chỉ số")]
    public TMP_Text staminaText;
    public TMP_Text cashText;

    [Header("Hotbar: 1 ô cưỡi + 6 ô Dino")]
    public HotbarSlotUI rideSlot;
    public HotbarSlotUI[] slots;

    [Header("Nút")]
    public Button penButton;             // nút quản lý chuồng (bên phải)
    public Button inventoryButton;       // nút mở danh sách Dinos
    public Button shopButton;
    public Button indexButton;

    [Header("Bảng")]
    public InventoryPopup inventoryPopup;
    public PenPanel penPanel;

    [Header("Cưỡi Dino hoang")]
    public RideRingsUI rings;
    public WorldPromptUI ridePrompt;

    [Header("Thông báo")]
    public TMP_Text toastText;
    public GameObject depositHint;       // "GO TO YOUR PEN TO DEPOSIT"

    GameManager gm;
    PlayerController player;
    RideSession ride;
    MountController mount;
    Pen pen;
    Camera cam;
    float toastUntil;

    void Awake() { Instance = this; }

    void Start()
    {
        gm = GameManager.Instance;
        player = PlayerController.Instance;
        mount = MountController.Instance;
        pen = FindObjectOfType<Pen>();
        cam = Camera.main;

        if (gm == null)
        {
            Debug.LogError("HudController: thiếu GameManager (Tools > Ride a Dino > 3. Setup World).");
            enabled = false;
            return;
        }
        if (player != null) ride = player.GetComponent<RideSession>();

        if (penButton != null && penPanel != null) penButton.onClick.AddListener(penPanel.Toggle);
        if (inventoryButton != null && inventoryPopup != null) inventoryButton.onClick.AddListener(inventoryPopup.Toggle);
        if (shopButton != null) shopButton.onClick.AddListener(delegate { Say("Shop: coming in Build 3"); });
        if (indexButton != null) indexButton.onClick.AddListener(delegate { Say("Index: coming in Build 3"); });

        if (rideSlot != null)
        {
            rideSlot.SetKey("Q");
            if (rideSlot.button != null) rideSlot.button.onClick.AddListener(delegate { if (mount != null) mount.ToggleRide(); });
        }
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                int idx = i;
                slots[i].SetKey((i + 1).ToString());
                if (slots[i].button != null) slots[i].button.onClick.AddListener(delegate { SelectSlot(idx); });
            }
        }

        if (toastText != null) toastText.gameObject.SetActive(false);
        if (ridePrompt != null) ridePrompt.gameObject.SetActive(false);

        gm.StatsChanged += RefreshStats;
        gm.Inventory.Changed += RefreshInventory;
        if (mount != null) mount.Changed += RefreshRideSlot;

        RefreshStats();
        RefreshInventory();
        RefreshRideSlot();
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.StatsChanged -= RefreshStats;
            gm.Inventory.Changed -= RefreshInventory;
        }
        if (mount != null) mount.Changed -= RefreshRideSlot;
    }

    // ---------- Refresh ----------
    void RefreshStats()
    {
        if (staminaText != null) staminaText.text = NumberFormat.Short(gm.Stamina);
        if (cashText != null) cashText.text = "$" + NumberFormat.Short(gm.Cash);
    }

    void RefreshInventory()
    {
        if (slots == null) return;
        var best = gm.Inventory.GetBest(slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            OwnedDino d = i < best.Count ? best[i] : null;
            slots[i].SetDino(d, d != null && gm.Inventory.Held == d);
        }
    }

    void RefreshRideSlot()
    {
        if (rideSlot == null) return;
        OwnedDino d = mount != null ? mount.SlotDino : null;
        rideSlot.SetDino(d, mount != null && mount.Mounted);
    }

    void SelectSlot(int i)
    {
        if (slots == null || i < 0 || i >= slots.Length || slots[i] == null) return;
        OwnedDino d = slots[i].Owned;
        if (d != null) gm.Inventory.ToggleHold(d);
    }

    public void ShowMessage(string msg, float seconds)
    {
        if (toastText == null) return;
        toastText.text = msg;
        toastText.gameObject.SetActive(true);
        toastUntil = Time.time + seconds;
    }

    // ---------- Update ----------
    void Update()
    {
        if (gm == null) return;

        // Phím tắt PC (mobile dùng nút)
        if (slots != null)
            for (int i = 0; i < slots.Length && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectSlot(i);
        if (Input.GetKeyDown(KeyCode.I) && inventoryPopup != null) inventoryPopup.Toggle();
        if (Input.GetKeyDown(KeyCode.P) && penPanel != null) penPanel.Toggle();

        if (toastText != null && toastText.gameObject.activeSelf && Time.time > toastUntil)
            toastText.gameObject.SetActive(false);

        if (cam == null) cam = Camera.main;
        if (rings != null) rings.Tick(player, ride, cam);

        if (depositHint != null)
        {
            bool holding = gm.Inventory.Held != null;
            bool inPen = pen != null && pen.PlayerInside;
            bool show = holding && !inPen && player != null && player.State == PlayerState.Walking;
            if (depositHint.activeSelf != show) depositHint.SetActive(show);
        }
    }
}
