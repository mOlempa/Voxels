using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GrowthBias
{
    /*public GrowthBiasType type;
    [HideInInspector] public Vector3 direction;

    public GrowthBias(GrowthBiasType _type)
    {
        type = _type;
        direction = GetDirection(_type);
    }*/

    public static Vector3 GetDirection(GrowthBiasType type)
    {
        switch (type)
        {
            default:
            case GrowthBiasType.None:
                return Vector3.zero;
            case GrowthBiasType.Up:
                return Vector3.forward;
            case GrowthBiasType.Down:
                return Vector3.back;
        }
    }
}

public enum GrowthBiasType
{
    None,
    Up,
    Down,
    Branch  // allows the biased direction to be based on previously defined branch direction, useful in collision handling
}