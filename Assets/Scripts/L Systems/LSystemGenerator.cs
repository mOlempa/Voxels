using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/**
 * Klasa odpowiedzialna za generowanie ci¹gu znaków L-systemu.
 */
public class LSystemGenerator : MonoBehaviour
{
    /**
     * Zmienna typu int okreœlaj¹ca limit iteracji algorytmu.
     */
    [Range(0, 30)]
    public int iterationLimit = 1;

    /**
     * Referencja do obiektu gramatyki L-systemu.
     */
    public Grammar grammar;

    /**
     * Zmienna typu bool umo¿liwiaj¹ca w³¹czenie wyœwietlania wybranych informacji dzia³ania programu w konsoli edytora.
     */
    public bool enablePrintDebug = false;

    /**
     * Metoda zawieraj¹ca pêtlê przechodzenia po kolejnych symbolach ci¹gu dla ka¿dej iteracji.
     * @param startingWord opcjonalny parametr do przekazania pocz¹tkowego ci¹gu symboli w postaci zmiennej string.
     * @return List ci¹g symboli bêd¹cych wynikiem wszystkich iteracji.
     */
    public List<Symbol> GenerateSentence(string startingWord = null)
    {
        if (grammar == null)
        {
            return new List<Symbol>();
        }

        grammar.CompileGrammar();


        if (startingWord == null) startingWord = grammar.rootSentence;

        List<Symbol> word = grammar.ConvertStringToSymbols(startingWord);
        List<Symbol> nextWord = new List<Symbol>();
        int symbolIndex;
        for (int i = 0; i < iterationLimit; i++)
        {
            printDebug("Iteration index: " + i + ", word: <color=yellow>" + Symbol.GetSymbolListString(word) + "</color>");
            symbolIndex = 0;
            foreach (Symbol symbol in word)
            {
                List<Symbol> successorSymbolList = new List<Symbol>();
                foreach (Rule rule in grammar.rules)
                {
                    successorSymbolList = rule.ApplyRule(symbol, word, symbolIndex);

                    if (successorSymbolList.Count > 0)
                    {
                        nextWord.AddRange(successorSymbolList);
                        break;
                    }
                }

                // If no successor was determined, the symbol is constant
                if(successorSymbolList.Count == 0) nextWord.Add(symbol);
                symbolIndex++;
            }

            word = new List<Symbol>(nextWord);
            nextWord.Clear();

        }

        printDebug("Final sentence: <color=yellow>" + Symbol.GetSymbolListString(word) + "</color>");


        return word;
    }

    /**
     * Metoda wyœwietlaj¹ca tekst w konsoli edytora.
     * @param str tekst do wyœwietlenia
     */
    void printDebug(string str)
    {
        if(enablePrintDebug)Debug.Log(str);
    }
}
