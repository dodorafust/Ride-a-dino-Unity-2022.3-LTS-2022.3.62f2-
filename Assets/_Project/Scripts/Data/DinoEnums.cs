using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic, Secret, Divine }
public enum DinoTrait { None, Night, Meteor, Bloodmoon, Storm, Rainbow }

// Money/s = base x Rarity x Trait x Variant (task "Data Dinos").
// Multiplier của Rarity/Trait là số tạm. Multiplier của Variant nằm trong asset DinoVariant.
public static class DinoMultipliers
{
    static readonly float[] RarityMul = { 1f, 2f, 4f, 8f, 16f, 32f, 64f, 128f };
    static readonly float[] TraitMul  = { 1f, 2f, 2f, 3f, 2f, 5f };

    public static float Of(Rarity r) { return RarityMul[(int)r]; }
    public static float Of(DinoTrait t) { return TraitMul[(int)t]; }
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
