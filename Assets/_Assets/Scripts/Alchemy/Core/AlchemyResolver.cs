using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AlchemyResolver
{
    public AspectProfile BuildInputProfile(List<ItemSlot> inputItems)
    {
        AspectProfile aspectProfile = new AspectProfile();

        foreach (var item in inputItems)
        {
            if(item.itemData is not AlchemyItemData)
                continue;

            AlchemyItemData alchemyItemData = (AlchemyItemData)item.itemData; 

            if(item.amount <= 0)
            {
                Debug.LogWarning("Item amount == 0");
            }
            
            for (int i = 0; i < item.amount; i++)
            {
                aspectProfile.AddAspectProfile(alchemyItemData.aspectProfile);    
            }
        }

        return aspectProfile;
    }

    public AspectProfile ApplyCircle(AspectProfile inputProfile,CircleDefinition circle)
    {
        AspectProfile outputProfile = inputProfile.Clone();

        ApplyModifiers(outputProfile, circle.aspectModifiers);
        ApplyConversions(outputProfile, circle.conversionRules);

        return outputProfile;
    }

    public void ApplyModifiers(AspectProfile profile,List<AspectModifier> modifiers)
    {
        foreach (var aspectModifier in modifiers)
        {
            aspectModifier.ApplyTo(profile);
        }
    }

    public void ApplyConversions(AspectProfile profile, List<AspectConversionRule> conversionRules)
    {
        foreach(AspectConversionRule conversionRule in conversionRules)
        {
            conversionRule.ApplyTo(profile);
        }
    }

    //Not implemented
    public bool IsTargetAffordable(
        AspectProfile inputProfile,
        AspectProfile targetProfile
    )
    {
        return false;
    }
}
