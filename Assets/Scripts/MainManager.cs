using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static Utilities;

/**
 * Obiekt typu enum u¿ywany w obs³udze wyboru algorytmu.
 */
public enum Algorithm
{
    LSystem,
    SpaceColonization
}

/**
 * Klasa zarz¹dzaj¹ca wybieraniem algorytmu i zapisywaniem pomiarów.
 */
public class MainManager : MonoBehaviour
{
    /**
     * Zmienna typu Algorithm pozwalaj¹ca na wybór algorytmu z domyœln¹ wartoœci¹, dostêpna w inspektorze.
     */
    public Algorithm usedAlgorithm = Algorithm.LSystem;

    /**
     * Zmienna typu Material przechowuj¹ca referencjê do materia³u roœliny, dostêpna w inspektorze.
     */
    [SerializeField]
    public Material plantMaterial;

    /**
     * Zmienna typu LSystemGenerator przechowuj¹ca referencjê do klasy zarz¹dzaj¹cej ci¹giem symboli, 
     * dostêpna w inspektorze.
     */
    [SerializeField]
    public LSystemGenerator lSystemGenerator;

    /**
     * Zmienna typu StructureGenerator przechowuj¹ca referencjê do klasy zarz¹dzaj¹cej budow¹ struktury z ci¹gu symboli, 
     * dostêpna w inspektorze.
     */
    [SerializeField]
    public LStructureGenerator structureGenerator;

    /**
     * Zmienna typu SpaceColonizer przechowuj¹ca referencjê do klasy zarz¹dzaj¹cej algorytmem kolonizacji przestrzeni, 
     * dostêpna w inspektorze.
     */
    [SerializeField]
    public SpaceColonizer spaceColonizer;

    /**
     * Zmienna typu ObstacleGenerator przechowuj¹ca referencjê do klasy zarz¹dzaj¹cej generowaniem przeszkód, 
     * dostêpna w inspektorze.
     */
    [SerializeField]
    ObstacleGenerator obstacleGenerator;

    /**
    * Tablica zmiennych typu VoxelColor, s³u¿¹ca do wizualnego odró¿niania gruboœci ga³êzi na etapach debugowania,
    * dostêpna w inspektorze.
    */
    [SerializeField]
    public VoxelColor[] worldColors;

    /**
    * Obiekt klasy Container, przechowuj¹cy dane wokseli i siatki.
    */
    [HideInInspector] public Container container;

    /**
    * Lista bajtów dla przechowywania identyfikatorów obiektów na scenie.
    */
    [HideInInspector] public List<byte> assignableObjectIdList = new List<byte>();

    /**
    * Zmienna typu string przechowuj¹ca œcie¿kê dla pliku z wynikami pomiarów.
    */
    string dir = Application.dataPath + "/Results";

    /**
    * Zmienna typu string s³u¿¹ca do przechowania zapisanych pomiarów algorytmów.
    */
    string generationData = "";

    /**
    * Obiekt klasy TimeManager odpowiedzialnej za pomiary czasu.
    */
    TimeManager timeManager = new TimeManager();

    /**
     * Zmienna typu bool uniemo¿liwiaj¹ca wielokrotne generowanie roœliny w tym samym eksperymencie.
     */
    bool finalized = false;

    /**
     * Instancja singletonu klasy MainManager.
     */
    private static MainManager _instance;

    /**
     * Odniesienie do instancji singletonu klasy MainManager.
     */
    public static MainManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<MainManager>();
            }
            return _instance;
        }
    }

    /**
     * Metoda automatycznie wywo³ywana na starcie aplikacji.
     */
    void Start()
    {
        GameObject cont = new GameObject("Container");
        cont.transform.parent = transform;
        container = cont.AddComponent<Container>();
        print("To generate model, press ENTER");
    }

    /**
     * Metoda automatycznie wywo³ywana w ka¿dej klatce aplikacji, wykrywaj¹ca wciœniêcie klawiszy Spacja b¹dŸ 
     * Enter na klawiaturze.
     */
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            TakeScreenshot();
            Debug.Log("Screenshot taken");
        }

        if(!finalized)
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartCoroutine(Execute());
                finalized = true;
            }
    }

    /**
     * Metoda zapisuj¹ca pomiary u¿ytego algorytmu do pliku .txt.
     */
    void WriteStats()
    {
        int iterations = usedAlgorithm == Algorithm.LSystem ? lSystemGenerator.iterationLimit : spaceColonizer.iterations;
        string alg = usedAlgorithm == Algorithm.LSystem ? "L" : "SC";
        string modelName = usedAlgorithm == Algorithm.LSystem ?
            lSystemGenerator.grammar.name : (spaceColonizer.trunk.nodesAmount > 1 ? "Acacia" : "Bush");
        float voxelBounds = container.meshData.mesh.bounds.size.x * container.meshData.mesh.bounds.size.y * container.meshData.mesh.bounds.size.z;

        string fileName = "TestingTree";
        print(dir);
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
        }
    }

    /**
     * Metoda wykonuj¹ca zrzut ekranu z widoku aktywnej kamery, wywo³ana po wciœniêciu klawisza Spacja.
     */
    void TakeScreenshot()
    {
        string time = DateTime.Now.ToString("ddMMyy-HHmmss");
        string filename = $"{time}--screenshot.png";
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, filename));
    }


    /**
     * Korutyna (wspó³program) wywo³uj¹ca generacjê przeszkód, modelu i zapis pomiarów do pliku.
     */
    IEnumerator Execute()
    {
        print("Generating model...");
        yield return new WaitForSeconds(0.2f);
        assignableObjectIdList.Add(0);
        DateTime start = DateTime.Now;

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

        WriteStats();

        Debug.Log("Total time: " + (DateTime.Now - start).TotalSeconds);

        ClearModel();
    }

    /**
     * Metoda wywo³uj¹ca generowanie modelu za pomoc¹ wybranego algorytmu.
     */
    void GeneratePlant()
    {
        if (usedAlgorithm == Algorithm.SpaceColonization)
        {
            spaceColonizer.Generate();
            generationData = spaceColonizer.GetStatistics();
        }

        if (usedAlgorithm == Algorithm.LSystem)
        {
            structureGenerator.Generate();
            generationData = structureGenerator.GetStatistics();
        }
        assignableObjectIdList.Add((byte)(assignableObjectIdList.Last() + 1));
    }

    /**
     * Metoda czyszcz¹ca dane siatki
     */
    void ClearModel()
    {
        container.ClearData();
    }

}
