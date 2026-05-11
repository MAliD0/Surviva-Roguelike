using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyItemData", menuName = "Alchemy/Residue")]
public class ResidueItemData : AlchemyItemData
{
    public new bool excludeFromNormalMatching = true;
    public new AlchemyItemCategory category = AlchemyItemCategory.Residue;
}
