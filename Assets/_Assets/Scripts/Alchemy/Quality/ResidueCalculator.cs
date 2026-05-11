using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResidueCalculator
{
    public static AspectProfile CalculateResidue(
        AspectProfile inputProfile,
        AspectProfile usedProfile
    )
    {
        return inputProfile.GetLeftoverAfterUsing(usedProfile); 
    }

    public static float CalculateResidueAmount(AspectProfile residueProfile)
    {
        return residueProfile.GetTotalAmount();
    }

    public static float CalculateResidueRatio(
        AspectProfile residueProfile,
        AspectProfile originalInputProfile
    )
    {
        float residueAmount = residueProfile.GetTotalAmount();
        float originalInputAmount = originalInputProfile.GetTotalAmount();

        return residueAmount / originalInputAmount;
    }
}
