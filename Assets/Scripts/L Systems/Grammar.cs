using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Action { 
    None,
    PlaceLine,
    // relative rotation
    RotateLeft, 
    RotateRight,
    RotateForward,
    RotateBackward,
    StartBranch,
    EndBranch
}


[CreateAssetMenu(menuName = "LSystems/Grammar")]
[ExecuteInEditMode]
public class Grammar : ScriptableObject
{
    [SerializeField]
    public string rootSentence;

    [SerializeField]
    public Symbol[] alphabet;

    public Dictionary<char, Symbol> symbols = new Dictionary<char, Symbol>();

    /*public char[] GetSymbols()
    {
        //string[] letters = new string[alphabet.Length];
        List<char> letters = new List<char>();
        foreach(Symbol s in alphabet)
        {
            // get non-repeated symbols
            if (!letters.Contains(s.symbol))
                letters.Add(s.symbol);
        }
        return letters.ToArray();
    }*/

    /*public bool IsSymbolConstant(char c)
    {
        foreach(Symbol s in alphabet)
        {
            if(s.symbol == c)
            {
                if (s.isConstant) return true;
                else return false;
            }
        }
        // if symbol not found in the alphabet, assume it as constant
        return true;
    }*/

    public bool AlphabetContainsSymbol(char c, out Symbol symbol)
    {
        foreach (Symbol s in alphabet)
        {
            if (s.symbol == c)
            {
                symbol = s;
                return true;
            }
        }
        symbol = new Symbol();
        return false;
    }

    public void UpdateSymbolDictionary()
    {
        foreach(var s in alphabet)
        {
            if (symbols.ContainsKey(s.symbol))
                symbols[s.symbol] = s;
            else
                symbols.Add(s.symbol, s);
        }
    }

    public Action GetSymbolAction(char c)
    {
        if (symbols.ContainsKey(c))
        {
            return symbols[c].action;
        }
        else
        {
            return Action.None;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (alphabet != null)
        {
            for (int i = 0; i < alphabet.Length; i++)
            {
                alphabet[i].name = alphabet[i].symbol.ToString();
                UpdateSymbolDictionary();
            }
        }
    }
#endif
}

[Serializable]
public struct Symbol
{
    [HideInInspector] public string name;

    [SerializeField] public char symbol;
    [SerializeField] public Action action;
    //[SerializeField] public string[] successors;   // if results length is 0, the symbol is constant
    [SerializedDictionary("Successor", "Probability")]
    public SerializedDictionary<string, int> successors; // if results length is 0, the symbol is constant

    public bool isConstant
    {
        get
        {
            return (successors == null ? true : successors.Count() == 0);
        }
    }
}
