using System.Collections.Generic;
using UnityEngine;

public class Dino : MonoBehaviour
{
    public static int Alive;

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

    [Header("Boost khi bị bắt")]
    public float boostMultiplier = 1.6f;
    public float boostDuration = 2.5f;

    [Header("Cảm biến tường (chỉ chạy khi đang được cưỡi)")]
    public Vector3 bodyCenter = new Vector3(0f, 1f, 0f);
    public Vector3 bodyHalfExtents = new Vector3(0.9f, 0.9f, 1.9f);

    [Header("References")]
    public Transform seat;
    public Color highlightColor = new Color(1f, 0.9f, 0.2f);
    public Vector3 labelOffset = new Vector3(0f, 3.8f, 0f);

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
    public float CurrentSpeed { get; private set; }
    public bool IsRidden { get; set; }
    public bool Claimed { get; private set; }
    public bool TouchingWall { get; private set; }

    float steer, heading, laneYaw;
    float boostTimer, slowTimer, slowMultiplier = 1f;
    float despawnTimer = 2f;
    RunZone runZone;

    readonly List<Renderer> rends = new List<Renderer>();
    MaterialPropertyBlock mpb;
    bool hasBaseTint;
    Color baseTint;
    bool highlighted;
    WorldLabel label;
    readonly Collider[] wallBuf = new Collider[32];

    // AI
    static readonly float[] aiAngles = { 0f, -0.5f, 0.5f, -1f, 1f };   // nhân với aiMaxOffset
    readonly RaycastHit[] castBuf = new RaycastHit[24];
    float aiOffset, aiTargetOffset, aiSenseTimer, aiSpeedK = 1f, aiSpeedGoal = 1f, aiSeed;
    bool wasRidden;
    Collider selfCol;

    void Awake()
    {
        Alive++;
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

    void OnDestroy() { Alive--; }

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
        Renderer[] all = visual.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < all.Length; i++)
            if (!(all[i] is ParticleSystemRenderer)) rends.Add(all[i]);

        hasBaseTint = DinoView.TryGetTint(owned, owned.data.modelPrefab == null, out baseTint);

        if (GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (label == null || Owned == null) return;
        float mine = GameManager.Instance != null ? GameManager.Instance.Stamina : 0f;
        string reqColor = mine >= Data.requiredStamina ? "55FF55" : "FF5555";
        label.SetText(
            "<color=#" + DinoColors.Hex(DinoColors.Of(Data.rarity)) + ">" + Data.rarity + "</color>\n" +
            Owned.Title + "\n" +
            "<color=#" + reqColor + ">Req. " + NumberFormat.Short(Data.requiredStamina) + "</color>");
    }

    void ApplyTint(Color c)
    {
        mpb.SetColor("_Color", c);
        mpb.SetColor("_BaseColor", c);
        for (int i = 0; i < rends.Count; i++) rends[i].SetPropertyBlock(mpb);
    }

    public void SetHighlight(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        if (on) ApplyTint(highlightColor);
        else if (hasBaseTint) ApplyTint(baseTint);
        else for (int i = 0; i < rends.Count; i++) rends[i].SetPropertyBlock(null);
    }

    public void SetSteer(float s) { steer = s; }

    public void SetHeading(float cameraYaw)
    {
        float delta = Mathf.DeltaAngle(laneYaw, cameraYaw);
        heading = limitTurnAngle ? Mathf.Clamp(delta, -maxSteerAngle, maxSteerAngle) : delta;
    }

    public void ResetHeading() { heading = 0f; }
    public void Boost() { boostTimer = boostDuration; }
    public void Claim() { Claimed = true; }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowTimer = duration;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsRidden) return;
        Obstacle ob = other.GetComponentInParent<Obstacle>();
        if (ob != null) ApplySlow(ob.slowMultiplier, ob.slowDuration);
    }

    void CheckWall()
    {
        TouchingWall = false;
        Vector3 c = transform.TransformPoint(bodyCenter);
        int n = Physics.OverlapBoxNonAlloc(c, bodyHalfExtents, wallBuf, transform.rotation,
                                           ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            if (wallBuf[i].GetComponentInParent<Wall>() != null) { TouchingWall = true; return; }
        }
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
        if (c.GetComponentInParent<Obstacle>() != null || c.GetComponentInParent<Wall>() != null) return true;
        if (c.isTrigger) return false;
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
            if (h.distance <= 0f || h.distance >= best) continue;   // 0 = đang chồng lấn sẵn (xử lý ở ResolvePenetration)
            Collider c = h.collider;
            Dino other = c.GetComponentInParent<Dino>();
            if (other == this) continue;
            if (other != null)
            {
                if (!countDinos) continue;
                // Con phía trước chạy nhanh hơn và cùng hướng thì không cần né
                if (other.CurrentSpeed >= CurrentSpeed * 0.98f &&
                    Vector3.Dot(other.transform.forward, dir) > 0.8f) continue;
            }
            else if (!IsSolid(c, h.normal, true)) continue;

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
        }

        aiSenseTimer -= dt;
        if (aiSenseTimer <= 0f)
        {
            aiSenseTimer = aiSenseInterval;
            SenseWild();
        }

        aiOffset = Mathf.MoveTowards(aiOffset, aiTargetOffset, aiTurnRate * dt);
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
        float bestScore = float.MinValue, bestAngle = 0f;
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
            if (score > bestScore) { bestScore = score; bestAngle = a; }
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

        aiTargetOffset = Mathf.Clamp(target, -aiMaxOffset, aiMaxOffset);

        // 4. Điều tốc: sắp đâm vật cản thì giảm tốc trong lúc bẻ lái, bám đuôi Dino chậm hơn
        float frontClear = Probe(origin, transform.forward, range, out blocker, true);
        aiSpeedGoal = 1f;
        if (frontClear < 5f)
        {
            aiSpeedGoal = Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(1f, 5f, frontClear));
            if (blocker != null && baseSpeed > 0.1f)
                aiSpeedGoal = Mathf.Max(aiSpeedGoal, Mathf.Min(1f, blocker.CurrentSpeed / baseSpeed) * 0.95f);
        }
    }

    // Đẩy ngang các Dino đứng quá sát nhau (chỉ theo phương ngang đường chạy -> không bao giờ bị đẩy lùi)
    void ApplySeparation(float dt)
    {
        Quaternion laneRot = Quaternion.Euler(0f, laneYaw, 0f);
        Vector3 right = laneRot * Vector3.right;
        int n = Physics.OverlapSphereNonAlloc(transform.position, aiSeparationRadius, wallBuf,
                                              1 << gameObject.layer, QueryTriggerInteraction.Collide);
        float push = 0f;
        for (int i = 0; i < n; i++)
        {
            Dino d = wallBuf[i].GetComponentInParent<Dino>();
            if (d == null || d == this) continue;
            Vector3 off = transform.position - d.transform.position;
            off.y = 0f;
            float dist = off.magnitude;
            if (dist >= aiSeparationRadius) continue;
            float lat = Vector3.Dot(off, right);
            float side = Mathf.Abs(lat) > 0.05f ? Mathf.Sign(lat)
                       : (GetInstanceID() > d.GetInstanceID() ? 1f : -1f);
            push += side * (1f - dist / aiSeparationRadius);
        }
        if (push != 0f)
            transform.position += right * Mathf.Clamp(push, -1.5f, 1.5f) * aiSeparationSpeed * dt;
    }

    // Chống đi xuyên: nếu thân Dino đang lấn vào vật cản / tường thì đẩy ra theo phương ngang
    void ResolvePenetration()
    {
        if (selfCol == null) return;
        Bounds b = selfCol.bounds;
        int n = Physics.OverlapBoxNonAlloc(b.center, b.extents, wallBuf, Quaternion.identity,
                                           ~((1 << 2) | (1 << 6)), QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Collider c = wallBuf[i];
            if (c == selfCol || c.GetComponentInParent<Dino>() != null) continue;
            if (!IsSolid(c, Vector3.zero, false)) continue;

            Vector3 dir; float dist;
            if (!Physics.ComputePenetration(selfCol, selfCol.transform.position, selfCol.transform.rotation,
                                            c, c.transform.position, c.transform.rotation, out dir, out dist))
                continue;
            Vector3 d = dir * dist;
            d.y = 0f;
            if (Mathf.Abs(dir.y) > 0.7f) continue;     // chủ yếu theo phương đứng = mặt đất, bỏ qua
            transform.position += d;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        float boostK = 1f;
        if (boostTimer > 0f)
        {
            boostTimer -= dt;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(boostTimer / boostDuration));
            boostK = Mathf.Lerp(1f, boostMultiplier, k);
        }
        float slowK = 1f;
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            slowK = Mathf.Lerp(1f, slowMultiplier, Mathf.Clamp01(slowTimer / 0.5f));
        }
        CurrentSpeed = baseSpeed * boostK * slowK;

        bool wild = !IsRidden;
        if (wild) { UpdateWildAI(dt); CurrentSpeed *= aiSpeedK; }
        else wasRidden = true;

        Quaternion goal = Quaternion.Euler(0f, laneYaw + heading, 0f);
        Vector3 move;
        if (!wild && instantCameraFollow)
        {
            Vector3 fwd = goal * Vector3.forward;
            Vector3 right = goal * Vector3.right;
            move = fwd * CurrentSpeed + right * (steer * steerSpeed);
            if (Mathf.Abs(steer) > 0.01f && move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, steerBodyTurnSpeed * dt);
            }
            else
            {
                transform.rotation = goal;
            }
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * dt);
            move = transform.forward * CurrentSpeed + transform.right * steer * steerSpeed;
        }
        transform.position += move * dt;

        if (wild)
        {
            ApplySeparation(dt);
            ResolvePenetration();
        }

        if (label != null) label.hidden = IsRidden;

        if (IsRidden) { CheckWall(); return; }
        TouchingWall = false;

        if (Claimed)
        {
            despawnTimer -= dt;
            if (despawnTimer <= 0f) Destroy(gameObject);
            return;
        }

        lifeTime -= dt;
        bool outside = runZone != null && !runZone.ContainsPoint(transform.position, despawnMargin);
        if (lifeTime <= 0f || outside || transform.position.y < -100f) Destroy(gameObject);
    }
}
