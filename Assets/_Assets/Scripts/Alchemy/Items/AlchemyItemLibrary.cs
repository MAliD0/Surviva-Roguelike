using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlchemyItemLibrary : ItemLibrary
{
    [SerializeField] string ItemsFolder = "Alchemy";

    public AlchemyItemData GetByAspectProfile(AspectProfile aspectProfile)
    {
        foreach(AlchemyItemData alchemyItem in dataLibrary.Values)
        {
            if(alchemyItem.aspectProfile == aspectProfile)
            {
                return alchemyItem;
            }
            continue;
        }
        

        return null;
    }

}
