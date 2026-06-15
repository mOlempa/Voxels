using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BranchCollisionHelper
{
    public int collisionsCount;
    public bool didCollide;
    public bool cutChildBranches;
    public int cutLevel;

    public BranchCollisionHelper()
    {
        collisionsCount = 0;
        didCollide = false;
        cutChildBranches = false;
        cutLevel = -1;
    }
}