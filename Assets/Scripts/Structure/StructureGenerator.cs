using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Linq;
using static UnityEngine.Rendering.HableCurve;
using static Utilities;
using static GrowthBias;


public class StructureGenerator : MonoBehaviour
{
    //[SerializeField] public Grammar grammar;
    [SerializeField] public LSystemGenerator lSystemGenerator;
    private Grammar grammar;
    public bool enablePrintDebug = false;
    //public bool ignoreThinBranchCollision = true;

    public int maxLength = 5;
    public int minLength = 3;
    public int maxAngle = 40;
    public int minAngle = 15;
    public int maxThickness = 5;
    private Vector3 startingAngles = Vector3.zero;
    private BranchCollisionHelper branchCollision = new BranchCollisionHelper();

    [Header("Branch Collision Handling")]
    [Range(0, 10)]
    public int allowedBranchCollisionLevel = 1;
    public GrowthBiasType collisionBranchGrowthBias = GrowthBiasType.None;
    [Range(1, 30)]
    public int branchTrialTimes = 1;
    [Range(0, 30)]
    public int collisionAngleOffset = 10;

    [Range(0f, 1f)]
    public float biasStrength = 1;

    public ushort assignableBranchId = 0;

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

    //For calculating local rotation for the branch with biased global growth direction
    public Vector3 GetBiasedLocalRotation(Vector3 prevGlobalEuler, Vector3 biasDirection)
    {
        Quaternion prevGlobalRot = Quaternion.Euler(prevGlobalEuler);

        // ONLY USED WHEN COLLISION DETECTED
        float randomXAngle = Random.Range(-collisionAngleOffset, collisionAngleOffset);
        float randomYAngle = Random.Range(-collisionAngleOffset, collisionAngleOffset);
        Quaternion randomLocalRot = Quaternion.Euler(randomXAngle, randomYAngle, 0f);

        Quaternion unbiasedGlobalRot;
        if (branchCollision.didCollide) unbiasedGlobalRot = prevGlobalRot * randomLocalRot;
        else unbiasedGlobalRot = prevGlobalRot;
        //Debug.Log($"<color=yellow>what would be without bias: {unbiasedGlobalRot}</color>");

        Vector3 unbiasedGlobalDir = unbiasedGlobalRot * Vector3.forward;
        //Debug.Log($"<color=cyan>global forward vector: {unbiasedGlobalDir}</color>");

        Vector3 biasedGlobalDir = Vector3.Slerp(unbiasedGlobalDir, biasDirection.normalized, biasStrength);
        //Debug.Log($"<color=cyan> interpolated vector towards bias: {biasedGlobalDir}</color>");

        // Using the previous rotation up vector to prevent the branch from unnaturally twisting along its own axis
        Vector3 prevUp = prevGlobalRot * Vector3.up;
        Quaternion newGlobalRot = Quaternion.LookRotation(biasedGlobalDir, prevUp);
        //Debug.Log($"<color=lime>new global rotation: {newGlobalRot}</color>");
        return newGlobalRot.eulerAngles;

    }

    private Vector3 GetRandomAngleChange(Vector3 currentAngles)
    {
        Vector3 angle;
        if (collisionBranchGrowthBias != GrowthBiasType.None)
        {
            Vector3 globalDirDifference = currentAngles - GetDirection(collisionBranchGrowthBias);
            //globalDirDifference = globalDirDifference.normalized;
            angle = globalDirDifference + new Vector3(5, 5, 5) * Random.Range(0.1f, 1f);
        }
        else
        {
            Vector3 direction = angleDirections[Random.Range(0, angleDirections.Length)] * (Random.Range(0, 1) == 1 ? 1 : -1);
            angle = direction * Random.Range(minAngle, maxAngle);
        }
        //Get some random overall angle, multiply it by a random from between predefined min and max,
        //multiply it by random betwen 1 and -1 to increase 
        /*Vector3 angle = angleDirections[Random.Range(0, angleDirections.Length)] *
            (Random.Range(minAngle, maxAngle)) * (Random.Range(0, 1) == 1 ? 1 : -1);*/

        return angle;
    }


    public List<Segment> ConvertSentenceToSegments(List<Symbol> sentence)
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

        //printDebug($"<color=#{WorldManager.Instance.worldColors[0].color.ToHexString().TrimEnd("00")}>{WorldManager.Instance.worldColors[0].color.ToHexString()}</color>");

        Stack<Node> stack = new Stack<Node>();
        stack.Push(new Node() { 
            position = new Vector3Int(0, 0, 0), 
            anglesDeg = startingAngles, 
            thickness = maxThickness, 
            branchLevel = 0, 
            prevNodeThickness = maxThickness
        });
        //final += $"<color=#{WorldManager.Instance.worldColors[maxThickness].color.ToHexString().TrimEnd("00")}>";

        foreach (var symbol in sentence)
        {
            Node currentNode;
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
                    currentNode.anglesDeg.x += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateLeft:
                    printDebug($"Symbol {symbol.name}, rotating left");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateForward:
                    printDebug($"Symbol {symbol.name}, rotating forward");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateBackward:
                    printDebug($"Symbol {symbol.name}, rotating backward");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateAxis:
                    printDebug($"Symbol {symbol.name}, rotating axis");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.z += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateRandomDir:
                    printDebug($"Symbol {symbol.name}, rotating random");
                    currentNode = stack.Pop();

                    // Choose the amount of direcitons in the mix (for example +, -o, ++
                    int n = Random.Range(0, 2);
                    for(int i = 0; i < n; i++)
                    {
                        int rand = Random.Range(0, 4);
                    }


                    stack.Push(currentNode);
                    break;

                case Action.StartBranch:
                    printDebug($"Symbol {symbol.name}, starting branch");
                    stack.Push(new Node()
                    {
                        position = stack.Peek().position,
                        anglesDeg = stack.Peek().anglesDeg,
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
                        GenerateLeaf(stack.Peek().position, Quaternion.Euler(
                            stack.Peek().anglesDeg.x, 
                            stack.Peek().anglesDeg.y, 
                            stack.Peek().anglesDeg.z));
                    break;

                default:
                    break;
            }
        }

        return segments;
    }
    

    private void GenerateSegment(ref Node currentNode, int randLength)
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
        currentNode.position = savedPos + GetLocalEndpoint(randLength, currentNode.anglesDeg);

        segment.endPoint = currentNode;
        //print("Segment at level " + segment.branchLevel);

        // Generate voxels
        List<Vector3Int> positions = new List<Vector3Int>();
        for (int i = 0; i < branchTrialTimes; i++)
        {
            branchCollision.didCollide = false;
            positions = GenerateThickLine(segment);
            // If no collision detected, proceed with the branch
            if (!branchCollision.didCollide) break;
            //print("<color=cyan>Reassigning branch angle...</color>");

            Vector3 biasedDir = collisionBranchGrowthBias == GrowthBiasType.Branch ?
                GetLocalEndpoint(randLength, currentNode.anglesDeg) : GetDirection(collisionBranchGrowthBias);

            // Biased towards specific branch direction
            currentNode.position = savedPos + GetLocalEndpoint(randLength,
                GetBiasedLocalRotation(currentNode.anglesDeg, biasedDir));

            segment.endPoint = currentNode;
        }
        //List<Vector3Int> positions = GenerateThickLine(segment);


        if (branchCollision.didCollide)
        {
            branchCollision.cutChildBranches = true;
            branchCollision.didCollide = false;
            branchCollision.cutLevel = segment.branchLevel;
            //print($"<color=yellow>Cut level = {branchCollision.cutLevel}</color>");
        }
        else
        {
            // Generate segment's voxels
            foreach (var pos in positions)
                WorldManager.Instance.container[pos] = new Voxel()
                {
                    //id = 1
                    id = WorldManager.Instance.worldColors.Length > segment.thickness ? (byte)(segment.thickness+1) : (byte)2,
                    branchId = segment.branchId,
                };
            //segments.Add(segment);
        }
    }

    public List<Segment> ConvertSentenceToSegmentsOriginal(List<Symbol> sentence)
    {
        if (grammar == null)
        {
            Debug.LogError("No grammar referenced in Structure Generator!!");
            return new List<Segment>();
        }
        List<Segment> segments = new List<Segment>();
        //printDebug($"<color=#{WorldManager.Instance.worldColors[0].color.ToHexString().TrimEnd("00")}>{WorldManager.Instance.worldColors[0].color.ToHexString()}</color>");

        List<Vector3Int> positions = new List<Vector3Int>
        {
            // starting position
            new Vector3Int(0, 0, 0)
        };
        Stack<Node> stack = new Stack<Node>();
        stack.Push(new Node() { position = positions[0], anglesDeg = startingAngles, thickness = maxThickness, branchLevel = 0 });
        string final = $"";
        //final += $"<color=#{WorldManager.Instance.worldColors[maxThickness].color.ToHexString().TrimEnd("00")}>";

        //int branchLevel = 0; // main trunk

        foreach (var symbol in sentence)
        {
            Node currentNode;
            int randAngle = UnityEngine.Random.Range(minAngle, maxAngle);
            int randLength;
            //int randAngle = 30;
            switch (grammar.GetSymbolAction(symbol))
            {
                case Action.PlaceLine:
                    printDebug($"Symbol {symbol.name}, placing line");
                    final += symbol.name;
                    currentNode = stack.Pop();

                    randLength = UnityEngine.Random.Range(minLength, maxLength);

                    InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?

                    Segment segment = new Segment()
                    {
                        startPoint = currentNode,
                        thickness = currentNode.thickness,
                        branchLevel = currentNode.branchLevel
                    };

                    currentNode.position = currentNode.position + GetLocalEndpoint(randLength, currentNode.anglesDeg);

                    segment.endPoint = currentNode;
                    segment.length = randLength;
                    segments.Add(segment);

                    positions.Add(currentNode.position);

                    currentNode.branchLevel++;
                    stack.Push(currentNode);
                    break;

                case Action.RotateRight:
                    printDebug($"Symbol {symbol.name}, rotating right");

                    InterpretRotationalParams(symbol, ref randAngle);

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateLeft:
                    printDebug($"Symbol {symbol.name}, rotating left");

                    InterpretRotationalParams(symbol, ref randAngle);

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateForward:
                    printDebug($"Symbol {symbol.name}, rotating forward");

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateBackward:
                    printDebug($"Symbol {symbol.name}, rotating backward");

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.StartBranch:
                    printDebug($"Symbol {symbol.name}, starting branch");
                    //currentThickness = currentThickness > 1 ? currentThickness - 1 : 1;
                    final += $"</color>";
                    //final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1].color.ToHexString().TrimEnd("00")}>";
                    final += symbol.name;
                    stack.Push(new Node()
                    {
                        position = stack.Peek().position,
                        anglesDeg = stack.Peek().anglesDeg,
                        thickness = stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1,
                        branchLevel = stack.Peek().branchLevel + 1,
                    });
                    break;

                case Action.EndBranch:
                    printDebug($"Symbol {symbol.name}, ending branch");
                    //currentThickness = currentThickness < maxThickness ? currentThickness + 1 : maxThickness;
                    final += symbol.name;
                    final += $"</color>";
                    //final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness].color.ToHexString().TrimEnd("00")}>";

                    stack.Pop();

                    break;

                default:
                    break;
            }
        }
        final += $"</color>";
        printDebug(final);

        return segments;
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
                        angle = Random.Range((int)symbol.parameters[0] - 5, (int)symbol.parameters[0] + 5);
                        break;
                    case 1:
                        // Get a random between two angles if there are two values
                        angle = Random.Range((int)symbol.parameters[0], (int)symbol.parameters[1]);
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

    public List<Vector3Int> GenerateThickLine(Segment segment)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(segment.startPos, segment.endPos);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = segment.thickness;
        int radiusSquared = radius * radius;

        // The branch must clear its own thickness before it cares about collisions
        int graceDistanceSquared = (radius *3) * (radius*3);

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
                                //print($"<color=orange>Collision detected at {voxelPos}!\nVoxel id is {WorldManager.Instance.container[voxelPos].id}</color>");
                                //print($"<color=yellow>Line {segment.startPos} --> {segment.endPos}</color>");
                                //print($"(point - A).sqrMagnitude = {(point - segment.startPos).sqrMagnitude},  graceDistanceSquared = {graceDistanceSquared}");

                                // If smaller branch collisions can be ignored
                                if (allowedBranchCollisionLevel > 0)
                                {
                                    // If it is the smaller branches that collide with each other, ignore collision
                                    if (WorldManager.Instance.container[voxelPos].id-1 <= allowedBranchCollisionLevel
                                        && segment.thickness <= allowedBranchCollisionLevel)
                                    {
                                        currentSpherePoints.Add(voxelPos);
                                        continue;
                                    }
                                }

                                // Ignore collisions with the parent branch
                                if(segment.parentBranchId != WorldManager.Instance.container[voxelPos].branchId)
                                {
                                    // Make the small branches move away, big branches will ignore collisions with smaller
                                    if (segment.thickness <= WorldManager.Instance.container[voxelPos].id - 1)
                                    {
                                        collisionDetected = true;
                                        branchCollision.collisionsCount++;
                                        branchCollision.didCollide = true;
                                        break;
                                    }
                                    /*collisionDetected = true;
                                    branchCollision.collisionsCount++;
                                    branchCollision.didCollide = true;
                                    break;*/
                                }
                                /*else
                                {
                                    print("<color=lime>Ignoring collision with parent branch</color>");
                                }*/


                                /*collisionDetected = true;
                                branchCollision.collisionsCount++;
                                branchCollision.didCollide = true;
                                break;*/
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


    private void GenerateLeaf(Vector3Int branchPointPos, Quaternion branchRot)
    {
        //print("Placing leaf at " + branchPointPos);

        Vector3Int leafStart = leafLines.FirstOrDefault(x => x.y == 0); // find the leaf tail starting position

        Vector3Int[] leafLinesCopy = leafLines.ToArray();
        List<Vector3Int> newLeafPositions = new List<Vector3Int>();

        for (int i = 0; i < leafLines.Length; i++)
        {
            if (i % 2 != 0)
            {
                //print($"int y = {leafLines[i].y}; y <= {leafLines[i - 1].y}; y++");
                for (int y = leafLines[i-1].y; y <= leafLines[i].y; y++)
                {
                    Vector3Int vec = new Vector3Int(leafLines[i].x, y, leafLines[i].z);
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
        newLeafPositions = VoxelRotator.RotateLeaves(originalLeaves, leafStart, branchRot, leafTilt).ToList();


        foreach (var pos in newLeafPositions)
        {
            WorldManager.Instance.container[pos + branchPointPos] = new Voxel()
            {
                id = 1
            };
        }
    }

    static readonly Vector3[] angleDirections = new Vector3[7]
    {
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1),
    };

    static readonly Vector3Int[] leaf = new Vector3Int[12]
    {
        new Vector3Int(0, 2, 0),
        new Vector3Int(0, 3, 0),
        new Vector3Int(0, 4, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 2, 0),
        new Vector3Int(1, 3, 0),
        new Vector3Int(1, 4, 0),
        new Vector3Int(1, 5, 0),
        new Vector3Int(2, 2, 0),
        new Vector3Int(2, 3, 0),
        new Vector3Int(2, 4, 0)
    };

    // rounder leaf
    /*static readonly Vector3Int[] leafLines = new Vector3Int[10]
    {
        new Vector3Int(0, 3, 0),
        new Vector3Int(0, 7, 0),
        new Vector3Int(1, 2, 0),
        new Vector3Int(1, 8, 0),
        new Vector3Int(2, 0, 0),
        new Vector3Int(2, 9, 0),
        new Vector3Int(3, 2, 0),
        new Vector3Int(3, 8, 0),
        new Vector3Int(4, 3, 0),
        new Vector3Int(4, 7, 0),
    };*/

    static readonly Vector3Int[] leafLines = new Vector3Int[2]
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(0, 6, 0)
    };

}

public static class VoxelRotator
{
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