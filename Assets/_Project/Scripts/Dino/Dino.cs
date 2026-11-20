using System.Collections.Generic;
using UnityEngine;

public class Dino : MonoBehaviour
{
    public static int Alive;
    public static readonly List<Dino> All = new List<Dino>();

    [Header("Movement")]
    public float baseSpeed = 6f;
    public float steerSpeed = 6f;          // A/D hoặc joystick: trượt ngang
    public float turnSpeed = 120f;         // độ/giây khi xoay (dùng cho AI hoặc khi tắt instantCameraFollow)
    public bool limitTurnAngle = true;     // bật/tắt giới hạn góc quay đầu khi đang cưỡi
    public float maxSteerAngle = 60f;
    public bool instantCameraFollow = true;// true = hướng chạy trùng hướng camera ngay lập tức, không độ trễ
    public float steerBodyTurnSpeed = 14f; // tốc độ xoay thân/đầu theo hướng kéo joystick/WASD
    public float lifeTime = 60f;
    public float despawnMargin = 10f;      // ra khỏi RunZone quá ngần này thì biến mất
    [Min(1f)] public float animReferenceSpeed = 15f; // tốc độ chạy chuẩn ứng với Animator.speed = 1

    [Header("Boost & Tốc độ khi bị bắt cưỡi")]
    public float boostMultiplier = 1.6f;           // hệ số vọt tốc độ ngay lúc mới nhảy lên lưng
    public float boostDuration = 2.5f;             // thời gian giảm dần từ Boost về tốc độ cưỡi ổn định
    public float riddenSpeedMultiplier = 1.25f;    // hệ số tốc độ giữ lại sau khi hết Boost (1.25 = nhanh hơn mặc định 25%)
    public float riddenBonusSpeed = 0f;            // tốc độ cộng thêm trực tiếp (m/s) khi đang cưỡi (nếu cần)

    [Header("Cảm biến tường (chỉ chạy khi đang được cưỡi)")]
    public Vector3 bodyCenter = new Vector3(0f, 1f, 0f);
    public float wallHitRadius = 0.7f;             // bán kính hình cầu kiểm tra & trượt tường (không bị kẹt góc như hộp)
    public float wallCrashDelay = 2f;              // giữ trạng thái húc tường liên tục ngần này giây mới chết

    [Header("References")]
    public Transform seat;
    public bool autoLabelAboveHead = true;         // tự đặt tên ngay trên đỉnh đầu Dino theo kích thước thật
    public float labelHeadPadding = 0.65f;         // khoảng cách chữ phía trên đỉnh đầu khi bật autoLabelAboveHead
    public Vector3 labelOffset = new Vector3(0f, 3.8f, 0f);

    [Header("Highlight khi được target")]
    public Color highlightColor = new Color(1f, 0.9f, 0.2f);
    [Min(0f)] public float highlightBrightness = 1.6f;     // độ sáng màu cơ thể khi target
    [Min(0f)] public float highlightEmission = 1.5f;       // cường độ phát sáng Emission
    [Range(0f, 1f)] public float highlightColorBlend = 0.75f; // mức pha màu highlight vào màu gốc

    [Header("Sắp hết Stamina - Nhấp nháy đỏ (Chung)")]
    public Color lowStaminaFlashColor = new Color(1f, 0.12f, 0.12f);
    [Range(0f, 1f)] public float lowStaminaColorBlend = 0.72f;
    [Min(0f)] public float lowStaminaFlashEmission = 1.4f;
    [Min(0.5f)] public float lowStaminaFlashFreqMin = 3.5f;
    [Min(0.5f)] public float lowStaminaFlashFreqMax = 9.0f;

    [Header("Sắp hết Stamina - Dino Ăn cỏ (Nhảy loạn xạ)")]
    [Min(0f)] public float herbivoreHopHeight = 0.65f;
    [Min(0.08f)] public float herbivoreHopDurationMin = 0.20f;
    [Min(0.1f)] public float herbivoreHopDurationMax = 0.32f;
    [Range(0f, 70f)] public float herbivoreErraticYawAngle = 32f;
    [Range(0f, 1f)] public float herbivoreControlScale = 0.25f;
    [Min(1f)] public float herbivoreHopTurnSnappiness = 10f;
    [Min(0f)] public float herbivoreHopPitchTilt = 12f;
    [Min(0f)] public float herbivoreHopRollTilt = 10f;
    [Min(0f)] public float herbivoreHopShakeIntensity = 0.06f;
    [Min(0.02f)] public float herbivoreHopShakeDuration = 0.08f;

    [Header("Sắp hết Stamina - Dino Ăn thịt (Chậm dần rồi dừng hẳn)")]
    [Range(0f, 1f)] public float carnivoreMinSpeedRatio = 0f;
    [Min(0.2f)] public float carnivoreSlowdownCurve = 1.2f;
    [Range(0f, 1f)] public float carnivoreMinTurnScale = 0.25f;

    [Header("Dino Ăn thịt - Nhấc lên & Nuốt vào miệng")]
    [Min(0.05f)] public float carnivoreLiftDuration = 0.16f;      // thời gian nhấc con mồi lên trước mặt
    [Min(0.05f)] public float carnivoreSwallowDuration = 0.20f;   // thời gian thu nhỏ và hút vào miệng
    public float carnivoreFrontDistance = 1.8f;                   // khoảng cách phía trước miệng khi nhấc lên
    public float carnivoreLiftHeight = 0.9f;                      // độ cao nhấc lên thêm trước khi nuốt
    [Range(0.01f, 0.5f)] public float carnivoreSwallowEndScale = 0.08f; // kích thước thu nhỏ tối thiểu khi chui vào miệng
    public float carnivoreVictimSpinSpeed = 240f;                 // xoay nhẹ con mồi lúc bị nhấc lên
    public bool carnivoreSpawnDebris = false;                     // bật nếu muốn văng thêm mảnh nhỏ lúc nuốt xong

    [Header("AI Dino hoang dã (khi chưa bị cưỡi)")]
    public float aiLookAhead = 10f;        // tầm nhìn phía trước (m)
    public float aiProbeRadius = 1.1f;     // bán kính tia dò (~ nửa bề ngang Dino)
    public float aiMaxOffset = 45f;        // lệch tối đa so với hướng đường chạy -> luôn chạy về phía trước
    public float aiTurnRate = 70f;         // tốc độ đổi hướng (độ/giây)
    public float aiSideProbe = 3.5f;       // tầm dò tường 2 bên
    public float aiEdgeMargin = 3f;        // cách mép RunZone bao nhiêu thì lái vào trong
    public float aiWanderAngle = 7f;       // lượn nhẹ cho tự nhiên
    public float aiSenseInterval = 0.1f;
    public float aiSeparationRadius = 3.2f;
    public float aiSeparationSpeed = 3f;

    public OwnedDino Owned { get; private set; }
    public DinoData Data { get; private set; }
    public int EffectiveSizeScore
    {
        get
        {
            DinoSpeciesSize baseS = Data != null ? Data.size : DinoSpeciesSize.Medium;
            DinoSizeType st = Owned != null ? Owned.sizeType : DinoSizeType.Normal;
            return DinoSizeModifiers.CombinedRank(baseS, st);
        }
    }
    public ObjectSize EffectiveSize
    {
        get
        {
            return DinoSizeModifiers.RankToObjectSize(EffectiveSizeScore);
        }
    }
    public float CurrentSpeed { get; private set; }
    public bool IsRidden { get; set; }
    public bool Claimed { get; private set; }
    public bool IsPetOwned { get; private set; }
    public bool IsBeingEaten { get; private set; }
    public bool TouchingWall { get; private set; }
    public Obstacle SlowingObstacle { get { return slowTimer > 0f ? lastHitObstacle : null; } }

    float steer, heading, laneYaw;
    float boostTimer, slowTimer, slowMultiplier = 1f;
    float despawnTimer = 2f;
    RunZone runZone;

    readonly List<Renderer> rends = new List<Renderer>();
    readonly List<Material[]> origMaterials = new List<Material[]>();
    static Material sharedHighlightMat;
    MaterialPropertyBlock mpb;
    bool hasBaseTint;
    Color baseTint;
    bool highlighted;
    WorldLabel label;
    readonly Collider[] wallBuf = new Collider[32];
    Obstacle lastHitObstacle;
    float lastObstacleHitTime;

    // AI
    static readonly float[] aiAngles = { 0f, -0.5f, 0.5f, -1f, 1f };   // nhân với aiMaxOffset
    readonly RaycastHit[] castBuf = new RaycastHit[24];
    float aiOffset, aiTargetOffset, aiSenseTimer, aiSpeedK = 1f, aiSpeedGoal = 1f, aiSeed;
    bool aiEmergencySteer;
    bool wasRidden;
    Collider selfCol;
    Animator dinoAnimator;
    Transform seatBone;
    Transform mouthBone;
    Vector3 baseSeatLocalPos, seatOffsetScaled;
    float visualScale = 1f;
    float wallTouchTimer;
    bool countedInAlive = true;
    bool petReturned;
    Transform visualRoot;
    Vector3 visualBaseLocalPos;
    Quaternion visualBaseLocalRot = Quaternion.identity;
    Material[][] flashMaterials;
    bool lowStaminaActive;
    bool lowStaminaForceStop;
    float lowStamina01;
    float lowStaminaFlashPhase;
    bool flashMaterialsApplied;
    float desiredHeading;
    float buckBaseHeading;
    float currentBuckYaw;
    float hopTimer;
    float hopDuration;
    float currentHopY;
    int lastBuckSide = 1;
    float summonRiseTimer;
    float summonRiseDuration;
    Transform windowPlayer;
    Vector3 windowOrigin, windowForward;
    float windowBehind = 50f, windowAhead = 125f;

    void Awake()
    {
        Alive++;
        All.Add(this);
        mpb = new MaterialPropertyBlock();
        CurrentSpeed = baseSpeed;
        selfCol = GetComponent<Collider>();
        aiSeed = Random.value * 100f;
        aiSenseTimer = Random.value * aiSenseInterval;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.StatsChanged += RefreshLabel;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.StatsChanged -= RefreshLabel;
    }

    void OnDestroy()
    {
        if (countedInAlive)
        {
            Alive--;
            All.Remove(this);
        }
        if (IsPetOwned && !petReturned && MountController.Instance != null)
        {
            petReturned = true;
            MountController.Instance.OnPetRunDinoReturned(Owned);
        }
        if (flashMaterials != null)
        {
            for (int i = 0; i < flashMaterials.Length; i++)
            {
                if (flashMaterials[i] == null) continue;
                for (int m = 0; m < flashMaterials[i].Length; m++)
                {
                    if (flashMaterials[i][m] != null) Destroy(flashMaterials[i][m]);
                }
            }
            flashMaterials = null;
        }
    }

    public void SuppressPetReturnOnDestroy()
    {
        petReturned = true;
    }

    public void PlaySummonRise(float duration = 0.36f)
    {
        summonRiseDuration = Mathf.Max(0.1f, duration);
        summonRiseTimer = summonRiseDuration;
        if (visualRoot != null)
        {
            float drop = Mathf.Max(0.8f, 1.3f * visualScale);
            visualRoot.localPosition = visualBaseLocalPos + Vector3.down * drop;
        }
        UpdateSeatTransform();
    }

    public void MarkAsPetOwned()
    {
        IsPetOwned = true;
        Claimed = true;
        if (countedInAlive)
        {
            countedInAlive = false;
            Alive--;
            All.Remove(this);
        }
        if (label != null) label.hidden = true;
    }

    // Gọi ngay sau Instantiate
    public void Setup(OwnedDino owned, float speed, float laneYawDeg, RunZone zone)
    {
        Owned = owned;
        Data = owned.data;
        baseSpeed = speed;
        CurrentSpeed = speed;
        laneYaw = laneYawDeg;
        runZone = zone;
        transform.rotation = Quaternion.Euler(0f, laneYaw, 0f);

        // Model: ưu tiên model 3D của DinoData, nếu không thì dùng "Body" greybox trong prefab
        Transform body = transform.Find("Body");
        GameObject visual;
        if (owned.data.modelPrefab != null)
        {
            if (body != null) body.gameObject.SetActive(false);
            visual = DinoView.BuildModel(transform, owned, Vector3.one);
        }
        else if (body != null)
        {
            visual = body.gameObject;
            DinoView.ApplyLook(visual, owned, true);
        }
        else
        {
            visual = DinoView.BuildModel(transform, owned, new Vector3(2f, 2f, 4f));
        }

        rends.Clear();
        origMaterials.Clear();
        Renderer[] all = visual.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is ParticleSystemRenderer) continue;
            rends.Add(all[i]);
            origMaterials.Add(all[i].sharedMaterials);
        }

        hasBaseTint = DinoView.TryGetTint(owned, owned.data.modelPrefab == null, out baseTint);
        dinoAnimator = visual != null ? visual.GetComponentInChildren<Animator>() : null;
        if (dinoAnimator != null) dinoAnimator.Play(0, 0, Random.value);

        float dataScale = Mathf.Max(0.1f, owned.data.modelScale * owned.SizeScaleMultiplier);
        visualRoot = visual != null ? visual.transform : null;
        visualBaseLocalPos = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
        visualBaseLocalRot = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
        visualScale = visual != null ? Mathf.Max(0.1f, visual.transform.localScale.y) : dataScale;
        baseSeatLocalPos = seat != null ? seat.localPosition : new Vector3(0f, 3f, 0f);
        float sizeMul = Mathf.Max(0.1f, owned.SizeScaleMultiplier);
        float baseVisualScale = visualScale / sizeMul;
        float seatSizeMul = DinoSizeModifiers.SeatOffsetMultiplier(owned.sizeType, owned.data.seatSizeScaleWeight);
        seatOffsetScaled = owned.data.seatOffset * (baseVisualScale * seatSizeMul);
        seatBone = DinoView.FindSeatBone(visual != null ? visual.transform : null, owned.data.seatBoneName);
        mouthBone = FindMouthBone(visual != null ? visual.transform : null, owned.data.mouthBoneName);
        wallHitRadius *= dataScale;
        UpdateSeatTransform();

        if (GameManager.Instance != null)
        {
            Vector3 finalLabelOffset = autoLabelAboveHead
                ? new Vector3(labelOffset.x, DinoView.GetModelTopOffset(visual, transform, labelHeadPadding, labelOffset.y * dataScale), labelOffset.z)
                : labelOffset * dataScale;
            label = GameManager.Instance.CreateLabel(transform, finalLabelOffset, "");
        }
        RefreshLabel();
    }

    public void SetWindow(Transform player, Vector3 origin, Vector3 forward, float behind, float ahead)
    {
        windowPlayer = player;
        windowOrigin = origin;
        windowForward = forward;
        windowBehind = behind;
        windowAhead = ahead;
    }

    void UpdateSeatTransform()
    {
        if (seat == null) return;
        if (seatBone != null)
        {
            seat.position = seatBone.position + transform.TransformVector(seatOffsetScaled);
        }
        else
        {
            float dataScale = (Data != null ? Mathf.Max(0.1f, Data.modelScale) : 1f) * (Owned != null ? Owned.SizeScaleMultiplier : 1f);
            seat.position = transform.TransformPoint(baseSeatLocalPos * dataScale + seatOffsetScaled) + Vector3.up * currentHopY;
        }
        seat.rotation = transform.rotation;
    }

    void RefreshLabel()
    {
        if (label == null || Owned == null) return;
        float mine = GameManager.Instance != null ? GameManager.Instance.Stamina : 0f;
        label.SetText(DinoView.BuildWorldLabelText(Owned, true, mine));
    }

    void ApplyTint(Color c)
    {
        mpb.Clear();
        mpb.SetColor("_Color", c);
        mpb.SetColor("_BaseColor", c);
        for (int i = 0; i < rends.Count; i++)
        {
            if (rends[i] != null) rends[i].SetPropertyBlock(mpb);
        }
    }

    Material GetHighlightMaterial()
    {
        if (sharedHighlightMat == null)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            sharedHighlightMat = new Material(sh);
            sharedHighlightMat.SetFloat("_Glossiness", 0f);
            sharedHighlightMat.SetFloat("_Metallic", 0f);
            sharedHighlightMat.EnableKeyword("_EMISSION");
        }

        Color glow = highlightColor * Mathf.Max(0.5f, highlightBrightness + highlightEmission);
        sharedHighlightMat.color = glow;
        sharedHighlightMat.SetColor("_Color", Color.black);
        sharedHighlightMat.SetColor("_EmissionColor", glow);
        return sharedHighlightMat;
    }

    public void SetHighlight(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;

        if (on)
        {
            flashMaterialsApplied = false;
            Material hMat = GetHighlightMaterial();
            for (int i = 0; i < rends.Count; i++)
            {
                Renderer r = rends[i];
                if (r == null) continue;
                r.SetPropertyBlock(null);
                int count = Mathf.Max(1, origMaterials[i].Length);
                Material[] mats = new Material[count];
                for (int m = 0; m < count; m++) mats[m] = hMat;
                r.sharedMaterials = mats;
            }
        }
        else
        {
            flashMaterialsApplied = false;
            if (lowStaminaActive) return;
            for (int i = 0; i < rends.Count; i++)
            {
                Renderer r = rends[i];
                if (r == null) continue;
                r.sharedMaterials = origMaterials[i];
                if (!hasBaseTint) r.SetPropertyBlock(null);
            }
            if (hasBaseTint) ApplyTint(baseTint);
        }
    }

    public void SetLowStaminaState(bool active, float progress01, bool forceStop = false)
    {
        if (!active)
        {
            ClearLowStaminaEffect();
            return;
        }
        if (!lowStaminaActive)
        {
            buckBaseHeading = heading;
            desiredHeading = heading;
            lowStaminaFlashPhase = 0f;
            hopTimer = 0f;
            hopDuration = 0f;
            currentBuckYaw = 0f;
            lastBuckSide = Random.value < 0.5f ? -1 : 1;
        }
        lowStaminaActive = true;
        lowStamina01 = Mathf.Clamp01(progress01);
        lowStaminaForceStop = forceStop;
    }

    public void ClearLowStaminaEffect()
    {
        bool hadState = lowStaminaActive || flashMaterialsApplied || currentHopY != 0f || currentBuckYaw != 0f;
        lowStaminaActive = false;
        lowStaminaForceStop = false;
        lowStamina01 = 0f;
        currentBuckYaw = 0f;
        currentHopY = 0f;
        hopTimer = 0f;
        hopDuration = 0f;

        if (!hadState) return;

        if (visualRoot != null)
        {
            if (summonRiseTimer <= 0f) visualRoot.localPosition = visualBaseLocalPos;
            visualRoot.localRotation = visualBaseLocalRot;
        }
        if (flashMaterialsApplied)
        {
            flashMaterialsApplied = false;
            if (!highlighted)
            {
                for (int i = 0; i < rends.Count; i++)
                {
                    Renderer r = rends[i];
                    if (r == null) continue;
                    r.sharedMaterials = origMaterials[i];
                    if (!hasBaseTint) r.SetPropertyBlock(null);
                }
                if (hasBaseTint) ApplyTint(baseTint);
            }
        }
    }

    void EnsureFlashMaterials()
    {
        if (flashMaterials != null) return;
        flashMaterials = new Material[origMaterials.Count][];
        for (int i = 0; i < origMaterials.Count; i++)
        {
            Material[] src = origMaterials[i];
            int count = src != null ? src.Length : 0;
            flashMaterials[i] = new Material[count];
            for (int m = 0; m < count; m++)
            {
                if (src[m] == null) continue;
                Material clone = new Material(src[m]);
                clone.EnableKeyword("_EMISSION");
                flashMaterials[i][m] = clone;
            }
        }
    }

    void UpdateLowStaminaFlash(float dt)
    {
        if (!lowStaminaActive || highlighted) return;
        EnsureFlashMaterials();
        if (!flashMaterialsApplied)
        {
            for (int i = 0; i < rends.Count; i++)
            {
                if (rends[i] != null && flashMaterials != null && i < flashMaterials.Length)
                    rends[i].sharedMaterials = flashMaterials[i];
            }
            flashMaterialsApplied = true;
        }

        float freq = Mathf.Lerp(lowStaminaFlashFreqMin, lowStaminaFlashFreqMax, lowStamina01);
        lowStaminaFlashPhase += dt * freq * Mathf.PI * 2f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(lowStaminaFlashPhase);

        Color baseC = hasBaseTint ? baseTint : Color.white;
        Color flashTint = Color.Lerp(baseC, lowStaminaFlashColor, pulse * lowStaminaColorBlend);
        Color flashEmit = lowStaminaFlashColor * (pulse * lowStaminaFlashEmission);

        mpb.Clear();
        mpb.SetColor("_Color", flashTint);
        mpb.SetColor("_BaseColor", flashTint);
        mpb.SetColor("_EmissionColor", flashEmit);
        for (int i = 0; i < rends.Count; i++)
        {
            if (rends[i] != null) rends[i].SetPropertyBlock(mpb);
        }
    }

    public void SetSteer(float s) { steer = s; }

    public void SetHeading(float cameraYaw)
    {
        float delta = Mathf.DeltaAngle(laneYaw, cameraYaw);
        float clamped = limitTurnAngle ? Mathf.Clamp(delta, -maxSteerAngle, maxSteerAngle) : delta;
        desiredHeading = clamped;
        bool herbivoreBucking = IsRidden && lowStaminaActive && (Data == null || Data.diet != DinoDiet.Carnivore);
        if (!herbivoreBucking) heading = clamped;
    }

    public void ResetHeading() { heading = 0f; desiredHeading = 0f; buckBaseHeading = 0f; }
    public void Boost() { boostTimer = boostDuration; }
    public void Claim() { Claimed = true; }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowTimer = duration;
    }

    void OnTriggerEnter(Collider other)
    {
        Obstacle ob = other.GetComponentInParent<Obstacle>();
        if (ob == null) return;
        if (IsRidden)
            HandleObstacleHit(ob, transform.TransformPoint(bodyCenter));
        else
            TryWildShatterObstacle(ob, transform.TransformPoint(bodyCenter));
    }

    void TryWildShatterObstacle(Obstacle ob, Vector3 hitPos)
    {
        if (ob == null || ob.IsBroken || !ob.breakable) return;
        if (Data != null && !Data.wildSmashBreakables) return;
        ObjectSize mySize = EffectiveSize;
        if (mySize > ob.size)
        {
            lastHitObstacle = ob;
            lastObstacleHitTime = Time.time;
            ob.Shatter(hitPos, transform.forward, CurrentSpeed, false);
        }
    }

    void HandleObstacleHit(Obstacle ob, Vector3 hitPos)
    {
        if (ob == null || ob.IsBroken) return;
        if (ob == lastHitObstacle && Time.time - lastObstacleHitTime < 0.45f) return;

        ObjectSize mySize = EffectiveSize;
        if (mySize > ob.size && ob.breakable)
        {
            lastHitObstacle = ob;
            lastObstacleHitTime = Time.time;
            ob.Shatter(hitPos, transform.forward, CurrentSpeed, true);
            return;
        }

        lastHitObstacle = ob;
        lastObstacleHitTime = Time.time;
        if (CameraRig.Instance != null)
            CameraRig.Instance.Shake(ob.slowShakeIntensity, ob.slowShakeDuration);
        ApplySlow(ob.slowMultiplier, ob.slowDuration);
    }

    static Transform FindMouthBone(Transform root, string preferredName)
    {
        if (root == null) return null;
        if (!string.IsNullOrEmpty(preferredName))
        {
            Transform exact = DinoView.FindSeatBone(root, preferredName);
            if (exact != null) return exact;
        }
        string[] fallbacks = { "mouth", "jaw", "head" };
        for (int i = 0; i < fallbacks.Length; i++)
        {
            Transform hit = DinoView.FindSeatBone(root, fallbacks[i]);
            if (hit != null) return hit;
        }
        return null;
    }

    Vector3 GetMouthWorldPosition()
    {
        Vector3 localOff = (Data != null ? Data.mouthOffset : new Vector3(0f, 1.4f, 1.6f)) * visualScale;
        if (mouthBone != null)
            return mouthBone.position + transform.TransformVector(localOff * 0.25f);
        return transform.TransformPoint(localOff);
    }

    public void BeginBeingEaten()
    {
        if (IsBeingEaten) return;
        IsBeingEaten = true;
        Claimed = true;
        if (selfCol != null) selfCol.enabled = false;
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = false;
        if (label != null) label.hidden = true;
    }

    // Dino ăn cỏ: húc / giẫm nát con nhỏ hơn khác loài
    void TrampleSmallerDino(Dino victim)
    {
        if (victim == null) return;
        Vector3 victimCenter = victim.transform.TransformPoint(victim.bodyCenter);
        float scaleMul = victim.Data != null ? victim.Data.modelScale : 1f;
        int debrisCount = Data != null ? Data.eatDebrisCount : 6;
        float shakeInt = Data != null ? Data.eatShakeIntensity : 0.25f;
        float shakeDur = Data != null ? Data.eatShakeDuration : 0.18f;
        Obstacle.SpawnDinoEatenDebris(victimCenter, transform.forward, scaleMul, debrisCount);
        if (shakeInt > 0f && shakeDur > 0f && CameraRig.Instance != null)
            CameraRig.Instance.Shake(shakeInt, shakeDur);
        Destroy(victim.gameObject);
    }

    // Dino ăn thịt: nhấc con nhỏ lên trước mặt, thu nhỏ rồi hút vào miệng
    void SwallowSmallerDino(Dino victim)
    {
        if (victim == null || victim.IsBeingEaten) return;
        victim.BeginBeingEaten();
        StartCoroutine(SwallowDinoRoutine(victim));
    }

    System.Collections.IEnumerator SwallowDinoRoutine(Dino victim)
    {
        if (victim == null) yield break;

        Vector3 startPos = victim.transform.position;
        Quaternion startRot = victim.transform.rotation;
        Vector3 startScale = victim.transform.localScale;
        Vector3 endScale = startScale * Mathf.Clamp(carnivoreSwallowEndScale, 0.01f, 0.5f);

        float liftDur = Mathf.Max(0.05f, carnivoreLiftDuration);
        float t = 0f;
        while (t < liftDur)
        {
            if (victim == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / liftDur);
            float ease = 1f - (1f - p) * (1f - p);

            Vector3 mouthPos = GetMouthWorldPosition();
            Vector3 frontHover = mouthPos
                + transform.forward * (carnivoreFrontDistance * Mathf.Clamp(visualScale, 0.7f, 2.2f))
                + Vector3.up * (carnivoreLiftHeight * Mathf.Clamp(visualScale, 0.7f, 2.2f));
            float arc = Mathf.Sin(p * Mathf.PI) * 0.35f * Mathf.Clamp(visualScale, 0.7f, 2f);

            victim.transform.position = Vector3.Lerp(startPos, frontHover, ease) + Vector3.up * arc;
            victim.transform.rotation = startRot * Quaternion.Euler(p * 25f, p * carnivoreVictimSpinSpeed * liftDur, 0f);
            yield return null;
        }

        float swallowDur = Mathf.Max(0.05f, carnivoreSwallowDuration);
        Vector3 hoverStartPos = victim != null ? victim.transform.position : GetMouthWorldPosition();
        Quaternion hoverStartRot = victim != null ? victim.transform.rotation : Quaternion.identity;
        t = 0f;
        while (t < swallowDur)
        {
            if (victim == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / swallowDur);
            float ease = p * p;

            Vector3 mouthPos = GetMouthWorldPosition();
            victim.transform.position = Vector3.Lerp(hoverStartPos, mouthPos, ease);
            victim.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
            victim.transform.rotation = hoverStartRot * Quaternion.Euler(p * 35f, p * carnivoreVictimSpinSpeed * swallowDur, 0f);
            yield return null;
        }

        float shakeInt = Data != null ? Data.eatShakeIntensity : 0.25f;
        float shakeDur = Data != null ? Data.eatShakeDuration : 0.18f;
        if (carnivoreSpawnDebris && victim != null)
        {
            float scaleMul = victim.Data != null ? victim.Data.modelScale : 1f;
            int debrisCount = Data != null ? Data.eatDebrisCount : 6;
            Obstacle.SpawnDinoEatenDebris(GetMouthWorldPosition(), transform.forward, scaleMul, debrisCount);
        }
        if (shakeInt > 0f && shakeDur > 0f && CameraRig.Instance != null)
            CameraRig.Instance.Shake(shakeInt, shakeDur);

        if (victim != null) Destroy(victim.gameObject);
    }

    void CheckWildObstacles()
    {
        if (Data != null && !Data.wildSmashBreakables) return;
        ObjectSize mySize = EffectiveSize;
        if (mySize == ObjectSize.Small) return;

        Vector3 c = transform.TransformPoint(bodyCenter);
        int n = Physics.OverlapSphereNonAlloc(c, wallHitRadius, wallBuf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Obstacle ob = wallBuf[i].GetComponentInParent<Obstacle>();
            if (ob != null)
                TryWildShatterObstacle(ob, c);
        }
    }

    void CheckWall(float dt)
    {
        bool hitWall = false;
        Vector3 c = transform.TransformPoint(bodyCenter);
        float targetDist = Mathf.Max(0.05f, wallHitRadius - 0.04f);
        ObjectSize mySize = EffectiveSize;
        float dataScale = (Data != null ? Mathf.Max(0.1f, Data.modelScale) : 1f) * (Owned != null ? Owned.SizeScaleMultiplier : 1f);
        bool isCarnivore = Data != null && Data.diet == DinoDiet.Carnivore;
        int myRank = EffectiveSizeScore;
        float eatRadius = wallHitRadius + (Data != null ? Data.eatRadiusBonus : 0f);
        Vector3 eatLocal = bodyCenter + (Data != null ? Data.eatCenterOffset * dataScale : Vector3.zero);
        Vector3 eatCenter = transform.TransformPoint(eatLocal);
        int en = Physics.OverlapSphereNonAlloc(eatCenter, eatRadius, wallBuf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < en; i++)
        {
            Dino otherDino = wallBuf[i].GetComponentInParent<Dino>();
            if (otherDino != null && otherDino != this && !otherDino.IsRidden && !otherDino.Claimed && !otherDino.IsPetOwned && !otherDino.IsBeingEaten)
            {
                bool diffSpecies = Data == null || otherDino.Data == null || otherDino.Data != Data;
                if (diffSpecies && myRank > otherDino.EffectiveSizeScore)
                {
                    if (isCarnivore)
                    {
                        SwallowSmallerDino(otherDino);
                        continue;
                    }
                    else
                    {
                        TrampleSmallerDino(otherDino);
                        continue;
                    }
                }
            }
        }

        int n = Physics.OverlapSphereNonAlloc(c, wallHitRadius, wallBuf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Collider col = wallBuf[i];
            Obstacle ob = col.GetComponentInParent<Obstacle>();
            if (ob != null)
            {
                HandleObstacleHit(ob, c);
                continue;
            }
            if (col.GetComponentInParent<Wall>() == null) continue;
            hitWall = true;

            if (!col.isTrigger && !(col is MeshCollider mc && !mc.convex))
            {
                Vector3 closest = col.ClosestPoint(c);
                Vector3 delta = c - closest;
                delta.y = 0f;
                float dist = delta.magnitude;
                if (dist < targetDist)
                {
                    Vector3 pushDir = dist > 0.0001f ? delta / dist : -transform.forward;
                    Vector3 push = pushDir * (targetDist - dist);
                    transform.position += push;
                    c += push;
                }
            }
        }

        if (hitWall)
        {
            wallTouchTimer += dt;
            TouchingWall = wallTouchTimer >= wallCrashDelay;
        }
        else
        {
            wallTouchTimer = 0f;
            TouchingWall = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.TransformPoint(bodyCenter), wallHitRadius);

        float dataScale = (Data != null ? Mathf.Max(0.1f, Data.modelScale) : 1f) * (Owned != null ? Owned.SizeScaleMultiplier : 1f);
        Vector3 eatLocal = bodyCenter + (Data != null ? Data.eatCenterOffset * dataScale : new Vector3(0f, 0f, 1.4f));
        float eatRadius = wallHitRadius + (Data != null ? Data.eatRadiusBonus : 0f);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.7f);
        Gizmos.DrawWireSphere(transform.TransformPoint(eatLocal), eatRadius);
    }

    // ================= AI cho Dino hoang dã =================
    // Luôn chạy về phía trước (lệch tối đa ±aiMaxOffset so với hướng đường chạy), né vật cản,
    // né Dino chậm phía trước, tránh tường 2 bên, không đi xuyên vật thể.

    Vector3 SensorOrigin()
    {
        return selfCol != null ? selfCol.bounds.center : transform.TransformPoint(bodyCenter);
    }

    // Vật rắn cần né: Obstacle / Wall / collider không phải trigger có mặt dựng đứng
    bool IsSolid(Collider c, Vector3 normal, bool checkNormal)
    {
        if (c.GetComponentInParent<PlayerController>() != null) return false;
        Obstacle ob = c.GetComponentInParent<Obstacle>();
        if (ob != null)
        {
            if (ob.IsBroken) return false;
            bool canSmash = Data == null || Data.wildSmashBreakables;
            ObjectSize mySize = EffectiveSize;
            if (canSmash && mySize > ob.size && ob.breakable) return false;
            return true;
        }
        if (c.GetComponentInParent<Wall>() != null) return true;
        if (c.isTrigger) return false;
        if (c.gameObject.layer == 3) return false;             // bỏ qua layer Ground
        return !checkNormal || Mathf.Abs(normal.y) < 0.6f;    // bỏ qua mặt đất / dốc thoải
    }

    // Khoảng trống (m) theo hướng dir. blocker = Dino chắn đường (nếu có)
    float Probe(Vector3 origin, Vector3 dir, float range, out Dino blocker, bool countDinos)
    {
        blocker = null;
        float best = range;
        const int mask = ~((1 << 2) | (1 << 6));             // bỏ Ignore Raycast + Player
        int n = Physics.SphereCastNonAlloc(origin, aiProbeRadius, dir, castBuf, range,
                                           mask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = castBuf[i];
            if (h.distance >= best) continue;
            Collider c = h.collider;
            Dino other = c.GetComponentInParent<Dino>();
            if (other == this) continue;
            if (other != null)
            {
                if (!countDinos) continue;
                ObjectSize mySize = EffectiveSize;
                ObjectSize otherSize = other.EffectiveSize;
                if (!other.IsRidden && mySize > otherSize) continue;
                // Con phía trước chạy nhanh hơn và cùng hướng thì không cần né
                if (other.CurrentSpeed >= CurrentSpeed * 0.98f &&
                    Vector3.Dot(other.transform.forward, dir) > 0.8f) continue;
            }
            else if (!IsSolid(c, h.distance <= 0f ? -dir : h.normal, true)) continue;

            if (h.distance <= 0f)
            {
                // Chỉ coi là bị chặn nếu vật thể nằm PHÍA TRƯỚC hướng dir (tránh tường bên phải làm mù cả hướng rẽ trái)
                Vector3 closest = (c is MeshCollider mc && !mc.convex) ? c.bounds.ClosestPoint(origin) : c.ClosestPoint(origin);
                Vector3 toCol = closest - origin;
                toCol.y = 0f;
                if (toCol.sqrMagnitude > 0.0001f && Vector3.Dot(toCol.normalized, dir) <= 0.05f)
                    continue;

                best = 0f;
                blocker = other;
                break;
            }

            best = h.distance;
            blocker = other;
        }
        return best;
    }

    void UpdateWildAI(float dt)
    {
        if (wasRidden)
        {
            // Vừa được thả ra: tiếp tục từ hướng hiện tại, không giật về giữa
            wasRidden = false;
            aiOffset = aiTargetOffset =
                Mathf.Clamp(Mathf.DeltaAngle(laneYaw, transform.eulerAngles.y), -aiMaxOffset, aiMaxOffset);
            aiSpeedK = aiSpeedGoal = 1f;
            aiEmergencySteer = false;
        }

        aiSenseTimer -= dt;
        if (aiSenseTimer <= 0f)
        {
            aiSenseTimer = aiSenseInterval;
            SenseWild();
        }

        float turnRate = aiEmergencySteer ? aiTurnRate * 2.5f : aiTurnRate;
        aiOffset = Mathf.MoveTowards(aiOffset, aiTargetOffset, turnRate * dt);
        heading = aiOffset;
        steer = 0f;
        aiSpeedK = Mathf.MoveTowards(aiSpeedK, aiSpeedGoal, 1.5f * dt);
    }

    void SenseWild()
    {
        Vector3 origin = SensorOrigin();
        float range = Mathf.Max(aiLookAhead, baseSpeed * 1.4f);
        Dino blocker;

        // 1. Quét quạt 5 hướng quanh hướng đường chạy, chọn hướng thoáng nhất (ưu tiên thẳng)
        float bestScore = float.MinValue, bestAngle = 0f, bestClear = 0f;
        bool allClear = true;
        for (int i = 0; i < aiAngles.Length; i++)
        {
            float a = aiAngles[i] * aiMaxOffset;
            Vector3 dir = Quaternion.Euler(0f, laneYaw + a, 0f) * Vector3.forward;
            float clear = Probe(origin, dir, range, out blocker, true);
            if (clear < range) allClear = false;

            float score = clear / range
                        - Mathf.Abs(a) / aiMaxOffset * 0.2f                     // ưu tiên chạy thẳng
                        - Mathf.Abs(a - aiOffset) / (2f * aiMaxOffset) * 0.15f; // ưu tiên giữ hướng đang đi
            if (score > bestScore) { bestScore = score; bestAngle = a; bestClear = clear; }
        }

        float target = allClear
            ? (Mathf.PerlinNoise(aiSeed, Time.time * 0.25f) * 2f - 1f) * aiWanderAngle   // lượn nhẹ
            : bestAngle;

        // 2. Tường 2 bên: càng sát càng bẻ lái vào trong
        Vector3 right = Quaternion.Euler(0f, laneYaw, 0f) * Vector3.right;
        float leftClear = Probe(origin, -right, aiSideProbe, out blocker, false);
        float rightClear = Probe(origin, right, aiSideProbe, out blocker, false);
        float wallBias = (1f - leftClear / aiSideProbe) - (1f - rightClear / aiSideProbe);
        target += wallBias * aiMaxOffset * 0.7f;

        // 3. Mép RunZone (phòng khi không có tường)
        if (runZone != null)
        {
            Vector3 p = transform.position;
            if (!runZone.ContainsPoint(p + right * aiEdgeMargin, 0f)) target = Mathf.Min(target, -aiMaxOffset * 0.5f);
            if (!runZone.ContainsPoint(p - right * aiEdgeMargin, 0f)) target = Mathf.Max(target, aiMaxOffset * 0.5f);
        }

        // 4. Điều tốc + né gắt nếu vật cản ngay sát trước mặt (tránh kẹt vào khối hộp rộng)
        float frontClear = Probe(origin, transform.forward, range, out blocker, true);
        aiEmergencySteer = frontClear < 3f || bestClear < 2.5f;
        if (bestClear < 2.5f)
        {
            // Mọi hướng ±45° đều bị chắn sát -> bẻ lái gắt sang bên thoáng hơn (tới ±80°) để lách qua vật cản
            float sideSign;
            if (Mathf.Abs(rightClear - leftClear) > 0.1f)
                sideSign = rightClear > leftClear ? 1f : -1f;
            else if (runZone != null)
                sideSign = Vector3.Dot(runZone.transform.position - transform.position, right) >= 0f ? 1f : -1f;
            else
                sideSign = 1f;
            if (Mathf.Abs(bestAngle) > 1f && bestClear > 0.5f) sideSign = Mathf.Sign(bestAngle);
            aiTargetOffset = sideSign * Mathf.Max(aiMaxOffset, 80f);
        }
        else
        {
            aiTargetOffset = Mathf.Clamp(target, -aiMaxOffset, aiMaxOffset);
        }

        aiSpeedGoal = 1f;
        if (frontClear < 5f)
        {
            aiSpeedGoal = Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(1f, 5f, frontClear));
            if (blocker != null && baseSpeed > 0.1f)
                aiSpeedGoal = Mathf.Max(aiSpeedGoal, Mathf.Min(1f, blocker.CurrentSpeed / baseSpeed) * 0.95f);
        }
    }

    // Đẩy ngang các Dino đứng quá sát nhau (chỉ chạy ở Dino hoang dã -> người chơi cưỡi Dino nhỏ va phải Dino to sẽ không bị đẩy)
    void ApplySeparation(float dt)
    {
        Quaternion laneRot = Quaternion.Euler(0f, laneYaw, 0f);
        Vector3 right = laneRot * Vector3.right;
        ObjectSize mySize = EffectiveSize;
        float largeWeight = Data != null ? Data.largerDinoPushWeight : 2.2f;
        float maxPush = Data != null ? Data.maxSeparationPush : 2.5f;
        bool yieldToCarnivore = Data != null && Data.yieldToRiddenCarnivore;
        float queryRadius = aiSeparationRadius + 2.5f;
        int n = Physics.OverlapSphereNonAlloc(transform.position, queryRadius, wallBuf,
                                              1 << gameObject.layer, QueryTriggerInteraction.Collide);
        float push = 0f;
        for (int i = 0; i < n; i++)
        {
            Dino d = wallBuf[i].GetComponentInParent<Dino>();
            if (d == null || d == this) continue;

            ObjectSize otherSize = d.EffectiveSize;
            // Dino lớn hơn không bị Dino bé hơn đẩy dịch
            if (mySize > otherSize) continue;
            // Nếu người chơi đang cưỡi Dino ăn thịt lớn hơn lao tới, không đẩy Dino nhỏ dạt ra để người chơi ăn thịt được dễ dàng (trừ khi bật yieldToRiddenCarnivore)
            if (!yieldToCarnivore && otherSize > mySize && d.IsRidden && d.Data != null && d.Data.diet == DinoDiet.Carnivore) continue;

            float effRadius = aiSeparationRadius + Mathf.Max(0f, d.wallHitRadius - 0.7f);
            Vector3 off = transform.position - d.transform.position;
            off.y = 0f;
            float dist = off.magnitude;
            if (dist >= effRadius) continue;
            float lat = Vector3.Dot(off, right);
            float side = Mathf.Abs(lat) > 0.05f ? Mathf.Sign(lat)
                       : (GetInstanceID() > d.GetInstanceID() ? 1f : -1f);
            float weight = otherSize > mySize ? largeWeight : 1f;
            push += side * (1f - dist / effRadius) * weight;
        }
        if (push != 0f)
            transform.position += right * Mathf.Clamp(push, -maxPush, maxPush) * aiSeparationSpeed * dt;
    }

    // Chống đi xuyên: đẩy bằng hình cầu thân (wallHitRadius) thay vì selfCol (3x2x4.5) để không kẹt từ xa
    void ResolvePenetration()
    {
        Vector3 c = transform.TransformPoint(bodyCenter);
        float r = Mathf.Max(0.3f, wallHitRadius);
        int n = Physics.OverlapSphereNonAlloc(c, r, wallBuf,
                                            ~((1 << 2) | (1 << 6)), QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Collider col = wallBuf[i];
            if (col == selfCol || col.GetComponentInParent<Dino>() != null) continue;
            if (!IsSolid(col, Vector3.zero, false)) continue;
            if (col.isTrigger || (col is MeshCollider mc && !mc.convex)) continue;

            Vector3 closest = col.ClosestPoint(c);
            Vector3 delta = c - closest;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < r)
            {
                Vector3 pushDir = dist > 0.0001f ? delta / dist : -transform.forward;
                Vector3 push = pushDir * (r - dist);
                transform.position += push;
                c += push;
            }
        }
    }

    void Update()
    {
        if (IsBeingEaten) return;
        float dt = Time.deltaTime;
        bool isCarnivore = Data != null && Data.diet == DinoDiet.Carnivore;
        bool herbivoreBucking = IsRidden && lowStaminaActive && !isCarnivore;
        bool carnivoreSlowing = IsRidden && lowStaminaActive && isCarnivore;

        UpdateLowStaminaFlash(dt);

        if (herbivoreBucking)
        {
            hopTimer += dt;
            if (hopDuration <= 0.001f || hopTimer >= hopDuration)
            {
                hopTimer = 0f;
                float minD = Mathf.Max(0.08f, herbivoreHopDurationMin);
                float maxD = Mathf.Max(minD, herbivoreHopDurationMax);
                hopDuration = Random.Range(minD, maxD);
                lastBuckSide = Random.value < 0.75f ? -lastBuckSide : lastBuckSide;
                currentBuckYaw = lastBuckSide
                    * Random.Range(herbivoreErraticYawAngle * 0.45f, herbivoreErraticYawAngle)
                    * Mathf.Lerp(0.75f, 1.15f, lowStamina01);
                if (herbivoreHopShakeIntensity > 0f && herbivoreHopShakeDuration > 0f && CameraRig.Instance != null)
                    CameraRig.Instance.Shake(herbivoreHopShakeIntensity, herbivoreHopShakeDuration);
            }

            float ht = Mathf.Clamp01(hopTimer / Mathf.Max(0.05f, hopDuration));
            float arc = 4f * ht * (1f - ht);
            currentHopY = arc * herbivoreHopHeight * Mathf.Clamp(visualScale, 0.7f, 2f);
            if (visualRoot != null)
            {
                float pitch = -Mathf.Sin(ht * Mathf.PI * 2f) * herbivoreHopPitchTilt;
                float roll = -Mathf.Sign(currentBuckYaw) * arc * herbivoreHopRollTilt;
                visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(pitch, 0f, roll);
            }

            float restrictedRate = Mathf.Max(0f, turnSpeed * Mathf.Clamp01(herbivoreControlScale));
            buckBaseHeading = Mathf.MoveTowardsAngle(buckBaseHeading, desiredHeading, restrictedRate * dt);
            if (limitTurnAngle) buckBaseHeading = Mathf.Clamp(buckBaseHeading, -maxSteerAngle, maxSteerAngle);
            heading = buckBaseHeading + currentBuckYaw;
        }
        else if (currentHopY != 0f || (visualRoot != null && visualRoot.localRotation != visualBaseLocalRot))
        {
            currentHopY = 0f;
            if (visualRoot != null) visualRoot.localRotation = visualBaseLocalRot;
        }

        float summonDrop = 0f;
        if (summonRiseTimer > 0f)
        {
            summonRiseTimer = Mathf.Max(0f, summonRiseTimer - dt);
            float t = 1f - summonRiseTimer / Mathf.Max(0.01f, summonRiseDuration);
            float ease = t * t * (3f - 2f * t);
            float drop = Mathf.Max(0.8f, 1.3f * visualScale);
            summonDrop = Mathf.Lerp(drop, 0f, ease);
        }
        if (visualRoot != null && (summonDrop > 0f || currentHopY > 0f || herbivoreBucking))
            visualRoot.localPosition = visualBaseLocalPos + Vector3.down * summonDrop + Vector3.up * currentHopY;
        else if (visualRoot != null && visualRoot.localPosition != visualBaseLocalPos && summonRiseTimer <= 0f)
            visualRoot.localPosition = visualBaseLocalPos;

        float dataRiddenMul = Data != null && Data.riddenSpeedMultiplier > 0.01f ? Data.riddenSpeedMultiplier : 1f;
        float steadyMul = IsRidden ? (riddenSpeedMultiplier * dataRiddenMul) : 1f;
        float boostK = steadyMul;
        if (boostTimer > 0f)
        {
            boostTimer -= dt;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(boostTimer / Mathf.Max(0.01f, boostDuration)));
            boostK = Mathf.Lerp(steadyMul, Mathf.Max(steadyMul, boostMultiplier), k);
        }
        float slowK = 1f;
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            slowK = Mathf.Lerp(1f, slowMultiplier, Mathf.Clamp01(slowTimer / 0.5f));
        }
        float bonusAdd = IsRidden ? riddenBonusSpeed : 0f;
        CurrentSpeed = (baseSpeed * boostK + bonusAdd) * slowK;

        if (carnivoreSlowing)
        {
            if (lowStaminaForceStop)
                CurrentSpeed = 0f;
            else
            {
                float curved = Mathf.Pow(lowStamina01, Mathf.Max(0.2f, carnivoreSlowdownCurve));
                CurrentSpeed *= Mathf.Lerp(1f, Mathf.Clamp01(carnivoreMinSpeedRatio), curved);
            }
        }

        bool wild = !IsRidden;
        if (wild) { UpdateWildAI(dt); CurrentSpeed *= aiSpeedK; }
        else wasRidden = true;

        if (dinoAnimator != null)
        {
            bool moving = CurrentSpeed > 0.05f;
            dinoAnimator.SetBool("IsMoving", moving);
            dinoAnimator.speed = moving
                ? Mathf.Clamp(CurrentSpeed / Mathf.Max(1f, animReferenceSpeed), 0.4f, 3f)
                : 1f;
        }

        Quaternion goal = Quaternion.Euler(0f, laneYaw + heading, 0f);
        Vector3 move;
        if (carnivoreSlowing && lowStaminaForceStop)
        {
            move = Vector3.zero;
        }
        else if (herbivoreBucking)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, goal, Mathf.Max(1f, herbivoreHopTurnSnappiness) * dt);
            move = transform.forward * CurrentSpeed + transform.right * (steer * herbivoreControlScale * steerSpeed);
        }
        else if (!wild && instantCameraFollow)
        {
            float turnMul = carnivoreSlowing ? Mathf.Lerp(1f, Mathf.Clamp01(carnivoreMinTurnScale), lowStamina01) : 1f;
            float turnRate = steerBodyTurnSpeed * turnMul;
            if (Mathf.Abs(steer) > 0.01f)
            {
                Vector3 fwd = goal * Vector3.forward;
                Vector3 right = goal * Vector3.right;
                move = fwd * CurrentSpeed + right * (steer * steerSpeed);
                if (move.sqrMagnitude > 0.01f && turnRate > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(move.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnRate * dt);
                }
            }
            else
            {
                if (turnRate > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, goal, Mathf.Max(0.1f, turnRate) * dt);
                move = transform.forward * CurrentSpeed;
            }
        }
        else
        {
            float turnMul = carnivoreSlowing ? Mathf.Lerp(1f, Mathf.Clamp01(carnivoreMinTurnScale), lowStamina01) : 1f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * turnMul * dt);
            move = transform.forward * CurrentSpeed + transform.right * steer * steerSpeed;
        }
        transform.position += move * dt;

        if (wild)
        {
            CheckWildObstacles();
            ApplySeparation(dt);
            ResolvePenetration();
        }

        if (label != null) label.hidden = IsRidden || IsPetOwned;

        if (IsRidden)
        {
            if (lowStaminaForceStop) { wallTouchTimer = 0f; TouchingWall = false; return; }
            CheckWall(dt);
            return;
        }
        wallTouchTimer = 0f;
        TouchingWall = false;

        if (Claimed && !IsPetOwned)
        {
            despawnTimer -= dt;
            if (despawnTimer <= 0f) Destroy(gameObject);
            return;
        }

        lifeTime -= dt;
        bool outside = runZone != null && !runZone.ContainsPoint(transform.position, despawnMargin);
        bool outOfWindow = false;
        if (windowPlayer != null)
        {
            float refDist = Mathf.Max(0f, Vector3.Dot(windowPlayer.position - windowOrigin, windowForward));
            float myDist = Vector3.Dot(transform.position - windowOrigin, windowForward);
            outOfWindow = myDist < refDist - windowBehind || myDist > refDist + windowAhead;
        }
        if (IsPetOwned)
        {
            if (outside || outOfWindow || transform.position.y < -100f)
            {
                if (!petReturned && MountController.Instance != null)
                {
                    petReturned = true;
                    MountController.Instance.OnPetRunDinoReturned(Owned);
                }
                Destroy(gameObject);
            }
            return;
        }
        if (lifeTime <= 0f || outside || outOfWindow || transform.position.y < -100f) Destroy(gameObject);
    }

    void LateUpdate()
    {
        UpdateSeatTransform();
    }
}
