using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static Utilities;

public enum Algorithm
{
    LSystem,
    SpaceColonization
}

public class WorldManager : MonoBehaviour
{
    //[HideInInspector] 
    //public BranchCollisionHelper branchCollision = new BranchCollisionHelper();

    public Algorithm usedAlgorithm = Algorithm.LSystem;
    public Material plantMaterial;

    [SerializeField]
    public LSystemGenerator lSystemGenerator;

    [SerializeField]
    public StructureGenerator structureGenerator;

    [SerializeField]
    public SpaceColonizer spaceColonizer;

    [SerializeField]
    ObstacleGenerator obstacleGenerator;

    public VoxelColor[] worldColors;

    [HideInInspector] public Container container;

    //public byte assignableObjectId = 0;
    [HideInInspector] public List<byte> assignableObjectIdList = new List<byte>();

    string dir = @"C:\Users\Meg\Desktop\Results";
    string generationData = "";
    TimeManager timeManager = new TimeManager();

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
        print("To generate model, press ENTER");
        /*string time = DateTime.Now.ToString("ddMMyy-HHmmss");
        assignableObjectIdList.Add(0);
        DateTime start = DateTime.Now;
        *//*GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();

        container.Initialize(plantMaterial, Vector3.zero);

        if(obstacleGenerator != null)
        {
            obstacleGenerator.GenerateObstacle();
        }

        string result = "";


        if (usedAlgorithm == Algorithm.SpaceColonization)
        {
            spaceColonizer.Colonize(new Vector3Int(0, 0, 0));
            result = spaceColonizer.GetDataString();
        }

        if (usedAlgorithm == Algorithm.LSystem)
        {
            List<Symbol> sentence = lSystemGenerator.GenerateSentence();
            structureGenerator.ConvertSentenceToSegments(sentence);
            result = structureGenerator.GetDataString();
        }

        container.GenerateMesh();
        container.UploadMesh();*//*


        GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();

        container.Initialize(plantMaterial, Vector3.zero);

        if (obstacleGenerator != null)
        {
            obstacleGenerator.GenerateObstacle();
            assignableObjectIdList.Add((byte)(assignableObjectIdList.Last() + 1));
        }

        GeneratePlant();
        timeManager.StartGenTimer();
        container.GenerateMesh();
        container.UploadMesh();
        timeManager.StopGenTimer();

        if(usedAlgorithm == Algorithm.LSystem) 
            dir += @"\LSystems\Stats";
        if (usedAlgorithm == Algorithm.SpaceColonization)
            dir += @"\SpaceColonization\Stats";

        WriteStats();

        *//*TakeScreenshot();
        using (StreamWriter sw = new StreamWriter(dir + $"/{time}--data.txt", true))
        {
            sw.Write(generationData);
            sw.WriteLine($"Voxel count: {container.data.Count - obstacleGenerator.voxelCount}");
            sw.WriteLine("Mesh generation time: " + timeManager.GetGenTime().ToString());
        }*//*

        Debug.Log("Total time: " + (DateTime.Now - start).TotalSeconds);

        ClearModel();*/
    }

    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            TakeScreenshot();
            Debug.Log("Screenshot taken");
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(Execute());
        }
    }


    void WriteStats()
    {
        int iterations = usedAlgorithm == Algorithm.LSystem ? lSystemGenerator.iterationLimit : spaceColonizer.iterations;
        string alg = usedAlgorithm == Algorithm.LSystem ? "L" : "SC";
        /*string fileName = alg;
        fileName += usedAlgorithm == Algorithm.LSystem ?
            lSystemGenerator.grammar.name + iterations : 
            (spaceColonizer.trunk.nodesAmount > 1 ? "Acacia" : "Bush") + iterations;*/
        string modelName = usedAlgorithm == Algorithm.LSystem ?
            lSystemGenerator.grammar.name : (spaceColonizer.trunk.nodesAmount > 1 ? "Acacia" : "Bush");
        float voxelBounds = container.meshData.mesh.bounds.size.x * container.meshData.mesh.bounds.size.y * container.meshData.mesh.bounds.size.z;

        string fileName = "TestingTree";

        using (StreamWriter sw = new StreamWriter(dir + $"/{fileName}--stats.txt", true))
        {
            sw.WriteLine(generationData +
                $"|{container.data.Count}" +
                $"|{container.data.Count - obstacleGenerator.voxelCount}" +
                $"|{timeManager.GetGenTime().ToString()}" +
                $"|{container.meshData.mesh.bounds.size.x}" +
                $"|{container.meshData.mesh.bounds.size.y}" +
                $"|{container.meshData.mesh.bounds.size.z}" +
                $"|{voxelBounds}" +
                $"|{alg}-{modelName}-it{iterations.ToString("D2")}" +
                $"|{(obstacleGenerator.obstacleColliders.Length > 0 ? "obs" : "clear")}");
            /*sw.WriteLine($"Voxel count: {container.data.Count - obstacleGenerator.voxelCount}");
            sw.WriteLine("Mesh generation time: " + timeManager.GetGenTime().ToString());*/
        }
    }

    void GeneratePlant()
    {
        if (usedAlgorithm == Algorithm.SpaceColonization)
        {
            spaceColonizer.Generate(new Vector3Int(0, 0, 0));
            //generationData = spaceColonizer.GetDataString();
            generationData = spaceColonizer.GetStatistics();
        }

        if (usedAlgorithm == Algorithm.LSystem)
        {
            structureGenerator.Generate();
            //generationData = structureGenerator.GetDataString();
            generationData = structureGenerator.GetStatistics();
        }
        assignableObjectIdList.Add((byte)(assignableObjectIdList.Last() + 1));
    }

    void ClearModel()
    {
        container.ClearData();
    }


    void TakeScreenshot()
    {
        string time = DateTime.Now.ToString("ddMMyy-HHmmss");
        string filename = $"{time}--screenshot.png";
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, filename));
    }



    IEnumerator Execute()
    {
        print("Generating model...");
        yield return new WaitForSeconds(0.2f);
        //string time = DateTime.Now.ToString("ddMMyy-HHmmss");
        assignableObjectIdList.Add(0);
        DateTime start = DateTime.Now;

        /*GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();*/

        container.Initialize(plantMaterial, Vector3.zero);

        if (obstacleGenerator != null)
        {
            obstacleGenerator.GenerateObstacle();
            assignableObjectIdList.Add((byte)(assignableObjectIdList.Last() + 1));
        }

        GeneratePlant();
        timeManager.StartGenTimer();
        container.GenerateMesh();
        container.UploadMesh();
        timeManager.StopGenTimer();

        /*if (usedAlgorithm == Algorithm.LSystem)
            dir += @"\LSystems\Stats";
        if (usedAlgorithm == Algorithm.SpaceColonization)
            dir += @"\SpaceColonization\Stats";*/

        WriteStats();

        Debug.Log("Total time: " + (DateTime.Now - start).TotalSeconds);

        ClearModel();
    }


    // TODO: to be deleted later!
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
