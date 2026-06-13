using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
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
        if (IsParametric)
        {
            string s = name + "(";
            foreach(var p in parameters)
            {
                s += p.ToString(new CultureInfo("en-US"));
                s += ",";
            }
            s = s.Remove(s.Length - 1);
            s += ")";
            return s;
        }
        else
        {
            return name;
        }
    }

    public bool IsParametric => parameters != null && parameters.Length > 0;

    /*public bool isParametrized
    {
        get
        {
            if (parameters == null || parameters.Length == 0)
                return false;
            else
                return true;
        }
    }*/


    /*public bool isConstant
    {
        get
        {
            if (!isParametrized && (stringSuccessors == null || stringSuccessors.Count == 0))
                return true;
            else
                return false;
        }
    }
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

        /*if(extractedParams.Count == parameters.Count)
        {
            for(int i = 0; i < extractedParams.Count; i++)
            {
                parameters[i] = extractedParams[i];
            }

        }*/
    }

    /*public List<Symbol> GetWeightedRandomSuccessor()
    {
        int totalSum = successors.Values.Sum();
        int random = UnityEngine.Random.Range(0, totalSum);
        foreach (var kvp in successors)
        {
            // If random number is smaller than probability of the successor, return the successor
            if (random <= kvp.Value)
            {
                return kvp.Key;
            }
            // Otherwise reduce random value by the probability of the current successor and go to the next one
            random -= kvp.Value;
        }
        // If for any reason a successor was not chosen before, just return the original symbol
        return new List<Symbol>() { this };

    }*/

}
