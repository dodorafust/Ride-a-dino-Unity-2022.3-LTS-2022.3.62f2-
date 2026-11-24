using UnityEngine;

[CreateAssetMenu(menuName = "RideADino/Dino Data", fileName = "NewDino")]
public class DinoData : ScriptableObject
{
    public string displayName = "Dino";
    public Rarity rarity = Rarity.Common;
    public DinoSpeciesSize size = DinoSpeciesSize.Medium; // kích thước gốc của loài (9 bậc, kết hợp cùng Hệ Size)
    public DinoDiet diet = DinoDiet.Herbivore;
    [Min(1)] public int zone = 1;

    [Header("Hiển thị")]
    public Sprite icon;
    public GameObject modelPrefab;
    public RuntimeAnimatorController animatorController;
    [Min(0.1f)] public float modelScale = 1f;
    public string seatBoneName = "spine";          // tên xương lưng trên model để Seat bám theo
    public Vector3 seatOffset = new Vector3(0f, 0.35f, 0f); // bù độ cao/tiến lùi từ xương lưng (ở scale 1)
    [Tooltip("Mức độ nhân Seat Offset theo Hệ Size (0 = giữ nguyên như Normal, 1 = nhân theo đúng tỉ lệ phóng to). Mặc định 0.35 để người ngồi bám sát lưng.")]
    [Range(0f, 1f)] public float seatSizeScaleWeight = 0.35f;
    [Tooltip("Chỉnh vị trí bù riêng khi cầm loài Dino này trên tay (X/Y/Z).")]
    public Vector3 holdPositionOffset = Vector3.zero;
    [Tooltip("Chỉnh góc xoay bù riêng khi cầm loài Dino này trên tay (độ X/Y/Z, dùng nếu model gốc bị ngửa/chúi).")]
    public Vector3 holdRotationOffset = Vector3.zero;

    [Header("Economy")]
    public float baseMoney = 1f;               // Money/s cơ bản

    [Header("Ride / Tame")]
    public float requiredStamina = 10f;        // Stamina cần để tame chắc chắn
    [Min(3f)] public float tameTime = 5f;      // giây để vòng Tame đầy
    public float runSpeed = 6f;
    [Min(0.1f)] public float riddenSpeedMultiplier = 1f; // nhân thêm vào tốc độ khi đang cưỡi riêng cho loài này (1 = dùng mặc định của Dino prefab)
    [Tooltip("Dành cho Dino ăn cỏ: bật nếu muốn con này hết Stamina trước khi tame xong sẽ giết người chơi thay vì hất văng đi.")]
    public bool killOnStaminaEmptyIfUntamed = false;

    [Header("Cutscene Giết Người Chơi (Khi hết Stamina)")]
    [Tooltip("Chọn trực tiếp 1 kiểu chết cố định cho loài Dino này, hoặc để AutoBySpecies để lấy theo các ô tích chọn bên dưới.")]
    public DinoKillCutsceneStyle killCutsceneStyle = DinoKillCutsceneStyle.AutoBySpecies;
    [Tooltip("Khi để AutoBySpecies: cho phép kiểu 1 - Vồ & Cạp khi người chơi nằm ngửa.")]
    public bool allowKillPounceBite = true;
    [Tooltip("Khi để AutoBySpecies: cho phép kiểu 2 - Hất khỏi lưng rồi đớp nuốt chửng vào bụng.")]
    public bool allowKillTossSwallow = true;
    [Tooltip("Khi để AutoBySpecies: cho phép kiểu 3 - Giẫm bẹp xuống đất.")]
    public bool allowKillStomp = false;
    [Tooltip("Góc quay camera ưu tiên khi loài Dino này giết người chơi (RandomMultiAngle = đổi nhiều góc quay điện ảnh).")]
    public DinoKillCameraStyle killCameraStyle = DinoKillCameraStyle.RandomMultiAngle;

    public DinoKillCutsceneStyle ResolveKillStyle()
    {
        if (killCutsceneStyle != DinoKillCutsceneStyle.AutoBySpecies)
            return killCutsceneStyle;

        DinoKillCutsceneStyle[] pool = new DinoKillCutsceneStyle[3];
        int count = 0;
        if (allowKillPounceBite) pool[count++] = DinoKillCutsceneStyle.PounceAndBite;
        if (allowKillTossSwallow) pool[count++] = DinoKillCutsceneStyle.TossAndSwallow;
        if (allowKillStomp) pool[count++] = DinoKillCutsceneStyle.Stomp;

        if (count > 0)
            return pool[Random.Range(0, count)];

        // Dự phòng theo loài & kích thước nếu người dùng bỏ tích cả 3 ô
        if (diet == DinoDiet.Herbivore)
            return DinoKillCutsceneStyle.Stomp;
        if (size <= DinoSpeciesSize.SmallMedium)
            return DinoKillCutsceneStyle.PounceAndBite;
        if (size >= DinoSpeciesSize.VeryLarge)
            return Random.value < 0.5f ? DinoKillCutsceneStyle.TossAndSwallow : DinoKillCutsceneStyle.Stomp;
        return Random.value < 0.5f ? DinoKillCutsceneStyle.PounceAndBite : DinoKillCutsceneStyle.TossAndSwallow;
    }

    [Header("Giới hạn xuất hiện trên đường chạy")]
    [Tooltip("Số lượng tối đa loài này xuất hiện cùng lúc trên đường chạy (0 = không giới hạn). Con đã bắt (Tame xong) không tính vào giới hạn này nên sẽ tự bổ sung con mới.")]
    [Min(0)] public int maxAliveOnTrack = 0;

    [Header("Ăn thịt / Giẫm nát / Đẩy Dino / AI Húc vật cản")]
    [Tooltip("Bật: AI hoang dã tự húc vỡ vật cản nhỏ hơn mà không cần né.")]
    public bool wildSmashBreakables = true;
    [Tooltip("Cộng thêm bán kính ăn thịt / giẫm nát (m) so với Wall Hit Radius.")]
    [Min(0f)] public float eatRadiusBonus = 0f;
    [Tooltip("Dịch tâm vòng quét ăn thịt / giẫm nát về phía trước mặt Dino (Z > 0 = lên phía trước, ở scale 1).")]
    public Vector3 eatCenterOffset = new Vector3(0f, 0f, 1.4f);
    [Tooltip("Tên xương miệng/đầu trên model (để trống sẽ tự tìm head/jaw/mouth hoặc dùng Mouth Offset).")]
    public string mouthBoneName = "head";
    [Tooltip("Vị trí miệng tương đối (ở scale 1) nếu không dùng xương miệng, hoặc bù thêm từ xương miệng.")]
    public Vector3 mouthOffset = new Vector3(0f, 1.4f, 1.6f);
    [Tooltip("Số mảnh thịt/xương văng ra khi Dino ăn cỏ giẫm nát (hoặc khi bật Debris ở Dino ăn thịt).")]
    [Min(0)] public int eatDebrisCount = 6;
    [Min(0f)] public float eatShakeIntensity = 0.25f;
    [Min(0f)] public float eatShakeDuration = 0.18f;
    [Tooltip("Hệ số đẩy dạt sang ngang khi loài này bị Dino lớn hơn đi sát qua.")]
    [Min(0.1f)] public float largerDinoPushWeight = 2.2f;
    [Tooltip("Giới hạn lực đẩy dạt tối đa khi tách đàn / bị Dino lớn ép.")]
    [Min(0.5f)] public float maxSeparationPush = 2.5f;
    [Tooltip("Tắt: Dino nhỏ không tự dạt ra trước mặt Carnivore đang cưỡi để người chơi ăn thịt dễ hơn.")]
    public bool yieldToRiddenCarnivore = false;

    [Header("Phần thưởng Dino Index (để 0 sẽ tự tính theo Base Money & Required Stamina)")]
    [Min(0)] public double indexRewardCash = 0;
    [Min(0f)] public float indexRewardStamina = 0f;
    [Min(0)] public double indexAllSizesRewardCash = 0;
    [Min(0)] public double indexAllVariantsRewardCash = 0;

    public double EffectiveIndexRewardCash
    {
        get
        {
            if (indexRewardCash > 0) return indexRewardCash;
            return System.Math.Round(baseMoney * DinoMultipliers.Of(rarity) * 100.0);
        }
    }

    public float EffectiveIndexRewardStamina
    {
        get
        {
            if (indexRewardStamina > 0f) return indexRewardStamina;
            return Mathf.Max(5f, Mathf.Round(requiredStamina * 2f));
        }
    }

    public double EffectiveAllSizesRewardCash
    {
        get
        {
            if (indexAllSizesRewardCash > 0) return indexAllSizesRewardCash;
            return EffectiveIndexRewardCash * 2.0;
        }
    }

    public double EffectiveAllVariantsRewardCash
    {
        get
        {
            if (indexAllVariantsRewardCash > 0) return indexAllVariantsRewardCash;
            return EffectiveIndexRewardCash * 4.0;
        }
    }
}
