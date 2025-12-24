using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoiseGenerator<T>
{
    public abstract float[,] GenerateNoiseMap(T parameters);
}
