using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;


public struct LNode
{
    public Vector3Int position;
    public Vector3 anglesDeg;
    public int thickness;
    public int branchLevel;
    public int prevNodeThickness;
    public ushort branchId;
    public ushort parentBranchId;

    public Transform transform;
}
