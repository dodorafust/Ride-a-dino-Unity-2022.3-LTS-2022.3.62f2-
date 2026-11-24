using System;
using UnityEngine;

[Serializable]
public class OwnedDino
{
    public string id;
    public DinoData data;
    public DinoVariant variant;     // null = Normal
    public DinoTrait trait;
    public DinoSizeType sizeType;   // fallback enum cũ
    public DinoSizeAsset sizeAsset; // asset hệ kích thước (giống DinoVariant)

    public OwnedDino(DinoData data, DinoVariant variant = null, DinoTrait trait = DinoTrait.None, DinoSizeType sizeType = DinoSizeType.Normal, DinoSizeAsset sizeAsset = null)
    {
        this.id = Guid.NewGuid().ToString("N");
        this.data = data;
        this.variant = variant;
        this.trait = trait;
        this.sizeAsset = sizeAsset;
        this.sizeType = sizeAsset != null ? sizeAsset.legacyType : sizeType;
    }

    public float SizeScaleMultiplier { get { return sizeAsset != null ? Mathf.Max(0.1f, sizeAsset.scaleMultiplier) : DinoSizeModifiers.ScaleOf(sizeType); } }
    public float SizeMoneyMultiplier { get { return sizeAsset != null ? Mathf.Max(0.1f, sizeAsset.moneyMultiplier) : DinoSizeModifiers.MoneyOf(sizeType); } }
    public float SizeStaminaMultiplier { get { return sizeAsset != null ? Mathf.Max(0.1f, sizeAsset.staminaMultiplier) : DinoSizeModifiers.StaminaOf(sizeType); } }
    public int SizeRankBonus { get { return sizeAsset != null ? sizeAsset.rankBonus : DinoSizeModifiers.RankBonusOf(sizeType); } }
    public string SizeTag
    {
        get
        {
            if (sizeAsset != null)
                return sizeAsset.isNormal || string.IsNullOrEmpty(sizeAsset.displayName) ? "" : sizeAsset.displayName.ToUpperInvariant();
            return DinoSizeModifiers.LabelOf(sizeType);
        }
    }
    public Color SizeColor { get { return sizeAsset != null ? sizeAsset.tagColor : DinoSizeModifiers.ColorOf(sizeType); } }

    public float SizeSeatOffsetMultiplier(float weight = 0.35f)
    {
        return Mathf.Lerp(1f, SizeScaleMultiplier, Mathf.Clamp01(weight));
    }

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
            float sMul = SizeStaminaMultiplier;
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
