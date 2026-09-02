using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

/**
 * Klasa reprezentuj¹ca strukturê przechowuj¹c¹ siatkê wokseli.
 */
public partial class Container : MonoBehaviour
{
    /**
     * Pozycja struktury siatki.
     */
    public Vector3 position;

    /**
     * Obiekt typu meshFiler s³u¿¹cy do przekazania danej siatki do renderowania.
     */
    public MeshFilter meshFilter;

    /**
     * Obiekt typu meshRenderer s³u¿¹cy do renderowania siatki.
     */
    public MeshRenderer meshRenderer;

    /**
     * Struktura danych typu Dictionary przechowuj¹ca woksele i ich pozycje.
     */
    public Dictionary<Vector3, Voxel> data;

    /**
     * Obiekt typu MeshData zarz¹dzaj¹cy danymi siatki wokseli.
     */
    public MeshData meshData = new MeshData();

    /**
     * Metoda inicjalizuj¹ca potrzebne komponenty.
     * @param mat materia³ dla siatki.
     * @pos pozycja dla struktury przechowuj¹cej siatkê.
     */
    public void Initialize(Material mat, Vector3 pos)
    {
        ConfigureComponent();
        data = new Dictionary<Vector3, Voxel>();
        meshRenderer.sharedMaterial = mat;
        position = pos;

    }

    /**
     * Metoda konfiguruj¹ca komponenty potrzebne do wyrenderowania siatki.
     */
    public void ConfigureComponent()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    /**
     * Metoda czyszcz¹ca dane wokseli.
     */
    public void ClearData()
    {
        data.Clear();
    }

    /**
     * Metoda obliczaj¹ca i generuj¹ca siatkê wokseli na podstawie ich danych.
     */
    public void GenerateMesh()
    {
        meshData.ClearData();
        Vector3 blockPos;
        Voxel block;

        int counter = 0;
        Vector3[] faceVertices = new Vector3[4];
        Vector2[] faceUVs = new Vector2[4];

        VoxelColor voxelColor;
        Color colorAlpha;
        Vector2 smoothness;

        foreach (KeyValuePair<Vector3, Voxel> kvp in data)
        {
            // Don't check empty voxels
            if (kvp.Value.id == 0) continue;

            blockPos = kvp.Key;
            block = kvp.Value;

            // Don't assign non-existing colors
            if(block.id > MainManager.Instance.worldColors.Length) voxelColor = new VoxelColor() { color = Color.gray };
            else if (block.id == 1) voxelColor = MainManager.Instance.worldColors[5];   // id = 1 reserved for leaves
            else voxelColor = MainManager.Instance.worldColors[block.id - 2];   // cause 0 is air
            colorAlpha = voxelColor.color;
            colorAlpha.a = 1;
            smoothness = new Vector2(voxelColor.metallic, voxelColor.smoothness);

            for (int i = 0; i < 6; i++)  // Iterating over each face direction
            {
                // If the face is neighboring to another solid block, skip rendering it
                if (this[blockPos + voxelFaceChecks[i]].isSolid) continue;


                // Collecting the appropriate vertices from the default vertices and adding the voxel pos
                for (int j = 0; j < 4; j++)
                {
                    faceVertices[j] = voxelVertices[voxelVertexIndex[i, j]] + blockPos;
                    faceUVs[j] = voxelUVs[j];
                }

                for (int j = 0; j < 6; j++)
                {
                    meshData.vertices.Add(faceVertices[voxelTris[i, j]]);
                    meshData.uvs.Add(faceUVs[voxelTris[i, j]]);
                    meshData.uvs2.Add(smoothness);
                    meshData.uvs3.Add(new Vector2(block.id, 0));
                    meshData.colors.Add(colorAlpha);
                    meshData.triangles.Add(counter++);
                }
            }
        }
    }

    /**
     * Metoda wgrywaj¹ca obliczon¹ siatkê.
     */
    public void UploadMesh()
    {
        meshData.UploadMesh();
        
        if(meshRenderer != null)
        {
            ConfigureComponent();
        }

        meshFilter.mesh = meshData.mesh;

    }

    /**
     * Metoda zwracaj¹ca woksel o danej pozycji.
     */
    public Voxel this[Vector3 index]
    {
        get
        {
            if(data.ContainsKey(index))
                return data[index];
            else
                return emptyVoxel;
        }
        set
        {
            if (data.ContainsKey(index))
                data[index] = value;
            else
                data.Add(index, value);
        }
    }

    /**
     * Zmienna typu Voxel reprezentuj¹ca pusty woksel (powietrze)
     */
    public static Voxel emptyVoxel = new Voxel() { id = 0 };

    /**
     * Tablica okreœlaj¹ca wierzcho³ki wokseli jako pozycje wierzcho³ków szeœcianu.
     */
    static readonly Vector3[] voxelVertices = new Vector3[8]
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),

        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1)
    };

    /**
     * Tablica okreœlaj¹ca indeksy wierzcho³ków wokseli.
     */
    static readonly int[,] voxelVertexIndex = new int[6, 4]
    {
        { 0, 1, 2, 3 },
        { 4, 5, 6, 7 },
        { 4, 0, 6, 2 },
        { 5, 1, 7, 3 },
        { 0, 1, 4, 5 },
        { 2, 3, 6, 7 },
    };

    /**
     * Tablica okreœlaj¹ca wierzcho³ki UV wokseli.
     */
    static readonly Vector2[] voxelUVs = new Vector2[4]
    {
        new Vector2(0,0),
        new Vector2(0,1),
        new Vector2(1,0),
        new Vector2(1,1),
    };

    /**
     * Tablica okreœlaj¹ca trójk¹ty dla siatki pojedynczego woksela.
     */
    static readonly int[,] voxelTris = new int[6, 6]
    {
        {0, 2, 3, 0, 3, 1 },
        {0, 1, 2, 1, 3, 2 },
        {0, 2, 3, 0, 3, 1 },
        {0, 1, 2, 1, 3, 2 },
        {0, 1, 2, 1, 3, 2 },
        {0, 2, 3, 0, 3, 1 },
    };

    /**
     * Tablica okreœlaj¹ca œciany s¹siednie do danego woksela.
     */
    static readonly Vector3[] voxelFaceChecks = new Vector3[6]
    {
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1),
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, 1, 0)
    };

}

