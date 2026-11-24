using UnityEngine;

// 1 hệ kích thước = 1 asset (giống DinoVariant).
// Tạo mới: Create > RideADino > Dino Size, chỉnh thông số rồi kéo vào DinoSpawnTable.
[CreateAssetMenu(menuName = "RideADino/Dino Size", fileName = "Size_New")]
public class DinoSizeAsset : ScriptableObject
{
    public string displayName = "NORMAL";
    public bool isNormal = true;                 // true: không thêm tag lên tên / badge
    [Min(0.1f)] public float scaleMultiplier = 1f;
    [Min(0.1f)] public float moneyMultiplier = 1f;
    [Min(0.1f)] public float staminaMultiplier = 1f;
    public int rankBonus = 0;
    public Color tagColor = Color.white;

    [Tooltip("Ánh xạ tùy chọn về enum cũ (dùng cho UI/fallback nếu cần).")]
    public DinoSizeType legacyType = DinoSizeType.Normal;
}
