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


public class LSystemGenerator : MonoBehaviour
{
    [Range(0, 10)]
    public int iterationLimit = 1;
    public Grammar grammar;
    public bool enablePrintDebug = false;

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
        string sentence = "";
        int symbolIndex;
        for (int i = 0; i < iterationLimit; i++)
        {
            printDebug("Iteration index: " + i + ", word: <color=yellow>" + GetSymbolListString(word) + "</color>");
            symbolIndex = 0;
            foreach (Symbol symbol in word)
            {
                List<Symbol> successorSymbolList = new List<Symbol>();
                foreach (Rule rule in grammar.rules)
                {
                    successorSymbolList = new List<Symbol>(rule.ApplyRule(symbol, word, symbolIndex));
                    //printDebug("Successor: " + GetSymbolListString(successorSymbolList));    

                    if (successorSymbolList.Count > 0)
                    {
                        nextWord.AddRange(successorSymbolList);
                        break;
                    }
                }

                // If no successor was determined, the symbol is constant
                if(successorSymbolList.Count == 0) nextWord.Add(symbol);
               // printDebug("nextWord: " + GetSymbolListString(nextWord));
                symbolIndex++;
            }
            sentence = "";
            foreach (Symbol symbol in nextWord)
            {
                sentence += symbol.GetSymbolString();
            }
            //print(sentence);
            word = new List<Symbol>(nextWord);
            nextWord.Clear();

        }

        printDebug("Final sentence: <color=yellow>" + sentence + "</color>");

        return word;

        //return GrowRecursive(word);
    }

    private string GetSymbolListString(List<Symbol> list)
    {
        string str = "";
        foreach(Symbol s in list)
        {
            str += s.GetSymbolString();
        }
        return str;
    }



    void printDebug(string str)
    {
        if(enablePrintDebug)Debug.Log(str);
    }
}
