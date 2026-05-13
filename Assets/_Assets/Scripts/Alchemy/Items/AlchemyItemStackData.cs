using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class AlchemyItemStackData 
{
    public QualityTier qualityTier = QualityTier.Stable;
    public AlchemyItemData alchemyItemData;

    public bool hasCustomAspectProfile = false;
    public AspectProfile customAspectProfile;

    public AlchemyItemStackData(
        QualityTier qualityTier, 
        AlchemyItemData alchemyItemData
    )
    {
        this.qualityTier =qualityTier;
        this.alchemyItemData = alchemyItemData;
    }

    public bool SetCustomAspectProfile(AspectProfile customAspectProfile)
    {
        if(!alchemyItemData.aspectProfile.IsEmpty()) return false;
        else
        {
            this.customAspectProfile = customAspectProfile;
            hasCustomAspectProfile = true;
            return true;
        }
    }

    public AlchemyItemStackData Clone()
    {
        return new AlchemyItemStackData(qualityTier, alchemyItemData)
        {
            hasCustomAspectProfile = hasCustomAspectProfile,
            customAspectProfile = customAspectProfile != null
                ? customAspectProfile.Clone()
                : null
        };
    }
}
