using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca przestrzeñ generowania atraktorów.
 */
public class AttractorSpawnArea : MonoBehaviour
{
    /**
     * Zmienna typu int, której zmiana wywo³uje zaktualizowanie widoku w inspektorze.
     */
    [Range(0, 100)]
    public int refreshCollider;

    /**
     * Referencja do obiektu siatki fizycznej (kolidera) siatki ograniczaj¹cej przestrzeñ generowania atraktorów.
     */
    MeshCollider meshCollider;

    /**
     * Zmienna typu Tuple przechowuj¹ca zakres siatki miêdzy dwoma punktami w osi X.
     */
    [HideInInspector] public (int from, int to) xBounds;

    /**
     * Zmienna typu Tuple przechowuj¹ca zakres siatki miêdzy dwoma punktami w osi Y.
     */
    [HideInInspector] public (int from, int to) yBounds;

    /**
     * Zmienna typu Tuple przechowuj¹ca zakres siatki miêdzy dwoma punktami w osi Z.
     */
    [HideInInspector] public (int from, int to) zBounds;
    
    /**
     * Metoda obliczaj¹ca zakres siatki miêdzy punktami na trzech osiach dla zmiennych klasy.
     * @param bounds zakres siatki w wokselach.
     * @param offset przesuniêcie siatki od punktu (0,0,0) na scenie.
     */
    public void Calculate(Vector3Int bounds, Vector3Int offset)
    {

        xBounds = (-bounds.x + offset.x, bounds.x + offset.x);
        yBounds = (offset.y, bounds.y * 2 + offset.y);
        zBounds = (-bounds.z + offset.z, bounds.z + offset.z);
    }


#if UNITY_EDITOR
    /**
     * Metoda aktualizuj¹ca siatkê po zmianach w zmiennych klasy.
     */
    private void OnValidate()
    {
        if (TryGetComponent(out MeshFilter meshFilter))
        {
            if(meshFilter.sharedMesh != null)
            {
                meshCollider = GetComponent<MeshCollider>();
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }

        }
    }
#endif

}