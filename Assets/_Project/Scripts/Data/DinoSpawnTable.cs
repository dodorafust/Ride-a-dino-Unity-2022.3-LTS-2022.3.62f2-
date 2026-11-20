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

    [Serializable]
    public class SizeWeight
    {
        public DinoSizeType sizeType = DinoSizeType.Normal;
        public float weight = 1f;
    }

    public List<Entry> entries = new List<Entry>();
    public List<VariantWeight> variants = new List<VariantWeight>();
    [Tooltip("Tỷ lệ các hệ kích thước (Normal, Tiny, Big, Huge, Giant, Colossal). Để trống sẽ dùng tỷ lệ mặc định.")]
    public List<SizeWeight> sizes = new List<SizeWeight>();

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

    public DinoSizeType PickSize()
    {
        if (sizes != null && sizes.Count > 0)
        {
            float total = 0f;
            for (int i = 0; i < sizes.Count; i++)
                if (sizes[i] != null && sizes[i].weight > 0f) total += sizes[i].weight;
            if (total > 0f)
            {
                float r = UnityEngine.Random.value * total;
                DinoSizeType last = DinoSizeType.Normal;
                for (int i = 0; i < sizes.Count; i++)
                {
                    SizeWeight sw = sizes[i];
                    if (sw == null || sw.weight <= 0f) continue;
                    last = sw.sizeType;
                    r -= sw.weight;
                    if (r <= 0f) return sw.sizeType;
                }
                return last;
            }
        }

        // Tỷ lệ mặc định khi chưa cấu hình danh sách Sizes trong Table
        float roll = UnityEngine.Random.value * 100f;
        if (roll < 70f) return DinoSizeType.Normal;
        if (roll < 82f) return DinoSizeType.Tiny;
        if (roll < 92f) return DinoSizeType.Big;
        if (roll < 97f) return DinoSizeType.Huge;
        if (roll < 99.5f) return DinoSizeType.Giant;
        return DinoSizeType.Colossal;
    }
}
