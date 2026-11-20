using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

// Đặt object này ở đầu đường chạy, xoay sao cho trục xanh dương (forward) hướng dọc đường chạy.
public class DinoSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ZoneSegment
    {
        public string name = "Vùng 1";
        public float startDistance = 0f;
        public float endDistance = 200f;
        public DinoSpawnTable table;
        public bool overrideFogColor;
        public Color fogColor = new Color(0.72f, 0.86f, 0.96f, 1f);
    }

    [Header("Cơ bản")]
    public Dino dinoPrefab;
    public DinoSpawnTable table;      // bảng mặc định nếu chưa chia ZoneSegments
    public RunZone zone;              // Dino ra khỏi vùng này sẽ biến mất
    public float interval = 0.8f;
    public float laneHalfWidth = 10f;
    public float spacing = 7f;
    public int maxAlive = 35;

    [Header("Chia cấp bậc Dino theo Vùng (tùy chọn)")]
    public List<ZoneSegment> zoneSegments = new List<ZoneSegment>();

    [Header("Cửa sổ Spawn quanh người chơi")]
    public bool useActiveWindow = true;
    public Transform player;
    public float startLineOffset = 2f;
    public float bandLength = 20f;
    public int maxPerBand = 5;
    public float minDinoSpacing = 3.2f;
    [Tooltip("Khoảng cách tối thiểu phía trước mặt để spawn (nên đặt trong vùng sương mù, vd: 60)")]
    public float spawnAheadMin = 60f;
    [Tooltip("Khoảng cách tối đa phía trước mặt để spawn (nên đặt gần Run Fog End, vd: 95)")]
    public float spawnAheadMax = 95f;
    [Tooltip("Thu hồi con đã tụt lại sau lưng quá khoảng này (chìm vào sương mù) khi cần slot spawn phía trước")]
    public float recycleBehindDist = 30f;
    [Tooltip("Khoảng cách tối thiểu phía sau để spawn khi người chơi đi ngược về sau")]
    public float spawnBehindMin = 35f;
    [Tooltip("Số Dino tối thiểu trong vùng gần phía trước; thiếu sẽ spawn thêm từ sau lưng chạy vượt lên.")]
    public int minNearAheadDinos = 4;
    public float despawnBehind = 70f;
    public float despawnAhead = 135f;

    [Header("Sương mù & Tầm nhìn (Fog)")]
    [Tooltip("Chỉ bật khi sàn map đã chia lưới nhỏ hoặc dùng shader per-pixel fog; sàn 1 khối dài trong Built-in sẽ bị trắng xóa khi vertex vượt quá fogEnd.")]
    public bool controlFog = false;
    public Color defaultFogColor = new Color(0.72f, 0.86f, 0.96f, 1f);
    public float runFogStart = 48f;
    public float runFogEnd = 95f;
    public float runFarClip = 140f;
    public float penFogStart = 75f;
    public float penFogEnd = 150f;
    public float penFarClip = 200f;
    public float fogBlendSpeed = 4f;

    float timer;
    PlayerController pc;
    Camera mainCam;
    CinemachineVirtualCamera vcam;

    void Start()
    {
        EnsureRefs();
        BiomeManager bm = BiomeManager.Instance != null ? BiomeManager.Instance : FindObjectOfType<BiomeManager>();
        if (bm == null) bm = gameObject.AddComponent<BiomeManager>();
        if (bm.trackOrigin == null) bm.trackOrigin = transform;
        if (bm.spawner == null) bm.spawner = this;
        if (bm.runZone == null) bm.runZone = zone;
        bm.EnsureInitialized();

        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogStartDistance = penFogStart;
            RenderSettings.fogEndDistance = penFogEnd;
            ApplyFarClip(penFarClip);
        }

        if (useActiveWindow)
            PrefillAhead();
    }

    void EnsureRefs()
    {
        if (pc == null) pc = PlayerController.Instance;
        if (player == null && pc != null) player = pc.transform;
        if (mainCam == null) mainCam = Camera.main;
        if (vcam == null) vcam = FindObjectOfType<CinemachineVirtualCamera>();
    }

    float GetPlayerDistance()
    {
        if (player == null) return 0f;
        return Mathf.Max(0f, Vector3.Dot(player.position - transform.position, transform.forward));
    }

    DinoSpawnTable PickTableAt(float dist, out ZoneSegment seg)
    {
        seg = null;
        if (BiomeManager.Instance != null && BiomeManager.Instance.Sections.Count > 0)
        {
            BiomeSection biomeSec = BiomeManager.Instance.GetBiomeAtDistance(dist, out _);
            if (biomeSec != null && biomeSec.SpawnTable != null)
                return biomeSec.SpawnTable;
        }
        for (int i = 0; i < zoneSegments.Count; i++)
        {
            ZoneSegment s = zoneSegments[i];
            if (s != null && dist >= s.startDistance && dist < s.endDistance)
            {
                seg = s;
                if (s.table != null) return s.table;
            }
        }
        return table;
    }

    int CountDinosInBand(float bandStart, float bandEnd)
    {
        int count = 0;
        Vector3 origin = transform.position;
        Vector3 fwd = transform.forward;
        for (int i = 0; i < Dino.All.Count; i++)
        {
            Dino d = Dino.All[i];
            if (d == null || d.IsRidden || d.Claimed) continue;
            float dist = Vector3.Dot(d.transform.position - origin, fwd);
            if (dist >= bandStart && dist < bandEnd) count++;
        }
        return count;
    }

    bool FindLeastPopulatedBand(float winStart, float winEnd, int capPerBand, out float bestStart, out int bestCount)
    {
        bestStart = winStart;
        bestCount = int.MaxValue;
        float step = Mathf.Max(8f, bandLength);
        bool found = false;
        for (float bStart = winStart; bStart < winEnd; bStart += step)
        {
            float bEnd = Mathf.Min(winEnd, bStart + step);
            int c = CountDinosInBand(bStart, bEnd);
            if (c < bestCount)
            {
                bestCount = c;
                bestStart = bStart;
                found = true;
            }
        }
        return found && bestCount < capPerBand;
    }

    void PrefillAhead()
    {
        float pDist = GetPlayerDistance();
        float winStart = Mathf.Max(startLineOffset, pDist + startLineOffset);
        float winEnd = pDist + spawnAheadMax;
        float step = Mathf.Max(8f, bandLength);
        for (float d = winStart; d < winEnd && Dino.Alive < maxAlive; d += step)
        {
            int cap = Mathf.Max(1, maxPerBand - CountDinosInBand(d, d + step));
            SpawnHerdAt(d + Random.Range(0f, step * 0.4f), cap);
        }
    }

    void Update()
    {
        EnsureRefs();
        UpdateFog();

        if (useActiveWindow)
        {
            timer -= Time.deltaTime;
            if (timer > 0f && Dino.Alive > 0) return;
            timer = Mathf.Max(0.2f, interval);

            float pDist = GetPlayerDistance();
            bool goingBackward = IsGoingBackward(pDist);

            float winStart;
            float winEnd;
            if (goingBackward)
            {
                winStart = Mathf.Max(startLineOffset, pDist - spawnAheadMax);
                winEnd = Mathf.Max(startLineOffset, pDist - spawnBehindMin);
            }
            else
            {
                int nearAhead = CountDinosInBand(pDist, pDist + spawnAheadMin);
                if (nearAhead < minNearAheadDinos && pDist > spawnBehindMin + 10f)
                {
                    int behindRecycled = 0;
                    while (Dino.Alive - behindRecycled >= maxAlive && TryRecycleFarthest(pDist, true))
                        behindRecycled++;
                    if (Dino.Alive - behindRecycled < maxAlive)
                    {
                        float catchUpDist = Mathf.Max(startLineOffset, pDist - Random.Range(spawnBehindMin, spawnBehindMin + 18f));
                        SpawnHerdAt(catchUpDist, Mathf.Max(1, minNearAheadDinos - nearAhead), behindRecycled);
                    }
                }

                float aheadMin = nearAhead <= 1 ? Mathf.Min(spawnAheadMin, 40f) : spawnAheadMin;
                winStart = pDist < 25f ? startLineOffset : pDist + aheadMin;
                winEnd = pDist + spawnAheadMax;
            }

            if (winEnd > winStart + 2f && FindLeastPopulatedBand(winStart, winEnd, maxPerBand, out float bandStart, out int currentCount))
            {
                int allowed = Mathf.Max(1, maxPerBand - currentCount);
                int recycled = 0;
                while (Dino.Alive - recycled >= maxAlive && TryRecycleFarthest(pDist, goingBackward))
                    recycled++;

                if (Dino.Alive - recycled < maxAlive)
                {
                    float spawnDist = bandStart + Random.Range(0f, Mathf.Max(2f, bandLength * 0.4f));
                    SpawnHerdAt(spawnDist, allowed, recycled);
                }
            }
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;
        if (Dino.Alive >= maxAlive) return;
        SpawnHerdAt(0f, maxAlive);
    }

    void UpdateFog()
    {
        bool inRun = pc != null && pc.RunActive;
        float dt = Time.deltaTime;
        float k = 1f - Mathf.Exp(-fogBlendSpeed * dt);
        float targetClip = inRun ? runFarClip : penFarClip;
        ApplyFarClip(Mathf.Lerp(GetCurrentFarClip(), targetClip, k));

        if (!controlFog) return;

        float targetStart = inRun ? runFogStart : penFogStart;
        float targetEnd = inRun ? runFogEnd : penFogEnd;

        Color targetColor = defaultFogColor;
        if (inRun)
        {
            float pDist = GetPlayerDistance();
            if (BiomeManager.Instance != null && BiomeManager.Instance.Sections.Count > 0)
            {
                BiomeManager.Instance.EvaluateAtmosphere(pDist, defaultFogColor, runFogStart, runFogEnd,
                                                         out targetColor, out targetStart, out targetEnd);
            }
            else
            {
                PickTableAt(pDist, out ZoneSegment seg);
                if (seg != null && seg.overrideFogColor) targetColor = seg.fogColor;
            }
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, targetStart, k);
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, targetEnd, k);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, k);
    }

    float GetCurrentFarClip()
    {
        if (vcam != null) return vcam.m_Lens.FarClipPlane;
        return mainCam != null ? mainCam.farClipPlane : 1000f;
    }

    void ApplyFarClip(float far)
    {
        if (vcam != null)
        {
            var lens = vcam.m_Lens;
            lens.FarClipPlane = far;
            vcam.m_Lens = lens;
        }
        if (mainCam != null) mainCam.farClipPlane = far;
    }

    bool IsGoingBackward(float pDist)
    {
        if (pDist <= startLineOffset + 20f || pc == null) return false;
        Vector3 v = pc.Velocity;
        v.y = 0f;
        if (v.sqrMagnitude > 0.5f)
            return Vector3.Dot(v.normalized, transform.forward) < -0.35f;
        if (mainCam != null)
            return Vector3.Dot(Vector3.ProjectOnPlane(mainCam.transform.forward, Vector3.up).normalized, transform.forward) < -0.35f;
        return false;
    }

    bool TryRecycleFarthest(float pDist, bool recycleAhead)
    {
        Dino farthest = null;
        float bestDist = recycleAhead ? pDist + Mathf.Max(5f, recycleBehindDist) : pDist - Mathf.Max(5f, recycleBehindDist);
        Vector3 origin = transform.position;
        Vector3 fwd = transform.forward;
        for (int i = 0; i < Dino.All.Count; i++)
        {
            Dino d = Dino.All[i];
            if (d == null || d.IsRidden || d.Claimed) continue;
            float dist = Vector3.Dot(d.transform.position - origin, fwd);
            if (recycleAhead ? dist > bestDist : dist < bestDist)
            {
                bestDist = dist;
                farthest = d;
            }
        }
        if (farthest == null) return false;
        Dino.All.Remove(farthest);
        Destroy(farthest.gameObject);
        return true;
    }

    bool IsClearOfDinos(Vector3 candidate)
    {
        float minSqr = minDinoSpacing * minDinoSpacing;
        for (int i = 0; i < Dino.All.Count; i++)
        {
            Dino d = Dino.All[i];
            if (d == null) continue;
            if ((d.transform.position - candidate).sqrMagnitude < minSqr) return false;
        }
        return true;
    }

    int CountAliveWildOfSpecies(DinoData target)
    {
        if (target == null) return 0;
        int c = 0;
        for (int i = 0; i < Dino.All.Count; i++)
        {
            Dino d = Dino.All[i];
            if (d == null || d.Data != target || d.Claimed || d.IsPetOwned || d.IsBeingEaten) continue;
            c++;
        }
        return c;
    }

    int RemainingSlotsForSpecies(DinoData target)
    {
        if (target == null || target.maxAliveOnTrack <= 0) return int.MaxValue;
        return Mathf.Max(0, target.maxAliveOnTrack - CountAliveWildOfSpecies(target));
    }

    void SpawnHerdAt(float forwardDist, int maxSpawnCount, int recycledSlots = 0)
    {
        DinoSpawnTable activeTable = PickTableAt(forwardDist, out _);
        if (activeTable == null || dinoPrefab == null)
        {
            Debug.LogWarning("DinoSpawner: thiếu Table hoặc Dino Prefab.");
            return;
        }

        DinoSpawnTable.Entry entry = null;
        int speciesRemaining = int.MaxValue;
        for (int pickTry = 0; pickTry < 10; pickTry++)
        {
            DinoSpawnTable.Entry cand = activeTable.PickEntry();
            if (cand == null || cand.dino == null) continue;
            int rem = RemainingSlotsForSpecies(cand.dino);
            if (rem > 0)
            {
                entry = cand;
                speciesRemaining = rem;
                break;
            }
        }
        if (entry == null) return;

        int count = Mathf.Min(maxSpawnCount, Random.Range(entry.herdMin, entry.herdMax + 1), speciesRemaining);
        float baseX = Random.Range(-laneHalfWidth, laneHalfWidth);
        float yaw = transform.eulerAngles.y;
        Vector3 herdOrigin = transform.position + transform.forward * forwardDist;

        for (int i = 0; i < count; i++)
        {
            if (Dino.Alive - recycledSlots >= maxAlive) break;

            Vector3 pos = Vector3.zero;
            bool clear = false;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                float x = Mathf.Clamp(baseX + Random.Range(-4.5f, 4.5f), -laneHalfWidth, laneHalfWidth);
                Vector3 candidate = herdOrigin + transform.right * x + transform.forward * (i * spacing);
                if (Physics.Raycast(candidate + Vector3.up * 6f, Vector3.down, out RaycastHit gHit, 16f, 1 << 3, QueryTriggerInteraction.Ignore))
                    candidate.y = gHit.point.y;
                if (zone != null && !zone.ContainsPoint(candidate, 0f)) continue;
                if (!IsClearOfDinos(candidate)) continue;
                if (!Physics.CheckSphere(candidate + Vector3.up * 1.1f, 1.1f,
                                         ~((1 << 2) | (1 << 3) | (1 << 6) | (1 << 7)),
                                         QueryTriggerInteraction.Ignore))
                {
                    pos = candidate;
                    clear = true;
                    break;
                }
            }
            if (!clear) continue;

            Dino d = Instantiate(dinoPrefab, pos, Quaternion.Euler(0f, yaw, 0f));
            var owned = new OwnedDino(entry.dino, activeTable.PickVariant(), DinoTrait.None, activeTable.PickSize());
            float speed = entry.dino.runSpeed + Random.Range(-entry.speedJitter, entry.speedJitter);
            d.Setup(owned, speed, yaw, zone);
            if (useActiveWindow)
                d.SetWindow(player, transform.position, transform.forward, despawnBehind, despawnAhead);
        }
    }
}
