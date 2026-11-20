using UnityEngine;

public enum RideEvent { None, TameCompleted, StaminaEmptyDead, StaminaEmptyThrow, StaminaEmptyCarnivoreKill }

// Quản lý 1 lượt cưỡi: vòng Stamina (xanh, giảm từng nấc) và vòng Tame (đầy dần).
public class RideSession : MonoBehaviour
{
    [Header("Stamina (giảm từng nấc)")]
    public float staminaTickInterval = 0.35f;
    public float gripScale = 1f;
    public float minGripTime = 1.5f;

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
    public float Stamina01 { get; private set; }   // 1 -> 0
    public float Tame01 { get; private set; }      // 0 -> 1
    public float GripDuration { get; private set; }
    public float TameDuration { get; private set; }
    public Dino Dino { get; private set; }

    public float SmoothStamina01
    {
        get
        {
            if (!Active) return 0f;
            return Mathf.Clamp01(Stamina01 - tickTimer / Mathf.Max(0.01f, GripDuration));
        }
    }
    public bool IsLowStamina { get { return Active && SmoothStamina01 <= lowStaminaThreshold; } }
    public float LowStamina01
    {
        get
        {
            if (!IsLowStamina) return 0f;
            return Mathf.Clamp01(1f - SmoothStamina01 / Mathf.Max(0.01f, lowStaminaThreshold));
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

        // Đủ Stamina yêu cầu -> chắc chắn tame xong và còn dư ít nhất postTameExtraTime giây để xử lý nhảy sang con khác.
        // Thiếu Stamina -> tụt nhanh ở đoạn đầu trước khi chạm đỏ, chạm đỏ vẫn chậm lại nhưng tổng thời gian bị giới hạn <= untamableMaxGripRatio * TameDuration.
        float reqStamina = (d != null && d.Owned != null) ? d.Owned.EffectiveRequiredStamina : (d != null && d.Data != null ? d.Data.requiredStamina : 10f);
        float ratio = playerStamina / Mathf.Max(1f, reqStamina);
        CanTame = IsPetOwned || ratio >= 1f;
        float grip;
        if (!CanTame)
        {
            float fastMul = Mathf.Max(1f, untamableDrainMultiplier);
            float slowMul = Mathf.Clamp(lowStaminaDrainScale, 0.05f, 1f);
            float actualFactor = (1f - lowStaminaThreshold) / fastMul + lowStaminaThreshold / slowMul;
            float targetActualTime = Mathf.Min(TameDuration * ratio, TameDuration * untamableMaxGripRatio);
            grip = targetActualTime / Mathf.Max(0.2f, actualFactor);
            GripDuration = Mathf.Max(0.4f, grip * gripScale);
        }
        else
        {
            grip = Mathf.Max(TameDuration * ratio, TameDuration + postTameExtraTime);
            GripDuration = Mathf.Max(minGripTime, grip * gripScale);
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
        elapsed += dt;

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
            return canKill ? RideEvent.StaminaEmptyDead : RideEvent.StaminaEmptyThrow;
        }

        return RideEvent.None;
    }
}
