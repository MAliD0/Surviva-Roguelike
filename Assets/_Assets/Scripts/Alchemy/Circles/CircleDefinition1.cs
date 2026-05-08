using System.Collections.Generic;

public class CircleDefinition1 
{
    public string id;
    public string displayName;

    public CircleShape shape;
    public CircleElement element;

    public List<CirclePortDefinition> ports;

    public float precision = 0.5f;
    public int complexityLimit = 1;

    public List<AspectModifier> aspectModifiers;
    public List<AspectConversionRule> conversionRules;

    public string description;

    public int GetPortCount(CirclePortType type)
    {
        return ports.Find(x => x.portType == type).count;
    }
    public bool HasPort(CirclePortType type)
    {
        return ports.Find(x => x.portType == type) != null;
    }
    public bool CanHandleComplexity(int itemComplexity)
    {
        return complexityLimit >= itemComplexity;
    }
}
