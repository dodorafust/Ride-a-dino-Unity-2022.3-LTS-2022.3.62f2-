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

    [Header("Các vòng Stamina phụ bên ngoài (tự tạo nếu để trống)")]
    public Image[] extraStaminaRings;
    public float extraRingScaleStep = 0.24f;
    public Color[] extraRingColors = new Color[]
    {
        new Color(0.18f, 0.72f, 0.25f, 1f),
        new Color(0.12f, 0.58f, 0.20f, 1f)
    };
    public Color extraRingBgColor = new Color(0.08f, 0.12f, 0.10f, 0.50f);
    public Color tameColor = new Color(1f, 0.4f, 0.65f);
    public Color tameDoneColor = new Color(1f, 0.85f, 0.1f);
    public float tameDoneFadeDelay = 0.2f;
    public float tameDoneFadeDuration = 0.45f;

    Image[] extraRingBgs;
    Image[] extraRingTrails;
    float[] extraLastValues;
    float[] extraTrailValues;
    float[] extraTrailTimers;
    bool extraRingsBuilt;

    CanvasGroup tameGroup;
    bool wasTamed;
    float tameDoneTimer;
    float lastCoreValue = 1f;
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
            lastCoreValue = 1f;
            trailValue = 1f;
            trailHoldTimer = 0f;
            if (staminaTrailFill != null) staminaTrailFill.fillAmount = 1f;
            if (tameGroup != null) tameGroup.alpha = 1f;
            ResetExtraTrails();
            SetExtraRingsVisible(false);
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

        float totalRings = Mathf.Max(1f, ride.TotalRings);
        float remRings = ride.RingsRemaining;
        float core = Mathf.Clamp01(remRings);
        if (staminaFill != null)
        {
            EnsureTrailFill();
            UpdateTrailState(core, ref lastCoreValue, ref trailValue, ref trailHoldTimer, dt);

            if (staminaTrailFill != null)
            {
                staminaTrailFill.fillAmount = trailValue;
                staminaTrailFill.color = staminaTrailColor;
            }

            staminaFill.fillAmount = core;
            staminaFill.color = ride.IsLowStamina ? staminaLow : (core > 0.5f ? staminaHigh : staminaMid);

            UpdateExtraRings(totalRings, remRings, dt);
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

    void EnsureExtraRings(int needed)
    {
        if (staminaFill == null || needed <= 0) return;
        if (extraStaminaRings != null && extraStaminaRings.Length >= needed && extraRingsBuilt) return;

        if (extraStaminaRings == null || extraStaminaRings.Length < needed)
        {
            var newRings = new Image[needed];
            if (extraStaminaRings != null)
            {
                for (int i = 0; i < extraStaminaRings.Length; i++)
                    newRings[i] = extraStaminaRings[i];
            }
            extraStaminaRings = newRings;
        }
        if (extraRingBgs == null || extraRingBgs.Length < needed)
        {
            var newBgs = new Image[needed];
            if (extraRingBgs != null)
            {
                for (int i = 0; i < extraRingBgs.Length; i++)
                    newBgs[i] = extraRingBgs[i];
            }
            extraRingBgs = newBgs;
        }
        if (extraRingTrails == null || extraRingTrails.Length < needed)
        {
            var newTrails = new Image[needed];
            if (extraRingTrails != null)
            {
                for (int i = 0; i < extraRingTrails.Length; i++)
                    newTrails[i] = extraRingTrails[i];
            }
            extraRingTrails = newTrails;
            extraLastValues = new float[needed];
            extraTrailValues = new float[needed];
            extraTrailTimers = new float[needed];
            for (int i = 0; i < needed; i++)
            {
                extraLastValues[i] = 1f;
                extraTrailValues[i] = 1f;
            }
        }

        Transform parent = staminaFill.transform.parent;
        int baseSibling = staminaTrailFill != null ? staminaTrailFill.transform.GetSiblingIndex() : staminaFill.transform.GetSiblingIndex();

        for (int i = 0; i < needed; i++)
        {
            float scaleMul = 1f + (i + 1) * Mathf.Max(0.1f, extraRingScaleStep);
            if (extraRingBgs[i] == null)
            {
                extraRingBgs[i] = CloneRingImage("ExtraStaminaBg_" + (i + 2), parent, baseSibling, scaleMul, extraRingBgColor);
            }
            if (extraRingTrails[i] == null)
            {
                int trailSibling = extraRingBgs[i] != null ? extraRingBgs[i].transform.GetSiblingIndex() + 1 : baseSibling + 1;
                extraRingTrails[i] = CloneRingImage("ExtraStaminaTrail_" + (i + 2), parent, trailSibling, scaleMul, staminaTrailColor);
            }
            if (extraStaminaRings[i] == null)
            {
                Color c = (extraRingColors != null && extraRingColors.Length > 0)
                    ? extraRingColors[Mathf.Min(i, extraRingColors.Length - 1)]
                    : staminaHigh;
                int fillSibling = extraRingTrails[i] != null ? extraRingTrails[i].transform.GetSiblingIndex() + 1 : baseSibling + 2;
                extraStaminaRings[i] = CloneRingImage("ExtraStaminaFill_" + (i + 2), parent, fillSibling, scaleMul, c);
            }
        }
        extraRingsBuilt = true;
    }

    Image CloneRingImage(string goName, Transform parent, int siblingIdx, float scaleMul, Color color)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(Mathf.Max(0, siblingIdx));

        RectTransform srcRt = staminaFill.rectTransform;
        RectTransform dstRt = go.GetComponent<RectTransform>();
        dstRt.anchorMin = srcRt.anchorMin;
        dstRt.anchorMax = srcRt.anchorMax;
        dstRt.pivot = srcRt.pivot;
        dstRt.anchoredPosition = srcRt.anchoredPosition;
        dstRt.sizeDelta = srcRt.sizeDelta;
        dstRt.localScale = srcRt.localScale * scaleMul;
        dstRt.localRotation = srcRt.localRotation;

        Image img = go.GetComponent<Image>();
        img.sprite = staminaFill.sprite;
        img.type = Image.Type.Filled;
        img.fillMethod = staminaFill.fillMethod;
        img.fillOrigin = staminaFill.fillOrigin;
        img.fillClockwise = staminaFill.fillClockwise;
        img.preserveAspect = staminaFill.preserveAspect;
        img.raycastTarget = false;
        img.color = color;
        img.fillAmount = 1f;
        return img;
    }

    void UpdateTrailState(float curVal, ref float lastVal, ref float trailVal, ref float holdTimer, float dt)
    {
        if (curVal > lastVal + 0.01f)
        {
            // Vừa nhảy sang Dino mới -> reset vệt trắng về đầy ngay
            trailVal = curVal;
            holdTimer = 0f;
        }
        else if (curVal < lastVal - 0.0001f)
        {
            // Vừa tụt 1 khúc -> giữ vệt trắng ở mức cũ trong trailHoldTime rồi mới thu gọn
            if (trailVal < lastVal) trailVal = lastVal;
            holdTimer = trailHoldTime;
        }
        lastVal = curVal;

        if (holdTimer > 0f)
            holdTimer -= dt;
        else if (trailVal > curVal)
            trailVal = Mathf.MoveTowards(trailVal, curVal, Mathf.Max(0.05f, trailShrinkSpeed) * dt);
        else
            trailVal = curVal;
    }

    void ResetExtraTrails()
    {
        if (extraLastValues == null) return;
        for (int i = 0; i < extraLastValues.Length; i++)
        {
            extraLastValues[i] = 1f;
            if (extraTrailValues != null && i < extraTrailValues.Length) extraTrailValues[i] = 1f;
            if (extraTrailTimers != null && i < extraTrailTimers.Length) extraTrailTimers[i] = 0f;
            if (extraRingTrails != null && i < extraRingTrails.Length && extraRingTrails[i] != null)
                extraRingTrails[i].fillAmount = 1f;
        }
    }

    void UpdateExtraRings(float totalRings, float remRings, float dt)
    {
        int needed = Mathf.Max(0, Mathf.CeilToInt(totalRings) - 1);
        if (needed > 0) EnsureExtraRings(needed);
        if (extraStaminaRings == null) return;

        for (int i = 0; i < extraStaminaRings.Length; i++)
        {
            float ringMin = 1f + i;
            float ringVal = Mathf.Clamp01(remRings - ringMin);
            bool hasRing = i < needed && totalRings > ringMin + 0.001f;
            bool visible = hasRing && remRings > ringMin + 0.001f;

            float trailVal = ringVal;
            if (extraLastValues != null && i < extraLastValues.Length)
            {
                if (!hasRing)
                {
                    extraLastValues[i] = 0f;
                    extraTrailValues[i] = 0f;
                    extraTrailTimers[i] = 0f;
                }
                else
                {
                    UpdateTrailState(ringVal, ref extraLastValues[i], ref extraTrailValues[i], ref extraTrailTimers[i], dt);
                    trailVal = extraTrailValues[i];
                }
            }

            if (extraRingBgs != null && i < extraRingBgs.Length && extraRingBgs[i] != null)
            {
                if (extraRingBgs[i].gameObject.activeSelf != visible)
                    extraRingBgs[i].gameObject.SetActive(visible);
                if (visible)
                    extraRingBgs[i].fillAmount = Mathf.Clamp01(totalRings - ringMin);
            }

            if (extraRingTrails != null && i < extraRingTrails.Length && extraRingTrails[i] != null)
            {
                bool showTrail = hasRing && (visible || trailVal > 0.001f);
                if (extraRingTrails[i].gameObject.activeSelf != showTrail)
                    extraRingTrails[i].gameObject.SetActive(showTrail);
                if (showTrail)
                {
                    extraRingTrails[i].fillAmount = trailVal;
                    extraRingTrails[i].color = staminaTrailColor;
                }
            }

            if (extraStaminaRings[i] != null)
            {
                if (extraStaminaRings[i].gameObject.activeSelf != visible)
                    extraStaminaRings[i].gameObject.SetActive(visible);
                if (visible)
                    extraStaminaRings[i].fillAmount = ringVal;
            }
        }
    }

    void SetExtraRingsVisible(bool visible)
    {
        if (extraStaminaRings != null)
        {
            for (int i = 0; i < extraStaminaRings.Length; i++)
                if (extraStaminaRings[i] != null && extraStaminaRings[i].gameObject.activeSelf != visible)
                    extraStaminaRings[i].gameObject.SetActive(visible);
        }
        if (extraRingTrails != null)
        {
            for (int i = 0; i < extraRingTrails.Length; i++)
                if (extraRingTrails[i] != null && extraRingTrails[i].gameObject.activeSelf != visible)
                    extraRingTrails[i].gameObject.SetActive(visible);
        }
        if (extraRingBgs != null)
        {
            for (int i = 0; i < extraRingBgs.Length; i++)
                if (extraRingBgs[i] != null && extraRingBgs[i].gameObject.activeSelf != visible)
                    extraRingBgs[i].gameObject.SetActive(visible);
        }
    }
}
