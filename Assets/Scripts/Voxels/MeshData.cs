using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * Klasa przechowuj¹ca i zarz¹dzaj¹ca danymi siatki wokselowej.
 */
public class MeshData
{
    /**
     * Obiekt typu mesh przechowuj¹cy siatkê.
     */
    public Mesh mesh;

    /**
     * Lista wierzcho³ków siatki.
     */
    public List<Vector3> vertices = new List<Vector3>();

    /**
     * Lista trójk¹tów siatki obliczanych na podstawie indeksów wierzcho³ków.
     */
    public List<int> triangles = new List<int>();

    /**
     * Lista wierzcho³ków uv siatki dla tekstury.
     */
    public List<Vector2> uvs = new List<Vector2>();

    /**
     * Lista dodatkowych wierzcho³ków uv siatki do przekazania programu cieniuj¹cemu dodatkowych
     * w³aœciwoœci tekstury.
     */
    public List<Vector2> uvs2 = new List<Vector2>();

    /**
     * Lista dodatkowych wierzcho³ków uv siatki do przekazania programu cieniuj¹cemu identyfikatorów wokseli.
     */
    public List<Vector2> uvs3 = new List<Vector2>();

    /**
     * Lista kolorów siatki.
     */
    public List<Color> colors = new List<Color>();

    /**
     * Zmienna typu bool przechowuj¹ca informacjê o inicjalizacji siatki.
     */
    public bool initialized;

    /**
     * Metoda czyszcz¹ca dane siatki.
     */
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

    /**
     * Metoda wgrywaj¹ca dane do siatki w celu jej utworzenia.
     */
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
