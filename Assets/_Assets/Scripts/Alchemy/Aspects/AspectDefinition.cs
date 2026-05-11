using UnityEngine;


[CreateAssetMenu(fileName = "Aspect", menuName = "Alchemy/Aspect")]
public class AspectDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public Color color = Color.white;

    public int complexity = 1;

    [TextArea]
    public string description;
}