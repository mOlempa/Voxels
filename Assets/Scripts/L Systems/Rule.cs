using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

[Serializable]
public class Rule
{
    [Tooltip("e.g., 'F' for standard, or 'A(x,y)' for parametric")]
    public string predecessor;

    [Tooltip("e.g. BB_A (BB before and A after the symbol), or leave empty for no context")]
    public string context;

    [Tooltip("If parametric, add comparison rule with parameter name, e.g. 'x < 2'")]
    public string condition = "";

    // A successor dictionary for inspector UI
    [Tooltip("e.g., 'F[+F]' or 'A(x*2, y+1)'")]
    [SerializedDictionary("Successor", "Probability")]
    public SerializedDictionary<string, int> userSuccessors;

    // A list of successors to be referenced every time the rule fires
    private List<Successor> successors = new List<Successor>();

    // Possibly make it a list, might do multiple parameter comparison rules later but idk
    private (int index, Func<float, bool> func) compiledCondition;    // comparison with parameter index

    private Dictionary<int, char[]> namedParams;    // saves the names of parameters used at each occurrence of a param symbol in successor
    private List<(char name, float value)> predecessorParams;   // is for passing the names of predecessor params together with current values


    public void CompileRule()
    {
        successors = new List<Successor>();
        namedParams = new Dictionary<int, char[]>();
        predecessorParams = new List<(char name, float value)>();
        CompilePredecessor();
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

        // Extract the arguments inside the brackets
        string argsContent = str.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');
        for(int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == paramName)
            {
                Debug.Log(str + $" ---> Parameter {paramName} index: " + i);
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

        string argsContent = predecessor.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');
        foreach(string str in tokens)
        {   
            // Add parameter names with default value of 0
            predecessorParams.Add((str[0], 0));
        }

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
            compiledCondition.index = index;

            // Create a Func<float, bool> comparing parameter to read value
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
                    compiledCondition.func = x => x > value;
                    break;
                case "<":
                    compiledCondition.func = x => x < value;
                    break;
                case ">=":
                    compiledCondition.func = x => x >= value;
                    break;
                case "<=":
                    compiledCondition.func = x => x <= value;
                    break;
                case "=":
                    compiledCondition.func = x => x == value;
                    break;
                default:
                case "==":
                    compiledCondition.func = x => x == value;
                    break;
            }

        }
    }

    private void CompileSuccessor(string pattern, int probability)
    {
        bool skipCharacters = false;
        Successor successor = new Successor(probability, predecessor[0]);
        int parametricSymbolOccurrenceIndex = -1;
        foreach (char c in pattern)
        {
            //Debug.Log("CHAR " + c);
            // If a previous loop was processing parameters, skip characters until closing bracket
            if (skipCharacters)
            {
                if (c == ')') skipCharacters = false;
                continue;
            }

            // If the symbol is parametric, evaluate operations given in the successor, like for example F(+1)
            if (c == '(')
            {
                parametricSymbolOccurrenceIndex++;
                // Find opening and first closing bracket
                int openBracket = GetNthIndex(pattern, '(', parametricSymbolOccurrenceIndex + 1);
                int closeBracket = GetNthIndex(pattern, ')', parametricSymbolOccurrenceIndex + 1);

                // Extract the arguments inside the brackets (e.g., "x+1,y*2")
                string argsContent = pattern.Substring(openBracket + 1, closeBracket - openBracket - 1);
                string[] tokens = argsContent.Split(',');
                //paramLength = tokens.Length;
                Symbol s = successor.successorSymbols.Last();
                //Debug.Log($"Removing symbol: {s.name}, with no parameters");
                successor.successorSymbols.RemoveAt(successor.successorSymbols.Count - 1);
                s.parameters = new float[tokens.Length];
                //Debug.Log($"Adding new symbol: {s.name}, with {s.parameters.Length} parameters");
                successor.successorSymbols.Add(s);

                // If the amount of parameters is not correct
                /*if (tokens.Length != symbol.parameters.Length)
                {
                    Debug.LogError($"[Evaluator] Invalid pattern, wrong amount of parameters: {pattern}");
                    return;
                }*/

                CompileOperations(parametricSymbolOccurrenceIndex, tokens, ref successor, out char[] names);
                //SuccessorParser.ParseParamOperations(tokens, ref successor.indexedOperations, out char[] names);

                namedParams.Add(parametricSymbolOccurrenceIndex, names);

                // Start skipping characters until closing bracket
                skipCharacters = true;
                continue;
            }

            // Create the new symbol and save it
            //Debug.Log($"Adding new symbol: {c}, with {paramLength} parameters");
            Symbol symbol = new Symbol(c);
            successor.successorSymbols.Add(symbol);
        }
        // Add the new successor to the list together with its probability
        successors.Add(successor);
        foreach(var p in namedParams)
        {
            Debug.Log(p.Key + " : " + p.Value.Length);
        }
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

    private void CompileOperations(int parametricSymbolOccurrenceIndex, string[] tokens, ref Successor successor, out char[] paramNames)
    {
        // Find the single parameter name (the only alphabetical character in the token)

        //Debug.Log("parametricSymbolOccurrence: " + parametricSymbolOccurrenceIndex);
        successor.indexedOperations.Add(parametricSymbolOccurrenceIndex, new List<Func<float, float>>());
        Debug.Log("successor.indexedOperations.count = " + successor.indexedOperations.Count);
        List<char> names = new List<char>();

        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            Debug.Log("Trimmed token 1: " + trimmed);

            // Find the single parameter name (the only alphabetical character in the token)
            char? paramChar = trimmed.FirstOrDefault(char.IsLetter);
            if (paramChar == null)
            {
                throw new ArgumentException($"Token '{trimmed}' does not contain a valid parameter name.");
            }
            names.Add(paramChar.Value);
            trimmed = token.Trim(paramChar.Value);
            trimmed = trimmed.Trim();
            Debug.Log("Trimmed token 2: " + trimmed);
            Debug.Log("Trimmed length: " + trimmed.Length);

            // Compile relative addition: "+1"
            if (trimmed.StartsWith("+"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(x => x + val);
            }
            // Compile relative subtraction: "-1"
            else if (trimmed.StartsWith("-"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(x => x - val);
            }
            // Compile relative multiplication: "*2"
            else if (trimmed.StartsWith("*"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(x => x * val);
            }
            // Compile relative division: "/2"
            else if (trimmed.StartsWith("/"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(x => x / val);
            }
            // Compile no operation on parameter
            else if (trimmed.StartsWith("=") || trimmed.Length == 0)
            {
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(x => x);
            }
            // Compile absolute constants: "0" or "5.5" (ignores the input parameter)
            else
            {
                float val = float.Parse(trimmed, CultureInfo.InvariantCulture);
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(x => val);
            }
        }
        paramNames = names.ToArray();
        Debug.Log("Successor indexed operations length: " + successor.indexedOperations.Count);
    }


    public List<Symbol> ApplyRule(Symbol symbol)
    {
        //List<Symbol> result = new List<Symbol>();
        // If the first character of the predecessor is not the symbol's character, the rule doesn't apply
        if (predecessor[0] != symbol.character)
        {
            //Debug.Log("The rule does not apply - Rule for " + predecessor);
            return new List<Symbol>() { };
        }

        // If there are parameters
        if (symbol.IsParametric)
        {
            // Assign parameter values to parameter names defined by the predecessor
            for(int i = 0; i < symbol.parameters.Length; i++)
            {
                predecessorParams[i] = (predecessorParams[i].name, symbol.parameters[i]);
            }

            if(compiledCondition.func == null)
            {
                Debug.LogWarning($"No condition set for parametric rule: {predecessor} -> {userSuccessors}");
                // repeat from IF below
                Successor successor = GetWeightedRandomSuccessor();
                List<Symbol> evaluatedSuccessor = successor.ApplyOperations(symbol, namedParams, predecessorParams);
                return evaluatedSuccessor;
            }
            //Debug.Log("Symbol has parameters, returning random parametric successor");
            if (compiledCondition.index > symbol.parameters.Length - 1)
            {
                Debug.LogError($"Wrong rule - {predecessor} has not enough parameters!");
                return new List<Symbol>() { };
            }
            // Get the parameter under index saved with the compiled comparison
            // and compare it to variable saved within comparison to check if the rule applies
            if (compiledCondition.func(symbol.parameters[compiledCondition.index]))
            {
                // Check probabilities - get weighted random
                Successor successor = GetWeightedRandomSuccessor();

                // Apply operations to successor symbols
                List<Symbol> evaluatedSuccessor = successor.ApplyOperations(symbol, namedParams, predecessorParams);

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

