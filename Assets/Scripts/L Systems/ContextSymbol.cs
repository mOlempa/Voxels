using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;

/**
 * Klasa reprezentuj¹ca symbol kontekstu regu³y produkcyjnej.
 */
public class ContextSymbol
{
    /**
     * Zmienna typu char okreœlaj¹ca znak symbolu.
     */
    public char character;

    /**
     * Lista przechowuj¹ca warunki dla parametrów w postaci delegata porównuj¹cego wartoœci.
     */
    public List<Func<float, float, bool>> paramConditions;

    /**
     * Lista przechowuj¹ca nazwy lub wartoœci parametrów do porównania dla sprawdzenia stosowalnoœci kontekstu.
     */
    public List<object> comparisonVariables;

    /**
     * Konstruktor obiektu klasy.
     * @param c znak symbolu.
     */
    public ContextSymbol(char c)
    {
        character = c;
        paramConditions = new List<Func<float, float, bool>>();
        comparisonVariables = new List<object>();
    }

    /**
     * Konstruktor obiektu klasy.
     * @param c znak symbolu.
     * @param conditions lista warunków do aplikowania kontekstu w postaci delegatów.
     */
    public ContextSymbol(char c, List<Func<float, float, bool>> conditions)
    {
        character = c;
        paramConditions = conditions;
        comparisonVariables = new List<object>();
    }

    /**
     * Metoda kompiluj¹ca warunki aplikowania kontekstu.
     * @param condition warunek aplikowalnoœci kontekstu w postaci ci¹gu znaków string wpisanego przez u¿ytkownika.
     */
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

    /**
     * Metoda porównuj¹ca parametry z warunkami do spe³nienia aby kontekst by³ aplikowalny.
     * @param symbol symbol z parametrami do porównania z warunkiem.
     * @param predecessorParams lista parametrów poprzednika zawieraj¹ca ich nazwy i wartoœci.
     * @return bool stwierdzenie czy wartoœæ parametru symbolu spe³nia warunek.
     */
    public bool CompareVariables(Symbol symbol, List<(char name, float value)> predecessorParams)
    {
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

