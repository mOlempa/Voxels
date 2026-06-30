using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Utilities
{
    public static int GetNthIndex(string s, char t, int n)
    {
        int count = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == t)
            {
                count++;
                if (count == n)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    // Returns the parameter index from user-declared predecessor, e.g. from string "F(x,y)":
    // for name "x" returns 0,
    // for name "y" returns 1,
    // for name "a" returns -1
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

    public static Vector3Int GetLocalEndpoint(float length, Vector3 eulerAngles)
    {
        // Converting the Euler angles into a rotation Quaternion
        Quaternion rotation = Quaternion.Euler(eulerAngles);

        // Multiplying the rotation by Unity's forward vector (0, 0, 1) scaled by length
        // (in Unity, multiplying a Quaternion by a Vector3 rotates that vector)
        Vector3 floatingPointTarget = rotation * Vector3.forward * length;

        // Converting the floating-point position to integer voxel coordinates
        return Vector3Int.RoundToInt(floatingPointTarget);
    }

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

    // Generating a line of voxels (positions) between two points based on Bresenham 3D algorithm
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

    public static List<Vector3Int> GenerateThickLine(Vector3Int A, Vector3Int B, int radius)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(A, B);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points

        int radiusSquared = radius * radius;

        // Applying a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        // Check if this local offset is within the sphere's radius
                        // (doing x*x + y*y + z*z is much faster than Vector3.Distance)
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

    public static bool IsPointInCollider(MeshCollider other, Vector3 point)
    {
        Vector3 direction = other.bounds.center - point;
        RaycastHit[] hits = Physics.RaycastAll(point, direction);

        foreach (RaycastHit hit in hits)
        {
            // IF collider was hit, the point is outside of the mesh colldier
            if (hit.collider == other)
            {
                return false;
            }
        }

        // No hits means the point is inside it
        return true;
    }

    public static Vector3 GetRandomRotatedDirection(Vector3 originalDirection, float angleDegrees)
    {
        // 1. Normalize the original direction to keep calculations accurate
        originalDirection.Normalize();

        // 2. Find a perpendicular vector to act as a baseline rotation axis
        Vector3 perpendicularAxis = Vector3.Cross(originalDirection, Vector3.up);
        
        // Edge Case: If originalDirection points straight up or down, the cross product fails (returns zero).
        // If that happens, we use Vector3.forward instead to establish a perpendicular axis.
        if (perpendicularAxis.sqrMagnitude < 0.001f)
        {
            perpendicularAxis = Vector3.Cross(originalDirection, Vector3.forward);
        }
        perpendicularAxis.Normalize();

        // 3. Tilt the vector away from the center by the exact angle
        Vector3 tiltedVector = Quaternion.AngleAxis(angleDegrees, perpendicularAxis) * originalDirection;

        // 4. Spin the tilted vector around the original direction axis by a random 360-degree angle
        float randomRoll = Random.Range(0f, 360f);
        Vector3 finalDirection = Quaternion.AngleAxis(randomRoll, originalDirection) * tiltedVector;

        return finalDirection;
    }
}
