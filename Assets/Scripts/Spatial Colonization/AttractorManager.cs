using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utilities;

public class AttractorManager : MonoBehaviour
{
    public bool showAttractors = true;

    [Highlight(1, 0.8f, 0)]
    [Range(20, 5000)]
    public int attractorsAmount = 100;

    public GameObject attractorSpawnArea;

    public Vector3 spawnAreaScale = Vector3.one;

    public Vector3Int spawnAreaOffset = Vector3Int.zero;

    [Highlight(0.5f, 0.9f, 0.5f)]
    [Range(1, 100)]
    public int maxDistance = 10;
    [Highlight(0.5f, 0.9f, 0.5f)]
    [Range(1, 100)]
    public int minDistance = 2;


    [Highlight(1, 0, 0.3f)]
    [Range(1, 100)]
    public int attractorKillRadius = 2;

    public HashSet<Vector3Int> attractors = new HashSet<Vector3Int>();

    byte attractorVoxelID = 6;

    public void RemoveReachedAttractors(HashSet<SCNode> nodes)
    {
        List<Vector3Int> newAttractors = new List<Vector3Int>(attractors);

        foreach (var attractor in attractors)
        {
            foreach (var node in nodes)
            {
                float distance = Vector3Int.Distance(node.position, attractor);
                //if (distance < 5)
                //   print($"Distance <color=yellow>{node.position}</color> --> <color=lime>{attractor}</color> = {distance}");

                if (distance < attractorKillRadius)
                {
                    newAttractors.Remove(attractor);
                    //print("<color=red>Removed attractor at " + attractor + "</color>");
                    if (showAttractors)
                        WorldManager.Instance.container[attractor] = new Voxel()
                        {
                            //id = 1
                            id = 2
                        };
                }
            }
        }
        attractors = new HashSet<Vector3Int>(newAttractors);
    }

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
            /*Vector3Int randPos = new Vector3Int(
                Random.Range(-meshBounds.x, meshBounds.x),
                Random.Range(0, meshBounds.y*2),
                Random.Range(-meshBounds.z, meshBounds.z)
                );
            randPos += spawnAreaOffset;*/
            if (IsPointInCollider(meshCollider, randPos))
                attractors.Add(randPos);

        }
        attractorSpawnArea.GetComponent<MeshCollider>().enabled = false;
    }


    public void ShowAttractors()
    {
        if (showAttractors)
            foreach (var attractor in attractors)
            {
                WorldManager.Instance.container[attractor] = new Voxel()
                {
                    id = attractorVoxelID
                };
            }
    }
}
