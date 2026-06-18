using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using static Utilities;


[Serializable]
public class Rule
{
    [Tooltip("e.g., 'F' for standard, or 'A(x,y)' for parametric")]
    public string predecessor;

    [Tooltip("e.g. AA_C, AF(>3,0)_C(<5), BB_, _X(=a, >1), or leave empty for no context. ALWAYS give " +
        "parameter comparison if symbol is parametric!")]
    public string userContext = "";

    [Tooltip("If parametric, add comparison rule with parameter name, e.g. 'x < 2'")]
    public string condition = "";

    // A successor dictionary for inspector UI
    [Tooltip("e.g., 'F[+F]' or 'A(x*2, y+1)'")]
    [SerializedDictionary("Successor", "Probability")]
    public SerializedDictionary<string, int> userSuccessors;

    // A list of successors to be referenced every time the rule fires
    private List<Successor> successors = new List<Successor>();

    // Possibly make it a list, might do multiple parameter comparison rules later but idk
    private (int index, Func<float, bool> func) compiledParamCondition;    // comparison with parameter index

    private Context compiledContext;

    private List<(char name, float value)> predecessorParams;   // is for passing the names of predecessor params together with current values

    private const char noName = '#';

    public void CompileRule()
    {
        successors = new List<Successor>();
        predecessorParams = new List<(char name, float value)>();
        CompilePredecessor();

        compiledContext = new Context();
        compiledContext.ReadContext(userContext);

        ReadCondition();

        foreach(var s in userSuccessors)
        {
            CompileSuccessor(s.Key, s.Value);
        }
    }
    

    private void CompilePredecessor()
    {
        int openBracket = predecessor.IndexOf('(');
        int closeBracket = predecessor.IndexOf(')');
        if (openBracket == -1 || closeBracket == -1) return;
        predecessor = Regex.Replace(predecessor, @"\s+", "");

        string argsContent = predecessor.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');
        // Add parameter names with default value of 0
        foreach (string str in tokens)
        {
            predecessorParams.Add((str[0], 0));
        }

    }


    private void ReadCondition()
    {
        if(condition.Length > 0)
        {
            // Remove any white spaces from the rule
            condition = Regex.Replace(condition, @"\s+", "");
            predecessor = Regex.Replace(predecessor, @"\s+", "");

            // Read the first character of condition as the parameter name and find the index of the parameter
            int index = GetDeclaredParamIndex(predecessor, condition[0]);
            if (index == -1) return;

            compiledParamCondition.index = index;

            // Create a Func<float, bool> comparing parameter to read value
            compiledParamCondition.func = EvaluateComparisonLambda(condition);
        }
    }


    private Func<float, bool> EvaluateComparisonLambda(string condition)
    {
        Match match = Regex.Match(condition, @">=|<=|=|>|<|==");
        Match matchNumber = Regex.Match(condition, @"(\d+)");
        if (!match.Success)
        {
            Debug.LogError($"No operator comparing parameter given in the parameter rule string! (predecessor {predecessor})");
        }
        if (!matchNumber.Success)
        {
            Debug.LogError($"No value to compare parameter given in the parameter rule string! (predecessor {predecessor})");
        }

        float.TryParse(matchNumber.Value, out float value);
        switch (match.Value)
        {
            case ">":
                return x => x > value;
            case "<":
                return x => x < value;
            case ">=":
                return x => x >= value;
            case "<=":
                return x => x <= value;
            case "=":
                return x => x == value;
            default:
            case "==":
                return x => x == value;
        }
    }

    private void CompileSuccessor(string pattern, int probability)
    {
        bool skipCharacters = false;
        Successor successor = new Successor(probability, predecessor[0]);
        int parametricSymbolOccurrenceIndex = -1;
        int bracketAmount = 0;
        string symbolParamString = "";

        foreach (char c in pattern)
        {
            Debug.Log("CHAR " + c);
            // If a previous loop was processing parameters, skip characters until closing bracket
            if (skipCharacters)
            {
                if (c == '(')
                {
                    bracketAmount++;
                }
                if (c == ')') {
                    bracketAmount--;
                    if (bracketAmount == 0)  
                    {
                        parametricSymbolOccurrenceIndex++;

                        // Extract the arguments inside the brackets (e.g., "x+1,y*2")
                        string[] tokens = symbolParamString.Split(',');
                        /*Symbol s = successor.successorSymbols.Last();
                        successor.successorSymbols.RemoveAt(successor.successorSymbols.Count - 1);
                        s.parameters = new float[tokens.Length];
                        successor.successorSymbols.Add(s);*/
                        SuccessorParser.ParseParamOperations(tokens, ref successor, parametricSymbolOccurrenceIndex, out char[] names);

                        successor.namedParams.Add(parametricSymbolOccurrenceIndex, names);
                        skipCharacters = false;
                        symbolParamString = "";
                        continue;
                    }
                }
                symbolParamString += c;
                continue;
            }

            // If the symbol is parametric, evaluate operations given in the successor, like for example F(+1)
            if (c == '(')
            {
                bracketAmount++;

                // Start skipping characters until closing bracket
                skipCharacters = true;
                continue;
            }

            // Create the new symbol and save it
            Symbol symbol = new Symbol(c);
            successor.successorSymbols.Add(symbol);
        }
        // Add the new successor to the list together with its probability
        successors.Add(successor);
    }

    public List<Symbol> ApplyRule(Symbol symbol, List<Symbol> currentWord, int symbolIndex)
    {
        // If the first character of the predecessor is not the symbol's character, the rule doesn't apply
        if (!symbol.HasChar(predecessor[0]))
        {
            //Debug.Log("The rule does not apply - Rule for " + predecessor);
            return new List<Symbol>() { };
        }

        if (symbol.IsParametric)    // Assign the parameter values to their names cause context might use them
        {
            // Assign parameter values to parameter names defined by the predecessor
            for (int i = 0; i < symbol.parameters.Length; i++)
            {
                //Debug.Log("Symbol: " + symbol.character);
                //predecessorParams[i] = (predecessorParams[i].name, symbol.parameters[i]);
                //Debug.Log(" predecessorParams.count = " + predecessorParams.Count);

                // If there are no predecessor parameters created yet, add the value with a "no name" name
                if (predecessorParams.Count <= i) predecessorParams.Add((noName, symbol.parameters[i]));    // TODO: What?
                else predecessorParams[i] = (predecessorParams[i].name, symbol.parameters[i]);
            }
        }

        // If there is context condition to be checked
        if (userContext.Length > 0)
        {
            if(!compiledContext.DoesContextApply(currentWord, symbolIndex, predecessorParams))
            {
                return new List<Symbol>();
            }
        }

        // If there are parameters
        if (symbol.IsParametric)
        {
            // If there are no conditions for parameters to check
            if(compiledParamCondition.func == null)
            {
                // Return the successor symbols after applying operations to them
                return ApplySuccessorOperations(symbol);
            }

            if (compiledParamCondition.index > symbol.parameters.Length - 1)
            {
                Debug.LogError($"Wrong rule - {predecessor} has not enough parameters!");
                return new List<Symbol>() { };
            }

            // Get the parameter under index saved with the compiled comparison
            // and compare it to variable saved within comparison to check if the rule applies
            if (compiledParamCondition.func(symbol.parameters[compiledParamCondition.index]))
            {
                // Return the successor (list of symbols)
                return ApplySuccessorOperations(symbol);
            }
            // If the comparison condition does not apply, assume other condition will and return empty list
            else
            {
                return new List<Symbol>();
            }

        }
        // If there are no parameters, return the weighted random successor
        else if (successors.Count > 0)
        {
            //Debug.Log("No parameters, returning random successor");
            return GetWeightedRandomSuccessor().GetSymbolClones();
        }
        // If there is no successor, return a new list with just the symbol
        else
        {
            //Debug.Log("No rules/successors, returning the symbol");
            return new List<Symbol>() { symbol.Clone() };
        }
    }

    private List<Symbol> ApplySuccessorOperations(Symbol currentSymbol)
    {
        // Check probabilities - get weighted random
        Successor successor = GetWeightedRandomSuccessor();

        // Apply operations to successor symbols
        List<Symbol> evaluatedSuccessor = successor.ApplyOperations(currentSymbol, predecessorParams);

        // Return the successor (list of symbols)
        return evaluatedSuccessor;
    }


    private Successor GetWeightedRandomSuccessor()
    {
        int totalSum = userSuccessors.Values.Sum();
        int random = UnityEngine.Random.Range(0, totalSum);
        foreach (var s in successors)
        {
            // If random number is smaller than probability of the successor, return the successor
            if (random <= s.probability)
            {
                return s;
            }
            // Otherwise reduce random value by the probability of the current successor and go to the next one
            random -= s.probability;
        }
        // If for any reason a successor was not chosen before, just return an empty list (no successors)
        Debug.LogWarning("No successors for the rule were chosen!");
        return new Successor();
    }
}

