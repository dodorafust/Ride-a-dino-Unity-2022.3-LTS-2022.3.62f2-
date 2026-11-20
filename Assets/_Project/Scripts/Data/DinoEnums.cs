using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic, Secret, Divine }
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

public static class DinoSizeModifiers
{
    static readonly float[] ScaleMul   = { 1.0f, 0.65f, 1.30f, 1.60f, 1.95f, 2.40f };
    static readonly float[] MoneyMul   = { 1.0f, 0.85f, 1.25f, 1.50f, 2.00f, 3.00f };
    static readonly float[] StaminaMul = { 1.0f, 1.15f, 1.30f, 1.60f, 2.00f, 2.60f };
    static readonly int[]   SizeBonus  = { 0, -1, 1, 2, 3, 4 };
    static readonly string[] Labels    = { "", "TINY", "BIG", "HUGE", "GIANT", "COLOSSAL" };

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
        switch (s)
        {
            case DinoSizeType.Tiny:     return new Color(0.58f, 0.90f, 1.0f);
            case DinoSizeType.Big:      return new Color(0.45f, 1.0f, 0.45f);
            case DinoSizeType.Huge:     return new Color(1.0f, 0.85f, 0.25f);
            case DinoSizeType.Giant:    return new Color(1.0f, 0.55f, 0.18f);
            case DinoSizeType.Colossal: return new Color(1.0f, 0.25f, 0.25f);
            default:                    return Color.white;
        }
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

// Money/s = base x Rarity x Trait x Variant (task "Data Dinos").
// Multiplier của Rarity/Trait là số tạm. Multiplier của Variant nằm trong asset DinoVariant.
public static class DinoMultipliers
{
    static readonly float[] RarityMul       = { 1f, 2f, 4f, 8f, 16f, 32f, 64f, 128f };
    static readonly float[] TraitMul        = { 1f, 2f, 2f, 3f, 2f, 5f };
    static readonly float[] TraitStaminaMul = { 1f, 1.5f, 1.5f, 2f, 1.5f, 3f };

    public static float Of(Rarity r) { return RarityMul[(int)r]; }
    public static float Of(DinoTrait t) { return TraitMul[(int)t]; }
    public static float StaminaOf(DinoTrait t) { return TraitStaminaMul[(int)t]; }
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
            default:               return new Color(1f, 1f, 0.6f);
        }
    }

    public static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
}
