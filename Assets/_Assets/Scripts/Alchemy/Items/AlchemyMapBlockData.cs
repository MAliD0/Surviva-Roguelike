using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyItemData", menuName = "Alchemy/AlchemyMapBlock")]
public class AlchemyMapBlockData : MapBlockData
{
    public AspectProfile aspectProfile;
    public int complexity = 1;
    public bool excludeFromNormalMatching = false;
    public string alchemyDescription;
    public AlchemyItemCategory category;
}