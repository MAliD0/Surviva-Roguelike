using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Alchemy/Circles/Sigil Definition")]
public class CircleSigilDefinition : ScriptableObject
{
    public string id;
    public string displayName;

    public CircleElement element;

    public List<AspectModifier> aspectModifiers;
    public List<AspectConversionRule> conversionRules;
}
