using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;

public class Context
{
    public List<ContextSymbol> leftContext;
    public List<ContextSymbol> rightContext;

    public Context()
    {
        leftContext = new List<ContextSymbol>();
        rightContext = new List<ContextSymbol>();
    }

    // Context examples: AA_C, AF(>3,0)_C(<5), BB_, _X(=a, >1)
    public void ReadContext(string userContext)
    {
        if (userContext.Length == 0)
        {
            return;
        }
        // Remove any white spaces
        userContext = Regex.Replace(userContext, @"\s+", "");

        bool isLeftContext = true;
        bool skipCharacters = false;
        string condition = "";
        ContextSymbol contextSymbol;
        //Debug.Log("Reading context");

        for (int i = 0; i < userContext.Length; i++)
        {
            //Debug.Log("CHAR: " + userContext[i]);
            if (skipCharacters)
            {
                if (userContext[i] == ')' || userContext[i] == ',')
                {
                    if (userContext[i] == ')') skipCharacters = false;
                    //Debug.Log("Processing string " + condition);
                    if (isLeftContext)
                    {
                        // Add to the symbol a parameter condition
                        leftContext[leftContext.Count - 1].CompileParamComparisons(condition);
                        condition = "";
                    }
                    else
                    {
                        rightContext[rightContext.Count - 1].CompileParamComparisons(condition);
                        condition = "";
                    }
                }
                else
                {
                    condition += userContext[i];
                }
                continue;
            }
            if (userContext[i] == '_') { isLeftContext = false; continue; }

            if (userContext[i] == '(') { skipCharacters = true; continue; }

            contextSymbol = new ContextSymbol(userContext[i]);
            if (isLeftContext)
            {
                leftContext.Add(contextSymbol);
            }
            else
            {
                rightContext.Add(contextSymbol);
            }
        }
    }

    public bool DoesContextApply(List<Symbol> currentWord, int symbolIndex, List<(char name, float value)> predecessorParams)
    {
        int firstIndex = symbolIndex - leftContext.Count;

        if (firstIndex >= 0 && symbolIndex + rightContext.Count < currentWord.Count)
        {
            bool contextRuleApplies = true;

            // If there is left context
            if (leftContext.Count != 0)
            {
                //Debug.Log("Detected left context");
                List<Symbol> beforeSymbols = currentWord.GetRange(firstIndex, leftContext.Count);
                //Debug.Log("beforeSymbols: " + Symbol.GetSymbolListString(beforeSymbols));

                // Check if rule applies with symbol characters and their potential parameters
                // For each symbol on the left (from the amount picked earlier)
                for (int i = 0; i < beforeSymbols.Count; i++)
                {
                    // If character is the same, check for parameter conditions. Otherwise context rule does not apply
                    if (beforeSymbols[i].HasChar(leftContext[i].character))
                    {
                        if (beforeSymbols[i].IsParametric)
                            contextRuleApplies = leftContext[i].CompareVariables(beforeSymbols[i], predecessorParams);
                    }
                    else
                    {
                        contextRuleApplies = false;
                    }
                }
            }
            if (rightContext.Count != 0)
            {
                List<Symbol> afterSymbols = currentWord.GetRange(symbolIndex + 1, rightContext.Count);
                for (int i = 0; i < afterSymbols.Count; i++)
                {
                    // If character is the same, check for parameter conditions. Otherwise context rule does not apply
                    if (afterSymbols[i].HasChar(rightContext[i].character))
                    {
                        if (afterSymbols[i].IsParametric)
                            contextRuleApplies = rightContext[i].CompareVariables(afterSymbols[i], predecessorParams);
                    }
                    else
                    {
                        contextRuleApplies = false;
                    }
                }
            }

            if (contextRuleApplies)
            {
                //Debug.Log("<color=lime>Rule applies</color>");
                return true;
            }
            else
            {
                //Debug.Log("<color=red>Rule does not apply</color>");
                return false;
            }
        }
        else
        {
            //Debug.Log("<color=red>Rule does not apply</color>");
            return false;
        }
        
    }

}

public class ContextSymbol
{
    public char character;
    public List<Func<float, float, bool>> paramConditions;
    public List<object> comparisonVariables;

    public ContextSymbol(char c)
    {
        character = c;
        paramConditions = new List<Func<float, float, bool>>();
        comparisonVariables = new List<object>();
    }

    public ContextSymbol(char c, List<Func<float, float, bool>> conditions)
    {
        character = c;
        paramConditions = conditions;
        comparisonVariables = new List<object>();
    }

    public void CompileParamComparisons(string condition)
    {
        //Debug.Log("Compiling context parameters for symbol " + character);
        Match match = Regex.Match(condition, @">=|<=|=|>|<|==");
        if (!match.Success)
        {
            Debug.LogError($"No operator comparing parameter given in the parameter rule string!");
        }
        string conditionValue = condition.Split(match.Value).Last();
        if (conditionValue.Length > 1)
        {
            Debug.LogError($"Wrong format! Only one (1) character allowed after comparison sign!");
        }

        Match matchNumber = Regex.Match(conditionValue, @"(\d+)");
        if (matchNumber.Success)
        {
            // If condition has a number value, add it as a float
            float.TryParse(matchNumber.Value, out float value);
            comparisonVariables.Add(value);
            //Debug.Log("Added a VALUE as parameter comparison");
        }
        else
        {
            // If condition has a parameter name, add it as a char
            comparisonVariables.Add(conditionValue[0]);
            //Debug.Log("Added a NAME as parameter comparison");
        }

        switch (match.Value)
        {
            case ">":
                paramConditions.Add((x, y) => x > y);
                break;
            case "<":
                paramConditions.Add((x, y) => x < y);
                break;
            case ">=":
                paramConditions.Add((x, y) => x >= y);
                break;
            case "<=":
                paramConditions.Add((x, y) => x <= y);
                break;
            case "=":
                paramConditions.Add((x, y) => x == y);
                break;
            case "==":
            default:
                paramConditions.Add((x, y) => x == y);
                break;
        }
    }
    

    public bool CompareVariables(Symbol symbol, List<(char name, float value)> predecessorParams)
    {
        //Debug.Log("Checking context parameters for symbol " + character);

        // For each parameter of the symbol
        for (int n = 0; n < symbol.parameters.Length; n++)
        {
            // If there are any parameter comparisons in the condition (symbol could be parametric but condition ignores it)
            if (comparisonVariables.Count != 0)
            {
                // If the variable we are comparing param to is a float value, just compare it
                if (comparisonVariables[n].GetType() == typeof(float))
                    return paramConditions[n](symbol.parameters[n], (float)comparisonVariables[n]);

                // If the variable we are comparing param to is a char name of a param, get the current value
                if (comparisonVariables[n].GetType() == typeof(char))
                    foreach (var param in predecessorParams)
                    {
                        if (param.name == (char)comparisonVariables[n])
                            return paramConditions[n](symbol.parameters[n], param.value);
                    }
            }
        }
        Debug.LogWarning("Something went wrong in context variable comparisons!");
        return false;
    }

}
