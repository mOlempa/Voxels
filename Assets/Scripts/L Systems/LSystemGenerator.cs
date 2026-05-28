using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public struct Node
{
    public Vector3Int position;
    public Vector3 anglesDeg;
    public int thickness;
}

public struct Segment
{
    public Node startPoint, endPoint;
    public int thickness;
    public Vector3Int startPos
    {
        get
        {
            return startPoint.position;
        }
    }
    public Vector3Int endPos
    {
        get
        {
            return endPoint.position;
        }
    }
}

public class LSystemGenerator : MonoBehaviour
{
    [Range(0, 10)]
    public int iterationLimit = 1;
    public Grammar grammar;


    // Generate L-System sentence from given starting word
    public string GenerateSentence(string word = null)
    {
        if (grammar == null)
        {
            return "";
        }

        grammar.UpdateSymbolDictionary();

        if (word == null) word = grammar.rootSentence;
        StringBuilder newWord = new StringBuilder();

        for (int i = 0; i < iterationLimit; i++)
        {
            print("Iteration index: " + i + ", word: " + word);
            newWord.Clear();
            foreach (char c in word)
            {
                Symbol symbol;

                // If the alphabet does not contain the symbol or the character is constant (no rule is applied to it),
                // just rewrite the character as is
                if (!grammar.AlphabetContainsSymbol(c, out symbol) || symbol.isConstant)
                {
                    newWord.Append(c);
                    continue;
                }

                // Apply each rule to the character
                print("Applying rule to " + c);

                //Appy a (random if multiple) rule from the list

                //newWord.Append(symbol.successors[UnityEngine.Random.Range(0, symbol.successors.Length)]);
                
                newWord.Append(GetWeightedRandomSuccessor(symbol));

            }
            word = newWord.ToString();
        }

        print("Final sentence: " + word);

        return word;

        //return GrowRecursive(word);
    }

    public string GetWeightedRandomSuccessor(Symbol symbol)
    {
        int totalSum = symbol.successors.Values.Sum();
        int random = UnityEngine.Random.Range(0, totalSum);
        foreach (var kvp in symbol.successors)
        {
            // If random number is smaller than probability of the successor, return the successor
            if (random <= kvp.Value)
            {
                return kvp.Key;
            }
            // Otherwise reduce random value by the probability of the current successor and go to the next one
            random -= kvp.Value;
        }
        // If for any reason a successor was not chosen before, just return the original symbol character
        return symbol.symbol.ToString();
    }


    public int GetRandomWeightedIndex(int[] weights)
    {
        // Get the total sum of all the weights.
        int weightSum = 0;
        for (int i = 0; i < weights.Length; ++i)
        {
            weightSum += weights[i];
        }

        // Step through all the possibilities, one by one, checking to see if each one is selected.
        int index = 0;
        int lastIndex = weights.Length - 1;
        while (index < lastIndex)
        {
            // Do a probability check with a likelihood of weights[index] / weightSum.
            if (UnityEngine.Random.Range(0, weightSum) < weights[index])
            {
                return index;
            }

            // Remove the last item from the sum of total untested weights and try again.
            weightSum -= weights[index++];
        }

        // No other item was selected, so return very last index.
        return index;
    }



    int lineLength = 4;
    int angle = 30;

    public int maxLength = 5;
    public int minLength = 3;
    public int maxAngle = 40;
    public int minAngle = 15;
    public int maxThickness = 5;
    public Vector3 startingAngles = Vector3.zero;

    public List<Segment> ConvertSentenceToSegments(string sentence)
    {
        List<Segment> segments = new List<Segment>();
        //int currentThickness = maxThickness;

        print($"<color=#FF1100>aaaaa</color>");
        //print($"<color=#{WorldManager.Instance.worldColors[0].color.ToHexString().TrimEnd("00")}>{WorldManager.Instance.worldColors[0].color.ToHexString()}</color>");

        List<Vector3Int> positions = new List<Vector3Int>
        {
            // starting position
            new Vector3Int(0, 0, 0)
        };
        Stack<Node> stack = new Stack<Node>();
        stack.Push(new Node() { position = positions[0], anglesDeg = startingAngles, thickness = maxThickness });
        string final = $"";
        final += $"<color=#{WorldManager.Instance.worldColors[maxThickness].color.ToHexString().TrimEnd("00")}>";

        foreach (var c in sentence)
        {
            Node currentNode;
            int randAngle = UnityEngine.Random.Range(minAngle, maxAngle);
            //int randAngle = 30;
            switch (grammar.GetSymbolAction(c))
            {
                case Action.PlaceLine:
                    final += c;
                    currentNode = stack.Pop();

                    Segment segment = new Segment() { startPoint = currentNode };
                    segment.thickness = currentNode.thickness;

                    int randLength = UnityEngine.Random.Range(minLength, maxLength);

                    currentNode.position = currentNode.position + GetLocalEndpoint(randLength, currentNode.anglesDeg);

                    segment.endPoint = currentNode;
                    segments.Add(segment);

                    positions.Add(currentNode.position);
                    stack.Push(currentNode);
                    break;

                case Action.RotateRight:
                    final += c;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateLeft:
                    final += c;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateForward:
                    final += c;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateBackward:
                    final += c;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.StartBranch:
                    //currentThickness = currentThickness > 1 ? currentThickness - 1 : 1;
                    final += $"</color>";
                    final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1].color.ToHexString().TrimEnd("00")}>";
                    final += c;
                    stack.Push(new Node() { 
                        position = stack.Peek().position, 
                        anglesDeg = stack.Peek().anglesDeg,
                        thickness = stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1});
                    break;

                case Action.EndBranch:
                    //currentThickness = currentThickness < maxThickness ? currentThickness + 1 : maxThickness;
                    final += c;
                    final += $"</color>";
                    final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness].color.ToHexString().TrimEnd("00")}>";

                    stack.Pop();

                    break;

                default:
                    break;
            }
        }
        final += $"</color>";
        print(final);
        /*print("Positions: ");
        foreach (var pos in positions)
        {
            print(pos);
        }*/

        return segments;
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


}
