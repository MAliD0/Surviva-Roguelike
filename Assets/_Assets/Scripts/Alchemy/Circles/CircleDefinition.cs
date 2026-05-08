using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyCircle", menuName = "Alchemy/AlchemyCircle")]
public class CircleDefinition : ScriptableObject
{
    public string id;
    public string displayName { get { return name; } set { displayName = name; } }
    
    public CircleElement element;

    public float strength = 1f;
    public float precision = 0.5f;
    public float stabilityBonus = 0f;

    public int maxComplexity = 1;

    public List<AspectModifier> aspectModifier = new();
    public List<AspectConversionRule> conversionRules = new();
}
