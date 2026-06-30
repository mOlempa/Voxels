using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utilities;
using static Constants;

public class Trunk : MonoBehaviour
{
    [Range(1, 50)]
    public int nodesAmount = 3;
    [Range(1, 50)]
    public int segmentLength = 10;
    [Range(1, 20)]
    public int startingThickness = 5;
    [Range(-1, 1)]
    public float thicknessIncrease = 1;
    [Range(0, 1)]
    public float minBranchHeight = 0;
    [Range(0, 1)]
    public float maxBranchHeight = 1;

    float unroundedCurrentThickness = 1;
    public List<SCNode> nodes;

    public List<SCNode> GenerateTrunk(Vector3Int startingPoint)
    {
        List<SCNode> nodes = new List<SCNode>()
        {
            new SCNode()
            {
                position = startingPoint,
                direction = new Vector3(0, 1, 0),
                thickness = startingThickness,
                startsBranch = false,
                energy = MAX_ENERGY,
                branchLevel = 0,
            }
        };
        unroundedCurrentThickness = startingThickness;

        // generate nodes
        for (int i = 1; i < nodesAmount; i++) 
        {
            unroundedCurrentThickness += thicknessIncrease;
            SCNode node = new SCNode()
            {
                position = nodes[i - 1].position + Vector3Int.RoundToInt(nodes[i - 1].direction * segmentLength),
                direction = nodes[i - 1].direction,   // take previous direction
                thickness = Mathf.RoundToInt(unroundedCurrentThickness) > 0 ? Mathf.RoundToInt(unroundedCurrentThickness) : 1,
                startsBranch = false,
                energy = MAX_ENERGY,
                branchLevel = 0,
            };

            nodes.Add(node);
        }

        // generate segments
        for(int n = 0; n < nodesAmount-1; n++)
        {
            GenerateVoxels(GenerateThickLine(nodes[n].position, nodes[n + 1].position, nodes[n].thickness));
        }

        int indexA = Mathf.RoundToInt(minBranchHeight * nodesAmount);
        int indexB = Mathf.RoundToInt(maxBranchHeight * nodesAmount);
        List<SCNode> branchNodes = new List<SCNode>();
        for(int i = indexA; i < indexB; i++)
        {
            print("Adding branch node at index " + i + " with trunk thickness " + nodes[i].thickness);
            branchNodes.Add(nodes[i]);
        }

        return branchNodes;

    }

    void GenerateVoxels(List<Vector3Int> positions)
    {
        foreach (var pos in positions)
        {
            if (WorldManager.Instance.container[pos].id == 2) continue;
            WorldManager.Instance.container[pos] = new Voxel()
            {
                //id = 1
                id = 3
            };
        }
    }
}
