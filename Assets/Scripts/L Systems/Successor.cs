using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Linq;

/**
 * Klasa reprezentuj¹ca nastêpcê dla regu³ produkcyjnych L-systemu.
 */
public struct Successor
{
    /**
     * Lista przechowuj¹ca symbole nastêpcy.
     */
    public List<Symbol> successorSymbols;

    /**
     * Zmienna typu int okreœlaj¹ca prawdopodobieñstwo wylosowania danego nastêpcy.
     */
    public int probability;

    /**
     * Struktura danych typu Dictionary przechowuj¹ca indeksy dla poszczególnych parametryzowanych symboli 
     * i przypisan¹ do nich listê delegatów operacji na parametrach danego symbolu.
     * Przyk³adowo dla ci¹gu symboli F(x+1,y)BA(x-10) s³ownik wygl¹da nastêpuj¹co: {0: [+1, =], 1: [-10]}.
     */
    public Dictionary<int, List<Func<float, float>>> indexedOperations;

    // Saves the names of parameters used at each occurrence of a param symbol in successor
    /**
     * Struktura danych typu Dictionary przechowuj¹ca nazwy parametrów danych symboli wraz z ich indeksem 
     * wystêpowania jako symbol parametryzowany.
     */
    public Dictionary<int, char[]> namedParams;

    /**
     * Zmienna typu char przechowuj¹ca znak symbolu poprzednika.
     */
    [HideInInspector] public char predecessorSymbolChar;

    /**
     * Konstruktor obiektu klasy.
     * @param probability prawdopodobieñstwo wylosowania nastêpcy.
     * @param symbolChar znak symbolu poprzednika.
     */
    public Successor(int probability, char symbolChar)
    {
        successorSymbols = new List<Symbol>();
        indexedOperations = new Dictionary<int, List<Func<float, float>>>();
        this.probability = probability;
        predecessorSymbolChar = symbolChar;
        namedParams = new Dictionary<int, char[]>();
    }

    /**
     * Metoda zwracaj¹ca kopie symbolów nastêpcy.
     * @return List ci¹g symboli nastêpcy.
     */
    public List<Symbol> GetSymbolClones()
    {
        List<Symbol> result = new List<Symbol>();
        foreach (Symbol symbol in successorSymbols) result.Add(symbol.Clone());
        return result;
    }

    /**
     * Metoda aplikuj¹ca operacje przypisane do nastêpcy do parametrów danego symbolu.
     * @param currentSymbol symbol, do którego parametrów zaaplikowane zostan¹ operacje
     * @param predecessorParams lista nazw i wartoœci parametrów poprzednika.
     * @return List ci¹g symboli z parametrami wynikowymi po zaaplikowaniu operacji.
     */
    public List<Symbol> ApplyOperations(Symbol currentSymbol,
        List<(char name, float value)> predecessorParams)  // gives us current value of the named parameter
    {
        int parametricSymbolOccurrenceIndex = -1;
        List<Symbol> symbolList = new List<Symbol>();

        // return empty list if there are no successors
        if (successorSymbols == null || successorSymbols.Count == 0) return symbolList;

        // Go through each symbol in the successor
        foreach (Symbol symbol in successorSymbols)
        {
            // If symbol is prepared for parameters
            if (symbol.parameters != null)
            {
                parametricSymbolOccurrenceIndex++;
                // go through each saved parameter from the successor and apply equivalent operation
                // (they are saved the same time and list lengths should be the same)

                // find the names of the parameter for this symbol and their indexes
                char[] names = namedParams[parametricSymbolOccurrenceIndex];

                // find the value from the current parameter value and execute operations on it
                for (int i = 0; i < names.Length; i++)
                {
                    (char name, float value) p = predecessorParams.Find(x => x.name == names[i]);
                    symbol.parameters[i] = indexedOperations[parametricSymbolOccurrenceIndex][i](p.value);
                    //Debug.Log($"Executing: {p.name} {p.value} [operation] = {symbol.parameters[i]}");
                }
            }
            // If no params given, yet it is the same symbol that gets exchanged and there were parameters earlier, inherit them
            else if(currentSymbol.IsParametric && symbol.HasChar(currentSymbol.character))
            {
                Symbol s = symbol.Clone();
                s.parameters = currentSymbol.parameters;
                symbolList.Add(s.Clone());
                continue;
            }
            symbolList.Add(symbol.Clone());
        }


        return new List<Symbol>(symbolList);
    }


}