using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AlchemyItemData", menuName = "Alchemy/AlchemyArrayComponentMapBlock")]
public class AlchemyArrayComponentMapBlockData : MapBlockData
{
    new void OnValidate()
    {
        base.OnValidate();

        base.mapLayerType= MapLayerType.circleElementLayer;
        base.gridAligned = true;
        base.itemType = ItemType.Placeable;
    }
}
