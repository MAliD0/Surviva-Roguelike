using Sirenix.OdinInspector.Editor.StateUpdaters;
using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyItemData", menuName = "Alchemy/AlchemyItem")]
public class AlchemyItemData : ItemData, IAlchemyAspectSource
{
    public AspectProfile aspectProfile;
    public int complexity = 1;

    public AspectProfile AspectProfile => aspectProfile;
    public int Complexity => complexity;
    public bool excludeFromNormalMatching = false;
    public AlchemyItemCategory category;
}