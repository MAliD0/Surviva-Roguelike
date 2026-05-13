[System.Serializable]
public class AlchemyItemStackData
{
    public QualityTier qualityTier = QualityTier.Stable;
    public float residueRatio;

    public bool hasCustomAspectProfile;
    public AspectProfile customAspectProfile;

    public AlchemyItemStackData()
    {
    }

    public AlchemyItemStackData(QualityTier qualityTier)
    {
        this.qualityTier = qualityTier;
    }

    public bool SetCustomAspectProfile(AspectProfile profile)
    {
        if (profile == null || profile.IsEmpty())
            return false;

        customAspectProfile = profile.Clone();
        hasCustomAspectProfile = true;
        return true;
    }

    public AspectProfile GetEffectiveProfile(AlchemyItemData baseItem)
    {
        if (hasCustomAspectProfile && customAspectProfile != null)
            return customAspectProfile;

        return baseItem != null ? baseItem.aspectProfile : null;
    }

    public AlchemyItemStackData Clone()
    {
        return new AlchemyItemStackData
        {
            qualityTier = qualityTier,
            residueRatio = residueRatio,
            hasCustomAspectProfile = hasCustomAspectProfile,
            customAspectProfile = customAspectProfile != null
                ? customAspectProfile.Clone()
                : null
        };
    }
}