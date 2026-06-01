using AYellowpaper.SerializedCollections;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting.FullSerializer;
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

    public void CompileRules()
    {
        foreach (var symbol in alphabet)
        {
            foreach(var par in symbol.parameters.Values)
            {
                foreach (var rule in par.rules)
                {
                    rule.CompileRule();
                }
            }
        }
    }

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

    public string GetLogicOperatorSign(LogicOperator op)
    {
        switch (op)
        {
            case LogicOperator.EqualTo:
                return "=";
            case LogicOperator.BiggerThan:
                return ">";
            case LogicOperator.LessThan:
                return "<";
            case LogicOperator.BiggerOrEqualTo:
                return ">=";
            case LogicOperator.LessOrEqualTo:
                return "<=";
            default:
                return "=";
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

                if (alphabet[i].parameters != null)
                {
                    foreach (var p in alphabet[i].parameters)
                    {
                        if (p.Value.rules != null)
                        {
                            for (int j = 0; j < p.Value.rules.Count(); j++)
                            {

                                p.Value.rules[j].name = p.Key + " " + GetLogicOperatorSign(p.Value.rules[j].logicOperator) 
                                    + " " + p.Value.rules[j].comparedVariable + "  -->  " + p.Value.rules[j].successor;
                            }
                        }
                    }
                }
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
    //[SerializeField] public Parameter[] parameters;
    [SerializedDictionary("Successor", "Probability")]
    public SerializedDictionary<string, int> successors;

    //[SerializeField] public string[] successors;   // if results length is 0, the symbol is constant
    [SerializedDictionary("Name", "Properties")]
    public SerializedDictionary<ParamName, Parameter> parameters;

    public bool isParametrized
    {
        get
        {
            if (parameters == null || parameters.Count() == 0)
                return false;
            else 
                return true;
        }
    }

    public bool isConstant
    {
        get
        {
            if (!isParametrized && (successors == null || successors.Count() == 0))
                return true;
            else
                return false;
        }
    }

}


