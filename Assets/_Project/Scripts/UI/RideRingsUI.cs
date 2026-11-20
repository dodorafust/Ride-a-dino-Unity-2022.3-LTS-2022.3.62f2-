using UnityEngine;
using UnityEngine.UI;

// 2 vòng (Stamina xanh, Tame hồng) hiện cạnh nhân vật khi cưỡi Dino hoang.
// Gắn lên object gốc "RideRings"; các vòng là con của nó, bạn tự dời vị trí / đổi hình.
// Image Fill phải để Image Type = Filled, Radial 360 (builder đã đặt sẵn).
public class RideRingsUI : MonoBehaviour
{
    public Image staminaFill;
    public Image tameFill;
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    public float bounceFollowMultiplier = 1.6f; // độ rung lên xuống theo nhịp nhún cơ thể người chơi
    public float swayFollowMultiplier = 2.0f;   // độ rung lắc ngang trái/phải theo nhịp bước chân
    public float springStiffness = 220f;        // độ cứng lò xo tạo gia tốc rung mượt (cao = bám nhanh, thấp = có độ trễ/văng)
    public float springDamping = 18f;           // độ cản lò xo (thấp hơn = nảy thêm nhẹ)

    Vector2 springOffset;
    Vector2 springVel;

    public Color staminaHigh = new Color(0.25f, 0.85f, 0.3f);
    public Color staminaMid = new Color(1f, 0.85f, 0.1f);
    public Color staminaLow = new Color(0.9f, 0.2f, 0.25f);
    [Header("Vệt trắng phần Stamina vừa tụt")]
    public Image staminaTrailFill;
    public Color staminaTrailColor = Color.white;
    [Min(0f)] public float trailHoldTime = 0.08f;
    [Min(0.05f)] public float trailShrinkSpeed = 0.75f;
    public Color tameColor = new Color(1f, 0.4f, 0.65f);
    public Color tameDoneColor = new Color(1f, 0.85f, 0.1f);
    public float tameDoneFadeDelay = 0.2f;
    public float tameDoneFadeDuration = 0.45f;

    CanvasGroup tameGroup;
    bool wasTamed;
    float tameDoneTimer;
    float lastStaminaValue = 1f;
    float trailValue = 1f;
    float trailHoldTimer;

    public void Tick(PlayerController player, RideSession ride, Camera cam)
    {
        bool show = ride != null && ride.Active && player != null && cam != null;
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (!show)
        {
            springOffset = Vector2.zero;
            springVel = Vector2.zero;
            wasTamed = false;
            tameDoneTimer = 0f;
            lastStaminaValue = 1f;
            trailValue = 1f;
            trailHoldTimer = 0f;
            if (staminaTrailFill != null) staminaTrailFill.fillAmount = 1f;
            if (tameGroup != null) tameGroup.alpha = 1f;
            return;
        }

        float dt = Time.deltaTime;
        Vector2 targetOffset = new Vector2(
            player.RideBounceOffset.x * swayFollowMultiplier,
            player.RideBounceOffset.y * bounceFollowMultiplier);
        Vector2 accel = (targetOffset - springOffset) * springStiffness - springVel * springDamping;
        springVel += accel * dt;
        springOffset += springVel * dt;

        Vector3 localOffset = player.transform.right * springOffset.x + Vector3.up * springOffset.y;
        Vector3 worldPos = player.transform.position + worldOffset + localOffset;
        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z < 0f) return;
        transform.position = new Vector3(sp.x, sp.y, 0f);

        float s = ride.Stamina01;
        if (staminaFill != null)
        {
            EnsureTrailFill();
            if (s > lastStaminaValue + 0.01f)
            {
                // Vừa nhảy sang Dino mới -> reset vệt trắng về đầy ngay
                trailValue = s;
                trailHoldTimer = 0f;
            }
            else if (s < lastStaminaValue - 0.0001f)
            {
                // Vừa tụt 1 khúc -> giữ vệt trắng ở mức cũ trong trailHoldTime rồi mới thu gọn
                if (trailValue < lastStaminaValue) trailValue = lastStaminaValue;
                trailHoldTimer = trailHoldTime;
            }
            lastStaminaValue = s;

            if (trailHoldTimer > 0f)
                trailHoldTimer -= dt;
            else if (trailValue > s)
                trailValue = Mathf.MoveTowards(trailValue, s, Mathf.Max(0.05f, trailShrinkSpeed) * dt);
            else
                trailValue = s;

            if (staminaTrailFill != null)
            {
                staminaTrailFill.fillAmount = trailValue;
                staminaTrailFill.color = staminaTrailColor;
            }

            staminaFill.fillAmount = s;
            staminaFill.color = ride.IsLowStamina ? staminaLow : (s > 0.5f ? staminaHigh : staminaMid);
        }
        if (tameFill != null)
        {
            Transform tameRoot = (tameFill.transform.parent != null && tameFill.transform.parent != transform)
                ? tameFill.transform.parent
                : tameFill.transform;
            if (tameGroup == null)
            {
                tameGroup = tameRoot.GetComponent<CanvasGroup>();
                if (tameGroup == null) tameGroup = tameRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (ride.IsPetOwned)
            {
                wasTamed = false;
                tameDoneTimer = 0f;
                tameGroup.alpha = 1f;
                if (tameRoot.gameObject.activeSelf) tameRoot.gameObject.SetActive(false);
            }
            else
            {
                tameFill.fillAmount = ride.Tame01;
                tameFill.color = ride.Tamed ? tameDoneColor : tameColor;

                if (ride.Tamed)
                {
                    if (!wasTamed)
                    {
                        wasTamed = true;
                        tameDoneTimer = 0f;
                    }
                    tameDoneTimer += dt;
                    float fadeDur = Mathf.Max(0.05f, tameDoneFadeDuration);
                    float alpha = tameDoneTimer < tameDoneFadeDelay
                        ? 1f
                        : Mathf.Clamp01(1f - (tameDoneTimer - tameDoneFadeDelay) / fadeDur);
                    tameGroup.alpha = alpha;
                    bool keepAlive = alpha > 0.001f;
                    if (tameRoot.gameObject.activeSelf != keepAlive)
                        tameRoot.gameObject.SetActive(keepAlive);
                }
                else
                {
                    wasTamed = false;
                    tameDoneTimer = 0f;
                    tameGroup.alpha = 1f;
                    if (!tameRoot.gameObject.activeSelf) tameRoot.gameObject.SetActive(true);
                }
            }
        }
    }

    void EnsureTrailFill()
    {
        if (staminaTrailFill != null || staminaFill == null) return;

        Transform parent = staminaFill.transform.parent;
        GameObject go = new GameObject("StaminaTrailFill", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(staminaFill.transform.GetSiblingIndex());

        RectTransform srcRt = staminaFill.rectTransform;
        RectTransform dstRt = go.GetComponent<RectTransform>();
        dstRt.anchorMin = srcRt.anchorMin;
        dstRt.anchorMax = srcRt.anchorMax;
        dstRt.pivot = srcRt.pivot;
        dstRt.anchoredPosition = srcRt.anchoredPosition;
        dstRt.sizeDelta = srcRt.sizeDelta;
        dstRt.localScale = srcRt.localScale;
        dstRt.localRotation = srcRt.localRotation;

        staminaTrailFill = go.GetComponent<Image>();
        staminaTrailFill.sprite = staminaFill.sprite;
        staminaTrailFill.type = Image.Type.Filled;
        staminaTrailFill.fillMethod = staminaFill.fillMethod;
        staminaTrailFill.fillOrigin = staminaFill.fillOrigin;
        staminaTrailFill.fillClockwise = staminaFill.fillClockwise;
        staminaTrailFill.preserveAspect = staminaFill.preserveAspect;
        staminaTrailFill.raycastTarget = false;
        staminaTrailFill.color = staminaTrailColor;
        staminaTrailFill.fillAmount = 1f;
    }
}
