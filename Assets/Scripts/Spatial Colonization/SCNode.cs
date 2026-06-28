using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SCNode
{
    public Vector3Int position;
    public bool startsBranch;
    public Vector3 direction;
    public int energy;
    public int branchLevel;

    public SCNode Clone()
    {
        return new SCNode
        {
            position = this.position,
            startsBranch = this.startsBranch,
            direction = this.direction,
            energy = this.energy,
            branchLevel = this.branchLevel,
        };
    }

    public bool IsDead
    {
        get
        {
            return energy <= 0;
        }
    }

    public override int GetHashCode()
    {
        return position.x + position.y + position.z;
    }
    public override bool Equals(object obj)
    {
        return obj is SCNode && Equals((SCNode)obj);
    }

    public bool Equals(SCNode n)
    {
        return n.position.x == position.x && n.position.y == position.y && n.position.z == position.z;
    }
}