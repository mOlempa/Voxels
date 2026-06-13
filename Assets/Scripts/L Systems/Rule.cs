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


[Serializable]
public class Rule
{
    [Tooltip("e.g., 'F' for standard, or 'A(x,y)' for parametric")]
    public string predecessor;

    [Tooltip("e.g. BB_A (BB before and A after the symbol), or leave empty for no context")]
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


    private class Context
    {
        public List<ContextSymbol> leftContext;
        public List<ContextSymbol> rightContext;

        public Context()
        {
            leftContext = new List<ContextSymbol>();
            rightContext = new List<ContextSymbol>();
        }

    }

    private class ContextSymbol
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
            Debug.Log("Compiling context parameters for symbol " + character);
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
                Debug.Log("Added a VALUE as parameter comparison");
            }
            else
            {
                // If condition has a parameter name, add it as a char
                comparisonVariables.Add(conditionValue[0]);
                Debug.Log("Added a NAME as parameter comparison");
            }

            switch (match.Value)
            {
                case ">":
                    paramConditions.Add( (x, y) => x > y);
                    break;
                case "<":
                    paramConditions.Add( (x, y) => x < y);
                    break;
                case ">=":
                    paramConditions.Add( (x, y) => x >= y);
                    break;
                case "<=":
                    paramConditions.Add( (x, y) => x <= y);
                    break;
                case "=":
                    paramConditions.Add( (x, y) => x == y);
                    break;
                case "==":
                default:
                    paramConditions.Add( (x, y) => x == y);
                    break;
            }
        }

        public bool CompareVariables(Symbol symbol, List<(char name, float value)> predecessorParams)
        {
            Debug.Log("Checking context parameters for symbol " + character);

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
                        foreach(var param in predecessorParams)
                        {
                            if (param.name == (char)comparisonVariables[n])
                                return paramConditions[n](symbol.parameters[n], param.value);
                        }
                }
            }
            Debug.LogError("Something went wrong in context variable comparisons!");
            return false;
        }

    }

    public void CompileRule()
    {
        successors = new List<Successor>();
        //namedParams = new List<Dictionary<int, char[]>>();
        predecessorParams = new List<(char name, float value)>();
        compiledContext = new Context();
        CompilePredecessor();
        ReadContext();
        ReadCondition();
        foreach(var s in userSuccessors)
        {
            CompileSuccessor(s.Key, s.Value);
        }

        /*foreach (var successor in successors)
        {
            string str = "";
            foreach (var s in successor.symbols)
            {
                str += s.name;
            }
            Debug.Log("Successor " + str + ", probability " + successor.probability);

        }*/

    }


    private int GetParamIndex(string str, string paramName)
    {
        int openBracket = str.IndexOf('(');
        int closeBracket = str.IndexOf(')');
        if (openBracket == -1 || closeBracket == -1) return -1;

        // Extract the arguments inside the brackets
        string argsContent = str.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');
        for(int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == paramName)
            {
                //Debug.Log(str + $" ---> Parameter {paramName} index: " + i);
                return i;
            }
        }

        // In case the parameter name was not found in the string, return -1
        return -1;
    }

    public void CompilePredecessor()
    {
        int openBracket = predecessor.IndexOf('(');
        int closeBracket = predecessor.IndexOf(')');
        if (openBracket == -1 || closeBracket == -1) return;
        predecessor = Regex.Replace(predecessor, @"\s+", "");

        string argsContent = predecessor.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');
        foreach(string str in tokens)
        {
            //Debug.Log("Token: " + str);
            //Debug.Log("<color=lime>Adding parameter named [" + str[0] + "] to processorParams</color>");
            // Add parameter names with default value of 0
            predecessorParams.Add((str[0], 0));
        }

    }

    // Context examples: AA_C, AF(>3,0)_C(<5), BB_, _X(=a, >1)
    public void ReadContext()
    {
        if(userContext.Length == 0)
        {
            return;
        }
        // Remove any white spaces
        userContext = Regex.Replace(userContext, @"\s+", "");

        bool leftContext = true;
        bool skipCharacters = false;
        string condition = "";
        ContextSymbol contextSymbol;
        Debug.Log("Reading context");

        for (int i = 0; i < userContext.Length; i++)
        {
            Debug.Log("CHAR: " + userContext[i]);
            if (skipCharacters)
            {
                if (userContext[i] == ')' || userContext[i] == ',')
                {
                    if(userContext[i] == ')') skipCharacters = false;
                    Debug.Log("Processing string " + condition);
                    if (leftContext)
                    {
                        // Add to the symbol a parameter condition
                        compiledContext.leftContext[compiledContext.leftContext.Count - 1].CompileParamComparisons(condition);
                        condition = "";
                    }
                    else
                    {
                        compiledContext.rightContext[compiledContext.rightContext.Count - 1].CompileParamComparisons(condition);
                        condition = "";
                    }
                }
                else
                {
                    condition += userContext[i];
                }
                continue;
            }
            if (userContext[i] == '_') { leftContext = false; continue; }

            if (userContext[i] == '(') { skipCharacters = true; continue; }

            contextSymbol = new ContextSymbol(userContext[i]);
            if (leftContext)
            {
                compiledContext.leftContext.Add(contextSymbol);
            }
            else
            {
                compiledContext.rightContext.Add(contextSymbol);
            }
        }
        Debug.Log("Finished processing context");

    }

    public void ReadCondition()
    {
        if(condition.Length > 0)
        {
            // Remove any white spaces from the rule
            condition = Regex.Replace(condition, @"\s+", "");
            predecessor = Regex.Replace(predecessor, @"\s+", "");

            // Read the first character as the parameter name and find the index of the parameter
            int index = GetParamIndex(predecessor, condition[0].ToString());
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
        //Debug.Log($"Rule: {parameterRule[0]} {match} {matchNumber}, param index = {index}");

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
            //Debug.Log("CHAR " + c);
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
                        Symbol s = successor.successorSymbols.Last();
                        successor.successorSymbols.RemoveAt(successor.successorSymbols.Count - 1);
                        s.parameters = new float[tokens.Length];
                        successor.successorSymbols.Add(s);

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

    public int GetNthIndex(string s, char t, int n)
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
                predecessorParams[i] = (predecessorParams[i].name, symbol.parameters[i]);
            }
        }

        // If there is context condition to be checked
        if (userContext.Length > 0)
        {
            Debug.Log("Checking context");
            int firstIndex = symbolIndex - compiledContext.leftContext.Count;
            //int lastIndex = symbolIndex + compiledContext.rightContext.Count;
            if (firstIndex >= 0 && symbolIndex + compiledContext.rightContext.Count < currentWord.Count)
            {
                bool contextRuleApplies = true;

                // If there is left context
                if (compiledContext.leftContext.Count != 0) 
                {
                    List<Symbol> beforeSymbol = currentWord.GetRange(firstIndex, compiledContext.leftContext.Count);
                    // Check if rule applies with symbol characters and their potential parameters
                    // For each symbol on the left (from the amount picked earlier)
                    for (int i = 0; i < beforeSymbol.Count; i++)
                    {
                        // If character is the same, check for parameter conditions. Otherwise context rule does not apply
                        if (beforeSymbol[i].HasChar(compiledContext.leftContext[i].character))
                        {
                            if (beforeSymbol[i].IsParametric)
                                contextRuleApplies = compiledContext.leftContext[i].CompareVariables(beforeSymbol[i], predecessorParams);
                        }
                        else
                        {
                            contextRuleApplies = false;
                        }
                    }
                }
                if(compiledContext.rightContext.Count != 0)
                {
                    List<Symbol> afterSymbol = currentWord.GetRange(symbolIndex + 1, compiledContext.rightContext.Count);
                    for (int i = 0; i < afterSymbol.Count; i++)
                    {
                        // If character is the same, check for parameter conditions. Otherwise context rule does not apply
                        if (afterSymbol[i].HasChar(compiledContext.rightContext[i].character))
                        {
                            if(afterSymbol[i].IsParametric)
                                contextRuleApplies = compiledContext.rightContext[i].CompareVariables(afterSymbol[i], predecessorParams);
                        }
                        else
                        {
                            contextRuleApplies = false;
                        }
                    }
                }
                
                if (contextRuleApplies)
                {
                    Debug.Log("<color=lime>Rule applies</color>");
                }
                else
                {
                    Debug.Log("<color=red>Rule does not apply</color>");
                    return new List<Symbol>();
                }
            }
            else
            {
                Debug.Log("<color=red>Rule does not apply - too many symbols in context</color>");
                return new List<Symbol>();
            }
        }

        // If there are parameters
        if (symbol.IsParametric)
        {
            // Assign parameter values to parameter names defined by the predecessor
            /*for(int i = 0; i < symbol.parameters.Length; i++)
            {
                predecessorParams[i] = (predecessorParams[i].name, symbol.parameters[i]);
            }*/ // ALREADY ASSIGNED EARLIER !!!

            if(compiledParamCondition.func == null)
            {
                //Debug.LogWarning($"No condition set for parametric rule: {predecessor} -> {userSuccessors}");
                // repeat from IF below
                Successor successor = GetWeightedRandomSuccessor();
                List<Symbol> evaluatedSuccessor = successor.ApplyOperations(symbol, predecessorParams);
                return evaluatedSuccessor;
            }
            //Debug.Log("Symbol has parameters, returning random parametric successor");
            if (compiledParamCondition.index > symbol.parameters.Length - 1)
            {
                Debug.LogError($"Wrong rule - {predecessor} has not enough parameters!");
                return new List<Symbol>() { };
            }
            // Get the parameter under index saved with the compiled comparison
            // and compare it to variable saved within comparison to check if the rule applies
            if (compiledParamCondition.func(symbol.parameters[compiledParamCondition.index]))
            {
                // Check probabilities - get weighted random
                Successor successor = GetWeightedRandomSuccessor();

                // Apply operations to successor symbols
                List<Symbol> evaluatedSuccessor = successor.ApplyOperations(symbol, predecessorParams);

                // Return the successor (list of symbols)
                return evaluatedSuccessor;
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


    public Successor GetWeightedRandomSuccessor()
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

