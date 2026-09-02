using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * Klasa zawieraj¹ca dodatkowe metody statyczne u¿ywane w pozosta³ych skryptach.
 */
public class Utilities
{
    /**
     * Metoda zwracaj¹ca indeks parametru z poprzednika wprowadzonego przez u¿ytkownika na podstawie nazwy parametru.
     * Przyk³adowo dla "F(x,y)": nazwa "x" zwraca 0; nazwa "y" zwraca 1; nazwa "a" zwraca -1.
     * @param str poprzednik zadeklarowany przez u¿ytkownika w postaci ci¹gu znaków.
     * @param paramName nazwa parametru.
     * @return int indeks parametru.
     */
    public static int GetDeclaredParamIndex(string str, char paramName)
    {
        int openBracket = str.IndexOf('(');
        int closeBracket = str.IndexOf(')');
        if (openBracket == -1 || closeBracket == -1) return -1;

        // Extract the arguments inside the brackets
        string argsContent = str.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');

        for (int i = 0; i < tokens.Length; i++)
        {
            // If there are more than one characters given as one parameter, log a warning
            if (tokens[i].Length > 1)
            {
                Debug.LogWarning($"Wrong parameter name {tokens[i]} - only first letter of parameter name will be read.");
            }
            // The condition below assumes the string token has only one character as a parameter name
            if (tokens[i][0] == paramName) return i;
        }

        // In case the parameter name was not found in the string, return -1
        return -1;
    }

    /**
     * Metoda obliczaj¹ca lokalny punkt koñcz¹cy segment jako przesuniêcie od punktu startowego.
     * @param length d³ugoœæ segmentu pomiêdzy punktami.
     * @param eulerAngles k¹t obrotu zamierzonego segmentu.
     * @return Vector3Int pozycja punktu koñcz¹cego segment w odniesieniu do punktu startowego segmentu.
     */
    public static Vector3Int GetLocalEndpoint(float length, Vector3 eulerAngles)
    {
        Quaternion rotation = Quaternion.Euler(eulerAngles);

        // Multiplying the rotation by forward vector scaled by length
        Vector3 floatingPointTarget = rotation * Vector3.forward * length;

        return Vector3Int.RoundToInt(floatingPointTarget);
    }

    /**
     * Metoda obliczaj¹ca uœredniony znormalizowany wektor kierunku.
     * @param startPosition pozycja wêz³a startowego.
     * @param endpointPositions pozycja wêz³ów koñcz¹cych w kierunku pojedynczych atraktorów.
     * @return Vector3 znormalizowany uœredniony wektor kierunku.
     */
    public static Vector3 GetAveragedNormalizedDirectionVector(Vector3Int startPosition, List<Vector3Int> endpointPositions)
    {
        Vector3 vectorSum = new Vector3();
        for (int i = 0; i < endpointPositions.Count; i++)
        {
            //positions[i] -= currentPos;
            vectorSum += Vector3.Normalize(endpointPositions[i] - startPosition);
        }
        return Vector3.Normalize(vectorSum / endpointPositions.Count);
    }

    /**
     * Metoda generuj¹ca listê pozycji wokseli pomiêdzy dwoma punktami w przestrzeni na podstawie
     * trójwymiarowej wersji algorytmu Bresenhama.
     * @param A pozycja pocz¹tkowa
     * @param B pozycja koñcowa
     * @return List lista pozycji wokseli dla segmentu miêdzy podanymi punktami.
     */
    public static List<Vector3Int> GenerateLine(Vector3Int A, Vector3Int B)
    {
        List<Vector3Int> points = new List<Vector3Int>
        {
            A
        };

        Vector3Int d = new Vector3Int(Mathf.Abs(B.x - A.x), Mathf.Abs(B.y - A.y), Mathf.Abs(B.z - A.z));
        Vector3Int step = new Vector3Int(B.x > A.x ? 1 : -1, B.y > A.y ? 1 : -1, B.z > A.z ? 1 : -1);

        if (d.x >= d.y && d.x >= d.z)
        {
            int p1 = 2 * d.y - d.x;
            int p2 = 2 * d.z - d.x;
            while (A.x != B.x)
            {
                A.x += step.x;
                if (p1 >= 0)
                {
                    A.y += step.y;
                    p1 -= 2 * d.x;
                }
                if (p2 >= 0)
                {
                    A.z += step.z;
                    p2 -= 2 * d.x;
                }
                p1 += 2 * d.y;
                p2 += 2 * d.z;
                points.Add(A);
            }
        }
        else if (d.y >= d.x && d.y >= d.z)
        {
            int p1 = 2 * d.x - d.y;
            int p2 = 2 * d.z - d.y;
            while (A.y != B.y)
            {
                A.y += step.y;
                if (p1 >= 0)
                {
                    A.x += step.x;
                    p1 -= 2 * d.y;
                }
                if (p2 >= 0)
                {
                    A.z += step.z;
                    p2 -= 2 * d.y;
                }
                p1 += 2 * d.x;
                p2 += 2 * d.z;
                points.Add(A);
            }
        }
        else
        {
            int p1 = 2 * d.y - d.z;
            int p2 = 2 * d.x - d.z;
            while (A.z != B.z)
            {
                A.z += step.z;
                if (p1 >= 0)
                {
                    A.y += step.y;
                    p1 -= 2 * d.z;
                }
                if (p2 >= 0)
                {
                    A.x += step.x;
                    p2 -= 2 * d.z;
                }
                p1 += 2 * d.y;
                p2 += 2 * d.x;
                points.Add(A);
            }
        }


        return points;
    }

    /**
     * Metoda generuj¹ca pozycje wokseli wokó³ segmentu na podstawie jego gruboœci.
     * @param A pocz¹tkowa pozycja segmentu.
     * @param B koñcowa pozycja segmentu.
     * @param radius promieñ po¿¹danego przekroju segmentu.
     * @return List lsita pozycji wokseli powoduj¹cych zgrubienie segmentu.
     */
    public static List<Vector3Int> GenerateThickLine(Vector3Int A, Vector3Int B, int radius)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(A, B);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points

        int radiusSquared = radius * radius;

        // Apply a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        // Check if this local offset is within the sphere's radius
                        if (x * x + y * y + z * z <= radiusSquared)
                        {
                            thickLine.Add(new Vector3Int(point.x + x, point.y + y, point.z + z));
                        }
                    }
                }
            }
        }

        return thickLine.ToList();
    }

    /**
     * Metoda zwracaj¹ca informacjê o tym, czy punkt znajduje siê w fizycznej siatce obiektu.
     * @param other obiekt typu Collider, w którym badana jest obecnoœæ punktu.
     * @param point badany punkt w przestrzeni.
     * @return bool zwracana informacja o obecnoœci punktu w siatce.
     */
    public static bool IsPointInCollider(Collider other, Vector3 point)
    {
        Vector3 direction = other.bounds.center - point;
        RaycastHit[] hits = Physics.RaycastAll(point, direction);

        foreach (RaycastHit hit in hits)
        {
            // If collider was hit, the point is outside of the mesh colldier
            if (hit.collider == other)
            {
                return false;
            }
        }

        // No hits means the point is inside it
        return true;
    }

    /**
     * Metoda zwracaj¹ca losowy kierunek na powierzchni "sto¿ka" zdefiniowanego przez k¹t obrotu od
     * oryginalnego wektora kierunku segmentu.
     * @param originalDirection oryginalny kierunek segmentu.
     * @param angleDegrees k¹t, o który mo¿liwy jest obrót segmentu.
     * @return Vector3 wylosowany kierunek.
     */
    public static Vector3 GetRandomRotatedDirection(Vector3 originalDirection, float angleDegrees)
    {
        // Normalize the original direction to keep calculations accurate
        originalDirection.Normalize();

        // Find a perpendicular vector to act as a baseline rotation axis
        Vector3 perpendicularAxis = Vector3.Cross(originalDirection, Vector3.up);
        
        // If originalDirection points straight up or down, the cross product returns zero
        if (perpendicularAxis.sqrMagnitude < 0.001f)
        {
            perpendicularAxis = Vector3.Cross(originalDirection, Vector3.forward);
        }
        perpendicularAxis.Normalize();

        // Tilt the vector away from the center by the exact angle
        Vector3 tiltedVector = Quaternion.AngleAxis(angleDegrees, perpendicularAxis) * originalDirection;

        // Spin the tilted vector around the original direction axis by a random 360-degree angle
        float randomRoll = Random.Range(0f, 360f);
        Vector3 finalDirection = Quaternion.AngleAxis(randomRoll, originalDirection) * tiltedVector;

        return finalDirection;
    }

    /**
     * Metoda zwracaj¹ca wektor kierunku na podstawie typu GrowthBiasType okreœlaj¹cego preferowany kierunek rozrostu.
     * @param type typ preferowanego kierunku rozrostu.
     * @return Vector3 zwracany kierunek.
     */
    public static Vector3 GetDirection(GrowthBiasType type)
    {
        switch (type)
        {
            default:
            case GrowthBiasType.None:
                return Vector3.zero;
            case GrowthBiasType.Up:
                return Vector3.up;
            case GrowthBiasType.Down:
                return Vector3.down;
        }
    }


}
