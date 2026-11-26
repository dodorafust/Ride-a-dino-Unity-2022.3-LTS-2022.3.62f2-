using UnityEngine;

public enum RideEvent { None, TameCompleted, StaminaEmptyDead, StaminaEmptyThrow, StaminaEmptyCarnivoreKill }

// Quản lý 1 lượt cưỡi: vòng Stamina (xanh, giảm từng nấc) và vòng Tame (đầy dần).
public class RideSession : MonoBehaviour
{
    [Header("Stamina (giảm từng nấc & Multi-Ring)")]
    public float staminaTickInterval = 0.35f;
    public float gripScale = 1f;
    public float minGripTime = 1.5f;
    [Tooltip("Số vòng Stamina tối đa (1 vòng chính + tối đa 2 vòng ngoài = 3).")]
    [Range(1, 5)] public int maxStaminaRings = 3;
    [Tooltip("Thời gian chuẩn (giây) cho 1 vòng Stamina ở bậc Common.")]
    [Min(2f)] public float secondsPerRing = 10f;
    [Tooltip("Hệ số logarit để mọc thêm vòng ngoài khi dư Stamina (3 = dư 3x mọc vòng 2, dư 9x mọc vòng 3).")]
    [Min(1.5f)] public float extraRingLogBase = 3f;

    [Header("Hệ số tụt nhanh hơn theo Rarity (áp dụng cho cả Dino ngoài đường & trong Pen)")]
    public float commonDrainMultiplier = 1.0f;
    public float uncommonDrainMultiplier = 1.0f;
    public float rareDrainMultiplier = 1.05f;
    public float epicDrainMultiplier = 1.15f;
    public float legendaryDrainMultiplier = 1.30f;
    public float mythicDrainMultiplier = 1.45f;
    public float secretDrainMultiplier = 1.65f;
    public float eternalDrainMultiplier = 1.85f;
    public float divineDrainMultiplier = 2.10f;

    [Header("Sắp hết Stamina (Cảnh báo & Bảo hiểm)")]
    [Range(0.05f, 0.8f)] public float lowStaminaThreshold = 0.30f;
    [Tooltip("Hệ số tốc độ tụt Stamina khi vào vùng đỏ lúc CHƯA tame xong (< 1 = tụt chậm lại để kịp xử lý).")]
    [Range(0.05f, 1f)] public float lowStaminaDrainScale = 0.40f;
    [Tooltip("Hệ số tốc độ tụt Stamina khi vào vùng đỏ lúc ĐÃ tame xong (lớn hơn Low Stamina Drain Scale để phần bảo hiểm ngắn hạn hơn).")]
    [Range(0.05f, 1f)] public float tamedLowStaminaDrainScale = 0.70f;
    [Tooltip("Thời gian dư tối thiểu (giây) sau khi tame xong đối với Dino đủ Stamina để người chơi kịp bay sang con khác.")]
    [Min(0f)] public float postTameExtraTime = 2.0f;

    [Header("Thiếu Stamina (Chắc chắn không bắt được)")]
    [Tooltip("Hệ số nhân tốc độ tụt Stamina khi nhảy lên con chắc chắn không bắt được (ép nhảy sang con mới sớm).")]
    [Min(1f)] public float untamableDrainMultiplier = 1.8f;
    [Tooltip("Giới hạn thời gian bám tối đa theo tỉ lệ Tame Time khi thiếu Stamina.")]
    [Range(0.1f, 0.9f)] public float untamableMaxGripRatio = 0.60f;

    public bool Active { get; private set; }
    public bool Tamed { get; private set; }
    public bool CanTame { get; private set; }
    public bool IsPetOwned { get; private set; }
    public float Stamina01 { get; private set; }   // 1 -> 0 (tổng cả các vòng)
    public float Tame01 { get; private set; }      // 0 -> 1
    public float GripDuration { get; private set; }
    public float TameDuration { get; private set; }
    public float TotalRings { get; private set; }
    public Dino Dino { get; private set; }

    public float SmoothStamina01
    {
        get
        {
            if (!Active) return 0f;
            return Mathf.Clamp01(Stamina01 - tickTimer / Mathf.Max(0.01f, GripDuration));
        }
    }
    public float RingsRemaining
    {
        get { return Active ? Stamina01 * Mathf.Max(1f, TotalRings) : 0f; }
    }
    public float SmoothRingsRemaining
    {
        get { return Active ? SmoothStamina01 * Mathf.Max(1f, TotalRings) : 0f; }
    }
    public bool IsLowStamina { get { return Active && SmoothRingsRemaining <= lowStaminaThreshold; } }
    public float LowStamina01
    {
        get
        {
            if (!IsLowStamina) return 0f;
            return Mathf.Clamp01(1f - SmoothRingsRemaining / Mathf.Max(0.01f, lowStaminaThreshold));
        }
    }

    float tickTimer, elapsed;

    public void Begin(Dino d, float playerStamina)
    {
        Dino = d;
        Active = true;
        IsPetOwned = d != null && d.IsPetOwned;
        Tamed = IsPetOwned;
        Stamina01 = 1f;
        Tame01 = IsPetOwned ? 1f : 0f;
        tickTimer = 0f;
        elapsed = 0f;

        TameDuration = Mathf.Max(0.5f, d.Data.tameTime);

        // Đủ Stamina yêu cầu (hoặc Dino lấy từ Pen) -> số vòng tính theo logarit + trần maxStaminaRings (tối đa 3 vòng),
        // tốc độ tụt theo bậc Rarity (bậc cao tụt nhanh hơn).
        // Thiếu Stamina -> chỉ 1 vòng, tụt nhanh ở đoạn đầu trước khi chạm đỏ.
        float reqStamina = (d != null && d.Owned != null) ? d.Owned.EffectiveRequiredStamina : (d != null && d.Data != null ? d.Data.requiredStamina : 10f);
        float ratio = playerStamina / Mathf.Max(1f, reqStamina);
        CanTame = IsPetOwned || ratio >= 1f;
        float rarityMul = GetRarityDrainMultiplier(d);

        if (!CanTame)
        {
            TotalRings = 1f;
            float fastMul = Mathf.Max(1f, untamableDrainMultiplier) * rarityMul;
            float slowMul = Mathf.Clamp(lowStaminaDrainScale, 0.05f, 1f);
            float actualFactor = (1f - lowStaminaThreshold) / fastMul + lowStaminaThreshold / slowMul;
            float targetActualTime = Mathf.Min(TameDuration * ratio, TameDuration * untamableMaxGripRatio);
            float grip = targetActualTime / Mathf.Max(0.2f, actualFactor);
            GripDuration = Mathf.Max(0.4f, grip * gripScale);
        }
        else
        {
            float bonusRings = ratio > 1f ? Mathf.Log(ratio, Mathf.Max(1.5f, extraRingLogBase)) : 0f;
            TotalRings = Mathf.Clamp(1f + bonusRings, 1f, Mathf.Max(1, maxStaminaRings));

            float secPerRing = Mathf.Max(1f, secondsPerRing) / Mathf.Max(0.1f, rarityMul);
            float grip = TotalRings * secPerRing;
            float minSafe = TameDuration + postTameExtraTime;
            if (grip < minSafe)
            {
                grip = minSafe;
                TotalRings = Mathf.Clamp(grip / secPerRing, 1f, Mathf.Max(1, maxStaminaRings));
            }
            GripDuration = Mathf.Max(minGripTime, grip * gripScale);
        }
    }

    float GetRarityDrainMultiplier(Dino d)
    {
        if (d == null || d.Data == null) return 1f;
        switch (d.Data.rarity)
        {
            case Rarity.Common: return commonDrainMultiplier;
            case Rarity.Uncommon: return uncommonDrainMultiplier;
            case Rarity.Rare: return rareDrainMultiplier;
            case Rarity.Epic: return epicDrainMultiplier;
            case Rarity.Legendary: return legendaryDrainMultiplier;
            case Rarity.Mythic: return mythicDrainMultiplier;
            case Rarity.Secret: return secretDrainMultiplier;
            case Rarity.Eternal: return eternalDrainMultiplier;
            case Rarity.Divine: return divineDrainMultiplier;
            default: return 1f;
        }
    }

    public void End()
    {
        Active = false;
        Dino = null;
    }

    public RideEvent Tick(float dt)
    {
        if (!Active) return RideEvent.None;
        float tameMul = GameManager.Instance != null ? GameManager.Instance.GetAuraBuffMultiplier(AuraBuffType.TameSpeedMultiplier) : 1f;
        elapsed += dt * tameMul;

        if (!Tamed)
        {
            Tame01 = Mathf.Clamp01(elapsed / TameDuration);
            if (Tame01 >= 1f)
            {
                Tamed = true;
                return RideEvent.TameCompleted;
            }
        }

        float speedScale;
        if (IsLowStamina)
        {
            speedScale = Tamed ? Mathf.Clamp(tamedLowStaminaDrainScale, 0.05f, 1f)
                               : Mathf.Clamp(lowStaminaDrainScale, 0.05f, 1f);
        }
        else
        {
            speedScale = CanTame ? 1f : Mathf.Max(1f, untamableDrainMultiplier);
        }
        float drainDt = dt * speedScale;
        tickTimer += drainDt;
        if (tickTimer >= staminaTickInterval)
        {
            tickTimer -= staminaTickInterval;
            Stamina01 = Mathf.Max(0f, Stamina01 - staminaTickInterval / GripDuration);
        }

        if (Stamina01 <= 0.001f)
        {
            bool isCarnivore = Dino != null && Dino.Data != null && Dino.Data.diet == DinoDiet.Carnivore;
            if (isCarnivore) return RideEvent.StaminaEmptyCarnivoreKill;
            bool canKill = !Tamed && Dino != null && Dino.Data != null && Dino.Data.killOnStaminaEmptyIfUntamed;
            return canKill ? RideEvent.StaminaEmptyCarnivoreKill : RideEvent.StaminaEmptyThrow;
        }

        return RideEvent.None;
    }
}
