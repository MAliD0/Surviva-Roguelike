using System;
using System.Collections.Generic;

[Serializable]
public class AspectConversionRule 
{
    public AspectDefinition inputAspect;
    public float inputAmount;
    public List<AspectStack> outputAspects;
    public float efficiency = 1f;

    public bool CanApply(AspectProfile profile)
    {
        return profile.HasAspect(inputAspect);    
    }

    public void ApplyTo(AspectProfile profile)
    {
        if (CanApply(profile))
        {
            int amount = profile.GetAmount(inputAspect);
            int numberOfOutputAspects = (int)Math.Floor(amount/inputAmount);

            int numberOfAspectToRemove = (int)Math.Floor(numberOfOutputAspects * inputAmount);

            profile.TryRemoveAspect(inputAspect, numberOfAspectToRemove);

            foreach (var outputAspect in outputAspects)
            {
                int numberOfAspect = (int)Math.Floor(
                    outputAspect.amount * numberOfOutputAspects * efficiency
                ) ;

                profile.AddAspect(
                    outputAspect.aspect,
                    numberOfAspect 
                );
            }
        }
    }
}
