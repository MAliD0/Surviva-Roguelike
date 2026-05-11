using Sirenix.OdinInspector.Editor.StateUpdaters;
using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyItemData", menuName = "Alchemy/AlchemyItem")]
public class AlchemyItemData : ItemData
{
    public AspectProfile aspectProfile;
    public int complexity = 1;
    public bool excludeFromNormalMatching = false;
    [TextArea]
    public string alchemyDescription;

    public AlchemyItemCategory category;

    public AspectProfile GetAspectProfile()
    {
        return aspectProfile;
    }
    public void Clear()
    {
        aspectProfile = new AspectProfile();
        complexity = 1;
        alchemyDescription = "";
        category = AlchemyItemCategory.Basic;
    }
    public bool IsAlchemyItem()
    {
        return true;
    }

    public bool HasSameAspectProfileInside(AspectProfile aspectProfile)
    {
        return this.aspectProfile.GetAspectsNotContainedIn(aspectProfile) == null;
    }
}
