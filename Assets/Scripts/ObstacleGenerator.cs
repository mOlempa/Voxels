using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    [SerializeField]
    Collider[] obstacleColliders;

    public void GenerateObstacle()
    {
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

        }
    }

    void GenerateVoxels(List<Vector3Int> positions)
    {
        foreach (var pos in positions)
        {
            WorldManager.Instance.container[pos] = new Voxel()
            {
                id = 100,
            };
        }
    }
}
