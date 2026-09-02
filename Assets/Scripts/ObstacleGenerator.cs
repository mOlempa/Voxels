using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa zarz¹dzaj¹ca generowaniem przeszkód.
 */
public class ObstacleGenerator : MonoBehaviour
{
    /**
     * Referencja do obiektu nadrzêdnego dla obiektów zawieraj¹cych siatki przeszkód.
     */
    [SerializeField]
    GameObject obstaclesParent;

    /**
     * Tablica obiektów typu Collider przechowuj¹ca siatkê fizyczn¹ obiektu.
     */
    [HideInInspector] public Collider[] obstacleColliders;

    /**
     * Zmienna typu int przechowuj¹ca liczbê wokseli dla wygenerowanych przeszkód.
     */
    [HideInInspector] public int voxelCount = 0;

    /**
     * Metoda generuj¹ca przeszkody.
     */
    public void GenerateObstacle()
    {
        obstacleColliders = obstaclesParent.GetComponentsInChildren<Collider>();

        if (obstacleColliders == null) return;
        foreach(Collider collider in obstacleColliders) 
        { 
            if (collider == null) return;
            Bounds bounds = collider.bounds;
            List<Vector3Int> positions = new List<Vector3Int>();   

            for(int x = Mathf.RoundToInt(bounds.min.x); x < Mathf.RoundToInt(bounds.max.x); x++)
            {
                for (int y = Mathf.RoundToInt(bounds.min.y); y < Mathf.RoundToInt(bounds.max.y); y++)
                {
                    for (int z = Mathf.RoundToInt(bounds.min.z); z < Mathf.RoundToInt(bounds.max.z); z++)
                    {
                        Vector3Int point = new Vector3Int(x, y, z);
                        if (Utilities.IsPointInCollider(collider, point))
                            positions.Add(point);
                    }
                }
            }
            GenerateVoxels(positions);
            collider.gameObject.SetActive(false);
        }
    }

    /**
     * Metoda generuj¹ca woksele dla przeszkód na podstawie obliczonych pozycji.
     * @param positions lista pozycji dla wokseli przeszkód.
     */
    void GenerateVoxels(List<Vector3Int> positions)
    {
        voxelCount = positions.Count;
        foreach (var pos in positions)
        {
            MainManager.Instance.container[pos] = new Voxel()
            {
                id = 100,
                objectId = 0,    // obstacles are counted together as one object
            };
        }
    }
}
