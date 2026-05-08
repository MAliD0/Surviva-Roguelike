using System;
using UnityEngine;

[Serializable]
public class AspectStack
{
    public AspectDefinition aspect;
    public int amount;
    public float purity;

    public AspectStack(AspectDefinition aspectDefinition, int amount)
    {
        this.aspect = aspectDefinition;
        this.amount = amount;
    }

    public AspectStack Clone()
    {
        return new AspectStack(aspect, amount);
    }
}