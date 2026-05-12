using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AlchemyRequest
{
    public AlchemyRequest(){}

    public AlchemyRequest(List<ItemSlot> inputItems, CircleInstance circle, AlchemyProcessType processType, float availableEnergy, KnowledgeState knowledge)
    {
        this.inputItems = inputItems;
        this.circle = circle;
        this.processType = processType;
        this.availableEnergy = availableEnergy;
        this.knowledge = knowledge;
    }
    public List<ItemSlot> inputItems;
    public CircleInstance circle;
    public AlchemyProcessType processType;
    public float availableEnergy;
    public KnowledgeState knowledge;
    public AlchemyItemData intendedTarget; // optional
}
