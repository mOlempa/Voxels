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

/**
 * Klasa regu³ produkcyjnych L-systemów.
 */
[Serializable]
public class Rule
{
    /**
     * Zmienna typu string okreœlaj¹ca poprzednika dla regu³y, dostêpna w inspektorze.
     */
    [Tooltip("e.g., 'F' for standard, or 'A(x,y)' for parametric")]
    public string predecessor;

    /**
     * Zmienna typu string okreœlaj¹ca kontekst dla regu³y, dostêpna w inspektorze.
     */
    [Tooltip("e.g. AA_C, AF(>3,0)_C(<5), BB_, _X(=a, >1), or leave empty for no context. ALWAYS give " +
        "parameter comparison if symbol is parametric!")]
    public string userContext = "";

    /**
     * Zmienna typu string okreœlaj¹ca warunek dla regu³y, dostêpna w inspektorze.
     */
    [Tooltip("If parametric, add comparison rule with parameter name, e.g. 'x < 2'")]
    public string condition = "";

    /**
     * Struktura danych typu SerializedDictionary okreœlaj¹ca nastêpców dla regu³y, dostêpna w inspektorze.
     */
    [Tooltip("e.g., 'F[+F]' or 'A(x*2, y+1)'")]
    [SerializedDictionary("Successor", "Probability")]
    public SerializedDictionary<string, int> userSuccessors;

    /**
     * Lista do przechowywania nastêpców dla regu³y.
     */
    private List<Successor> successors = new List<Successor>();

    /**
     * Struktura danych typu Tuple przechowuj¹ca delegat funkcji matematycznej dla warunku regu³y razem z indeksem
     * parametru symbolu, którego delegat dotyczy.
     */
    private (int index, Func<float, bool> func) compiledParamCondition;    // comparison with parameter index

    /**
     * Obiekt typu Context przechowuj¹cy skompilowany kontekst regu³y.
     */
    private Context compiledContext;

    /**
     * Lista przechowuj¹ca parametry poprzednika razem z ich nazwami.
     */
    private List<(char name, float value)> predecessorParams;   // for passing the names of predecessor params together with current values

    /**
     * Sta³a zmienna typu char okreœlaj¹ca nazwê parametru dla przypadków, gdzie jej brak.
     */
    private const char noName = '#';

    /**
     * Metoda zarz¹dzaj¹ca kompilacj¹ poszczególnych elementów regu³y produkcyjnej.
     */
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
    
    /**
     * Metoda kompiluj¹ca poprzenika z regu³y.
     */
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

    /**
     * Metoda kompiluj¹ca warunek z regu³y.
     */
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

    /**
     * Metoda znajduj¹ca funkcjê delegata dla porównania parametru w warunku regu³y produkcyjnej.
     * @param condition warunek w postaci zmiennej typu string.
     * @return Func<float, bool> znaleziony delegat porównuj¹cy parametr.
     */
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

    /**
     * Metoda kompiluj¹ca nastêpcê dla regu³y na podstawie ci¹gu znaków typu string i podanego 
     * prawdopodobieñstwa wylosowania nastêpcy.
     * @param pattern ci¹g znaków wpisany przez u¿ytkownika jako nastêpca.
     * @param probability liczba przypisana do nastêpcy jako jego prawdopodobieñstwo
     */
    private void CompileSuccessor(string pattern, int probability)
    {
        bool skipCharacters = false;
        Successor successor = new Successor(probability, predecessor[0]);
        int parametricSymbolOccurrenceIndex = -1;
        int bracketAmount = 0;
        string symbolParamString = "";

        foreach (char c in pattern)
        {
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

    /**
     * Metoda aplikuj¹ca regu³ê produkcyjn¹ dla danego symbolu.
     * @param symbol symbol dla którego aplikowana jest regu³a.
     * @param currentWord ca³oœæ ci¹gu symboli.
     * @param symbolIndex indeks symbolu w ci¹gu znaków.
     * @return List<Symbol> nastêpca w postaci listy nowych symboli.
     */
    public List<Symbol> ApplyRule(Symbol symbol, List<Symbol> currentWord, int symbolIndex)
    {
        // If the first character of the predecessor is not the symbol's character, the rule doesn't apply
        if (!symbol.HasChar(predecessor[0]))
        {
            return new List<Symbol>() { };
        }

        if (symbol.IsParametric)    // Assign the parameter values to their names cause context might use them
        {
            // Assign parameter values to parameter names defined by the predecessor
            for (int i = 0; i < symbol.parameters.Length; i++)
            {

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
            return GetWeightedRandomSuccessor().GetSymbolClones();
        }
        // If there is no successor, return a new list with just the symbol
        else
        {
            return new List<Symbol>() { symbol.Clone() };
        }
    }

    /**
     * Metoda aplikuj¹ca operacje na parametrach dla danego symbolu poprzednika.
     * @param currentSymbol symbol poprzednika.
     * @return List<Symbol> nastêpca w postaci listy symboli.
     */
    private List<Symbol> ApplySuccessorOperations(Symbol currentSymbol)
    {
        // Check probabilities - get weighted random
        Successor successor = GetWeightedRandomSuccessor();

        // Apply operations to successor symbols
        List<Symbol> evaluatedSuccessor = successor.ApplyOperations(currentSymbol, predecessorParams);

        // Return the successor (list of symbols)
        return evaluatedSuccessor;
    }

    /**
     * Metoda zwracaj¹ca losowego nastêpcê z listy nastêpców na podstawie ich prawdopodobieñstw.
     * @return Successor wylosowany nastêpca.
     */
    private Successor GetWeightedRandomSuccessor()
    {
        int totalSum = userSuccessors.Values.Sum();
        int random = UnityEngine.Random.Range(1, totalSum + 1);
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
        return new Successor();
    }
}

