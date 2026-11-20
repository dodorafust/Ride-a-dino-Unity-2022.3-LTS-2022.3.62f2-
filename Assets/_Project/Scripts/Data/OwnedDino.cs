using System;

[Serializable]
public class OwnedDino
{
    public string id;
    public DinoData data;
    public DinoVariant variant;     // null = Normal
    public DinoTrait trait;
    public DinoSizeType sizeType;   // hệ kích thước: Normal, Tiny, Big, Huge, Giant, Colossal

    public OwnedDino(DinoData data, DinoVariant variant = null, DinoTrait trait = DinoTrait.None, DinoSizeType sizeType = DinoSizeType.Normal)
    {
        this.id = Guid.NewGuid().ToString("N");
        this.data = data;
        this.variant = variant;
        this.trait = trait;
        this.sizeType = sizeType;
    }

    public float SizeScaleMultiplier { get { return DinoSizeModifiers.ScaleOf(sizeType); } }
    public float SizeMoneyMultiplier { get { return DinoSizeModifiers.MoneyOf(sizeType); } }
    public string SizeTag { get { return DinoSizeModifiers.LabelOf(sizeType); } }

    public double MoneyPerSecond
    {
        get
        {
            float vm = variant != null ? variant.moneyMultiplier : 1f;
            return (double)data.baseMoney * DinoMultipliers.Of(data.rarity) * DinoMultipliers.Of(trait) * vm * SizeMoneyMultiplier;
        }
    }

    public double SellValue { get { return MoneyPerSecond * 30.0; } }

    public float EffectiveRequiredStamina
    {
        get
        {
            float baseReq = data != null ? data.requiredStamina : 10f;
            float vMul = variant != null ? variant.EffectiveStaminaMultiplier : 1f;
            float sMul = DinoSizeModifiers.StaminaOf(sizeType);
            float tMul = DinoMultipliers.StaminaOf(trait);
            return (float)Math.Round(baseReq * vMul * sMul * tMul);
        }
    }

    public string Title
    {
        get
        {
            string baseTitle = (variant == null || variant.isNormal)
                ? data.displayName
                : variant.displayName + " " + data.displayName;
            string sTag = SizeTag;
            return string.IsNullOrEmpty(sTag) ? baseTitle : sTag + " " + baseTitle;
        }
    }
}
