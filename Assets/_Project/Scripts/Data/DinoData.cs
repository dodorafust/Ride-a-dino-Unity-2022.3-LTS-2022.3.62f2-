using UnityEngine;

[CreateAssetMenu(menuName = "RideADino/Dino Data", fileName = "NewDino")]
public class DinoData : ScriptableObject
{
    public string displayName = "Dino";
    public Rarity rarity = Rarity.Common;
    [Min(1)] public int zone = 1;

    [Header("Hiển thị")]
    public Sprite icon;
    public GameObject modelPrefab;

    [Header("Economy")]
    public float baseMoney = 1f;               // Money/s cơ bản

    [Header("Ride / Tame")]
    public float requiredStamina = 10f;        // Stamina cần để tame chắc chắn
    [Min(3f)] public float tameTime = 5f;      // giây để vòng Tame đầy
    public float runSpeed = 6f;
}
