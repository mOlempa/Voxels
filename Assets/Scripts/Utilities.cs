using System.Collections;
using System.Collections.Generic;
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
}
