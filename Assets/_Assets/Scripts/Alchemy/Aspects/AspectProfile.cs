using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AspectProfile
{
    [SerializeField] private List<AspectStack> aspects = new();

    public IReadOnlyList<AspectStack> Aspects => aspects;

    public AspectProfile()
    {
        this.aspects = new List<AspectStack>();
    }

    public AspectProfile(AspectStack[] aspects)
    {
        this.aspects = aspects.ToList();
    }

    public bool HasAspect(AspectDefinition aspect)
    {
        return aspects.Find(x => x.aspect == aspect) != null;
    }

    public int GetAmount(AspectDefinition aspect)
    {
        if (aspect == null)
            return 0;

        foreach (AspectStack stack in aspects)
        {
            if (stack.aspect == aspect)
                return stack.amount;
        }

        return 0;
    }
    public int GetAspectAmount()
    {
        return aspects.Count;
    }

    public bool HasAmount(AspectDefinition aspect, int amount)
    {
        return GetAmount(aspect) >= amount;
    }
    public void AddAspectProfile(AspectProfile aspectProfile)
    {
        foreach(var aspect in aspectProfile.aspects)
        {
            AddAspect(aspect.aspect, aspect.amount);
        }
    }

    public void AddAspect(AspectDefinition aspect, int amount)
    {
        if (aspect == null || amount <= 0f)
            return;

        AspectStack stack = FindStack(aspect);

        if (stack != null)
        {
            stack.amount += amount;
        }
        else
        {
            aspects.Add(new AspectStack
            (aspect, amount));
        }
    }

    public bool TryRemoveAspect(AspectDefinition aspect, int amount)
    {
        if (aspect == null || amount <= 0f)
            return false;

        AspectStack stack = FindStack(aspect);

        if (stack == null)
            return false;

        if (stack.amount < amount)
            return false;

        stack.amount -= amount;

        if (stack.amount <= 0f)
            aspects.Remove(stack);

        return true;
    }

    public void RemoveAspectUnsafe(AspectDefinition aspect, int amount)
    {
        if (aspect == null || amount <= 0f)
            return;

        AspectStack stack = FindStack(aspect);

        if (stack == null)
            return;

        stack.amount -= amount;

        if (stack.amount <= 0f)
            aspects.Remove(stack);
    }

    public void MultiplyAspect(AspectDefinition aspect, float amount)
    {
        if (aspect == null || amount == 0f)
            return;

        AspectStack stack = FindStack(aspect);

        if (stack != null)
        {
            stack.amount = (int)Math.Floor(amount * stack.amount);
        }
        else
        {
            return;
        }
    }

    public void Merge(AspectProfile other)
    {
        if (other == null)
            return;

        foreach (AspectStack stack in other.Aspects)
        {
            AddAspect(stack.aspect, stack.amount);
        }
    }

    public AspectProfile Clone()
    {
        AspectProfile clone = new AspectProfile();

        foreach (AspectStack stack in aspects)
        {
            clone.AddAspect(stack.aspect, stack.amount);
        }

        return clone;
    }

    public int GetTotalAmount()
    {
        int total = 0;

        foreach (AspectStack stack in aspects)
        {
            total += (int)Mathf.Max(0f, stack.amount);
        }

        return total;
    }

    public bool IsEmpty()
    {
        return aspects.Count == 0 || GetTotalAmount() <= 0f;
    }

    public void RemoveZeroOrNegative()
    {
        aspects.RemoveAll(stack => stack == null || stack.aspect == null || stack.amount <= 0f);
    }

    private AspectStack FindStack(AspectDefinition aspect)
    {
        foreach (AspectStack stack in aspects)
        {
            if (stack.aspect == aspect)
                return stack;
        }

        return null;
    }

    public AspectStack[] GetAspectsNotContainedIn(AspectProfile outerAspects)
    {
        List<AspectStack> notContainingAspects = new List<AspectStack>();

        foreach (AspectStack aspect in outerAspects.aspects)
        {
            AspectStack aspectToFind = aspects.Find(x => x.aspect == aspect.aspect);

            if(aspectToFind != null)
                continue;
            
            notContainingAspects.Add(aspect);
        }

        return notContainingAspects.ToArray();
    }

    public bool ContainsAllAspectsFrom(AlchemyItemData alchemyItem)
    {
        List<AspectStack> notContainingAspects = new List<AspectStack>();

        foreach (AspectStack aspect in alchemyItem.aspectProfile.aspects)
        {
            AspectStack aspectToFind = aspects.Find(x => x.aspect == aspect.aspect);

            if(aspectToFind == null)
                return false;
        }

        return true;
    }

    public bool HasEnoughAspectsAmount(AspectProfile outerAspects)
    {
        foreach (AspectStack aspect in outerAspects.aspects)
        {
            AspectStack aspectToFind = aspects.Find(x => x.aspect == aspect.aspect);

            if (aspectToFind == null || aspectToFind.amount < aspect.amount)
                return false;
        }

        return true;
    }
    
    #region Operators override

    public static bool operator ==(AspectProfile left, AspectProfile right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left.aspects.Count != right.aspects.Count)
            return false;

        foreach (AspectStack stack in left.aspects)
        {
            AspectStack otherStack = right.aspects.Find(x => x.aspect == stack.aspect);
            if (otherStack == null || otherStack.amount != stack.amount)
                return false;
        }

        return true;
    }

    public static bool operator !=(AspectProfile left, AspectProfile right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        return this == obj as AspectProfile;
    }

    public override int GetHashCode()
    {
        int hash = 17;

        foreach (AspectStack stack in aspects)
        {
            int aspectHash = stack.aspect != null ? stack.aspect.GetHashCode() : 0;
            hash = hash * 31 + aspectHash;
            hash = hash * 31 + stack.amount.GetHashCode();
        }

        return hash;
    }

    public override string ToString()
    {
        string output = "";
        foreach (var aspect in aspects)
        {
            output += $"{aspect.aspect.ToString()} {aspect.amount} | ";
        }

        return output;
    }
    #endregion
}
