using UnityEngine;

[ExecuteAlways]
public class PlayerAuraController : MonoBehaviour
{
    public static PlayerAuraController Instance { get; private set; }

    [Header("1. Điểm gắn VFX trên cơ thể Player (Aura Slot)")]
    [Tooltip("Transform con trên người chơi dùng làm chỗ gắn mọi VFX Aura. Tự tạo nếu để trống.")]
    public Transform auraVfxAnchor;
    [Tooltip("Prefab VFX Aura mặc định dùng chung nếu trong file AuraData chưa gán VFX Prefab riêng. Nếu để trống nữa sẽ tự tạo VFX hào quang hạt sáng.")]
    public GameObject defaultVfxPrefab;
    public Vector3 globalVfxOffset = new Vector3(0f, 0.05f, 0f);
    public Vector3 globalVfxScale = Vector3.one;
    [Tooltip("Tự động nhuộm màu các ParticleSystem trong VFX theo màu Aura Color của AuraData đang dùng.")]
    public bool tintParticlesWithAuraColor = true;

    [Header("2. Tính năng: Chỉ hiện Aura khi đứng yên (Di chuyển tự tắt)")]
    [Tooltip("Bật: Chỉ hiện VFX Aura khi người chơi đứng yên; ngay khi di chuyển / nhảy / cưỡi Dino sẽ tự động tắt. Tắt: Luôn hiện VFX Aura.")]
    public bool onlyShowWhenStationary = true;
    [Tooltip("Thời gian đứng yên tối thiểu (giây) trước khi VFX Aura bật sáng trở lại.")]
    [Min(0f)] public float stationaryShowDelay = 0.05f;
    [Tooltip("Ngưỡng vận tốc di chuyển (m/s) để coi là đang di chuyển và tắt Aura.")]
    [Min(0.02f)] public float moveSpeedThreshold = 0.22f;
    [Tooltip("Bật: Khi bắt đầu di chuyển sẽ ngừng phun hạt mới (StopEmitting) để hào quang tan dần mượt mà thay vì biến mất đột ngột.")]
    public bool smoothFadeOnMove = true;
    [Tooltip("Cho phép hiển thị Aura khi đang ngồi yên trên máy tập DinoMachine.")]
    public bool showWhileSittingOnMachine = true;

    [Header("3. Xem thử / Trạng thái hiện tại")]
    [Tooltip("Aura hiện đang được Equip trên người chơi.")]
    public AuraData currentAura;

    GameObject spawnedVfxInstance;
    ParticleSystem[] cachedParticles;
    Renderer[] cachedNonParticleRenderers;
    PlayerController player;
    Vector3 lastWorldPos;
    float stationaryTimer;
    bool vfxCurrentlyEmitting;

    static Material fallbackParticleMat;
    static Texture2D radialGlowTex;

    void Awake()
    {
        if (Application.isPlaying)
            Instance = this;
        player = GetComponent<PlayerController>();
        EnsureAnchor();
        lastWorldPos = transform.position;
    }

    void OnEnable()
    {
        if (Application.isPlaying)
            Instance = this;
        EnsureAnchor();
    }

    public void EnsureAnchor()
    {
        if (auraVfxAnchor == null)
        {
            Transform existing = transform.Find("AuraVfxAnchor");
            if (existing != null)
            {
                auraVfxAnchor = existing;
            }
            else
            {
                GameObject anchorGo = new GameObject("AuraVfxAnchor");
                anchorGo.transform.SetParent(transform, false);
                anchorGo.transform.localPosition = globalVfxOffset;
                anchorGo.transform.localRotation = Quaternion.identity;
                anchorGo.transform.localScale = Vector3.one;
                auraVfxAnchor = anchorGo.transform;
            }
        }
    }

    public void ApplyAura(AuraData aura)
    {
        currentAura = aura;
        EnsureAnchor();
        ClearSpawnedVfx();

        if (aura == null || !Application.isPlaying)
            return;

        auraVfxAnchor.localPosition = globalVfxOffset + aura.vfxPositionOffset;
        auraVfxAnchor.localRotation = Quaternion.Euler(aura.vfxRotationOffset);
        auraVfxAnchor.localScale = Vector3.Scale(globalVfxScale, aura.vfxScale);

        GameObject prefabToUse = aura.vfxPrefab != null ? aura.vfxPrefab : defaultVfxPrefab;
        if (prefabToUse != null)
        {
            spawnedVfxInstance = Instantiate(prefabToUse, auraVfxAnchor, false);
            spawnedVfxInstance.name = "AuraVFX_" + aura.displayName;
            spawnedVfxInstance.transform.localPosition = Vector3.zero;
            spawnedVfxInstance.transform.localRotation = Quaternion.identity;
            spawnedVfxInstance.transform.localScale = Vector3.one;
        }
        else
        {
            spawnedVfxInstance = BuildProceduralAuraVfx(auraVfxAnchor, aura);
        }

        cachedParticles = spawnedVfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        if (tintParticlesWithAuraColor)
            TintParticleSystems(cachedParticles, aura.auraColor);

        Renderer[] allRends = spawnedVfxInstance.GetComponentsInChildren<Renderer>(true);
        int nonPsCount = 0;
        for (int i = 0; i < allRends.Length; i++)
            if (!(allRends[i] is ParticleSystemRenderer)) nonPsCount++;

        cachedNonParticleRenderers = new Renderer[nonPsCount];
        int idx = 0;
        for (int i = 0; i < allRends.Length; i++)
            if (!(allRends[i] is ParticleSystemRenderer)) cachedNonParticleRenderers[idx++] = allRends[i];

        vfxCurrentlyEmitting = true;
        stationaryTimer = stationaryShowDelay;
        SetVfxEmissionActive(true, true);
    }

    void ClearSpawnedVfx()
    {
        if (spawnedVfxInstance != null)
        {
            if (Application.isPlaying) Destroy(spawnedVfxInstance);
            else DestroyImmediate(spawnedVfxInstance);
            spawnedVfxInstance = null;
        }
        cachedParticles = null;
        cachedNonParticleRenderers = null;
        vfxCurrentlyEmitting = false;
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            EnsureAnchor();
            return;
        }

        if (currentAura == null || spawnedVfxInstance == null)
        {
            lastWorldPos = transform.position;
            return;
        }

        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        Vector3 delta = transform.position - lastWorldPos;
        delta.y = 0f;
        float horizSpeed = delta.magnitude / dt;
        lastWorldPos = transform.position;

        bool requireStationary = onlyShowWhenStationary;
        if (currentAura.stationaryVisibilityMode == AuraStationaryMode.OnlyWhenStationary)
            requireStationary = true;
        else if (currentAura.stationaryVisibilityMode == AuraStationaryMode.AlwaysVisible)
            requireStationary = false;

        bool shouldShow = true;
        if (player != null)
        {
            if (player.State == PlayerState.Dead)
            {
                shouldShow = false;
            }
            else if (requireStationary)
            {
                bool mountedPet = MountController.Instance != null && MountController.Instance.Mounted;
                bool ridingOrAir = player.State == PlayerState.Riding
                                || player.State == PlayerState.Jumping
                                || player.State == PlayerState.Catching
                                || mountedPet;
                bool sittingForbidden = player.State == PlayerState.Sitting && !showWhileSittingOnMachine;
                bool hasInputMove = GameInput.Move.sqrMagnitude > 0.01f || Input.GetButton("Jump");
                bool isMoving = ridingOrAir || sittingForbidden || hasInputMove || horizSpeed > moveSpeedThreshold;

                if (isMoving)
                {
                    stationaryTimer = 0f;
                    shouldShow = false;
                }
                else
                {
                    stationaryTimer += dt;
                    shouldShow = stationaryTimer >= stationaryShowDelay;
                }
            }
        }

        if (shouldShow != vfxCurrentlyEmitting)
        {
            vfxCurrentlyEmitting = shouldShow;
            SetVfxEmissionActive(shouldShow, false);
        }
    }

    void SetVfxEmissionActive(bool active, bool forceImmediate)
    {
        if (spawnedVfxInstance == null) return;

        if (cachedParticles != null && cachedParticles.Length > 0)
        {
            for (int i = 0; i < cachedParticles.Length; i++)
            {
                ParticleSystem ps = cachedParticles[i];
                if (ps == null) continue;
                if (active)
                {
                    if (!ps.isEmitting) ps.Play(true);
                }
                else
                {
                    ParticleSystemStopBehavior stopMode = (!forceImmediate && smoothFadeOnMove)
                        ? ParticleSystemStopBehavior.StopEmitting
                        : ParticleSystemStopBehavior.StopEmittingAndClear;
                    ps.Stop(true, stopMode);
                }
            }
        }

        if (cachedNonParticleRenderers != null)
        {
            for (int i = 0; i < cachedNonParticleRenderers.Length; i++)
            {
                if (cachedNonParticleRenderers[i] != null)
                    cachedNonParticleRenderers[i].enabled = active;
            }
        }
    }

    static void TintParticleSystems(ParticleSystem[] systems, Color auraColor)
    {
        if (systems == null) return;
        Color bright = Color.Lerp(auraColor, Color.white, 0.32f);
        bright.a = 0.92f;
        Color deep = auraColor;
        deep.a = 0.75f;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            var main = systems[i].main;
            main.startColor = new ParticleSystem.MinMaxGradient(deep, bright);
        }
    }

    static Material GetFallbackParticleMaterial()
    {
        if (fallbackParticleMat != null) return fallbackParticleMat;

        if (radialGlowTex == null)
        {
            int size = 64;
            radialGlowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            radialGlowTex.wrapMode = TextureWrapMode.Clamp;
            radialGlowTex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            radialGlowTex.SetPixels32(px);
            radialGlowTex.Apply();
        }

        Shader sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        fallbackParticleMat = new Material(sh);
        fallbackParticleMat.mainTexture = radialGlowTex;
        return fallbackParticleMat;
    }

    // Tự động dựng VFX hào quang 2 lớp (luồng khí bốc lên quanh người + vòng xoáy dưới chân giống ảnh mẫu 3)
    // khi người dùng chưa gắn Prefab VFX riêng vào AuraData.
    static GameObject BuildProceduralAuraVfx(Transform parent, AuraData aura)
    {
        GameObject root = new GameObject("AuraVFX_Procedural_" + aura.displayName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Material pMat = GetFallbackParticleMaterial();
        Color col = aura.auraColor;
        Color lightCol = Color.Lerp(col, Color.white, 0.45f);

        // 1. Luồng hào quang bốc lên quanh thân nhân vật
        GameObject flamesGo = new GameObject("RisingAuraFlames");
        flamesGo.transform.SetParent(root.transform, false);
        flamesGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        ParticleSystem flamesPs = flamesGo.AddComponent<ParticleSystem>();
        var main = flamesPs.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(col, lightCol);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = flamesPs.emission;
        emission.rateOverTime = 28f;

        var shape = flamesPs.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.55f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colOverLife = flamesPs.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(lightCol, 0f), new GradientColorKey(col, 0.55f), new GradientColorKey(col, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.22f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = grad;

        var sizeOverLife = flamesPs.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.15f)
        );
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer flamesRend = flamesGo.GetComponent<ParticleSystemRenderer>();
        flamesRend.sharedMaterial = pMat;
        flamesRend.renderMode = ParticleSystemRenderMode.Stretch;
        flamesRend.lengthScale = 1.85f;
        flamesRend.velocityScale = 0.25f;

        // 2. Vòng sóng năng lượng tỏa ra dưới mặt đất
        GameObject ringGo = new GameObject("GroundAuraWaves");
        ringGo.transform.SetParent(root.transform, false);
        ringGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        ParticleSystem ringPs = ringGo.AddComponent<ParticleSystem>();
        var rMain = ringPs.main;
        rMain.loop = true;
        rMain.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
        rMain.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        rMain.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
        rMain.startColor = new ParticleSystem.MinMaxGradient(col, lightCol);
        rMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        rMain.maxParticles = 40;

        var rEmit = ringPs.emission;
        rEmit.rateOverTime = 22f;

        var rShape = ringPs.shape;
        rShape.shapeType = ParticleSystemShapeType.Circle;
        rShape.radius = 0.25f;
        rShape.rotation = new Vector3(90f, 0f, 0f);

        var rCol = ringPs.colorOverLifetime;
        rCol.enabled = true;
        rCol.color = grad;

        ParticleSystemRenderer ringRend = ringGo.GetComponent<ParticleSystemRenderer>();
        ringRend.sharedMaterial = pMat;

        return root;
    }
}
