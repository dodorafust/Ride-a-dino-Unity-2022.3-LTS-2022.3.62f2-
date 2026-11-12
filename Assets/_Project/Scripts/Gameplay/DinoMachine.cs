using System;
using UnityEngine;

// Thú nhún: chạm để ngồi, tự cộng Stamina mỗi nửa giây (số +Stamina bay lên), đung đưa trước-sau như lò xo.
// Đứng dậy: nút Jump hoặc di chuyển. Có nâng cấp giống chuồng (bảng UpgradeSign riêng).
public class DinoMachine : MonoBehaviour, IClickable, IUpgradable
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

    [Header("Level")]
    public int level = 1;
    public int maxLevel = 10;
    public double baseUpgradeCost = 100;
    public double upgradeCostMultiplier = 2.5;
    public LevelVisual[] levelVisuals;

    [Header("Khác")]
    public float interactRange = 8f;
    public Vector3 labelOffset = new Vector3(0f, 4.2f, 0f);

    public bool Occupied { get; private set; }
    public float StaminaPerSecond { get { return baseStaminaPerSecond * level; } }
    public Vector3 ExitPosition
    {
        get { return exitPoint != null ? exitPoint.position : transform.position + new Vector3(0f, 1f, 3.5f); }
    }

    // ---- IUpgradable
    public string UpgradeName { get { return "MACHINE"; } }
    public int Level { get { return level; } }
    public bool IsMaxLevel { get { return level >= maxLevel; } }
    public double UpgradeCost { get { return baseUpgradeCost * Math.Pow(upgradeCostMultiplier, level - 1); } }
    public string NextLevelInfo { get { return "+" + NumberFormat.Small(baseStaminaPerSecond) + " Stamina/s"; } }
    public event Action Changed;

    float amp, tickTimer;
    Quaternion springRest = Quaternion.identity;
    WorldLabel label;

    void Start()
    {
        if (spring != null) springRest = spring.localRotation;
        if (GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
        LevelVisual.Apply(levelVisuals, level);
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (label == null) return;
        label.SetText("DINO MACHINE  Lv " + level + "\n<color=#FFE633>+" + NumberFormat.Small(StaminaPerSecond) +
                      " Stamina/s</color>\nTap to sit");
    }

    public void SetOccupied(bool v)
    {
        Occupied = v;
        tickTimer = 0f;
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

    public void OnClicked(PlayerController player)
    {
        if (Occupied) return;
        if (Vector3.Distance(player.transform.position, transform.position) > interactRange)
        {
            HudController.Say("Get closer to the machine");
            return;
        }
        if (player.SitOn(this)) SetOccupied(true);
    }

    public void TryUpgrade()
    {
        if (IsMaxLevel) { HudController.Say("Machine is at max level"); return; }
        if (!GameManager.Instance.TrySpend(UpgradeCost))
        {
            HudController.Say("Not enough cash! Need $" + NumberFormat.Short(UpgradeCost));
            return;
        }
        level++;
        LevelVisual.Apply(levelVisuals, level);
        RefreshLabel();
        HudController.Say("Machine upgraded to Lv " + level + "  (+" + NumberFormat.Small(StaminaPerSecond) + " Stamina/s)");
        if (Changed != null) Changed();
    }
}
