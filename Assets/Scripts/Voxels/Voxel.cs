using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public byte id;
    public ushort branchId;
    public bool isSolid
    {
        get
        {
            return (id != 0);
        }
    }
}
