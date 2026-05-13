using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyItemData", menuName = "Alchemy/Residue")]
public class ResidueItemData : AlchemyItemData
{
    private void OnValidate()
    {
        excludeFromNormalMatching = true;
        category = AlchemyItemCategory.Residue;
        isStackable = false; // for now, until stack data supports residue identity
    }
}
