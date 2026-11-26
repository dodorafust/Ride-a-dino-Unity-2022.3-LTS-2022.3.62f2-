using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Chuồng nuôi. Vùng chuồng = 1 Collider (Is Trigger) do bạn đặt/scale bằng tay. Đi xuyên qua được.
// Model hàng rào Blender: kéo vào "Level Visuals" (mỗi phần tử bật khi level >= From Level).
public class Pen : MonoBehaviour, IClickable, IUpgradable
{
    [Header("Vùng chuồng (Collider, Is Trigger)")]
    public Collider area;
    public Vector3 labelOffset = new Vector3(0f, 4.5f, 0f);
    public bool showFloatingLabel = true;

    [Header("Level / Slots")]
    public int level = 1;
    public int maxLevel = 9;
    public int baseSlots = 3;
    public int slotsPerLevel = 2;
    public double baseUpgradeCost = 100;
    public double upgradeCostMultiplier = 46.42;
    [Tooltip("Bảng giá nâng cấp từng cấp (tùy chọn, phần tử 0 = giá nâng Lv 1 -> 2). Nếu để trống hoặc 0 sẽ dùng Base Upgrade Cost * Multiplier^(level-1).")]
    public double[] upgradeCosts = new double[]
    {
        100,                // Lv 1 -> 2 ($100)
        5000,               // Lv 2 -> 3 ($5K)
        200000,             // Lv 3 -> 4 ($200K)
        10000000,           // Lv 4 -> 5 ($10M)
        500000000,          // Lv 5 -> 6 ($500M)
        25000000000,        // Lv 6 -> 7 ($25B)
        1000000000000,      // Lv 7 -> 8 ($1T)
        50000000000000      // Lv 8 -> 9 ($50T)
    };

    [Header("Skin theo level (model hàng rào)")]
    public LevelVisual[] levelVisuals;

    [Header("Mở rộng diện tích chuồng theo Level")]
    [Tooltip("Bật nếu muốn tự động mở rộng BoxCollider Area (và phóng to model hàng rào) mỗi khi nâng cấp (áp dụng khi tier không có Area riêng).")]
    public bool autoExpandAreaPerLevel = false;
    [Tooltip("Số mét cộng thêm vào chiều rộng (X) và chiều dài (Z) của Area ở mỗi cấp tăng thêm.")]
    public Vector2 areaExpandPerLevelXZ = new Vector2(2f, 2f);
    [Tooltip("Tự phóng to model hàng rào đang bật theo đúng tỉ lệ mở rộng X/Z của Area.")]
    public bool autoScaleVisualWithArea = true;

    [Header("Economy")]
    public bool autoDeposit = true;      // true: tiền vào ví realtime bất cứ đâu, không cần bấm Collect hay vào chuồng

    [Header("Vị trí chữ & tiền trên đầu Dino trong chuồng")]
    public float dinoLabelHeadPadding = 1.4f;
    public float dinoMoneyPopupHeadPadding = 0.35f;

    // ---- IUpgradable
    public string UpgradeName { get { return "PEN"; } }
    public int Level { get { return level; } }
    public bool IsMaxLevel { get { return level >= maxLevel; } }
    public double UpgradeCost
    {
        get
        {
            int idx = level - 1;
            if (upgradeCosts != null && idx >= 0 && idx < upgradeCosts.Length && upgradeCosts[idx] > 0)
                return upgradeCosts[idx];
            return baseUpgradeCost * Math.Pow(upgradeCostMultiplier, idx);
        }
    }
    public string NextLevelInfo { get { return "+" + slotsPerLevel + " slots"; } }
    public event Action Changed;

    public int Slots { get { return baseSlots + (level - 1) * slotsPerLevel; } }
    public bool PlayerInside { get; private set; }
    public double Pending { get; private set; }
    public double IncomePerSecond { get; private set; }
    public IReadOnlyList<OwnedDino> Active { get { return active; } }
    public Bounds AreaBounds
    {
        get
        {
            Collider c = activeArea != null ? activeArea : area;
            return c != null ? c.bounds : new Bounds(transform.position, new Vector3(16f, 1f, 10f));
        }
    }

    readonly List<OwnedDino> active = new List<OwnedDino>();
    readonly Dictionary<OwnedDino, PenDino> visuals = new Dictionary<OwnedDino, PenDino>();
    readonly Dictionary<GameObject, Vector3> baseVisualScales = new Dictionary<GameObject, Vector3>();

    Collider activeArea;
    Vector3 baseAreaSize;
    bool hasBaseAreaSize;

    LineRenderer ring;
    WorldLabel label;
    float labelTimer;

    void Awake()
    {
        if (area == null) area = GetComponentInChildren<Collider>();
        activeArea = area;
        if (area is BoxCollider box)
        {
            baseAreaSize = box.size;
            hasBaseAreaSize = true;
        }
        if (levelVisuals != null)
        {
            for (int i = 0; i < levelVisuals.Length; i++)
            {
                if (levelVisuals[i] != null && levelVisuals[i].root != null && !baseVisualScales.ContainsKey(levelVisuals[i].root))
                    baseVisualScales[levelVisuals[i].root] = levelVisuals[i].root.transform.localScale;
            }
        }

        var go = new GameObject("PenRing");
        go.transform.SetParent(transform, false);
        ring = go.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = 4;
        ring.startWidth = 0.3f;
        ring.endWidth = 0.3f;
        ring.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        ring.startColor = new Color(1f, 0.9f, 0.1f);
        ring.endColor = new Color(1f, 0.9f, 0.1f);
        ring.enabled = false;
    }

    void Start()
    {
        if (showFloatingLabel && GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
        ApplyLevelVisualsAndArea();
        RefreshLabel();
    }

    void ApplyLevelVisualsAndArea()
    {
        LevelVisual.Apply(levelVisuals, level);
        LevelVisual activeLv = LevelVisual.GetActive(levelVisuals, level);

        Collider tierArea = null;
        if (activeLv != null)
        {
            if (activeLv.areaOverride != null)
            {
                tierArea = activeLv.areaOverride;
            }
            else if (activeLv.root != null)
            {
                Transform areaChild = activeLv.root.transform.Find("Area");
                if (areaChild != null) tierArea = areaChild.GetComponent<Collider>();
            }
        }

        if (tierArea != null)
        {
            activeArea = tierArea;
        }
        else
        {
            activeArea = area;
            if (autoExpandAreaPerLevel && hasBaseAreaSize && area is BoxCollider box)
            {
                int extraLevels = Mathf.Max(0, level - 1);
                Vector3 targetSize = new Vector3(
                    baseAreaSize.x + areaExpandPerLevelXZ.x * extraLevels,
                    baseAreaSize.y,
                    baseAreaSize.z + areaExpandPerLevelXZ.y * extraLevels);
                box.size = targetSize;

                if (autoScaleVisualWithArea && activeLv != null && activeLv.root != null &&
                    baseVisualScales.TryGetValue(activeLv.root, out Vector3 baseScale))
                {
                    float scaleX = baseAreaSize.x > 0.001f ? targetSize.x / baseAreaSize.x : 1f;
                    float scaleZ = baseAreaSize.z > 0.001f ? targetSize.z / baseAreaSize.z : 1f;
                    activeLv.root.transform.localScale = new Vector3(
                        baseScale.x * scaleX,
                        baseScale.y,
                        baseScale.z * scaleZ);
                }
            }
        }

        Physics.SyncTransforms();
        RefreshPenRing();
    }

    void RefreshPenRing()
    {
        if (ring == null) return;
        Bounds b = AreaBounds;
        float y = b.min.y + 0.1f;
        float m = 0.3f;
        ring.SetPosition(0, new Vector3(b.min.x + m, y, b.min.z + m));
        ring.SetPosition(1, new Vector3(b.min.x + m, y, b.max.z - m));
        ring.SetPosition(2, new Vector3(b.max.x - m, y, b.max.z - m));
        ring.SetPosition(3, new Vector3(b.max.x - m, y, b.min.z + m));
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UpgradeSign[] signs = FindObjectsOfType<UpgradeSign>(true);
        for (int i = 0; i < signs.Length; i++)
            if (signs[i] != null && signs[i].target == this) signs[i].RefreshPreviewInEditor();
    }
#endif

    void Update()
    {
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            Bounds b = AreaBounds;
            Vector3 pos = p.transform.position;
            bool inside = pos.x > b.min.x && pos.x < b.max.x &&
                          pos.z > b.min.z && pos.z < b.max.z;
            if (inside != PlayerInside)
            {
                PlayerInside = inside;
                ring.enabled = inside;
                if (inside) Collect(true);
            }
        }

        if (IncomePerSecond > 0.0)
        {
            double earned = IncomePerSecond * Time.deltaTime;
            if (autoDeposit && GameManager.Instance != null)
            {
                if (Pending > 0.0)
                {
                    earned += Pending;
                    Pending = 0.0;
                }
                GameManager.Instance.AddCash(earned);
            }
            else
            {
                Pending += earned;
            }
        }

        labelTimer += Time.deltaTime;
        if (labelTimer >= 0.25f)
        {
            labelTimer = 0f;
            if (autoDeposit) Collect(false);
            RefreshLabel();
        }
    }

    void RefreshLabel()
    {
        if (label == null) return;
        string incomeLine = "<color=#55FF55>+$" + NumberFormat.Short(IncomePerSecond) + "/s</color>";
        if (!autoDeposit) incomeLine += "   Stored: $" + NumberFormat.Short(Pending);
        label.SetText("PEN  Lv " + level + "\n" + active.Count + "/" + Slots + " slots\n" + incomeLine);
    }

    void RecalcIncome()
    {
        double sum = 0.0;
        for (int i = 0; i < active.Count; i++) sum += active[i].MoneyPerSecond;
        IncomePerSecond = sum;
    }

    void Fire()
    {
        if (Changed != null) Changed();
        RefreshLabel();
    }

    // ---------- Thu tiền ----------
    public void Collect(bool toast)
    {
        if (Pending < 1.0 || GameManager.Instance == null) return;
        GameManager.Instance.AddCash(Pending);
        if (toast) HudController.Say("Collected $" + NumberFormat.Short(Pending));
        Pending = 0.0;
    }

    // ---------- Chạm sàn chuồng để đặt Dino đang cầm ----------
    public void OnClicked(PlayerController player)
    {
        if (!PlayerInside) { HudController.Say("Walk into the pen first"); return; }

        Inventory inv = GameManager.Instance.Inventory;
        OwnedDino held = inv.Held;
        if (held == null) { HudController.Say("Pick a dino from the hotbar first"); return; }
        if (active.Count >= Slots) { HudController.Say("Pen is full! Upgrade it at the sign"); return; }

        if (DepositInternal(held))
        {
            HudController.Say("Placed " + held.Title + " in the pen");
            List<OwnedDino> next = inv.GetBest(1);
            if (next.Count > 0) inv.SetHeld(next[0]);   // tự cầm con kế tiếp để đặt liên tục
            Fire();
        }
    }

    bool DepositInternal(OwnedDino d)
    {
        if (active.Count >= Slots) return false;
        if (!GameManager.Instance.Inventory.Remove(d)) return false;
        active.Add(d);

        var go = new GameObject("PenDino_" + d.Title);
        go.transform.SetParent(transform, true);
        PenDino pd = go.AddComponent<PenDino>();
        pd.labelHeadPadding = dinoLabelHeadPadding;
        pd.moneyPopupHeadPadding = dinoMoneyPopupHeadPadding;
        pd.Init(d, this);
        visuals[d] = pd;

        RecalcIncome();
        return true;
    }

    bool WithdrawInternal(OwnedDino d)
    {
        if (!active.Remove(d)) return false;
        PenDino pd;
        if (visuals.TryGetValue(d, out pd))
        {
            if (pd != null) Destroy(pd.gameObject);
            visuals.Remove(d);
        }
        GameManager.Instance.Inventory.Add(d, false);
        RecalcIncome();
        return true;
    }

    public void Withdraw(OwnedDino d)
    {
        if (WithdrawInternal(d)) Fire();
    }

    public void EquipBest()
    {
        Inventory inv = GameManager.Instance.Inventory;
        bool changed = false;
        while (true)
        {
            OwnedDino bestInv = inv.GetBest(1).FirstOrDefault();
            if (bestInv == null) break;

            if (active.Count < Slots)
            {
                if (!DepositInternal(bestInv)) break;
                changed = true;
                continue;
            }

            OwnedDino worst = active.OrderBy(x => x.MoneyPerSecond).First();
            if (bestInv.MoneyPerSecond > worst.MoneyPerSecond)
            {
                WithdrawInternal(worst);
                DepositInternal(bestInv);
                changed = true;
                continue;
            }
            break;
        }
        if (changed) Fire();
    }

    // ---------- Cho MountController ----------
    public bool Contains(OwnedDino d) { return d != null && active.Contains(d); }

    public bool TryGetVisual(OwnedDino d, out PenDino pd)
    {
        pd = null;
        return d != null && visuals.TryGetValue(d, out pd) && pd != null;
    }

    public void SetRidden(OwnedDino d, bool ridden, Vector3? walkBackFromPos = null, Quaternion? startRot = null)
    {
        PenDino pd;
        if (TryGetVisual(d, out pd)) pd.SetRidden(ridden, walkBackFromPos, startRot);
    }

    // Dino trong chuồng gần vị trí pos nhất (bỏ qua con đang được cưỡi)
    public OwnedDino GetNearest(Vector3 pos, float range, OwnedDino exclude)
    {
        OwnedDino best = null;
        float bestDist = range;
        foreach (KeyValuePair<OwnedDino, PenDino> kv in visuals)
        {
            if (kv.Value == null || kv.Value.IsRidden || kv.Key == exclude) continue;
            float d = Vector3.Distance(pos, kv.Value.transform.position);
            if (d < bestDist) { bestDist = d; best = kv.Key; }
        }
        return best;
    }

    // ---------- Nâng cấp ----------
    public void TryUpgrade()
    {
        if (IsMaxLevel) { HudController.Say("Pen is at max level"); return; }
        if (!GameManager.Instance.TrySpend(UpgradeCost))
        {
            HudController.Say("Not enough cash! Need $" + NumberFormat.Short(UpgradeCost));
            return;
        }
        level++;
        ApplyLevelVisualsAndArea();
        HudController.Say("Pen upgraded to Lv " + level + "  (" + Slots + " slots)");
        Fire();
    }

    [ContextMenu("Apply Balanced Costs (9 Pens, Lv7->8 = $1T)")]
    void ApplyBalancedNineLevelCosts()
    {
        maxLevel = 9;
        baseUpgradeCost = 100;
        upgradeCostMultiplier = 46.42;
        upgradeCosts = new double[]
        {
            100,
            5000,
            200000,
            10000000,
            500000000,
            25000000000,
            1000000000000,
            50000000000000
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        OnValidate();
#endif
    }
}
