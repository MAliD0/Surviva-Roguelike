using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirclePortDefinition
{
    public CirclePortType portType;
    public int count;
    public bool required;

    public bool IsInput()
    {
        return 
        portType == CirclePortType.ItemInput ||
        portType == CirclePortType.CatalystInput ||
        portType == CirclePortType.ControlInput ||
        portType == CirclePortType.EnergyInput
        ;
    }
    public bool IsOutput()
    {
        return !IsInput();
    }
}
