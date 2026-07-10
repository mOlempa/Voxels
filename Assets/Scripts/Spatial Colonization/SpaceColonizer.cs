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
using static LeavesManager;
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
    [Range(0, 1)]
    public float branchDirBiasStrength = 0;
    [Highlight(0.6f, 0.7f, 0.6f)]
    public Vector3 addedBiasDirection = Vector3.up;
    [Highlight(0.6f, 0.7f, 0.6f)]
    [Range(0, 1)]
    public float addedBiasStrength = 0;

    [Header("Trunk")]
    [Range(1, 6)]
    public int branchesPerTrunkNode = 1;
    public int trunkBranchAngle = 90;

    [Header("Nodes Settings")]
    [Range(2, 100)]
    public int segmentLength = 5;
    [Range(0, 180)]
    public int maxBranchRotationAngle = 45;
    [Range(0, 180)]
    public int maxDebranchRotationAngle = 90;
    [Range(1, 100)]
    public int maxBranchLevel = 10;
    [Range(0, 5)]
    public int maxBranchOuts = 2;
    [Highlight(0.7f, 0.7f, 1f)]
    public bool seekingBranchesEnabled = false;
    [Highlight(0.7f, 0.7f, 1f)]
    [Range(0, 1)]
    public float randomizeBranchDirection = 0;

    [Header("Collision Stuff")]
    [Range(0, 10)]
    public int allowedBranchCollisionLevel = 1;
    public int branchTrialTimes = 0;

    [Header("Leaves")]
    [SerializeField] LeafShape leafShape;
    public int recursionLevel = 3;

    private BranchCollisionHelper branchCollision = new BranchCollisionHelper();


    HashSet<SCNode> nodes = new HashSet<SCNode>();
    //HashSet<Vector3Int> attractors = new HashSet<Vector3Int>();
    SpatialHashGrid<SCNode> nodesGrid;
    HashSet<SCNode> newNodes = new HashSet<SCNode>();
    Dictionary<Vector3Int, List<Vector3Int>> nodesWithAttractors = new Dictionary<Vector3Int, List<Vector3Int>>();

    Trunk trunk;
    AttractorManager attractorManager;

    //byte attractorVoxelID = 6;
    byte killedAttractorVoxelID = 3;
    byte branchVoxelID = 2;

    ushort assignableBranchId = 0;
    public byte assignableObjectId = 0;

    [HideInInspector] TimeManager timeManager = new TimeManager();

    public void Colonize(Vector3Int startingPoint)
    {
        trunk = GetComponent<Trunk>();
        if(!TryGetComponent(out attractorManager))
        {
            Debug.LogWarning("No attractor manager component found!");
            return;
        };

        timeManager.StartGenTimer();

        attractorManager.GenerateAttractors();
        attractorManager.ShowAttractors();

        //return;
        assignableObjectId = WorldManager.Instance.assignableObjectIdList.Last();

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
                branchOuts = 0,
                branchId = assignableBranchId,
                parentBranchId = 0,
            });
            //Debug.Log($"Creating new branch node {nodes.Last().position} with thickness {nodes.Last().thickness}");
            assignableBranchId++;
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

        GrowLeaves();

        timeManager.StopGenTimer();

    }

    public string GetDataString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine($"--General--");
        result.AppendLine($"iterations: {iterations}");
        result.AppendLine($"maxThickness: {maxThickness}");
        result.AppendLine($"branchDirBiasStrength: {branchDirBiasStrength}");
        result.AppendLine($"addedBiasDirection: {addedBiasDirection}");
        result.AppendLine($"addedBiasStrength: {addedBiasStrength}");
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
        result.AppendLine($"maxBranchOuts: {maxBranchOuts}");
        result.AppendLine($"seekingBranchesEnabled: {seekingBranchesEnabled}");
        result.AppendLine($"randomizeBranchDirection: {randomizeBranchDirection}");
        result.AppendLine();
        result.AppendLine($"--Collision--");
        result.AppendLine($"allowedBranchCollisionLevel: {allowedBranchCollisionLevel}");
        result.AppendLine($"branchTrialTimes: {branchTrialTimes}");
        result.AppendLine();
        result.AppendLine($"--Leaves--");
        result.AppendLine($"leafShape: {leafShape.name}");
        result.AppendLine();
        result.AppendLine($"--Timers--");
        result.AppendLine($"Generation time: {timeManager.GetGenTime()}");
        result.AppendLine($"Avg collision detection time: {timeManager.GetColTimeAvg()}");

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

    void GrowLeaves()
    {
        if (leafShape == null || leafShape.leafPoints.Length == 0) return;

        List<SCNode> leafNodes = new List<SCNode>();
        //string ids = "";
        foreach(var node in nodes)
        {
            //ids += $"{node.branchId}-{node.parentBranchId} == ";
            if (!node.startsBranch)
            {
                // Add either 1 or 2 leaves
                for(int n = 0; n < Random.Range(1, 2);  n++) leafNodes.Add(node);

                for (int i = 0; i < recursionLevel; i++)
                {
                    // Find parent of the last added parent
                    SCNode parent = nodes.FirstOrDefault(n => n.branchId == leafNodes.Last().parentBranchId);
                    if (parent.branchId != leafNodes.Last().parentBranchId) break;
                    // Add this new parent
                   //leafNodes.Add(parent);
                    for (int n = 0; n < Random.Range(1, 2); n++) leafNodes.Add(parent);

                }
            }
        }
        //Debug.Log(ids);

        foreach(SCNode node in leafNodes)
        {
            Vector3 dir = Vector3.Slerp(node.direction, Random.insideUnitSphere, 0.8f);
            GenerateLeaf(leafShape, node.position, Quaternion.LookRotation(dir, Vector3.up));
        }
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
        // TODO: Make a separate list for nodes that won't grow branches anymore and connect it at the end?
        if (node.branchLevel >= maxBranchLevel)
        {
            print($"<color=red>XX</color> Max branch level reached for node <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }
        if (node.branchOuts >= maxBranchOuts)
        {
            print($"<color=red>XX</color> Max branch outs reached for node <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }
        if (node.IsDead)
        {
            print($"<color=red>XX</color> Branch dead. Stopped branch growth at <color=yellow>{node.position}</color>");
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
            Vector3 biasedDirectionVec = Vector3.Slerp(directionVec, node.direction, branchDirBiasStrength);
            if(addedBiasStrength > 0)
                biasedDirectionVec = Vector3.Slerp(biasedDirectionVec, addedBiasDirection.normalized, addedBiasStrength);


            Vector3Int endpointOffset = Vector3Int.RoundToInt(biasedDirectionVec * node.length);

            CreateSegment(node, endpointOffset, biasedDirectionVec, false);
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

                /*if (addedBiasStrength > 0)
                    randomDirection = Vector3.Slerp(randomDirection, addedBiasDirection.normalized, addedBiasStrength);*/

                Vector3Int endpointOffset = Vector3Int.RoundToInt(randomDirection * node.length);
                // Create a new segment
                CreateSegment(node, endpointOffset, randomDirection, true);

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


    void CreateSegment(SCNode node, Vector3Int endpointOffset, Vector3 endpointDirection, bool decreaseEnergy)
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
            branchOuts = node.branchOuts + 1,
            parentBranchId = node.parentBranchId,
            branchId = node.branchId,
        };

        int thickness;
        // if inherited thickness is larger than the max thickness, clamp it
        if (node.thickness > maxThickness) thickness = maxThickness;
        // if the node starts a branch already decrease the thickness (if larger than 1), otherwise
        // (case: node ends the branch) inherit thickness
        //else thickness = node.startsBranch ? (node.thickness > 1 ? node.thickness - 1 : 1) : node.thickness;
        else 
        {
            if (node.startsBranch)
            {
                thickness = node.thickness > 1 ? node.thickness - 1 : 1;
            }
            else
            {
                /*thickness = (int)(node.branchLevel / maxBranchLevel * maxThickness);
                Debug.Log($"thickness {thickness} = {node.branchLevel} / {maxBranchLevel} * {maxthi}");*/
                if(node.branchLevel == 0) thickness = node.thickness > 1 ? node.thickness - 1 : 1;
                else thickness = node.thickness;
            }
        }

        SCNode endNode = new SCNode()
        {
            position = node.position + endpointOffset,
            direction = endpointDirection,
            energy = decreaseEnergy ? node.energy - 1 : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = false,
            thickness = thickness,
            length = length,
            branchOuts = 0,
            parentBranchId = node.branchId,
            branchId = assignableBranchId,
            //length = node.startsBranch && length > 3 ? length - 1 : length,
        };

        print($"New start node: {startNode.position} -- startsBranch = <color=lime>{startNode.startsBranch}</color>");
        print($"New end node: {endNode.position} -- startsBranch = <color=lime>{endNode.startsBranch}</color>");


        //GenerateVoxels(Utilities.GenerateThickLine(startNode.position, endNode.position, startNode.thickness));
        List<Vector3Int> voxelPositions = GenerateThickLine(startNode, endNode, startNode.thickness);

        //Vector3 savedDir = endNode.direction;

        timeManager.StartColTimer();

        for (int i = 0; i < branchTrialTimes; i++)
        {
            branchCollision.didCollide = false;
            voxelPositions = GenerateThickLine(startNode, endNode, startNode.thickness);
            // If no collision detected, proceed with the branch
            if (!branchCollision.didCollide) break;
            //print("<color=cyan>Reassigning branch angle...</color>");

            Vector3 randomOffset = Random.insideUnitSphere;
            Vector3 randomDirection = (node.direction + randomOffset).normalized;
            Vector3 biasedDirectionVec = Vector3.Slerp(randomDirection, addedBiasDirection, addedBiasStrength);

            Vector3Int offset = Vector3Int.RoundToInt(biasedDirectionVec * node.length);

            /*Vector3 biasedCollisionDir = collisionBranchGrowthBias == GrowthBiasType.Branch ?
                GetLocalEndpoint(randLength, currentNode.eulerAngles) : GetDirection(collisionBranchGrowthBias);

            // Biased towards specific branch direction
            currentNode.position = savedPos + GetLocalEndpoint(randLength,
                GetBiasedLocalRotation(currentNode.eulerAngles, biasedCollisionDir));*/

            endNode.position = node.position + offset;
            endNode.direction = biasedDirectionVec;
        }

        timeManager.StopColTimer();

        if (branchCollision.didCollide)
        {
            //Debug.Log($"Collision at <color=red>{startNode.position}</color>!");
            newNodes.Add(startNode);
            //Debug.Log($"Branch stays at {startNode.position}");
        }
        else
        {
            GenerateVoxels(voxelPositions, startNode.branchId);
            newNodes.Add(startNode);
            newNodes.Add(endNode);
            assignableBranchId++;
            /*Debug.Log($"New branch {startNode.position} - {endNode.position}  -->  " +
                $"<color=lime>id: {startNode.branchId}-{endNode.branchId}</color>");*/
        }


        /*GenerateVoxels(voxelPositions, startNode.branchId);
        newNodes.Add(startNode);
        newNodes.Add(endNode);*/
        //assignableBranchId++;
    }

    

    void GenerateVoxels(List<Vector3Int> positions, ushort branchId)
    {
        foreach (var pos in positions)
        {
            // Don't overwrite killed attractors
            if (WorldManager.Instance.container[pos].id == killedAttractorVoxelID) continue;
            WorldManager.Instance.container[pos] = new Voxel()
            {
                id = branchVoxelID,
                branchId = branchId,
                objectId = assignableObjectId
            };
        }
    }

    void print(string str)
    {
        if (enableDebug)
            Debug.Log(str);
    }

    public List<Vector3Int> GenerateThickLine(SCNode startNode, SCNode endNode, int thickness)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(startNode.position, endNode.position);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = thickness;
        int radiusSquared = radius * radius;

        // Applying a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
            bool collisionDetected = false;
            List<Vector3Int> currentSpherePoints = new List<Vector3Int>();

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
                            if (WorldManager.Instance.container[voxelPos].id != 0)
                            {
                                // If it is a completely different object (other plant or obstacle)
                                if (WorldManager.Instance.container[voxelPos].objectId != assignableObjectId)
                                {
                                    collisionDetected = true;
                                    branchCollision.collisionsCount++;
                                    branchCollision.didCollide = true;
                                    break;
                                }

                                // If smaller branch collisions can be ignored
                                if (allowedBranchCollisionLevel > 0)
                                {
                                    // If it is the smaller branches that collide with each other, ignore collision
                                    if (WorldManager.Instance.container[voxelPos].id - 1 <= allowedBranchCollisionLevel
                                        && thickness <= allowedBranchCollisionLevel)
                                    {
                                        currentSpherePoints.Add(voxelPos);
                                        continue;
                                    }
                                }

                                // Ignore collisions with the parent branch
                                if (startNode.parentBranchId != WorldManager.Instance.container[voxelPos].branchId)
                                {
                                    // If the branches have the same parent and same-parent collision can be ignored
                                    /*if (ignoreSameParentBranchCollision &&
                                        startNode.parentBranchId == allSegments[collidedBranchId].parentBranchId)
                                    {

                                    }
                                    // Make the small branches move away, big branches will ignore collisions with smaller
                                    else */if (thickness <= WorldManager.Instance.container[voxelPos].id - 1)
                                    {
                                        /*Debug.Log($"-> Collision of branch - thickness: {thickness}, " +
                                            $"branchId: {startNode.branchId}, parentBranchId: {startNode.parentBranchId}");*/
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
    }

}
