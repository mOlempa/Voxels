using AYellowpaper.SerializedCollections;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/**
 * Typ zmiennej akcji, definiuj¹cy mo¿liwe znaczenia symboli dla budowy struktury.
 */
public enum Action { 
    None,
    PlaceLine,
    RotateLeft, 
    RotateRight,
    RotateForward,
    RotateBackward,
    StartBranch,
    EndBranch,
    PlaceLeaf,
    RotateRandomDir,
    RotateAxis
}

/**
 * Klasa reprezentuj¹ca gramatykê L-systemu.
 */
[CreateAssetMenu(menuName = "LSystems/Grammar")]
[ExecuteInEditMode]
public class Grammar : ScriptableObject
{
    /**
     * Zmienna typu string definiuj¹ca pocz¹tkowe znaki (axiom) ci¹gu.
     */
    [SerializeField]
    public string rootSentence;

    /**
     * Tablica obiektów typu Symbol zawieraj¹ca zdefiniowane symbole i ich akcje.
     */
    [SerializeField]
    public Symbol[] definedSymbols;

    /**
     * Tablica obiektów typu Rule zawieraj¹ca zdefiniowane regu³y produkcyjne.
     */
    [SerializeField]
    public Rule[] rules;

    /**
     * Struktura danych typu Dictionary przechowuj¹ca pary znak-symbol.
     */
    public Dictionary<char, Symbol> symbols = new Dictionary<char, Symbol>();

    /**
     * Metoda kompiluj¹ca gramatykê.
     */
    public void CompileGrammar()
    {
        UpdateSymbolDictionary();
        foreach(var rule in rules)
        {
            //rule.ReadCondition();
            rule.CompileRule();
        }
    }

    /**
     * Metoda konwertuj¹ca ci¹g znaków typu string na obiekty typu Symbol.
     * @param str ci¹g znaków do konwersji na symbole.
     * @return List lista przekonwertowanych obiektów typu Symbol.
     */
    public List<Symbol> ConvertStringToSymbols(string str)
    {
        List<Symbol> wordSymbols = new List<Symbol>();

        bool symbolIsParameterized = false;
        string paramStr = "";

        foreach (char c in str)
        {
            // PROCESSING PARAMETERS (if specified by previous loop iteration)
            if (symbolIsParameterized)
            {
                // If the brackets just closed, finish processing parameters
                if (c == ')')
                {
                    // Create a new symbol that is parameterized
                    Symbol newSymbol = new Symbol(wordSymbols.Last().character, GetParamsFromString(paramStr));

                    // Remove symbol that was last added to the list and add the new parameterized one
                    wordSymbols.RemoveAt(wordSymbols.Count - 1);
                    wordSymbols.Add(newSymbol);

                    // End processing parameters' part
                    symbolIsParameterized = false;
                    paramStr = "";
                    continue;
                }

                // Add every character after opening bracket to the string (except the closing bracket)
                if (c != '(') paramStr += c;

                continue;
            }

            IsSymbolDefined(c, out Symbol symbol);

            if (c == '(')
            {
                symbolIsParameterized = true;
                continue;
            }

            wordSymbols.Add(symbol);
        }

        return wordSymbols;
    }


    /**
     * Metoda zwracaj¹ca wartoœci parametrów z ci¹gu znaków typu string.
     * @param paramStr ci¹g znaków, z których wyci¹gane s¹ wartoœci parametrów.
     * @return float[] tablica wartoœci wyci¹gniêtych parametrów.
     */
    private float[] GetParamsFromString(string paramStr)
    {
        string numberStr = "";
        List<float> extractedParams = new List<float>();

        foreach (char c in paramStr)
        {
            //print("CHAR " + c);
            if (c == ',')
            {
                float.TryParse(numberStr.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float value);
                extractedParams.Add(value);
                //print("Added " + value + " to extracted params");
                numberStr = "";
                continue;
            }
            numberStr += c;
        }

        float.TryParse(numberStr.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float v);
        extractedParams.Add(v);
        return extractedParams.ToArray();
    }


    /**
     * Metoda sprawdzaj¹ca czy znak jest zdefiniowany jako symbol w obiekcie gramatyki.
     * @param c znak do sprawdzenia.
     * @param symbol symbol do zwrócenia w przypadku, gdy znak jest zdefiniowany.
     * @return bool czy znak jest zdefiniowany.
     */
    public bool IsSymbolDefined(char c, out Symbol symbol)
    {
        foreach (Symbol s in definedSymbols)
        {
            if (s.HasChar(c)) // If true, returns the copy of the symbol
            {
                symbol = s;
                return true;
            }
        }
        symbol = new Symbol(c);
        return false;
    }

    /**
     * Metoda aktualizuj¹ca s³ownik przechowuj¹cy zdefiniowane symbole.
     */
    public void UpdateSymbolDictionary()
    {
        foreach(Symbol s in definedSymbols)
        {
            if (symbols.ContainsKey(s.character))
                symbols[s.character] = s;
            else
                symbols.Add(s.character, s);
        }
    }
    
    /**
     * Metoda zwracaj¹ca akcjê zdefiniowanego symbolu.
     * @param symbol symbol, z którego zwracana jest akcja, jeœli jest on zdefiniowany.
     * @return Action akcja symbolu, w przypadku braku symbolu w gramatyce zwracane jest Action.None.
     */
    public Action GetSymbolAction(Symbol symbol)
    {
        if (symbols.ContainsKey(symbol.character))
        {
            return symbols[symbol.character].action;
        }
        else
        {
            return Action.None;
        }
    }


#if UNITY_EDITOR
    /**
     * Metoda aktualizuj¹ca widok gramatyki w inspektorze edytora Unity.
     */
    private void OnValidate()
    {
        if (definedSymbols != null)
        {
            for (int i = 0; i < definedSymbols.Length; i++)
            {
                definedSymbols[i].name = definedSymbols[i].character.ToString();
                UpdateSymbolDictionary();

            }
        }
    }
#endif

}



