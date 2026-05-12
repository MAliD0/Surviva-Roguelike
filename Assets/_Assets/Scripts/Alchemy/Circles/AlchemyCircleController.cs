using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlchemyCircleController : MonoBehaviour
{
    [SerializeField] CircleInstance circleInstance;

    void Awake()
    {
        circleInstance = this.GetComponent<CircleInstance>();
    }
}
