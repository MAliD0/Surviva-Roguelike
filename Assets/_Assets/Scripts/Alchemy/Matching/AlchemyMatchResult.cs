using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlchemyMatchResult
{
    public AlchemyItemData item;
    public float matchQuality;

    public bool isAffordable;
    public bool isKnown;
    public bool isComplexityAllowed;

    public AspectProfile usedAspects;
    public AspectProfile leftoverAspects;

    public AlchemyMatchResult(
        AlchemyItemData itemData, 
        float matchQuality,
        bool isAffordable,
        bool isKnown,
        bool isComplexityAllowed,
        AspectProfile usedAspects,
        AspectProfile lefoverAspects    
    )
    {
        this.item = itemData;
        this.matchQuality = matchQuality;
        this.isAffordable = isAffordable;
        this.isKnown = isKnown;
        this.isComplexityAllowed = isComplexityAllowed;
        this.usedAspects = usedAspects;
        this.leftoverAspects = lefoverAspects;
    }
}
