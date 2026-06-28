using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialHashGrid<T>
{
    private float cellSize;
    // Using a string or a custom struct (like Vector3Int) for the dictionary key
    private Dictionary<Vector3Int, List<T>> grid = new Dictionary<Vector3Int, List<T>>();

    public SpatialHashGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }

    // Convert world position to grid coordinate
    private Vector3Int GetCellCoords(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public void Add(Vector3 position, T item)
    {
        Vector3Int cellCoords = GetCellCoords(position);

        if (!grid.ContainsKey(cellCoords))
        {
            grid[cellCoords] = new List<T>();
        }
        grid[cellCoords].Add(item);
    }

    // Find all items in the target cell and its 26 neighbors around it
    public List<T> GetNearby(Vector3 position)
    {
        List<T> nearbyItems = new List<T>();
        Vector3Int centerCell = GetCellCoords(position);

        // Loop through the 3x3x3 grid around the center cell
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int neighborCell = centerCell + new Vector3Int(x, y, z);
                    if (grid.TryGetValue(neighborCell, out List<T> itemsInCell))
                    {
                        nearbyItems.AddRange(itemsInCell);
                    }
                }
            }
        }
        return nearbyItems;
    }
}