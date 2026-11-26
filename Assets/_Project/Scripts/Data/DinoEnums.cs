using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic, Secret, Eternal, Divine }
public enum DinoTrait { None, Night, Meteor, Bloodmoon, Storm, Rainbow }
public enum ObjectSize { Small = 0, Medium = 1, Large = 2, Giant = 3 }
public enum DinoSpeciesSize
{
    Tiny = 0,
    Small = 1,
    SmallMedium = 2,
    Medium = 3,
    MediumLarge = 4,
    Large = 5,
    VeryLarge = 6,
    Giant = 7,
    Colossal = 8
}
public enum DinoDiet { Herbivore = 0, Carnivore = 1 }
public enum DinoSizeType { Normal = 0, Tiny = 1, Big = 2, Huge = 3, Giant = 4, Colossal = 5 }

public enum DinoKillCutsceneStyle
{
    AutoBySpecies = 0,     // Tự chọn theo loài / các ô tích chọn bên dưới
    PounceAndBite = 1,     // Kiểu 1: Vồ & Cạp khi người chơi nằm ngửa
    TossAndSwallow = 2,    // Kiểu 2: Hất khỏi lưng rồi đớp nuốt chửng vào bụng
    Stomp = 3              // Kiểu 3: Giẫm bẹp xuống đất
}

public enum DinoKillCameraStyle
{
    RandomMultiAngle = 0,  // Đa góc quay điện ảnh (chuyển cắt cảnh tự động)
    LowAnglePredator = 1,  // Góc thấp sát đất ngước lên Dino
    CloseUpVictimPOV = 2,  // Góc cận mặt / hàm Dino từ phía nạn nhân
    SideOrbitAction = 3    // Góc nghiêng xoay vòng quanh cảnh hành động
}

[System.Serializable]
public class DinoSizeTypeConfig
{
    public DinoSizeType sizeType;
    public string label;
    [Min(0.1f)] public float scaleMultiplier = 1f;
    [Min(0.1f)] public float moneyMultiplier = 1f;
    [Min(0.1f)] public float staminaMultiplier = 1f;
    public int rankBonus = 0;
    public Color tagColor = Color.white;
}

public static class DinoSizeModifiers
{
    static readonly float[] ScaleMul   = { 1.0f, 0.65f, 1.30f, 1.60f, 1.95f, 2.40f };
    static readonly float[] MoneyMul   = { 1.0f, 0.85f, 1.25f, 1.50f, 2.00f, 3.00f };
    static readonly float[] StaminaMul = { 1.0f, 1.15f, 1.30f, 1.60f, 2.00f, 2.60f };
    static readonly int[]   SizeBonus  = { 0, -1, 1, 2, 3, 4 };
    static readonly string[] Labels    = { "", "TINY", "BIG", "HUGE", "GIANT", "COLOSSAL" };
    static readonly Color[] Colors     = {
        Color.white,
        new Color(0.58f, 0.90f, 1.0f),
        new Color(0.45f, 1.0f, 0.45f),
        new Color(1.0f, 0.85f, 0.25f),
        new Color(1.0f, 0.55f, 0.18f),
        new Color(1.0f, 0.25f, 0.25f)
    };

    public static int MinRankDiffToEat = 4;

    public static DinoSizeTypeConfig[] CreateDefaultConfigs()
    {
        DinoSizeTypeConfig[] arr = new DinoSizeTypeConfig[6];
        for (int i = 0; i < 6; i++)
        {
            arr[i] = new DinoSizeTypeConfig
            {
                sizeType = (DinoSizeType)i,
                label = Labels[i],
                scaleMultiplier = ScaleMul[i],
                moneyMultiplier = MoneyMul[i],
                staminaMultiplier = StaminaMul[i],
                rankBonus = SizeBonus[i],
                tagColor = Colors[i]
            };
        }
        return arr;
    }

    public static void ApplyConfigs(DinoSizeTypeConfig[] configs)
    {
        if (configs == null) return;
        for (int i = 0; i < configs.Length; i++)
        {
            DinoSizeTypeConfig c = configs[i];
            if (c == null) continue;
            int idx = (int)c.sizeType;
            if (idx < 0 || idx >= ScaleMul.Length) continue;
            ScaleMul[idx] = Mathf.Max(0.1f, c.scaleMultiplier);
            MoneyMul[idx] = Mathf.Max(0.1f, c.moneyMultiplier);
            StaminaMul[idx] = Mathf.Max(0.1f, c.staminaMultiplier);
            SizeBonus[idx] = c.rankBonus;
            Labels[idx] = c.label ?? "";
            Colors[idx] = c.tagColor;
        }
    }

    public static float ScaleOf(DinoSizeType s)   { return ScaleMul[(int)s]; }
    public static float MoneyOf(DinoSizeType s)   { return MoneyMul[(int)s]; }
    public static float StaminaOf(DinoSizeType s) { return StaminaMul[(int)s]; }
    public static int   RankBonusOf(DinoSizeType s) { return SizeBonus[(int)s]; }
    public static string LabelOf(DinoSizeType s)  { return Labels[(int)s]; }

    public static float SeatOffsetMultiplier(DinoSizeType s, float weight = 0.35f)
    {
        return Mathf.Lerp(1f, ScaleOf(s), Mathf.Clamp01(weight));
    }

    public static Color ColorOf(DinoSizeType s)
    {
        int idx = (int)s;
        return (idx >= 0 && idx < Colors.Length) ? Colors[idx] : Color.white;
    }

    public static int CombinedRank(DinoSpeciesSize baseSize, DinoSizeType sizeType)
    {
        return (int)baseSize * 2 + RankBonusOf(sizeType);
    }

    public static ObjectSize RankToObjectSize(int rank)
    {
        if (rank <= 3) return ObjectSize.Small;
        if (rank <= 7) return ObjectSize.Medium;
        if (rank <= 11) return ObjectSize.Large;
        return ObjectSize.Giant;
    }

    public static ObjectSize ToObjectSize(float effectiveScale)
    {
        if (effectiveScale < 1.0f) return ObjectSize.Small;
        if (effectiveScale < 1.7f) return ObjectSize.Medium;
        if (effectiveScale < 2.6f) return ObjectSize.Large;
        return ObjectSize.Giant;
    }
}

// Money/s = baseMoney x Trait x Variant x Size (RarityMul = 1 để Base Money chính là $/s thực tế của size Normal).
public static class DinoMultipliers
{
    static readonly float[] RarityMul       = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
    static readonly float[] TraitMul        = { 1f, 2f, 2f, 3f, 2f, 5f };
    static readonly float[] TraitStaminaMul = { 1f, 1.5f, 1.5f, 2f, 1.5f, 3f };

    public static float Of(Rarity r) { return RarityMul[Mathf.Clamp((int)r, 0, RarityMul.Length - 1)]; }
    public static float Of(DinoTrait t) { return TraitMul[Mathf.Clamp((int)t, 0, TraitMul.Length - 1)]; }
    public static float StaminaOf(DinoTrait t) { return TraitStaminaMul[Mathf.Clamp((int)t, 0, TraitStaminaMul.Length - 1)]; }
}

public static class DinoColors
{
    public static Color Of(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return new Color(0.82f, 0.82f, 0.82f);
            case Rarity.Uncommon:  return new Color(0.25f, 0.85f, 0.25f);
            case Rarity.Rare:      return new Color(0.2f, 0.55f, 1f);
            case Rarity.Epic:      return new Color(0.8f, 0.25f, 1f);
            case Rarity.Legendary: return new Color(1f, 0.65f, 0.1f);
            case Rarity.Mythic:    return new Color(1f, 0.25f, 0.25f);
            case Rarity.Secret:    return new Color(0.1f, 0.9f, 0.9f);
            case Rarity.Eternal:   return new Color(0.95f, 0.35f, 0.95f);
            case Rarity.Divine:    return new Color(1f, 0.95f, 0.55f);
            default:               return new Color(1f, 1f, 0.6f);
        }
    }

    public static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
}
