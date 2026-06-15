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
    EndBranch,
    PlaceLeaf
}


[CreateAssetMenu(menuName = "LSystems/Grammar")]
[ExecuteInEditMode]
public class Grammar : ScriptableObject
{
    [SerializeField]
    public string rootSentence;

    [SerializeField]
    public Symbol[] definedSymbols;

    [SerializeField]
    public Rule[] rules;

    public Dictionary<char, Symbol> symbols = new Dictionary<char, Symbol>();

    public void CompileGrammar()
    {
        UpdateSymbolDictionary();
        foreach(var rule in rules)
        {
            //rule.ReadCondition();
            rule.CompileRule();
        }

        /*foreach (var symbol in alphabet)
        {
            foreach(var par in symbol.parameters)
            {
                foreach (var rule in par.rules)
                {
                    rule.CompileRule(alphabet);
                }
            }
            foreach(var s in symbol.stringSuccessors)
            {
                Debug.Log("Symbol successor detected: " + s.Key);
                symbol.successors.Add(ConvertStringToSymbols(s.Key), s.Value);
            }
        }*/
    }

    public List<Symbol> ConvertStringToSymbols(string str)
    {
        List<Symbol> wordSymbols = new List<Symbol>();

        bool symbolIsParameterized = false;
        string paramStr = "";

        foreach (char c in str)
        {
            // PROCESSING PARAMETERS (if specified by previous loop iteration)
            if (symbolIsParameterized)
            {
                // If the brackets just closed, finish processing parameters
                if (c == ')')
                {
                    // Create a new symbol that is parameterized
                    Symbol newSymbol = new Symbol(wordSymbols.Last().character, GetParamsFromString(paramStr));

                    // Remove symbol that was last added to the list and add the new parameterized one
                    wordSymbols.RemoveAt(wordSymbols.Count - 1);
                    wordSymbols.Add(newSymbol);

                    // End processing parameters' part
                    symbolIsParameterized = false;
                    paramStr = "";
                    continue;
                }

                // Add every character after opening bracket to the string (except the closing bracket)
                if (c != '(') paramStr += c;

                continue;
            }

            IsSymbolDefined(c, out Symbol symbol);

            if (c == '(')
            {
                symbolIsParameterized = true;
                continue;
            }

            wordSymbols.Add(symbol);
        }

        return wordSymbols;
    }

    private float[] GetParamsFromString(string paramStr)
    {
        string numberStr = "";
        List<float> extractedParams = new List<float>();

        foreach (char c in paramStr)
        {
            //print("CHAR " + c);
            if (c == ',')
            {
                float.TryParse(numberStr.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float value);
                extractedParams.Add(value);
                //print("Added " + value + " to extracted params");
                numberStr = "";
                continue;
            }
            numberStr += c;
        }

        float.TryParse(numberStr.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float v);
        extractedParams.Add(v);
        return extractedParams.ToArray();
    }


    // If true, returns the copy of the symbol
    public bool IsSymbolDefined(char c, out Symbol symbol)
    {
        foreach (Symbol s in definedSymbols)
        {
            if (s.HasChar(c))
            {
                symbol = s;
                return true;
            }
        }
        symbol = new Symbol(c);
        return false;
    }

    public void UpdateSymbolDictionary()
    {
        foreach(Symbol s in definedSymbols)
        {
            if (symbols.ContainsKey(s.character))
                symbols[s.character] = s;
            else
                symbols.Add(s.character, s);
            //s.AttachToGrammar(this);
        }
    }

    public Action GetSymbolAction(Symbol symbol)
    {
        if (symbols.ContainsKey(symbol.character))
        {
            return symbols[symbol.character].action;
        }
        else
        {
            return Action.None;
        }
    }

    /*public string GetLogicOperatorSign(LogicOperator op)
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
    }*/

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (definedSymbols != null)
        {
            for (int i = 0; i < definedSymbols.Length; i++)
            {
                definedSymbols[i].name = definedSymbols[i].character.ToString();
                UpdateSymbolDictionary();

            }
        }
    }
#endif

    /*#if UNITY_EDITOR
        private void OnValidate()
        {
            if (alphabet != null)
            {
                for (int i = 0; i < alphabet.Length; i++)
                {
                    alphabet[i].name = alphabet[i].character.ToString();
                    UpdateSymbolDictionary();

                    if (alphabet[i].parameters != null)
                    {
                        foreach (var p in alphabet[i].parameters)
                        {
                            if (p.rules != null)
                            {
                                for (int j = 0; j < p.rules.Count(); j++)
                                {

                                    p.rules[j].name = p.name + " " + GetLogicOperatorSign(p.rules[j].logicOperator) 
                                        + " " + p.rules[j].comparedVariable + "  -->  " + p.rules[j].successorStr;
                                }
                            }
                        }
                    }
                }
            }
        }
    #endif*/
}



