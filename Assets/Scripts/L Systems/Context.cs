using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


/**
 * Klasa reprezentuj¹ca kontekst dla regu³ produkcyjnych L-systemów.
 */
public class Context
{
    /**
     * Lista symboli kontekstu przed poprzednikiem.
     */
    public List<ContextSymbol> leftContext;

    /**
     * Lista symboli kontekstu za poprzednikiem.
     */
    public List<ContextSymbol> rightContext;

    /**
     * Konstruktor obiektu klasy.
     */
    public Context()
    {
        leftContext = new List<ContextSymbol>();
        rightContext = new List<ContextSymbol>();
    }

    /**
     * Metoda interpretuj¹ca kontekst regu³y produkcyjnej z ci¹gu znaków typu string.
     * @param userContext kontekst wpisany przez u¿ytkownika w inspektorze.
     */
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

    /**
     * Metoda sprawdzaj¹ca czy skompilowany kontekst jest aplikowalny do symbolu.
     * @param currentWord ca³oœæ ci¹gu symboli w danej iteracji.
     * @param symbolIndex indeks sprawdzanego symbolu w ci¹gu.
     * @param predecessorParams lista parametrów symbolu (poprzednika) z ich nazwami i wartoœciami.
     * @return bool aplikowalnoœæ kontekstu do symbolu.
     */
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

