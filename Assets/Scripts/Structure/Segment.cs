using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Segment
{
    public LNode startPoint, endPoint;
    public int thickness;
    public int branchLevel;
    public int length;
    public int parentThickness;
    public ushort branchId;
    public ushort parentBranchId;
    public Vector3Int startPos
    {
        get
        {
            return startPoint.position;
        }
    }
    public Vector3Int endPos
    {
        get
        {
            return endPoint.position;
        }
    }

    public Segment ChangeEndpointPos(Vector3Int pos)
    {
        return new Segment
        {
            startPoint = startPoint,
            endPoint = new LNode
            {
                position = pos,
                branchLevel = endPoint.branchLevel,
                anglesDeg = endPoint.anglesDeg,
                thickness = endPoint.thickness
            },
            thickness = thickness,
            branchLevel = branchLevel,
            length = length
        };
    }
}
