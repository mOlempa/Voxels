using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static Utilities;

public class WorldManager : MonoBehaviour
{
    [SerializeField]
    public LSystemGenerator lSystemGenerator;

    [SerializeField]
    public StructureGenerator structureGenerator;

    [SerializeField]
    public SpaceColonizer spaceColonizer;

    public VoxelColor[] worldColors;
    public Material plantMaterial;

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

        container.Initialize(plantMaterial, Vector3.zero);

        // Space Colonization Generation -----
        //SpaceColonizer spaceColonizer = new SpaceColonizer();
        /*spaceColonizer.GenerateAttractors(300, new Vector3Int(50, 100, 50));
        spaceColonizer.ShowAttractors();
        spaceColonizer.Colonize(new Vector3Int(0, 0, 0));*/
        // -------


        // L-System Plant Generation------

        List<Symbol> sentence = lSystemGenerator.GenerateSentence();

        // Collision detection generation
        structureGenerator.ConvertSentenceToSegments(sentence);

        // -------

        container.GenerateMesh();
        container.UploadMesh();

        //transform.Rotate(-90, 0, 0); 
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
