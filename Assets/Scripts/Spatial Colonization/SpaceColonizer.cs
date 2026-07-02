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
using System.Text;

[RequireComponent(typeof(Trunk))]
public class SpaceColonizer : MonoBehaviour
{
    public bool enableDebug = false;

    [Header("General")]
    public int iterations = 5;
    [Range(1, 50)]
    public int maxThickness = 5;
    [Range(0,1)]
    public float biasStrength = 0;

    [Header("Trunk")]
    [Range(1, 6)]
    public int branchesPerTrunkNode = 1;
    public int trunkBranchAngle = 90;

    [Header("Nodes Settings")]
    [Range(2, 100)]
    public int segmentLength = 5;
    [Range(5, 180)]
    public int maxBranchRotationAngle = 45;
    [Range(5, 180)]
    public int maxDebranchRotationAngle = 90;
    [Range(1, 100)]
    public int maxBranchLevel = 10;
    [Highlight(0.7f, 0.7f, 1f)]
    public bool seekingBranchesEnabled = false;
    [Highlight(0.7f, 0.7f, 1f)]
    [Range(0, 1)]
    public float randomizeBranchDirection = 0;

    
    HashSet<SCNode> nodes = new HashSet<SCNode>();
    //HashSet<Vector3Int> attractors = new HashSet<Vector3Int>();
    SpatialHashGrid<SCNode> nodesGrid;
    HashSet<SCNode> newNodes = new HashSet<SCNode>();
    Dictionary<Vector3Int, List<Vector3Int>> nodesWithAttractors = new Dictionary<Vector3Int, List<Vector3Int>>();

    Trunk trunk;
    AttractorManager attractorManager;

    //byte attractorVoxelID = 6;
    byte killedAttractorVoxelID = 2;
    byte branchVoxelID = 3;


    public void Colonize(Vector3Int startingPoint)
    {
        trunk = GetComponent<Trunk>();
        if(!TryGetComponent(out attractorManager))
        {
            Debug.LogWarning("No attractor manager component found!");
            return;
        };

        attractorManager.GenerateAttractors();
        attractorManager.ShowAttractors();



        List<SCNode> branchStartingNodes = trunk.GenerateTrunk(startingPoint);

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
            //Debug.Log($"Creating new branch node {nodes.Last().position} with thickness {nodes.Last().thickness}");

            // Populating the hash grid
            RepopulateNodesGrid();
        }

        for (int i = 0; i < iterations; i++)
        {
            print($"Iteration <color=red>{i}</color>");
            FindNearestNodes();

            for (int b = 0; b < branchesPerTrunkNode; b++)
            {
                foreach(SCNode branchStartNode in branchStartingNodes)
                {
                    GrowBranches();
                }

            }
        }
    }

    public string GetDataString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine($"--General--");
        result.AppendLine($"iterations: {iterations}");
        result.AppendLine($"maxThickness: {maxThickness}");
        result.AppendLine($"biasStrength: {biasStrength}");
        result.AppendLine();
        result.AppendLine($"--Trunk--");
        result.AppendLine($"branchesPerTrunkNode: {branchesPerTrunkNode}");
        result.AppendLine($"trunkBranchAngle: {trunkBranchAngle}");
        result.AppendLine();
        result.AppendLine($"--Attractors--");
        result.AppendLine($"attractorsAmount: {attractorManager.attractorsAmount}");
        result.AppendLine($"maxDistance: {attractorManager.maxDistance}");
        result.AppendLine($"minDistance: {attractorManager.minDistance}");
        result.AppendLine($"attractorKillRadius: {attractorManager.attractorKillRadius}");
        result.AppendLine();
        result.AppendLine($"--Nodes--");
        result.AppendLine($"segmentLength: {segmentLength}");
        result.AppendLine($"maxBranchRotationAngle: {maxBranchRotationAngle}");
        result.AppendLine($"maxDebranchRotationAngle: {maxDebranchRotationAngle}");
        result.AppendLine($"maxBranchLevel: {maxBranchLevel}");
        result.AppendLine($"randomizeBranchDirection: {randomizeBranchDirection}");
        return result.ToString();

    }

    void GrowBranches()
    {
        //FindNearestNodes();

        foreach (SCNode node in nodes)
        {
            ManageNewBranch(node);
        }

        print($"-----New nodes:");
        foreach (var n in newNodes)
            print($"   <color=yellow>{n.position}</color>, energy = <b>{n.energy}</b>, branchLevel: {n.branchLevel}");

        nodes = new HashSet<SCNode>(newNodes);
        newNodes.Clear();

        attractorManager.RemoveReachedAttractors(nodes);
        RepopulateNodesGrid();
    }

    void RepopulateNodesGrid()
    {
        nodesGrid = new SpatialHashGrid<SCNode>(attractorManager.maxDistance);
        foreach (var node in nodes)
        {
            nodesGrid.Add(node.position, node);
        }
    }


    void FindNearestNodes()
    {
        nodesWithAttractors.Clear();
        // TODO: attractors here HAS TO BE CHANGED TO SPATIAL HASH GRID!

        /*foreach(var attractor in attractors)
        {
            List<SCNode> nearbyNodes = nodesGrid.GetNearby(attractor);

        }*/

        foreach (var attractor in attractorManager.attractors)
        {
            List<SCNode> nearbyNodes = nodesGrid.GetNearby(attractor);
            if (nearbyNodes.Count == 0) continue;

            (SCNode node, float distance) closestNode = (new SCNode(), INFINITE_DISTANCE);

            foreach(var node in nearbyNodes)
            {
                float distance = Vector3Int.Distance(node.position, attractor);

                // If node is within detection distance
                if (distance < attractorManager.maxDistance && distance > attractorManager.minDistance)
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
        /*if(node.length < 3)
        {
            print($"<color=red>XX</color> Node reached minimal length <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }*/

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

            Vector3Int endpointOffset = Vector3Int.RoundToInt(biasedDirectionVec * node.length);

            CreateSegment(node, endpointOffset, biasedDirectionVec, false);
            /*(SCNode startNode, SCNode endNode) branchNodes =
                GenerateStartEndNodes(node, endpointOffset, biasedDirectionVec, false);

            CreateSegment(branchNodes.startNode, branchNodes.endNode);*/
        }
        // If no attractors nearby
        else
        {
            print("No close attractors found for node " + node.position);
            if (!seekingBranchesEnabled)
            {
                newNodes.Add(node.Clone());
                return;
            }

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

                Vector3 randomOffset = Random.insideUnitSphere * randomizeBranchDirection;
                Vector3 randomDirection = (node.direction + randomOffset).normalized;

                Vector3Int endpointOffset = Vector3Int.RoundToInt(randomDirection * node.length);
                // Create a new segment
                CreateSegment(node, endpointOffset, randomDirection, true);

                /*(SCNode startNode, SCNode endNode) branchNodes =
                    GenerateStartEndNodes(node, endpointOffset, randomDirection, true);

                // Create a new segment
                CreateSegment(branchNodes.startNode, branchNodes.endNode);*/
            }
            else
            {
                print($"--> Adding cloned node <color=cyan>{node.position}</color>");
                newNodes.Add(node.Clone());
            }
        }
    }

    // In case of growing multiple times in the same direction, manipulate the direction vector
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
    /*(SCNode startNode, SCNode endNode) GenerateStartEndNodes(SCNode node, Vector3Int endpointOffset, 
        Vector3 endpointDirection, bool decreaseEnergy)
    {
        int branchLevel = node.startsBranch ? node.branchLevel + 1 : node.branchLevel;
        int length = node.length; //-  branchLevel/maxBranchLevel;
        SCNode startNode = new SCNode()
        {
            position = node.position,
            direction = node.direction,
            energy = decreaseEnergy ? node.energy : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = true,
            thickness = node.thickness,
            length = length,
        };

        int thickness;
        if (node.thickness > maxThickness) thickness = maxThickness;
        else thickness = node.startsBranch ? (node.thickness > 1 ? node.thickness - 1 : 1) : node.thickness;

        SCNode endNode = new SCNode()
        {
            position = node.position + endpointOffset,
            direction = endpointDirection,
            energy = decreaseEnergy ? node.energy - 1 : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = false,
            thickness = thickness,
            length = length,
            //length = node.startsBranch && length > 3 ? length - 1 : length,
        };

        print($"New start node: {startNode.position} -- startsBranch = <color=lime>{startNode.startsBranch}</color>");
        print($"New end node: {endNode.position} -- startsBranch = <color=lime>{endNode.startsBranch}</color>");

        return (startNode, endNode);
    }*/


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


    void CreateSegment(SCNode node, Vector3Int endpointOffset,
        Vector3 endpointDirection, bool decreaseEnergy)
    {
        int branchLevel = node.startsBranch ? node.branchLevel + 1 : node.branchLevel;
        int length = node.length; //-  branchLevel/maxBranchLevel;
        SCNode startNode = new SCNode()
        {
            position = node.position,
            direction = node.direction,
            energy = decreaseEnergy ? node.energy : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = true,
            thickness = node.thickness,
            length = length,
        };

        int thickness;
        if (node.thickness > maxThickness) thickness = maxThickness;
        else thickness = node.startsBranch ? (node.thickness > 1 ? node.thickness - 1 : 1) : node.thickness;

        SCNode endNode = new SCNode()
        {
            position = node.position + endpointOffset,
            direction = endpointDirection,
            energy = decreaseEnergy ? node.energy - 1 : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = false,
            thickness = thickness,
            length = length,
            //length = node.startsBranch && length > 3 ? length - 1 : length,
        };

        print($"New start node: {startNode.position} -- startsBranch = <color=lime>{startNode.startsBranch}</color>");
        print($"New end node: {endNode.position} -- startsBranch = <color=lime>{endNode.startsBranch}</color>");


        GenerateVoxels(Utilities.GenerateThickLine(startNode.position, endNode.position, startNode.thickness));
        newNodes.Add(startNode);
        newNodes.Add(endNode);
    }

    

    void GenerateVoxels(List<Vector3Int> positions)
    {
        foreach (var pos in positions)
        {
            // Don't overwrite killed attractors
            if (WorldManager.Instance.container[pos].id == killedAttractorVoxelID) continue;
            WorldManager.Instance.container[pos] = new Voxel()
            {
                id = branchVoxelID
            };
        }
    }

    void print(string str)
    {
        if (enableDebug)
            Debug.Log(str);
    }

    /*public List<Vector3Int> GenerateThickLine(Segment segment)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(segment.startPos, segment.endPos);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = segment.thickness;
        int radiusSquared = radius * radius;

        // The branch must clear its own thickness before it cares about collisions
        int graceDistanceSquared = (radius * 3) * (radius * 3);

        // Make grace zone based on the size of the parent branch thickness
        //int graceDistanceSquared = segment.parentThickness * segment.parentThickness * 2;
        //print($"graceDistanceSquared = {graceDistanceSquared}");
        //print($"<color=lime>New line {A} --> {B}</color>");

        // Applying a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
            bool collisionDetected = false;
            List<Vector3Int> currentSpherePoints = new List<Vector3Int>();
            // Calculate distance from start point A to handle the grace zone
            bool insideGraceZone = (point - segment.startPos).sqrMagnitude <= graceDistanceSquared;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        // Check if this local offset is within the sphere's radius
                        // (doing x*x + y*y + z*z is much faster than Vector3.Distance)
                        if (x * x + y * y + z * z <= radiusSquared)
                        {
                            //thickLine.Add(new Vector3Int(point.x + x, point.y + y, point.z + z));

                            Vector3Int voxelPos = new Vector3Int(point.x + x, point.y + y, point.z + z);

                            // If it's occupied and we are out of the grace zone it is a collision
                            if (!insideGraceZone && WorldManager.Instance.container[voxelPos].id != 0)
                            {
                                // If smaller branch collisions can be ignored
                                if (allowedBranchCollisionLevel > 0)
                                {
                                    // If it is the smaller branches that collide with each other, ignore collision
                                    if (WorldManager.Instance.container[voxelPos].id - 1 <= allowedBranchCollisionLevel
                                        && segment.thickness <= allowedBranchCollisionLevel)
                                    {
                                        currentSpherePoints.Add(voxelPos);
                                        continue;
                                    }
                                }

                                // Ignore collisions with the parent branch
                                if (segment.parentBranchId != WorldManager.Instance.container[voxelPos].branchId)
                                {
                                    // Make the small branches move away, big branches will ignore collisions with smaller
                                    if (segment.thickness <= WorldManager.Instance.container[voxelPos].id - 1)
                                    {
                                        collisionDetected = true;
                                        branchCollision.collisionsCount++;
                                        branchCollision.didCollide = true;
                                        break;
                                    }
                                }
                            }

                            currentSpherePoints.Add(voxelPos);
                        }
                    }
                    if (collisionDetected) break;
                }
                if (collisionDetected) break;
            }

            // If hit something outside the grace zone, stop growing the branch right here
            if (collisionDetected)
            {
                break;
            }

            // Otherwise, commit these points to the branch
            foreach (var pos in currentSpherePoints)
            {
                thickLine.Add(pos);
            }
        }

        return thickLine.ToList();
    }*/

}
