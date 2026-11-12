using System.Collections.Generic;
using UnityEngine;

public class Dino : MonoBehaviour
{
    public static int Alive;

    [Header("Movement")]
    public float baseSpeed = 6f;
    public float steerSpeed = 6f;          // A/D hoặc joystick: trượt ngang
    public float turnSpeed = 120f;         // độ/giây khi xoay theo camera
    public float maxSteerAngle = 60f;
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

    void Awake()
    {
        Alive++;
        mpb = new MaterialPropertyBlock();
        CurrentSpeed = baseSpeed;
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
        heading = Mathf.Clamp(Mathf.DeltaAngle(laneYaw, cameraYaw), -maxSteerAngle, maxSteerAngle);
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

        Quaternion goal = Quaternion.Euler(0f, laneYaw + heading, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * dt);

        Vector3 move = transform.forward * CurrentSpeed + transform.right * steer * steerSpeed;
        transform.position += move * dt;

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
