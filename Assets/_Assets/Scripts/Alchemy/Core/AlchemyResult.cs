using System;
using System.Collections.Generic;

[Serializable]
public class AlchemyResult
{
    public bool success;
    public string failureReason;

    public AlchemyItemData outputItem;
    public QualityTier qualityTier;

    public AspectProfile originalInputProfile;
    public AspectProfile transformedProfile;
    public AspectProfile residueProfile;

    public float residueRatio;
    public float matchQuality;

    public List<AlchemyMatchResult> possibleMatches;

    public AlchemyResult()
    {
        
    }

    public AlchemyResult(AlchemyItemData outputItem, 
    QualityTier qualityTier, 
    AspectProfile input,
    AspectProfile output,
    AspectProfile residue,
    float residueRatio,
    float matchQuality,
    List<AlchemyMatchResult> possibleMatches
    )
    {
        this.success = true;
        this.outputItem = outputItem;
        this.qualityTier = qualityTier;
        this.originalInputProfile = input;
        this.transformedProfile = output;
        this.residueProfile = residue;
        this.residueRatio = residueRatio;
        this.matchQuality = matchQuality;
        this.possibleMatches = possibleMatches;
        this.failureReason = null;
    }

    public bool HasResidue()
    {
        return residueProfile != null;
    }
    public bool HasOutput()
    {
        return outputItem != null;
    }

    public void Failed(string failureReason)
    {
        success = false;
        this.failureReason = failureReason;
        outputItem = null;
    }
}
