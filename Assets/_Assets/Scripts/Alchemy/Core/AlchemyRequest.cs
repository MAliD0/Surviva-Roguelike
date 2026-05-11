using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AlchemyRequest
{
    public List<ItemSlot> inputItems;
    public CircleInstance circle;
    public AlchemyProcessType processType;
    public float availableEnergy;
    public KnowledgeState knowledge;
    public AlchemyItemData intendedTarget; // optional
}
