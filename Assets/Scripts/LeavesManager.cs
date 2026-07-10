using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LeavesManager
{
    public static void GenerateLeaf(LeafShape leafShape, Vector3Int branchPointPos, Quaternion branchRot)
    {
        //print("Placing leaf at " + branchPointPos);

        Vector3Int leafStart = leafShape.leafPoints.FirstOrDefault(x => x.y == 0); // find the leaf tail starting position

        Vector3Int[] leafLinesCopy = leafShape.leafPoints;
        List<Vector3Int> newLeafPositions = new List<Vector3Int>();

        for (int i = 0; i < leafShape.leafPoints.Length; i++)
        {
            if (i % 2 != 0)
            {
                //print($"int y = {leafLines[i].y}; y <= {leafLines[i - 1].y}; y++");
                for (int y = leafShape.leafPoints[i - 1].y; y <= leafShape.leafPoints[i].y; y++)
                {
                    Vector3Int vec = new Vector3Int(leafShape.leafPoints[i].x, y, leafShape.leafPoints[i].z);
                    newLeafPositions.Add(vec);
                    //print("Adding position " + vec);
                }
            }

            /*leafLinesCopy[i] = leafLinesCopy[i] + branchPointPos - leafStart;
            // if it is an ending line point (index is odd)
            if (i%2 != 0)
            {
                newLeafPositions.AddRange(GenerateLine(leafLinesCopy[i - 1], leafLinesCopy[i]));
            }*/
        }


        Vector3Int[] originalLeaves = newLeafPositions.ToArray();
        //Vector3 connectionPoint = new Vector3(1, 0, 0); // Where the leaves attach to the branch

        // The additional tilt for the leaves to have
        Quaternion leafTilt = Quaternion.Euler(30f, 0f, 0f);

        // Process the array
        newLeafPositions = RotateLeaves(originalLeaves, leafStart, branchRot, leafTilt).ToList();


        foreach (var pos in newLeafPositions)
        {
            WorldManager.Instance.container[pos + branchPointPos] = new Voxel()
            {
                id = 1,
                objectId = WorldManager.Instance.assignableObjectIdList.Last()
            };
        }
    }

    public static Vector3Int[] RotateLeaves(Vector3Int[] voxels, Vector3 pivot, Quaternion branchRotation, Quaternion extraRotation)
    {
        Quaternion finalRotation = branchRotation * extraRotation;

        Vector3Int[] rotatedVoxels = new Vector3Int[voxels.Length];

        for (int i = 0; i < voxels.Length; i++)
        {
            // Convert to float-based Vector3
            Vector3 pos = voxels[i];

            // Find the position relative to the pivot
            Vector3 dirFromPivot = pos - pivot;

            // Rotate the relative position
            Vector3 rotatedDir = finalRotation * dirFromPivot;

            // Add the pivot back to get the final world/local position
            Vector3 finalPos = pivot + rotatedDir;

            // Snap back to the voxel grid
            rotatedVoxels[i] = new Vector3Int(
                Mathf.RoundToInt(finalPos.x),
                Mathf.RoundToInt(finalPos.y),
                Mathf.RoundToInt(finalPos.z)
            );
        }

        return rotatedVoxels;
    }
}
