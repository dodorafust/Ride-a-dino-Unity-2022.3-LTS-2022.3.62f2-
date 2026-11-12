using System;

[Serializable]
public class OwnedDino
{
    public string id;
    public DinoData data;
    public DinoVariant variant;     // null = Normal
    public DinoTrait trait;

    public OwnedDino(DinoData data, DinoVariant variant = null, DinoTrait trait = DinoTrait.None)
    {
        this.id = Guid.NewGuid().ToString("N");
        this.data = data;
        this.variant = variant;
        this.trait = trait;
    }

    public double MoneyPerSecond
    {
        get
        {
            float vm = variant != null ? variant.moneyMultiplier : 1f;
            return (double)data.baseMoney * DinoMultipliers.Of(data.rarity) * DinoMultipliers.Of(trait) * vm;
        }
    }

    public double SellValue { get { return MoneyPerSecond * 30.0; } }

    public string Title
    {
        get
        {
            if (variant == null || variant.isNormal) return data.displayName;
            return variant.displayName + " " + data.displayName;
        }
    }
}
