using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.HableCurve;
using static Utilities;
using static Constants;
using Unity.VisualScripting;

[RequireComponent(typeof(Trunk))]
public class SpaceColonizer : MonoBehaviour
{
    public bool enableDebug = false;

    [Header("General")]
    public int iterations = 5;
    public bool showAttractors = true;
    [Range(1, 50)]
    public int maxThickness = 5;
    [Range(0,1)]
    public float biasStrength = 0;

    [Header("Trunk")]
    Trunk trunk;
    [Range(1, 6)]
    public int branchesPerTrunkNode = 1;
    public int trunkBranchAngle = 90;

    [Highlight(1, 0.8f, 0)]
    [Header("Attractors Settings")]
    [Range(20, 5000)]
    public int attractorsAmount = 100;
    public GameObject attractorSpawnArea;
    public Vector3 spawnAreaScale = Vector3.one;
    public Vector3Int spawnAreaOffset = Vector3Int.zero;

    [Range(1, 100)]
    public int maxDistance = 10;
    [Range(1, 100)]
    public int minDistance = 2;
    [Range(1, 100)]
    public int attractorKillRadius = 2;

    [Header("Nodes Settings")]
    [Range(2, 100)]
    public int segmentLength = 5;
    [Range(5, 180)]
    public int maxBranchRotationAngle = 45;
    [Range(5, 180)]
    public int maxDebranchRotationAngle = 90;
    [Range(1, 100)]
    public int maxBranchLevel = 10;
    [Range(0, 1)]
    public float randomizeBranchDirection = 0;

    
    HashSet<SCNode> nodes = new HashSet<SCNode>();
    HashSet<Vector3Int> attractors = new HashSet<Vector3Int>();
    HashSet<SCNode> newNodes = new HashSet<SCNode>();
    Dictionary<Vector3Int, List<Vector3Int>> nodesWithAttractors = new Dictionary<Vector3Int, List<Vector3Int>>();

    /*Vector3 GetTrunkBranchRandDir()
    {
        Quaternion localRotation = Quaternion.Euler(-90, 0, 0);
        // rotate branch around its own axis (global Y axis)
        Quaternion quaternion = Quaternion.Euler(0, Random.Range(0, 360), 0);
        // returns new trunk branch direction vector with angle between trunk node dir vector and the returned vector defined above
    }*/

    public void Colonize(Vector3Int startingPoint)
    {
        trunk = GetComponent<Trunk>();
        List<SCNode> branchStartingNodes = trunk.GenerateTrunk(startingPoint);
        /*nodes.Add(new SCNode()
        {
            position = new Vector3Int(0, 0, 0),
            startsBranch = false,
            direction = new Vector3(0, 1, 0),
            energy = Constants.MAX_ENERGY,
            branchLevel = 0,
            thickness = maxThickness,
        });*/

        foreach(SCNode node in branchStartingNodes)
        {
            //GetRandomRotatedDirection(node.direction, trunkBranchAngle);
            nodes.Add(new SCNode()
            {
                position = node.position,
                startsBranch = false,
                direction = GetRandomRotatedDirection(node.direction, trunkBranchAngle),
                energy = MAX_ENERGY,
                branchLevel = 0,
                thickness = maxThickness > node.thickness ? node.thickness : maxThickness,
                length = segmentLength,
            });
            Debug.Log($"Creating new branch node {nodes.Last().position} with thickness {nodes.Last().thickness}");
        }

        for (int i = 0; i < iterations; i++)
        {
            print($"Iteration <color=red>{i}</color>");

            for(int b = 0; b < branchesPerTrunkNode; b++)
            {
                foreach(SCNode branchStartNode in branchStartingNodes)
                {
                    GrowBranches();
                }

            }
        }
    }

    void GrowBranches()
    {
        FindNearestNodes();

        foreach (SCNode node in nodes)
        {
            ManageNewBranch(node);
        }

        print($"-----New nodes:");
        foreach (var n in newNodes)
            print($"   <color=yellow>{n.position}</color>, energy = <b>{n.energy}</b>, branchLevel: {n.branchLevel}");

        nodes = new HashSet<SCNode>(newNodes);
        newNodes.Clear();

        RemoveReachedAttractors();
    }

    /*public void Colonize(Vector3Int startingPoint)
    {
        trunk.GenerateTrunk(startingPoint);

        nodes.Add(new SCNode()
        {
            position = new Vector3Int(0, 0, 0),
            startsBranch = false,
            direction = new Vector3(0, 1, 0),
            energy = Constants.MAX_ENERGY,
            branchLevel = 0,
            thickness = maxThickness,
        });

        for (int i = 0; i < iterations; i++)
        {
            print($"Iteration <color=red>{i}</color>");

            FindNearestNodes();

            foreach (SCNode node in nodes)
            {
                ManageNewBranch(node);
            }

            print($"-----New nodes:");
            foreach (var n in newNodes)
                print($"   <color=yellow>{n.position}</color>, energy = <b>{n.energy}</b>, branchLevel: {n.branchLevel}");

            nodes = new HashSet<SCNode>(newNodes);
            newNodes.Clear();

            RemoveReachedAttractors();
        }
    }*/

    void FindNearestNodes()
    {
        nodesWithAttractors.Clear();
        // TODO: attractors here HAS TO BE CHANGED TO SPATIAL HASH GRID!
        foreach (var attractor in attractors)
        {
            (SCNode node, float distance) closestNode = (new SCNode(), INFINITE_DISTANCE);
            foreach(var node in nodes)
            {
                float distance = Vector3Int.Distance(node.position, attractor);

                // If node is within detection distance
                if (distance < maxDistance && distance > minDistance)
                {
                    if (distance < closestNode.distance)
                    {
                        closestNode.distance = distance;
                        closestNode.node = node;
                    }
                }
            }
            // If the attractor has found a close node, assign it 
            if(closestNode.distance < INFINITE_DISTANCE)
            {
                Vector3Int nodePos = closestNode.node.position;
                if (nodesWithAttractors.ContainsKey(nodePos))
                {
                    nodesWithAttractors[closestNode.node.position].Add(attractor);
                }
                else
                {
                    nodesWithAttractors.Add(nodePos, new List<Vector3Int>() { attractor });
                }
            }
        }

    }



    void ManageNewBranch(SCNode node)
    {
        if (node.branchLevel >= maxBranchLevel)
        {
            print($"<color=red>XX</color> Max branch level reached for node <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }

        // If there are any attractors nearby
        if (nodesWithAttractors.ContainsKey(node.position))
        {
            string str = "<color=lime>";
            foreach (var a in nodesWithAttractors[node.position]) str += a.ToString() + "  ";
            print($"Found close attractors for node <color=yellow>{node.position}</color> : {str}</color>");

            Vector3 directionVec;

            // Get the average direction from all associated attractors
            directionVec = GetAveragedNormalizedDirectionVector(node.position, nodesWithAttractors[node.position]);
            // Clamp the angle of the direction if it exceeds max angle given by user
            directionVec = ClampDirectionAngle(directionVec, node);

            // If attractors cause the node to grow the same branch
            if (nodes.Any(n => n.position ==
                node.position + Vector3Int.RoundToInt(directionVec * node.length)))
            {
                directionVec = HandleRegrowth(node, directionVec);
            }

            // If none of the branch growth tries was successful, skip this node (ignore attractors)
            if (directionVec.Equals(Vector3.zero)) return;

            // Make the branch growth biased
            Vector3 biasedDirectionVec = Vector3.Slerp(directionVec, node.direction, biasStrength);

            Vector3Int endpointOffset = Vector3Int.RoundToInt(directionVec * node.length);
            (SCNode startNode, SCNode endNode) branchNodes =
                GenerateStartEndNodes(node, endpointOffset, directionVec, false);

            CreateSegment(branchNodes.startNode, branchNodes.endNode);
        }
        // If no attractors nearby
        else
        {
            print("No close attractors found for node " + node.position);

            // If the node is at the end of branches, grow it further towards some direction
            if (node.startsBranch == false)
            {
                print("Node ends branch, generating further branch...");
                // If the node has been searching for attractors with no luck, stop growing this branch
                if (node.IsDead) 
                { 
                    print($"Stopped branch growth at <color=yellow>{node.position}</color>"); 
                    return;
                }

                Vector3 randomDirection = node.direction + new Vector3(
                    Random.Range(0, randomizeBranchDirection),
                    Random.Range(0, randomizeBranchDirection),
                    Random.Range(0, randomizeBranchDirection)
                );

                Vector3Int endpointOffset = Vector3Int.RoundToInt(randomDirection * node.length);
                (SCNode startNode, SCNode endNode) branchNodes =
                    GenerateStartEndNodes(node, endpointOffset, randomDirection, true);

                // Create a new segment
                CreateSegment(branchNodes.startNode, branchNodes.endNode);
            }
            else
            {
                print($"--> Adding cloned node <color=cyan>{node.position}</color>");
                newNodes.Add(node.Clone());
            }
        }
    }

    Vector3 HandleRegrowth(SCNode node, Vector3 directionVec)
    {
        // Try each close attractor for collision with already grown branches
        foreach(var attractor in nodesWithAttractors[node.position])
        {
            print($"Trying growth towards attractor <color=lime>{attractor}</color>");
            // If the node position is already taken (the branch has already grown that way)
            if (nodes.Any(n => n.position ==
                node.position + Vector3Int.RoundToInt(directionVec * node.length)))
            {
                print($"<color=red>Node {node.position} regrowth!</color>");

                // Try growing towards next attractor from the assigned list
                directionVec = Vector3.Normalize(attractor - node.position);
                directionVec = ClampDirectionAngle(directionVec, node);
                print($"New direction vector: <color=cyan>{directionVec}</color>");
            }
            else
            {
                print("<color=lime>Growth successful</color>");
                return directionVec;
            }
        }
        print($"<color=red>Node growth unsuccessful, ignoring attractors</color>");
        newNodes.Add(node.Clone());
        return Vector3.zero;
    }

    // Creates start and end node objects
    (SCNode startNode, SCNode endNode) GenerateStartEndNodes(SCNode node, Vector3Int endpointOffset, 
        Vector3 endpointDirection, bool inheritsEnergy)
    {
        SCNode startNode = new SCNode()
        {
            position = node.position,
            direction = node.direction,
            energy = inheritsEnergy ? node.energy : MAX_ENERGY,
            branchLevel = node.branchLevel,
            startsBranch = true,
            thickness = node.thickness,
            length = node.length,
        };

        SCNode endNode = new SCNode()
        {
            position = node.position + endpointOffset,
            direction = endpointDirection,
            energy = inheritsEnergy ? node.energy - 1 : MAX_ENERGY,
            branchLevel = node.branchLevel + 1,
            startsBranch = false,
            thickness = node.thickness > 1 ? node.thickness - 1 : 1,
            length = inheritsEnergy ? node.length - 1 : node.length,
        };

        print($"New start node: {startNode.position} -- startsBranch = <color=lime>{startNode.startsBranch}</color>");
        print($"New end node: {endNode.position} -- startsBranch = <color=lime>{endNode.startsBranch}</color>");

        return (startNode, endNode);
    }


    Vector3 ClampDirectionAngle(Vector3 directionVec, SCNode node)
    {
        // Check if angle change for the branch (angle between vectors) is more than max
        float angle = Mathf.Abs(Vector3.SignedAngle(directionVec, node.direction, Vector3.forward));
        float maxAngle = node.startsBranch ? maxDebranchRotationAngle : maxBranchRotationAngle;
        float t = 1 - maxAngle / angle;
        if (angle > maxBranchRotationAngle)
        {
            /*Debug.LogWarning($"Angle <color=cyan>{angle} --> " +
                $"{Vector3.SignedAngle(Vector3.Slerp(directionVec, node.direction, t), node.direction, Vector3.forward)}" +
                $"</color>");*/
            return Vector3.Slerp(directionVec, node.direction, t);
        }
        return directionVec;
    }


    void CreateSegment(SCNode startNode, SCNode endNode)
    {
        GenerateVoxels(GenerateThickLine(startNode.position, endNode.position, startNode.thickness));
        newNodes.Add(startNode);
        newNodes.Add(endNode);
    }

    

    void RemoveReachedAttractors()
    {
        List<Vector3Int> newAttractors = new List<Vector3Int>(attractors);

        foreach (var attractor in attractors)
        {
            foreach (var node in nodes)
            {
                float distance = Vector3Int.Distance(node.position, attractor);
                //if (distance < 5)
                //   print($"Distance <color=yellow>{node.position}</color> --> <color=lime>{attractor}</color> = {distance}");

                if (distance < attractorKillRadius)
                {
                    newAttractors.Remove(attractor);
                    //print("<color=red>Removed attractor at " + attractor + "</color>");
                    if (showAttractors)
                        WorldManager.Instance.container[attractor] = new Voxel()
                        {
                            //id = 1
                            id = 2
                        };
                }
            }
        }
        attractors = new HashSet<Vector3Int>(newAttractors);
    }

    public void GenerateAttractors()
    {
        Vector3Int meshBounds;
        AttractorSpawnArea spawnArea;
        attractorSpawnArea.GetComponent<MeshRenderer>().enabled = false;
        MeshCollider meshCollider = attractorSpawnArea.GetComponent<MeshCollider>();
        if(meshCollider != null)
        {
            attractorSpawnArea.transform.localScale = Vector3.Scale(attractorSpawnArea.transform.localScale, spawnAreaScale);
            attractorSpawnArea.transform.position += spawnAreaOffset;
            Vector3Int calculatedOffset = new Vector3Int(
                Mathf.RoundToInt(attractorSpawnArea.transform.position.x),
                Mathf.RoundToInt(meshCollider.bounds.center.y - meshCollider.bounds.extents.y),   // center contains local position coords
                Mathf.RoundToInt(attractorSpawnArea.transform.position.z));
            meshBounds = Vector3Int.RoundToInt(meshCollider.bounds.extents);
            spawnArea = new AttractorSpawnArea(meshBounds, Vector3Int.RoundToInt(calculatedOffset));
        }
        else
        {
            Debug.LogWarning("No mesh collider attached to the spawn area!");
            meshBounds = new Vector3Int(100, 50, 100);
            spawnArea = new AttractorSpawnArea(meshBounds, Vector3Int.zero);
        }


        for (int i = 0; i < attractorsAmount; i++)
        {
            Vector3Int randPos = new Vector3Int(
                Random.Range(spawnArea.xBounds.from, spawnArea.xBounds.to),
                Random.Range(spawnArea.yBounds.from, spawnArea.yBounds.to),
                Random.Range(spawnArea.zBounds.from, spawnArea.zBounds.to)
                );
            /*Vector3Int randPos = new Vector3Int(
                Random.Range(-meshBounds.x, meshBounds.x),
                Random.Range(0, meshBounds.y*2),
                Random.Range(-meshBounds.z, meshBounds.z)
                );
            randPos += spawnAreaOffset;*/
            if (IsPointInCollider(meshCollider, randPos))
                attractors.Add(randPos);

        }
        attractorSpawnArea.GetComponent<MeshCollider>().enabled = false;
    }

    public void ShowAttractors()
    {
        if (showAttractors)
            foreach (var attractor in attractors)
            {
                WorldManager.Instance.container[attractor] = new Voxel()
                {
                    id = 6
                };
            }
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

    void print(string str)
    {
        if (enableDebug)
            Debug.Log(str);
    }

}
