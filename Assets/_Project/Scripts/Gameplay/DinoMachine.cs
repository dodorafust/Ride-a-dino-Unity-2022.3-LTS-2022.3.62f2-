using System;
using UnityEngine;

[Serializable]
public class MachineLevelConfig
{
    public string displayName;             // tên hiển thị ở level này (vd: Dodo, Allosaurus) - để trống sẽ hiện Level X > Y
    public Sprite icon;                    // ảnh icon ở level này (tùy chọn)
    [Tooltip("Model/Prefab gộp trọn bộ (Đế + Lò xo + Thân Dino có yên) cho level này. Kéo trực tiếp Prefab từ Project hoặc object trong Scene đều được.")]
    public GameObject model;
    [Tooltip("Tùy chọn: chỉ định riêng điểm Seat cho level này (để trống sẽ tự tìm object con tên 'Seat' bên trong Model).")]
    public Transform seatOverride;
    [Tooltip("Tên tiền tố chuỗi xương lò xo bên trong Model (ví dụ 'Spring_' cho Spring_01..Spring_07, hoặc tên 1 xương duy nhất).")]
    public string springBonePrefix = "Spring_";
    [Tooltip("Trục xoay cục bộ để uốn lò xo trước-sau (mặc định X = (1,0,0); nếu xương Blender xoay hướng khác có thể đổi sang (0,0,1) hoặc (-1,0,0)).")]
    public Vector3 springBendAxis = new Vector3(1f, 0f, 0f);
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
    public Transform seat;                     // chỗ ngồi (tự cập nhật theo Seat bên trong Model từng Level nếu có)
    public Transform spring;                   // phần đung đưa dự phòng (nếu Level không dùng model có xương Spring_)
    public Transform exitPoint;                // chỗ đứng khi rời máy
    [Tooltip("Điểm gốc đặt Model/Prefab gộp của từng Level (để trống sẽ đặt ngay tại gốc DinoMachine).")]
    public Transform modelContainer;
    [Tooltip("Ưu tiên dùng Animator (tham số bool 'IsRiding' / 'IsMoving') nếu Model của Level có gắn Animator Controller.")]
    public bool useAnimatorIfPresent = true;

    [Header("Stamina")]
    public float baseStaminaPerSecond = 1f;    // Stamina/s = base x level
    public float tickInterval = 0.5f;          // cộng + hiện số mỗi ngần này giây
    public Vector3 staminaPopupOffset = new Vector3(0f, 2.4f, 0f); // vị trí chữ +Stamina bay lên quanh người chơi

    [Header("Đung đưa & Nhún lò xo khi ngồi lên")]
    public float maxSwingAngle = 25f;          // độ
    public float swingFrequency = 1.4f;        // lần/giây
    public float rampUpTime = 0.8f;
    public float decayTime = 1.5f;             // dừng dần sau khi đứng dậy
    [Tooltip("Độ lún xuống của lò xo khi nhân vật vừa nhảy lên ngồi (m).")]
    [Min(0f)] public float landingSquashDepth = 0.22f;
    [Tooltip("Thời gian lò xo nhún xuống và bật nảy lên lại trước khi bắt đầu lắc (giây).")]
    [Min(0.05f)] public float landingSquashDuration = 0.30f;

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
    public bool showFloatingLabel = true;
    [Tooltip("Gắn SignBoardUI nếu muốn dùng bảng World Space UI thay cho chữ lơ lửng WorldLabel.")]
    public SignBoardUI boardUI;
    public string labelTitleFormat = "DINO MACHINE  Lv {0}";
    public string labelBonusFormat = "+{0} Stamina/s";
    public string labelPromptText = "Touch to ride";

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
                return "+" + NumberFormat.Small(StaminaPerSecond) + " > +" + NumberFormat.Small(levels[level].staminaPerSecond) + " Stamina/s";
            float nextSpd = baseStaminaPerSecond * (level + 1);
            return "+" + NumberFormat.Small(StaminaPerSecond) + " > +" + NumberFormat.Small(nextSpd) + " Stamina/s";
        }
    }
    public string NextDisplayName
    {
        get
        {
            if (HasLevelTable && level < levels.Length && levels[level] != null && !string.IsNullOrEmpty(levels[level].displayName))
                return levels[level].displayName;
            return null;
        }
    }
    public Sprite NextIcon
    {
        get
        {
            if (HasLevelTable && level < levels.Length && levels[level] != null && levels[level].icon != null)
                return levels[level].icon;
            return null;
        }
    }
    public event Action Changed;

    float amp, tickTimer;
    Quaternion springRest = Quaternion.identity;
    Transform defaultSeat;
    GameObject activeSpawnedModel;
    Animator activeModelAnimator;
    readonly System.Collections.Generic.List<Transform> activeSpringChain = new System.Collections.Generic.List<Transform>();
    readonly System.Collections.Generic.List<Quaternion> activeSpringChainRest = new System.Collections.Generic.List<Quaternion>();
    Vector3 topSpringRestLocalPos;
    bool hasTopSpringRestPos;
    float landingSquashTimer;
    float swingPhase;
    Vector3 activeSpringAxis = Vector3.right;
    WorldLabel label;

    void Start()
    {
        defaultSeat = seat;
        if (spring != null) springRest = spring.localRotation;
        if (boardUI == null) boardUI = GetComponentInChildren<SignBoardUI>(true);
        if (showFloatingLabel && boardUI == null && GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
        ApplyCurrentLevelVisuals(false);
        EnsureTouchColliders();
        RefreshLabel();
    }

    void EnsureTouchColliders()
    {
        // Đảm bảo luôn có Collider kích hoạt ngồi lên máy ngay cả khi ẩn Base cũ hoặc dùng Prefab FBX chưa gắn Collider
        Collider[] cols = GetComponentsInChildren<Collider>(false);
        if (cols == null || cols.Length == 0)
        {
            BoxCollider baseCol = gameObject.AddComponent<BoxCollider>();
            baseCol.isTrigger = false;
            baseCol.center = new Vector3(0f, 0.22f, 0f);
            baseCol.size = new Vector3(2.6f, 0.45f, 2.6f);

            BoxCollider triggerCol = gameObject.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = new Vector3(0f, 1.0f, 0f);
            triggerCol.size = new Vector3(3.1f, 2.0f, 3.1f);
        }
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

    void ApplyCurrentLevelVisuals(bool playBurst)
    {
        LevelVisual.Apply(levelVisuals, level);
        if (!HasLevelTable) return;

        int activeIdx = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        MachineLevelConfig active = levels[activeIdx];
        GameObject activeModelAsset = active != null ? active.model : null;
        GameObject activeVfx = active != null ? active.vfx : null;

        // Nếu Player đang ngồi trên model cũ, tạm tách Player ra trước khi hủy model cũ (tránh Player bị Destroy theo!)
        PlayerController sittingPlayer = PlayerController.Instance;
        bool reattachSittingPlayer = Occupied && sittingPlayer != null && sittingPlayer.State == PlayerState.Sitting
                                  && sittingPlayer.CurrentMachine == this;
        if (reattachSittingPlayer)
        {
            sittingPlayer.transform.SetParent(null, true);
            sittingPlayer.transform.localScale = Vector3.one;
        }

        // Dọn model Prefab đã sinh ở level trước (nếu có)
        if (activeSpawnedModel != null)
        {
            Destroy(activeSpawnedModel);
            activeSpawnedModel = null;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null) continue;
            GameObject m = levels[i].model;
            if (m != null && m.scene.IsValid() && m != activeModelAsset && m != gameObject && !transform.IsChildOf(m.transform))
                m.SetActive(false);
            GameObject v = levels[i].vfx;
            if (v != null && v.scene.IsValid() && v != activeVfx && v != gameObject && !transform.IsChildOf(v.transform))
                v.SetActive(false);
        }

        GameObject activeInstance = null;
        if (activeModelAsset != null)
        {
            if (activeModelAsset.scene.IsValid())
            {
                activeModelAsset.SetActive(true);
                activeInstance = activeModelAsset;
            }
            else
            {
                Transform parent = modelContainer != null ? modelContainer : transform;
                activeSpawnedModel = Instantiate(activeModelAsset, parent, false);
                activeSpawnedModel.name = "MachineModel_Lv" + level;
                activeInstance = activeSpawnedModel;
            }
        }

        BindActiveModelRig(activeInstance, active);

        if (reattachSittingPlayer && seat != null)
        {
            sittingPlayer.transform.SetParent(seat, true);
            sittingPlayer.transform.localPosition = Vector3.zero;
            sittingPlayer.transform.localRotation = Quaternion.identity;
        }

        if (activeVfx != null && activeVfx.scene.IsValid()) activeVfx.SetActive(true);
        if (playBurst && active != null && active.upgradeBurstVfx != null)
            active.upgradeBurstVfx.Play();
    }

    void BindActiveModelRig(GameObject modelRoot, MachineLevelConfig cfg)
    {
        activeSpringChain.Clear();
        activeSpringChainRest.Clear();
        activeModelAnimator = null;
        activeSpringAxis = (cfg != null && cfg.springBendAxis.sqrMagnitude > 0.0001f)
            ? cfg.springBendAxis.normalized
            : Vector3.right;

        if (modelRoot == null)
        {
            if (defaultSeat != null) seat = defaultSeat;
            return;
        }

        // 1. Tìm Seat riêng của model (ưu tiên seatOverride -> child tên "Seat" -> defaultSeat)
        Transform foundSeat = cfg != null ? cfg.seatOverride : null;
        if (foundSeat == null) foundSeat = FindChildIgnoreCase(modelRoot.transform, "Seat");
        if (foundSeat != null) seat = foundSeat;
        else if (defaultSeat != null) seat = defaultSeat;

        // 2. Kiểm tra Animator nếu model có sẵn Animation Controller
        Animator anim = modelRoot.GetComponentInChildren<Animator>();
        if (useAnimatorIfPresent && anim != null && anim.runtimeAnimatorController != null)
            activeModelAnimator = anim;

        // 3. Tìm chuỗi xương lò xo (ví dụ Spring_01..Spring_07) để uốn lò xo mượt bằng code khi chưa có clip Animation
        string prefix = (cfg != null && !string.IsNullOrEmpty(cfg.springBonePrefix)) ? cfg.springBonePrefix : "Spring_";
        Transform[] all = modelRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                activeSpringChain.Add(all[i]);
        }
        activeSpringChain.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < activeSpringChain.Count; i++)
            activeSpringChainRest.Add(activeSpringChain[i].localRotation);

        hasTopSpringRestPos = activeSpringChain.Count > 0;
        if (hasTopSpringRestPos)
            topSpringRestLocalPos = activeSpringChain[activeSpringChain.Count - 1].localPosition;

        // 4. Gắn toàn bộ phần nằm phía trên lò xo (Thân Dino, Yên xe, Đĩa tròn xanh lá trên đỉnh lò xo...) vào xương đỉnh lò xo (Spring_07).
        // Mọi phần đế nằm dưới đất (bounds.center.y <= splitY) và bản thân cuộn lò xo ("spring") được giữ nguyên đứng yên/uốn cong.
        if (activeSpringChain.Count > 0)
        {
            Transform topSpring = activeSpringChain[activeSpringChain.Count - 1];
            float splitY = Mathf.Lerp(modelRoot.transform.position.y, topSpring.position.y, 0.45f);
            Renderer[] rends = modelRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                Transform tr = rends[i].transform;
                string n = tr.name.ToLowerInvariant();
                if (n.Contains("spring") || n.Contains("floor") || n.Contains("ground")) continue;

                bool isExplicitBody = n.Contains("dodo") || n.Contains("rex") || n.Contains("cerato") || n.Contains("trice") || n.Contains("carno") || n.Contains("allo") || n.Contains("bronto") || n.Contains("acro") || n.Contains("juga") || n.Contains("alpha") || n.Contains("dino") || n.Contains("saddle") || n.Contains("body") || n.Contains("head");
                bool isAboveSpringMid = rends[i].bounds.center.y > splitY;
                if (!isExplicitBody && !isAboveSpringMid) continue;
                if (tr == topSpring || tr.IsChildOf(topSpring)) continue;
                tr.SetParent(topSpring, true);
            }
        }
    }

    static Transform FindChildIgnoreCase(Transform root, string targetName)
    {
        if (root == null) return null;
        if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindChildIgnoreCase(root.GetChild(i), targetName);
            if (hit != null) return hit;
        }
        return null;
    }

    void RefreshLabel()
    {
        string title = string.Format(labelTitleFormat, level);
        string bonus = string.Format(labelBonusFormat, NumberFormat.Small(StaminaPerSecond));
        if (boardUI != null)
        {
            boardUI.SetCustomInfo(title, bonus, labelPromptText, "", Color.clear);
            return;
        }
        if (label == null) return;
        label.gameObject.SetActive(showFloatingLabel);
        if (!showFloatingLabel) return;
        label.SetText(title + "\n<color=#FFE633>" + bonus + "</color>\n" + labelPromptText);
    }

    float nextEnterTime;

    public void SetOccupied(bool v)
    {
        if (Occupied && !v)
        {
            nextEnterTime = Time.time + 0.8f;
            landingSquashTimer = 0f;
        }
        Occupied = v;
        tickTimer = 0f;
    }

    public void TriggerLandingBounce()
    {
        landingSquashTimer = Mathf.Max(0.05f, landingSquashDuration);
        amp = 0f;
        swingPhase = 0f;
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
        // Hiệu ứng nhún lò xo xuống khi vừa nhảy ngồi lên máy, bật nảy lại rồi mới bắt đầu lắc
        float dipOffset = 0f;
        bool inLandingSquash = false;
        if (landingSquashTimer > 0f)
        {
            inLandingSquash = true;
            landingSquashTimer = Mathf.Max(0f, landingSquashTimer - Time.deltaTime);
            float p = 1f - (landingSquashTimer / Mathf.Max(0.05f, landingSquashDuration));
            // Lún nhanh xuống (0 -> 0.35) rồi nảy bật nhẹ lên và về 0
            if (p < 0.35f)
            {
                float k = p / 0.35f;
                dipOffset = Mathf.Sin(k * Mathf.PI * 0.5f) * landingSquashDepth;
            }
            else
            {
                float k = (p - 0.35f) / 0.65f;
                dipOffset = Mathf.Cos(k * Mathf.PI * 1.5f) * (1f - k) * landingSquashDepth;
            }
        }

        // Biên độ tăng dần sau khi nhún xong, giảm dần khi rời (như lò xo tắt dần)
        float target = (Occupied && !inLandingSquash) ? 1f : 0f;
        float rate = Time.deltaTime / Mathf.Max(0.01f, Occupied ? rampUpTime : decayTime);
        amp = Mathf.MoveTowards(amp, target, rate);
        swingPhase += Time.deltaTime * Mathf.PI * 2f * swingFrequency;

        if (activeModelAnimator != null)
        {
            activeModelAnimator.SetBool("IsRiding", Occupied);
            activeModelAnimator.SetBool("IsMoving", Occupied);
            activeModelAnimator.speed = Occupied ? 1f : 0.4f;
        }
        else if (activeSpringChain.Count > 0)
        {
            float totalAngle = maxSwingAngle * amp * Mathf.Sin(swingPhase);
            float perBoneAngle = totalAngle / activeSpringChain.Count;
            Quaternion deltaRot = Quaternion.AngleAxis(perBoneAngle, activeSpringAxis);
            for (int i = 0; i < activeSpringChain.Count; i++)
            {
                if (activeSpringChain[i] != null)
                    activeSpringChain[i].localRotation = activeSpringChainRest[i] * deltaRot;
            }
            if (hasTopSpringRestPos)
            {
                Transform topSpring = activeSpringChain[activeSpringChain.Count - 1];
                if (topSpring != null && topSpring.parent != null)
                {
                    Vector3 worldDip = Vector3.down * dipOffset;
                    topSpring.localPosition = topSpringRestLocalPos + topSpring.parent.InverseTransformVector(worldDip);
                }
            }
        }
        else if (spring != null)
        {
            float angle = maxSwingAngle * amp * Mathf.Sin(swingPhase);
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
            Vector3 pos = (p != null ? p.transform.position : transform.position) + staminaPopupOffset;
            float effectiveGain = gain * GameManager.Instance.AuraStaminaMultiplier;
            GameManager.Instance.ShowPopup(pos, "+" + NumberFormat.Small(effectiveGain), PopupKind.Stamina);
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
