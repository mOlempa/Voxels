using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Klasa reprezentuj¹ca przestrzenn¹ siatkê haszuj¹c¹ w celach przyspieszenia przeszukiwania 
 * przestrzeni w algorytmie kolonizacji przestrzeni.
 */
public class SpatialHashGrid<T>
{
    /**
     * Zmienna typu float definiuj¹ca rozmiar szeœcianu komórki siatki przestrzennej
     */
    private float cellSize;

    /**
     * Struktura danych typu Dictionary przechowuj¹ca pozycje komórek siatki haszuj¹cej i zawarte w nich dane.
     */
    private Dictionary<Vector3Int, List<T>> grid = new Dictionary<Vector3Int, List<T>>();

    /**
     * Konstruktor klasy siatki haszuj¹cej.
     * @param cellsize rozmiar komórki siatki
     */
    public SpatialHashGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }

    /**
     * Metoda zwracaj¹ca pozycjê komórki siatki haszuj¹cej na podstawie pozycji punktu w przestrzeni.
     * @param position pozycja punktu w przestrzeni.
     * @return Vector3Int zwrócona pozycja komórki siatki, w której znajduje siê punkt.
     */
    private Vector3Int GetCellCoords(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    /**
     * Metoda dodaj¹ca dany obiekt do przestrzennej siatki haszuj¹cej na podstawie ich pozycji.
     * @param position pozycja obiektu.
     * @param item obiekt do dodania do siatki.
     */
    public void Add(Vector3 position, T item)
    {
        Vector3Int cellCoords = GetCellCoords(position);

        if (!grid.ContainsKey(cellCoords))
        {
            grid[cellCoords] = new List<T>();
        }
        grid[cellCoords].Add(item);
    }

    /**
     * Metoda zwracaj¹ca obiekty znajduj¹ce siê w 26 s¹siednich komórkach siatki do komórki z danym punktem.
     * @param position pozycja punktu odniesienia.
     * @return List lista obiektów, które siatka przechowuje w komórkach s¹siednich do komórki z punktem odniesienia.
     */
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