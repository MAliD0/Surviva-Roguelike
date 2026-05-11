using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class AlchemyTest : MonoBehaviour
{
    [SerializeField] AlchemyEngine alchemyEngine;
    [SerializeField] AlchemyRequest alchemyRequest;

    [SerializeField] AlchemyResult alchemyResult;
    
    [Button]
    public void Check()
    {
        print("enter");
        alchemyResult = alchemyEngine.Execute(alchemyRequest);
    }
}
