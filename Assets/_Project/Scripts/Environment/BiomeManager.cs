using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Quản lý danh sách Biome dọc theo đường chạy, đo độ dài từng vùng từ map thực tế,
// theo dõi quãng đường + kỷ lục BEST, pha màu Fog/Light mượt và bắn sự kiện đổi Biome cho UI & Spawner.
public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    const string BestDistancePrefKey = "RideADino_BestDistance";

    [Header("Tham chiếu đường chạy")]
    public Transform trackOrigin;
    public DinoSpawner spawner;
    public RunZone runZone;
    public Light mainLight;

    [Header("Danh sách Biome (trống = tự quét trong Scene)")]
    public List<BiomeSection> sections = new List<BiomeSection>();

    [Header("Chuyển vùng & Banner")]
    [Min(0f)] public float fogTransitionZone = 30f;
    public bool showBannerOnFirstBiome = false;
    [Tooltip("Bật để tự reset kỷ lục BEST về 0 m mỗi khi bấm Play (dùng để test).")]
    public bool resetBestOnPlay = false;

    public IReadOnlyList<BiomeSection> Sections => sections;
    public int ActiveBiomeIndex { get; private set; } = -1;
    public float CurrentDistance { get; private set; }
    public float BestDistance { get; private set; }
    public float TotalLength { get; private set; }
    public bool Initialized { get; private set; }

    public event Action OnSectionsChanged;
    public event Action<int, BiomeSection, bool> OnBiomeChanged;

    bool wasInRun;
    bool bestDirty;
    float lastBestSaveTime;
    Color defaultLightColor = Color.white;
    float defaultLightIntensity = 1f;
    bool hasDefaultLight;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        if (resetBestOnPlay)
        {
            BestDistance = 0f;
            PlayerPrefs.DeleteKey(BestDistancePrefKey);
            PlayerPrefs.Save();
        }
        else
        {
            BestDistance = PlayerPrefs.GetFloat(BestDistancePrefKey, 0f);
        }
    }

    [ContextMenu("Reset Best Distance")]
    public void ResetBestDistance()
    {
        BestDistance = 0f;
        bestDirty = false;
        PlayerPrefs.DeleteKey(BestDistancePrefKey);
        PlayerPrefs.Save();
    }

    void Start()
    {
        EnsureInitialized();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        FlushBestDistance();
    }

    void OnApplicationQuit()
    {
        FlushBestDistance();
    }

    public void EnsureInitialized()
    {
        if (Initialized) return;
        InitializeSections();
    }

    [ContextMenu("Quét lại các cụm Biome trong Scene")]
    public void InitializeSections()
    {
        if (spawner == null) spawner = FindObjectOfType<DinoSpawner>();
        if (runZone == null) runZone = FindObjectOfType<RunZone>();
        if (trackOrigin == null && spawner != null) trackOrigin = spawner.transform;
        if (trackOrigin == null && runZone != null) trackOrigin = runZone.transform;

        if (mainLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    mainLight = lights[i];
                    break;
                }
            }
        }
        if (mainLight != null && !hasDefaultLight)
        {
            defaultLightColor = mainLight.color;
            defaultLightIntensity = mainLight.intensity;
            hasDefaultLight = true;
        }

        sections.RemoveAll(s => s == null);
        if (sections.Count == 0)
        {
            AutoDiscoverSections();
        }

        RecalculateAllSpans();
        Initialized = true;
        OnSectionsChanged?.Invoke();
    }

    void AutoDiscoverSections()
    {
        BiomeSection[] existing = FindObjectsOfType<BiomeSection>();
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null && !sections.Contains(existing[i]))
                sections.Add(existing[i]);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (go == null) continue;
            if (!go.name.StartsWith("Biome_", StringComparison.OrdinalIgnoreCase)) continue;

            BiomeSection sec = go.GetComponent<BiomeSection>();
            if (sec == null)
            {
                sec = go.AddComponent<BiomeSection>();
                ApplyPresetDefaults(sec, go.name);
            }
            if (!sections.Contains(sec))
                sections.Add(sec);
        }
    }

    void ApplyPresetDefaults(BiomeSection sec, string objectName)
    {
        string clean = ParseBiomeDisplayName(objectName);
        sec.fallbackName = clean;
        sec.fallbackSpawnTable = spawner != null ? spawner.table : null;
        sec.overrideFog = true;
        sec.fallbackFogStart = spawner != null ? spawner.runFogStart : 48f;
        sec.fallbackFogEnd = spawner != null ? spawner.runFogEnd : 95f;

        string lower = clean.ToLowerInvariant();
        if (lower.Contains("forest") && !lower.Contains("giant"))
        {
            sec.fallbackCardColor = new Color(0.16f, 0.45f, 0.22f, 1f);
            sec.fallbackBorderColor = new Color(0.24f, 0.78f, 0.34f, 1f);
            sec.fallbackFogColor = new Color(0.56f, 0.78f, 0.64f, 1f);
        }
        else if (lower.Contains("desert"))
        {
            sec.fallbackCardColor = new Color(0.78f, 0.56f, 0.24f, 1f);
            sec.fallbackBorderColor = new Color(0.95f, 0.74f, 0.30f, 1f);
            sec.fallbackFogColor = new Color(0.92f, 0.80f, 0.58f, 1f);
        }
        else if (lower.Contains("jungle"))
        {
            sec.fallbackCardColor = new Color(0.10f, 0.48f, 0.42f, 1f);
            sec.fallbackBorderColor = new Color(0.18f, 0.80f, 0.68f, 1f);
            sec.fallbackFogColor = new Color(0.45f, 0.74f, 0.68f, 1f);
        }
        else if (lower.Contains("snow"))
        {
            sec.fallbackCardColor = new Color(0.55f, 0.72f, 0.88f, 1f);
            sec.fallbackBorderColor = new Color(0.75f, 0.90f, 1f, 1f);
            sec.fallbackFogColor = new Color(0.84f, 0.91f, 0.98f, 1f);
        }
        else if (lower.Contains("volcano"))
        {
            sec.fallbackCardColor = new Color(0.62f, 0.20f, 0.12f, 1f);
            sec.fallbackBorderColor = new Color(0.96f, 0.36f, 0.16f, 1f);
            sec.fallbackFogColor = new Color(0.76f, 0.36f, 0.24f, 1f);
        }
        else
        {
            // Plains mặc định
            sec.fallbackCardColor = new Color(0.38f, 0.74f, 0.22f, 1f);
            sec.fallbackBorderColor = new Color(0.60f, 0.96f, 0.26f, 1f);
            sec.fallbackFogColor = spawner != null ? spawner.defaultFogColor : new Color(0.72f, 0.86f, 0.96f, 1f);
        }
    }

    static string ParseBiomeDisplayName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "Biome";
        string s = rawName;
        if (s.StartsWith("Biome_", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(6);

        // Bỏ tiền tố số thứ tự như "01_", "02_"
        int underscore = s.IndexOf('_');
        if (underscore > 0 && int.TryParse(s.Substring(0, underscore), out _))
            s = s.Substring(underscore + 1);

        return string.IsNullOrWhiteSpace(s) ? rawName : s.Replace('_', ' ').Trim();
    }

    public void RecalculateAllSpans()
    {
        Vector3 origin = GetOrigin();
        Vector3 fwd = GetForward();

        for (int i = 0; i < sections.Count; i++)
        {
            if (sections[i] != null)
                sections[i].RecalculateSpan(origin, fwd);
        }

        sections.Sort((a, b) => a.StartDistance.CompareTo(b.StartDistance));

        if (sections.Count > 0)
        {
            if (sections[0].StartDistance < 12f)
                sections[0].SetSpan(0f, sections[0].EndDistance);

            for (int i = 1; i < sections.Count; i++)
            {
                BiomeSection prev = sections[i - 1];
                BiomeSection cur = sections[i];
                if (Mathf.Abs(cur.StartDistance - prev.EndDistance) < 12f || cur.StartDistance < prev.EndDistance)
                    cur.SetSpan(prev.EndDistance, Mathf.Max(prev.EndDistance + 10f, cur.EndDistance));
            }
            TotalLength = Mathf.Max(1f, sections[sections.Count - 1].EndDistance);
        }
        else
        {
            TotalLength = 1f;
        }
    }

    Vector3 GetOrigin()
    {
        if (trackOrigin != null) return trackOrigin.position;
        return Vector3.zero;
    }

    Vector3 GetForward()
    {
        if (trackOrigin != null) return trackOrigin.forward;
        return Vector3.forward;
    }

    public float MeasureDistance(Vector3 worldPos)
    {
        Vector3 origin = GetOrigin();
        Vector3 fwd = GetForward();
        return Mathf.Max(0f, Vector3.Dot(worldPos - origin, fwd));
    }

    void Update()
    {
        EnsureInitialized();

        PlayerController pc = PlayerController.Instance;
        bool inRun = pc != null && pc.RunActive;

        if (inRun)
        {
            CurrentDistance = MeasureDistance(pc.transform.position);
            if (CurrentDistance > BestDistance)
            {
                BestDistance = CurrentDistance;
                bestDirty = true;
                if (Time.time - lastBestSaveTime >= 1.5f)
                    FlushBestDistance();
            }

            BiomeSection currentSec = GetBiomeAtDistance(CurrentDistance, out int idx);
            if (idx >= 0 && idx != ActiveBiomeIndex)
            {
                bool isTransition = wasInRun && ActiveBiomeIndex >= 0;
                ActiveBiomeIndex = idx;
                if (isTransition || showBannerOnFirstBiome)
                    OnBiomeChanged?.Invoke(idx, currentSec, isTransition);
            }

            UpdateDirectionalLight(Time.deltaTime);
            wasInRun = true;
        }
        else
        {
            if (wasInRun)
            {
                wasInRun = false;
                ActiveBiomeIndex = -1;
                CurrentDistance = 0f;
                FlushBestDistance();
            }
            RestoreDirectionalLight(Time.deltaTime);
        }
    }

    void FlushBestDistance()
    {
        if (!bestDirty) return;
        bestDirty = false;
        lastBestSaveTime = Time.time;
        PlayerPrefs.SetFloat(BestDistancePrefKey, BestDistance);
        PlayerPrefs.Save();
    }

    public BiomeSection GetBiomeAtDistance(float dist, out int index)
    {
        index = -1;
        if (sections.Count == 0) return null;

        for (int i = 0; i < sections.Count; i++)
        {
            BiomeSection s = sections[i];
            if (s == null) continue;
            if (dist >= s.StartDistance && dist < s.EndDistance)
            {
                index = i;
                return s;
            }
        }

        if (dist < sections[0].StartDistance)
        {
            index = 0;
            return sections[0];
        }

        index = sections.Count - 1;
        return sections[index];
    }

    public float GetProgressAlongBar01(float dist)
    {
        int count = sections.Count;
        if (count == 0) return 0f;

        BiomeSection s = GetBiomeAtDistance(dist, out int idx);
        if (s == null || idx < 0) return 0f;

        float localT = Mathf.Clamp01((dist - s.StartDistance) / s.Length);
        return Mathf.Clamp01((idx + localT) / count);
    }

    public void EvaluateAtmosphere(float dist, Color defaultFog, float defaultStart, float defaultEnd,
                                   out Color fogColor, out float fogStart, out float fogEnd)
    {
        fogColor = defaultFog;
        fogStart = defaultStart;
        fogEnd = defaultEnd;

        BiomeSection cur = GetBiomeAtDistance(dist, out int idx);
        if (cur == null || idx < 0) return;

        fogColor = cur.OverrideFog ? cur.FogColor : defaultFog;
        fogStart = cur.OverrideFog ? cur.FogStart : defaultStart;
        fogEnd = cur.OverrideFog ? cur.FogEnd : defaultEnd;

        if (fogTransitionZone <= 0.1f) return;

        float halfZone = fogTransitionZone * 0.5f;

        // Nửa sau vùng đệm ở cuối Biome hiện tại -> pha mượt sang Biome kế tiếp
        if (idx + 1 < sections.Count && cur.EndDistance - dist <= halfZone)
        {
            BiomeSection next = sections[idx + 1];
            float t = Mathf.InverseLerp(cur.EndDistance - halfZone, cur.EndDistance + halfZone, dist);
            Color nextCol = next.OverrideFog ? next.FogColor : defaultFog;
            float nextStart = next.OverrideFog ? next.FogStart : defaultStart;
            float nextEnd = next.OverrideFog ? next.FogEnd : defaultEnd;
            fogColor = Color.Lerp(fogColor, nextCol, t);
            fogStart = Mathf.Lerp(fogStart, nextStart, t);
            fogEnd = Mathf.Lerp(fogEnd, nextEnd, t);
        }
        // Nửa sau vùng đệm ở đầu Biome hiện tại -> nối tiếp mượt từ Biome trước đó
        else if (idx - 1 >= 0 && dist - cur.StartDistance <= halfZone)
        {
            BiomeSection prev = sections[idx - 1];
            float t = Mathf.InverseLerp(cur.StartDistance - halfZone, cur.StartDistance + halfZone, dist);
            Color prevCol = prev.OverrideFog ? prev.FogColor : defaultFog;
            float prevStart = prev.OverrideFog ? prev.FogStart : defaultStart;
            float prevEnd = prev.OverrideFog ? prev.FogEnd : defaultEnd;
            fogColor = Color.Lerp(prevCol, fogColor, t);
            fogStart = Mathf.Lerp(prevStart, fogStart, t);
            fogEnd = Mathf.Lerp(prevEnd, fogEnd, t);
        }
    }

    void UpdateDirectionalLight(float dt)
    {
        if (mainLight == null || !hasDefaultLight) return;
        BiomeSection cur = GetBiomeAtDistance(CurrentDistance, out int idx);
        if (cur == null) return;

        Color targetCol = cur.OverrideLight ? cur.LightColor : defaultLightColor;
        float targetInt = cur.OverrideLight ? cur.LightIntensity : defaultLightIntensity;

        if (fogTransitionZone > 0.1f && idx + 1 < sections.Count)
        {
            float halfZone = fogTransitionZone * 0.5f;
            if (cur.EndDistance - CurrentDistance <= halfZone)
            {
                BiomeSection next = sections[idx + 1];
                float t = Mathf.InverseLerp(cur.EndDistance - halfZone, cur.EndDistance + halfZone, CurrentDistance);
                Color nextCol = next.OverrideLight ? next.LightColor : defaultLightColor;
                float nextInt = next.OverrideLight ? next.LightIntensity : defaultLightIntensity;
                targetCol = Color.Lerp(targetCol, nextCol, t);
                targetInt = Mathf.Lerp(targetInt, nextInt, t);
            }
        }

        float k = 1f - Mathf.Exp(-4f * dt);
        mainLight.color = Color.Lerp(mainLight.color, targetCol, k);
        mainLight.intensity = Mathf.Lerp(mainLight.intensity, targetInt, k);
    }

    void RestoreDirectionalLight(float dt)
    {
        if (mainLight == null || !hasDefaultLight) return;
        float k = 1f - Mathf.Exp(-4f * dt);
        mainLight.color = Color.Lerp(mainLight.color, defaultLightColor, k);
        mainLight.intensity = Mathf.Lerp(mainLight.intensity, defaultLightIntensity, k);
    }
}
