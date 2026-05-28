using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Container : MonoBehaviour
{
    public Vector3 position;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public Dictionary<Vector3, Voxel> data;
    public MeshData meshData = new MeshData();

    public void Initialize(Material mat, Vector3 pos)
    {
        ConfigureComponent();
        data = new Dictionary<Vector3, Voxel>();
        meshRenderer.sharedMaterial = mat;
        position = pos;
    }

    public void ConfigureComponent()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public void ClearData()
    {
        data.Clear();
    }

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
            // don't check empty voxels
            if(kvp.Value.id == 0) continue;

            blockPos = kvp.Key;
            block = kvp.Value;

            voxelColor = WorldManager.Instance.worldColors[block.id - 1];   // cause 0 is air
            colorAlpha = voxelColor.color;
            colorAlpha.a = 1;
            smoothness = new Vector2(voxelColor.metallic, voxelColor.smoothness);

            for (int i = 0; i < 6; i++)  // iterating over each face direction
            {
                // if the face is neighboring to another solid block, skip rendering it
                if (this[blockPos + voxelFaceChecks[i]].isSolid) continue;

                // drawing the face

                // collecting the appropriate vertices from the default vertices and adding the voxel pos
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
                    meshData.colors.Add(colorAlpha);
                    meshData.triangles.Add(counter++);
                }
            }
        }
    }

    public void UploadMesh()
    {
        meshData.UploadMesh();
        
        if(meshRenderer != null)
        {
            ConfigureComponent();
        }

        meshFilter.mesh = meshData.mesh;
        if(meshData.vertices.Count > 3)
        {
            meshCollider.sharedMesh = meshData.mesh;
        }

    }

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

    public static Voxel emptyVoxel = new Voxel() { id = 0 };

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

    static readonly int[,] voxelVertexIndex = new int[6, 4]
    {
        { 0, 1, 2, 3 },
        { 4, 5, 6, 7 },
        { 4, 0, 6, 2 },
        { 5, 1, 7, 3 },
        { 0, 1, 4, 5 },
        { 2, 3, 6, 7 },
    };

    static readonly Vector2[] voxelUVs = new Vector2[4]
    {
        new Vector2(0,0),
        new Vector2(0,1),
        new Vector2(1,0),
        new Vector2(1,1),
    };

    static readonly int[,] voxelTris = new int[6, 6]
    {
        {0, 2, 3, 0, 3, 1 },
        {0, 1, 2, 1, 3, 2 },
        {0, 2, 3, 0, 3, 1 },
        {0, 1, 2, 1, 3, 2 },
        {0, 1, 2, 1, 3, 2 },
        {0, 2, 3, 0, 3, 1 },
    };

    // for checking neighboring faces
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
