using UnityEngine;

// Gắn lên từng cụm map (vd: Biome_01_Plains, Biome_02_Forest) để tự đo chiều dài theo đường chạy.
public class BiomeSection : MonoBehaviour
{
    public BiomeData data;

    [Header("Dự phòng (khi chưa gán BiomeData asset)")]
    public string fallbackName = "Biome";
    public Sprite fallbackIcon;
    public Color fallbackCardColor = new Color(0.28f, 0.65f, 0.24f, 1f);
    public Color fallbackBorderColor = new Color(0.55f, 0.95f, 0.25f, 1f);
    public DinoSpawnTable fallbackSpawnTable;
    public bool overrideFog = true;
    public Color fallbackFogColor = new Color(0.72f, 0.86f, 0.96f, 1f);
    public float fallbackFogStart = 48f;
    public float fallbackFogEnd = 95f;
    public bool overrideLight = false;
    public Color fallbackLightColor = Color.white;
    public float fallbackLightIntensity = 1f;

    [Header("Đo chiều dài Map (tùy chọn)")]
    [Tooltip("Để trống sẽ tự quét các sàn (layer Ground hoặc tên chứa Lane/Floor) bên trong cụm này.")]
    public Collider customBoundsCollider;

    public float StartDistance { get; private set; }
    public float EndDistance { get; private set; }
    public float Length => Mathf.Max(1f, EndDistance - StartDistance);

    public string BiomeName => data != null && !string.IsNullOrEmpty(data.biomeName) ? data.biomeName : fallbackName;
    public Sprite Icon => data != null && data.icon != null ? data.icon : fallbackIcon;
    public Color CardColor => data != null ? data.cardColor : fallbackCardColor;
    public Color BorderColor => data != null ? data.borderColor : fallbackBorderColor;
    public DinoSpawnTable SpawnTable => data != null && data.spawnTable != null ? data.spawnTable : fallbackSpawnTable;
    public bool OverrideFog => data != null ? data.overrideFog : overrideFog;
    public Color FogColor => data != null ? data.fogColor : fallbackFogColor;
    public float FogStart => data != null ? data.fogStart : fallbackFogStart;
    public float FogEnd => data != null ? data.fogEnd : fallbackFogEnd;
    public bool OverrideLight => data != null ? data.overrideLight : overrideLight;
    public Color LightColor => data != null ? data.lightColor : fallbackLightColor;
    public float LightIntensity => data != null ? data.lightIntensity : fallbackLightIntensity;

    Bounds lastMeasuredBounds;
    bool hasMeasuredBounds;

    public void SetSpan(float start, float end)
    {
        StartDistance = start;
        EndDistance = Mathf.Max(start + 1f, end);
    }

    public bool RecalculateSpan(Vector3 trackOrigin, Vector3 trackForward)
    {
        Vector3 fwd = trackForward.sqrMagnitude > 0.001f ? trackForward.normalized : Vector3.forward;
        hasMeasuredBounds = false;

        if (customBoundsCollider != null)
        {
            ProjectBounds(customBoundsCollider.bounds, trackOrigin, fwd, out float minD, out float maxD);
            lastMeasuredBounds = customBoundsCollider.bounds;
            hasMeasuredBounds = true;
            SetSpan(minD, maxD);
            return true;
        }

        // Ưu tiên đo phần sàn (layer 3 Ground hoặc tên chứa Lane/Floor)
        if (TryMeasureChildren(trackOrigin, fwd, true, out float pMin, out float pMax))
        {
            SetSpan(pMin, pMax);
            return true;
        }

        // Dự phòng: đo toàn bộ Collider/Renderer con
        if (TryMeasureChildren(trackOrigin, fwd, false, out float fMin, out float fMax))
        {
            SetSpan(fMin, fMax);
            return true;
        }

        float fallbackStart = Vector3.Dot(transform.position - trackOrigin, fwd);
        SetSpan(fallbackStart, fallbackStart + 100f);
        return false;
    }

    bool TryMeasureChildren(Vector3 trackOrigin, Vector3 fwd, bool groundOnly, out float minD, out float maxD)
    {
        minD = float.MaxValue;
        maxD = float.MinValue;
        bool found = false;

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider c = cols[i];
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (groundOnly && !IsFloorObject(c.gameObject)) continue;
            IncludeBounds(c.bounds, trackOrigin, fwd, ref minD, ref maxD, ref found);
        }

        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];
            if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
            if (groundOnly && !IsFloorObject(r.gameObject)) continue;
            IncludeBounds(r.bounds, trackOrigin, fwd, ref minD, ref maxD, ref found);
        }

        return found;
    }

    static bool IsFloorObject(GameObject go)
    {
        if (go.layer == 3) return true;
        string n = go.name;
        return n.IndexOf("Lane", System.StringComparison.OrdinalIgnoreCase) >= 0
            || n.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0
            || n.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void IncludeBounds(Bounds b, Vector3 origin, Vector3 fwd, ref float minD, ref float maxD, ref bool found)
    {
        ProjectBounds(b, origin, fwd, out float bMin, out float bMax);
        if (bMin < minD) minD = bMin;
        if (bMax > maxD) maxD = bMax;
        if (!hasMeasuredBounds) { lastMeasuredBounds = b; hasMeasuredBounds = true; }
        else lastMeasuredBounds.Encapsulate(b);
        found = true;
    }

    static void ProjectBounds(Bounds b, Vector3 origin, Vector3 fwd, out float minD, out float maxD)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;
        minD = float.MaxValue;
        maxD = float.MinValue;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 corner = new Vector3(c.x + sx * e.x, c.y + sy * e.y, c.z + sz * e.z);
                    float d = Vector3.Dot(corner - origin, fwd);
                    if (d < minD) minD = d;
                    if (d > maxD) maxD = d;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Bounds b;
        if (customBoundsCollider != null) b = customBoundsCollider.bounds;
        else if (hasMeasuredBounds) b = lastMeasuredBounds;
        else
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0) return;
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                if (rends[i] != null) b.Encapsulate(rends[i].bounds);
        }

        Color col = BorderColor;
        col.a = 0.85f;
        Gizmos.color = col;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
