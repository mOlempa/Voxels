using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * Klasa zarz¹dzaj¹ca generowaniem liœci.
 */
public static class LeavesManager
{
    /**
     * Metoda generuj¹ca liœæ na danej pozycji.
     * @param leafShape kszta³t liœcia do wygenerowania.
     * @param branchPointPos pozycja na ga³êzi, na której zostanie wygenerowany liœæ.
     * @param branchRot obrót liœcia.
     */
    public static void GenerateLeaf(LeafShape leafShape, Vector3Int branchPointPos, Quaternion branchRot)
    {
        Vector3Int leafStart = leafShape.leafPoints.FirstOrDefault(x => x.y == 0); // find the leaf tail starting position

        Vector3Int[] leafLinesCopy = leafShape.leafPoints;
        List<Vector3Int> newLeafPositions = new List<Vector3Int>();

        for (int i = 0; i < leafShape.leafPoints.Length; i++)
        {
            if (i % 2 != 0)
            {
                for (int y = leafShape.leafPoints[i - 1].y; y <= leafShape.leafPoints[i].y; y++)
                {
                    Vector3Int vec = new Vector3Int(leafShape.leafPoints[i].x, y, leafShape.leafPoints[i].z);
                    newLeafPositions.Add(vec);
                }
            }

        }

        Vector3Int[] originalLeaves = newLeafPositions.ToArray();

        // The additional tilt for the leaves to have
        Quaternion leafTilt = Quaternion.Euler(30f, 0f, 0f);

        // Process the array
        newLeafPositions = RotateLeaves(originalLeaves, leafStart, branchRot, leafTilt).ToList();

        foreach (var pos in newLeafPositions)
        {
            MainManager.Instance.container[pos + branchPointPos] = new Voxel()
            {
                id = 1,
                objectId = MainManager.Instance.assignableObjectIdList.Last()
            };
        }
    }

    /**
     * Metoda zarz¹dzaj¹ca obrotem liœcia.
     * @param voxelPositions pozycje wokseli liœcia.
     * @param pivot miejsce po³¹czenia liœcia z ga³êzi¹.
     * @param branchRotation rotacja ga³êzi.
     * @param extraRotation rotacja nadana liœciu.
     * @return Vector3Int[] nowe pozycje wokseli dla obróconego liœcia.
     */
    public static Vector3Int[] RotateLeaves(Vector3Int[] voxelPositions, Vector3 pivot, 
        Quaternion branchRotation, Quaternion extraRotation)
    {
        Quaternion finalRotation = branchRotation * extraRotation;

        Vector3Int[] rotatedVoxels = new Vector3Int[voxelPositions.Length];

        for (int i = 0; i < voxelPositions.Length; i++)
        {
            // Convert to float-based Vector3
            Vector3 pos = voxelPositions[i];

            // Find the position relative to the pivot and rotate
            Vector3 dirFromPivot = pos - pivot;
            Vector3 rotatedDir = finalRotation * dirFromPivot;

            // Add the pivot back to get the final world/local position
            Vector3 finalPos = pivot + rotatedDir;

            rotatedVoxels[i] = new Vector3Int(
                Mathf.RoundToInt(finalPos.x),
                Mathf.RoundToInt(finalPos.y),
                Mathf.RoundToInt(finalPos.z)
            );
        }

        return rotatedVoxels;
    }
}
