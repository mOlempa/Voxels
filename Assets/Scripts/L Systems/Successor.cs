using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Linq;

public struct Successor
{
    public List<Symbol> successorSymbols;
    public int probability;
    // A list cuz there could be multiple parameters, inside a list cuz there could be multiple param symbols in successor
    //public List<List<Func<float, float>>> indexedOperations;

    // A dictionary of parameter name
    // for each parameter there is a list of operations (so they are indexed)
    // ----for example for F(x+1,y)A(x-10) the dictionary will be {x: [+1, -10], y: [=]}---
    // for example for F(x+1,y)BA(x-10) the dictionary will be {0: [+1, =], 1: [-10]}
    public Dictionary<int, List<Func<float, float>>> indexedOperations;
    public Dictionary<int, char[]> namedParams;

    [HideInInspector] public char predecessorSymbolChar;

    public Successor(int probability, char symbolChar)
    {
        successorSymbols = new List<Symbol>();
        indexedOperations = new Dictionary<int, List<Func<float, float>>>();
        //indexedOperations = new List<List<Func<float, float>>>();
        this.probability = probability;
        predecessorSymbolChar = symbolChar;
        namedParams = new Dictionary<int, char[]>();
    }

    public List<Symbol> GetSymbolClones()
    {
        List<Symbol> result = new List<Symbol>();
        foreach (Symbol symbol in successorSymbols) result.Add(symbol.Clone());
        return result;
    }

    public List<Symbol> ApplyOperations(Symbol currentSymbol,
        List<(char name, float value)> predecessorParams)  // gives us current value of the named parameter
    {
        int parametricSymbolOccurrenceIndex = -1;
        List<Symbol> symbolList = new List<Symbol>();

        // Go through each symbol in the successor
        foreach (Symbol symbol in successorSymbols)
        {
            // If symbol is prepared for parameters
            if (symbol.parameters != null)
            {
                parametricSymbolOccurrenceIndex++;
                // go through each saved parameter from the successor and apply equivalent operation
                // (they are saved the same time and list lengths should be the same

                // find the names of the parameter for this symbol and their indexes
                char[] names = namedParams[parametricSymbolOccurrenceIndex];

                // find the value from the current parameter value and execute operations on it
                for (int i = 0; i < names.Length; i++)
                {
                    (char name, float value) p = predecessorParams.Find(x => x.name == names[i]);
                    symbol.parameters[i] = indexedOperations[parametricSymbolOccurrenceIndex][i](p.value);
                    //Debug.Log($"Executing: {p.name} {p.value} [operation] = {symbol.parameters[i]}");
                }
            }
            symbolList.Add(symbol.Clone());
        }

        //Debug.Log("Evaluated successor: " + GetSymbolListString(symbolList));

        return new List<Symbol>(symbolList);
    }



    private string GetSymbolListString(List<Symbol> list)
    {
        string str = "";
        foreach (Symbol s in list)
        {
            str += s.GetSymbolString();
        }
        return str;
    }

}