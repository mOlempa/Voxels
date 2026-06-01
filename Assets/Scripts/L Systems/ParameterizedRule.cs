using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public enum ParamName
{
    p0,
    p1,
    p2
}

public enum LogicOperator
{
    EqualTo,
    BiggerThan,
    BiggerOrEqualTo,
    LessThan,
    LessOrEqualTo
}

[Serializable]
public struct Parameter
{
    [SerializeField] public ParameterizedRule[] rules;
}


[Serializable]
public class ParameterizedRule
{
    [HideInInspector] public string name;
    public LogicOperator logicOperator;
    public string comparedVariable;
    public float comparedVariableInt; // MAKE IT ASSIGN INTS AT START AND SAVE THEM (NOT RECALCULATE EVERY TIME!!)
    public string successor;

    private string _prefix;
    private const string Suffix = ")";
    private List<Func<float, float>> _compiledOps;

    // Reusable StringBuilder to prevent garbage generation during frequent calls
    private StringBuilder _stringBuilder;

    public bool CompareParameter(float p)
    {
        float.TryParse(comparedVariable.ToString(), out comparedVariableInt);
        switch (logicOperator)
        {
            default:
            case LogicOperator.EqualTo:
                return p == comparedVariableInt;
            case LogicOperator.BiggerThan:
                return p > comparedVariableInt;
            case LogicOperator.BiggerOrEqualTo:
                return p >= comparedVariableInt;
            case LogicOperator.LessThan:
                return p < comparedVariableInt;
            case LogicOperator.LessOrEqualTo:
                return p <= comparedVariableInt;
        }
    }


    public void CompileRule()
    {
        _stringBuilder = new StringBuilder();
        _compiledOps = new List<Func<float, float>>();
        CompileSuccessor(successor);
    }

    // Call this during initialization (once)
    private void CompileSuccessor(string pattern)
    {
        int openBracket = pattern.IndexOf('(');
        int closeBracket = pattern.IndexOf(')');

        if (openBracket == -1 || closeBracket == -1)
        {
            if(openBracket == -1 && closeBracket == -1){
                return;
            }
            Debug.LogError($"[Evaluator] Invalid pattern format: {pattern}");
            return;
        }

        // Extract the prefix (e.g., "F(")
        _prefix = pattern.Substring(0, openBracket + 1);

        // Extract the arguments inside the brackets (e.g., "+1,*2")
        string argsContent = pattern.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] tokens = argsContent.Split(',');

        foreach (string token in tokens)
        {
            string trimmed = token.Trim();

            // Compile relative addition: "+1"
            if (trimmed.StartsWith("+"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                _compiledOps.Add(x => x + val);
            }
            // Compile relative subtraction: "-1"
            else if (trimmed.StartsWith("-"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                _compiledOps.Add(x => x - val);
            }
            // Compile relative multiplication: "*2"
            else if (trimmed.StartsWith("*"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                _compiledOps.Add(x => x * val);
            }
            // Compile relative division: "/2"
            else if (trimmed.StartsWith("/"))
            {
                float val = float.Parse(trimmed.Substring(1), CultureInfo.InvariantCulture);
                _compiledOps.Add(x => x / val);
            }
            // Compile absolute constants: "0" or "5.5" (ignores the input parameter)
            else
            {
                float val = float.Parse(trimmed, CultureInfo.InvariantCulture);
                _compiledOps.Add(x => val);
            }
        }

    }

    // Call this multiple times in your application loops
    public string EvaluateSuccessor(List<float> parameters)
    {
        // If there are no parameters in the successor
        if (_compiledOps.Count == 0) return successor;

        _stringBuilder.Clear();
        _stringBuilder.Append(_prefix);

        for (int i = 0; i < _compiledOps.Count; i++)
        {
            // Fallback to 0 if the provided list has fewer elements than the rule expects
            float input = (i < parameters.Count) ? parameters[i] : 0f;

            // Execute the pre-compiled lambda function
            float result = _compiledOps[i](input);

            // Append using InvariantCulture to ensure dots are used instead of commas in European regions
            _stringBuilder.Append(result.ToString(CultureInfo.InvariantCulture));

            if (i < _compiledOps.Count - 1)
            {
                _stringBuilder.Append(",");
            }
        }

        _stringBuilder.Append(Suffix);
        return _stringBuilder.ToString();
    }
}

