using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class BranchCollisionHelper
{
    public int collisionsCount;
    public bool didCollide;
    public bool cutChildBranches;
    public int cutLevel;

    public BranchCollisionHelper()
    {
        collisionsCount = 0;
        didCollide = false;
        cutChildBranches = false;
        cutLevel = -1;
    }
}

public class WorldManager : MonoBehaviour
{
    [SerializeField]
    public LSystemGenerator lSystemGenerator;

    [SerializeField]
    public StructureGenerator structureGenerator;

    public VoxelColor[] worldColors;
    public Material worldMaterial;

    public Container container;

    private static WorldManager _instance;
    public static WorldManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<WorldManager>();
            }
            return _instance;
        }
    }

    


    void Start()
    {
        GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();

        container.Initialize(worldMaterial, Vector3.zero);

        //string sentence = lSystemGenerator.GenerateSentence();
        List<Symbol> sentence = lSystemGenerator.GenerateSentence();
        //return;
        //List<Vector3Int> points = GenerateLine(pointA, pointB);

        List<Segment> segments = structureGenerator.ConvertSentenceToSegmentsOriginal(sentence);

        // No collision detection generation
        foreach (var segment in segments)
        {
            List<Vector3Int> positions = GenerateThickLineOriginal(segment.startPos, segment.endPos, segment.thickness);
            //List<Vector3Int> positions = GenerateLine(segment.startPos, segment.endPos);
            foreach (var pos in positions)
                container[pos + new Vector3Int(0, 200, 0)] = new Voxel()
                {
                    //id = 1
                    id = worldColors.Length > segment.thickness ? (byte)segment.thickness : (byte)1
                };
        }

        structureGenerator.ConvertSentenceToSegments(sentence);

        //int counter = 0;
        /*BranchCollisionHelper branchCollision = new BranchCollisionHelper();
        int branchTryAmount = 3;

        foreach (var segment in segments)
        {
            // Don't generate further child branches if collision was detected earlier
            if (branchCollision.cutChildBranches)
            {
                // Check if we are still in the children branches
                if (segment.branchLevel <= branchCollision.cutLevel)
                {
                    branchCollision.cutChildBranches = false;
                }
                else
                {
                    continue;
                }
            }
            branchCollision.didCollide = false;
            List<Vector3Int> positions = new List<Vector3Int>();
            Segment segmentCopy = segment;

            // Try to generate a branch set amount of times in case a collision happens
            for (int i = 0; i < branchTryAmount; i++)
            {
                positions = GenerateThickLine(segmentCopy, ref branchCollision);
                // If no collision detected, proceed with the branch
                if (!branchCollision.didCollide) break;
                // If collision detected, assign new end node
                segmentCopy = segmentCopy.ChangeEndpointPos(
                    StructureGenerator.GetLocalEndpoint(segmentCopy.length, segmentCopy.startPoint.anglesDeg));
            }

            //print("<color=lime>Segment " + counter++ + "</color>");
            //List<Vector3Int> positions = GenerateThickLine(segment, ref branchCollision);

            // If collision detected, stop generating further child branches
            if (branchCollision.didCollide)
            {
                branchCollision.cutChildBranches = true;
                branchCollision.cutLevel = segment.branchLevel;
                continue;
            }

            // Generate voxels
            foreach (var pos in positions)
            {
                container[pos] = new Voxel()
                {
                    id = branchCollision.didCollide ? (byte)1 : (byte)5
                    //id = worldColors.Length > segment.thickness ? (byte)segment.thickness : (byte)1
                };
            }
        }

        print($"<color=orange>Collisions count = {branchCollision.collisionsCount}</color>");*/

        // generating structure
        /*foreach (var segment in segments)
        {
            List<Vector3Int> positions = GenerateThickLine1(segment.startPos, segment.endPos, segment.thickness);
            //List<Vector3Int> positions = GenerateLine(segment.startPos, segment.endPos);
            foreach (var pos in positions)
                container[pos + new Vector3Int(0, 300, 0)] = new Voxel()
                {
                    //id = 1
                    id = worldColors.Length > segment.thickness ? (byte)segment.thickness : (byte)1
                };
        }*/


        /*for (int x = 0; x < aaa; x++)
        {
            for (int z = 0; z < aaa; z++)
            {
                for (int y = 0; y < aaa; y++)
                {
                    container[new Vector3(x, y, z)] = new Voxel() { id = 1 };
                }
            }
        }*/

        container.GenerateMesh();
        container.UploadMesh();

        transform.Rotate(-90, 0, 0); 
    }




    // Generating a line of voxels (positions) between two points based on Bresenham 3D algorithm
    List<Vector3Int> GenerateLine(Vector3Int A, Vector3Int B)
    {
        List<Vector3Int> points = new List<Vector3Int>
        {
            A
        };

        Vector3Int d = new Vector3Int(Mathf.Abs(B.x-A.x), Mathf.Abs(B.y - A.y), Mathf.Abs(B.z - A.z));
        Vector3Int step = new Vector3Int(B.x > A.x ? 1 : -1, B.y > A.y ? 1 : -1, B.z > A.z ? 1 : -1);

        if (d.x >= d.y && d.x >= d.z)
        {
            int p1 = 2 * d.y - d.x;
            int p2 = 2 * d.z - d.x;
            while(A.x != B.x)
            {
                A.x += step.x;
                if(p1 >= 0)
                {
                    A.y += step.y;
                    p1 -= 2 * d.x;
                }
                if(p2 >= 0)
                {
                    A.z += step.z;
                    p2 -= 2 * d.x;
                }
                p1 += 2 * d.y;
                p2 += 2 * d.z;
                points.Add(A);
            }
        }
        else if(d.y >= d.x && d.y >= d.z){
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

    public List<Vector3Int> GenerateThickLine(Segment segment, ref BranchCollisionHelper branchCollision)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(segment.startPos, segment.endPos);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = segment.thickness;
        int radiusSquared = radius * radius;

        // The branch must clear its own thickness before it cares about collisions
        int graceDistanceSquared = (radius + 3) * (radius + 3);
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
                            if (!insideGraceZone && container[voxelPos].id != 0)
                            {
                                //print($"<color=orange>Collision detected at {voxelPos}!\nVoxel id is {container[voxelPos].id}</color>");
                                //print($"<color=yellow>Line {A} --> {B}</color>");
                                //print($"(point - A).sqrMagnitude = {(point - A).sqrMagnitude},  graceDistanceSquared = {graceDistanceSquared}");
                                //collisionDetected = true;
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

    public List<Vector3Int> GenerateThickLineOriginal(Vector3Int A, Vector3Int B, int radius)
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

}
