using UnityEngine;

// 1 biến thể = 1 asset. Muốn thêm biến thể mới: Create > RideADino > Dino Variant,
// chỉnh multiplier / màu / VFX, rồi thêm vào DinoSpawnTable. Không cần sửa code.
[CreateAssetMenu(menuName = "RideADino/Dino Variant", fileName = "Variant_New")]
public class DinoVariant : ScriptableObject
{
    public string displayName = "Normal";
    public bool isNormal = true;               // true: không thêm tiền tố vào tên Dino
    public float moneyMultiplier = 1f;

    [Header("Giao diện")]
    public bool useTint;                       // nhuộm màu model
    public Color tint = Color.white;
    public GameObject vfxPrefab;               // VFX gắn vào model (Particle System...)
}
