using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Utilities;

/**
 * Klasa zarz¹dzaj¹ca roz³o¿eniem atraktorów dla algorytmu kolonizacji przestrzeni.
 */
public class AttractorManager : MonoBehaviour
{
    /**
     * Zmienna typu bool definiuj¹ca widocznoœæ atraktorów na scenie, dostêpna w inspektorze.
     */
    public bool showAttractors = true;

    /**
     * Zmienna typu int okreœlaj¹ca iloœæ pierwotnie wygenerowanych atraktorów, dostêpna w inspektorze.
     */
    [Highlight(1, 0.8f, 0)]
    [Range(20, 5000)]
    public int attractorsAmount = 100;

    /**
     * Obiekt GameObject bêd¹cy referencj¹ do obiektu na scenie posiadaj¹cego siatkê, w której
     * atraktory bêd¹ generowane.
     */
    public GameObject attractorSpawnArea;

    /**
     * Obiekt Vector3 okreœlaj¹cy skalê przestrzeni, w której bêd¹ generowane atraktory, dostêpny w inspektorze.
     */
    public Vector3 spawnAreaScale = Vector3.one;

    /**
     * Obiekt Vector3Int okreœlaj¹cy przesuniêcie przestrzeni, w której bêd¹ generowane atraktory, dostêpny w inspektorze.
     */
    public Vector3Int spawnAreaOffset = Vector3Int.zero;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny promieñ wykrywania atraktorów, dostêpna w inspektorze.
     */
    [Highlight(0.5f, 0.9f, 0.5f)]
    [Range(1, 100)]
    public int maxDistance = 10;

    /**
     * Zmienna typu int okreœlaj¹ca minimalny promieñ wykrywania atraktorów, dostêpna w inspektorze.
     */
    [Highlight(0.5f, 0.9f, 0.5f)]
    [Range(1, 100)]
    public int minDistance = 2;

    /**
     * Zmienna typu int okreœlaj¹ca promieñ usuwania atraktorów, dostêpna w inspektorze.
     */
    [Highlight(1, 0, 0.3f)]
    [Range(1, 100)]
    public int attractorKillRadius = 2;

    /**
     * Struktura danych typu HashSet, przechowuj¹ca atraktory w postaci pozycji wokseli.
     */
    public HashSet<Vector3Int> attractors = new HashSet<Vector3Int>();

    /**
     * Zmienna typu byte okreœlaj¹ca identyfikator wokseli, jaki zostanie u¿yty w przypadku 
     * wizualnego generowania atraktorów.
     */
    byte attractorVoxelID = 6;

    /**
     * Metoda usuwaj¹ca atraktory, do których dotar³a struktura.
     * @param nodes wêz³y struktury.
     */
    public void RemoveReachedAttractors(HashSet<SCNode> nodes)
    {
        List<Vector3Int> newAttractors = new List<Vector3Int>(attractors);
        foreach (var attractor in attractors)
        {
            foreach (var node in nodes)
            {
                float distance = Vector3Int.Distance(node.position, attractor);

                if (distance < attractorKillRadius)
                {
                    newAttractors.Remove(attractor);
                    if (showAttractors)
                        MainManager.Instance.container[attractor] = new Voxel()
                        {
                            id = 2
                        };
                }
            }
        }
        attractors = new HashSet<Vector3Int>(newAttractors);
    }

    /**
     * Metoda generuj¹ca atraktory.
     */
    public void GenerateAttractors()
    {
        Vector3Int meshBounds;
        AttractorSpawnArea spawnArea = attractorSpawnArea.GetComponent<AttractorSpawnArea>();
        attractorSpawnArea.GetComponent<MeshRenderer>().enabled = false;
        MeshCollider meshCollider = attractorSpawnArea.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            attractorSpawnArea.transform.localScale = Vector3.Scale(attractorSpawnArea.transform.localScale, spawnAreaScale);
            attractorSpawnArea.transform.position += spawnAreaOffset;
            Vector3Int calculatedOffset = new Vector3Int(
                Mathf.RoundToInt(attractorSpawnArea.transform.position.x),
                Mathf.RoundToInt(meshCollider.bounds.center.y - meshCollider.bounds.extents.y),   // center contains local position coords
                Mathf.RoundToInt(attractorSpawnArea.transform.position.z));
            meshBounds = Vector3Int.RoundToInt(meshCollider.bounds.extents);
            spawnArea.Calculate(meshBounds, Vector3Int.RoundToInt(calculatedOffset));
        }
        else
        {
            Debug.LogWarning("No mesh collider attached to the spawn area!");
            meshBounds = new Vector3Int(100, 50, 100);
            spawnArea.Calculate(meshBounds, Vector3Int.zero);
        }
        for (int i = 0; i < attractorsAmount; i++)
        {
            Vector3Int randPos = new Vector3Int(
                Random.Range(spawnArea.xBounds.from, spawnArea.xBounds.to),
                Random.Range(spawnArea.yBounds.from, spawnArea.yBounds.to),
                Random.Range(spawnArea.zBounds.from, spawnArea.zBounds.to)
                );
            if (IsPointInCollider(meshCollider, randPos))
            {
                // calc distance from center
                float distance = Vector3.Distance(randPos, meshCollider.bounds.center);

                // the futher the distance the better chance for adding the attractor
                float randDistance = Vector3.Distance(meshCollider.bounds.center + 
                    Random.insideUnitSphere * meshCollider.bounds.extents.x, meshCollider.bounds.center);

                attractors.Add(randPos);
            }

        }
        attractorSpawnArea.GetComponent<MeshCollider>().enabled = false;
    }

    /**
     * Metoda dodaj¹ca woksele wizualizuj¹ce atraktory do sceny.
     */
    public void ShowAttractors()
    {
        if (showAttractors)
            foreach (var attractor in attractors)
            {
                MainManager.Instance.container[attractor] = new Voxel()
                {
                    id = attractorVoxelID
                };
            }
    }
}
