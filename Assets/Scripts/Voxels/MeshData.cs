using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MeshData
{
    public Mesh mesh;
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();
    public List<Vector2> uvs2 = new List<Vector2>();
    public List<Vector2> uvs3 = new List<Vector2>();
    public List<Color> colors = new List<Color>();

    public bool initialized;

    public void ClearData()
    {
        if (!initialized)
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();
            uvs2 = new List<Vector2>();
            uvs3 = new List<Vector2>();

            colors = new List<Color>();
            mesh = new Mesh();
            initialized = true;
        }
        else
        {
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            uvs2.Clear();
            uvs3.Clear();

            colors.Clear();
            mesh.Clear();
        }
    }

    public void UploadMesh()
    {
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;    // default UInt16 allows too little vertices
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, false);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(2, uvs2);
        mesh.SetUVs(3, uvs3);
        mesh.SetColors(colors);

        mesh.Optimize();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
    }
    
}
