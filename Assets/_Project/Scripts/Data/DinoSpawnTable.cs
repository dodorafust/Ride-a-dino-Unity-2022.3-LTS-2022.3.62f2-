using System;
using System.Collections.Generic;
using UnityEngine;

// Bảng spawn: loài nào, tỉ lệ bao nhiêu, đàn lớn cỡ nào, biến thể nào. Tạo nhiều bảng cho nhiều vùng / event.
[CreateAssetMenu(menuName = "RideADino/Spawn Table", fileName = "SpawnTable_New")]
public class DinoSpawnTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public DinoData dino;
        public float weight = 1f;
        public int herdMin = 3;
        public int herdMax = 6;
        public float speedJitter = 0.5f;
    }

    [Serializable]
    public class VariantWeight
    {
        public DinoVariant variant;
        public float weight = 1f;
    }

    public List<Entry> entries = new List<Entry>();
    public List<VariantWeight> variants = new List<VariantWeight>();

    public Entry PickEntry()
    {
        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].dino != null && entries[i].weight > 0f) total += entries[i].weight;
        if (total <= 0f) return null;

        float r = UnityEngine.Random.value * total;
        Entry last = null;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.dino == null || e.weight <= 0f) continue;
            last = e;
            r -= e.weight;
            if (r <= 0f) return e;
        }
        return last;
    }

    public DinoVariant PickVariant()
    {
        float total = 0f;
        for (int i = 0; i < variants.Count; i++)
            if (variants[i].variant != null && variants[i].weight > 0f) total += variants[i].weight;
        if (total <= 0f) return null;

        float r = UnityEngine.Random.value * total;
        DinoVariant last = null;
        for (int i = 0; i < variants.Count; i++)
        {
            VariantWeight v = variants[i];
            if (v.variant == null || v.weight <= 0f) continue;
            last = v.variant;
            r -= v.weight;
            if (r <= 0f) return v.variant;
        }
        return last;
    }
}
