using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

    public AlchemyItemStackData Clone()
    {
        return new AlchemyItemStackData(qualityTier, alchemyItemData);
    }
}
