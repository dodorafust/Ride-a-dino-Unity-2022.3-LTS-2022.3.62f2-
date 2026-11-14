using System;
using UnityEngine;

[Serializable]
public class MachineLevelConfig
{
    public GameObject model;               // model hiển thị ở level này
    public GameObject vfx;                 // VFX bật liên tục ở level này (tùy chọn)
    public ParticleSystem upgradeBurstVfx; // VFX nổ 1 lần ngay khi vừa nâng lên level này (tùy chọn)
    public float staminaPerSecond = 1f;    // lượng Stamina cộng mỗi giây ở level này
    public double upgradeCost = 100;       // giá tiền để nâng cấp LÊN level này (Lv 1 để 0)
}

// Thú nhún: đi lại chạm vào máy để nhảy lên ngồi, tự cộng Stamina mỗi nửa giây (số +Stamina bay lên), đung đưa trước-sau như lò xo.
// Đứng dậy: nút Jump hoặc di chuyển. Có nâng cấp giống chuồng (bảng UpgradeSign riêng).
public class DinoMachine : MonoBehaviour, IUpgradable
{
    [Header("Bộ phận")]
    public Transform seat;                     // chỗ ngồi (con của spring)
    public Transform spring;                   // phần đung đưa, pivot đặt ở chân (đáy) thú nhún
    public Transform exitPoint;                // chỗ đứng khi rời máy

    [Header("Stamina")]
    public float baseStaminaPerSecond = 1f;    // Stamina/s = base x level
    public float tickInterval = 0.5f;          // cộng + hiện số mỗi ngần này giây

    [Header("Đung đưa")]
    public float maxSwingAngle = 25f;          // độ
    public float swingFrequency = 1.4f;        // lần/giây
    public float rampUpTime = 0.8f;
    public float decayTime = 1.5f;             // dừng dần sau khi đứng dậy

    [Header("Cấu hình chi tiết từng Level (ưu tiên dùng nếu có phần tử)")]
    public MachineLevelConfig[] levels;

    [Header("Level (dự phòng khi chưa điền danh sách Levels ở trên)")]
    public int level = 1;
    public int maxLevel = 10;
    public double baseUpgradeCost = 100;
    public double upgradeCostMultiplier = 2.5;
    public LevelVisual[] levelVisuals;

    [Header("Khác")]
    public float interactRange = 8f;
    public Vector3 labelOffset = new Vector3(0f, 4.2f, 0f);

    bool HasLevelTable { get { return levels != null && levels.Length > 0; } }
    int EffectiveMaxLevel { get { return HasLevelTable ? levels.Length : maxLevel; } }

    public bool Occupied { get; private set; }
    public float StaminaPerSecond
    {
        get
        {
            if (HasLevelTable)
            {
                int idx = Mathf.Clamp(level - 1, 0, levels.Length - 1);
                return levels[idx] != null ? levels[idx].staminaPerSecond : baseStaminaPerSecond * level;
            }
            return baseStaminaPerSecond * level;
        }
    }
    public Vector3 ExitPosition
    {
        get { return exitPoint != null ? exitPoint.position : transform.position + new Vector3(0f, 1f, 3.5f); }
    }

    // ---- IUpgradable
    public string UpgradeName { get { return "MACHINE"; } }
    public int Level { get { return level; } }
    public bool IsMaxLevel { get { return level >= EffectiveMaxLevel; } }
    public double UpgradeCost
    {
        get
        {
            if (HasLevelTable)
            {
                if (level >= levels.Length) return 0;
                return levels[level] != null ? levels[level].upgradeCost : 0;
            }
            return baseUpgradeCost * Math.Pow(upgradeCostMultiplier, level - 1);
        }
    }
    public string NextLevelInfo
    {
        get
        {
            if (HasLevelTable && level < levels.Length && levels[level] != null)
            {
                float diff = levels[level].staminaPerSecond - StaminaPerSecond;
                return "+" + NumberFormat.Small(diff) + " Stamina/s (" + NumberFormat.Small(levels[level].staminaPerSecond) + "/s)";
            }
            return "+" + NumberFormat.Small(baseStaminaPerSecond) + " Stamina/s";
        }
    }
    public event Action Changed;

    float amp, tickTimer;
    Quaternion springRest = Quaternion.identity;
    WorldLabel label;

    void Start()
    {
        if (spring != null) springRest = spring.localRotation;
        if (GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
        ApplyCurrentLevelVisuals(false);
        RefreshLabel();
    }

    void ApplyCurrentLevelVisuals(bool playBurst)
    {
        LevelVisual.Apply(levelVisuals, level);
        if (!HasLevelTable) return;

        int activeIdx = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        MachineLevelConfig active = levels[activeIdx];
        GameObject activeModel = active != null ? active.model : null;
        GameObject activeVfx = active != null ? active.vfx : null;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null) continue;
            GameObject m = levels[i].model;
            if (m != null && m != activeModel && m != gameObject && !transform.IsChildOf(m.transform))
                m.SetActive(false);
            GameObject v = levels[i].vfx;
            if (v != null && v != activeVfx && v != gameObject && !transform.IsChildOf(v.transform))
                v.SetActive(false);
        }

        if (activeModel != null) activeModel.SetActive(true);
        if (activeVfx != null) activeVfx.SetActive(true);
        if (playBurst && active != null && active.upgradeBurstVfx != null)
            active.upgradeBurstVfx.Play();
    }

    void RefreshLabel()
    {
        if (label == null) return;
        label.SetText("DINO MACHINE  Lv " + level + "\n<color=#FFE633>+" + NumberFormat.Small(StaminaPerSecond) +
                      " Stamina/s</color>\nTouch to ride");
    }

    float nextEnterTime;

    public void SetOccupied(bool v)
    {
        if (Occupied && !v) nextEnterTime = Time.time + 0.8f;
        Occupied = v;
        tickTimer = 0f;
    }

    public bool TryTouchMount(PlayerController player)
    {
        if (Occupied || player == null || Time.time < nextEnterTime) return false;
        return player.SitOn(this);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null) TryTouchMount(player);
    }

    void Update()
    {
        // Biên độ tăng dần khi có người ngồi, giảm dần khi rời (như lò xo tắt dần)
        float target = Occupied ? 1f : 0f;
        float rate = Time.deltaTime / Mathf.Max(0.01f, Occupied ? rampUpTime : decayTime);
        amp = Mathf.MoveTowards(amp, target, rate);

        if (spring != null)
        {
            float angle = maxSwingAngle * amp * Mathf.Sin(Time.time * Mathf.PI * 2f * swingFrequency);
            spring.localRotation = springRest * Quaternion.Euler(angle, 0f, 0f);   // trước - sau
        }

        if (!Occupied) return;

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            float gain = StaminaPerSecond * tickInterval;
            if (GameManager.Instance == null) return;
            GameManager.Instance.AddStamina(gain);

            PlayerController p = PlayerController.Instance;
            Vector3 pos = (p != null ? p.transform.position : transform.position) + Vector3.up * 2.4f;
            GameManager.Instance.ShowPopup(pos, "+" + NumberFormat.Small(gain), PopupKind.Stamina);
        }
    }

    // Không còn dùng click để ngồi; kích hoạt khi cơ thể Player chạm vào máy (TryTouchMount / OnTriggerEnter)

    public void TryUpgrade()
    {
        if (IsMaxLevel) { HudController.Say("Machine is at max level"); return; }
        if (!GameManager.Instance.TrySpend(UpgradeCost))
        {
            HudController.Say("Not enough cash! Need $" + NumberFormat.Short(UpgradeCost));
            return;
        }
        level++;
        ApplyCurrentLevelVisuals(true);
        RefreshLabel();
        HudController.Say("Machine upgraded to Lv " + level + "  (+" + NumberFormat.Small(StaminaPerSecond) + " Stamina/s)");
        if (Changed != null) Changed();
    }
}
