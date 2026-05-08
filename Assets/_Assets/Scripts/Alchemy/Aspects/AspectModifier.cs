using System;

[Serializable]
public class AspectModifier
{
    public AspectDefinition aspect;
    public float multiplier = 1f;
    public int flatChange = 0;
    public bool canCreateIfMissing = false;

    public void ApplyTo(AspectProfile profile)
    {
        if(! profile.HasAspect(aspect)) return;

        profile.AddAspect(aspect, flatChange);
        profile.MultiplyAspect(aspect, multiplier);
    }
}

