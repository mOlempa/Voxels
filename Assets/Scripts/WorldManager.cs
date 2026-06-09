using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class WorldManager : MonoBehaviour
{
    [SerializeField]
    public LSystemGenerator lSystemGenerator;

    [SerializeField]
    public StructureGenerator structureGenerator;

    public VoxelColor[] worldColors;
    public Material worldMaterial;

    private Container container;

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

    // Start is called before the first frame update
    void Start()
    {
        /*string[] tokens = { "x-1", "a", "y/2+1", "(1-e)^2", "x*5" };
        var result = new Dictionary<char, List<Func<float, float>>>();
        SuccessorParser.ParseParamOperations(tokens, ref result);

        // Let's test evaluating the compiled 'x' functions (x-1 and x*5)
        if (result.ContainsKey('a'))
        {
            print("Evaluating operations for parameter 'x' with input value 10:");
            foreach (var func in result['a'])
            {
                // Output 1: 10 - 1 = 9
                // Output 2: 10 * 5 = 50
                print($"Result: {func(5)}");
            }
        }

        // Let's test evaluating the 'e' function: (1 - e)^2
        if (result.ContainsKey('e'))
        {
            var eFunc = result['e'].First();
            print($"\nEvaluating '(1-e)^2' where e = 3: {eFunc(3f)}"); // (1 - 3)^2 = 4
        }
        return;*/


        GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();

        container.Initialize(worldMaterial, Vector3.zero);

        //string sentence = lSystemGenerator.GenerateSentence();
        List<Symbol> sentence = lSystemGenerator.GenerateSentence();
        //return;
        //List<Vector3Int> points = GenerateLine(pointA, pointB);
        List<Segment> segments = structureGenerator.ConvertSentenceToSegments(sentence);

        // generating structure
        foreach (var segment in segments)
        {
            List<Vector3Int> positions = GenerateThickLine(segment.startPos, segment.endPos, segment.thickness);
            //List<Vector3Int> positions = GenerateLine(segment.startPos, segment.endPos);
            foreach (var pos in positions)
                container[pos] = new Voxel() { id = 
                    worldColors.Length > segment.thickness ? (byte)segment.thickness : (byte)1};
        }


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




    // Generating a line of voxels between two points based on Bresenham 3D algorithm
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

}
