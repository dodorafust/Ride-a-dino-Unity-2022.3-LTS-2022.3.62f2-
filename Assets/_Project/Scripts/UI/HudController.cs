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

    [Header("Hotbar: 6 ô Dino + nút Q (Ride) tách riêng")]
    public HotbarSlotUI rideSlot;
    public HotbarSlotUI[] slots;
    public float hotbarSlotSpacing = 16f;
    public bool detachRideSlotFromHotbar = true;
    public Vector2 standaloneRidePos = new Vector2(-450f, 16f);
    public Vector2 standaloneRideSize = new Vector2(104f, 104f);

    [Header("Nút")]
    public Button penButton;             // nút quản lý chuồng (bên phải)
    public Button inventoryButton;       // nút mở danh sách Dinos
    public Button shopButton;
    public Button indexButton;

    [Header("Bảng")]
    public InventoryPopup inventoryPopup;
    public PenPanel penPanel;
    public SellDinosPopup sellDinosPopup;
    public DinoIndexPopup dinoIndexPopup;

    [Header("Cưỡi Dino hoang")]
    public RideRingsUI rings;
    public WorldPromptUI ridePrompt;
    public TameSecuredUI tameSecuredUI;

    [Header("Thanh tiến trình & Banner Biome")]
    public BiomeHudUI biomeHud;

    [Header("Thông báo")]
    public TMP_Text toastText;
    public GameObject depositHint;       // "GO TO YOUR PEN TO DEPOSIT"

    [Header("Dev Cheat (F1..F6 trên màn hình)")]
    public bool showDevCheatButtons = true;
    public double cheatCashAmount = 1000;
    public float cheatStaminaAmount = 10f;
    public Button cheatStaminaButton;                 // tùy chọn: để trống sẽ tự tạo đủ cụm nút F1..F6
    public Vector2 cheatButtonPos = new Vector2(1690f, -40f);
    public Vector2 cheatButtonSize = new Vector2(200f, 52f);
    public float cheatButtonSpacing = 8f;
    [Tooltip("Dịch vị trí riêng cho từng nút F1..F6 (tùy chọn).")]
    public Vector2[] cheatButtonOffsets = new Vector2[6];

    GameManager gm;
    PlayerController player;
    RideSession ride;
    MountController mount;
    Pen pen;
    Camera cam;
    float toastUntil;
    GameObject indexAlertBadge;
    readonly RectTransform[] spawnedCheatRects = new RectTransform[6];
    readonly TextMeshProUGUI[] spawnedCheatLabels = new TextMeshProUGUI[6];

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

        if (penButton != null && penPanel != null)
        {
            penButton.onClick.AddListener(penPanel.Toggle);
            UIEffectUtil.AttachButtonBounce(this, penButton);
        }
        if (inventoryButton != null && inventoryPopup != null)
        {
            inventoryButton.onClick.AddListener(inventoryPopup.Toggle);
            UIEffectUtil.AttachButtonBounce(this, inventoryButton);
        }
        if (dinoIndexPopup == null) dinoIndexPopup = FindObjectOfType<DinoIndexPopup>(true);
        if (dinoIndexPopup == null) dinoIndexPopup = gameObject.AddComponent<DinoIndexPopup>();
        dinoIndexPopup.EnsureInitialized();

        if (shopButton != null)
        {
            shopButton.onClick.AddListener(delegate { Say("Shop: coming soon"); });
            UIEffectUtil.AttachButtonBounce(this, shopButton);
        }
        if (indexButton != null)
        {
            indexButton.onClick.AddListener(delegate { if (dinoIndexPopup != null) dinoIndexPopup.Toggle(); });
            UIEffectUtil.AttachButtonBounce(this, indexButton);
            CreateIndexAlertBadge();
        }

        if (rideSlot != null)
        {
            if (detachRideSlotFromHotbar && rideSlot.transform.parent != null && rideSlot.transform.parent.name == "Hotbar")
            {
                Transform hudRoot = rideSlot.transform.parent.parent;
                if (hudRoot != null)
                {
                    rideSlot.transform.SetParent(hudRoot, false);
                    RectTransform rt = rideSlot.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0f);
                        rt.anchorMax = new Vector2(0.5f, 0f);
                        rt.pivot = new Vector2(0.5f, 0f);
                        rt.anchoredPosition = standaloneRidePos;
                        rt.sizeDelta = standaloneRideSize;
                    }
                }
            }
            rideSlot.SetKey("Q");
            if (rideSlot.button != null)
            {
                rideSlot.button.onClick.AddListener(delegate { if (mount != null) mount.ToggleRide(); });
                UIEffectUtil.AttachButtonBounce(this, rideSlot.button);
            }
        }
        if (slots != null)
        {
            if (slots.Length > 0 && slots[0] != null && slots[0].transform.parent != null)
            {
                HorizontalLayoutGroup hlg = slots[0].transform.parent.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.spacing = hotbarSlotSpacing;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                int idx = i;
                slots[i].SetKey((i + 1).ToString());
                if (slots[i].button != null)
                {
                    slots[i].button.onClick.AddListener(delegate { SelectSlot(idx); });
                    UIEffectUtil.AttachButtonBounce(this, slots[i].button);
                }
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

        if (cheatStaminaButton != null)
        {
            cheatStaminaButton.gameObject.SetActive(showDevCheatButtons);
            if (showDevCheatButtons)
                cheatStaminaButton.onClick.AddListener(delegate { if (gm != null) gm.AddStamina(cheatStaminaAmount); });
        }
        else if (showDevCheatButtons)
        {
            CreateDevCheatButtons();
        }

        if (biomeHud == null) biomeHud = GetComponent<BiomeHudUI>();
        if (biomeHud == null) biomeHud = gameObject.AddComponent<BiomeHudUI>();

        if (tameSecuredUI == null) tameSecuredUI = GetComponentInChildren<TameSecuredUI>(true);
        if (tameSecuredUI == null) tameSecuredUI = FindObjectOfType<TameSecuredUI>(true);
        if (tameSecuredUI == null) tameSecuredUI = gameObject.AddComponent<TameSecuredUI>();

        if (sellDinosPopup == null) sellDinosPopup = FindObjectOfType<SellDinosPopup>(true);
        if (sellDinosPopup == null) sellDinosPopup = gameObject.AddComponent<SellDinosPopup>();

        if (FindObjectOfType<SellDinoShop>(true) == null)
        {
            GameObject shopGo = new GameObject("SellDinoShop");
            shopGo.transform.position = new Vector3(14.5f, -2.1f, -52f);
            shopGo.AddComponent<SellDinoShop>();
        }
    }

    void CreateDevCheatButtons()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        UnityEngine.Events.UnityAction[] actions = new UnityEngine.Events.UnityAction[6]
        {
            delegate { if (gm != null) gm.AddCash(cheatCashAmount); },
            delegate { if (gm != null) gm.AddStamina(cheatStaminaAmount); },
            delegate { if (gm != null) gm.DebugMountBySize(ObjectSize.Small); },
            delegate { if (gm != null) gm.DebugMountBySize(ObjectSize.Medium); },
            delegate { if (gm != null) gm.DebugMountBySize(ObjectSize.Large); },
            delegate { if (gm != null) gm.DebugMountBySize(ObjectSize.Giant); }
        };

        for (int i = 0; i < 6; i++)
        {
            GameObject go = new GameObject("CheatButton_F" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            spawnedCheatRects[i] = rt;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.14f, 0.78f);

            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI label = txtGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = 17f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = i < 2 ? new Color(1f, 0.9f, 0.25f) : new Color(0.55f, 0.95f, 1f);
            label.raycastTarget = false;
            spawnedCheatLabels[i] = label;

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(actions[i]);
            UIEffectUtil.AttachButtonBounce(this, btn);
        }

        RefreshCheatButtonsLayout();
    }

    void CreateIndexAlertBadge()
    {
        if (indexButton == null || indexAlertBadge != null) return;
        Transform existing = indexButton.transform.Find("IndexAlertBadge");
        if (existing != null) { indexAlertBadge = existing.gameObject; return; }

        GameObject go = new GameObject("IndexAlertBadge", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(indexButton.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-6f, -6f);
        rt.sizeDelta = new Vector2(28f, 28f);

        Image img = go.GetComponent<Image>();
        img.sprite = UIEffectUtil.GetRoundedRectSprite(32, 16);
        img.color = new Color(0.95f, 0.22f, 0.22f, 1f);
        img.raycastTarget = false;

        GameObject txtGo = new GameObject("Bang", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(rt, false);
        RectTransform trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "!";
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        indexAlertBadge = go;
        indexAlertBadge.SetActive(false);
    }

    void RefreshCheatButtonsLayout()
    {
        string[] texts = new string[6]
        {
            "+$" + NumberFormat.Short(cheatCashAmount) + " Cash (F1)",
            "+" + NumberFormat.Short(cheatStaminaAmount) + " Stamina (F2)",
            "Q: Small Dino (F3)",
            "Q: Medium Dino (F4)",
            "Q: Large Dino (F5)",
            "Q: Giant Dino (F6)"
        };

        for (int i = 0; i < 6; i++)
        {
            RectTransform rt = spawnedCheatRects[i];
            if (rt == null) continue;
            rt.gameObject.SetActive(showDevCheatButtons);
            Vector2 extraOffset = (cheatButtonOffsets != null && i < cheatButtonOffsets.Length) ? cheatButtonOffsets[i] : Vector2.zero;
            float yOffset = -i * (cheatButtonSize.y + cheatButtonSpacing);
            rt.anchoredPosition = cheatButtonPos + new Vector2(0f, yOffset) + extraOffset;
            rt.sizeDelta = cheatButtonSize;
            if (spawnedCheatLabels[i] != null)
                spawnedCheatLabels[i].text = texts[i];
        }
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

        bool showBaseSideButtons = player == null || !player.RunActive;
        if (shopButton != null && shopButton.gameObject.activeSelf != showBaseSideButtons)
            shopButton.gameObject.SetActive(showBaseSideButtons);
        if (indexButton != null && indexButton.gameObject.activeSelf != showBaseSideButtons)
            indexButton.gameObject.SetActive(showBaseSideButtons);
        if (indexAlertBadge != null && dinoIndexPopup != null)
        {
            bool showAlert = showBaseSideButtons && dinoIndexPopup.TotalUnclaimedCount > 0;
            if (indexAlertBadge.activeSelf != showAlert) indexAlertBadge.SetActive(showAlert);
        }

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

        RefreshCheatButtonsLayout();
    }
}
