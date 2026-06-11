using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/*public struct Node
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
}*/

public class LSystemGenerator : MonoBehaviour
{
    [Range(0, 10)]
    public int iterationLimit = 1;
    public Grammar grammar;
    public bool enablePrintDebug = false;

    public List<Symbol> GenerateSentence(string startingWord = null)
    {
        if (grammar == null)
        {
            return new List<Symbol>();
        }

        grammar.CompileGrammar();


        if (startingWord == null) startingWord = grammar.rootSentence;

        List<Symbol> word = grammar.ConvertStringToSymbols(startingWord);
        List<Symbol> nextWord = new List<Symbol>();
        string sentence = "";
        int symbolIndex;
        for (int i = 0; i < iterationLimit; i++)
        {
            printDebug("Iteration index: " + i + ", word: <color=yellow>" + GetSymbolListString(word) + "</color>");
            symbolIndex = 0;
            foreach (Symbol symbol in word)
            {
                List<Symbol> successorSymbolList = new List<Symbol>();
                foreach (Rule rule in grammar.rules)
                {
                    successorSymbolList = new List<Symbol>(rule.ApplyRule(symbol, word, symbolIndex));
                    printDebug("Successor: " + GetSymbolListString(successorSymbolList));    

                    if (successorSymbolList.Count > 0)
                    {
                        nextWord.AddRange(successorSymbolList);
                        break;
                    }
                }

                // If no successor was determined, the symbol is constant
                if(successorSymbolList.Count == 0) nextWord.Add(symbol);
                printDebug("nextWord: " + GetSymbolListString(nextWord));        // ?
                symbolIndex++;
            }
            sentence = "";
            foreach (Symbol symbol in nextWord)
            {
                sentence += symbol.GetSymbolString();
            }
            //print(sentence);
            word = new List<Symbol>(nextWord);
            nextWord.Clear();

        }

        printDebug("Final sentence: <color=yellow>" + sentence + "</color>");

        return word;

        //return GrowRecursive(word);
    }

    private string GetSymbolListString(List<Symbol> list)
    {
        string str = "";
        foreach(Symbol s in list)
        {
            str += s.GetSymbolString();
        }
        return str;
    }




    /*public int maxLength = 5;
    public int minLength = 3;
    public int maxAngle = 40;
    public int minAngle = 15;
    public int maxThickness = 5;
    public Vector3 startingAngles = Vector3.zero;

    public List<Segment> ConvertSentenceToSegments(List<Symbol> sentence)
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

        foreach (var symbol in sentence)
        {
            Node currentNode;
            int randAngle = UnityEngine.Random.Range(minAngle, maxAngle);
            //int randAngle = 30;
            switch (grammar.GetSymbolAction(symbol))
            {
                case Action.PlaceLine:
                    print($"Symbol {symbol.name}, placing line");
                    final += symbol.name;
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
                    print($"Symbol {symbol.name}, rotating right");

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateLeft:
                    print($"Symbol {symbol.name}, rotating left");

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.x -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateForward:
                    print($"Symbol {symbol.name}, rotating forward");

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y += randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.RotateBackward:
                    print($"Symbol {symbol.name}, rotating backward");

                    final += symbol.name;
                    currentNode = stack.Pop();
                    currentNode.anglesDeg.y -= randAngle;
                    stack.Push(currentNode);
                    break;

                case Action.StartBranch:
                    print($"Symbol {symbol.name}, starting branch");

                    //currentThickness = currentThickness > 1 ? currentThickness - 1 : 1;
                    final += $"</color>";
                    final += $"<color=#{WorldManager.Instance.worldColors[stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1].color.ToHexString().TrimEnd("00")}>";
                    final += symbol.name;
                    stack.Push(new Node() { 
                        position = stack.Peek().position, 
                        anglesDeg = stack.Peek().anglesDeg,
                        thickness = stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1});
                    break;

                case Action.EndBranch:
                    print($"Symbol {symbol.name}, ending branch");

                    //currentThickness = currentThickness < maxThickness ? currentThickness + 1 : maxThickness;
                    final += symbol.name;
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
    }*/

    void printDebug(string str)
    {
        if(enablePrintDebug)Debug.Log(str);
    }
}
