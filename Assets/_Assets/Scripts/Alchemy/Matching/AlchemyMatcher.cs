using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class AlchemyMatcher 
{
    float aspectMultiplier;

    public List<AlchemyMatchResult> FindMatches(
        AspectProfile inputProfile,
        IEnumerable<AlchemyItemData> candidates,
        CircleDefinition circle,
        KnowledgeState knowledge
    )
    {
        List<AlchemyItemData> possibleAlchemyItems = candidates.ToList();

        //Circle has same or more complexity
        for(int i = possibleAlchemyItems.Count - 1; i >= 0; i--)
        {
            if(circle.maxComplexity < possibleAlchemyItems[i].complexity)
                possibleAlchemyItems.RemoveAt(i);
        }

        //Input contains all aspects of candidate 
       for(int i = possibleAlchemyItems.Count - 1; i >= 0; i--)
        {
            if (!inputProfile.ContainsAllAspectsFrom(possibleAlchemyItems[i]))
                possibleAlchemyItems.RemoveAt(i);
        }

        //InputProfile has enough number of aspects
        for(int i = possibleAlchemyItems.Count - 1; i >= 0; i--)
        {
            if(inputProfile.HasEnoughAspectsAmount(possibleAlchemyItems[i].aspectProfile))
                possibleAlchemyItems.RemoveAt(i);
        }

        List<AlchemyMatchResult> results = new();

        foreach (AlchemyItemData candidate in possibleAlchemyItems)
        {
            AlchemyMatchResult result = EvaluateMatch(
                inputProfile,
                candidate,
                circle,
                knowledge
            );

            results.Add(result);
        }

        return results;

    }

    public AlchemyMatchResult FindBestMatch(
        AspectProfile inputProfile,
        List<AlchemyItemData> candidates,
        CircleDefinition circle,
        KnowledgeState knowledge
    )
    {
        List<AlchemyMatchResult> alchemyMatchResults = FindMatches(inputProfile, candidates,circle,knowledge);
        
        AlchemyMatchResult bestMatch = alchemyMatchResults[0];
        for (int i = 1; i < alchemyMatchResults.Count - 1; i++)
        {
            Debug.Log($"{bestMatch.item.name}: {bestMatch.matchQuality} | {alchemyMatchResults[i].item.name}: {alchemyMatchResults[i].matchQuality}");
            if(alchemyMatchResults[i].matchQuality > bestMatch.matchQuality)
                bestMatch = alchemyMatchResults[i];
        }        

        return bestMatch;
    }

    public AlchemyMatchResult SelectBestMatch(
        List<AlchemyMatchResult> candidates
    )
    {
        AlchemyMatchResult bestMatch = candidates[0];
        for (int i = 1; i < candidates.Count - 1; i++)
        {
            Debug.Log($"{bestMatch.item.name}: {bestMatch.matchQuality} | {candidates[i].item.name}: {candidates[i].matchQuality}");
            if(candidates[i].matchQuality > bestMatch.matchQuality)
                bestMatch = candidates[i];
        }        

        return bestMatch;
    }

    private AlchemyMatchResult EvaluateMatch(
        AspectProfile inputProfile,
        AlchemyItemData candidate,
        CircleDefinition circle,
        KnowledgeState knowledge
    )
    {
        /*TODO:
        1.Add is affordable check
        2.

        */
        AlchemyMatchResult alchemyMatchResult = 
        new AlchemyMatchResult(
            candidate,
            CalculateMatchQuality(inputProfile, candidate.aspectProfile),
            true,
            true,
            circle.maxComplexity >= candidate.complexity,
            candidate.aspectProfile,
            new AspectProfile(inputProfile.GetAspectsNotContainedIn(candidate.aspectProfile))
        );

        return alchemyMatchResult;
    }

    private float CalculateMatchQuality(
        AspectProfile inputProfile,
        AspectProfile targetProfile
    )
    {
        AspectStack[] differentAspects = inputProfile.GetAspectsNotContainedIn(targetProfile);

        int amountOfDifferentAspects = inputProfile.GetAspectAmount() - targetProfile.GetAspectAmount();
        
        float differentAspectsTotalAmount = 0;
        foreach (var item in differentAspects)
        {
            differentAspectsTotalAmount += item.amount;
        }
        

        float percentOfDifferentAspects = amountOfDifferentAspects / inputProfile.GetAspectAmount();
        float percentOfDifferentAspectsAmount = differentAspectsTotalAmount / inputProfile.GetTotalAmount(); 

        return 1f - (percentOfDifferentAspects * percentOfDifferentAspectsAmount);        
    }
}
