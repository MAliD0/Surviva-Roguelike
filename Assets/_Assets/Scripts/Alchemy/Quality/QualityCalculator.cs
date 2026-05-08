public class QualityCalculator
{
    public QualityTier CalculateQuality(float residueRatio)
    {
        return residueRatio switch
        {
            >= 0.4f => QualityTier.Impure,
            >= 0.25f and < 0.4f => QualityTier.Crude,
            >= 0.1f and < 0.25f => QualityTier.Stable,
            >= 0.03f and < 0.1f => QualityTier.Refined,
            >= 0f and < 0.03f => QualityTier.Pure,
            _ => QualityTier.Impure,
        };
    }

    public QualityTier CalculateQuality(
        AspectProfile residueProfile,
        AspectProfile originalInputProfile
    )
    {
        return CalculateQuality(ResidueCalculator.CalculateResidueRatio(residueProfile, originalInputProfile));
    }
}
