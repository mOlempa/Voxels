using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Node
{
    public Vector3Int position;
    public Vector3 anglesDeg;
    public int thickness;
    public int branchLevel;
    public int prevNodeThickness;
}