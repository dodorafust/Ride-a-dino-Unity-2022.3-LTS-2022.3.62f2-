using System;
using UnityEngine;
[Serializable]
public class LevelVisual
{
    public int fromLevel = 1;
    public GameObject root;
    [Tooltip("Tùy chọn: Collider vùng chuồng riêng cho bậc này (nếu để trống sẽ tự tìm child tên 'Area' trong root, hoặc dùng Area mặc định).")]
    public Collider areaOverride;

    public static LevelVisual GetActive(LevelVisual[] list, int level)
    {
        if (list == null || list.Length == 0) return null;
        int best = -1;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null || list[i].root == null) continue;
            if (list[i].fromLevel > level) continue;
            if (best < 0 || list[i].fromLevel >= list[best].fromLevel) best = i;
        }
        return best >= 0 ? list[best] : null;
    }

    public static void Apply(LevelVisual[] list, int level)
    {
        if (list == null || list.Length == 0) return;
        LevelVisual active = GetActive(list, level);
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null || list[i].root == null) continue;
            list[i].root.SetActive(list[i] == active);
        }
    }
}
