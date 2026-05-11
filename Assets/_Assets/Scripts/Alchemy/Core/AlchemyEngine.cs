using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AlchemyEngine : MonoBehaviour
{
    [SerializeField] private AlchemyResolver resolver = new AlchemyResolver();
    [SerializeField] private AlchemyMatcher matcher = new AlchemyMatcher();
    //private ResidueCalculator residueCalculator; //is static
    [SerializeField] private QualityCalculator qualityCalculator = new QualityCalculator();
    [SerializeField] private AlchemyItemDatabase itemDatabase;

    public AlchemyResult Execute(AlchemyRequest request)
    {
        AlchemyResult alchemyResult = new AlchemyResult();

        AspectProfile inputProfile = resolver.BuildInputProfile(request.inputItems);

        AspectProfile changedProfile = resolver.ApplyCircle(inputProfile , request.circle.definition);

        List<AlchemyMatchResult> candidates = 
        matcher.FindMatches(
            changedProfile, 
            itemDatabase.getAllAlchemyItems(), 
            request.circle.definition, 
            request.knowledge);

        AlchemyMatchResult alchemyMatchResult = matcher.SelectBestMatch(candidates);

        if(alchemyMatchResult.item == null)
        {
            alchemyResult.Failed($"Didn't find suitable match for {changedProfile.ToString()}");
            return alchemyResult;
        }
        
        AspectProfile residueProfile = 
        ResidueCalculator.CalculateResidue(changedProfile, alchemyMatchResult.item.aspectProfile);

        
        alchemyResult = 
        new AlchemyResult(
            alchemyMatchResult.item,
            qualityCalculator.CalculateQuality(residueProfile, inputProfile),
            inputProfile,
            changedProfile,
            residueProfile,
            ResidueCalculator.CalculateResidueRatio(residueProfile, inputProfile),
            alchemyMatchResult.matchQuality,
            candidates
        );

        return alchemyResult;
    }

    private AlchemyResult ExecuteTransmute(AlchemyRequest request)
    {
        return null;
    }

    private bool ValidateRequest(
        AlchemyRequest request,
        out string failureReason
    )
    {
        failureReason = "didn't write method";
        return false;
    }
}
