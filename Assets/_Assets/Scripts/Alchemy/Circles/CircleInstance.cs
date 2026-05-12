using System.Collections.Generic;
using UnityEngine;

public class CircleInstance: MonoBehaviour
{
    public CircleDefinition definition;
    public int level = 1;
    public float currentEnergy;
    public bool isActive = true;

    public bool HasEnoughEnergy(float amount)
    {
        return currentEnergy >= amount;
    }
    public void AddEnergy(float amount)
    {
        currentEnergy += amount;
    }
    public bool ConsumeEnergy(float amount)
    {
        if(HasEnoughEnergy(amount))
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
        
    }
    public CircleDefinition GetDefinition()
    {
        return definition;
    }
}
