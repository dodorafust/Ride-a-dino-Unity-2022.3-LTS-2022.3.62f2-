using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum AuraBuffType
{
    StaminaMultiplier = 0,
    CashMultiplier = 1,
    WalkSpeedMultiplier = 2,
    RideSpeedMultiplier = 3,
    TameSpeedMultiplier = 4,
    JumpHeightMultiplier = 5
}

public enum AuraStationaryMode
{
    UseGlobalSetting = 0,      // Theo cài đặt chung trên PlayerAuraController
    OnlyWhenStationary = 1,    // Chỉ hiện VFX khi đứng yên, di chuyển tự tắt
    AlwaysVisible = 2          // Luôn hiện VFX cả khi chạy/nhảy
}

[Serializable]
public class AuraBuffEntry
{
    [Tooltip("Loại chỉ số muốn buff thêm (ví dụ CashMultiplier, WalkSpeedMultiplier, RideSpeedMultiplier...).")]
    public AuraBuffType buffType = AuraBuffType.CashMultiplier;
    [Tooltip("Hệ số nhân của buff này (ví dụ 1.25 = x1.25).")]
    [Min(0.1f)] public float multiplier = 1.25f;
    [Tooltip("Chữ hiển thị tùy chỉnh cho buff này (để trống sẽ tự tạo theo loại buff).")]
    public string customLabel = "";
}

[CreateAssetMenu(menuName = "RideADino/Aura Data", fileName = "NewAura")]
public class AuraData : ScriptableObject
{
    [Header("1. Thông tin cơ bản")]
    public string auraId = "silver_aura";
    public string displayName = "Silver Aura";
    [Min(1)] public int level = 1;
    public Rarity rarity = Rarity.Common;
    [Tooltip("Giá mua bằng tiền Cash trong game (0 = miễn phí / có thể mở khóa sẵn).")]
    [Min(0)] public double priceCash = 0;
    [Tooltip("Bật: Người chơi sở hữu sẵn Aura này ngay từ đầu (như Silver Aura Lvl 1).")]
    public bool unlockedByDefault = false;

    [Header("2. Giao diện Thẻ trong Shop Auras")]
    [Tooltip("Ảnh minh họa trong khung vuông giữa thẻ (để trống sẽ tự vẽ hình nhân vật bọc hào quang theo Aura Color).")]
    public Sprite icon;
    [Tooltip("Màu chủ đạo của hào quang (dùng cho cả thẻ UI và nhuộm màu VFX).")]
    public Color auraColor = new Color(0.84f, 0.87f, 0.94f, 1f);
    public Color cardTopColor = new Color(0.88f, 0.90f, 0.96f, 1f);
    public Color cardBottomColor = new Color(0.56f, 0.60f, 0.68f, 1f);

    [Header("3. Chỉ số Buff (Mở rộng dễ dàng cho tương lai)")]
    [Tooltip("Hệ số nhân Stamina chính của Aura này (ví dụ 1.5 = x1.5 Stamina, 2 = x2 Stamina).")]
    [Min(0.1f)] public float staminaMultiplier = 1.5f;
    [Tooltip("Chữ hiển thị buff tùy chỉnh trên thẻ (để trống sẽ tự tạo chữ 'x1.5 Stamina' cùng các Extra Buffs).")]
    public string customBuffDisplayText = "";
    [Tooltip("Danh sách các Buff mở rộng (bấm dấu + để thêm buff Tiền, Tốc độ chạy, Tốc độ cưỡi Dino, Tốc độ Tame, Độ cao nhảy...).")]
    public List<AuraBuffEntry> extraBuffs = new List<AuraBuffEntry>();

    [Header("4. VFX Hào quang gắn lên cơ thể Player")]
    [Tooltip("Kéo Prefab VFX riêng của Aura này vào đây. Để trống sẽ dùng VFX mặc định đổi màu theo Aura Color.")]
    public GameObject vfxPrefab;
    [Tooltip("Độ lệch vị trí VFX so với điểm gắn AuraVfxAnchor trên người chơi.")]
    public Vector3 vfxPositionOffset = Vector3.zero;
    [Tooltip("Góc xoay bù của VFX.")]
    public Vector3 vfxRotationOffset = Vector3.zero;
    [Tooltip("Tỉ lệ phóng to/thu nhỏ VFX của riêng Aura này.")]
    public Vector3 vfxScale = Vector3.one;
    [Tooltip("Quy tắc ẩn/hiện VFX khi đứng yên hoặc di chuyển.")]
    public AuraStationaryMode stationaryVisibilityMode = AuraStationaryMode.UseGlobalSetting;

    public float GetTotalMultiplier(AuraBuffType type)
    {
        float mul = (type == AuraBuffType.StaminaMultiplier) ? Mathf.Max(0.1f, staminaMultiplier) : 1f;
        if (extraBuffs != null)
        {
            for (int i = 0; i < extraBuffs.Count; i++)
            {
                AuraBuffEntry e = extraBuffs[i];
                if (e != null && e.buffType == type && e.multiplier > 0.01f)
                    mul *= e.multiplier;
            }
        }
        return mul;
    }

    public string BuildBuffDisplayText()
    {
        if (!string.IsNullOrEmpty(customBuffDisplayText))
            return customBuffDisplayText;

        StringBuilder sb = new StringBuilder();
        float stam = GetTotalMultiplier(AuraBuffType.StaminaMultiplier);
        sb.Append("<color=#55FF55>x").Append(FormatMultiplier(stam)).Append("</color> Stamina");

        if (extraBuffs != null)
        {
            for (int i = 0; i < extraBuffs.Count; i++)
            {
                AuraBuffEntry e = extraBuffs[i];
                if (e == null || e.buffType == AuraBuffType.StaminaMultiplier || Mathf.Approximately(e.multiplier, 1f))
                    continue;
                sb.Append("\n");
                if (!string.IsNullOrEmpty(e.customLabel))
                {
                    sb.Append(e.customLabel);
                }
                else
                {
                    sb.Append("<color=#55FF55>x").Append(FormatMultiplier(e.multiplier)).Append("</color> ").Append(GetBuffLabel(e.buffType));
                }
            }
        }
        return sb.ToString();
    }

    public static string FormatMultiplier(float m)
    {
        if (Mathf.Approximately(m, Mathf.Round(m)))
            return Mathf.RoundToInt(m).ToString();
        return m.ToString("0.##");
    }

    public static string GetBuffLabel(AuraBuffType type)
    {
        switch (type)
        {
            case AuraBuffType.StaminaMultiplier: return "Stamina";
            case AuraBuffType.CashMultiplier: return "Cash";
            case AuraBuffType.WalkSpeedMultiplier: return "Speed";
            case AuraBuffType.RideSpeedMultiplier: return "Ride Speed";
            case AuraBuffType.TameSpeedMultiplier: return "Tame Speed";
            case AuraBuffType.JumpHeightMultiplier: return "Jump";
            default: return type.ToString();
        }
    }
}
