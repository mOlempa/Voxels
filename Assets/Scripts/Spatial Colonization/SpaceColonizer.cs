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

public class SpaceColonizer : MonoBehaviour
{
    [Header("General")]
    public int iterations = 1;
    public bool showAttractors = true;

    [Header("Attractors Settings")]
    [Range(1, 100)]
    public int maxDistance = 10;
    [Range(1, 100)]
    public int minDistance = 2;
    [Range(1, 100)]
    public int attractorKillRadius = 2;

    [Header("Nodes Settings")]
    [Range(2, 100)]
    public int branchLength = 5;
    [Range(5, 180)]
    public int maxBranchRotationAngle = 45;
    [Range(1, 100)]
    public int maxBranchLevel = 10;
    [Range(0, 1)]
    public float randomizeBranchDirection = 0;

    
    HashSet<SCNode> nodes = new HashSet<SCNode>() {
        new SCNode() {
            position = new Vector3Int(0,0,0),
            startsBranch = false,
            direction = new Vector3(0, 1, 0),
            energy = Constants.MAX_ENERGY,
            branchLevel = 0
        }};
    List<Vector3Int> attractors = new List<Vector3Int>();
    HashSet<SCNode> newNodes = new HashSet<SCNode>();
    Dictionary<Vector3Int, List<Vector3Int>> nodesWithAttractors = new Dictionary<Vector3Int, List<Vector3Int>>();
  

    public void Colonize(Vector3Int startingPoint)
    {
        for (int i = 0; i < iterations; i++)
        {
            print($"Iteration <color=red>{i}</color>");

            FindNearestNodes();
            //GrowBranches();

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
    }

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
        /*foreach(var node in nodesWithAttractors)
        {
            print($"Node: <color=lime>{node.Key}</color>");
            foreach(var attractor in node.Value)
            {
                print($"   ---> <color=yellow>{attractor}</color>");
            }
        }*/

    }


    /*void GrowBranches()
    {
        foreach (SCNode node in nodes)
        {
            ManageNewBranch(node);

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

                // TODO: when the attractors list for the node changes, the averaged direction shifts slightly,
                // so it isn't detected as the same branch

                // If attractors cause the node to grow the same branch
                if (nodes.Any(n => n.position ==
                    node.position + Vector3Int.RoundToInt(directionVec * branchLength)))
                {
                    // If it is just one attractor causes the node to grow the same branch, ignore attractor completely
                    if (nodesWithAttractors[node.position].Count == 1)
                    {
                        newNodes.Add(node.Clone());
                        continue;
                    }

                    Debug.LogWarning($"<color=red>Node {node.position} regrowth!</color>");

                    // If it is multiple attractors, decide on one to grow towards
                    directionVec = Vector3.Normalize(nodesWithAttractors[node.position].First() - node.position);
                    directionVec = ClampDirectionAngle(directionVec, node);
                    print($"New direction vector: <color=cyan>{directionVec}</color>");

                    // TODO: length is weird here (branches are too long), fix it!!!!!!
                }

                Vector3Int endpointOffset = Vector3Int.RoundToInt(directionVec * branchLength);
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
                    if (node.IsDead) { print($"Stopped branch growth at <color=yellow>{node.position}</color>"); continue; }

                    // The node doesn't get detected as final node (startsBranch is false) and the random
                    // values are added to a new third branch going in/out of the node
                    // TODO: Why?????
                    Vector3 randomDirection = node.direction + new Vector3(
                        Random.Range(0, randomizeBranchDirection),
                        Random.Range(0, randomizeBranchDirection),
                        Random.Range(0, randomizeBranchDirection)
                    );

                    Vector3Int endpointOffset = Vector3Int.RoundToInt(randomDirection * branchLength);
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
    }*/


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

            // TODO: when the attractors list for the node changes, the averaged direction shifts slightly,
            // so it isn't detected as the same branch

            // If attractors cause the node to grow the same branch
            if (nodes.Any(n => n.position ==
                node.position + Vector3Int.RoundToInt(directionVec * branchLength)))
            {
                // If it is just one attractor causes the node to grow the same branch, ignore attractor completely
                if (nodesWithAttractors[node.position].Count == 1)
                {
                    newNodes.Add(node.Clone());
                    return;
                }

                Debug.LogWarning($"<color=red>Node {node.position} regrowth!</color>");

                // If it is multiple attractors, decide on one to grow towards
                directionVec = Vector3.Normalize(nodesWithAttractors[node.position].First() - node.position);
                directionVec = ClampDirectionAngle(directionVec, node);
                print($"New direction vector: <color=cyan>{directionVec}</color>");

                // TODO: length is weird here (branches are too long), fix it!!!!!!
            }

            /*SCNode startNode = new SCNode()
            {
                position = node.position,
                direction = node.direction,
                energy = Constants.MAX_ENERGY,
                branchLevel = node.branchLevel,
                startsBranch = true,
            };
            SCNode endNode = new SCNode()
            {
                position = node.position + Vector3Int.RoundToInt(directionVec * branchLength),
                direction = directionVec,
                energy = Constants.MAX_ENERGY,
                branchLevel = node.branchLevel + 1,
                startsBranch = false,
            };*/

            Vector3Int endpointOffset = Vector3Int.RoundToInt(directionVec * branchLength);
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
                if (node.IsDead) { print($"Stopped branch growth at <color=yellow>{node.position}</color>"); return; }

                // The node doesn't get detected as final node (startsBranch is false) and the random
                // values are added to a new third branch going in/out of the node
                // TODO: Why?????
                Vector3 randomDirection = node.direction + new Vector3(
                    Random.Range(0, randomizeBranchDirection),
                    Random.Range(0, randomizeBranchDirection),
                    Random.Range(0, randomizeBranchDirection)
                );

                /*SCNode startNode = new SCNode()
                {
                    position = node.position,
                    direction = node.direction,
                    energy = node.energy,
                    branchLevel = node.branchLevel,
                    startsBranch = true,
                };

                SCNode endNode = new SCNode()
                {
                    position = node.position + Vector3Int.RoundToInt(randomDirection * branchLength),
                    direction = randomDirection,
                    energy = node.energy - 1,
                    branchLevel = node.branchLevel + 1,
                    startsBranch = false,
                };*/

                Vector3Int endpointOffset = Vector3Int.RoundToInt(randomDirection * branchLength);
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
        };

        SCNode endNode = new SCNode()
        {
            position = node.position + endpointOffset,
            direction = endpointDirection,
            energy = inheritsEnergy ? node.energy - 1 : MAX_ENERGY,
            branchLevel = node.branchLevel + 1,
            startsBranch = false,
        };

        print($"New start node: {startNode.position} -- startsBranch = <color=lime>{startNode.startsBranch}</color>");
        print($"New end node: {endNode.position} -- startsBranch = <color=lime>{endNode.startsBranch}</color>");

        return (startNode, endNode);
    }


    Vector3 ClampDirectionAngle(Vector3 directionVec, SCNode node)
    {
        // Check if angle change for the branch (angle between vectors) is more than max
        float angle = Mathf.Abs(Vector3.SignedAngle(directionVec, node.direction, Vector3.forward));
        float t = 1 - maxBranchRotationAngle / angle;
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
        GenerateVoxels(GenerateLine(startNode.position, endNode.position));
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
        attractors = new List<Vector3Int>(newAttractors);
    }

    public void GenerateAttractors(int amount, Vector3Int bounds)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3Int randPos = new Vector3Int(
                Random.Range(-bounds.x, bounds.x),
                Random.Range(10, bounds.y),
                Random.Range(-bounds.z, bounds.z)
                );
            // TODO: add positions ONLY IF they are not repeating!
            attractors.Add(randPos);
        }
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

}
