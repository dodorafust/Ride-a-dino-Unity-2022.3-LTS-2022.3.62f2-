using System.Collections.Generic;
using UnityEngine;

// Gắn lên đá / cây / vật cản bất kỳ có Collider.
// - Dino bé hoặc bằng vật cản: bị làm chậm, rung nhẹ màn hình và đi xuyên qua.
// - Dino lớn hơn vật cản: húc vỡ văng ra các khối block giả theo bảng màu debrisColors, rơi xuống đất rồi mờ tan biến dần + tự hồi phục sau 5..10s.
public class Obstacle : MonoBehaviour
{
    public ObjectSize size = ObjectSize.Medium;
    [Range(0.1f, 1f)] public float slowMultiplier = 0.4f;
    public float slowDuration = 1.5f;

    [Header("Hiệu ứng vỡ (Mảnh Block giả)")]
    public bool breakable = true;
    [Min(1)] public int debrisCount = 6;
    public Vector2 debrisScaleRange = new Vector2(0.32f, 0.65f);
    [Tooltip("Ưu tiên dùng các Material có sẵn ở đây (kéo Material đá/gỗ/lá vào). Nếu để trống sẽ dùng bảng màu Debris Colors bên dưới.")]
    public Material[] debrisMaterials;
    [Tooltip("Danh sách màu của các khối mảnh vỡ (dùng khi Debris Materials để trống).")]
    public Color[] debrisColors = new Color[]
    {
        new Color(0.55f, 0.56f, 0.58f, 1f),
        new Color(0.42f, 0.43f, 0.45f, 1f)
    };
    [Tooltip("Để trống sẽ tự dùng khối Cube chuẩn của Unity.")]
    public Mesh customDebrisMesh;
    public float shatterForce = 3.5f;
    public float shatterUpward = 2.2f;
    public float shatterSpin = 4f;
    public float debrisLifeTime = 2.0f;
    public float debrisFadeDuration = 0.8f;
    public float cameraShakeIntensity = 0.35f;
    public float cameraShakeDuration = 0.22f;

    [Header("Phân vùng Thân / Tán (bật cho Cây)")]
    public bool splitTrunkAndCanopy = false;
    [Tooltip("Khoảng chiều cao của vùng Thân (0 = đáy gốc, 1 = đỉnh ngọn). Vd: 0.05 -> 0.45")]
    public Vector2 trunkHeightRange = new Vector2(0.05f, 0.45f);
    [Range(0.05f, 1f)] public float trunkHorizontalRadius = 0.25f;
    [Min(1)] public int trunkDebrisCount = 3;
    public Material trunkMaterial;
    public Color trunkColor = new Color(0.45f, 0.28f, 0.14f, 1f);
    public Vector3 trunkPieceScale = new Vector3(0.32f, 0.78f, 0.32f);
    [Tooltip("Khoảng chiều cao của vùng Tán (0 = đáy gốc, 1 = đỉnh ngọn). Vd: 0.55 -> 0.95")]
    public Vector2 canopyHeightRange = new Vector2(0.55f, 0.95f);
    [Range(0.05f, 1f)] public float canopyHorizontalRadius = 0.75f;
    [Min(1)] public int canopyDebrisCount = 5;
    public Material canopyMaterial;
    public Color canopyColor = new Color(0.22f, 0.72f, 0.22f, 1f);
    public Vector3 canopyPieceScale = new Vector3(0.75f, 0.48f, 0.75f);

    [Header("Dùng mảnh vỡ gốc của Model (tùy chọn)")]
    [Tooltip("Tích vào để dùng chính các mảnh con của model 3D thay vì khối Block giả.")]
    public bool useModelPieces = false;
    [Range(0.1f, 1f)] public float modelPiecesDropRatio = 0.75f;
    [Range(0.2f, 1.5f)] public float modelPieceScale = 0.78f;

    [Header("Rung màn hình khi tông (bị làm chậm, không vỡ)")]
    public float slowShakeIntensity = 0.2f;
    public float slowShakeDuration = 0.16f;

    [Header("Tự hồi phục sau khi vỡ")]
    public bool autoRespawn = true;
    public float respawnMinDelay = 5f;
    public float respawnMaxDelay = 10f;

    public bool IsBroken { get; private set; }

    static Mesh sharedCubeMesh;
    static Shader sharedStandardShader;

    Collider[] cachedCols;
    MeshRenderer[] cachedRends;
    Material[][] origSharedMats;
    Material fallbackMat;
    bool[] origColEnabled;
    bool[] origRendEnabled;
    Vector3 origScale;
    float respawnTimer;
    float popTimer;
    readonly List<Collider> debrisCols = new List<Collider>();
    readonly List<int> activePieceIndices = new List<int>();
    readonly List<Vector3> activePieceCenters = new List<Vector3>();

    static Mesh GetCubeMesh()
    {
        if (sharedCubeMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sharedCubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }
        return sharedCubeMesh;
    }

    static Shader GetStandardShader()
    {
        if (sharedStandardShader == null)
        {
            sharedStandardShader = Shader.Find("Standard");
            if (sharedStandardShader == null)
                sharedStandardShader = Shader.Find("Legacy Shaders/Diffuse");
        }
        return sharedStandardShader;
    }

    void Awake()
    {
        origScale = transform.localScale;
        cachedCols = GetComponentsInChildren<Collider>(true);
        cachedRends = GetComponentsInChildren<MeshRenderer>(true);
        origSharedMats = new Material[cachedRends.Length][];
        origColEnabled = new bool[cachedCols.Length];
        for (int i = 0; i < cachedCols.Length; i++) origColEnabled[i] = cachedCols[i].enabled;
        origRendEnabled = new bool[cachedRends.Length];
        for (int i = 0; i < cachedRends.Length; i++)
        {
            origRendEnabled[i] = cachedRends[i].enabled;
            origSharedMats[i] = cachedRends[i].sharedMaterials;
            if (fallbackMat == null && cachedRends[i].sharedMaterial != null)
                fallbackMat = cachedRends[i].sharedMaterial;
        }
    }

    void Update()
    {
        if (IsBroken)
        {
            if (!autoRespawn) return;
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f) Restore();
            return;
        }

        if (popTimer > 0f)
        {
            popTimer = Mathf.Max(0f, popTimer - Time.deltaTime);
            float t = 1f - (popTimer / 0.25f);
            transform.localScale = Vector3.Lerp(origScale * 0.1f, origScale, t * t * (3f - 2f * t));
        }
    }

    void Restore()
    {
        IsBroken = false;
        popTimer = 0.25f;
        transform.localScale = origScale * 0.1f;
        for (int i = 0; i < cachedRends.Length; i++)
            if (cachedRends[i] != null) cachedRends[i].enabled = origRendEnabled[i];
        for (int i = 0; i < cachedCols.Length; i++)
            if (cachedCols[i] != null) cachedCols[i].enabled = origColEnabled[i];
    }

    Bounds ComputeWorldBounds()
    {
        bool hasBounds = false;
        Bounds b = new Bounds(transform.position + Vector3.up * 0.8f, Vector3.one);
        for (int i = 0; i < cachedRends.Length; i++)
        {
            MeshRenderer mr = cachedRends[i];
            if (mr == null || !mr.enabled) continue;
            if (!hasBounds) { b = mr.bounds; hasBounds = true; }
            else b.Encapsulate(mr.bounds);
        }
        return b;
    }

    float GetSizeScaleMultiplier()
    {
        switch (size)
        {
            case ObjectSize.Small:  return 0.85f;
            case ObjectSize.Large:  return 1.25f;
            case ObjectSize.Giant:  return 1.55f;
            default:                return 1.0f;
        }
    }

    Bounds BuildSubZoneBounds(Bounds total, Vector2 heightRange01, float horizRadiusScale)
    {
        float minT = Mathf.Clamp01(Mathf.Min(heightRange01.x, heightRange01.y));
        float maxT = Mathf.Clamp01(Mathf.Max(heightRange01.x, heightRange01.y));
        float minY = Mathf.Lerp(total.min.y, total.max.y, minT);
        float maxY = Mathf.Lerp(total.min.y, total.max.y, maxT);
        float h = Mathf.Max(0.2f, maxY - minY);
        Vector3 center = new Vector3(total.center.x, (minY + maxY) * 0.5f, total.center.z);
        Vector3 size = new Vector3(
            Mathf.Max(0.2f, total.size.x * horizRadiusScale),
            h,
            Mathf.Max(0.2f, total.size.z * horizRadiusScale));
        return new Bounds(center, size);
    }

    void OnDrawGizmosSelected()
    {
        if (!splitTrunkAndCanopy) return;
        MeshRenderer[] rends = GetComponentsInChildren<MeshRenderer>(true);
        if (rends == null || rends.Length == 0) return;
        Bounds total = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) total.Encapsulate(rends[i].bounds);

        Bounds tb = BuildSubZoneBounds(total, trunkHeightRange, trunkHorizontalRadius);
        Gizmos.color = new Color(0.65f, 0.38f, 0.15f, 0.9f);
        Gizmos.DrawWireCube(tb.center, tb.size);

        Bounds cb = BuildSubZoneBounds(total, canopyHeightRange, canopyHorizontalRadius);
        Gizmos.color = new Color(0.2f, 0.9f, 0.25f, 0.9f);
        Gizmos.DrawWireCube(cb.center, cb.size);
    }

    public void Shatter(Vector3 hitPos, Vector3 dinoDir, float dinoSpeed, bool shakeCamera = true)
    {
        if (IsBroken || !breakable) return;
        IsBroken = true;

        if (shakeCamera && CameraRig.Instance != null)
            CameraRig.Instance.Shake(cameraShakeIntensity, cameraShakeDuration);

        Bounds bounds = ComputeWorldBounds();
        Bounds trunkBounds = default, canopyBounds = default;
        if (!useModelPieces && splitTrunkAndCanopy)
        {
            trunkBounds = BuildSubZoneBounds(bounds, trunkHeightRange, trunkHorizontalRadius);
            canopyBounds = BuildSubZoneBounds(bounds, canopyHeightRange, canopyHorizontalRadius);
        }

        activePieceIndices.Clear();
        activePieceCenters.Clear();
        if (useModelPieces)
        {
            for (int i = 0; i < cachedRends.Length; i++)
            {
                MeshRenderer mr = cachedRends[i];
                if (mr == null || !mr.enabled) continue;
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                activePieceIndices.Add(i);
                activePieceCenters.Add(mr.bounds.center);
            }
        }

        for (int i = 0; i < cachedCols.Length; i++)
            if (cachedCols[i] != null) cachedCols[i].enabled = false;
        for (int i = 0; i < cachedRends.Length; i++)
            if (cachedRends[i] != null) cachedRends[i].enabled = false;

        Vector3 fwd = dinoDir.sqrMagnitude > 0.001f ? dinoDir.normalized : transform.forward;
        int playerLayer = LayerMask.NameToLayer("Player");
        int debrisLayer = playerLayer >= 0 ? playerLayer : 2;
        Mesh blockMesh = customDebrisMesh != null ? customDebrisMesh : GetCubeMesh();
        float sizeMul = GetSizeScaleMultiplier();
        debrisCols.Clear();

        if (useModelPieces && activePieceIndices.Count > 0)
        {
            SpawnModelPiecesDebris(bounds.center, fwd, debrisLayer);
        }
        else if (splitTrunkAndCanopy)
        {
            SpawnZoneDebris(trunkBounds, Mathf.Max(1, trunkDebrisCount), trunkPieceScale * sizeMul,
                            CreateFixedDebrisMaterial(trunkMaterial, trunkColor), blockMesh, debrisLayer,
                            fwd, 0.45f, 1.0f, 1.0f, "Trunk");
            SpawnZoneDebris(canopyBounds, Mathf.Max(1, canopyDebrisCount), canopyPieceScale * sizeMul,
                            CreateFixedDebrisMaterial(canopyMaterial, canopyColor), blockMesh, debrisLayer,
                            fwd, 0.15f, 0.45f, 0.35f, "Canopy");
        }
        else
        {
            int count = Mathf.Max(1, debrisCount);
            float minS = Mathf.Max(0.1f, Mathf.Min(debrisScaleRange.x, debrisScaleRange.y)) * sizeMul;
            float maxS = Mathf.Max(minS, Mathf.Max(debrisScaleRange.x, debrisScaleRange.y) * sizeMul);
            Vector3 ext = Vector3.Max(bounds.extents * 0.55f, new Vector3(0.35f, 0.35f, 0.35f));

            for (int i = 0; i < count; i++)
            {
                float angle = (i * 137.5f + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                Vector3 radialDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float radiusFrac = Random.Range(0.25f, 0.85f);
                float heightFrac = (count > 1) ? ((float)i / (count - 1)) : 0.5f;
                float spawnY = Mathf.Lerp(bounds.min.y + 0.25f, Mathf.Max(bounds.min.y + 0.4f, bounds.max.y - 0.15f), heightFrac);
                Vector3 spawnPos = new Vector3(
                    bounds.center.x + radialDir.x * ext.x * radiusFrac,
                    spawnY,
                    bounds.center.z + radialDir.z * ext.z * radiusFrac);
                Vector3 scale = new Vector3(Random.Range(minS, maxS), Random.Range(minS, maxS), Random.Range(minS, maxS));
                SpawnSingleDebris(spawnPos, scale, CreateDebrisMaterial(i), blockMesh, debrisLayer,
                                  fwd, radialDir, 0.35f, 1.0f, 1.0f, "Block_" + i);
            }
        }

        for (int a = 0; a < debrisCols.Count; a++)
            for (int b = a + 1; b < debrisCols.Count; b++)
                if (debrisCols[a] != null && debrisCols[b] != null)
                    Physics.IgnoreCollision(debrisCols[a], debrisCols[b]);

        if (autoRespawn)
            respawnTimer = Random.Range(Mathf.Min(respawnMinDelay, respawnMaxDelay),
                                        Mathf.Max(respawnMinDelay, respawnMaxDelay));
        else
            Destroy(gameObject);
    }

    void SpawnModelPiecesDebris(Vector3 rockCenter, Vector3 fwd, int debrisLayer)
    {
        int total = activePieceIndices.Count;
        int dropCount = Mathf.Clamp(Mathf.RoundToInt(total * modelPiecesDropRatio), 1, total);

        // Xáo trộn ngẫu nhiên để chọn 70..80% số mảnh của model
        for (int i = 0; i < total; i++)
        {
            int swap = Random.Range(i, total);
            int tmpIdx = activePieceIndices[i];
            activePieceIndices[i] = activePieceIndices[swap];
            activePieceIndices[swap] = tmpIdx;

            Vector3 tmpCenter = activePieceCenters[i];
            activePieceCenters[i] = activePieceCenters[swap];
            activePieceCenters[swap] = tmpCenter;
        }

        float scaleMul = Mathf.Max(0.1f, modelPieceScale);
        for (int k = 0; k < dropCount; k++)
        {
            int idx = activePieceIndices[k];
            Vector3 origCenter = activePieceCenters[k];
            MeshRenderer mr = cachedRends[idx];
            MeshFilter mf = mr != null ? mr.GetComponent<MeshFilter>() : null;
            if (mr == null || mf == null || mf.sharedMesh == null) continue;

            GameObject piece = new GameObject(mr.name + "_ModelDebris");
            piece.layer = debrisLayer;
            piece.transform.SetPositionAndRotation(mr.transform.position, mr.transform.rotation);
            piece.transform.localScale = mr.transform.lossyScale * scaleMul;

            MeshFilter pmf = piece.AddComponent<MeshFilter>();
            pmf.sharedMesh = mf.sharedMesh;

            MeshRenderer pmr = piece.AddComponent<MeshRenderer>();
            pmr.sharedMaterials = origSharedMats[idx] != null ? origSharedMats[idx] : mr.sharedMaterials;
            pmr.shadowCastingMode = mr.shadowCastingMode;
            pmr.receiveShadows = mr.receiveShadows;

            // Giữ mảnh vỡ ở đúng vị trí hình học thực ban đầu sau khi thu nhỏ modelPieceScale (kể cả khi Pivot gốc lệch về (0,0,0))
            piece.transform.position += (origCenter - pmr.bounds.center);

            BoxCollider bc = piece.AddComponent<BoxCollider>();
            bc.center = mf.sharedMesh.bounds.center;
            bc.size = mf.sharedMesh.bounds.size * 0.8f;
            debrisCols.Add(bc);

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 0.4f;
            rb.angularDrag = 0.5f;
            rb.centerOfMass = bc.center;

            float radialAngle = (k * 360f / Mathf.Max(1, dropCount) + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            Vector3 radialDir = new Vector3(Mathf.Cos(radialAngle), 0f, Mathf.Sin(radialAngle));
            Vector3 outDir = origCenter - rockCenter;
            outDir.y = 0f;
            outDir = outDir.sqrMagnitude > 0.01f ? (outDir.normalized * 0.7f + radialDir * 0.3f).normalized : radialDir;

            float sideSpeed = Mathf.Max(1.8f, shatterForce) * Random.Range(0.85f, 1.3f);
            Vector3 impulse = fwd * (shatterForce * 0.35f)
                            + outDir * sideSpeed
                            + Vector3.up * (shatterUpward * Random.Range(0.8f, 1.2f));
            rb.AddForce(impulse, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * Mathf.Max(2.2f, shatterSpin), ForceMode.VelocityChange);

            DebrisFade fade = piece.AddComponent<DebrisFade>();
            fade.Init(debrisLifeTime, debrisFadeDuration);
        }
    }

    void SpawnZoneDebris(Bounds b, int count, Vector3 baseScale, Material templateMat, Mesh blockMesh, int layer,
                         Vector3 fwd, float fwdWeight, float sideWeight, float upWeight, string tag)
    {
        Vector3 ext = b.extents;
        for (int i = 0; i < count; i++)
        {
            float angle = (i * 137.5f + Random.Range(-22f, 22f)) * Mathf.Deg2Rad;
            Vector3 radialDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float r = Random.Range(0.15f, 0.85f);
            float y = Mathf.Lerp(b.min.y, b.max.y,
                                 count > 1 ? (float)i / (count - 1) : 0.5f);
            Vector3 spawnPos = new Vector3(b.center.x + radialDir.x * ext.x * r, y, b.center.z + radialDir.z * ext.z * r);
            Vector3 scale = new Vector3(
                baseScale.x * Random.Range(0.82f, 1.18f),
                baseScale.y * Random.Range(0.82f, 1.18f),
                baseScale.z * Random.Range(0.82f, 1.18f));
            Material pieceMat = new Material(templateMat);
            SpawnSingleDebris(spawnPos, scale, pieceMat, blockMesh, layer, fwd, radialDir, fwdWeight, sideWeight, upWeight, tag + "_" + i);
        }
        Destroy(templateMat);
    }

    void SpawnSingleDebris(Vector3 pos, Vector3 scale, Material pieceMat, Mesh blockMesh, int layer,
                           Vector3 fwd, Vector3 radialDir, float fwdWeight, float sideWeight, float upWeight, string suffix)
    {
        GameObject piece = new GameObject(name + "_" + suffix);
        piece.layer = layer;
        piece.transform.position = pos;
        piece.transform.rotation = Random.rotation;
        piece.transform.localScale = scale;

        MeshFilter pmf = piece.AddComponent<MeshFilter>();
        pmf.sharedMesh = blockMesh;

        MeshRenderer pmr = piece.AddComponent<MeshRenderer>();
        pmr.sharedMaterial = pieceMat;

        BoxCollider bc = piece.AddComponent<BoxCollider>();
        bc.size = Vector3.one * 0.85f;
        debrisCols.Add(bc);

        Rigidbody rb = piece.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.4f;
        rb.angularDrag = 0.5f;

        float sideSpeed = Mathf.Max(1.5f, shatterForce) * sideWeight * Random.Range(0.8f, 1.25f);
        Vector3 impulse = fwd * (shatterForce * fwdWeight)
                        + radialDir * sideSpeed
                        + Vector3.up * (shatterUpward * upWeight * Random.Range(0.8f, 1.25f));
        rb.AddForce(impulse, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * Mathf.Max(2f, shatterSpin), ForceMode.VelocityChange);

        DebrisFade fade = piece.AddComponent<DebrisFade>();
        fade.Init(debrisLifeTime, debrisFadeDuration, pieceMat);
    }

    Material CreateFixedDebrisMaterial(Material overrideMat, Color color)
    {
        if (overrideMat != null) return new Material(overrideMat);
        Material m = fallbackMat != null ? new Material(fallbackMat) : new Material(GetStandardShader());
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        color.a = 1f;
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        return m;
    }

    Material CreateDebrisMaterial(int index)
    {
        if (debrisMaterials != null && debrisMaterials.Length > 0)
        {
            Material matSrc = debrisMaterials[index % debrisMaterials.Length];
            if (matSrc != null) return new Material(matSrc);
        }

        Material m = fallbackMat != null ? new Material(fallbackMat) : new Material(GetStandardShader());
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        if (debrisColors != null && debrisColors.Length > 0)
        {
            Color c = debrisColors[index % debrisColors.Length];
            c.a = 1f;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }
        return m;
    }

    public static void SpawnDinoEatenDebris(Vector3 center, Vector3 forward, float scaleMul, int debrisCount = 6)
    {
        int count = Mathf.Max(0, debrisCount);
        if (count == 0) return;

        Mesh blockMesh = GetCubeMesh();
        Shader sh = GetStandardShader();
        int playerLayer = LayerMask.NameToLayer("Player");
        int debrisLayer = playerLayer >= 0 ? playerLayer : 2;
        Vector3 fwd = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        float sMul = Mathf.Clamp(scaleMul, 0.7f, 2.2f);
        Collider[] cols = new Collider[count];

        for (int i = 0; i < count; i++)
        {
            GameObject piece = new GameObject("DinoMeatDebris_" + i);
            piece.layer = debrisLayer;
            float angle = (i * 137.5f + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            Vector3 radialDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            piece.transform.position = center + radialDir * Random.Range(0.15f, 0.5f) + Vector3.up * Random.Range(0.1f, 0.6f);
            piece.transform.rotation = Random.rotation;
            float s = Random.Range(0.28f, 0.45f) * sMul;
            piece.transform.localScale = new Vector3(s, s, s);

            MeshFilter pmf = piece.AddComponent<MeshFilter>();
            pmf.sharedMesh = blockMesh;

            Material mat = new Material(sh);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);
            Color c = (i % 3 == 0)
                ? new Color(0.92f, 0.88f, 0.82f, 1f)
                : new Color(Random.Range(0.75f, 0.92f), Random.Range(0.15f, 0.25f), Random.Range(0.15f, 0.22f), 1f);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);

            MeshRenderer pmr = piece.AddComponent<MeshRenderer>();
            pmr.sharedMaterial = mat;

            BoxCollider bc = piece.AddComponent<BoxCollider>();
            bc.size = Vector3.one * 0.85f;
            cols[i] = bc;

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 0.4f;
            rb.angularDrag = 0.5f;
            Vector3 impulse = fwd * Random.Range(1.5f, 3.2f)
                            + radialDir * Random.Range(2.8f, 4.8f)
                            + Vector3.up * Random.Range(3.2f, 5.2f);
            rb.AddForce(impulse, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);

            DebrisFade fade = piece.AddComponent<DebrisFade>();
            fade.Init(1.6f, 0.6f, mat);
        }

        for (int a = 0; a < count; a++)
            for (int b = a + 1; b < count; b++)
                if (cols[a] != null && cols[b] != null)
                    Physics.IgnoreCollision(cols[a], cols[b]);
    }
}

public class DebrisFade : MonoBehaviour
{
    float lifeTime = 2f;
    float fadeDuration = 0.8f;
    float timer;
    bool fading;
    Material ownedMat;
    Color baseColor = Color.white;
    readonly List<Material> extraMats = new List<Material>();
    readonly List<Color> extraBaseColors = new List<Color>();

    public void Init(float totalLife, float fadeTime, Material mat = null)
    {
        lifeTime = Mathf.Max(0.2f, totalLife);
        fadeDuration = Mathf.Clamp(fadeTime, 0.1f, lifeTime);
        ownedMat = mat;
        if (ownedMat != null)
        {
            baseColor = ownedMat.HasProperty("_Color") ? ownedMat.color
                      : (ownedMat.HasProperty("_BaseColor") ? ownedMat.GetColor("_BaseColor") : Color.white);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        float fadeStart = lifeTime - fadeDuration;
        if (timer < fadeStart) return;

        if (!fading)
        {
            fading = true;
            if (ownedMat != null)
            {
                MaterialFadeUtil.ConfigureFadeMaterial(ownedMat);
            }
            else
            {
                Renderer[] rends = GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rends.Length; i++)
                {
                    if (rends[i] == null) continue;
                    Material[] src = rends[i].materials;
                    for (int j = 0; j < src.Length; j++)
                    {
                        if (src[j] == null) continue;
                        MaterialFadeUtil.ConfigureFadeMaterial(src[j]);
                        extraMats.Add(src[j]);
                        extraBaseColors.Add(src[j].HasProperty("_Color") ? src[j].color
                                          : (src[j].HasProperty("_BaseColor") ? src[j].GetColor("_BaseColor") : Color.white));
                    }
                }
            }
        }

        float t = 1f - Mathf.Clamp01((timer - fadeStart) / fadeDuration);
        if (ownedMat != null)
            MaterialFadeUtil.SetMaterialAlpha(ownedMat, baseColor, t);
        for (int i = 0; i < extraMats.Count; i++)
            MaterialFadeUtil.SetMaterialAlpha(extraMats[i], extraBaseColors[i], t);

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (ownedMat != null)
            Destroy(ownedMat);
        for (int i = 0; i < extraMats.Count; i++)
            if (extraMats[i] != null) Destroy(extraMats[i]);
    }
}
