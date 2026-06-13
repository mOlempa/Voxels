using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Linq;
using static UnityEngine.Rendering.HableCurve;

public struct Node
{
    public Vector3Int position;
    public Vector3 anglesDeg;
    public int thickness;
    public int branchLevel;
}

public struct Segment
{
    public Node startPoint, endPoint;
    public int thickness;
    public int branchLevel;
    public int length;
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
            endPoint = new Node { 
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

public class StructureGenerator : MonoBehaviour
{
    [SerializeField] public Grammar grammar;
    public bool enablePrintDebug = false;

    public int maxLength = 5;
    public int minLength = 3;
    public int maxAngle = 40;
    public int minAngle = 15;
    public int maxThickness = 5;
    private Vector3 startingAngles = Vector3.zero;
    private BranchCollisionHelper branchCollision = new BranchCollisionHelper();

    [Header("Branch Generation Retrial")]
    [Range(1, 30)]
    public int branchTrialTimes = 1;
    [Range(0, 30)]
    public int retryBranchAngleValue = 10;
    private Vector3 GetRandomAngleChange(int currentAngleChange)
    {
        Vector3 angle = angleDirection[Random.Range(0, angleDirection.Length)] *
            (Random.Range(minAngle, maxAngle)+retryBranchAngleValue) * (Random.Range(0, 1) == 1 ? 1 : -1);

        //Vector3 angle = angleDirection[Random.Range(0, angleDirection.Length)] * retryBranchAngleValue * (Random.Range(0, 1) == 1 ? 1 : -1)
        //angle.x = Mathf.Clamp(angle.x, randAngle)

        return angle;
    }


    public List<Segment> ConvertSentenceToSegments2(List<Symbol> sentence)
    {
        if (grammar == null)
        {
            Debug.LogError("No grammar referenced in Structure Generator!!");
            return new List<Segment>();
        }
        List<Segment> segments = new List<Segment>();
        //int currentThickness = maxThickness;

        printDebug($"<color=#FF1100>aaaaa</color>");
        //printDebug($"<color=#{WorldManager.Instance.worldColors[0].color.ToHexString().TrimEnd("00")}>{WorldManager.Instance.worldColors[0].color.ToHexString()}</color>");

        Stack<Node> stack = new Stack<Node>();
        stack.Push(new Node() { position = new Vector3Int(0, 0, 0), anglesDeg = startingAngles, thickness = maxThickness, branchLevel = 0 });
        string final = $"";
        //final += $"<color=#{WorldManager.Instance.worldColors[maxThickness].color.ToHexString().TrimEnd("00")}>";

        //int branchLevel = 0; // main trunk

        foreach (var symbol in sentence)
        {
            Node currentNode;
            int randAngle = UnityEngine.Random.Range(minAngle, maxAngle);
            int randLength;

            switch (grammar.GetSymbolAction(symbol))
            {
                case Action.PlaceLine:
                    printDebug($"Symbol {symbol.name}, placing line");
                    final += symbol.name;

                    currentNode = stack.Pop();
                    randLength = UnityEngine.Random.Range(minLength, maxLength);

                    if (branchCollision.cutChildBranches)
                    {
                        if (currentNode.branchLevel <= branchCollision.cutLevel)
                        {
                            branchCollision.cutChildBranches = false;
                            InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?
                            GenerateSegment(ref currentNode, randLength, randAngle);
                        }
                    }
                    else 
                    {
                        // If the symbol is parameterized, assign parameters to appropriate variables
                        InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?
                        GenerateSegment(ref currentNode, randLength, randAngle);

                        /*Segment segment = new Segment()
                        {
                            startPoint = currentNode,
                            thickness = currentNode.thickness,
                            branchLevel = currentNode.branchLevel,
                            length = randLength
                        };
                        currentNode.position = currentNode.position + GetLocalEndpoint(randLength, currentNode.anglesDeg);
                        segment.endPoint = currentNode;

                        // Generate voxels
                        List<Vector3Int> positions = new List<Vector3Int>();
                        for (int i = 0; i < branchTryAmount; i++)
                        {
                            positions = GenerateThickLine(segment, ref branchCollision);
                            // If no collision detected, proceed with the branch
                            if (!branchCollision.didCollide) break;
                            // If collision detected, assign new end node
                            currentNode.position = currentNode.position + GetLocalEndpoint(randLength,
                                currentNode.anglesDeg + new Vector3(5, 5, 5));  // make the new angles random in some range !!!
                            segment.endPoint = currentNode;
                        }

                        if (branchCollision.didCollide)
                        {
                            branchCollision.cutChildBranches = true;
                            branchCollision.cutLevel = segment.branchLevel;
                        }
                        else
                        {
                            foreach (var pos in positions)
                                WorldManager.Instance.container[pos] = new Voxel()
                                {
                                    //id = 1
                                    id = WorldManager.Instance.worldColors.Length > segment.thickness ? (byte)segment.thickness : (byte)1
                                };
                            //segments.Add(segment);
                        }*/

                    }

                    //segments.Add(segment);
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
                    //final += $"</color>";
                    //final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1].color.ToHexString().TrimEnd("00")}>";
                    final += symbol.name;
                    stack.Push(new Node()
                    {
                        position = stack.Peek().position,
                        anglesDeg = stack.Peek().anglesDeg,
                        thickness = stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1,
                        branchLevel = stack.Peek().branchLevel,
                    });
                    break;

                case Action.EndBranch:
                    printDebug($"Symbol {symbol.name}, ending branch");
                    //currentThickness = currentThickness < maxThickness ? currentThickness + 1 : maxThickness;
                    final += symbol.name;
                    //final += $"</color>";
                    //final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness].color.ToHexString().TrimEnd("00")}>";

                    stack.Pop();

                    break;

                default:
                    break;
            }
        }
        //final += $"</color>";
        printDebug(final);

        return segments;
    }


    private void GenerateSegment(ref Node currentNode, int randLength, int randAngle)
    {
        Segment segment = new Segment()
        {
            startPoint = currentNode,
            thickness = currentNode.thickness,
            branchLevel = currentNode.branchLevel,
            length = randLength
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
            print("<color=cyan>Reassigning branch angle...</color>");
            // If collision detected, assign new end node
            currentNode.position = savedPos + GetLocalEndpoint(randLength, currentNode.anglesDeg += GetRandomAngleChange(randAngle));
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
            foreach (var pos in positions)
                WorldManager.Instance.container[pos] = new Voxel()
                {
                    //id = 1
                    id = WorldManager.Instance.worldColors.Length > segment.thickness ? (byte)segment.thickness : (byte)1
                };
            //segments.Add(segment);
        }
    }

    public List<Segment> ConvertSentenceToSegments(List<Symbol> sentence)
    {
        if (grammar == null)
        {
            Debug.LogError("No grammar referenced in Structure Generator!!");
            return new List<Segment>();
        }
        List<Segment> segments = new List<Segment>();
        //int currentThickness = maxThickness;

        printDebug($"<color=#FF1100>aaaaa</color>");
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
                        angle = (int)symbol.parameters[0];
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

    public static Vector3Int GetLocalEndpoint(float length, Vector3 eulerAngles)
    {
        // Converting the Euler angles into a rotation Quaternion
        Quaternion rotation = Quaternion.Euler(eulerAngles);

        // Multiplying the rotation by Unity's forward vector (0, 0, 1) scaled by length
        // (in Unity, multiplying a Quaternion by a Vector3 rotates that vector)
        Vector3 floatingPointTarget = rotation * Vector3.forward * length;

        // Converting the floating-point position to integer voxel coordinates
        return Vector3Int.RoundToInt(floatingPointTarget);
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

                            // If it's occupied and we are out of the grace zone -> Collision!
                            if (!insideGraceZone && WorldManager.Instance.container[voxelPos].id != 0)
                            {
                                //print($"<color=orange>Collision detected at {voxelPos}!\nVoxel id is {WorldManager.Instance.container[voxelPos].id}</color>");
                                //print($"<color=yellow>Line {segment.startPos} --> {segment.endPos}</color>");
                                //print($"(point - A).sqrMagnitude = {(point - segment.startPos).sqrMagnitude},  graceDistanceSquared = {graceDistanceSquared}");
                                collisionDetected = true;
                                branchCollision.collisionsCount++;
                                branchCollision.didCollide = true;
                                break;
                            }

                            currentSpherePoints.Add(voxelPos);
                        }
                    }
                    if (collisionDetected) break;
                }
                if (collisionDetected) break;
            }

            // If we hit something outside the grace zone, stop growing the branch right here
            if (collisionDetected)
            {
                break;
            }

            // Otherwise, commit these points to our branch
            foreach (var pos in currentSpherePoints)
            {
                thickLine.Add(pos);
            }
        }

        return thickLine.ToList();
    }

    public List<Vector3Int> GenerateThickLine(Vector3Int A, Vector3Int B, int radius)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(A, B);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points

        int radiusSquared = radius * radius;

        // Applying a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
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
                            thickLine.Add(new Vector3Int(point.x + x, point.y + y, point.z + z));
                        }
                    }
                }
            }
        }

        return thickLine.ToList();
    }

    // Generating a line of voxels (positions) between two points based on Bresenham 3D algorithm
    List<Vector3Int> GenerateLine(Vector3Int A, Vector3Int B)
    {
        List<Vector3Int> points = new List<Vector3Int>
        {
            A
        };

        Vector3Int d = new Vector3Int(Mathf.Abs(B.x - A.x), Mathf.Abs(B.y - A.y), Mathf.Abs(B.z - A.z));
        Vector3Int step = new Vector3Int(B.x > A.x ? 1 : -1, B.y > A.y ? 1 : -1, B.z > A.z ? 1 : -1);

        if (d.x >= d.y && d.x >= d.z)
        {
            int p1 = 2 * d.y - d.x;
            int p2 = 2 * d.z - d.x;
            while (A.x != B.x)
            {
                A.x += step.x;
                if (p1 >= 0)
                {
                    A.y += step.y;
                    p1 -= 2 * d.x;
                }
                if (p2 >= 0)
                {
                    A.z += step.z;
                    p2 -= 2 * d.x;
                }
                p1 += 2 * d.y;
                p2 += 2 * d.z;
                points.Add(A);
            }
        }
        else if (d.y >= d.x && d.y >= d.z)
        {
            int p1 = 2 * d.x - d.y;
            int p2 = 2 * d.z - d.y;
            while (A.y != B.y)
            {
                A.y += step.y;
                if (p1 >= 0)
                {
                    A.x += step.x;
                    p1 -= 2 * d.y;
                }
                if (p2 >= 0)
                {
                    A.z += step.z;
                    p2 -= 2 * d.y;
                }
                p1 += 2 * d.x;
                p2 += 2 * d.z;
                points.Add(A);
            }
        }
        else
        {
            int p1 = 2 * d.y - d.z;
            int p2 = 2 * d.x - d.z;
            while (A.z != B.z)
            {
                A.z += step.z;
                if (p1 >= 0)
                {
                    A.y += step.y;
                    p1 -= 2 * d.z;
                }
                if (p2 >= 0)
                {
                    A.x += step.x;
                    p2 -= 2 * d.z;
                }
                p1 += 2 * d.y;
                p2 += 2 * d.x;
                points.Add(A);
            }
        }


        return points;
    }

    static readonly Vector3[] angleDirection = new Vector3[7]
    {
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1),
    };
}
