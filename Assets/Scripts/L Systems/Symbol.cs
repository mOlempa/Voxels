using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.TextCore.Text;

/**
 * Klasa reprezentuj¹ca pojedynczy symbol w algorytmie L-systemów.
 */
[Serializable]
public struct Symbol
{
    /**
     * Zmienna typu string okreœlaj¹ca nazwê symbolu.
     */
    [HideInInspector] public string name;

    /**
     * Zmienna typu char okreœlaj¹ca znak symbolu.
     */
    [SerializeField] public char character;

    /**
     * Zmienna typu Action okreœlaj¹ca akcjê symbolu do interpretacji podczas budowy struktury.
     */
    [SerializeField] public Action action;

    /**
     * Tablica wartoœci typu float przechowuj¹ca wartoœci parametrów symbolu.
     */
    [HideInInspector] public float[] parameters;


    /**
     * Konstruktor obiektu klasy dla standardowego symbolu.
     */
    public Symbol(char _character)
    {
        name = _character.ToString();
        character = _character;
        parameters = null;
        action = Action.None;
    }

    /**
     * Konstruktor obiektu klasy dla parametrycznego symbolu.
     */
    public Symbol(char _character, float[] _parameters)
    {
        character = _character;
        parameters = _parameters;
        action = Action.None;
        name = _character.ToString();
    }


    /**
     * Metoda sprawdzaj¹ca czy symbol ma dany znak.
     */
    public bool HasChar(char c)
    {
        return character == c;
    }

    /**
     * Metoda klonuj¹ca symbol.
     * @return Symbol kopia symbolu.
     */
    public Symbol Clone()
    {
        return new Symbol
        {
            name = this.name,
            character = this.character,
            parameters = parameters != null ? (float[])this.parameters.Clone() : null
        };
    }

    /**
     * Metoda zwracaj¹ca symbol jako zmienna typu string ³¹cznie z parametrami.
     * @return string symbol jako ci¹g znaków.
     */
    public string GetSymbolString()
    {
        StringBuilder s = new StringBuilder("");
        if (IsParametric)
        {
            s.Append(name + "(");
            foreach(var p in parameters)
            {
                s.Append(p.ToString(new CultureInfo("en-US")));
                s.Append(",");
            }
            s = s.Remove(s.Length - 1, 1);
            s.Append(")");
            return s.ToString();
        }
        else
        {
            return name;
        }
    }

    /**
     * Metoda sprawdzaj¹ca czy symbol jest parametryczny.
     */
    public bool IsParametric => parameters != null && parameters.Length > 0;

    /**
     * Metoda przypisuj¹ca wartoœci z ci¹gu znaków string parametrom symbolu.
     * @param paramStr ci¹g znaków z wartoœciami parametrów do zapisania.
     */
    public void AssignParameterValues(string paramStr)
    {
        string numberStr = "";
        List<float> extractedParams = new List<float>();
        //Debug.Log("Param string: " + paramStr);

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
        parameters = extractedParams.ToArray();

    }

    /**
     * Metoda zwracaj¹ca listê symboli w postaci ci¹gu znaków string.
     * @param list lista symboli do przekonwertowania.
     * @return string przekonwertowana lista.
     */
    public static string GetSymbolListString(List<Symbol> list)
    {
        StringBuilder str = new StringBuilder("");
        foreach (Symbol s in list)
        {
            str.Append(s.GetSymbolString());
        }
        return str.ToString();

    }
}
