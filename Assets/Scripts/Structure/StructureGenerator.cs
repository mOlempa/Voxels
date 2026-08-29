using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Linq;
using static UnityEngine.Rendering.HableCurve;
using static Utilities;
using static LeavesManager;
using static GrowthBias;
using System.Text;
using System;


public class StructureGenerator : MonoBehaviour
{
    //[SerializeField] public Grammar grammar;
    [HideInInspector] LSystemGenerator lSystemGenerator;
    [SerializeField] public LeafShape leafShape;
    private Grammar grammar;
    public bool enablePrintDebug = false;

    public int maxLength = 5;
    public int minLength = 3;
    public int maxAngle = 40;
    public int minAngle = 15;
    public int maxThickness = 5;
    public Vector3 startingAngles = new Vector3(0, 0, 0);
    /*[Header("Growth Bias")]
    public GrowthBiasType branchGrowthBias = GrowthBiasType.None;
    [Range(0f, 1f)]
    public float biasStrength = 1;*/

    private BranchCollisionHelper branchCollision = new BranchCollisionHelper();

    [Header("Branch Collision Handling")]
    public bool ignoreSameParentBranchCollision = true;
    public bool useGraceZone = true;
    [Range(0, 10)]
    public int allowedBranchCollisionLevel = 1;
    public GrowthBiasType collisionBranchGrowthBias = GrowthBiasType.None;
    [Range(1, 30)]
    public int branchTrialTimes = 1;
    [Range(0, 30)]
    public int collisionAngleOffset = 10;

    [Range(0f, 1f)]
    public float collisionBiasStrength = 1;

    public ushort assignableBranchId = 0;
    public byte assignableObjectId = 0;
    

    private Dictionary<ushort, Segment> allSegments = new Dictionary<ushort, Segment>();

    [HideInInspector] public TimeManager timeManager = new TimeManager();

    private void Awake()
    {
        lSystemGenerator = GetComponent<LSystemGenerator>();
        if (lSystemGenerator == null)
        {
            Debug.LogWarning("No L-system generator component to get grammar from!");
            gameObject.SetActive(false);
        }
        else
        {
            grammar = lSystemGenerator.grammar;
        }
    }

    public void Generate()
    {
        timeManager.StartAdditionalTimer();
        List<Symbol> sentence = lSystemGenerator.GenerateSentence();
        timeManager.StopAdditionalTimer();

        ConvertSentenceToSegments(sentence);
    }

    public string GetDataString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine($"--Grammar--");
        result.AppendLine($"grammar: {grammar.name}");
        result.AppendLine();
        result.AppendLine($"--Leaf--");
        result.AppendLine($"leafShape: {leafShape.name}");
        result.AppendLine();
        result.AppendLine($"--Settings--");
        result.AppendLine($"iterationLimit: {lSystemGenerator.iterationLimit}");
        result.AppendLine($"maxLength: {maxLength}");
        result.AppendLine($"minLength: {minLength}");
        result.AppendLine($"maxAngle: {maxAngle}");
        result.AppendLine($"minAngle: {minAngle}");
        result.AppendLine($"maxThickness: {maxThickness}");
        result.AppendLine($"startingAngles: {startingAngles}");
        //result.AppendLine($"branchGrowthBias: {branchGrowthBias}");
        //result.AppendLine($"biasStrength: {biasStrength}");
        result.AppendLine();
        result.AppendLine($"--Collision--");
        result.AppendLine($"useGraceZone: {useGraceZone}");
        result.AppendLine($"ignoreSameParentBranchCollision: {ignoreSameParentBranchCollision}");
        result.AppendLine($"allowedBranchCollisionLevel: {allowedBranchCollisionLevel}");
        result.AppendLine($"collisionBranchGrowthBias: {collisionBranchGrowthBias}");
        result.AppendLine($"branchTrialTimes: {branchTrialTimes}");
        result.AppendLine($"collisionAngleOffset: {collisionAngleOffset}");
        result.AppendLine($"collisionBiasStrength: {collisionBiasStrength}");
        result.AppendLine();
        result.AppendLine($"--Statistics--");
        result.AppendLine($"Sentence generation time: {timeManager.GetAdditionalTime()}");
        result.AppendLine($"Plant generation time: {timeManager.GetGenTime()}");
        result.AppendLine($"Avg collision detection time: {timeManager.GetCollisionDetTimeAvg()}");

        return result.ToString();

    }

    public string GetStatistics()
    {
        StringBuilder result = new StringBuilder();
        result.Append($"{timeManager.GetAdditionalTime()}|");
        result.Append($"{timeManager.GetGenTime()}|");
        result.Append($"{timeManager.GetCollisionDetTimeAvg()}|");
        result.Append($"{timeManager.GetColCount()}|");
        result.Append($"{allSegments.Count + 1}");

        return result.ToString();

    }

    //For calculating local rotation for the branch with biased global growth direction
    public Vector3 GetBiasedLocalRotation(Vector3 prevGlobalEuler, Vector3 biasDirection)
    {
        Quaternion prevGlobalRot = Quaternion.Euler(prevGlobalEuler);

        // ONLY USED WHEN COLLISION DETECTED
        float randomXAngle = UnityEngine.Random.Range(-collisionAngleOffset, collisionAngleOffset);
        float randomYAngle = UnityEngine.Random.Range(-collisionAngleOffset, collisionAngleOffset);
        Quaternion randomLocalRot = Quaternion.Euler(randomXAngle, randomYAngle, 0f);

        Quaternion unbiasedGlobalRot;
        if (branchCollision.didCollide) unbiasedGlobalRot = prevGlobalRot * randomLocalRot;
        else unbiasedGlobalRot = prevGlobalRot;
        //Debug.Log($"<color=yellow>what would be without bias: {unbiasedGlobalRot}</color>");

        Vector3 unbiasedGlobalDir = unbiasedGlobalRot * Vector3.forward;
        //Debug.Log($"<color=cyan>global forward vector: {unbiasedGlobalDir}</color>");

        Vector3 biasedGlobalDir = Vector3.Slerp(unbiasedGlobalDir, biasDirection.normalized, collisionBiasStrength);
        //Debug.Log($"<color=cyan> interpolated vector towards bias: {biasedGlobalDir}</color>");

        // Using the previous rotation up vector to prevent the branch from unnaturally twisting along its own axis
        Vector3 prevUp = prevGlobalRot * Vector3.up;
        Quaternion newGlobalRot = Quaternion.LookRotation(biasedGlobalDir, prevUp);
        //Debug.Log($"<color=lime>new global rotation: {newGlobalRot}</color>");
        return newGlobalRot.eulerAngles;

    }

    List<Segment> ConvertSentenceToSegments(List<Symbol> sentence)
    {
        if (grammar == null)
        {
            if(lSystemGenerator != null) grammar = lSystemGenerator.grammar;
            if(grammar == null)
            {
                Debug.LogWarning("No grammar referenced in Structure Generator!!");
                return new List<Segment>();
            }
        }
        List<Segment> segments = new List<Segment>();
        assignableObjectId = WorldManager.Instance.assignableObjectIdList.Last();

        timeManager.StartGenTimer();

        //printDebug($"<color=#{WorldManager.Instance.worldColors[0].color.ToHexString().TrimEnd("00")}>{WorldManager.Instance.worldColors[0].color.ToHexString()}</color>");

        Stack<LNode> stack = new Stack<LNode>();
        stack.Push(new LNode() { 
            position = new Vector3Int(0, 0, 0),
            //anglesDeg = startingAngles, 
            localRotation = Quaternion.identity,
            rotation = Quaternion.Euler(startingAngles),
            thickness = maxThickness, 
            branchLevel = 0, 
            prevNodeThickness = maxThickness,
            branchId = assignableBranchId,
        });
        //final += $"<color=#{WorldManager.Instance.worldColors[maxThickness].color.ToHexString().TrimEnd("00")}>";

        foreach (var symbol in sentence)
        {
            LNode currentNode;
            int randAngle = UnityEngine.Random.Range(minAngle, maxAngle);
            int randLength;

            switch (grammar.GetSymbolAction(symbol))
            {
                case Action.PlaceLine:
                    printDebug($"Symbol {symbol.name}, placing line");

                    currentNode = stack.Pop();
                    randLength = UnityEngine.Random.Range(minLength, maxLength);

                    if (branchCollision.cutChildBranches)
                    {
                        if (currentNode.branchLevel <= branchCollision.cutLevel)
                        {
                            branchCollision.cutChildBranches = false;
                            InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?
                            GenerateSegment(ref currentNode, randLength);

                        }
                    }
                    else 
                    {
                        // If the symbol is parameterized, assign parameters to appropriate variables
                        InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?
                        GenerateSegment(ref currentNode, randLength);

                    }

                    currentNode.branchLevel++;
                    stack.Push(currentNode);
                    break;

                case Action.RotateRight:
                    printDebug($"Symbol {symbol.name}, rotating right");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    //currentNode.anglesDeg.x += randAngle;
                    currentNode.Rotate(randAngle, 0, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();
                    stack.Push(currentNode);
                    break;

                case Action.RotateLeft:
                    printDebug($"Symbol {symbol.name}, rotating left");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();

                    //currentNode.anglesDeg.x -= randAngle;
                    //currentNode.Rotate(-randAngle, 0, 0, Space.World);
                    currentNode.Rotate(-randAngle, 0, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();

                    stack.Push(currentNode);
                    break;

                case Action.RotateForward:
                    printDebug($"Symbol {symbol.name}, rotating forward");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();

                    //currentNode.anglesDeg.z += randAngle;
                    currentNode.Rotate(0, randAngle, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();

                    stack.Push(currentNode);
                    break;

                case Action.RotateBackward:
                    printDebug($"Symbol {symbol.name}, rotating backward");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    //currentNode.anglesDeg.z -= randAngle;
                    currentNode.Rotate(0, -randAngle, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();

                    stack.Push(currentNode);
                    break;

                case Action.RotateAxis:
                    printDebug($"Symbol {symbol.name}, rotating axis");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    //currentNode.anglesDeg.y += randAngle;
                    currentNode.Rotate(0, 0, randAngle, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();
                    stack.Push(currentNode);
                    break;

                case Action.StartBranch:
                    printDebug($"Symbol {symbol.name}, starting branch");
                    stack.Push(new LNode()
                    {
                        position = stack.Peek().position,
                        //anglesDeg = stack.Peek().anglesDeg,
                        localRotation = stack.Peek().localRotation,
                        rotation = stack.Peek().rotation,
                        prevNodeThickness = stack.Peek().thickness,
                        branchId = (ushort)(assignableBranchId + 1),
                        parentBranchId = stack.Peek().branchId,
                        // decreasing the thickness by 1 each node by default (can be changed later by parameters)
                        thickness = stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1, 
                        branchLevel = stack.Peek().branchLevel,
                    });
                    assignableBranchId++;
                    break;

                case Action.EndBranch:
                    printDebug($"Symbol {symbol.name}, ending branch");
                    stack.Pop();

                    break;

                case Action.PlaceLeaf:
                    if (!branchCollision.cutChildBranches)
                        GenerateLeaf(leafShape, stack.Peek().position, stack.Peek().rotation);
                    break;

                default:
                    break;
            }
        }

        timeManager.StopGenTimer();

        Debug.Log("Generation time: " + timeManager.GetGenTime());
        Debug.Log("Average collision time: " + timeManager.GetCollisionDetTimeAvg());


        return segments;
    }
    

    private void GenerateSegment(ref LNode currentNode, int randLength)
    {
        Segment segment = new Segment()
        {
            startPoint = currentNode,
            thickness = currentNode.thickness,
            parentThickness = currentNode.prevNodeThickness,
            branchLevel = currentNode.branchLevel,
            length = randLength,
            branchId = currentNode.branchId,    // TODO: couldn't we use startPoint Node for these values?
            parentBranchId = currentNode.parentBranchId,
        };
        Vector3Int savedPos = currentNode.position;


        /*Vector3 biasedDir = branchGrowthBias == GrowthBiasType.Branch ?
                GetLocalEndpoint(randLength, currentNode.eulerAngles) : GetDirection(branchGrowthBias);*/

        currentNode.position = savedPos + GetLocalEndpoint(randLength, currentNode.eulerAngles);
        /*currentNode.position = savedPos + GetLocalEndpoint(randLength, 
            GetBiasedLocalRotation(currentNode.eulerAngles, biasedDir));*/


        segment.endPoint = currentNode;
        //print("Segment at level " + segment.branchLevel);

        timeManager.StartColTimer();

        // Generate voxels
        List<Vector3Int> positions = new List<Vector3Int>();
        for (int i = 0; i < branchTrialTimes; i++)
        {
            branchCollision.didCollide = false;
            positions = GenerateThickLine(segment);
            // If no collision detected, proceed with the branch
            if (!branchCollision.didCollide) break;
            //print("<color=cyan>Reassigning branch angle...</color>");

            timeManager.AddColCount();

            Vector3 biasedCollisionDir = collisionBranchGrowthBias == GrowthBiasType.Branch ?
                GetLocalEndpoint(randLength, currentNode.eulerAngles) : GetDirection(collisionBranchGrowthBias);

            // Biased towards specific branch direction
            currentNode.position = savedPos + GetLocalEndpoint(randLength,
                GetBiasedLocalRotation(currentNode.eulerAngles, biasedCollisionDir));

            segment.endPoint = currentNode;
        }

        timeManager.StopColTimer();

        if (branchCollision.didCollide)
        {
            branchCollision.cutChildBranches = true;
            branchCollision.didCollide = false;
            branchCollision.cutLevel = segment.branchLevel;
            //print($"<color=yellow>Cut level = {branchCollision.cutLevel}</color>");
        }
        else
        {
            //print($"Segment at {segment.startPos}, id = <b>{segment.branchId}</b>");
            if(!allSegments.ContainsKey(segment.branchId))
                allSegments.Add(segment.branchId, segment);
            // Generate segment's voxels
            foreach (var pos in positions)
                WorldManager.Instance.container[pos] = new Voxel()
                {
                    //id = 1
                    id = WorldManager.Instance.worldColors.Length > segment.thickness ? (byte)(segment.thickness+1) : (byte)2,
                    branchId = segment.branchId,
                    objectId = assignableObjectId,
                };
            //segments.Add(segment);
        }
    }
    

    private void InterpretRotationalParams(Symbol symbol, ref int angle)
    {
        if (symbol.IsParametric)
        {
            for (int i = 0; i < symbol.parameters.Length; i++)
            {
                switch (i)
                {
                    default:
                    case 0:
                        // angle = Random.Range((int)symbol.parameters[0] - 5, (int)symbol.parameters[0] + 5);
                        angle = UnityEngine.Random.Range((int)symbol.parameters[0], (int)symbol.parameters[0]);

                        break;
                    case 1:
                        // Get a random between two angles if there are two values
                        angle = UnityEngine.Random.Range((int)symbol.parameters[0], (int)symbol.parameters[1]);
                        break;
                }
            }
        }
    }

    private void InterpretLineParams(Symbol symbol, ref int length, ref int width)
    {
        if (symbol.IsParametric)
        {
            for (int i = 0; i < symbol.parameters.Length; i++)
            {
                switch (i)
                {
                    default:
                    case 0:
                        length = (int)symbol.parameters[0];
                        break;
                    case 1:
                        width = (int)symbol.parameters[1];
                        break;
                }
            }
        }
    }


    public void printDebug(string str)
    {
        if(enablePrintDebug)Debug.Log(str);
    }

    List<Vector3Int> GenerateThickLine(Segment segment)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(segment.startPos, segment.endPos);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = segment.thickness;
        int radiusSquared = radius * radius;

        int graceDistanceSquared = (radius *3) * (radius*3);

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
                        if (x * x + y * y + z * z <= radiusSquared)
                        {
                            //thickLine.Add(new Vector3Int(point.x + x, point.y + y, point.z + z));

                            Vector3Int voxelPos = new Vector3Int(point.x + x, point.y + y, point.z + z);

                            // If it's occupied and we are out of the grace zone it might be a collision
                            if ((useGraceZone ? !insideGraceZone : true) // if we are even using the grace zone
                                && WorldManager.Instance.container[voxelPos].id != 0)
                            {
                                //if it is a leaf, ignore collision
                                if (WorldManager.Instance.container[voxelPos].id == 1)
                                {
                                    currentSpherePoints.Add(voxelPos);
                                    continue;
                                }
                                /*print($"Object id = {assignableObjectId},  " +
                                    $"collided object id = {WorldManager.Instance.container[voxelPos].objectId}");
                                print("Position: " + voxelPos);*/
                                // If it is a completely different object (other plant or obstacle)
                                if (WorldManager.Instance.container[voxelPos].objectId != assignableObjectId)
                                {
                                    collisionDetected = true;
                                    branchCollision.collisionsCount++;
                                    branchCollision.didCollide = true;
                                    break;
                                }
                                //print("assignableObjectId = " + assignableObjectId);

                                // If smaller branch collisions can be ignored
                                if (allowedBranchCollisionLevel > 0)
                                {
                                    // If the collided branches have a smaller level that is allowed to collide,
                                    // ignore collision
                                    if (WorldManager.Instance.container[voxelPos].id-1 <= allowedBranchCollisionLevel
                                        && segment.thickness <= allowedBranchCollisionLevel)
                                    {
                                        currentSpherePoints.Add(voxelPos);
                                        continue;
                                    }
                                }

                                ushort collidedBranchId = WorldManager.Instance.container[voxelPos].branchId;
                                //print("collidedBranchId = " + collidedBranchId);

                                // If collided with a branch other than parent branch
                                if (segment.parentBranchId != collidedBranchId)
                                {
                                    // If the branches have the same parent and same-parent collision can be ignored
                                    if (ignoreSameParentBranchCollision &&
                                        segment.parentBranchId == allSegments[collidedBranchId].parentBranchId)
                                    {
                                        //print("Ignoring neighbour branch collision");
                                        /*print($"Neighbor branch collision: <color=yellow>{segment.startPos} --> " +
                                            $"{allSegments[collidedBranchId].startPos}</color>");*/
                                    }
                                    // Make the small branches move away, big branches will ignore collisions with smaller
                                    else if(segment.thickness <= WorldManager.Instance.container[voxelPos].id - 1)
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
    }

}
