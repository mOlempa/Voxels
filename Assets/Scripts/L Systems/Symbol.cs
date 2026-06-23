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

[Serializable]
public struct Symbol
{
    [HideInInspector] public string name;

    [SerializeField] public char character;
    [SerializeField] public Action action;

    /*[SerializedDictionary("Symbol", "Action")]
    public SerializedDictionary<char, Action> stringSuccessors;*/

    //[HideInInspector] public Dictionary<List<Symbol>, int> successors;
    //[SerializeField] public Parameter[] parameters;

    [HideInInspector] public float[] parameters;


    /*[SerializedDictionary("Name", "Value")]
    [SerializeField] public SerializedDictionary<char, float> parameters;*/
    //public Dictionary<char, float> parameters;

    // Constructor for Standard Symbol
    public Symbol(char _character)
    {
        name = _character.ToString();
        character = _character;
        parameters = null;
        action = Action.None;
    }

    // Constructor for Parametric Symbol
    public Symbol(char _character, float[] _parameters)
    {
        character = _character;
        parameters = _parameters;
        action = Action.None;
        name = _character.ToString();
    }

    public bool HasChar(char c)
    {
        return character == c;
    }

    public Symbol Clone()
    {
        return new Symbol
        {
            name = this.name,
            character = this.character,
            parameters = parameters != null ? (float[])this.parameters.Clone() : null
        };
    }

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

    public bool IsParametric => parameters != null && parameters.Length > 0;

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


    public static string GetSymbolListString(List<Symbol> list)
    {
        StringBuilder str = new StringBuilder("");
        foreach (Symbol s in list)
        {
            str.Append(s.GetSymbolString());
        }
        return str.ToString();

        /*string str = "";
        foreach (Symbol s in list)
        {
            str += s.GetSymbolString();
        }
        return str;*/
    }
}
