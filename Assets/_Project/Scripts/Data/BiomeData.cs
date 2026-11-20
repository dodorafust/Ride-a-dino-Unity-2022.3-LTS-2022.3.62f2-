using UnityEngine;

[CreateAssetMenu(menuName = "RideADino/Biome Data", fileName = "Biome_New")]
public class BiomeData : ScriptableObject
{
    public string biomeName = "Plains";
    public Sprite icon;
    public Color cardColor = new Color(0.28f, 0.65f, 0.24f, 1f);
    public Color borderColor = new Color(0.55f, 0.95f, 0.25f, 1f);
    public DinoSpawnTable spawnTable;

    [Header("Sương mù & Ánh sáng")]
    public bool overrideFog = true;
    public Color fogColor = new Color(0.72f, 0.86f, 0.96f, 1f);
    public float fogStart = 48f;
    public float fogEnd = 95f;
    public bool overrideLight = false;
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;
}
