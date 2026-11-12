using System;
using UnityEngine;
[Serializable]
public class LevelVisual
{
    public int fromLevel = 1;
    public GameObject root;

    public static void Apply(LevelVisual[] list, int level)
    {
        if (list == null || list.Length == 0) return;
        int best = -1;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null || list[i].root == null) continue;
            if (list[i].fromLevel > level) continue;
            if (best < 0 || list[i].fromLevel >= list[best].fromLevel) best = i;
        }
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null || list[i].root == null) continue;
            list[i].root.SetActive(i == best);
        }
    }
}
