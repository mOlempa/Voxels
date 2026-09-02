using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Typ enum reprezentuj¹cy rodzaje mo¿liwych preferowanych kierunków rozrostu.
 */
public enum GrowthBiasType
{
    None,
    Up,
    Down,
    Branch  // allows the biased direction to be based on previously defined branch direction, useful in collision handling
}
