using UnityEngine;

public enum RideEvent { None, TameCompleted, StaminaEmptyDead, StaminaEmptyThrow }

// Quản lý 1 lượt cưỡi: vòng Stamina (xanh, giảm từng nấc) và vòng Tame (đầy dần).
public class RideSession : MonoBehaviour
{
    [Header("Stamina (giảm từng nấc)")]
    public float staminaTickInterval = 0.35f;
    public float gripScale = 1f;
    public float minGripTime = 1.5f;

    public bool Active { get; private set; }
    public bool Tamed { get; private set; }
    public float Stamina01 { get; private set; }   // 1 -> 0
    public float Tame01 { get; private set; }      // 0 -> 1
    public float GripDuration { get; private set; }
    public float TameDuration { get; private set; }
    public Dino Dino { get; private set; }

    float tickTimer, elapsed;

    public void Begin(Dino d, float playerStamina)
    {
        Dino = d;
        Active = true;
        Tamed = false;
        Stamina01 = 1f;
        Tame01 = 0f;
        tickTimer = 0f;
        elapsed = 0f;

        TameDuration = Mathf.Max(0.5f, d.Data.tameTime);

        // Đủ Stamina yêu cầu -> bám lâu hơn thời gian tame. Thiếu -> bám ngắn hơn (chắc chắn thua).
        float ratio = playerStamina / Mathf.Max(1f, d.Data.requiredStamina);
        float grip = TameDuration * ratio;
        if (ratio < 1f) grip = Mathf.Min(grip, TameDuration * 0.9f);
        else grip += 0.1f;
        GripDuration = Mathf.Max(minGripTime, grip * gripScale);
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

        tickTimer += dt;
        if (tickTimer >= staminaTickInterval)
        {
            tickTimer -= staminaTickInterval;
            Stamina01 = Mathf.Max(0f, Stamina01 - staminaTickInterval / GripDuration);
        }

        if (Stamina01 <= 0.001f)
            return Tamed ? RideEvent.StaminaEmptyThrow : RideEvent.StaminaEmptyDead;

        return RideEvent.None;
    }
}
