using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MaterialFadeUtil
{
    public static void ConfigureFadeMaterial(Material m)
    {
        if (m == null) return;
        if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 2f); // Standard shader Fade mode
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
    }

    public static void SetMaterialAlpha(Material m, Color baseColor, float alpha)
    {
        if (m == null) return;
        Color c = baseColor;
        c.a = alpha;
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    }
}

// Làm mờ dần vật cản (Obstacle) và Dino khi camera đi sát hoặc xuyên qua, tránh che khuất nhân vật.
[DefaultExecutionOrder(200)]
public class CameraOcclusionFader : MonoBehaviour
{
    [System.Serializable]
    public struct FadeProfile
    {
        public bool enabled;
        public float fadeMinDist;
        public float fadeMaxDist;
        [Range(0.05f, 1f)] public float minAlpha;

        public FadeProfile(bool enabled, float minDist, float maxDist, float minAlpha)
        {
            this.enabled = enabled;
            this.fadeMinDist = minDist;
            this.fadeMaxDist = maxDist;
            this.minAlpha = minAlpha;
        }
    }

    [Header("Chung")]
    public float scanRadius = 9f;
    public float blockCastRadius = 1.2f;
    public float fadeSpeed = 12f;

    public enum ObstacleFadeRule
    {
        AlwaysFade,
        LargerThanRiddenDino,
        AtLeastRiddenDino
    }

    [Header("Độ mờ Vật cản theo 4 kích thước")]
    public bool fadeObstacles = true;
    [Tooltip("LargerThanRiddenDino: chỉ làm mờ vật cản > Dino (bỏ qua <=). AtLeastRiddenDino: chỉ làm mờ >= Dino. AlwaysFade: luôn làm mờ.")]
    public ObstacleFadeRule riddenFadeRule = ObstacleFadeRule.LargerThanRiddenDino;
    public FadeProfile smallObstacleFade  = new FadeProfile(true, 1.2f, 4.0f, 0.20f);
    public FadeProfile mediumObstacleFade = new FadeProfile(true, 1.4f, 5.5f, 0.15f);
    public FadeProfile largeObstacleFade  = new FadeProfile(true, 1.6f, 7.0f, 0.12f);
    public FadeProfile giantObstacleFade  = new FadeProfile(true, 1.8f, 8.5f, 0.10f);

    [Header("Độ mờ riêng cho Vật cản đang làm chậm Dino")]
    [Tooltip("Nếu Dino đang cưỡi bị vật cản làm chậm, vật cản đó sẽ dùng độ mờ riêng này thay vì độ mờ mặc định.")]
    public bool customFadeForSlowingObstacle = true;
    public float slowingObstacleMinDist = 1.4f;
    public float slowingObstacleMaxDist = 6.5f;
    [Range(0.05f, 1f)] public float slowingObstacleMinAlpha = 0.45f;

    [Header("Độ mờ Dino")]
    public bool fadeDinos = true;
    public float dinoFadeMinDist = 1.4f;
    public float dinoFadeMaxDist = 5.5f;
    [Range(0.05f, 1f)] public float dinoMinAlpha = 0.18f;

    [Header("Tránh xuyên đầu nhân vật khi sát tường")]
    public float playerHideDist = 0.72f;
    public float closeNearClip = 0.06f;

    class FadeEntry
    {
        public Renderer rend;
        public Material[] origShared;
        public Material[] fadeMats;
        public Color[] baseColors;
        public float currentAlpha = 1f;
        public float targetAlpha = 1f;
        public bool usingFadeMats;
    }

    readonly Dictionary<Renderer, FadeEntry> entries = new Dictionary<Renderer, FadeEntry>();
    readonly List<Renderer> toRemove = new List<Renderer>();
    readonly Collider[] overlapBuf = new Collider[48];
    readonly RaycastHit[] castBuf = new RaycastHit[24];
    readonly List<Renderer> hiddenPlayerRends = new List<Renderer>();
    Camera cam;
    float origNearClip = -1f;
    bool playerHidden;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (origNearClip < 0f) origNearClip = cam.nearClipPlane;

        Vector3 camPos = cam.transform.position;
        PlayerController player = PlayerController.Instance;
        Dino riddenDino = player != null ? player.CurrentDino : null;

        UpdateClosePlayerHeadGuard(camPos, player);

        foreach (var kv in entries)
            kv.Value.targetAlpha = 1f;

        int n = Physics.OverlapSphereNonAlloc(camPos, scanRadius, overlapBuf, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            EvaluateCollider(overlapBuf[i], camPos, riddenDino);

        if (player != null)
        {
            Vector3 targetPos = player.transform.position + Vector3.up * 1.4f;
            Vector3 toTarget = targetPos - camPos;
            float dist = toTarget.magnitude;
            if (dist > 0.3f)
            {
                int m = Physics.SphereCastNonAlloc(camPos, blockCastRadius, toTarget / dist, castBuf,
                                                   dist, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < m; i++)
                    EvaluateCollider(castBuf[i].collider, camPos, riddenDino);
            }
        }

        float dt = Time.deltaTime;
        toRemove.Clear();
        foreach (var kv in entries)
        {
            FadeEntry e = kv.Value;
            if (e.rend == null) { toRemove.Add(kv.Key); continue; }

            e.currentAlpha = Mathf.MoveTowards(e.currentAlpha, e.targetAlpha, fadeSpeed * dt);
            if (e.currentAlpha < 0.99f)
            {
                EnsureFadeMaterials(e);
                e.usingFadeMats = true;
                for (int i = 0; i < e.fadeMats.Length; i++)
                    MaterialFadeUtil.SetMaterialAlpha(e.fadeMats[i], e.baseColors[i], e.currentAlpha);
            }
            else if (e.usingFadeMats)
            {
                e.rend.sharedMaterials = e.origShared;
                e.usingFadeMats = false;
                if (e.targetAlpha >= 0.99f) toRemove.Add(kv.Key);
            }
            else if (e.targetAlpha >= 0.99f)
            {
                toRemove.Add(kv.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Renderer r = toRemove[i];
            if (entries.TryGetValue(r, out FadeEntry e) && e.fadeMats != null)
            {
                for (int j = 0; j < e.fadeMats.Length; j++)
                    if (e.fadeMats[j] != null) Destroy(e.fadeMats[j]);
            }
            entries.Remove(r);
        }
    }

    void UpdateClosePlayerHeadGuard(Vector3 camPos, PlayerController player)
    {
        Vector3 headPos = CameraRig.Instance != null && CameraRig.Instance.cameraTarget != null
            ? CameraRig.Instance.cameraTarget.position
            : (player != null ? player.transform.position + Vector3.up * 1.4f : camPos + Vector3.forward * 10f);

        float headDist = player != null ? Vector3.Distance(camPos, headPos) : 10f;
        cam.nearClipPlane = headDist < 1.25f ? Mathf.Min(origNearClip, closeNearClip) : origNearClip;

        bool shouldHide = player != null && headDist < playerHideDist;
        if (shouldHide == playerHidden) return;

        playerHidden = shouldHide;
        if (shouldHide)
        {
            hiddenPlayerRends.Clear();
            if (player.animator != null)
            {
                Renderer[] rends = player.animator.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rends.Length; i++)
                {
                    if (rends[i] != null && rends[i].enabled)
                    {
                        rends[i].enabled = false;
                        hiddenPlayerRends.Add(rends[i]);
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < hiddenPlayerRends.Count; i++)
                if (hiddenPlayerRends[i] != null) hiddenPlayerRends[i].enabled = true;
            hiddenPlayerRends.Clear();
        }
    }

    void EvaluateCollider(Collider col, Vector3 camPos, Dino riddenDino)
    {
        if (col == null) return;
        Obstacle ob = col.GetComponentInParent<Obstacle>();
        Dino dino = ob == null ? col.GetComponentInParent<Dino>() : null;
        if (ob == null && dino == null) return;
        bool isSlowingObstacle = false;
        if (ob != null)
        {
            if (!fadeObstacles || ob.IsBroken) return;
            if (riddenDino != null)
            {
                ObjectSize dinoSize = riddenDino.EffectiveSize;
                bool canSlowDino = ob.size >= dinoSize || !ob.breakable;
                isSlowingObstacle = customFadeForSlowingObstacle && (riddenDino.SlowingObstacle == ob || canSlowDino);
                if (!isSlowingObstacle && riddenFadeRule != ObstacleFadeRule.AlwaysFade)
                {
                    if (riddenFadeRule == ObstacleFadeRule.LargerThanRiddenDino && ob.size <= dinoSize) return;
                    if (riddenFadeRule == ObstacleFadeRule.AtLeastRiddenDino && ob.size < dinoSize) return;
                }
            }
        }
        if (dino != null && (!fadeDinos || dino == riddenDino || dino.IsRidden)) return;

        Transform root = ob != null ? ob.transform : dino.transform;
        Vector3 closest = (col is MeshCollider mc && !mc.convex)
            ? col.bounds.ClosestPoint(camPos)
            : col.ClosestPoint(camPos);
        float dist = Vector3.Distance(camPos, closest);

        float minD, maxD, minA;
        if (isSlowingObstacle)
        {
            minD = slowingObstacleMinDist;
            maxD = slowingObstacleMaxDist;
            minA = slowingObstacleMinAlpha;
        }
        else if (ob != null)
        {
            FadeProfile prof = GetObstacleProfile(ob.size);
            if (!prof.enabled) return;
            minD = prof.fadeMinDist;
            maxD = prof.fadeMaxDist;
            minA = prof.minAlpha;
        }
        else
        {
            minD = dinoFadeMinDist;
            maxD = dinoFadeMaxDist;
            minA = dinoMinAlpha;
        }

        float t = Mathf.InverseLerp(minD, maxD, dist);
        float alpha = Mathf.Lerp(minA, 1f, t);
        if (alpha >= 0.99f) return;

        Renderer[] rends = root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];
            if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
            if (!entries.TryGetValue(r, out FadeEntry e))
            {
                e = new FadeEntry { rend = r, origShared = r.sharedMaterials };
                entries[r] = e;
            }
            if (alpha < e.targetAlpha) e.targetAlpha = alpha;
        }
    }

    FadeProfile GetObstacleProfile(ObjectSize size)
    {
        switch (size)
        {
            case ObjectSize.Small:  return smallObstacleFade;
            case ObjectSize.Large:  return largeObstacleFade;
            case ObjectSize.Giant:  return giantObstacleFade;
            default:                return mediumObstacleFade;
        }
    }

    void EnsureFadeMaterials(FadeEntry e)
    {
        if (e.fadeMats == null)
        {
            int len = e.origShared.Length;
            e.fadeMats = new Material[len];
            e.baseColors = new Color[len];
            for (int i = 0; i < len; i++)
            {
                Material src = e.origShared[i];
                if (src == null) continue;
                Material m = new Material(src);
                MaterialFadeUtil.ConfigureFadeMaterial(m);
                e.fadeMats[i] = m;
                e.baseColors[i] = m.HasProperty("_Color") ? m.color
                                : (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white);
            }
        }
        if (e.rend.sharedMaterials != e.fadeMats)
            e.rend.sharedMaterials = e.fadeMats;
    }

    void OnDestroy()
    {
        foreach (var kv in entries)
        {
            FadeEntry e = kv.Value;
            if (e.rend != null && e.usingFadeMats)
                e.rend.sharedMaterials = e.origShared;
            if (e.fadeMats != null)
            {
                for (int i = 0; i < e.fadeMats.Length; i++)
                    if (e.fadeMats[i] != null) Destroy(e.fadeMats[i]);
            }
        }
        entries.Clear();
    }
}
